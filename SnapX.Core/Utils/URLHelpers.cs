// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Utils;

public static class URLHelpers
{
    public const string URLCharacters = Helpers.Alphanumeric + "-._~"; // 45 46 95 126
    public const string URLPathCharacters = URLCharacters + "/"; // 47
    public const string ValidURLCharacters = URLPathCharacters + ":?#[]@!$&'()*+,;= ";

    private static readonly string[] URLPrefixes = ["http://", "https://", "ftp://", "ftps://", "file://", "//", "\\\\"];
    private static readonly char[] BidiControlCharacters = ['\u200E', '\u200F', '\u202A', '\u202B', '\u202C', '\u202D', '\u202E'];

    public static void OpenURL(string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        Task.Run(() =>
        {
            try
            {
                using var process = new Process();
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = true,
                };
                if (!string.IsNullOrEmpty(HelpersOptions.BrowserPath))
                {
                    psi.FileName = HelpersOptions.BrowserPath;
                    psi.Arguments = url;
                }
                else
                {
                    psi.FileName = url;
                }

                process.StartInfo = psi;
                process.Start();

                DebugHelper.WriteLine("URL opened: " + url);
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e, $"OpenURL({url}) failed");
            }
        });
    }

    public static string? URLEncode(string? text, bool isPath = false, bool ignoreEmoji = false)
    {
        if (ignoreEmoji)
        {
            return URLEncodeIgnoreEmoji(text, isPath);
        }

        var sb = new StringBuilder();
        if (string.IsNullOrEmpty(text)) return sb.ToString();


        var unreservedCharacters = isPath ? URLPathCharacters : URLCharacters;

        foreach (char c in Encoding.UTF8.GetBytes(text))
        {
            if (unreservedCharacters.Contains(c))
            {
                sb.Append(c);
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "%{0:X2}", (int)c);
            }
        }

        return sb.ToString();
    }

    public static string? URLEncodeIgnoreEmoji(string? text, bool isPath = false)
    {
        var sb = new StringBuilder();

        for (var i = 0; i < text.Length; i++)
        {
            var remainingText = text[i..];

            sb.Append(URLEncode(remainingText[0..1], isPath));
        }

        return sb.ToString();
    }

    public static string? RemoveBidiControlCharacters(string? text)
    {
        return new string(text.Where(c => !BidiControlCharacters.Contains(c)).ToArray());
    }

    public static string? ReplaceReservedCharacters(string? text, string replace)
    {
        var sb = new StringBuilder();

        string last = null;

        foreach (var c in text)
        {
            if (URLCharacters.Contains(c))
            {
                last = c.ToString();
            }
            else if (last != replace)
            {
                last = replace;
            }
            else
            {
                continue;
            }

            sb.Append(last);
        }

        return sb.ToString();
    }

    public static string HtmlEncode(string text)
    {
        var chars = HttpUtility.HtmlEncode(text).ToCharArray();
        var result = new StringBuilder(chars.Length + (int)(chars.Length * 0.1));

        foreach (var c in chars)
        {
            var value = Convert.ToInt32(c);

            if (value > 127)
            {
                result.AppendFormat("&#{0};", value);
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public static string? JSONEncode(string? text)
    {
        text = JsonSerializer.Serialize(text);

        // Remove the surrounding quotes added during serialization
        return text[1..^1];
    }

    public static string? XMLEncode(string? text)
    {
        return SecurityElement.Escape(text);
    }

    public static string? URLDecode(string? url, int count = 1)
    {
        string? temp = null;

        for (var i = 0; i < count && url != temp; i++)
        {
            temp = url;
            url = HttpUtility.UrlDecode(url);
        }

        return url;
    }

    public static string? CombineURL(string? url1, string? url2)
    {
        if (string.IsNullOrEmpty(url1)) return url2 ?? "";
        if (string.IsNullOrEmpty(url2)) return url1;

        url1 = url1.TrimEnd('/');
        url2 = url2.TrimStart('/');

        return $"{url1}/{url2}";
    }

    public static string? CombineURL(params string?[] urls) => urls.Aggregate(CombineURL);
    public static bool IsValidURL(string? url, bool useRegex = true)
    {
        if (string.IsNullOrEmpty(url)) return false;

        url = url.Trim();

        if (useRegex)
        {
            const string pattern = @"^
            (?:(?:https?|ftp)://)                             # protocol identifier
            (?:\S+(?::\S*)?@)?                                # user:pass authentication
            (?:(?!(?:10|127)(?:\.\d{1,3}){3})                 # IP address exclusion
               (?!(?:169\.254|192\.168)(?:\.\d{1,3}){2})
               (?!172\.(?:1[6-9]|2\d|3[0-1])(?:\.\d{1,3}){2})
               (?:[1-9]\d?|1\d\d|2[01]\d|22[0-3])            # valid IP address range
               (?:\.(?:1?\d{1,2}|2[0-4]\d|25[0-5])){2}
               (?:\.(?:[1-9]\d?|1\d\d|2[0-4]\d|25[0-4]))
            |                                                 # OR host name
            (?:(?:[a-z\u00a1-\uffff0-9]-*)*[a-z\u00a1-\uffff0-9]+)
            (?:\.(?:[a-z\u00a1-\uffff0-9]-*)*[a-z\u00a1-\uffff0-9]+)* # domain name
            (?:\.(?:[a-z\u00a1-\uffff]{2,}))                    # TLD
            \.?
            )
            (?::\d{2,5})?                                     # optional port number
            (?:[/?#]\S*)?                                     # optional resource path
            $";

            return Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        }

        return !url.StartsWith("file://") && Uri.IsWellFormedUriString(url, UriKind.Absolute);
    }

    public static string? AddSlash(string? url, SlashType slashType) => AddSlash(url, slashType, 1);

    public static string? AddSlash(string? url, SlashType slashType, int count)
    {
        if (string.IsNullOrEmpty(url))
        {
            return slashType == SlashType.Prefix ? new string('/', count) : url;
        }

        return slashType switch
        {
            SlashType.Prefix => $"{new string('/', count)}{url.TrimStart('/')}",
            SlashType.Suffix => $"{url.TrimEnd('/')}{new string('/', count)}",
            _ => throw new ArgumentException("Invalid slash type.", nameof(slashType))
        };
    }

    public static string? GetFileName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var fileName = Path.GetFileName(path);
        var cleanFileName = fileName.Split(['?', '#'])[0];

        return cleanFileName;
    }

    public static bool IsFileURL(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        var path = url.Substring(url.LastIndexOf('/') + 1);

        return !string.IsNullOrEmpty(path) && path.Contains('.');
    }

    public static string? GetDirectoryPath(string? path)
    {
        return path.Contains("/") ? path.Substring(0, path.LastIndexOf('/')) : path;
    }

    public static List<string?> GetPaths(string? path)
    {
        return path.Split('/')
            .Where(p => !string.IsNullOrEmpty(p))
            .Aggregate(new List<string>(), (list, part) =>
            {
                list.Add(part);
                return list;
            });
    }

    public static bool HasPrefix(string? url)
    {
        return URLPrefixes.Any(x => url.StartsWith(x, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetPrefix(string? url)
    {
        return URLPrefixes.FirstOrDefault(x => url.StartsWith(x, StringComparison.OrdinalIgnoreCase));
    }

    public static string? FixPrefix(string? url, string prefix = "https://")
    {
        if (!string.IsNullOrEmpty(url) && !HasPrefix(url))
        {
            return prefix + url;
        }

        return url;
    }

    public static string? ForcePrefix(string? url, string prefix = "https://")
    {
        if (!string.IsNullOrEmpty(url))
        {
            url = prefix + RemovePrefixes(url);
        }

        return url;
    }

    public static string? RemovePrefixes(string? url)
    {
        foreach (var prefix in URLPrefixes)
        {
            if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            url = url.Remove(0, prefix.Length);
            break;
        }

        return url;
    }

    public static string? GetHostName(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
        {
            return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? uri.Host[4..]
                : uri.Host;
        }

        return url;
    }

    public static string? CreateQueryString(Dictionary<string, string?> args, bool customEncoding = false)
    {
        if (args == null || args.Count == 0)
        {
            return string.Empty;
        }

        var pairs = new List<string>();

        foreach (var arg in args)
        {
            string pair;
            if (string.IsNullOrEmpty(arg.Value))
            {
                pair = arg.Key;
            }
            else
            {
                var value = customEncoding ? URLEncode(arg.Value) : HttpUtility.UrlEncode(arg.Value);
                pair = $"{arg.Key}={value}";
            }
            pairs.Add(pair);
        }

        return string.Join("&", pairs);
    }

    public static string? CreateQueryString(string? url, Dictionary<string, string?> args, bool customEncoding = false)
    {
        var query = CreateQueryString(args, customEncoding);

        if (string.IsNullOrEmpty(query)) return url;

        return url.Contains("?") ? $"{url}&{query}" : $"{url}?{query}";
    }

    public static string? RemoveQueryString(string? url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        int index = url.IndexOf("?");
        return index > -1 ? url.Remove(index) : url;
    }


    public static NameValueCollection ParseQueryString(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var index = url.IndexOf("?");
        return index > -1 && index + 1 < url.Length
            ? HttpUtility.ParseQueryString(url.Substring(index + 1))
            : null;
    }

    public static string? BuildUri(string root, string path, string query = null)
    {
        var builder = new UriBuilder(root) { Path = path, Query = query };
        return builder.Uri.AbsoluteUri;
    }
}
