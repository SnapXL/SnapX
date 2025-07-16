// SPDX-License-Identifier: GPL-3.0-or-later


using System.Net;
using System.Reflection;
using System.Text;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.OAuth;

public class OAuthListener : IDisposable
{
    public IOAuth2Loopback OAuth { get; private set; }

    private HttpListener listener;

    public OAuthListener(IOAuth2Loopback oauth)
    {
        OAuth = oauth;
    }

    public void Dispose()
    {
        if (listener != null)
        {
            listener.Close();
            listener = null;
        }
    }

    public async Task<bool> ConnectAsync()
    {
        Dispose();

        var ip = IPAddress.Loopback;
        var port = WebHelpers.GetRandomUnusedPort();
        var redirectURI = $"http://{ip}:{port}/";
        var state = Helpers.GetRandomAlphanumeric(32);

        OAuth.RedirectURI = redirectURI;
        OAuth.State = state;

        var url = OAuth.GetAuthorizationURL();

        if (string.IsNullOrEmpty(url))
        {
            DebugHelper.WriteLine("Authorization URL is empty.");
            return false;
        }

        URLHelpers.OpenURL(url);
        DebugHelper.WriteLine("Authorization URL is opened: " + url);

        try
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add(redirectURI);
            listener.Start();

            var context = await listener.GetContextAsync();
            var queryCode = context.Request.QueryString.Get("code");
            var queryState = context.Request.QueryString.Get("state");

            using var response = context.Response;
            var status = (queryState == state && !string.IsNullOrEmpty(queryCode))
                ? "Authorization completed successfully."
                : queryState != state
                    ? "Invalid state parameter."
                    : "Authorization did not succeed.";

            var assembly = Assembly.GetExecutingAssembly();
            await using var stream = assembly.GetManifestResourceStream("OAuthCallbackPage.html");
            if (stream == null || stream.Length == 0) return false;
            using var reader = new StreamReader(stream);
            var oAuthCallbackPage = reader.ReadToEnd();
            var responseText = oAuthCallbackPage.Replace("{0}", status);
            var buffer = Encoding.UTF8.GetBytes(responseText);

            response.ContentLength64 = buffer.Length;
            response.KeepAlive = false;

            await using var responseOutput = response.OutputStream;
            await responseOutput.WriteAsync(buffer, 0, buffer.Length);
            await responseOutput.FlushAsync();

            if (queryState == state && !string.IsNullOrEmpty(queryCode))
            {
                return await Task.Run(() => OAuth.GetAccessToken(queryCode));
            }
        }
        catch (ObjectDisposedException)
        {
            // Listener is DISPOSED.
        }
        finally
        {
            Dispose();
        }

        return false;
    }
}

