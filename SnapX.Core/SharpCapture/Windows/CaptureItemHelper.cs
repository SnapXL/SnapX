using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Versioning;
using Windows.Graphics.Capture;
using WinRT;

namespace SnapX.Core.SharpCapture.Windows;

// [ComImport]
[GeneratedComInterface]
[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]

// [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
// [ComVisible(true)]
internal partial interface IGraphicsCaptureItemInterop
{
    IntPtr CreateForWindow(
         IntPtr window,
         ref Guid iid);

    IntPtr CreateForMonitor(
         IntPtr monitor,
         Guid iid);
}

[GeneratedComInterface]
[Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
internal partial interface IInitializeWithWindow
{
    void Initialize(
        IntPtr hwnd);
}


[SupportedOSPlatform("windows10.0.19045")]
public static class CaptureItemHelper
{
    private static Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    internal static void SetWindow(this GraphicsCapturePicker picker, IntPtr hwnd)
    {
        var interop = picker.As<IInitializeWithWindow>();
        interop.Initialize(hwnd);
    }

    internal static GraphicsCaptureItem CreateItemForWindow(IntPtr hwnd)
    {
        DebugHelper.WriteLine($"CreateItemForWindow: {hwnd}");
        var factory = ActivationFactory.Get(typeof(GraphicsCaptureItem).FullName!);
        // var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();
        var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();
        var itemPointer = interop.CreateForWindow(hwnd, ref GraphicsCaptureItemGuid);
        if (itemPointer == null || itemPointer == IntPtr.Zero)
        {
            DebugHelper.WriteLine($"CreateItemForWindow: itemPointer {itemPointer} is invalid!");
            return null;
        }
        ComWrappers cw = new DefaultComWrappers();
         var item = cw.GetOrCreateObjectForComInstance(itemPointer, CreateObjectFlags.None) as GraphicsCaptureItem;
        // var item = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
        Marshal.Release(itemPointer);
        return item;
    }
}
