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
    private string? _currentText;
    private IntPtr _atomText;

    private X11ClipboardHandler()
    {
        _display = LinuxAPI.XOpenDisplay(null);
        if (_display == IntPtr.Zero)
            throw new Exception("Unable to open X11 display for clipboard handler.");

        var request = new NeedMainWindowHandle();
        SnapX.EventAggregator.Publish(request);

        var mainWindowHandle = request.ResultHandle;


        if (mainWindowHandle == IntPtr.Zero)
        {
            DebugHelper.WriteLine("Unable to get app window for clipboard handler, creating dummy window.");
            _clipboardWindow = LinuxAPI.XCreateSimpleWindow(
                _display,
                LinuxAPI.XDefaultRootWindow(_display),
                0, 0, 1, 1, 0, 0, 0
            );
            LinuxAPI.XMapWindow(_display, _clipboardWindow);
        }
        else
        {
            _clipboardWindow = mainWindowHandle;
            DebugHelper.WriteLine($"Using app window 0x{_clipboardWindow.ToInt64():X} for clipboard handler.");
        }

        LinuxAPI.XFlush(_display);

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
        _atomText = LinuxAPI.XInternAtom(_display, "TEXT", false);

    }

    public void SetImage(Image image, string? filename = null)
    {
        lock (_lock)
        {
            _currentImage = image;
            _currentText = null;
            _currentFilename = string.IsNullOrEmpty(filename) ? "image.png" : filename;

            ClaimClipboardOwnership();
        }
    }
    public void SetText(string text)
    {
        lock (_lock)
        {
            _currentText = text;
            _currentImage = null;
            _currentFilename = null;

            ClaimClipboardOwnership();
        }
    }
    private void ClaimClipboardOwnership()
    {
        LinuxAPI.XSetSelectionOwner(_display, _atomClipboard, _clipboardWindow, LinuxAPI.CurrentTime);
        LinuxAPI.XFlush(_display);

        var owner = LinuxAPI.XGetSelectionOwner(_display, _atomClipboard);
        if (owner == _clipboardWindow)
            DebugHelper.Logger?.Debug("Successfully claimed X11 CLIPBOARD ownership.");
        else
            DebugHelper.Logger?.Warning("Failed to claim X11 CLIPBOARD ownership. Another application might own it.");
    }
    private void EventLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (LinuxAPI.XPending(_display) == 0)
            {
                Thread.Sleep(10);
                continue;
            }
            DebugHelper.Logger.Debug("EventLoop: Processing event!");
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
                    DebugHelper.Logger.Debug("Unknown event data type: {EventDataType}", eventData.type);
                    break;
            }
        }
        DebugHelper.Logger?.Debug("X11ClipboardHandler event loop terminated.");
    }

    private void HandleSelectionRequest(LinuxAPI.XSelectionRequestEvent request)
    {
        DebugHelper.Logger?.Debug($"SelectionRequest target={GetAtomName(request.target)} property={GetAtomName(request.property)}");

        var property = request.property;
        var type = IntPtr.Zero;
        byte[]? data = null;
        var format = 0;
        var nElements = 0;

        try
        {
            if (request.selection != _atomClipboard)
                return;

            if (_currentImage == null && _currentText == null)
            {
                DebugHelper.Logger?.Warning("Selection request received but clipboard is empty.");
                property = IntPtr.Zero;
            }
            else if (request.target == _atomTargets)
            {
                var supportedTargets = new List<IntPtr> { _atomTargets, _atomUtf8String, _atomText };
                if (_currentImage != null)
                {
                    supportedTargets.Add(_atomPng);
                    supportedTargets.Add(_atomImagePng);
                }

                data = new byte[supportedTargets.Count * Marshal.SizeOf<IntPtr>()];
                for (int i = 0; i < supportedTargets.Count; i++)
                    Marshal.WriteIntPtr(data, i * Marshal.SizeOf<IntPtr>(), supportedTargets[i]);

                type = _atomTargets;
                format = 32;
                nElements = supportedTargets.Count;
            }
            else if (_currentImage != null &&
                     (request.target == _atomPng || request.target == _atomImagePng))
            {
                using var ms = new MemoryStream();
                _currentImage.Save(ms, new PngEncoder());
                data = ms.ToArray();
                type = request.target;
                format = 8;
                nElements = data.Length;
            }
            else if (_currentText != null &&
                     (request.target == _atomUtf8String || request.target == _atomText))
            {
                data = Encoding.UTF8.GetBytes(_currentText);
                type = _atomUtf8String;
                format = 8;
                nElements = data.Length;
            }
            else
            {
                DebugHelper.Logger?.Debug($"Unsupported selection target: {GetAtomName(request.target)}");
                property = IntPtr.Zero;
            }

            if (property != IntPtr.Zero && data != null)
            {
                LinuxAPI.XChangeProperty(_display, request.requestor, property, type,
                    format, LinuxAPI.PropModeReplace, data, nElements);
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
                DebugHelper.Logger?.Warning("XSendEvent for SelectionNotify failed.");
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
                _currentText = null;
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
