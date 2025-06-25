using System.Text;
using Serilog;

namespace SnapX.Core.Utils;

public class LoggingHttpMessageHandler(HttpMessageHandler InnerHandler, ILogger Logger)
    : DelegatingHandler(InnerHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            Logger.Debug("Sending HTTP Request: {Method} {Uri} {@Headers}",
                request.Method, request.RequestUri, request.Headers);
            var response = await base.SendAsync(request, cancellationToken);

            Logger.Debug("Received HTTP Response: {StatusCode} for {Method} {Uri} (HTTP {Version})",
                response.StatusCode, request.Method, request.RequestUri, response.Version);

            Logger.Debug("Response Headers: {@Headers}", response.Headers);

            var contentType = response.Content.Headers.ContentType?.MediaType;

            // Be careful, some response bodies are huge...
            if (!IsBinaryContentType(contentType))
            {
                var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                var responseBodySizeBytes = contentBytes.Length;
                var responseBodySizeMiB = responseBodySizeBytes / (1024.0 * 1024.0);
                var content = Encoding.UTF8.GetString(contentBytes);
                Logger.Debug("Response Body ({Size} MiB): {Content}", responseBodySizeMiB, content);
            }
            else
            {
                Logger.Debug("Response body is binary (Content-Type: {ContentType}), skipping body logging.", contentType);
            }
            return response;

        }
        catch (Exception ex)
        {
            Logger.Error(ex, ex.Message);
            return new HttpResponseMessage();
        }
    }
    private static bool IsBinaryContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        // Common binary content types to skip
        if (contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
