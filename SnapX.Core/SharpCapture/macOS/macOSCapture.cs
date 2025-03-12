using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SnapX.Core.SharpCapture.macOS;

[SupportedOSPlatform ("maccatalyst18.2")]
[SupportedOSPlatform("macos12.3")]
public class macOSCapture : BaseCapture
{
    private const string ScreenCaptureKit = "/System/Library/Frameworks/ScreenCaptureKit.framework/ScreenCaptureKit";
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [DllImport(ScreenCaptureKit)]
    private static extern IntPtr SCStreamCreateSnapshot(IntPtr stream, out IntPtr error);

    [DllImport(ScreenCaptureKit)]
    private static extern void SCStreamReleaseSnapshot(IntPtr snapshot);

    [DllImport(ScreenCaptureKit)]
    private static extern IntPtr SCStreamCopyBitmapRepresentation(IntPtr snapshot);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGImageCreateCopy(IntPtr image);

    [DllImport(CoreGraphics)]
    private static extern void CGImageRelease(IntPtr image);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGImageDestinationCreateWithData(IntPtr mutableData, IntPtr type, IntPtr count, IntPtr options);

    [DllImport(CoreGraphics)]
    private static extern void CGImageDestinationAddImage(IntPtr dest, IntPtr image, IntPtr options);

    [DllImport(CoreGraphics)]
    private static extern bool CGImageDestinationFinalize(IntPtr dest);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CFDataGetBytePtr(IntPtr data);

    [DllImport(CoreGraphics)]
    private static extern int CFDataGetLength(IntPtr data);

    [DllImport(CoreGraphics)]
    private static extern void CFRelease(IntPtr data);

    [DllImport(CoreGraphics)]
    private static extern int CGGetActiveDisplayList(int maxDisplays, uint[] activeDisplays, out int displayCount);
    [DllImport(CoreGraphics)]
    private static extern uint CGMainDisplayID();
    [DllImport(CoreGraphics)]
    private static extern IntPtr CGDisplayBounds(uint displayId);

    public override async Task<Image?> CaptureFullscreen()
    {
        return await Task.Run(() =>
        {
            IntPtr error;
            var snapshot = SCStreamCreateSnapshot(IntPtr.Zero, out error);
            if (snapshot == IntPtr.Zero)
            {
                DebugHelper.WriteLine("Failed to capture screenshot.");
                return null;
            }

            var bitmap = SCStreamCopyBitmapRepresentation(snapshot);
            SCStreamReleaseSnapshot(snapshot);

            if (bitmap == IntPtr.Zero)
            {
                DebugHelper.WriteLine("Failed to retrieve bitmap.");
                return null;
            }

            var cgImage = CGImageCreateCopy(bitmap);
            if (cgImage == IntPtr.Zero)
            {
                DebugHelper.WriteLine("Failed to create CGImage.");
                return null;
            }

            var pngData = ConvertCGImageToPNG(cgImage);
            CGImageRelease(cgImage);

            if (pngData == IntPtr.Zero)
            {
                DebugHelper.WriteLine("Failed to convert image to PNG.");
                return null;
            }

            var pngBytes = CFDataToByteArray(pngData);
            CFRelease(pngData);
            return Image.Load(pngBytes);
        });
    }
    public override async Task<Image?> CaptureScreen(Point? pos)
    {
        return await Task.Run(() =>
        {
            var displayId = GetDisplayForPoint(pos);
            if (displayId == 0)
            {
                DebugHelper.WriteLine("No display found for given position.");
                return null;
            }

            var snapshot = SCStreamCreateSnapshot(IntPtr.Zero, out var error);
            if (snapshot == IntPtr.Zero)
            {
                DebugHelper.WriteLine("Failed to capture screenshot.");
                return null;
            }

            var bitmap = SCStreamCopyBitmapRepresentation(snapshot);
            SCStreamReleaseSnapshot(snapshot);

            if (bitmap == IntPtr.Zero)
            {
                DebugHelper.WriteLine("Failed to retrieve bitmap.");
                return null;
            }

            var cgImage = CGImageCreateCopy(bitmap);
            if (cgImage == IntPtr.Zero)
            {
                DebugHelper.WriteLine("Failed to create CGImage.");
                return null;
            }

            var pngData = ConvertCGImageToPNG(cgImage);
            CGImageRelease(cgImage);

            if (pngData == IntPtr.Zero)
            {
                DebugHelper.WriteLine("Failed to convert image to PNG.");
                return null;
            }

            var pngBytes = CFDataToByteArray(pngData);
            CFRelease(pngData);

            using var ms = new MemoryStream(pngBytes);
            return Image.Load<Rgba32>(ms);
        });
    }

    private static IntPtr ConvertCGImageToPNG(IntPtr cgImage)
    {
        var pngType = Marshal.StringToHGlobalAuto("public.png");
        var mutableData = IntPtr.Zero;
        var dest = CGImageDestinationCreateWithData(mutableData, pngType, (IntPtr)1, IntPtr.Zero);

        if (dest == IntPtr.Zero)
        {
            DebugHelper.WriteLine("Failed to create image destination.");
            return IntPtr.Zero;
        }

        CGImageDestinationAddImage(dest, cgImage, IntPtr.Zero);
        if (!CGImageDestinationFinalize(dest))
        {
            DebugHelper.WriteLine("Failed to finalize image destination.");
            return IntPtr.Zero;
        }

        Marshal.FreeHGlobal(pngType);
        return mutableData;
    }
    private uint GetDisplayForPoint(Point? pos)
    {
        if (!pos.HasValue)
            return CGMainDisplayID();

        var displays = new uint[16];
        if (CGGetActiveDisplayList(displays.Length, displays, out var displayCount) != 0)
            return CGMainDisplayID(); // Fallback

        foreach (var display in displays)
        {
            var boundsPtr = CGDisplayBounds(display);
            if (boundsPtr == IntPtr.Zero) continue;

            var bounds = Marshal.PtrToStructure<CGRect>(boundsPtr);
            if (pos.Value.X >= bounds.X && pos.Value.X < bounds.X + bounds.Width &&
                pos.Value.Y >= bounds.Y && pos.Value.Y < bounds.Y + bounds.Height)
            {
                return display;
            }
        }

        return CGMainDisplayID();
    }
    private static byte[] CFDataToByteArray(IntPtr data)
    {
        var length = CFDataGetLength(data);
        if (length == 0) return null;

        var bytePtr = CFDataGetBytePtr(data);
        var managedArray = new byte[length];
        Marshal.Copy(bytePtr, managedArray, 0, length);

        return managedArray;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public int X, Y, Width, Height;
    }
}
