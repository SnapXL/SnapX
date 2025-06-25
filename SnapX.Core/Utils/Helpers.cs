
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using SnapX.Core.Utils.Random;

namespace SnapX.Core.Utils;

public static class Helpers
{
    public const string Numbers = "0123456789"; // 48 ... 57
    public const string AlphabetCapital = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"; // 65 ... 90
    public const string Alphabet = "abcdefghijklmnopqrstuvwxyz"; // 97 ... 122
    public const string Alphanumeric = Numbers + AlphabetCapital + Alphabet;
    public const string AlphanumericInverse = Numbers + Alphabet + AlphabetCapital;
    public const string Hexadecimal = Numbers + "ABCDEF";
    public const string Base58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"; // https://en.wikipedia.org/wiki/Base58
    public const string Base56 = "23456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz"; // A variant, Base56, excludes 1 (one) and o (lowercase o) compared to Base 58.

    public static readonly Version OSVersion = Environment.OSVersion.Version;
    public static string AddZeroes(string input, int digits = 2)
    {
        return input.PadLeft(digits, '0');
    }

    public static string AddZeroes(int number, int digits = 2)
    {
        return AddZeroes(number.ToString(), digits);
    }

    public static string HourTo12(int hour)
    {
        if (hour == 0)
        {
            return 12.ToString();
        }

        if (hour > 12)
        {
            return AddZeroes(hour - 12);
        }

        return AddZeroes(hour);
    }
    public static string GetImageExtension(Image image)
    {
        return image.Metadata.DecodedImageFormat switch
        {
            JpegFormat => ".jpg",
            PngFormat => ".png",
            GifFormat => ".gif",
            WebpFormat => ".webp",
            TiffFormat => ".tiff",
            BmpFormat _ => ".bmp",
            _ => ".png"
        };
    }

    public static string StripPII(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var currentUser = Environment.UserName;
        var hostName = Environment.MachineName;
        var usernamePattern = Regex.Escape(currentUser);
        var hostnamePattern = Regex.Escape(hostName);
        var emailRegex = new Regex(
            @"^(?!\.)(""[^""\r\\]*(\\.[^""\r\\]*)*""|""[^""\\]*(\\.[^""\\]*)*"")@(?=\S)(?!-)(?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,63}$",
            RegexOptions.IgnoreCase);
        var publicIPRegex = new Regex(@"^(?!10(\.|\b))(?!(172\.(1[6-9]|2[0-9]|3[01])(\.|\b)))(?!(192\.168(\.|\b)))(?!0(\.|\b))(?!(255(\.|\b)))(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
        var publicIPv6Regex = new Regex(@"^(?!fe80(:|::))(?!fc00(:|::))(?!ff00(:|::))(?!::1$)(?!2001:db8::)(?!::ffff:.*)([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$", RegexOptions.IgnoreCase);
        var credentialsRegex = new Regex(@"(?i)(Authorization|Client\-Id|Client\-Token|Api\-Key)\s*:\s*([A-Za-z0-9\-_.]+)", RegexOptions.IgnoreCase);

        input = Regex.Replace(input, usernamePattern, "anonymous", RegexOptions.IgnoreCase);
        input = Regex.Replace(input, hostnamePattern, "[REDACTED HOSTNAME]", RegexOptions.IgnoreCase);
        input = credentialsRegex.Replace(input, "[REDACTED CREDENTIALS]");
        input = emailRegex.Replace(input, "[REDACTED EMAIL]");
        input = publicIPRegex.Replace(input, "[REDACTED IPv4]");
        input = publicIPv6Regex.Replace(input, "[REDACTED IPv6]");
        return input;
    }

    public static char GetRandomChar(string chars)
    {
        return chars[RandomCrypto.Next(chars.Length - 1)];
    }

    public static string? GitHubIssueReport(Exception ex)
    {
        try
        {
            // Format the title and body for the issue
            var title = StripPII(ex.Message);
            var body = StripPII($"### Exception Details:\n\n{ex.Message}\n\n### Stack Trace:\n\n```\n{ex.StackTrace}\n```\n\nInner Exception: {ex.InnerException?.Message}\n\n## Stack Trace:\n\n```\n{ex.InnerException?.StackTrace}\n```\n\nAssembly: {Assembly.GetEntryAssembly()}\n\nOperating System: {OsInfo.GetFancyOSNameAndVersion()}");

            var encodedTitle = HttpUtility.UrlEncode(title);
            var encodedBody = HttpUtility.UrlEncode(body);
            var labels = new[] { "bug", "high severity" };
            var encodedLabels = HttpUtility.UrlEncode(string.Join(",", labels));

            // Construct the GitHub issue creation URL
            var issueUrl = $"{Links.GitHub}/issues/new?title={encodedTitle}&body={encodedBody}&labels={encodedLabels}";

            // Return the generated URL
            return issueUrl;
        }
        catch (Exception e)
        {
            DebugHelper.Logger.Error($"Error generating GitHub issue URL: {e.Message}");
            return null;
        }
    }

    public static string? GetRandomString(string chars, int length)
    {
        var sb = new StringBuilder();

        while (length-- > 0)
        {
            sb.Append(GetRandomChar(chars));
        }

        return sb.ToString();
    }

    public static string? GetRandomNumber(int length)
    {
        return GetRandomString(Numbers, length);
    }

    public static string? GetRandomAlphanumeric(int length)
    {
        return GetRandomString(Alphanumeric, length);
    }

    public static string GetRandomKey(int length = 5, int count = 3, char separator = '-')
    {
        return Enumerable.Range(1, ((length + 1) * count) - 1).Aggregate("", (x, index) => x += index % (length + 1) == 0 ? separator : GetRandomChar(Alphanumeric));
    }

    public static string GetAllCharacters()
    {
        return Encoding.UTF8.GetString(Enumerable.Range(1, 255).Select(i => (byte)i).ToArray());
    }

    public static string? GetRandomLine(string text)
    {
        var lines = text.Trim().Lines();

        if (lines == null || lines.Length == 0)
        {
            return null;
        }

        return RandomCrypto.Pick(lines);
    }

    public static string? GetRandomLineFromFile(string? filePath)
    {
        var text = File.ReadAllText(filePath, Encoding.UTF8);
        return GetRandomLine(text);
    }

    public static string? GetValidURL(string? url, bool replaceSpace = false)
    {
        if (replaceSpace) url = url.Replace(' ', '_');
        return HttpUtility.UrlPathEncode(url);
    }

    public static string GetXMLValue(string input, string tag)
    {
        return Regex.Match(input, string.Format("(?<={0}>).+?(?=</{0})", tag)).Value;
    }

    [RequiresDynamicCode("Uploader")]
    public static T[] GetEnums<T>()
    {
        return (T[])Enum.GetValues(typeof(T));
    }

    [RequiresDynamicCode("Uploader")]
    public static string[] GetEnumDescriptions<T>(int skip = 0)
    {
        return Enum.GetValues(typeof(T)).OfType<Enum>().Skip(skip).Select(x => x.GetDescription()).ToArray();
    }

    public static T GetEnumFromIndex<T>(int i)
    {
        return GetEnums<T>()[i];
    }

    // Example: "TopLeft" becomes "Top left"
    // Example2: "Rotate180" becomes "Rotate 180"
    public static string GetProperName(string name, bool keepCase = false)
    {
        var sb = new StringBuilder();
        var number = false;

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (i > 0 && (char.IsUpper(c) || (!number && char.IsNumber(c))))
                sb.Append(' ');

            sb.Append(keepCase || i == 0 || !char.IsUpper(c) ? c : char.ToLowerInvariant(c));

            number = char.IsNumber(c);
        }

        return sb.ToString();
    }


    public static string GetApplicationVersion()
    {
        var version = Assembly.GetEntryAssembly()!.GetName().Version ?? new Version(0, 0, 0);
        return version.ToString();
    }

    public static string GetFileExtensionFromBytes(ReadOnlySpan<byte> rawData)
    {
        return rawData.Length switch
        {
            >= 8 when rawData[0] == 0x89 && rawData[1] == 0x50 && rawData[2] == 0x4E && rawData[3] == 0x47 => ".png",
            >= 2 when rawData[0] == 0xFF && rawData[1] == 0xD8 => ".jpg",
            _ => ".png"
        };
    }

    /// <summary>
    /// If version1 newer than version2 = 1
    /// If version1 equal to version2 = 0
    /// If version1 older than version2 = -1
    /// </summary>
    public static int CompareVersion(string version1, string version2, bool ignoreRevision = false)
    {
        return NormalizeVersion(version1, ignoreRevision).CompareTo(NormalizeVersion(version2, ignoreRevision));
    }

    /// <summary>
    /// If version1 newer than version2 = 1
    /// If version1 equal to version2 = 0
    /// If version1 older than version2 = -1
    /// </summary>
    public static int CompareVersion(Version version1, Version version2, bool ignoreRevision = false)
    {
        return version1.Normalize(ignoreRevision).CompareTo(version2.Normalize(ignoreRevision));
    }

    /// <summary>
    /// If version newer than ApplicationVersion = 1
    /// If version equal to ApplicationVersion = 0
    /// If version older than ApplicationVersion = -1
    /// </summary>
    public static int CompareApplicationVersion(string version)
    {
        return CompareVersion(version, GetApplicationVersion());
    }

    public static Version NormalizeVersion(string version, bool ignoreRevision = false)
    {
        return Version.Parse(version).Normalize(ignoreRevision);
    }

    public static bool IsWindows11OrGreater(int build = -1)
    {
        build = System.Math.Max(22000, build);
        return OSVersion.Major >= 10 && OSVersion.Build >= build;
    }
    public static string ProperTimeSpan(TimeSpan ts)
    {
        var time = $"{ts.Minutes:00}:{ts.Seconds:00}";
        var hours = (int)ts.TotalHours;
        if (hours > 0) time = hours + ":" + time;
        return time;
    }

    public static bool WaitWhile(Func<bool> check, int interval, int timeout = -1)
    {
        var timer = Stopwatch.StartNew();

        while (check())
        {
            if (timeout >= 0 && timer.ElapsedMilliseconds >= timeout)
            {
                return false;
            }

            Thread.Sleep(interval);
        }

        return true;
    }

    public static async Task WaitWhileAsync(Func<bool> check, int interval, int timeout, Action onSuccess, int waitStart = 0)
    {
        var result = false;

        await Task.Run(() =>
        {
            if (waitStart > 0)
            {
                Thread.Sleep(waitStart);
            }

            result = WaitWhile(check, interval, timeout);
        });

        if (result) onSuccess();
    }

    public static string GetUniqueID()
    {
        return Guid.CreateVersion7().ToString("N");
    }
    public static async Task<string> SendPing(string host)
    {
        return await SendPingAsync(host, 1);
    }

    public static async Task<string> SendPingAsync(string host, int count)
    {
        var status = new StringBuilder();

        using (var ping = new Ping())
        {
            for (int i = 0; i < count; i++)
            {
                PingReply reply = await ping.SendPingAsync(host, 3000);

                if (reply.Status == IPStatus.Success)
                {
                    status.Append(reply.RoundtripTime).Append(" ms");
                }
                else
                {
                    status.Append("Timeout");
                }

                if (i < count - 1)
                {
                    status.Append(", ");
                }

                // Add delay asynchronously instead of blocking the thread
                await Task.Delay(100);
            }
        }

        return status.ToString();
    }

    public static bool IsAdministrator()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        return GetCurrentUid() == 0;
    }

    private static int GetCurrentUid()
    {
        var uidStr = Environment.GetEnvironmentVariable("UID");
        if (!string.IsNullOrEmpty(uidStr) && int.TryParse(uidStr, out var uid)) return uid;

        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "id",
                Arguments = "-u",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null) return 1000;

            process.WaitForExit();
            if (process.ExitCode != 0) return 1000;

            using var reader = process.StandardOutput;
            var output = reader.ReadToEnd();
            if (int.TryParse(output.Trim(), out uid)) return uid;

            return 1000;
        }
        catch (Exception) { return 1000; }
    }

    public static bool IsRunning(string name)
    {
        try
        {
            var mutex = Mutex.OpenExisting(name);
            mutex.ReleaseMutex();
        }
        catch
        {
            return false;
        }

        return true;
    }

    [RequiresUnreferencedCode("Uploader")]
    public static IEnumerable<T> GetInstances<T>() where T : class
    {
        var baseType = typeof(T);
        var assembly = baseType.Assembly;
        if (assembly == null) throw new InvalidOperationException();
        var types = assembly.GetTypes();
        if (types == null) throw new InvalidOperationException();

        return types
            .Where(t => t.IsClass && t.IsSubclassOf(baseType) && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => Activator.CreateInstance(t) as T)
            .Where(instance => instance != null);
    }

    [RequiresUnreferencedCode("Uploader")]
    public static IEnumerable<Type> FindSubclassesOf<T>()
    {
        var baseType = typeof(T);
        var assembly = baseType.Assembly;
        return assembly.GetTypes().Where(t => t.IsSubclassOf(baseType));
    }

    public static string GetOperatingSystemProductName(bool includeArch = false)
    {
        var productName = OsInfo.GetFancyOSNameAndVersion();
        if (!includeArch) return productName;
        var architecture = RuntimeInformation.OSArchitecture.ToString();
        productName = $"{productName} ({architecture})";

        return productName;
    }


    public static string EscapeCLIText(string text)
    {
        var escapedText = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\"{escapedText}\"";
    }

    public static string BytesToHex(byte[] bytes)
    {
        var sb = new StringBuilder();
        foreach (var x in bytes)
        {
            sb.Append($"{x:x2}");
        }
        return sb.ToString();
    }

    public static byte[] ComputeSHA256(byte[] data)
    {
        return SHA256.HashData(data);
    }

    public static byte[] ComputeSHA256(Stream stream, int bufferSize = 1024 * 32)
    {
        var bufferedStream = new BufferedStream(stream, bufferSize);

        return SHA256.HashData(bufferedStream);

    }

    public static byte[] ComputeSHA256(string data)
    {
        return ComputeSHA256(Encoding.UTF8.GetBytes(data));
    }

    public static byte[] ComputeHMACSHA256(byte[] data, byte[] key)
    {
        return HMACSHA256.HashData(key, data);

    }

    public static byte[] ComputeHMACSHA256(string? data, string key)
    {
        return ComputeHMACSHA256(Encoding.UTF8.GetBytes(data), Encoding.UTF8.GetBytes(key));
    }

    public static byte[] ComputeHMACSHA256(byte[] data, string key)
    {
        return ComputeHMACSHA256(data, Encoding.UTF8.GetBytes(key));
    }

    public static byte[] ComputeHMACSHA256(string data, byte[] key)
    {
        return ComputeHMACSHA256(Encoding.UTF8.GetBytes(data), key);
    }

    public static string SafeStringFormat(string format, params object[] args)
    {
        return SafeStringFormat(null, format, args);
    }

    public static string SafeStringFormat(IFormatProvider provider, string format, params object[] args)
    {
        try
        {
            if (provider != null)
            {
                return string.Format(provider, format, args);
            }

            return string.Format(format, args);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return format;
    }

    public static string NumberToLetters(int num)
    {
        var result = "";
        while (--num >= 0)
        {
            result = (char)('A' + (num % 26)) + result;
            num /= 26;
        }
        return result;
    }

    private static string GetNextRomanNumeralStep(ref int num, int step, string numeral)
    {
        if (num < step) return "";

        var result = numeral.Repeat(num / step);
        num %= step;
        return result;
    }

    public static string NumberToRomanNumeral(int num)
    {
        var result = "";
        result += GetNextRomanNumeralStep(ref num, 1000, "M");
        result += GetNextRomanNumeralStep(ref num, 900, "CM");
        result += GetNextRomanNumeralStep(ref num, 500, "D");
        result += GetNextRomanNumeralStep(ref num, 400, "CD");
        result += GetNextRomanNumeralStep(ref num, 100, "C");
        result += GetNextRomanNumeralStep(ref num, 90, "XC");
        result += GetNextRomanNumeralStep(ref num, 50, "L");
        result += GetNextRomanNumeralStep(ref num, 40, "XL");
        result += GetNextRomanNumeralStep(ref num, 10, "X");
        result += GetNextRomanNumeralStep(ref num, 9, "IX");
        result += GetNextRomanNumeralStep(ref num, 5, "V");
        result += GetNextRomanNumeralStep(ref num, 4, "IV");
        result += GetNextRomanNumeralStep(ref num, 1, "I");
        return result;
    }
    public static string? RepeatGenerator(int count, Func<string> generator)
    {
        return string.Concat(Enumerable.Repeat(0, count).Select(_ => generator()));
    }

    [RequiresUnreferencedCode("Uploader")]
    public static string JSONFormat(string json, JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions { WriteIndented = true, AllowTrailingCommas = true, AllowOutOfOrderMetadataProperties = true };
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement, options);
    }

    public static string XMLFormat(string xml)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);

        using var ms = new MemoryStream();
        using var writer = new XmlTextWriter(ms, Encoding.Unicode) { Formatting = Formatting.Indented };
        document.Save(writer);

        return Encoding.Unicode.GetString(ms.ToArray());
    }

    public static string GetChecksum(string filePath) => GetChecksum(filePath, SHA256.Create());


    public static string GetChecksum(string filePath, HashAlgorithm hashAlgorithm)
    {
        using var fs = System.IO.File.OpenRead(filePath);
        var hash = hashAlgorithm.ComputeHash(fs);
        return Convert.ToHexString(hash);
    }

    public static string CreateChecksumFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            return "/dev/null";

        var checksum = GetChecksum(filePath);
        var fileName = Path.GetFileName(filePath);
        var content = $"{checksum}  {fileName}";

        var outputFilePath = $"{filePath}.sha256";
        System.IO.File.WriteAllText(outputFilePath, content);

        return outputFilePath;
    }

    public static Task ForEachAsync<T>(IEnumerable<T> inputEnumerable, Func<T, Task> asyncProcessor, int maxDegreeOfParallelism)
    {
        var throttler = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);

        IEnumerable<Task> tasks = inputEnumerable.Select(async input =>
        {
            await throttler.WaitAsync();

            try
            {
                await asyncProcessor(input);
            }
            finally
            {
                throttler.Release();
            }
        });

        return Task.WhenAll(tasks);
    }

    public static bool IsDefaultSettings<T>(IEnumerable<T> current, IEnumerable<T> source, Func<T, T, bool> predicate)
    {
        if (current != null && current.Any())
        {
            return current.All(x => source.Any(y => predicate(x, y)));
        }

        return true;
    }

    public static IEnumerable<int> Range(int from, int to, int increment = 1)
    {
        if (increment == 0)
        {
            throw new ArgumentException("Increment cannot be zero.", nameof(increment));
        }

        if (from == to)
        {
            yield return from;
            yield break;
        }

        increment = System.Math.Abs(increment);

        if (from < to)
        {
            for (var i = from; i <= to; i += increment)
            {
                yield return i;
            }
        }
        else
        {
            for (var i = from; i >= to; i -= increment)
            {
                yield return i;
            }
        }
    }
}

