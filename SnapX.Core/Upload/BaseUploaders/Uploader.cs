
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using Math = System.Math;

namespace SnapX.Core.Upload.BaseUploaders;
public class Uploader
{
    public delegate void ProgressEventHandler(ProgressManager progress);
    public event ProgressEventHandler ProgressChanged;

    public event Action<string> EarlyURLCopyRequested;

    public bool IsUploading { get; protected set; }
    public UploaderErrorManager Errors { get; private set; } = new UploaderErrorManager();
    public bool IsError => !StopUploadRequested && Errors != null && Errors.Count > 0;
    public int BufferSize { get; set; } = 8192;

    protected bool StopUploadRequested { get; set; }
    protected bool AllowReportProgress { get; set; } = true;
    protected bool ReturnResponseOnError { get; set; }

    protected ResponseInfo LastResponseInfo { get; set; }

    private HttpWebRequest currentWebRequest;

    protected void OnProgressChanged(ProgressManager progress)
    {
        ProgressChanged?.Invoke(progress);
    }

    protected void OnEarlyURLCopyRequested(string url)
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

            if (currentWebRequest != null)
            {
                try
                {
                    currentWebRequest.Abort();
                }
                catch (Exception e)
                {
                    DebugHelper.WriteException(e);
                }
            }
        }
    }

    internal string SendRequest(HttpMethod method, string url, Dictionary<string, string> args = null, NameValueCollection headers = null, CookieCollection cookies = null)
    {
        return SendRequest(method, url, (Stream)null, null, args, headers, cookies);
    }

    protected string SendRequest(HttpMethod method, string url, Stream data, string contentType = null,
        Dictionary<string, string> args = null, NameValueCollection headers = null, CookieCollection cookies = null)
    {
        using var response = GetResponse(method, url, data, contentType, args, headers, cookies);
        return ProcessWebResponseText(response);
    }


    protected string SendRequest(HttpMethod method, string url, string content, string contentType = null, Dictionary<string, string> args = null, NameValueCollection headers = null,
        CookieCollection cookies = null)
    {
        byte[] data = Encoding.UTF8.GetBytes(content);

        using (MemoryStream ms = new MemoryStream())
        {
            ms.Write(data, 0, data.Length);

            return SendRequest(method, url, ms, contentType, args, headers, cookies);
        }
    }

    internal string SendRequestURLEncoded(HttpMethod method, string url, Dictionary<string, string> args, NameValueCollection headers = null, CookieCollection cookies = null)
    {
        string query = URLHelpers.CreateQueryString(args);

        return SendRequest(method, url, query, RequestHelpers.ContentTypeURLEncoded, null, headers, cookies);
    }

    protected bool SendRequestDownload(HttpMethod method, string url, Stream downloadStream,
        Dictionary<string, string> args = null, NameValueCollection headers = null,
        CookieCollection cookies = null, string contentType = null)
    {
        using var response = GetResponse(method, url, downloadStream, contentType, args, headers, cookies);
        if (response?.Content == null) return false;
        using var responseStream = response.Content.ReadAsStream();
        responseStream.CopyStreamTo(downloadStream, BufferSize);
        return true;
    }


    protected string SendRequestMultiPart(string url, Dictionary<string, string> args,
        NameValueCollection headers = null, CookieCollection cookies = null, HttpMethod method = null)
    {
        if (method == null) method = HttpMethod.Post;

        string boundary = RequestHelpers.CreateBoundary();
        string contentType = RequestHelpers.ContentTypeMultipartFormData + "; boundary=" + boundary;

        // Create multipart/form-data body
        byte[] data = RequestHelpers.MakeInputContent(boundary, args);

        using var stream = new MemoryStream(data);

        using var response = GetResponse(method, url, stream, contentType, null, headers, cookies);
        return ProcessWebResponseText(response);
    }

    protected UploadResult SendRequestFile(string url, Stream data, string fileName, string fileFormName,
        Dictionary<string, string> args = null, NameValueCollection headers = null, CookieCollection cookies = null,
        HttpMethod method = null, string contentType = RequestHelpers.ContentTypeMultipartFormData, string relatedData = null)
    {
        method ??= HttpMethod.Post;

        var result = new UploadResult();
        IsUploading = true;
        StopUploadRequested = false;

        try
        {
            var client = HttpClientFactory.Get();
            var boundary = RequestHelpers.CreateBoundary();
            contentType += "; boundary=" + boundary;

            var multipartContent = new MultipartFormDataContent(boundary);

            if (args != null)
            {
                foreach (var arg in args)
                {
                    multipartContent.Add(new StringContent(arg.Value), arg.Key);
                }
            }

            // Add related data if provided (this could be JSON or some other related data)
            if (relatedData != null)
            {
                var relatedContent = RequestHelpers.MakeRelatedFileInputContentOpen(boundary, "application/json; charset=UTF-8", relatedData, fileName);

                var byteArrayContent = new ByteArrayContent(relatedContent);

                byteArrayContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                multipartContent.Add(byteArrayContent, "relatedData");
            }

            var fileContent = new StreamContent(data);
            fileContent.Headers.Add("Content-Type", contentType);
            multipartContent.Add(fileContent, fileFormName, fileName);

            if (headers != null)
            {
                foreach (var key in headers.AllKeys)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(key, headers[key]);
                }
            }

            if (cookies != null)
            {
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            }

            var requestMessage = new HttpRequestMessage(method, url)
            {
                Content = multipartContent
            };

            var response = client.SendAsync(requestMessage).Result;

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
            currentWebRequest = null;
            IsUploading = false;
        }

        return result;
    }


    protected UploadResult SendRequestFileRange(string url, Stream data, string fileName, long contentPosition = 0, long contentLength = -1,
        Dictionary<string, string> args = null, NameValueCollection headers = null, CookieCollection cookies = null, HttpMethod method = null)
    {
        if (method == null) method = HttpMethod.Put;
        UploadResult result = new UploadResult();

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

            string contentType = MimeTypes.GetMimeType(fileName);

            if (headers == null)
            {
                headers = [];
            }
            long startByte = contentPosition;
            long endByte = startByte + contentLength - 1;
            long dataLength = data.Length;
            headers.Add("Content-Range", $"bytes {startByte}-{endByte}/{dataLength}");

            // HttpWebRequest request = CreateHttpRequest(method, url, headers, cookies, contentType, contentLength);
            //
            // using (Stream requestStream = request.GetRequestStream())
            // {
            //     if (!TransferData(data, requestStream, contentPosition, contentLength))
            //     {
            //         return null;
            //     }
            // }
            //
            // using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            // {
            //     result.ResponseInfo = ProcessWebResponse(response);
            //     result.Response = result.ResponseInfo?.ResponseText;
            // }

            result.IsSuccess = true;
        }
        catch (Exception e)
        {
            if (!StopUploadRequested)
            {
                string response = ProcessError(e, url);

                if (ReturnResponseOnError && e is WebException)
                {
                    result.Response = response;
                }
            }
        }
        finally
        {
            currentWebRequest = null;
            IsUploading = false;
        }

        return result;
    }

    protected HttpResponseMessage? GetResponse(HttpMethod method, string url, Stream data = null, string contentType = null, Dictionary<string, string> args = null,
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
            var request = RequestHelpers.CreateHttpRequest(method,  url, headers, cookies, contentType, contentLength).ConfigureAwait(false).GetAwaiter().GetResult();
            currentWebRequest = null;

            if (data != null && (method == HttpMethod.Post || method == HttpMethod.Put))
            {
                using var requestStream = request.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                data.CopyTo(requestStream);
            }

            var client = HttpClientFactory.Get();
            var response = client.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();

            return response;

        }
        catch (WebException we) when (we.Response != null && allowNon2xxResponses)
        {
            // if we.Response != null, then the request was successful, but
            // returned a non-200 status code
            return null;
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
            currentWebRequest = null;
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

private string ProcessError(Exception e, string requestURL)
{
    string responseText = null;

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

    private HttpRequestMessage CreateHttpRequest(HttpMethod method, string url, NameValueCollection headers = null, CookieCollection cookies = null,
        string contentType = null, long contentLength = 0)
    {
        LastResponseInfo = null;

        return RequestHelpers.CreateHttpRequest(method, url, headers, cookies, contentType, contentLength).GetAwaiter().GetResult();
    }
    private Dictionary<string, List<string>> ConvertHeadersToDictionary(HttpResponseMessage response)
    {
        var headersDictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            headersDictionary[header.Key] = header.Value.ToList();
        }

        if (response.Content != null)
        {
            foreach (var header in response.Content.Headers)
            {
                headersDictionary[header.Key] = header.Value.ToList();
            }
        }

        return headersDictionary;
    }


    private ResponseInfo ProcessWebResponse(HttpResponseMessage response)
    {
        if (response != null)
        {
            var responseInfo = new ResponseInfo
            {
                StatusCode = response.StatusCode,
                StatusDescription = response.ReasonPhrase ?? "OK",
                ResponseURL = response.RequestMessage?.RequestUri?.OriginalString ?? string.Empty,
                Headers = ConvertHeadersToDictionary(response)
            };

            using (var responseStream = response.Content.ReadAsStream())
            using (var reader = new StreamReader(responseStream, Encoding.UTF8))
            {
                responseInfo.ResponseText = reader.ReadToEnd();
            }

            LastResponseInfo = responseInfo;
            return responseInfo;
        }

        return null;
    }


    protected string ProcessWebResponseText(HttpResponseMessage response)
    {
        ResponseInfo responseInfo = ProcessWebResponse(response);

        return responseInfo?.ResponseText;
    }


    #endregion Helper methods

    #region OAuth methods

    protected string GetAuthorizationURL(string requestTokenURL, string authorizeURL, OAuthInfo authInfo,
        Dictionary<string, string> customParameters = null, HttpMethod httpMethod = null)
    {
        if (httpMethod == null) httpMethod = HttpMethod.Get;
        string url = OAuthManager.GenerateQuery(requestTokenURL, customParameters, httpMethod, authInfo);

        string response = SendRequest(httpMethod, url);

        if (!string.IsNullOrEmpty(response))
        {
            return OAuthManager.GetAuthorizationURL(response, authInfo, authorizeURL);
        }
        return null;
    }

    protected bool GetAccessToken(string accessTokenURL, OAuthInfo authInfo, HttpMethod httpMethod = null)
    {
        if (httpMethod == null) httpMethod = HttpMethod.Get;
        return GetAccessTokenEx(accessTokenURL, authInfo, httpMethod) != null;
    }

    protected NameValueCollection GetAccessTokenEx(string accessTokenURL, OAuthInfo authInfo, HttpMethod httpMethod = null)
    {
        if (httpMethod == null) httpMethod = HttpMethod.Get;
        if (string.IsNullOrEmpty(authInfo.AuthToken) || string.IsNullOrEmpty(authInfo.AuthSecret))
        {
            throw new Exception("Auth infos missing. Open Authorization URL first.");
        }

        string url = OAuthManager.GenerateQuery(accessTokenURL, null, httpMethod, authInfo);

        string response = SendRequest(httpMethod, url);

        if (!string.IsNullOrEmpty(response))
        {
            return OAuthManager.ParseAccessTokenResponse(response, authInfo);
        }

        return null;
    }

    #endregion OAuth methods
}
