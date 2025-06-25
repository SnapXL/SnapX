
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.OAuth;
using SnapX.Core.Upload.Utils;

namespace SnapX.Core.Upload.File;
public class JiraFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.Jira;

    public override bool CheckConfig(UploadersConfig config)
    {
        return OAuthInfo.CheckOAuth(config.JiraOAuthInfo);
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Jira(config.JiraHost, config.JiraOAuthInfo, config.JiraIssuePrefix);
    }
}

public class Jira : FileUploader, IOAuth
{
    private const string PathRequestToken = "/plugins/servlet/oauth/request-token";
    private const string PathAuthorize = "/plugins/servlet/oauth/authorize";
    private const string PathAccessToken = "/plugins/servlet/oauth/access-token";
    private const string PathApi = "/rest/api/2";
    private const string PathSearch = PathApi + "/search";
    private const string PathBrowseIssue = "/browse/{0}";
    private const string PathIssueAttachments = PathApi + "/issue/{0}/attachments";

    private static readonly X509Certificate2 jiraCertificate;

    public OAuthInfo AuthInfo { get; set; }

    private readonly string jiraBaseAddress;
    private readonly string jiraIssuePrefix;

    private Uri jiraRequestToken;
    private Uri jiraAuthorize;
    private Uri jiraAccessToken;
    private Uri jiraPathSearch;

    #region Keypair

    static Jira()
    {
        // Certificate generated using commands:
        // makecert -pe -n "CN=SnapX" -a sha1 -sky exchange -sp "Microsoft RSA SChannel Cryptographic Provider" -sy 12 -len 1024 -sv jira_sharex.pvk jira_sharex.cer
        // pvk2pfx -pvk jira_sharex.pvk -spc jira_sharex.cer -pfx jira_sharex.pfx
        // (Based on: http://nick-howard.blogspot.fr/2011/05/makecert-x509-certificates-and-rsa.html)

        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("SnapX.Core.jira_sharex.pfx");
        var pfx = new byte[stream.Length];
        stream.ReadExactly(pfx);
        jiraCertificate = X509CertificateLoader.LoadPkcs12(pfx, string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
    }

    internal static string PrivateKey
    {
        get
        {
            return jiraCertificate.GetRSAPrivateKey()?.ToXmlString(true);
        }
    }

    internal static string PublicKey
    {
        get
        {
            const int LineBreakIdx = 50;

            var publicKey = Convert.ToBase64String(ExportPublicKey(jiraCertificate.PublicKey));
            var sb = new StringBuilder();
            for (var i = 0; i < publicKey.Length; i++)
            {
                sb.Append(publicKey[i]);
                if ((i + 1) % LineBreakIdx == 0)
                    sb.AppendLine();
            }

            return $"-----BEGIN PUBLIC KEY-----{Environment.NewLine}{sb}{Environment.NewLine}-----END PUBLIC KEY-----";
        }
    }


    private static byte[] ExportPublicKey(PublicKey key)
    {
        // From: http://pstaev.blogspot.fr/2010/08/convert-rsa-public-key-from-xml-to-pem.html

        byte[] oid = [0x30, 0xD, 0x6, 0x9, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0xD, 0x1, 0x1, 0x1, 0x5, 0x0]; // Object ID for RSA

        //Transform the public key to PEM Base64 Format
        var binaryPublicKey = key.EncodedKeyValue.RawData.ToList();
        binaryPublicKey.Insert(0, 0x0); // Add NULL value

        CalculateAndAppendLength(ref binaryPublicKey);

        binaryPublicKey.Insert(0, 0x3);
        binaryPublicKey.InsertRange(0, oid);

        CalculateAndAppendLength(ref binaryPublicKey);

        binaryPublicKey.Insert(0, 0x30);
        return binaryPublicKey.ToArray();
    }

    private static void CalculateAndAppendLength(ref List<byte> binaryData)
    {
        var len = binaryData.Count;
        if (len <= byte.MaxValue)
        {
            binaryData.Insert(0, Convert.ToByte(len));
            binaryData.Insert(0, 0x81); //This byte means that the length fits in one byte
        }
        else
        {
            binaryData.Insert(0, Convert.ToByte(len % (byte.MaxValue + 1)));
            binaryData.Insert(0, Convert.ToByte(len / (byte.MaxValue + 1)));
            binaryData.Insert(0, 0x82); //This byte means that the length fits in two byte
        }
    }

    #endregion Keypair

    public Jira(string jiraBaseAddress, OAuthInfo oauth, string jiraIssuePrefix = null)
    {
        this.jiraBaseAddress = jiraBaseAddress;
        AuthInfo = oauth;
        this.jiraIssuePrefix = jiraIssuePrefix;

        InitUris();
    }

    public string? GetAuthorizationURL()
    {
        using (new SSLBypassHelper())
        {
            var args = new Dictionary<string, string?>
            {
                { OAuthManager.ParameterCallback, "oob" }
            };

            var url = OAuthManager.GenerateQuery(jiraRequestToken.ToString(), args, HttpMethod.Post, AuthInfo);
            var response = SendRequest(HttpMethod.Post, url);


            return !string.IsNullOrEmpty(response) ? OAuthManager.GetAuthorizationURL(response, AuthInfo, jiraAuthorize.ToString()) : null;
        }
    }


    public bool GetAccessToken(string? verificationCode)
    {
        using (new SSLBypassHelper())
        {
            AuthInfo.AuthVerifier = verificationCode;

            var nv = GetAccessTokenEx(jiraAccessToken.ToString(), AuthInfo, HttpMethod.Post);

            return nv != null;
        }
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        // TODO: Reimplement Jira
        throw new NotImplementedException("Jira Upload is not implemented");
        // using (new SSLBypassHelper())
        // {
        //     using (JiraUpload up = new JiraUpload(jiraIssuePrefix, GetSummary))
        //     {
        //         if (up.ShowDialog() == DialogResult.Cancel)
        //         {
        //             return new UploadResult
        //             {
        //                 IsSuccess = true,
        //                 IsURLExpected = false
        //             };
        //         }
        //
        //         Uri uri = Combine(jiraBaseAddress, string.Format(PathIssueAttachments, up.IssueId));
        //         string query = OAuthManager.GenerateQuery(uri.ToString(), null, HttpMethod.Post, AuthInfo);
        //
        //         NameValueCollection headers = new NameValueCollection();
        //         headers.Set("X-Atlassian-Token", "nocheck");
        //
        //         UploadResult res = SendRequestFile(query, stream, fileName, "file", headers: headers);
        //         if (res.Response.Contains("errorMessages"))
        //         {
        //             Errors.Add(res.Response);
        //         }
        //         else
        //         {
        //             res.IsURLExpected = true;
        //             var anonType = new[] { new { thumbnail = "" } };
        //             var anonObject = JsonConvert.DeserializeAnonymousType(res.Response, anonType);
        //             res.ThumbnailURL = anonObject[0].thumbnail;
        //             res.URL = Combine(jiraBaseAddress, string.Format(PathBrowseIssue, up.IssueId)).ToString();
        //         }
        //
        //         return res;
        //     }
        // }
    }

    private string GetSummary(string issueId)
    {
        using var bypasser = new SSLBypassHelper();

        var args = new Dictionary<string, string?>
        {
            { "jql", $"issueKey='{issueId}'" },
            { "maxResults", "10" },
            { "fields", "summary" }
        };

        var query = OAuthManager.GenerateQuery(jiraPathSearch.ToString(), args, HttpMethod.Get, AuthInfo);
        var response = SendRequest(HttpMethod.Get, query);

        if (string.IsNullOrEmpty(response)) return null;

        using var doc = JsonDocument.Parse(response);

        return doc.RootElement
            .GetProperty("issues")[0]
            .GetProperty("fields")
            .GetProperty("summary")
            .GetString();
    }


    private void InitUris()
    {
        jiraRequestToken = Combine(jiraBaseAddress, PathRequestToken);
        jiraAuthorize = Combine(jiraBaseAddress, PathAuthorize);
        jiraAccessToken = Combine(jiraBaseAddress, PathAccessToken);
        jiraPathSearch = Combine(jiraBaseAddress, PathSearch);
    }

    private Uri Combine(string path1, string path2)
    {
        return new Uri(path1.TrimEnd('/') + path2);
    }
}

