// SPDX-License-Identifier: GPL-3.0-or-later

namespace SnapX.Core.Upload.Utils;

public class SSLBypassHelper : IDisposable
{
    private readonly HttpClientHandler _httpClientHandler;
    private readonly HttpClient _httpClient;

    public SSLBypassHelper()
    {
        _httpClientHandler = new HttpClientHandler
        {
            // Allow all SSL certificates
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        };

        _httpClient = HttpClientFactory.Get();
    }

    public void Dispose()
    {
        // I will not dispose the HttpClient. Say it with me.
    }
}

