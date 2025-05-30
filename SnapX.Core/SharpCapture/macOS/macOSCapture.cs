using System.Runtime.Versioning;
using SixLabors.ImageSharp;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Native;
using uniffi.snapxrust;

namespace SnapX.Core.SharpCapture.macOS;

// [SupportedOSPlatform ("maccatalyst18.2")]
// [SupportedOSPlatform("macos12.3")]
// public class macOSCapture : BaseCapture
// {
//     // private const string ScreenCaptureKit = "/System/Library/Frameworks/ScreenCaptureKit.framework/ScreenCaptureKit";
//     private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
//     private const string SharpCaptureDylib = "SharpCapture.dylib";
//
//     [DllImport(CoreGraphics)]
//     private static extern IntPtr CGImageCreateCopy(IntPtr image);
//
//     [DllImport(CoreGraphics)]
//     private static extern void CGImageRelease(IntPtr image);
//
//     [DllImport(CoreGraphics)]
//     private static extern IntPtr CGImageDestinationCreateWithData(IntPtr mutableData, IntPtr type, IntPtr count, IntPtr options);
//
//     [DllImport(CoreGraphics)]
//     private static extern void CGImageDestinationAddImage(IntPtr dest, IntPtr image, IntPtr options);
//
//     [DllImport(CoreGraphics)]
//     private static extern bool CGImageDestinationFinalize(IntPtr dest);
//
//     [DllImport(CoreGraphics)]
//     private static extern IntPtr CFDataGetBytePtr(IntPtr data);
//
//     [DllImport(CoreGraphics)]
//     private static extern int CFDataGetLength(IntPtr data);
//
//     [DllImport(CoreGraphics)]
//     private static extern void CFRelease(IntPtr data);
//
//     [DllImport(CoreGraphics)]
//     private static extern int CGGetActiveDisplayList(int maxDisplays, uint[] activeDisplays, out int displayCount);
//     [DllImport(CoreGraphics)]
//     private static extern uint CGMainDisplayID();
//     [DllImport(CoreGraphics)]
//     private static extern IntPtr CGDisplayBounds(uint displayId);
//
//     [DllImport(SharpCaptureDylib)]
//     private static extern void captureFullscreen(Action<IntPtr?> completion);
//
//     [DllImport(SharpCaptureDylib)]
//     private static extern void captureScreen(NSRect bounds, Action<IntPtr?> completion);
//
//     [DllImport(SharpCaptureDylib)]
//     private static extern void captureScreen(CGFloat posX, CGFloat posY, Action<IntPtr?> completion);
//
//     [DllImport(SharpCaptureDylib)]
//     private static extern void captureWindow(CGFloat posX, CGFloat posY, Action<IntPtr?> completion);
//
//     [DllImport(SharpCaptureDylib)]
//     private static extern void captureRectangle(NSRect rect, Action<IntPtr?> completion);
//
//     [DllImport(SharpCaptureDylib)]
//     private static extern void startContinuousCapture(Action<IntPtr?> completion);
//
//     [DllImport(SharpCaptureDylib)]
//     private static extern void stopContinuousCapture();
//
//     [DllImport(SharpCaptureDylib)]
//     private static extern void getLatestFrame(Action<IntPtr?> completion);
//
//     private Action<Image?>? _continuousFrameCallback;
//     private CancellationTokenSource? _continuousCaptureCancellationTokenSource;
//
//     [StructLayout(LayoutKind.Sequential)]
//     public struct NSRect
//     {
//         public CGFloat x;
//         public CGFloat y;
//         public CGFloat width;
//         public CGFloat height;
//     }
//
//     [StructLayout(LayoutKind.Sequential)]
//     public struct CGFloat
//     {
//         public double Value;
//
//         public static implicit operator CGFloat(double value) => new() { Value = value };
//         public static implicit operator double(CGFloat cgFloat) => cgFloat.Value;
//     }
//     [DllImport(SharpCaptureDylib)]
//     private static extern ulong getNSDataLength(IntPtr data);
//     [DllImport(SharpCaptureDylib)]
//     private static extern void releaseNSData(IntPtr data);
//     private static Image? PngDataToImage(IntPtr? dataPtr)
//     {
//         if (dataPtr == IntPtr.Zero || dataPtr == null)
//         {
//             return null;
//         }
//
//         try
//         {
//             var length = getNSDataLength(dataPtr.Value);
//
//             var buffer = new byte[length];
//             Marshal.Copy(dataPtr.Value, buffer, 0, (int)length);
//             return Image.Load(buffer);
//         }
//         finally
//         {
//             releaseNSData(dataPtr.Value);
//         }
//     }
//
//     public override async Task<Image?> CaptureFullscreen()
//     {
//         var tcs = new TaskCompletionSource<Image?>();
//         Action<IntPtr?> completion = (dataPtr) =>
//         {
//             tcs.SetResult(PngDataToImage(dataPtr));
//         };
//         captureFullscreen(completion);
//         return await tcs.Task;
//     }
//
//     public override async Task<Image?> CaptureScreen(Rectangle bounds)
//     {
//         var tcs = new TaskCompletionSource<Image?>();
//         var nsRect = new NSRect
//         {
//             x = bounds.X,
//             y = bounds.Y,
//             width = bounds.Width,
//             height = bounds.Height
//         };
//         Action<IntPtr?> completion = (dataPtr) =>
//         {
//             tcs.SetResult(PngDataToImage(dataPtr));
//         };
//         captureScreen(nsRect, completion);
//         return await tcs.Task;
//     }
//
//     public override async Task<Image?> CaptureScreen(Point? pos)
//     {
//         if (!pos.HasValue)
//         {
//             return null;
//         }
//
//         TaskCompletionSource<Image?> tcs = new TaskCompletionSource<Image?>();
//         Action<IntPtr?> completion = (dataPtr) =>
//         {
//             tcs.SetResult(PngDataToImage(dataPtr));
//         };
//         captureScreen(pos.Value.X, pos.Value.Y, completion);
//         return await tcs.Task;
//     }
//
//     public override async Task<Image?> CaptureWindow(Point pos)
//     {
//         TaskCompletionSource<Image?> tcs = new TaskCompletionSource<Image?>();
//         Action<IntPtr?> completion = (dataPtr) =>
//         {
//             tcs.SetResult(PngDataToImage(dataPtr));
//         };
//         captureWindow(pos.X, pos.Y, completion);
//         return await tcs.Task;
//     }
//
//     public override async Task<Image?> CaptureRectangle(Rectangle rect)
//     {
//         TaskCompletionSource<Image?> tcs = new TaskCompletionSource<Image?>();
//         NSRect nsRect = new NSRect
//         {
//             x = rect.X,
//             y = rect.Y,
//             width = rect.Width,
//             height = rect.Height
//         };
//         Action<IntPtr?> completion = (dataPtr) =>
//         {
//             tcs.SetResult(PngDataToImage(dataPtr));
//         };
//         captureRectangle(nsRect, completion);
//         return await tcs.Task;
//     }
//
//     public Task StartContinuousCapture(Action<Image?> frameCallback)
//     {
//         if (_continuousCaptureCancellationTokenSource != null)
//         {
//             _continuousCaptureCancellationTokenSource.Cancel();
//             _continuousCaptureCancellationTokenSource.Dispose();
//             _continuousCaptureCancellationTokenSource = null;
//         }
//
//         _continuousFrameCallback = frameCallback;
//         _continuousCaptureCancellationTokenSource = new CancellationTokenSource();
//
//         Action<IntPtr?> completion = (dataPtr) =>
//         {
//             if (_continuousFrameCallback != null && !_continuousCaptureCancellationTokenSource.Token.IsCancellationRequested)
//             {
//                 _continuousFrameCallback(PngDataToImage(dataPtr));
//             }
//         };
//
//         // Start the continuous capture in Swift
//         startContinuousCapture(completion);
//
//         // For single frame capture requests during continuous capture
//         Task.Run(async () =>
//         {
//             try
//             {
//                 while (!_continuousCaptureCancellationTokenSource.Token.IsCancellationRequested)
//                 {
//                     await Task.Delay(16, _continuousCaptureCancellationTokenSource.Token); // Check for new frame roughly every 60 FPS
//                 }
//             }
//             catch (TaskCanceledException)
//             {
//                 // Expected when stopping continuous capture
//             }
//         });
//
//         return Task.CompletedTask;
//     }
//
//     public Task StopContinuousCapture()
//     {
//         stopContinuousCapture();
//         _continuousCaptureCancellationTokenSource?.Cancel();
//         _continuousCaptureCancellationTokenSource?.Dispose();
//         _continuousCaptureCancellationTokenSource = null;
//         _continuousFrameCallback = null;
//         return Task.CompletedTask;
//     }
//
//     private async Task<Image?> GetOneFrame()
//     {
//         TaskCompletionSource<Image?> tcs = new TaskCompletionSource<Image?>();
//         Action<IntPtr?> completion = (dataPtr) =>
//         {
//             tcs.SetResult(PngDataToImage(dataPtr));
//         };
//
//         // Temporarily start and immediately try to get the latest frame
//         startContinuousCapture(completion);
//         await Task.Delay(50); // Give it a short time to capture a frame
//         stopContinuousCapture();
//
//         return await tcs.Task;
//     }
//
//     private static IntPtr ConvertCGImageToPNG(IntPtr cgImage)
//     {
//         var pngType = Marshal.StringToHGlobalAuto("public.png");
//         var mutableData = IntPtr.Zero;
//         var dest = CGImageDestinationCreateWithData(mutableData, pngType, (IntPtr)1, IntPtr.Zero);
//
//         if (dest == IntPtr.Zero)
//         {
//             DebugHelper.WriteLine("Failed to create image destination.");
//             return IntPtr.Zero;
//         }
//
//         CGImageDestinationAddImage(dest, cgImage, IntPtr.Zero);
//         if (!CGImageDestinationFinalize(dest))
//         {
//             DebugHelper.WriteLine("Failed to finalize image destination.");
//             return IntPtr.Zero;
//         }
//
//         Marshal.FreeHGlobal(pngType);
//         return mutableData;
//     }
//     private uint GetDisplayForPoint(Point? pos)
//     {
//         if (!pos.HasValue)
//             return CGMainDisplayID();
//
//         var displays = new uint[16];
//         if (CGGetActiveDisplayList(displays.Length, displays, out var displayCount) != 0)
//             return CGMainDisplayID(); // Fallback
//
//         foreach (var display in displays)
//         {
//             var boundsPtr = CGDisplayBounds(display);
//             if (boundsPtr == IntPtr.Zero) continue;
//
//             var bounds = Marshal.PtrToStructure<CGRect>(boundsPtr);
//             if (pos.Value.X >= bounds.X && pos.Value.X < bounds.X + bounds.Width &&
//                 pos.Value.Y >= bounds.Y && pos.Value.Y < bounds.Y + bounds.Height)
//             {
//                 return display;
//             }
//         }
//
//         return CGMainDisplayID();
//     }
//     private static byte[] CFDataToByteArray(IntPtr data)
//     {
//         var length = CFDataGetLength(data);
//         if (length == 0) return null;
//
//         var bytePtr = CFDataGetBytePtr(data);
//         var managedArray = new byte[length];
//         Marshal.Copy(bytePtr, managedArray, 0, length);
//
//         return managedArray;
//     }
//     [StructLayout(LayoutKind.Sequential)]
//     private struct CGRect
//     {
//         public int X, Y, Width, Height;
//     }
// }

public class macOSCapture : BaseCapture
{
    public override async Task<Image?> CaptureFullscreen()
    {
        return ImageHelpers.ImageDataToImage(SnapxrustMethods.CaptureFullscreen());
    }

    public override async Task<Image?> CaptureScreen(Rectangle bounds)
    {
        return CaptureRectangleNative(bounds);
    }

    public override async Task<Image?> CaptureScreen(Point? pos)
    {
        return CaptureMonitor(Methods.GetCursorPosition());
    }

    public override async Task<Image?> CaptureRectangle(Rectangle rect)
    {
        return CaptureRectangleNative(rect);
    }

    public override async Task<Image?> CaptureWindow(Point pos)
    {
        return ImageHelpers.ImageDataToImage(SnapxrustMethods.CaptureWindow((uint)pos.X, (uint)pos.Y));
    }
    private Image CaptureMonitor(Point pos)
    {
        var monitor = SnapxrustMethods.GetMonitor((uint)pos.X, (uint)pos.Y);
        return ImageHelpers.ImageDataToImage(SnapxrustMethods.CaptureMonitor(monitor.name));
    }
    private Image CaptureRectangleNative(Rectangle rect, bool captureCursor = false)
    {
        return ImageHelpers.ImageDataToImage(SnapxrustMethods.CaptureRect((uint)rect.X, (uint)rect.Y, (uint)rect.Width, (uint)rect.Height));
    }

    public override async Task<Rectangle> GetWorkingArea()
    {
        var ScreenDimensions = SnapxrustMethods.GetWorkingArea();
        return new Rectangle(ScreenDimensions.x, ScreenDimensions.y, (int)ScreenDimensions.width, (int)ScreenDimensions.height);
    }

    public override async Task<Rectangle> GetPrimaryScreen()
    {
        var monitor = SnapxrustMethods.GetPrimaryMonitor();
        return new Rectangle(monitor.x, monitor.y, (int)monitor.width, (int)monitor.height);
    }

    public override async Task<Rectangle> GetScreen(Point pos)
    {
        var monitor = SnapxrustMethods.GetMonitor((uint)pos.X, (uint)pos.Y);
        return new Rectangle(monitor.x, monitor.y, (int)monitor.width, (int)monitor.height);
    }
}
