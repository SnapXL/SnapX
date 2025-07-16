// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using SnapX.Core.Utils;
using HttpMethod = System.Net.Http.HttpMethod;

namespace SnapX.Core.Upload.OAuth;

public static class OAuthManager
{
    private const string ParameterConsumerKey = "oauth_consumer_key";
    private const string ParameterSignatureMethod = "oauth_signature_method";
    private const string ParameterSignature = "oauth_signature";
    private const string ParameterTimestamp = "oauth_timestamp";
    private const string ParameterNonce = "oauth_nonce";
    private const string ParameterVersion = "oauth_version";
    private const string ParameterToken = "oauth_token";
    private const string ParameterTokenSecret = "oauth_token_secret";
    private const string ParameterVerifier = "oauth_verifier";
    internal const string ParameterCallback = "oauth_callback";

    private const string PlainTextSignatureType = "PLAINTEXT";
    private const string HMACSHA1SignatureType = "HMAC-SHA1";
    private const string RSASHA1SignatureType = "RSA-SHA1";

    public static string? GenerateQuery(string? url, Dictionary<string, string?> args, HttpMethod httpMethod, OAuthInfo oauth)
    {
        return GenerateQuery(url, args, httpMethod, oauth, out _);
    }

    public static string? GenerateQuery(
        string? url,
        Dictionary<string, string?> args,
        HttpMethod httpMethod,
        OAuthInfo oauth,
        out Dictionary<string, string?> parameters)
    {
        // Ensure OAuth credentials are valid
        ValidateOAuthCredentials(oauth);

        parameters = new Dictionary<string, string?>
        {
            { ParameterVersion, oauth.OAuthVersion },
            { ParameterNonce, GenerateNonce() },
            { ParameterTimestamp, GenerateTimestamp() },
            { ParameterConsumerKey, oauth.ConsumerKey },
            // Add signature method
            {
                ParameterSignatureMethod,
                oauth.SignatureMethod switch
                {
                    OAuthInfo.OAuthInfoSignatureMethod.HMAC_SHA1 => HMACSHA1SignatureType,
                    OAuthInfo.OAuthInfoSignatureMethod.RSA_SHA1 => RSASHA1SignatureType,
                    _ => throw new NotImplementedException("Unsupported signature method")
                }
            }
        };

        // Add token parameters if present
        string? secret = null;
        if (!string.IsNullOrEmpty(oauth.UserToken) && !string.IsNullOrEmpty(oauth.UserSecret))
        {
            secret = oauth.UserSecret;
            parameters.Add(ParameterToken, oauth.UserToken);
        }
        else if (!string.IsNullOrEmpty(oauth.AuthToken) && !string.IsNullOrEmpty(oauth.AuthSecret))
        {
            secret = oauth.AuthSecret;
            parameters.Add(ParameterToken, oauth.AuthToken);

            if (!string.IsNullOrEmpty(oauth.AuthVerifier))
            {
                parameters.Add(ParameterVerifier, oauth.AuthVerifier);
            }
        }

        // Add custom arguments if present
        if (args != null)
        {
            foreach (var arg in args)
            {
                parameters[arg.Key] = arg.Value;
            }
        }

        // Generate the signature
        string? normalizedUrl = NormalizeUrl(url);
        string? normalizedParameters = NormalizeParameters(parameters);
        string signatureBase = GenerateSignatureBase(httpMethod, normalizedUrl, normalizedParameters);

        byte[] signatureData = oauth.SignatureMethod switch
        {
            OAuthInfo.OAuthInfoSignatureMethod.HMAC_SHA1 => GenerateSignature(signatureBase, oauth.ConsumerSecret, secret),
            OAuthInfo.OAuthInfoSignatureMethod.RSA_SHA1 => GenerateSignatureRSASHA1(signatureBase, oauth.ConsumerPrivateKey),
            _ => throw new NotImplementedException("Unsupported signature method")
        };

        string? signature = Convert.ToBase64String(signatureData);
        parameters[ParameterSignature] = signature;

        return $"{normalizedUrl}?{normalizedParameters}&{ParameterSignature}={URLHelpers.URLEncode(signature)}";
    }

    private static void ValidateOAuthCredentials(OAuthInfo oauth)
    {
        if (string.IsNullOrEmpty(oauth.ConsumerKey) ||
            (oauth.SignatureMethod == OAuthInfo.OAuthInfoSignatureMethod.HMAC_SHA1 && string.IsNullOrEmpty(oauth.ConsumerSecret)) ||
            (oauth.SignatureMethod == OAuthInfo.OAuthInfoSignatureMethod.RSA_SHA1 && string.IsNullOrEmpty(oauth.ConsumerPrivateKey)))
        {
            throw new Exception("ConsumerKey, ConsumerSecret, or ConsumerPrivateKey is missing.");
        }
    }


    public static string? GetAuthorizationURL(string? requestTokenResponse, OAuthInfo oauth, string authorizeURL, string? callback = null)
    {
        var args = HttpUtility.ParseQueryString(requestTokenResponse);

        if (args[ParameterToken] == null)
        {
            return null;
        }

        oauth.AuthToken = args[ParameterToken];
        var url = $"{authorizeURL}?{ParameterToken}={oauth.AuthToken}";

        if (!string.IsNullOrEmpty(callback))
        {
            url += $"&{ParameterCallback}={URLHelpers.URLEncode(callback)}";
        }

        if (args[ParameterTokenSecret] != null)
        {
            oauth.AuthSecret = args[ParameterTokenSecret];
        }

        return url;
    }

    public static NameValueCollection ParseAccessTokenResponse(string? accessTokenResponse, OAuthInfo oauth)
    {
        var args = HttpUtility.ParseQueryString(accessTokenResponse);
        if (args?.Get(ParameterToken) == null) return null;

        oauth.UserToken = args[ParameterToken];

        if (args[ParameterTokenSecret] != null)
        {
            oauth.UserSecret = args[ParameterTokenSecret];
            return args;
        }

        return null;
    }


    private static string GenerateSignatureBase(HttpMethod httpMethod, string? normalizedUrl,
        string? normalizedParameters) =>
        $"{httpMethod}&{URLHelpers.URLEncode(normalizedUrl)}&{URLHelpers.URLEncode(normalizedParameters)}";

    private static byte[] GenerateSignature(string signatureBase, string? consumerSecret, string? userSecret = null)
    {
        using var hmacsha1 = new HMACSHA1();
        var key = $"{Uri.EscapeDataString(consumerSecret)}&{(string.IsNullOrEmpty(userSecret) ? "" : Uri.EscapeDataString(userSecret))}";

        hmacsha1.Key = Encoding.ASCII.GetBytes(key);

        return hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(signatureBase));
    }

    private static byte[] GenerateSignatureRSASHA1(string signatureBase, string privateKey)
    {
        var dataBuffer = Encoding.ASCII.GetBytes(signatureBase);

        using var sha1 = GenerateSha1Hash(dataBuffer);
        using var algorithm = new RSACryptoServiceProvider();
        algorithm.FromXmlString(privateKey);
        var formatter = new RSAPKCS1SignatureFormatter(algorithm);
        formatter.SetHashAlgorithm("MD5");

        return formatter.CreateSignature(sha1);
    }

    private static SHA1 GenerateSha1Hash(byte[] dataBuffer)
    {
        var sha1 = SHA1.Create();

        using var cs = new CryptoStream(Stream.Null, sha1, CryptoStreamMode.Write);
        cs.Write(dataBuffer, 0, dataBuffer.Length);

        return sha1;
    }

    private static string? GenerateTimestamp()
    {
        var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds).ToString();
    }

    private static string? GenerateNonce()
    {
        return Helpers.GetRandomAlphanumeric(12);
    }

    private static string? NormalizeUrl(string? url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) return uri.ToString();

        var port = uri.Port switch
        {
            80 when uri.Scheme == "http" => string.Empty,
            443 when uri.Scheme == "https" => string.Empty,
            20 when uri.Scheme == "ftp" => string.Empty,
            _ => $":{uri.Port}"
        };

        return $"{uri.Scheme}://{uri.Host}{port}{uri.AbsolutePath}";

    }

    private static string? NormalizeParameters(Dictionary<string, string?> parameters)
    {
        return string.Join("&", parameters
            .OrderBy(x => x.Key)
            .ThenBy(x => x.Value)
            .Select(x => $"{x.Key}={URLHelpers.URLEncode(x.Value)}"));
    }
}

