
// SPDX-License-Identifier: GPL-3.0-or-later


using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;

namespace SnapX.Core.Utils.Miscellaneous;

using System.Net.Http;
/// <summary>
/// Represents the event arguments for the HTTP progress.
/// Compatible with the legacy Microsoft.AspNet.WebApi.Client implementation.
/// </summary>
public class HttpProgressEventArgs(int ProgressPercentage, object? UserState, long BytesTransferred, long? TotalBytes)
    : ProgressChangedEventArgs(ProgressPercentage, UserState)
{
    /// <summary>
    /// Gets the number of bytes transferred so far.
    /// </summary>
    public long BytesTransferred { get; } = BytesTransferred;

    /// <summary>
    /// Gets the total number of bytes to be transferred (null if unknown).
    /// </summary>
    public long? TotalBytes { get; } = TotalBytes;
}
public class ModernProgressHandler(HttpMessageHandler? InnerHandler = null)
    : DelegatingHandler(InnerHandler ?? new SocketsHttpHandler())
{
    public event EventHandler<HttpProgressEventArgs>? HttpSendProgress;
    public event EventHandler<HttpProgressEventArgs>? HttpReceiveProgress;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content != null)
        {
            var totalUploadSize = request.Content.Headers.ContentLength;

            request.Content = new ProgressableStreamContent(request.Content, (sent) =>
            {
                var percentage = CalculatePercentage(sent, totalUploadSize);
                var stateIdentifier = request.RequestUri?.ToString() ?? "Unknown-Stream";
                HttpSendProgress?.Invoke(this, new HttpProgressEventArgs(percentage, stateIdentifier, sent, totalUploadSize));
            });
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Content != null)
        {
            var totalDownloadSize = response.Content.Headers.ContentLength;
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var originalHeaders = response.Content.Headers;

            var progressStream = new ProgressReadStream(stream, (read) =>
            {
                int percentage = CalculatePercentage(read, totalDownloadSize);
                var stateIdentifier = request.RequestUri?.ToString() ?? "Unknown-Stream";

                HttpReceiveProgress?.Invoke(this, new HttpProgressEventArgs(percentage, stateIdentifier, read, totalDownloadSize));
            });

            var newContent = new StreamContent(progressStream);
            foreach (var header in originalHeaders)
            {
                newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            response.Content = newContent;
        }

        return response;
    }

    private static int CalculatePercentage(long current, long? total) =>
        total is > 0
            ? Math.Clamp((int)Math.Round((double)current / total.Value * 100), 0, 100)
            : 0;
}

public static class HttpClientFactory
{
    private static readonly Lazy<SocketsHttpHandler> _lazyHandler = new(CreateHandler);

    public static SocketsHttpHandler Handler => _lazyHandler.Value;
    private static SocketsHttpHandler CreateHandler()
    {
        var clientHandler = new SocketsHttpHandler
        {
            EnableMultipleHttp3Connections = true,
            EnableMultipleHttp2Connections = true,
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(60),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            KeepAlivePingDelay = TimeSpan.FromSeconds(5),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(2),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
            SslOptions =
            {
                AllowTlsResume = true,
                EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            },
            Proxy = HelpersOptions.CurrentProxy.GetWebProxy(),
        };

        if (SnapX.Settings?.AcceptInvalidSSLCertificates ?? false)
        {
            clientHandler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        return clientHandler;
    }

    public static ModernProgressHandler? _ph = null;
    public static readonly Action<HttpClient> ConfigureClient = client =>
    {
        client.DefaultRequestVersion = HttpVersion.Version30;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        client.DefaultRequestHeaders.UserAgent.ParseAdd(SnapXResources.UserAgent);
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };
    };
    // Using Lazy<T> to handle thread-safe initialization of the HttpClient
    private static Lazy<HttpClient> _lazyClient = new(() =>
    {
        HttpMessageHandler handler = Handler;

#if DEBUG
        // Only for DEBUG. Do not enable in production. Or you'll be fired.
        var loggingHandler = new LoggingHttpMessageHandler(Handler, DebugHelper.Logger);
        handler = loggingHandler;
#endif
        var ph = new ModernProgressHandler(handler);
        _ph = ph;
        var httpClient = new HttpClient(ph);
        ConfigureClient(httpClient);

        return httpClient;
    });


    public static HttpClient Get() => _lazyClient.Value;

    public static HttpClient GetCopy()
    {
        var source = Get();
        var handler = CreateHandler();

        HttpMessageHandler finalHandler = handler;
#if DEBUG
        finalHandler = new LoggingHttpMessageHandler(handler, DebugHelper.Logger);
#endif
        var ph = new ModernProgressHandler(finalHandler);
        var newClient = new HttpClient(ph)

        {
            DefaultRequestVersion = source.DefaultRequestVersion,
            DefaultVersionPolicy = source.DefaultVersionPolicy,
            BaseAddress = source.BaseAddress,
            Timeout = source.Timeout

        };

        ConfigureClient(newClient);

        return newClient;
    }
}

