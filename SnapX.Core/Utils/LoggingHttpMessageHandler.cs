using System.Reflection;
using System.Text;
using Serilog;

namespace SnapX.Core.Utils;

public class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger _logger;

    public LoggingHttpMessageHandler(HttpMessageHandler innerHandler, ILogger logger)
        : base(innerHandler)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.Debug("Sending HTTP Request: {Method} {Uri} {@Headers}",
                request.Method, request.RequestUri, request.Headers);
            var response = await base.SendAsync(request, cancellationToken);

            _logger.Debug("Received HTTP Response: {StatusCode} for {Method} {Uri} (HTTP {Version})",
                response.StatusCode, request.Method, request.RequestUri, response.Version);

            _logger.Debug("Response Headers: {@Headers}", response.Headers);

            // Be careful, some response bodies are huge...
            var content = await response.Content.ReadAsStringAsync();
            var responseBodySizeBytes = Encoding.UTF8.GetByteCount(content);
            var responseBodySizeMiB = responseBodySizeBytes / (1024.0 * 1024.0);
            _logger.Debug("Response Body ({Size} MiB): {Content}", responseBodySizeMiB, content);
            return response;

        }
        catch (Exception ex)
        {
            _logger.Error(ex, ex.Message);
            return new HttpResponseMessage();
        }
    }
}
