using SnapX.Core.SharpCapture.Linux;
namespace Tmds.DBus;

static class ConnectionExtensions
{
    public static async Task<PortalResponse> Call(this Protocol.Connection connection,
        Func<Task<Protocol.ObjectPath>> request,
        CancellationToken cancel = default)
        => await PortalResponse.WaitAsync(connection, request, cancel);
}

