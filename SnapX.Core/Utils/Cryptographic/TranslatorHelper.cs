
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;
using System.Text.RegularExpressions;

namespace SnapX.Core.Utils.Cryptographic;

public static class TranslatorHelper
{

    public static string[] TextToBinary(string? text) =>
        text.Select(c => ByteToBinary((byte)c)).ToArray();
    public static string[] TextToHexadecimal(string? text) => BytesToHexadecimal(Encoding.UTF8.GetBytes(text));

    public static byte[] TextToASCII(string? text) => Encoding.ASCII.GetBytes(text);

    public static string? TextToBase64(string? text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    public static string? TextToHash(string? text, HashType hashType, bool uppercase = false)
    {
        using var hash = HashChecker.GetHashAlgorithm(hashType);
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(text));
        var hex = BytesToHexadecimal(bytes);
        var result = string.Concat(hex);
        if (uppercase) result = result.ToUpperInvariant();
        return result;
    }


    public static byte BinaryToByte(string binary) => Convert.ToByte(binary, 2);
    public static string BinaryToText(string binary)
    {
        binary = Regex.Replace(binary, @"[^01]", "");
        using var stream = new MemoryStream();
        foreach (var i in Enumerable.Range(0, binary.Length / 8))
        {
            stream.WriteByte(BinaryToByte(binary.Substring(i * 8, 8)));
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string ByteToBinary(byte b) => Convert.ToString(b, 2).PadLeft(8, '0');

    public static string[] BytesToHexadecimal(byte[] bytes) =>
        bytes.Select(b => b.ToString("x2")).ToArray();

    public static byte HexadecimalToByte(string hex) => Convert.ToByte(hex, 16);

    public static string HexadecimalToText(string hex)
    {
        hex = Regex.Replace(hex, @"[^0-9a-fA-F]", "");
        var byteCount = hex.Length / 2;
        var buffer = new byte[byteCount];

        foreach (var i in Enumerable.Range(0, byteCount))
        {
            buffer[i] = HexadecimalToByte(hex.Substring(i * 2, 2));
        }

        return Encoding.UTF8.GetString(buffer);
    }

    public static string Base64ToText(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    public static string ASCIIToText(string ascii)
    {
        var bytes = ascii
            .Split([' '], StringSplitOptions.RemoveEmptyEntries)
            .Where(s => byte.TryParse(s, out _))
            .Select(s => byte.Parse(s))
            .ToArray();

        return Encoding.ASCII.GetString(bytes);
    }

}
