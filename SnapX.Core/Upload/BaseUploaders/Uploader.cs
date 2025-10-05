
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Handlers;
using System.Text;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using Math = System.Math;

namespace SnapX.Core.Upload.BaseUploaders;
public class Uploader
{
    public delegate void ProgressEventHandler(ProgressManager progress);
    public event ProgressEventHandler ProgressChanged;

    public event Action<string> EarlyURLCopyRequested;

    public bool IsUploading { get; protected set; }
    public UploaderErrorManager Errors { get; private set; } = new UploaderErrorManager();
    public bool IsError => !StopUploadRequested && Errors is { Count: > 0 };
    public int BufferSize { get; set; } = 8192;

    protected bool StopUploadRequested { get; set; }
    protected bool AllowReportProgress { get; set; } = true;
    protected bool ReturnResponseOnError { get; set; }

    protected ResponseInfo LastResponseInfo { get; set; }


    protected void OnProgressChanged(ProgressManager progress)
    {
        ProgressChanged?.Invoke(progress);
    }

    protected void OnEarlyURLCopyRequested(string? url)
    {
        if (EarlyURLCopyRequested != null && !string.IsNullOrEmpty(url))
        {
            EarlyURLCopyRequested(url);
        }
    }

    public string ToErrorString()
    {
        if (IsError)
        {
            return string.Join(Environment.NewLine, Errors);
        }

        return "";
    }

    public virtual void StopUpload()
    {
        if (IsUploading)
        {
            StopUploadRequested = true;
            DebugHelper.WriteLine("StopUpload called, but, SnapX does not support cancelling HTTP requests!");
        }
    }

    internal string? SendRequest(HttpMethod method, string? url, Dictionary<string, string?> args = null, NameValueCollection headers = null, CookieCollection cookies = null)
    {
        return SendRequest(method, url, (Stream)null, null, args, headers, cookies);
    }

    protected string? SendRequest(HttpMethod method, string? url, Stream data, string contentType = null,
        Dictionary<string, string?> args = null, NameValueCollection headers = null, CookieCollection cookies = null)
    {
        using var response = GetResponse(method, url, data, contentType, args, headers, cookies);
        return ProcessWebResponseText(response);
    }


    protected string? SendRequest(HttpMethod method, string? url, string? content, string contentType = null, Dictionary<string, string?> args = null, NameValueCollection headers = null,
        CookieCollection cookies = null)
    {
        var data = Encoding.UTF8.GetBytes(content);

        using var ms = new MemoryStream();
        ms.Write(data, 0, data.Length);

        return SendRequest(method, url, ms, contentType, args, headers, cookies);
    }

    internal string? SendRequestURLEncoded(HttpMethod method, string? url, Dictionary<string, string?> args, NameValueCollection headers = null, CookieCollection cookies = null)
    {
        string? query = URLHelpers.CreateQueryString(args);

        return SendRequest(method, url, query, RequestHelpers.ContentTypeURLEncoded, null, headers, cookies);
    }

    protected bool SendRequestDownload(HttpMethod method, string? url, Stream downloadStream,
        Dictionary<string, string?> args = null, NameValueCollection headers = null,
        CookieCollection cookies = null, string contentType = null)
    {
        using var response = GetResponse(method, url, downloadStream, contentType, args, headers, cookies);
        if (response?.Content == null) return false;
        using var responseStream = response.Content.ReadAsStream();
        responseStream.CopyStreamTo(downloadStream, BufferSize);
        return true;
    }


    protected string? SendRequestMultiPart(string? url, Dictionary<string, string?> args,
        NameValueCollection headers = null, CookieCollection cookies = null, HttpMethod method = null)
    {
        if (method == null) method = HttpMethod.Post;


        var multipartContent = GetMultipartFormDataContent(args, null, null, null, null);

        var requestMessage = new HttpRequestMessage(method, url)
        {
            Content = multipartContent
        };

        if (headers != null)
        {
            foreach (var key in headers.AllKeys)
            {
                requestMessage.Headers.TryAddWithoutValidation(key, headers[key]);
            }
        }

        if (cookies != null)
        {
            var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            requestMessage.Headers.Add("Cookie", cookieHeader);
        }
        var client = HttpClientFactory.Get();
        var response = client.SendAsync(requestMessage).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return ProcessWebResponseText(response);
    }

    protected MultipartFormDataContent GetMultipartFormDataContent(Dictionary<string, string?>? args, Stream? data, string? fileName, string? fileFormName, string? relatedData = null)
    {
        var multipartContent = new MultipartFormDataContent();

        if (args != null)
        {
            foreach (var arg in args)
            {
                multipartContent.Add(new StringContent(arg.Value), arg.Key);
            }
        }

        if (relatedData != null)
        {
            multipartContent.Add(
                new StringContent(relatedData, Encoding.UTF8, "application/json"),
                "file",
                fileName
            );
        }
        else if (data is not null)
        {
            var fileContent = new StreamContent(data);
            var mimeType = MimeTypes.GetMimeType(fileName);

            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            multipartContent.Add(fileContent, fileFormName, fileName);
        }
        return multipartContent;
    }
    protected UploadResult SendRequestFile(string? url, Stream data, string? fileName, string fileFormName,
        Dictionary<string, string?> args = null, NameValueCollection headers = null, CookieCollection cookies = null,
        HttpMethod? method = null, string contentType = null, string relatedData = null)
    {
        method ??= HttpMethod.Post;

        var result = new UploadResult();
        IsUploading = true;
        StopUploadRequested = false;
        EventHandler<HttpProgressEventArgs>? handler = null;

        try
        {
            var client = HttpClientFactory.Get();
            var multipartContent = GetMultipartFormDataContent(args, data, fileName, fileFormName, relatedData);

            var requestMessage = new HttpRequestMessage(method, url)
            {
                Content = multipartContent
            };

            if (headers != null)
            {
                foreach (var key in headers.AllKeys)
                {
                    requestMessage.Headers.TryAddWithoutValidation(key, headers[key]);
                }
            }
            requestMessage.Headers.TransferEncodingChunked = true;
            requestMessage.Content.Headers.ContentLength = null;
            if (cookies != null)
            {
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                requestMessage.Headers.Add("Cookie", cookieHeader);
            }

            var ph = HttpClientFactory._ph!;
            var requestLength = data.Length;
            var progress = new ProgressManager(requestLength);

            handler = (_, args) =>
            {
                progress.Length = new[] { requestLength, args.TotalBytes ?? 0, args.BytesTransferred }.Max();

                if (AllowReportProgress && progress.UpdateAbsoluteProgress(args.BytesTransferred))
                    OnProgressChanged(progress);
            };

            ph.HttpSendProgress += handler;
            var response = client.SendAsync(requestMessage).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                result.ResponseInfo = ProcessWebResponse(response);
                result.Response = result.ResponseInfo?.ResponseText;
                result.IsSuccess = true;
            }
            else
            {
                result.IsSuccess = false;
                result.Response = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception e)
        {
            if (!StopUploadRequested)
            {
                var response = ProcessError(e, url);

                if (ReturnResponseOnError && e is HttpRequestException)
                {
                    result.Response = response;
                }

                result.IsSuccess = false;
            }
        }
        finally
        {
            IsUploading = false;
            HttpClientFactory._ph!.HttpSendProgress -= handler; // prevent dangling event ref
        }

        return result;
    }


    protected UploadResult? SendRequestFileRange(
        string? url,
        Stream data,
        string? fileName,
        long contentPosition = 0,
        long contentLength = -1,
        Dictionary<string, string?>? args = null,
        NameValueCollection? headers = null,
        CookieCollection? cookies = null,
        HttpMethod? method = null)
    {
        method ??= HttpMethod.Put;
        var result = new UploadResult();
        IsUploading = true;
        StopUploadRequested = false;

        try
        {
            url = URLHelpers.CreateQueryString(url, args);

            if (contentLength == -1)
            {
                contentLength = data.Length;
            }

            contentLength = Math.Min(contentLength, data.Length - contentPosition);
            var contentType = MimeTypes.GetMimeType(fileName);

            headers ??= new NameValueCollection();
            var startByte = contentPosition;
            var endByte = startByte + contentLength - 1;
            var dataLength = data.Length;
            headers.Add("Content-Range", $"bytes {startByte}-{endByte}/{dataLength}");

            var client = HttpClientFactory.Get();
            var requestMessage = new HttpRequestMessage(method, url);

            if (data.CanSeek)
            {
                data.Seek(contentPosition, SeekOrigin.Begin);
            }

            var partialStream = new LimitedStream(data, contentLength);
            var streamContent = new StreamContent(partialStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            streamContent.Headers.ContentLength = contentLength;

            requestMessage.Content = streamContent;

            foreach (var key in headers.AllKeys)
            {
                requestMessage.Headers.TryAddWithoutValidation(key, headers[key]);
            }

            if (cookies is { Count: > 0 })
            {
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                requestMessage.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            }

            var response = client.SendAsync(requestMessage).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                result.ResponseInfo = ProcessWebResponse(response);
                result.Response = result.ResponseInfo?.ResponseText;
                result.IsSuccess = true;
            }
            else
            {
                result.IsSuccess = false;
                result.Response = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception e)
        {
            if (!StopUploadRequested)
            {
                var response = ProcessError(e, url);

                if (ReturnResponseOnError && e is HttpRequestException)
                {
                    result.Response = response;
                }

                result.IsSuccess = false;
            }
        }
        finally
        {
            IsUploading = false;
        }

        return result;
    }

    class LimitedStream(Stream BaseStream, long Length1) : Stream
    {
        private long _read;
        public override bool CanRead => BaseStream.CanRead;
        public override bool CanSeek => BaseStream.CanSeek;
        public override bool CanWrite => BaseStream.CanWrite;
        public override long Length => Length1;
        public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }
        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = Length1 - _read;
            if (remaining <= 0) return 0;
            var toRead = (int)Math.Min(remaining, count);
            var read = BaseStream.Read(buffer, offset, toRead);
            _read += read;
            return read;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
    protected HttpResponseMessage? GetResponse(HttpMethod method, string? url, Stream data = null, string contentType = null, Dictionary<string, string?> args = null,
        NameValueCollection headers = null, CookieCollection cookies = null, bool allowNon2xxResponses = false)
    {
        IsUploading = true;
        StopUploadRequested = false;

        try
        {
            url = URLHelpers.CreateQueryString(url, args);
            long contentLength = 0;
            if (data != null)
            {
                contentLength = data.Length;
            }

            StreamContent? requestContent = null;

            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                requestContent = new StreamContent(data);
            }
            var requestMessage = new HttpRequestMessage(method, url)
            {
                Content = requestContent
            };

            if (headers != null)
            {
                foreach (var key in headers.AllKeys)
                {
                    requestMessage.Headers.TryAddWithoutValidation(key, headers[key]);
                }
            }

            if (cookies != null)
            {
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                requestMessage.Headers.Add("Cookie", cookieHeader);
            }

            var client = HttpClientFactory.Get();
            var response = client.SendAsync(requestMessage).ConfigureAwait(false).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode && !allowNon2xxResponses)
            {
                response.EnsureSuccessStatusCode();
            }

            return response;
        }
        catch (Exception e)
        {
            if (!StopUploadRequested)
            {
                ProcessError(e, url);
            }
        }
        finally
        {
            IsUploading = false;
        }

        return null;
    }

    #region Helper methods

    protected bool TransferData(Stream dataStream, Stream requestStream, long dataPosition = 0, long dataLength = -1)
    {
        if (dataPosition >= dataStream.Length)
        {
            return true;
        }

        if (dataStream.CanSeek)
        {
            dataStream.Position = dataPosition;
        }

        if (dataLength == -1)
        {
            dataLength = dataStream.Length;
        }
        dataLength = Math.Min(dataLength, dataStream.Length - dataPosition);

        ProgressManager progress = new ProgressManager(dataStream.Length, dataPosition);
        int length = (int)Math.Min(BufferSize, dataLength);
        byte[] buffer = new byte[length];
        int bytesRead;

        long bytesRemaining = dataLength;
        while (!StopUploadRequested && (bytesRead = dataStream.Read(buffer, 0, length)) > 0)
        {
            requestStream.Write(buffer, 0, bytesRead);
            bytesRemaining -= bytesRead;
            length = (int)Math.Min(buffer.Length, bytesRemaining);

            if (AllowReportProgress && progress.UpdateProgress(bytesRead))
            {
                OnProgressChanged(progress);
            }
        }

        return !StopUploadRequested;
    }

    private string? ProcessError(Exception e, string? requestURL)
    {
        string? responseText = null;

        if (e != null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Error message:");
            sb.AppendLine(e.Message);

            if (!string.IsNullOrEmpty(requestURL))
            {
                sb.AppendLine();
                sb.AppendLine("Request URL:");
                sb.AppendLine(requestURL);
            }

            switch (e)
            {
                case HttpRequestException httpRequestException:
                    {
                        if (httpRequestException.Data["HttpResponseMessage"] is HttpResponseMessage response)
                        {
                            try
                            {
                                ResponseInfo responseInfo = ProcessWebResponse(response);

                                if (responseInfo != null)
                                {
                                    responseText = responseInfo.ResponseText;

                                    sb.AppendLine();
                                    sb.AppendLine("Status code:");
                                    sb.AppendLine($"({(int)responseInfo.StatusCode}) {responseInfo.StatusDescription}");

                                    if (!string.IsNullOrEmpty(requestURL) && !requestURL.Equals(responseInfo.ResponseURL))
                                    {
                                        sb.AppendLine();
                                        sb.AppendLine("Response URL:");
                                        sb.AppendLine(responseInfo.ResponseURL);
                                    }

                                    if (responseInfo.Headers != null)
                                    {
                                        sb.AppendLine();
                                        sb.AppendLine("Headers:");
                                        sb.AppendLine(responseInfo.Headers.ToString().TrimEnd());
                                    }

                                    sb.AppendLine();
                                    sb.AppendLine("Response text:");
                                    sb.AppendLine(responseInfo.ResponseText);
                                }
                            }
                            catch (Exception nested)
                            {
                                DebugHelper.WriteException(nested);
                            }
                        }

                        break;
                    }
            }

            sb.AppendLine();
            sb.AppendLine("Stack trace:");
            sb.Append(e.StackTrace);

            var errorText = sb.ToString();

            Errors ??= new UploaderErrorManager();
            Errors.Add(errorText);

            DebugHelper.WriteLine("Error:\r\n" + errorText);
        }

        return responseText;
    }

    private Dictionary<string, string> ConvertHeadersToDictionary(HttpResponseMessage response)
    {
        var headersDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            headersDictionary[header.Key] = string.Join(", ", header.Value);
        }

        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headersDictionary[header.Key] = string.Join(", ", header.Value);
            }
        }

        return headersDictionary;
    }


    private ResponseInfo ProcessWebResponse(HttpResponseMessage response)
    {
        if (response == null)
        {
            DebugHelper.Logger.Error("HttpResponseMessage was null.");
            return new ResponseInfo
            {
                StatusCode = 0,
                StatusDescription = "No Response",
                ResponseURL = string.Empty,
                Headers = new Dictionary<string, string>()
            };
        }

        var responseInfo = new ResponseInfo
        {
            StatusCode = response.StatusCode,
            StatusDescription = response.ReasonPhrase ?? "ERROR",
            ResponseURL = response.RequestMessage?.RequestUri?.OriginalString ?? string.Empty,
            Headers = ConvertHeadersToDictionary(response),
            ResponseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        };

        // DebugHelper.WriteLine($"ResponseInfo: StatusCode={responseInfo.StatusCode} StatusDescription={responseInfo.StatusDescription} ResponseText={responseInfo.ResponseText} ResponseURL={responseInfo.ResponseURL}");

        LastResponseInfo = responseInfo;
        return responseInfo;
    }


    protected string? ProcessWebResponseText(HttpResponseMessage response)
    {
        var responseInfo = ProcessWebResponse(response);

        return responseInfo.ResponseText;
    }


    #endregion Helper methods

    #region OAuth methods

    protected string? GetAuthorizationURL(string? requestTokenURL, string authorizeURL, OAuthInfo authInfo,
        Dictionary<string, string?> customParameters = null, HttpMethod httpMethod = null)
    {
        if (httpMethod == null) httpMethod = HttpMethod.Get;
        string? url = OAuthManager.GenerateQuery(requestTokenURL, customParameters, httpMethod, authInfo);

        string? response = SendRequest(httpMethod, url);

        if (!string.IsNullOrEmpty(response))
        {
            return OAuthManager.GetAuthorizationURL(response, authInfo, authorizeURL);
        }
        return null;
    }

    protected bool GetAccessToken(string? accessTokenURL, OAuthInfo authInfo, HttpMethod httpMethod = null)
    {
        if (httpMethod == null) httpMethod = HttpMethod.Get;
        return GetAccessTokenEx(accessTokenURL, authInfo, httpMethod) != null;
    }

    protected NameValueCollection GetAccessTokenEx(string? accessTokenURL, OAuthInfo authInfo, HttpMethod httpMethod = null)
    {
        if (httpMethod == null) httpMethod = HttpMethod.Get;
        if (string.IsNullOrEmpty(authInfo.AuthToken) || string.IsNullOrEmpty(authInfo.AuthSecret))
        {
            throw new Exception("Auth infos missing. Open Authorization URL first.");
        }

        string? url = OAuthManager.GenerateQuery(accessTokenURL, null, httpMethod, authInfo);

        string? response = SendRequest(httpMethod, url);

        if (!string.IsNullOrEmpty(response))
        {
            return OAuthManager.ParseAccessTokenResponse(response, authInfo);
        }

        return null;
    }

    #endregion OAuth methods
}
