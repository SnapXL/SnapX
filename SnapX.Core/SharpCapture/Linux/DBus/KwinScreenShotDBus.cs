using Tmds.DBus.Protocol;

namespace SnapX.Core.SharpCapture.Linux.DBus;

record ScreenShot2Properties
{
    public uint Version { get; set; } = default!;
}
partial class ScreenShot2 : ScreenShot2ServiceObject
{
    private const string __Interface = "org.kde.KWin.ScreenShot2";
    public ScreenShot2(ScreenShot2Service service, ObjectPath path) : base(service, path)
    { }
    public Task<Dictionary<string, VariantValue>> CaptureWindowAsync(string handle, Dictionary<string, VariantValue> options, System.Runtime.InteropServices.SafeHandle pipe)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (ScreenShot2ServiceObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}h",
                member: "CaptureWindow");
            writer.WriteString(handle);
            writer.WriteDictionary(options);
            writer.WriteHandle(pipe);
            return writer.CreateMessage();
        }
    }
    public Task<Dictionary<string, VariantValue>> CaptureActiveWindowAsync(Dictionary<string, VariantValue> options, System.Runtime.InteropServices.SafeHandle pipe)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (ScreenShot2ServiceObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}h",
                member: "CaptureActiveWindow");
            writer.WriteDictionary(options);
            writer.WriteHandle(pipe);
            return writer.CreateMessage();
        }
    }
    public Task<Dictionary<string, VariantValue>> CaptureAreaAsync(int x, int y, uint width, uint height, Dictionary<string, VariantValue> options, System.Runtime.InteropServices.SafeHandle pipe)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (ScreenShot2ServiceObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "iiuua{sv}h",
                member: "CaptureArea");
            writer.WriteInt32(x);
            writer.WriteInt32(y);
            writer.WriteUInt32(width);
            writer.WriteUInt32(height);
            writer.WriteDictionary(options);
            writer.WriteHandle(pipe);
            return writer.CreateMessage();
        }
    }
    public Task<Dictionary<string, VariantValue>> CaptureScreenAsync(string name, Dictionary<string, VariantValue> options, System.Runtime.InteropServices.SafeHandle pipe)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (ScreenShot2ServiceObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}h",
                member: "CaptureScreen");
            writer.WriteString(name);
            writer.WriteDictionary(options);
            writer.WriteHandle(pipe);
            return writer.CreateMessage();
        }
    }
    public Task<Dictionary<string, VariantValue>> CaptureActiveScreenAsync(Dictionary<string, VariantValue> options, System.Runtime.InteropServices.SafeHandle pipe)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (ScreenShot2ServiceObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}h",
                member: "CaptureActiveScreen");
            writer.WriteDictionary(options);
            writer.WriteHandle(pipe);
            return writer.CreateMessage();
        }
    }
    public Task<Dictionary<string, VariantValue>> CaptureInteractiveAsync(uint kind, Dictionary<string, VariantValue> options, System.Runtime.InteropServices.SafeHandle pipe)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (ScreenShot2ServiceObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ua{sv}h",
                member: "CaptureInteractive");
            writer.WriteUInt32(kind);
            writer.WriteDictionary(options);
            writer.WriteHandle(pipe);
            return writer.CreateMessage();
        }
    }
    public Task<CaptureWorkspaceResult> CaptureWorkspaceAsync(Dictionary<string, VariantValue> options, System.Runtime.InteropServices.SafeHandle pipe)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadWorkspaceResult(m, (ScreenShot2ServiceObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}h",
                member: "CaptureWorkspace");
            writer.WriteDictionary(options);
            writer.WriteHandle(pipe);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Version"), (Message m, object? s) => ReadMessage_v_u(m, (ScreenShot2ServiceObject)s!), this);
    public Task<ScreenShot2Properties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (ScreenShot2ServiceObject)s!), this);
        static ScreenShot2Properties ReadMessage(Message message, ScreenShot2ServiceObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<ScreenShot2Properties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (ScreenShot2ServiceObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<ScreenShot2Properties> ReadMessage(Message message, ScreenShot2ServiceObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<ScreenShot2Properties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
        }
        static string[] ReadInvalidated(ref Reader reader)
        {
            List<string>? invalidated = null;
            ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.String);
            while (reader.HasNext(arrayEnd))
            {
                invalidated ??= new();
                var property = reader.ReadString();
                switch (property)
                {
                    case "Version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static ScreenShot2Properties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new ScreenShot2Properties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "Version":
                    reader.ReadSignature("u"u8);
                    props.Version = reader.ReadUInt32();
                    changedList?.Add("Version");
                    break;
                default:
                    reader.ReadVariantValue();
                    break;
            }
        }
        return props;
    }
}
partial class ScreenShot2Service
{
    public Tmds.DBus.Protocol.Connection Connection { get; }
    public string Destination { get; }
    public ScreenShot2Service(Tmds.DBus.Protocol.Connection connection, string destination)
        => (Connection, Destination) = (connection, destination);
    public ScreenShot2 CreateScreenShot2(ObjectPath path) => new ScreenShot2(this, path);
}
class ScreenShot2ServiceObject
{
    public ScreenShot2Service Service { get; }
    public ObjectPath Path { get; }
    protected Tmds.DBus.Protocol.Connection Connection => Service.Connection;
    protected ScreenShot2ServiceObject(ScreenShot2Service service, ObjectPath path)
        => (Service, Path) = (service, path);
    protected MessageBuffer CreateGetPropertyMessage(string @interface, string property)
    {
        var writer = this.Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Service.Destination,
            path: Path,
            @interface: "org.freedesktop.DBus.Properties",
            signature: "ss",
            member: "Get");
        writer.WriteString(@interface);
        writer.WriteString(property);
        return writer.CreateMessage();
    }
    protected MessageBuffer CreateGetAllPropertiesMessage(string @interface)
    {
        var writer = this.Connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: Service.Destination,
            path: Path,
            @interface: "org.freedesktop.DBus.Properties",
            signature: "s",
            member: "GetAll");
        writer.WriteString(@interface);
        return writer.CreateMessage();
    }
    protected ValueTask<IDisposable> WatchPropertiesChangedAsync<TProperties>(string @interface, MessageValueReader<PropertyChanges<TProperties>> reader, Action<Exception?, PropertyChanges<TProperties>> handler, bool emitOnCapturedContext, ObserverFlags flags)
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = Service.Destination,
            Path = Path,
            Interface = "org.freedesktop.DBus.Properties",
            Member = "PropertiesChanged",
            Arg0 = @interface
        };
        return this.Connection.AddMatchAsync(rule, reader,
            (Exception? ex, PropertyChanges<TProperties> changes, object? rs, object? hs) => ((Action<Exception?, PropertyChanges<TProperties>>)hs!).Invoke(ex, changes),
            this, handler, emitOnCapturedContext, flags);
    }
    public ValueTask<IDisposable> WatchSignalAsync<TArg>(string sender, string @interface, ObjectPath path, string signal, MessageValueReader<TArg> reader, Action<Exception?, TArg> handler, bool emitOnCapturedContext, ObserverFlags flags)
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = sender,
            Path = path,
            Member = signal,
            Interface = @interface
        };
        return this.Connection.AddMatchAsync(rule, reader,
            (Exception? ex, TArg arg, object? rs, object? hs) => ((Action<Exception?, TArg>)hs!).Invoke(ex, arg),
            this, handler, emitOnCapturedContext, flags);
    }
    public ValueTask<IDisposable> WatchSignalAsync(string sender, string @interface, ObjectPath path, string signal, Action<Exception?> handler, bool emitOnCapturedContext, ObserverFlags flags)
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = sender,
            Path = path,
            Member = signal,
            Interface = @interface
        };
        return this.Connection.AddMatchAsync<object>(rule, (Message message, object? state) => null!,
            (Exception? ex, object v, object? rs, object? hs) => ((Action<Exception?>)hs!).Invoke(ex), this, handler, emitOnCapturedContext, flags);
    }
    protected static Dictionary<string, VariantValue> ReadMessage_aesv(Message message, ScreenShot2ServiceObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadDictionaryOfStringToVariantValue();
    }

    protected static CaptureWorkspaceResult ReadWorkspaceResult(Message message, ScreenShot2ServiceObject _)
    {
        var reader = message.GetBodyReader();
        var dict = reader.ReadDictionaryOfStringToVariantValue();
        var result = new CaptureWorkspaceResult
        {
            Type = dict["type"].GetString(),
            Width = dict["width"].GetUInt32(),
            Height = dict["height"].GetUInt32(),
            Stride = dict["stride"].GetUInt32(),
            Format = (QImageFormat)dict["format"].GetUInt32(),
            Scale = dict["scale"].GetDouble()
        };
        return result;
    }

    protected static uint ReadMessage_v_u(Message message, ScreenShot2ServiceObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("u"u8);
        return reader.ReadUInt32();
    }
}

internal struct CaptureWorkspaceResult
{
    public string Type;
    public uint Width;
    public uint Height;
    public uint Stride;
    public QImageFormat Format;
    public double Scale;
}

internal enum QImageFormat : uint
{
    Invalid = 0,
    Mono = 1,
    MonoLSB = 2,
    Indexed8 = 3,
    RGB32 = 4,
    ARGB32 = 5,
    ARGB32Premultiplied = 6,
    RGB16 = 7,
    ARGB8565Premultiplied = 8,
    RGB666 = 9,
    ARGB6666Premultiplied = 10,
    RGB555 = 11,
    ARGB8555Premultiplied = 12,
    RGB888 = 13,
    RGB444 = 14,
    ARGB4444Premultiplied = 15,
    RGBX8888 = 16,
    RGBA8888 = 17,
    RGBA8888Premultiplied = 18,
    BGR30 = 19,
    A2BGR30Premultiplied = 20,
    RGB30 = 21,
    A2RGB30Premultiplied = 22,
    Alpha8 = 23,
    Grayscale8 = 24,
    RGBX64 = 25,
    RGBA64 = 26,
    RGBA64Premultiplied = 27,
    Grayscale16 = 28,
    BGR888 = 29,
    RGBX16FPx4 = 30,
    RGBA16FPx4 = 31,
    RGBA16FPx4Premultiplied = 32,
    RGBX32FPx4 = 33,
    RGBA32FPx4 = 34,
    RGBA32FPx4Premultiplied = 35,
    CMYK8888 = 36,
}
