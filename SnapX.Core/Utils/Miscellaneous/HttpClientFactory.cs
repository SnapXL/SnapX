// SPDX-License-Identifier: GPL-3.0-or-later


using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;

namespace SnapX.Core.Utils.Miscellaneous;
public static class HttpClientFactory
{
    // Using Lazy<T> to handle thread-safe initialization of the HttpClient
    private static Lazy<HttpClient> _lazyClient = new(() =>
    {
        var clientHandler = new SocketsHttpHandler
        {
            EnableMultipleHttp3Connections = true,
            EnableMultipleHttp2Connections = true,
            SslOptions =
            {
                AllowTlsResume = true,
                EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12
            },
            Proxy = HelpersOptions.CurrentProxy.GetWebProxy(),
        };
        if (SnapX.Settings.AcceptInvalidSSLCertificates)
        {
            clientHandler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }
        HttpMessageHandler handler = clientHandler;

#if DEBUG
        // Only for DEBUG. Do not enable in production. Or you'll be fired.
        var loggingHandler = new LoggingHttpMessageHandler(clientHandler, DebugHelper.Logger);
        handler = loggingHandler;
#endif
        var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestVersion = HttpVersion.Version20;
        httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(SnapXResources.UserAgent);
        httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };

        return httpClient;
    });


    public static HttpClient Get() => _lazyClient.Value;
}

