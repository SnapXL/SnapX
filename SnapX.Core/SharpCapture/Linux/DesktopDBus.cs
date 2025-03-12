using SnapX.Core.Utils.Extensions;

namespace SnapX.Core.SharpCapture.Linux;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
record InhibitProperties
{
    public uint Version { get; set; } = default!;
}
partial class Inhibit : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Inhibit";
    public Inhibit(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> InhibitAsync(string window, uint flags, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sua{sv}",
                member: "Inhibit");
            writer.WriteString(window);
            writer.WriteUInt32(flags);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> CreateMonitorAsync(string window, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "CreateMonitor");
            writer.WriteString(window);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task QueryEndResponseAsync(ObjectPath sessionHandle)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "o",
                member: "QueryEndResponse");
            writer.WriteObjectPath(sessionHandle);
            return writer.CreateMessage();
        }
    }
    public ValueTask<IDisposable> WatchStateChangedAsync(Action<Exception?, (ObjectPath SessionHandle, Dictionary<string, VariantValue> State)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "StateChanged", (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<InhibitProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static InhibitProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<InhibitProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<InhibitProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<InhibitProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static InhibitProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new InhibitProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record BackgroundProperties
{
    public uint Version { get; set; } = default!;
}
partial class Background : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Background";
    public Background(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> RequestBackgroundAsync(string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "RequestBackground");
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task SetStatusAsync(Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}",
                member: "SetStatus");
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<BackgroundProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static BackgroundProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<BackgroundProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<BackgroundProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<BackgroundProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static BackgroundProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new BackgroundProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record LocationProperties
{
    public uint Version { get; set; } = default!;
}
partial class Location : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Location";
    public Location(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> CreateSessionAsync(Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}",
                member: "CreateSession");
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> StartAsync(ObjectPath sessionHandle, string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "osa{sv}",
                member: "Start");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public ValueTask<IDisposable> WatchLocationUpdatedAsync(Action<Exception?, (ObjectPath SessionHandle, Dictionary<string, VariantValue> Location)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "LocationUpdated", (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<LocationProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static LocationProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<LocationProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<LocationProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<LocationProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static LocationProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new LocationProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record NotificationProperties
{
    public Dictionary<string, VariantValue> SupportedOptions { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class Notification : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Notification";
    public Notification(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task AddNotificationAsync(string id, Dictionary<string, VariantValue> notification)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "AddNotification");
            writer.WriteString(id);
            writer.WriteDictionary(notification);
            return writer.CreateMessage();
        }
    }
    public Task RemoveNotificationAsync(string id)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "s",
                member: "RemoveNotification");
            writer.WriteString(id);
            return writer.CreateMessage();
        }
    }
    public ValueTask<IDisposable> WatchActionInvokedAsync(Action<Exception?, (string Id, string Action, VariantValue[] Parameter)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "ActionInvoked", (Message m, object? s) => ReadMessage_ssav(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public Task<Dictionary<string, VariantValue>> GetSupportedOptionsAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SupportedOptions"), (Message m, object? s) => ReadMessage_v_aesv(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<NotificationProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static NotificationProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<NotificationProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<NotificationProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<NotificationProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "SupportedOptions": invalidated.Add("SupportedOptions"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static NotificationProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new NotificationProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "SupportedOptions":
                    reader.ReadSignature("a{sv}"u8);
                    props.SupportedOptions = reader.ReadDictionaryOfStringToVariantValue();
                    changedList?.Add("SupportedOptions");
                    break;
                case "version":
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
record ScreenshotProperties
{
    public uint Version { get; set; } = default!;
}
partial class Screenshot : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Screenshot";
    public Screenshot(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> ScreenshotAsync(string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "Screenshot");
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> PickColorAsync(string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "PickColor");
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<ScreenshotProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static ScreenshotProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<ScreenshotProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<ScreenshotProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<ScreenshotProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static ScreenshotProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new ScreenshotProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record AccountProperties
{
    public uint Version { get; set; } = default!;
}
partial class Account : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Account";
    public Account(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> GetUserInformationAsync(string window, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "GetUserInformation");
            writer.WriteString(window);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<AccountProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static AccountProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<AccountProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<AccountProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<AccountProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static AccountProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new AccountProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record NetworkMonitorProperties
{
    public uint Version { get; set; } = default!;
}
partial class NetworkMonitor : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.NetworkMonitor";
    public NetworkMonitor(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<bool> GetAvailableAsync()
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_b(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                member: "GetAvailable");
            return writer.CreateMessage();
        }
    }
    public Task<bool> GetMeteredAsync()
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_b(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                member: "GetMetered");
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetConnectivityAsync()
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_u(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                member: "GetConnectivity");
            return writer.CreateMessage();
        }
    }
    public Task<Dictionary<string, VariantValue>> GetStatusAsync()
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesv(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                member: "GetStatus");
            return writer.CreateMessage();
        }
    }
    public Task<bool> CanReachAsync(string hostname, uint port)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_b(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "su",
                member: "CanReach");
            writer.WriteString(hostname);
            writer.WriteUInt32(port);
            return writer.CreateMessage();
        }
    }
    public ValueTask<IDisposable> WatchChangedAsync(Action<Exception?> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "changed", handler, emitOnCapturedContext, flags);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<NetworkMonitorProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static NetworkMonitorProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<NetworkMonitorProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<NetworkMonitorProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<NetworkMonitorProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static NetworkMonitorProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new NetworkMonitorProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record PrintProperties
{
    public uint Version { get; set; } = default!;
}
partial class Print : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Print";
    public Print(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> PreparePrintAsync(string parentWindow, string title, Dictionary<string, VariantValue> settings, Dictionary<string, VariantValue> pageSetup, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ssa{sv}a{sv}a{sv}",
                member: "PreparePrint");
            writer.WriteString(parentWindow);
            writer.WriteString(title);
            writer.WriteDictionary(settings);
            writer.WriteDictionary(pageSetup);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> PrintAsync(string parentWindow, string title, System.Runtime.InteropServices.SafeHandle fd, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ssha{sv}",
                member: "Print");
            writer.WriteString(parentWindow);
            writer.WriteString(title);
            writer.WriteHandle(fd);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<PrintProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static PrintProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<PrintProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<PrintProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<PrintProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static PrintProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new PrintProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record SettingsProperties
{
    public uint Version { get; set; } = default!;
}
partial class Settings : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Settings";
    public Settings(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<Dictionary<string, Dictionary<string, VariantValue>>> ReadAllAsync(string[] namespaces)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_aesaesv(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "as",
                member: "ReadAll");
            writer.WriteArray(namespaces);
            return writer.CreateMessage();
        }
    }
    public Task<VariantValue> ReadAsync(string @namespace, string key)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_v(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ss",
                member: "Read");
            writer.WriteString(@namespace);
            writer.WriteString(key);
            return writer.CreateMessage();
        }
    }
    public Task<VariantValue> ReadOneAsync(string @namespace, string key)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_v(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ss",
                member: "ReadOne");
            writer.WriteString(@namespace);
            writer.WriteString(key);
            return writer.CreateMessage();
        }
    }
    public ValueTask<IDisposable> WatchSettingChangedAsync(Action<Exception?, (string Namespace, string Key, VariantValue Value)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "SettingChanged", (Message m, object? s) => ReadMessage_ssv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<SettingsProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static SettingsProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<SettingsProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<SettingsProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<SettingsProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static SettingsProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new SettingsProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record GameModeProperties
{
    public bool Active { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class GameMode : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.GameMode";
    public GameMode(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<int> QueryStatusAsync(int pid)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "i",
                member: "QueryStatus");
            writer.WriteInt32(pid);
            return writer.CreateMessage();
        }
    }
    public Task<int> RegisterGameAsync(int pid)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "i",
                member: "RegisterGame");
            writer.WriteInt32(pid);
            return writer.CreateMessage();
        }
    }
    public Task<int> UnregisterGameAsync(int pid)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "i",
                member: "UnregisterGame");
            writer.WriteInt32(pid);
            return writer.CreateMessage();
        }
    }
    public Task<int> QueryStatusByPidAsync(int target, int requester)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ii",
                member: "QueryStatusByPid");
            writer.WriteInt32(target);
            writer.WriteInt32(requester);
            return writer.CreateMessage();
        }
    }
    public Task<int> RegisterGameByPidAsync(int target, int requester)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ii",
                member: "RegisterGameByPid");
            writer.WriteInt32(target);
            writer.WriteInt32(requester);
            return writer.CreateMessage();
        }
    }
    public Task<int> UnregisterGameByPidAsync(int target, int requester)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ii",
                member: "UnregisterGameByPid");
            writer.WriteInt32(target);
            writer.WriteInt32(requester);
            return writer.CreateMessage();
        }
    }
    public Task<int> QueryStatusByPIDFdAsync(System.Runtime.InteropServices.SafeHandle target, System.Runtime.InteropServices.SafeHandle requester)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "hh",
                member: "QueryStatusByPIDFd");
            writer.WriteHandle(target);
            writer.WriteHandle(requester);
            return writer.CreateMessage();
        }
    }
    public Task<int> RegisterGameByPIDFdAsync(System.Runtime.InteropServices.SafeHandle target, System.Runtime.InteropServices.SafeHandle requester)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "hh",
                member: "RegisterGameByPIDFd");
            writer.WriteHandle(target);
            writer.WriteHandle(requester);
            return writer.CreateMessage();
        }
    }
    public Task<int> UnregisterGameByPIDFdAsync(System.Runtime.InteropServices.SafeHandle target, System.Runtime.InteropServices.SafeHandle requester)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_i(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "hh",
                member: "UnregisterGameByPIDFd");
            writer.WriteHandle(target);
            writer.WriteHandle(requester);
            return writer.CreateMessage();
        }
    }
    public Task<bool> GetActiveAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "Active"), (Message m, object? s) => ReadMessage_v_b(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<GameModeProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static GameModeProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<GameModeProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<GameModeProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<GameModeProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "Active": invalidated.Add("Active"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static GameModeProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new GameModeProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "Active":
                    reader.ReadSignature("b"u8);
                    props.Active = reader.ReadBool();
                    changedList?.Add("Active");
                    break;
                case "version":
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
record RemoteDesktopProperties
{
    public uint AvailableDeviceTypes { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class RemoteDesktop : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.RemoteDesktop";
    public RemoteDesktop(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> CreateSessionAsync(Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}",
                member: "CreateSession");
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> SelectDevicesAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "SelectDevices");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> StartAsync(ObjectPath sessionHandle, string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "osa{sv}",
                member: "Start");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task NotifyPointerMotionAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, double dx, double dy)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}dd",
                member: "NotifyPointerMotion");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteDouble(dx);
            writer.WriteDouble(dy);
            return writer.CreateMessage();
        }
    }
    public Task NotifyPointerMotionAbsoluteAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, uint stream, double x, double y)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}udd",
                member: "NotifyPointerMotionAbsolute");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteUInt32(stream);
            writer.WriteDouble(x);
            writer.WriteDouble(y);
            return writer.CreateMessage();
        }
    }
    public Task NotifyPointerButtonAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, int button, uint state)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}iu",
                member: "NotifyPointerButton");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteInt32(button);
            writer.WriteUInt32(state);
            return writer.CreateMessage();
        }
    }
    public Task NotifyPointerAxisAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, double dx, double dy)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}dd",
                member: "NotifyPointerAxis");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteDouble(dx);
            writer.WriteDouble(dy);
            return writer.CreateMessage();
        }
    }
    public Task NotifyPointerAxisDiscreteAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, uint axis, int steps)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}ui",
                member: "NotifyPointerAxisDiscrete");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteUInt32(axis);
            writer.WriteInt32(steps);
            return writer.CreateMessage();
        }
    }
    public Task NotifyKeyboardKeycodeAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, int keycode, uint state)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}iu",
                member: "NotifyKeyboardKeycode");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteInt32(keycode);
            writer.WriteUInt32(state);
            return writer.CreateMessage();
        }
    }
    public Task NotifyKeyboardKeysymAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, int keysym, uint state)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}iu",
                member: "NotifyKeyboardKeysym");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteInt32(keysym);
            writer.WriteUInt32(state);
            return writer.CreateMessage();
        }
    }
    public Task NotifyTouchDownAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, uint stream, uint slot, double x, double y)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}uudd",
                member: "NotifyTouchDown");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteUInt32(stream);
            writer.WriteUInt32(slot);
            writer.WriteDouble(x);
            writer.WriteDouble(y);
            return writer.CreateMessage();
        }
    }
    public Task NotifyTouchMotionAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, uint stream, uint slot, double x, double y)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}uudd",
                member: "NotifyTouchMotion");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteUInt32(stream);
            writer.WriteUInt32(slot);
            writer.WriteDouble(x);
            writer.WriteDouble(y);
            return writer.CreateMessage();
        }
    }
    public Task NotifyTouchUpAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, uint slot)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}u",
                member: "NotifyTouchUp");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            writer.WriteUInt32(slot);
            return writer.CreateMessage();
        }
    }
    public Task<System.Runtime.InteropServices.SafeHandle> ConnectToEISAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_h(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "ConnectToEIS");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetAvailableDeviceTypesAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AvailableDeviceTypes"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<RemoteDesktopProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static RemoteDesktopProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<RemoteDesktopProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<RemoteDesktopProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<RemoteDesktopProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "AvailableDeviceTypes": invalidated.Add("AvailableDeviceTypes"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static RemoteDesktopProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new RemoteDesktopProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "AvailableDeviceTypes":
                    reader.ReadSignature("u"u8);
                    props.AvailableDeviceTypes = reader.ReadUInt32();
                    changedList?.Add("AvailableDeviceTypes");
                    break;
                case "version":
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
record MemoryMonitorProperties
{
    public uint Version { get; set; } = default!;
}
partial class MemoryMonitor : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.MemoryMonitor";
    public MemoryMonitor(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public ValueTask<IDisposable> WatchLowMemoryWarningAsync(Action<Exception?, byte> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "LowMemoryWarning", (Message m, object? s) => ReadMessage_y(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<MemoryMonitorProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static MemoryMonitorProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<MemoryMonitorProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<MemoryMonitorProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<MemoryMonitorProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static MemoryMonitorProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new MemoryMonitorProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record OpenURIProperties
{
    public uint Version { get; set; } = default!;
}
partial class OpenURI : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.OpenURI";
    public OpenURI(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> OpenURIAsync(string parentWindow, string uri, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ssa{sv}",
                member: "OpenURI");
            writer.WriteString(parentWindow);
            writer.WriteString(uri);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> OpenFileAsync(string parentWindow, System.Runtime.InteropServices.SafeHandle fd, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sha{sv}",
                member: "OpenFile");
            writer.WriteString(parentWindow);
            writer.WriteHandle(fd);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> OpenDirectoryAsync(string parentWindow, System.Runtime.InteropServices.SafeHandle fd, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sha{sv}",
                member: "OpenDirectory");
            writer.WriteString(parentWindow);
            writer.WriteHandle(fd);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<bool> SchemeSupportedAsync(string scheme, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_b(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "SchemeSupported");
            writer.WriteString(scheme);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<OpenURIProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static OpenURIProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<OpenURIProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<OpenURIProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<OpenURIProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static OpenURIProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new OpenURIProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record RealtimeProperties
{
    public int MaxRealtimePriority { get; set; } = default!;
    public int MinNiceLevel { get; set; } = default!;
    public long RTTimeUSecMax { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class Realtime : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Realtime";
    public Realtime(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task MakeThreadRealtimeWithPIDAsync(ulong process, ulong thread, uint priority)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ttu",
                member: "MakeThreadRealtimeWithPID");
            writer.WriteUInt64(process);
            writer.WriteUInt64(thread);
            writer.WriteUInt32(priority);
            return writer.CreateMessage();
        }
    }
    public Task MakeThreadHighPriorityWithPIDAsync(ulong process, ulong thread, int priority)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "tti",
                member: "MakeThreadHighPriorityWithPID");
            writer.WriteUInt64(process);
            writer.WriteUInt64(thread);
            writer.WriteInt32(priority);
            return writer.CreateMessage();
        }
    }
    public Task<int> GetMaxRealtimePriorityAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MaxRealtimePriority"), (Message m, object? s) => ReadMessage_v_i(m, (DesktopObject)s!), this);
    public Task<int> GetMinNiceLevelAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "MinNiceLevel"), (Message m, object? s) => ReadMessage_v_i(m, (DesktopObject)s!), this);
    public Task<long> GetRTTimeUSecMaxAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "RTTimeUSecMax"), (Message m, object? s) => ReadMessage_v_x(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<RealtimeProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static RealtimeProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<RealtimeProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<RealtimeProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<RealtimeProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "MaxRealtimePriority": invalidated.Add("MaxRealtimePriority"); break;
                    case "MinNiceLevel": invalidated.Add("MinNiceLevel"); break;
                    case "RTTimeUSecMax": invalidated.Add("RTTimeUSecMax"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static RealtimeProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new RealtimeProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "MaxRealtimePriority":
                    reader.ReadSignature("i"u8);
                    props.MaxRealtimePriority = reader.ReadInt32();
                    changedList?.Add("MaxRealtimePriority");
                    break;
                case "MinNiceLevel":
                    reader.ReadSignature("i"u8);
                    props.MinNiceLevel = reader.ReadInt32();
                    changedList?.Add("MinNiceLevel");
                    break;
                case "RTTimeUSecMax":
                    reader.ReadSignature("x"u8);
                    props.RTTimeUSecMax = reader.ReadInt64();
                    changedList?.Add("RTTimeUSecMax");
                    break;
                case "version":
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
record SecretProperties
{
    public uint Version { get; set; } = default!;
}
partial class Secret : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Secret";
    public Secret(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> RetrieveSecretAsync(System.Runtime.InteropServices.SafeHandle fd, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ha{sv}",
                member: "RetrieveSecret");
            writer.WriteHandle(fd);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<SecretProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static SecretProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<SecretProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<SecretProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<SecretProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static SecretProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new SecretProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record CameraProperties
{
    public bool IsCameraPresent { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class Camera : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Camera";
    public Camera(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> AccessCameraAsync(Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}",
                member: "AccessCamera");
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<System.Runtime.InteropServices.SafeHandle> OpenPipeWireRemoteAsync(Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_h(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}",
                member: "OpenPipeWireRemote");
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<bool> GetIsCameraPresentAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "IsCameraPresent"), (Message m, object? s) => ReadMessage_v_b(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<CameraProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static CameraProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<CameraProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<CameraProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<CameraProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "IsCameraPresent": invalidated.Add("IsCameraPresent"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static CameraProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new CameraProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "IsCameraPresent":
                    reader.ReadSignature("b"u8);
                    props.IsCameraPresent = reader.ReadBool();
                    changedList?.Add("IsCameraPresent");
                    break;
                case "version":
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
record InputCaptureProperties
{
    public uint SupportedCapabilities { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class InputCapture : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.InputCapture";
    public InputCapture(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> CreateSessionAsync(string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "CreateSession");
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> GetZonesAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "GetZones");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> SetPointerBarriersAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options, Dictionary<string, VariantValue>[] barriers, uint zoneSet)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}aa{sv}u",
                member: "SetPointerBarriers");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            WriteType_aaesv(ref writer, barriers);
            writer.WriteUInt32(zoneSet);
            return writer.CreateMessage();
        }
    }
    public Task EnableAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "Enable");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task DisableAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "Disable");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task ReleaseAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "Release");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<System.Runtime.InteropServices.SafeHandle> ConnectToEISAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_h(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "ConnectToEIS");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public ValueTask<IDisposable> WatchDisabledAsync(Action<Exception?, (ObjectPath SessionHandle, Dictionary<string, VariantValue> Options)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Disabled", (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public ValueTask<IDisposable> WatchActivatedAsync(Action<Exception?, (ObjectPath SessionHandle, Dictionary<string, VariantValue> Options)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Activated", (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public ValueTask<IDisposable> WatchDeactivatedAsync(Action<Exception?, (ObjectPath SessionHandle, Dictionary<string, VariantValue> Options)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Deactivated", (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public ValueTask<IDisposable> WatchZonesChangedAsync(Action<Exception?, (ObjectPath SessionHandle, Dictionary<string, VariantValue> Options)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "ZonesChanged", (Message m, object? s) => ReadMessage_oaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public Task<uint> GetSupportedCapabilitiesAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SupportedCapabilities"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<InputCaptureProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static InputCaptureProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<InputCaptureProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<InputCaptureProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<InputCaptureProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "SupportedCapabilities": invalidated.Add("SupportedCapabilities"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static InputCaptureProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new InputCaptureProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "SupportedCapabilities":
                    reader.ReadSignature("u"u8);
                    props.SupportedCapabilities = reader.ReadUInt32();
                    changedList?.Add("SupportedCapabilities");
                    break;
                case "version":
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
record GlobalShortcutsProperties
{
    public uint Version { get; set; } = default!;
}
partial class GlobalShortcuts : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.GlobalShortcuts";
    public GlobalShortcuts(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> CreateSessionAsync(Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}",
                member: "CreateSession");
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> BindShortcutsAsync(ObjectPath sessionHandle, (string, Dictionary<string, VariantValue>)[] shortcuts, string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa(sa{sv})sa{sv}",
                member: "BindShortcuts");
            writer.WriteObjectPath(sessionHandle);
            WriteType_arsaesvz(ref writer, shortcuts);
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> ListShortcutsAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "ListShortcuts");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public ValueTask<IDisposable> WatchActivatedAsync(Action<Exception?, (ObjectPath SessionHandle, string ShortcutId, ulong Timestamp, Dictionary<string, VariantValue> Options)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Activated", (Message m, object? s) => ReadMessage_ostaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public ValueTask<IDisposable> WatchDeactivatedAsync(Action<Exception?, (ObjectPath SessionHandle, string ShortcutId, ulong Timestamp, Dictionary<string, VariantValue> Options)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Deactivated", (Message m, object? s) => ReadMessage_ostaesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public ValueTask<IDisposable> WatchShortcutsChangedAsync(Action<Exception?, (ObjectPath SessionHandle, (string, Dictionary<string, VariantValue>)[] Shortcuts)> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "ShortcutsChanged", (Message m, object? s) => ReadMessage_oarsaesvz(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<GlobalShortcutsProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static GlobalShortcutsProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<GlobalShortcutsProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<GlobalShortcutsProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<GlobalShortcutsProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static GlobalShortcutsProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new GlobalShortcutsProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record PowerProfileMonitorProperties
{
    public bool PowerSaverEnabled { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class PowerProfileMonitor : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.PowerProfileMonitor";
    public PowerProfileMonitor(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<bool> GetPowerSaverEnabledAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "power-saver-enabled"), (Message m, object? s) => ReadMessage_v_b(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<PowerProfileMonitorProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static PowerProfileMonitorProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<PowerProfileMonitorProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<PowerProfileMonitorProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<PowerProfileMonitorProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "power-saver-enabled": invalidated.Add("PowerSaverEnabled"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static PowerProfileMonitorProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new PowerProfileMonitorProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "power-saver-enabled":
                    reader.ReadSignature("b"u8);
                    props.PowerSaverEnabled = reader.ReadBool();
                    changedList?.Add("PowerSaverEnabled");
                    break;
                case "version":
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
record DynamicLauncherProperties
{
    public uint SupportedLauncherTypes { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class DynamicLauncher : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.DynamicLauncher";
    public DynamicLauncher(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task InstallAsync(string token, string desktopFileId, string desktopEntry, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sssa{sv}",
                member: "Install");
            writer.WriteString(token);
            writer.WriteString(desktopFileId);
            writer.WriteString(desktopEntry);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> PrepareInstallAsync(string parentWindow, string name, VariantValue iconV, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ssva{sv}",
                member: "PrepareInstall");
            writer.WriteString(parentWindow);
            writer.WriteString(name);
            writer.WriteVariant(iconV);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<string> RequestInstallTokenAsync(string name, VariantValue iconV, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_s(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sva{sv}",
                member: "RequestInstallToken");
            writer.WriteString(name);
            writer.WriteVariant(iconV);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task UninstallAsync(string desktopFileId, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "Uninstall");
            writer.WriteString(desktopFileId);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<string> GetDesktopEntryAsync(string desktopFileId)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_s(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "s",
                member: "GetDesktopEntry");
            writer.WriteString(desktopFileId);
            return writer.CreateMessage();
        }
    }
    public Task<(VariantValue IconV, string IconFormat, uint IconSize)> GetIconAsync(string desktopFileId)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_vsu(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "s",
                member: "GetIcon");
            writer.WriteString(desktopFileId);
            return writer.CreateMessage();
        }
    }
    public Task LaunchAsync(string desktopFileId, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "Launch");
            writer.WriteString(desktopFileId);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetSupportedLauncherTypesAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "SupportedLauncherTypes"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<DynamicLauncherProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static DynamicLauncherProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<DynamicLauncherProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<DynamicLauncherProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<DynamicLauncherProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "SupportedLauncherTypes": invalidated.Add("SupportedLauncherTypes"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static DynamicLauncherProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new DynamicLauncherProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "SupportedLauncherTypes":
                    reader.ReadSignature("u"u8);
                    props.SupportedLauncherTypes = reader.ReadUInt32();
                    changedList?.Add("SupportedLauncherTypes");
                    break;
                case "version":
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
record ScreenCastProperties
{
    public uint AvailableSourceTypes { get; set; } = default!;
    public uint AvailableCursorModes { get; set; } = default!;
    public uint Version { get; set; } = default!;
}
partial class ScreenCast : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.ScreenCast";
    public ScreenCast(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> CreateSessionAsync(Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "a{sv}",
                member: "CreateSession");
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> SelectSourcesAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "SelectSources");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> StartAsync(ObjectPath sessionHandle, string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "osa{sv}",
                member: "Start");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<System.Runtime.InteropServices.SafeHandle> OpenPipeWireRemoteAsync(ObjectPath sessionHandle, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_h(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "oa{sv}",
                member: "OpenPipeWireRemote");
            writer.WriteObjectPath(sessionHandle);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetAvailableSourceTypesAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AvailableSourceTypes"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<uint> GetAvailableCursorModesAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "AvailableCursorModes"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<ScreenCastProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static ScreenCastProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<ScreenCastProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<ScreenCastProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<ScreenCastProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "AvailableSourceTypes": invalidated.Add("AvailableSourceTypes"); break;
                    case "AvailableCursorModes": invalidated.Add("AvailableCursorModes"); break;
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static ScreenCastProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new ScreenCastProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "AvailableSourceTypes":
                    reader.ReadSignature("u"u8);
                    props.AvailableSourceTypes = reader.ReadUInt32();
                    changedList?.Add("AvailableSourceTypes");
                    break;
                case "AvailableCursorModes":
                    reader.ReadSignature("u"u8);
                    props.AvailableCursorModes = reader.ReadUInt32();
                    changedList?.Add("AvailableCursorModes");
                    break;
                case "version":
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
record EmailProperties
{
    public uint Version { get; set; } = default!;
}
partial class Email : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Email";
    public Email(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> ComposeEmailAsync(string parentWindow, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "sa{sv}",
                member: "ComposeEmail");
            writer.WriteString(parentWindow);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<EmailProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static EmailProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<EmailProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<EmailProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<EmailProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static EmailProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new EmailProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record TrashProperties
{
    public uint Version { get; set; } = default!;
}
partial class Trash : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Trash";
    public Trash(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<uint> TrashFileAsync(System.Runtime.InteropServices.SafeHandle fd)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_u(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "h",
                member: "TrashFile");
            writer.WriteHandle(fd);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<TrashProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static TrashProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<TrashProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<TrashProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<TrashProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static TrashProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new TrashProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record ProxyResolverProperties
{
    public uint Version { get; set; } = default!;
}
partial class ProxyResolver : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.ProxyResolver";
    public ProxyResolver(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<string[]> LookupAsync(string uri)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_as(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "s",
                member: "Lookup");
            writer.WriteString(uri);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<ProxyResolverProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static ProxyResolverProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<ProxyResolverProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<ProxyResolverProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<ProxyResolverProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static ProxyResolverProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new ProxyResolverProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record FileChooserProperties
{
    public uint Version { get; set; } = default!;
}
partial class FileChooser : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.FileChooser";
    public FileChooser(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task<ObjectPath> OpenFileAsync(string parentWindow, string title, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ssa{sv}",
                member: "OpenFile");
            writer.WriteString(parentWindow);
            writer.WriteString(title);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> SaveFileAsync(string parentWindow, string title, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ssa{sv}",
                member: "SaveFile");
            writer.WriteString(parentWindow);
            writer.WriteString(title);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<ObjectPath> SaveFilesAsync(string parentWindow, string title, Dictionary<string, VariantValue> options)
    {
        return this.Connection.CallMethodAsync(CreateMessage(), (Message m, object? s) => ReadMessage_o(m, (DesktopObject)s!), this);
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                signature: "ssa{sv}",
                member: "SaveFiles");
            writer.WriteString(parentWindow);
            writer.WriteString(title);
            writer.WriteDictionary(options);
            return writer.CreateMessage();
        }
    }
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<FileChooserProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static FileChooserProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<FileChooserProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<FileChooserProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<FileChooserProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static FileChooserProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new FileChooserProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
record SessionProperties
{
    public uint Version { get; set; } = default!;
}
partial class Session : DesktopObject
{
    private const string __Interface = "org.freedesktop.portal.Session";
    public Session(DesktopService service, ObjectPath path) : base(service, path)
    { }
    public Task CloseAsync()
    {
        return this.Connection.CallMethodAsync(CreateMessage());
        MessageBuffer CreateMessage()
        {
            var writer = this.Connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: Service.Destination,
                path: Path,
                @interface: __Interface,
                member: "Close");
            return writer.CreateMessage();
        }
    }
    public ValueTask<IDisposable> WatchClosedAsync(Action<Exception?, Dictionary<string, VariantValue>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
        => base.WatchSignalAsync(Service.Destination, __Interface, Path, "Closed", (Message m, object? s) => ReadMessage_aesv(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
    public Task<uint> GetVersionAsync()
        => this.Connection.CallMethodAsync(CreateGetPropertyMessage(__Interface, "version"), (Message m, object? s) => ReadMessage_v_u(m, (DesktopObject)s!), this);
    public Task<SessionProperties> GetPropertiesAsync()
    {
        return this.Connection.CallMethodAsync(CreateGetAllPropertiesMessage(__Interface), (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), this);
        static SessionProperties ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            return ReadProperties(ref reader);
        }
    }
    public ValueTask<IDisposable> WatchPropertiesChangedAsync(Action<Exception?, PropertyChanges<SessionProperties>> handler, bool emitOnCapturedContext = true, ObserverFlags flags = ObserverFlags.None)
    {
        return base.WatchPropertiesChangedAsync(__Interface, (Message m, object? s) => ReadMessage(m, (DesktopObject)s!), handler, emitOnCapturedContext, flags);
        static PropertyChanges<SessionProperties> ReadMessage(Message message, DesktopObject _)
        {
            var reader = message.GetBodyReader();
            reader.ReadString(); // interface
            List<string> changed = new(), invalidated = new();
            return new PropertyChanges<SessionProperties>(ReadProperties(ref reader, changed), ReadInvalidated(ref reader), changed.ToArray());
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
                    case "version": invalidated.Add("Version"); break;
                }
            }
            return invalidated?.ToArray() ?? Array.Empty<string>();
        }
    }
    private static SessionProperties ReadProperties(ref Reader reader, List<string>? changedList = null)
    {
        var props = new SessionProperties();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            var property = reader.ReadString();
            switch (property)
            {
                case "version":
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
partial class DesktopService
{
    public Tmds.DBus.Protocol.Connection Connection { get; }
    public string Destination { get; }
    public DesktopService(Tmds.DBus.Protocol.Connection connection, string destination)
        => (Connection, Destination) = (connection, destination);
    public Inhibit CreateInhibit(ObjectPath path) => new Inhibit(this, path);
    public Background CreateBackground(ObjectPath path) => new Background(this, path);
    public Location CreateLocation(ObjectPath path) => new Location(this, path);
    public Notification CreateNotification(ObjectPath path) => new Notification(this, path);
    public Screenshot CreateScreenshot(ObjectPath path) => new Screenshot(this, path);
    public Account CreateAccount(ObjectPath path) => new Account(this, path);
    public NetworkMonitor CreateNetworkMonitor(ObjectPath path) => new NetworkMonitor(this, path);
    public Print CreatePrint(ObjectPath path) => new Print(this, path);
    public Settings CreateSettings(ObjectPath path) => new Settings(this, path);
    public GameMode CreateGameMode(ObjectPath path) => new GameMode(this, path);
    public RemoteDesktop CreateRemoteDesktop(ObjectPath path) => new RemoteDesktop(this, path);
    public MemoryMonitor CreateMemoryMonitor(ObjectPath path) => new MemoryMonitor(this, path);
    public OpenURI CreateOpenURI(ObjectPath path) => new OpenURI(this, path);
    public Realtime CreateRealtime(ObjectPath path) => new Realtime(this, path);
    public Secret CreateSecret(ObjectPath path) => new Secret(this, path);
    public Camera CreateCamera(ObjectPath path) => new Camera(this, path);
    public InputCapture CreateInputCapture(ObjectPath path) => new InputCapture(this, path);
    public GlobalShortcuts CreateGlobalShortcuts(ObjectPath path) => new GlobalShortcuts(this, path);
    public PowerProfileMonitor CreatePowerProfileMonitor(ObjectPath path) => new PowerProfileMonitor(this, path);
    public DynamicLauncher CreateDynamicLauncher(ObjectPath path) => new DynamicLauncher(this, path);
    public ScreenCast CreateScreenCast(ObjectPath path) => new ScreenCast(this, path);
    public Email CreateEmail(ObjectPath path) => new Email(this, path);
    public Trash CreateTrash(ObjectPath path) => new Trash(this, path);
    public ProxyResolver CreateProxyResolver(ObjectPath path) => new ProxyResolver(this, path);
    public FileChooser CreateFileChooser(ObjectPath path) => new FileChooser(this, path);
    public Session CreateSession(ObjectPath path) => new Session(this, path);
}
class DesktopObject
{
    public DesktopService Service { get; }
    public ObjectPath Path { get; }
    protected Tmds.DBus.Protocol.Connection Connection => Service.Connection;
    protected DesktopObject(DesktopService service, ObjectPath path)
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
    protected static ObjectPath ReadMessage_o(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadObjectPath();
    }
    protected static (ObjectPath, Dictionary<string, VariantValue>) ReadMessage_oaesv(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadObjectPath();
        var arg1 = reader.ReadDictionaryOfStringToVariantValue();
        return (arg0, arg1);
    }
    protected static uint ReadMessage_v_u(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("u"u8);
        return reader.ReadUInt32();
    }
    protected static (string, string, VariantValue[]) ReadMessage_ssav(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadString();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadArrayOfVariantValue();
        return (arg0, arg1, arg2);
    }
    protected static Dictionary<string, VariantValue> ReadMessage_v_aesv(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("a{sv}"u8);
        return reader.ReadDictionaryOfStringToVariantValue();
    }
    protected static bool ReadMessage_b(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadBool();
    }
    protected static uint ReadMessage_u(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadUInt32();
    }
    protected static Dictionary<string, VariantValue> ReadMessage_aesv(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadDictionaryOfStringToVariantValue();
    }
    protected static Dictionary<string, Dictionary<string, VariantValue>> ReadMessage_aesaesv(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return ReadType_aesaesv(ref reader);
    }
    protected static VariantValue ReadMessage_v(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadVariantValue();
    }
    protected static (string, string, VariantValue) ReadMessage_ssv(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadString();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadVariantValue();
        return (arg0, arg1, arg2);
    }
    protected static int ReadMessage_i(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadInt32();
    }
    protected static bool ReadMessage_v_b(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("b"u8);
        return reader.ReadBool();
    }
    protected static System.Runtime.InteropServices.SafeHandle ReadMessage_h(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadHandle<Microsoft.Win32.SafeHandles.SafeFileHandle>();
    }
    protected static byte ReadMessage_y(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadByte();
    }
    protected static int ReadMessage_v_i(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("i"u8);
        return reader.ReadInt32();
    }
    protected static long ReadMessage_v_x(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        reader.ReadSignature("x"u8);
        return reader.ReadInt64();
    }
    protected static (ObjectPath, string, ulong, Dictionary<string, VariantValue>) ReadMessage_ostaesv(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadObjectPath();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadUInt64();
        var arg3 = reader.ReadDictionaryOfStringToVariantValue();
        return (arg0, arg1, arg2, arg3);
    }
    protected static (ObjectPath, (string, Dictionary<string, VariantValue>)[]) ReadMessage_oarsaesvz(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadObjectPath();
        var arg1 = ReadType_arsaesvz(ref reader);
        return (arg0, arg1);
    }
    protected static string ReadMessage_s(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadString();
    }
    protected static (VariantValue, string, uint) ReadMessage_vsu(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        var arg0 = reader.ReadVariantValue();
        var arg1 = reader.ReadString();
        var arg2 = reader.ReadUInt32();
        return (arg0, arg1, arg2);
    }
    protected static string[] ReadMessage_as(Message message, DesktopObject _)
    {
        var reader = message.GetBodyReader();
        return reader.ReadArrayOfString();
    }
    protected static Dictionary<string, Dictionary<string, VariantValue>> ReadType_aesaesv(ref Reader reader)
    {
        Dictionary<string, Dictionary<string, VariantValue>> dictionary = new();
        ArrayEnd dictEnd = reader.ReadDictionaryStart();
        while (reader.HasNext(dictEnd))
        {
            var key = reader.ReadString();
            var value = reader.ReadDictionaryOfStringToVariantValue();
            dictionary[key] = value;
        }
        return dictionary;
    }
    protected static (string, Dictionary<string, VariantValue>)[] ReadType_arsaesvz(ref Reader reader)
    {
        List<(string, Dictionary<string, VariantValue>)> list = new();
        ArrayEnd arrayEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(arrayEnd))
        {
            list.Add(ReadType_rsaesvz(ref reader));
        }
        return list.ToArray();
    }
    protected static (string, Dictionary<string, VariantValue>) ReadType_rsaesvz(ref Reader reader)
    {
        return (reader.ReadString(), reader.ReadDictionaryOfStringToVariantValue());
    }
    protected static void WriteType_aaesv(ref MessageWriter writer, Dictionary<string, VariantValue>[] value)
    {
        ArrayStart arrayStart = writer.WriteArrayStart(DBusType.Array);
        foreach (var item in value)
        {
            writer.WriteDictionary(item);
        }
        writer.WriteArrayEnd(arrayStart);
    }
    protected static void WriteType_arsaesvz(ref MessageWriter writer, (string, Dictionary<string, VariantValue>)[] value)
    {
        ArrayStart arrayStart = writer.WriteArrayStart(DBusType.Struct);
        foreach (var item in value)
        {
            WriteType_rsaesvz(ref writer, item);
        }
        writer.WriteArrayEnd(arrayStart);
    }
    protected static void WriteType_rsaesvz(ref MessageWriter writer, (string, Dictionary<string, VariantValue>) value)
    {
        writer.WriteStructureStart();
        writer.WriteString(value.Item1);
        writer.WriteDictionary(value.Item2);
    }
}
class PropertyChanges<TProperties>
{
    public PropertyChanges(TProperties properties, string[] invalidated, string[] changed)
        => (Properties, Invalidated, Changed) = (properties, invalidated, changed);
    public TProperties Properties { get; }
    public string[] Invalidated { get; }
    public string[] Changed { get; }
    public bool HasChanged(string property) => Array.IndexOf(Changed, property) != -1;
    public bool IsInvalidated(string property) => Array.IndexOf(Invalidated, property) != -1;
}
class PortalResponse
{
    public required string RequestPath { get; init; }
    public required Dictionary<string, VariantValue> Results { get; init; }

    public static async Task<PortalResponse> WaitAsync(Connection connection,
                                                       Func<Task<ObjectPath>> request,
                                                       CancellationToken cancel = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(request);

        var results = new List<PortalResponse>();
        var result = new TaskCompletionSource<PortalResponse>();
        string? requestPath = null;

        using var matcher = await connection.AddMatchAsync(
            new()
            {
                Type = MessageType.Signal,
                Member = "Response",
                Interface = "org.freedesktop.portal.Request",
            },
            reader: (message, state) =>
            {
                var reader = message.GetBodyReader();
                uint arg0 = reader.ReadUInt32();
                return new PortalResponse
                {
                    RequestPath = message.PathAsString!,
                    Results = reader.ReadDictionaryOfStringToVariantValue(),
                };
            },
            handler: (ex, response, _, __) =>
            {
                Console.WriteLine($"Got {response}");
                if (ex is null)
                {
                    lock (results)
                    {
                        if (requestPath != null)
                        {
                            if (response.RequestPath != requestPath)
                                return;

                            result.SetResult(response);
                            return;
                        }

                        results.Add(response);
                    }
                }
                else
                {
                    result.SetException(ex);
                }
            },
            ObserverFlags.NoSubscribe, emitOnCapturedContext: false).ConfigureAwait(false);

        var path = await request().WaitAsync(cancel).ConfigureAwait(false);

        lock (results)
        {
            requestPath = path;
            if (results.Find(r => r.RequestPath == path) is { } response)
                return response;
        }

        return await result.Task.WaitAsync(cancel).ConfigureAwait(false);
    }

    public override string ToString()
    {
        var result = new System.Text.StringBuilder();
        result.Append(this.RequestPath.AfterLast('/'));
        Console.WriteLine($"Request path: {RequestPath}");
        result.Append("{ ");
        foreach (var (key, value) in this.Results)
        {
            result.Append(key);
            result.Append('=');
            result.Append(value);
            result.AppendLine(",");
        }

        result.Append('}');
        return result.ToString();
    }
}
