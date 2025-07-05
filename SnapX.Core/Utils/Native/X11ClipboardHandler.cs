using System.Runtime.InteropServices;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace SnapX.Core.Utils.Native;

public class X11ClipboardHandler : IDisposable
{
    private static X11ClipboardHandler? _instance;
    private static readonly Lock _lock = new();

    private IntPtr _display;
    private IntPtr _clipboardWindow;
    private Thread? _eventThread;
    private CancellationTokenSource _cts = new();

    // X11 Atoms
    private IntPtr _atomClipboard;
    private IntPtr _atomTargets;
    private IntPtr _atomPng;
    private IntPtr _atomImagePng;
    private IntPtr _atomUtf8String;

    private Image? _currentImage;
    private string? _currentFilename;

    private X11ClipboardHandler()
    {
        _display = LinuxAPI.XOpenDisplay(null);
        if (_display == IntPtr.Zero)
        {
            throw new Exception("Unable to open X11 display for clipboard handler.");
        }

        _clipboardWindow = LinuxAPI.XCreateSimpleWindow(_display, LinuxAPI.XDefaultRootWindow(_display),
                                                   0, 0, 1, 1, 0, 0, 0);

        LinuxAPI.XSelectInput(_display, _clipboardWindow,
                             LinuxAPI.SelectionClearMask | LinuxAPI.SelectionRequestMask);

        InitializeAtoms();

        _eventThread = new Thread(() => EventLoop(_cts.Token))
        {
            IsBackground = true,
            Name = "X11ClipboardEventLoop"
        };
        _eventThread.Start();

        DebugHelper.Logger?.Debug("X11ClipboardHandler initialized and event loop started.");
    }

    public static X11ClipboardHandler Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new X11ClipboardHandler();
                return _instance;
            }
        }
    }

    private void InitializeAtoms()
    {
        _atomClipboard = LinuxAPI.XInternAtom(_display, "CLIPBOARD", false);
        _atomTargets = LinuxAPI.XInternAtom(_display, "TARGETS", false);
        _atomPng = LinuxAPI.XInternAtom(_display, "PNG", false);
        _atomImagePng = LinuxAPI.XInternAtom(_display, "image/png", false);
        _atomUtf8String = LinuxAPI.XInternAtom(_display, "UTF8_STRING", false);
    }

    public void SetImage(Image image, string? filename = null)
    {
        lock (_lock)
        {
            _currentImage = image;
            _currentFilename = string.IsNullOrEmpty(filename) ? "image.png" : filename;

            LinuxAPI.XSetSelectionOwner(_display, _atomClipboard, _clipboardWindow, LinuxAPI.CurrentTime);
            LinuxAPI.XFlush(_display);

            var owner = LinuxAPI.XGetSelectionOwner(_display, _atomClipboard);
            if (owner == _clipboardWindow)
            {
                DebugHelper.Logger?.Debug($"Successfully claimed X11 CLIPBOARD ownership for image and filename '{_currentFilename}'.");
            }
            else
            {
                DebugHelper.Logger?.Warning("Failed to claim X11 CLIPBOARD ownership. Another application might be the owner.");
            }
        }
    }

    private void EventLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (LinuxAPI.XEventsQueued(_display, 0) == 0)
            {
                Thread.Sleep(10);
                continue;
            }

            LinuxAPI.XNextEvent(_display, out var eventData);

            switch (eventData.type)
            {
                case LinuxAPI.SelectionRequest:
                    HandleSelectionRequest(eventData.xselectionrequest);
                    break;
                case LinuxAPI.SelectionClear:
                    HandleSelectionClear(eventData.xselectionclear);
                    break;
                default:
                    break;
            }
        }
        DebugHelper.Logger?.Debug("X11ClipboardHandler event loop terminated.");
    }

    private void HandleSelectionRequest(LinuxAPI.XSelectionRequestEvent request)
    {
        DebugHelper.Logger?.Debug($"SelectionRequest received: Target {GetAtomName(request.target)}");

        var property = request.property;
        var type = IntPtr.Zero;
        byte[]? data = null;
        var format = 0;
        var nElements = 0;

        try
        {
            if (request.selection != _atomClipboard)
            {
                DebugHelper.Logger?.Debug("Selection request for non-CLIPBOARD selection ignored.");
                return;
            }

            if (_currentImage == null)
            {
                DebugHelper.Logger?.Warning("Selection request received but no image is set.");
                property = IntPtr.Zero;
                type = IntPtr.Zero;
            }
            else if (request.target == _atomTargets)
            {
                DebugHelper.Logger?.Debug("Responding to TARGETS request.");
                IntPtr[] supportedTargets = { _atomTargets, _atomPng, _atomImagePng, _atomUtf8String };
                data = new byte[supportedTargets.Length * Marshal.SizeOf<IntPtr>()];
                for (int i = 0; i < supportedTargets.Length; i++)
                {
                    Marshal.WriteIntPtr(data, i * Marshal.SizeOf<IntPtr>(), supportedTargets[i]);
                }
                type = _atomTargets;
                format = 32;
                nElements = supportedTargets.Length;
            }
            else if (request.target == _atomPng || request.target == _atomImagePng)
            {
                DebugHelper.Logger?.Debug($"Responding to image/png request. Image dimensions: {_currentImage.Width}x{_currentImage.Height}");
                using var ms = new MemoryStream();
                _currentImage.Save(ms, new PngEncoder());
                data = ms.ToArray();
                type = request.target;
                format = 8;
                nElements = data.Length;
            }
            else if (request.target == _atomUtf8String && !string.IsNullOrEmpty(_currentFilename))
            {
                DebugHelper.Logger?.Debug("Responding to UTF8_STRING request (filename).");
                data = Encoding.UTF8.GetBytes(_currentFilename);
                type = _atomUtf8String;
                format = 8;
                nElements = data.Length;
            }
            else
            {
                DebugHelper.Logger?.Debug($"Unsupported selection target: {GetAtomName(request.target)}.");
                property = IntPtr.Zero;
                type = IntPtr.Zero;
            }

            if (property != IntPtr.Zero && data != null)
            {
                LinuxAPI.XChangeProperty(_display, request.requestor, property, type, format, LinuxAPI.PropModeReplace, data, nElements);
                DebugHelper.Logger?.Debug($"XChangeProperty successful for target {GetAtomName(request.target)}.");
            }
            else if (property != IntPtr.Zero && data == null)
            {
                DebugHelper.Logger?.Debug($"No data to provide for target {GetAtomName(request.target)}, setting property to None.");
                property = IntPtr.Zero;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.Logger?.Error($"Error handling SelectionRequest: {ex.Message}");
            property = IntPtr.Zero;
        }
        finally
        {
            SendSelectionNotify(request.requestor, request.selection, request.target, property, request.time);
        }
    }

    private void SendSelectionNotify(IntPtr requestor, IntPtr selection, IntPtr target, IntPtr property, long time)
    {
        var notifyEvent = new LinuxAPI.XSelectionEvent
        {
            type = LinuxAPI.SelectionNotify,
            display = _display,
            requestor = requestor,
            selection = selection,
            target = target,
            property = property,
            time = time,
            serial = IntPtr.Zero,
            send_event = true
        };

        var size = Marshal.SizeOf(notifyEvent);
        var eventPtr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(notifyEvent, eventPtr, false);

        try
        {
            var status = LinuxAPI.XSendEvent(_display, requestor, true, 0L, eventPtr);
            LinuxAPI.XFlush(_display);

            if (status == 0)
            {
                DebugHelper.Logger?.Warning("XSendEvent for SelectionNotify failed.");
            }
            else
            {
                DebugHelper.Logger?.Debug($"SelectionNotify sent to {requestor} (property: {GetAtomName(property)}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(eventPtr);
        }
    }

    private void HandleSelectionClear(LinuxAPI.XSelectionClearEvent clear)
    {
        if (clear.selection == _atomClipboard)
        {
            DebugHelper.Logger?.Debug("CLIPBOARD ownership lost (SelectionClear event).");
            lock (_lock)
            {
                _currentImage = null;
                _currentFilename = null;
            }
        }
    }

    private string GetAtomName(IntPtr atom)
    {
        if (atom == IntPtr.Zero) return "None";
        var namePtr = LinuxAPI.XGetAtomName(_display, atom);
        if (namePtr == IntPtr.Zero) return $"Atom_{atom}";
        var name = Marshal.PtrToStringAnsi(namePtr) ?? $"Atom_{atom}";
        LinuxAPI.XFree(namePtr);
        return name;
    }

    public void Dispose()
    {
        DebugHelper.Logger?.Debug("Disposing X11ClipboardHandler.");
        _cts.Cancel();
        _eventThread?.Join();

        if (_clipboardWindow != IntPtr.Zero)
        {
            LinuxAPI.XDestroyWindow(_display, _clipboardWindow);
            _clipboardWindow = IntPtr.Zero;
        }
        if (_display != IntPtr.Zero)
        {
            LinuxAPI.XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }
        _instance = null;
    }
}
