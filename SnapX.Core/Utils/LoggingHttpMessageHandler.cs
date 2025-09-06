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
            var requestHeaderString = "{\n" + string.Join(",\n", request.Headers.Select(h =>
                $"  \"{h.Key}\": \"{string.Join("; ", h.Value)}\"")) + "\n}";

            Logger.Debug("Sending HTTP Request: {Method} {Uri} {Headers}",
                request.Method, request.RequestUri, requestHeaderString);
            var response = await base.SendAsync(request, cancellationToken);

            Logger.Debug("Received HTTP Response: {StatusCode} for {Method} {Uri} (HTTP {Version})",
                response.StatusCode, request.Method, request.RequestUri, response.Version);
            var headerString = "{\n" + string.Join(",\n", response.Headers.Select(h =>
                $"  \"{h.Key}\": \"{string.Join("; ", h.Value)}\"")) + "\n}";
            Logger.Debug("Response Headers: {Headers}", headerString);

            var contentType = response.Content.Headers.ContentType?.MediaType;

            // Be careful, some response bodies are huge...
            if (!IsBinaryContentType(contentType))
            {
                var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                var responseBodySizeBytes = contentBytes.Length;
                var responseBodySizeMiB = responseBodySizeBytes / (1024.0 * 1024.0);
                var content = Encoding.UTF8.GetString(contentBytes);
                Logger.Debug("Response Body ({Size:N2} MiB): {Content}", responseBodySizeMiB, content);
            }
            else
            {
                var contentLength = response.Content.Headers.ContentLength;
                var sizeMiB = (contentLength ?? 0) / (1024.0 * 1024.0);
                Logger.Debug("Response body is binary (Content-Type: {ContentType}, Size: {Size:N2} MiB), skipping body logging.",
                    contentType,
                    sizeMiB);
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
        return contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) ||
               contentType.StartsWith("font/") ||
               contentType.StartsWith("application/zip") ||
               contentType.StartsWith("application/x-7z-compressed") ||
               contentType.StartsWith("application/x-rar-compressed") ||
               contentType.StartsWith("application/x-tar") ||
               contentType.StartsWith("application/gzip") ||
               contentType.StartsWith("application/msword") ||
               contentType.StartsWith("application/vnd.ms-excel") || // xls
               contentType.StartsWith("application/vnd.ms-powerpoint") || // ppt
               contentType.StartsWith("application/vnd.openxmlformats-officedocument") || // Office Open XML formats (.docx, .xlsx, .pptx)
               contentType.StartsWith("application/x-shockwave-flash") || // flash files
               contentType.StartsWith("application/x-msdownload") || // exe, dll
               contentType.StartsWith("application/x-binary") ||
               contentType.StartsWith("application/x-msdos-program") ||
               contentType.StartsWith("application/x-java-archive") || // jar
               contentType.StartsWith("application/x-font-ttf") ||
               contentType.StartsWith("application/x-font-woff") ||
               contentType.StartsWith("application/x-font-woff2") ||
               contentType.StartsWith("application/vnd.android.package-archive") || // apk
               contentType.StartsWith("application/x-debian-package") ||
               contentType.StartsWith("application/vnd.oasis.opendocument") || // LibreOffice formats
               contentType.StartsWith("application/x-rpm") || // RPM packages
               contentType.StartsWith("application/vnd.snap") || // Snap packages
               contentType.StartsWith("application/x-snap") || // Sometimes used for snap
               contentType.StartsWith("application/x-appimage") || // AppImage (if you want)
               contentType.StartsWith("application/x-flatpak") ||
               contentType.StartsWith("application/x-ostree-archive") ||
               contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }
}
