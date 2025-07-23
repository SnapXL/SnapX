using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32;
using WinRT;


namespace SnapX.Core.SharpCapture.Windows;

[SupportedOSPlatform("windows10.0.19045")]
public class WindowsCapture : BaseCapture
{
    private bool IsSupportedFeatureLevel(IDXGIAdapter1 adapter, FeatureLevel featureLevel,
        DeviceCreationFlags creationFlags)
    {
        var result = D3D11.D3D11CreateDevice(
            adapter,
            DriverType.Hardware,
            creationFlags,
            [featureLevel],
            out var device,
            out var supportedFeatureLevel,
            out _);

        if (result.Success && supportedFeatureLevel == featureLevel)
        {
            device?.Dispose();
            return true;
        }

        device?.Dispose();
        return false;
    }

    public override async Task<Image?> CaptureFullscreen()
    {
        var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>()!;

        var adapters = EnumerateAdapters(factory);

        if (adapters.Count == 0)
        {
            return null;
        }

        var outputs = EnumerateOutputs(adapters);

        if (outputs.Count == 0)
        {
            return null;
        }

        var totalWidth = 0;
        var totalHeight = 0;

        foreach (var (output, x, y, width, height, _) in outputs)
        {
            totalWidth = Math.Max(totalWidth, x + width);
            totalHeight = Math.Max(totalHeight, y + height);
        }

        var combinedImage = new Image<Rgba32>(totalWidth, totalHeight);

        var captureTasks = new List<Task<Image?>>();

        foreach (var (output, x, y, width, height, adapter) in outputs)
        {
            var bounds = new Rectangle(x, y, width, height);
            var captureTask = CaptureOutputImage(output, adapter, bounds);
            captureTasks.Add(captureTask);
        }

        var capturedImages = await Task.WhenAll(captureTasks);

        foreach (var (_, x, y, _, _, _) in outputs)
        {
            var monitorImage = capturedImages.FirstOrDefault(image => image != null);
            if (monitorImage != null)
            {
                combinedImage.Mutate(ctx => ctx.DrawImage(monitorImage, new Point(x, y), 1f));
            }
        }

        foreach (var output in outputs)
        {
            output.Output.Dispose();
        }

        foreach (var adapter in adapters)
        {
            adapter.Dispose();
        }

        return combinedImage;
    }


    public override async Task<Image?> CaptureScreen(Point? pos)
    {
        var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>()!;

        var adapters = EnumerateAdapters(factory);

        if (adapters.Count == 0)
        {
            DebugHelper.WriteLine($"{nameof(WindowsCapture)}: No adapters found");
            return null;
        }

        var outputs = EnumerateOutputs(adapters);

        if (outputs.Count == 0)
        {
            DebugHelper.WriteLine($"{nameof(WindowsCapture)}: No output found");
            return null;
        }

        if (pos.HasValue)
        {
            var targetOutput = outputs.FirstOrDefault(output =>
                pos.Value.X >= output.X && pos.Value.X < output.X + output.Width &&
                pos.Value.Y >= output.Y && pos.Value.Y < output.Y + output.Height);

            if (targetOutput.Equals(default))
            {
                return null;
            }

            var output = targetOutput.Output;
            var adapter = targetOutput.Adapter;
            var bounds = new Rectangle(targetOutput.X, targetOutput.Y, targetOutput.Width, targetOutput.Height);

            return await CaptureOutputImage(output, adapter, bounds);
        }

        var defaultOutput = outputs.FirstOrDefault();
        if (defaultOutput.Equals(default))
        {
            return null;
        }

        var defaultBounds = new Rectangle(defaultOutput.X, defaultOutput.Y, defaultOutput.Width, defaultOutput.Height);
        return await CaptureOutputImage(defaultOutput.Output, defaultOutput.Adapter, defaultBounds);
    }

    [DllImport(
        "d3d11.dll",
        EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
        SetLastError = true,
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall
    )]
    private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
    private static IDirect3DDevice CreateDirect3DDeviceFromVorticeDevice(ID3D11Device d3dDevice)
    {
        IDirect3DDevice device = null;

        // Acquire the DXGI interface for the Direct3D device.
        using var dxgiDevice = d3dDevice.QueryInterface<ID3D11Device3>();
        // Wrap the native device using a WinRT interop object.
        var hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var pUnknown);

        if (hr != 0) return device;
        ComWrappers cw = new DefaultComWrappers();

        device = cw.GetOrCreateObjectForComInstance(pUnknown, CreateObjectFlags.UniqueInstance) as IDirect3DDevice;
        Marshal.Release(pUnknown);

        return device;
    }

    private static ID3D11Texture2D Texture2DFromSurface(IDirect3DSurface surface)
    {
        var access = surface.As<IDirect3DDxgiInterfaceAccess>();
        var texture = access.QueryInterface<ID3D11Texture2D>();
        return texture;
    }
    public override async Task<Image?> CaptureWindow(Point pos)
    {
        if (!GraphicsCaptureSession.IsSupported())
        {
            DebugHelper.WriteLine("WindowsCapture: GraphicsCaptureSession is not supported on this device. Perhaps update your Windows?");
            return null;
        }
        var hwnd = PInvoke.WindowFromPoint(new System.Drawing.Point(pos.X, pos.Y));
        if (hwnd == IntPtr.Zero)
        {
            DebugHelper.WriteLine("WindowsCapture was provided a invalid window handle");
            return null;
        }

        var captureItem = CaptureItemHelper.CreateItemForWindow(hwnd);
        if (captureItem == null)
        {
            DebugHelper.WriteLine("WindowsCapture was provided with a invalid item (null) for Windows.Graphics.Capture to capture window... :(");
            return null;
        }

        using var d3d11Device = D3D11.D3D11CreateDevice(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
        using var device = CreateDirect3DDeviceFromVorticeDevice(d3d11Device);
        if (device == null)
        {
            DebugHelper.WriteLine("WindowsCapture was provided with a invalid  IDirect3DDevice (null) for Windows.Graphics.Capture to capture window... :( ");
            return null;
        }

        var size = captureItem.Size;
        DebugHelper.WriteLine($"Capture Item Size... Width: {size.Width}, Height: {size.Height}");
        using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, DirectXPixelFormat.B8G8R8A8UIntNormalized,
            1,
            size);
        var asyncFrame = new TaskCompletionSource<Direct3D11CaptureFrame>();
        framePool.FrameArrived += (Sender, Args) =>
        {
            asyncFrame.SetResult(Sender.TryGetNextFrame());
            DebugHelper.WriteLine("Frame arrived");
        };
        using var session = framePool.CreateCaptureSession(captureItem);
        if (session == null)
        {
            DebugHelper.WriteLine($"Capture Session could not be created from {captureItem}");
            return null;
        }
        session.IsBorderRequired = false;
        session.IncludeSecondaryWindows = true;
        session.StartCapture();
        using var result = await asyncFrame.Task.WaitAsync(TimeSpan.FromSeconds(1));
        if (result == null)
        {
            DebugHelper.WriteLine($"The frame from framePool ({framePool}) was null for {captureItem} :(");
            return null;
        }

        var width = size.Width;
        var height = size.Height;
        var currentFrame = Texture2DFromSurface(result.Surface);
        var tempTexture = currentFrame.QueryInterface<ID3D11Texture2D>();

        d3d11Device.ImmediateContext.CopyResource(currentFrame, tempTexture);
        var dataBox = d3d11Device.ImmediateContext.Map(currentFrame, 0);

        var screenshotBytes = GetDataAsByteArray(dataBox.DataPointer, (int)dataBox.RowPitch, width,
            height);
        d3d11Device.ImmediateContext.Unmap(currentFrame, 0);
        return Image.LoadPixelData<Rgba32>(screenshotBytes, width, height);

        // var sample = MediaStreamSample.CreateFromDirect3D11Surface(result.Surface, result.SystemRelativeTime);
        // var currentFrame = texture ?? throw new ArgumentNullException(nameof(texture));
    }

    private List<IDXGIAdapter1> EnumerateAdapters(IDXGIFactory1 factory)
    {
        var adapters = new List<IDXGIAdapter1>();

        for (uint adapterIndex = 0; factory.EnumAdapters1(adapterIndex, out var adapter).Success; adapterIndex++)
        {
            var desc = adapter.Description1;

            if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                adapter.Dispose();
                continue;
            }

            if (IsSupportedFeatureLevel(adapter, FeatureLevel.Level_11_1, DeviceCreationFlags.BgraSupport))
            {
                DebugHelper.WriteLine(
                    $"Feature level {FeatureLevel.Level_11_1} not supported. Skipping Adapter {adapter.Description}");
                adapter.Dispose();
                continue;
            }

            adapters.Add(adapter);
        }

        return adapters;
    }

    private List<(IDXGIOutput1 Output, int X, int Y, int Width, int Height, IDXGIAdapter Adapter)> EnumerateOutputs(
        List<IDXGIAdapter1> adapters)
    {
        var outputs = new List<(IDXGIOutput1 Output, int X, int Y, int Width, int Height, IDXGIAdapter Adapter)>();

        foreach (var adapter in adapters)
        {
            for (uint outputIndex = 0; adapter.EnumOutputs(outputIndex, out var output).Success; outputIndex++)
            {
                var firstOutput = output.QueryInterface<IDXGIOutput1>();
                var bounds = firstOutput.Description.DesktopCoordinates;

                var width = bounds.Right - bounds.Left;
                var height = bounds.Bottom - bounds.Top;
                var x = bounds.Left;
                var y = bounds.Top;

                outputs.Add((firstOutput, x, y, width, height, adapter));
            }
        }

        return outputs;
    }

    private static async Task<Image?> CaptureOutputImage(IDXGIOutput1 output, IDXGIAdapter adapter, Rectangle bounds)
    {
        if (output == null) throw new ArgumentNullException(nameof(output));
        if (adapter == null) throw new ArgumentNullException(nameof(adapter));
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new ArgumentException("Invalid bounds", nameof(bounds));
        var hr = D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport,
            [FeatureLevel.Level_11_1], out var device);
        if (hr.Failure) throw new InvalidOperationException($"D3D11CreateDevice failed: {hr}");
        output.GetParent<IDXGIAdapter>(out var outputAdapter);
        if (!outputAdapter?.NativePointer.Equals(adapter.NativePointer) ?? false)
        {
            throw new InvalidOperationException("The IDXGIAdapter used does not match the one used to create this IDXGIOutput1.");
        }
        IDXGIOutput baseOutput = output;
        var outputDesc = baseOutput.Description;
        DebugHelper.WriteLine("=== Output Description ===");
        DebugHelper.WriteLine($"Device Name           : {outputDesc.DeviceName}");
        DebugHelper.WriteLine($"Attached To Desktop   : {outputDesc.AttachedToDesktop}");
        DebugHelper.WriteLine($"Monitor Coordinates   : L:{outputDesc.DesktopCoordinates.Left}, T:{outputDesc.DesktopCoordinates.Top}, R:{outputDesc.DesktopCoordinates.Right}, B:{outputDesc.DesktopCoordinates.Bottom}");
        DebugHelper.WriteLine($"Monitor Handle (HMONITOR): 0x{outputDesc.Monitor.ToInt64():X}");
        DebugHelper.WriteLine($"Rotation              : {outputDesc.Rotation}");
        DebugHelper.WriteLine("==========================");

        var desc = adapter.Description;

        DebugHelper.WriteLine("=== Adapter Info ===");
        DebugHelper.WriteLine($"Description           : {desc.Description}");
        DebugHelper.WriteLine($"VendorId              : 0x{desc.VendorId:X} ({desc.VendorId})");
        DebugHelper.WriteLine($"DeviceId              : 0x{desc.DeviceId:X} ({desc.DeviceId})");
        DebugHelper.WriteLine($"SubsystemId           : 0x{desc.SubsystemId:X} ({desc.SubsystemId})");
        DebugHelper.WriteLine($"Revision              : {desc.Revision}");
        DebugHelper.WriteLine($"DedicatedVideoMemory  : {desc.DedicatedVideoMemory / 1024 / 1024} MB");
        DebugHelper.WriteLine($"DedicatedSystemMemory : {desc.DedicatedSystemMemory / 1024 / 1024} MB");
        DebugHelper.WriteLine($"SharedSystemMemory    : {desc.SharedSystemMemory / 1024 / 1024} MB");
        DebugHelper.WriteLine("======================");

        var duplication = output.DuplicateOutput(device);

        var textureDesc = new Texture2DDescription
        {
            CPUAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = (uint)bounds.Width,
            Height = (uint)bounds.Height,
            MiscFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };
        var currentFrame = device.CreateTexture2D(textureDesc);

        await Task.Delay(100);

        duplication.AcquireNextFrame(500, out var frameInfo, out var desktopResource);
        var tempTexture = desktopResource.QueryInterface<ID3D11Texture2D>();

        device.ImmediateContext.CopyResource(currentFrame, tempTexture);
        var dataBox = device.ImmediateContext.Map(currentFrame, 0);
        var screenshotBytes = GetDataAsByteArray(dataBox.DataPointer, (int)dataBox.RowPitch, bounds.Width,
            bounds.Height);
        duplication.ReleaseFrame();
        device.ImmediateContext.Unmap(currentFrame, 0);
        return Image.LoadPixelData<Rgba32>(screenshotBytes, bounds.Width, bounds.Height);
    }

    private static byte[] GetDataAsByteArray(IntPtr dataPointer, int rowPitch, int width, int height)
    {
        // Create a byte[] array to hold the pixel data
        var pixelData = new byte[height * rowPitch];

        // Copy the data from unmanaged memory to the byte array
        for (var y = 0; y < height; y++)
        {
            // Pointer arithmetic to calculate the address of each row
            var rowPointer = IntPtr.Add(dataPointer, y * rowPitch);

            // Copy the row from unmanaged memory to the byte array
            Marshal.Copy(rowPointer, pixelData, y * width * 4, width * 4); // Assuming 4 bytes per pixel (RGBA)
        }

        for (var i = 0; i < pixelData.Length; i += 4)
        {
            // Deconstruct the RGBA values and swap the red and blue channels
            (pixelData[i + 2], pixelData[i]) =
                (pixelData[i], pixelData[i + 2]); // Swap Blue (index 0) and Red (index 2)
        }

        return pixelData;
    }
}
