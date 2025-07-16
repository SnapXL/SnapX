// SPDX-License-Identifier: GPL-3.0-or-later


namespace SnapX.Core.Utils.Cryptographic;

public class Translator
{
    // http://en.wikipedia.org/wiki/UTF-8
    public string? Text { get; private set; }

    // http://en.wikipedia.org/wiki/Binary_numeral_system
    public string[] Binary { get; private set; }

    public string? BinaryText =>
        Binary == null || Binary.Length == 0 ? null :
            string.Join(" ", Binary);

    // http://en.wikipedia.org/wiki/Hexadecimal
    public string[] Hexadecimal { get; private set; }

    public string? HexadecimalText =>
        Hexadecimal == null || Hexadecimal.Length == 0 ? null :
            string.Join(string.Empty, Hexadecimal).ToUpperInvariant();

    // http://en.wikipedia.org/wiki/ASCII
    public byte[] ASCII { get; private set; }

    public string ASCIIText => ASCII?.Length > 0 ? string.Join(string.Empty, ASCII) : null;


    // http://en.wikipedia.org/wiki/Base64
    public string? Base64 { get; private set; }

    // https://en.wikipedia.org/wiki/Cyclic_redundancy_check
    public string? CRC32 { get; private set; }

    // http://en.wikipedia.org/wiki/MD5
    public string? MD5 { get; private set; }

    // http://en.wikipedia.org/wiki/SHA-1
    public string? SHA1 { get; private set; }

    // http://en.wikipedia.org/wiki/SHA-2
    public string? SHA256 { get; private set; }
    public string? SHA384 { get; private set; }
    public string? SHA512 { get; private set; }

    public void Clear()
    {
        Text = Base64 = CRC32 = MD5 = SHA1 = SHA256 = SHA384 = SHA512 = null;
        Binary = null;
        Hexadecimal = null;
        ASCII = null;
    }

    public void EncodeText(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;

        Clear();

        Text = text;
        Binary = TranslatorHelper.TextToBinary(text);
        Hexadecimal = TranslatorHelper.TextToHexadecimal(text);
        ASCII = TranslatorHelper.TextToASCII(text);
        Base64 = TranslatorHelper.TextToBase64(text);

        var hashTypes = new[] { HashType.CRC32, HashType.MD5, HashType.SHA1, HashType.SHA256, HashType.SHA384, HashType.SHA512 };

        foreach (var hashType in hashTypes)
        {
            var hash = TranslatorHelper.TextToHash(text, hashType, true);
            SetHash(hashType, hash);
        }
    }


    private void SetHash(HashType hashType, string? hash)
    {
        switch (hashType)
        {
            case HashType.CRC32:
                CRC32 = hash;
                break;
            case HashType.MD5:
                MD5 = hash;
                break;
            case HashType.SHA1:
                SHA1 = hash;
                break;
            case HashType.SHA256:
                SHA256 = hash;
                break;
            case HashType.SHA384:
                SHA384 = hash;
                break;
            case HashType.SHA512:
                SHA512 = hash;
                break;
            default:
                throw new NotImplementedException("Unknown hash type: " + hashType);
        }
    }

    public bool DecodeBinary(string binary) => !string.IsNullOrEmpty(TranslatorHelper.HexadecimalToText(binary));

    public bool DecodeHex(string hex) => !string.IsNullOrEmpty(TranslatorHelper.HexadecimalToText(hex));

    public bool DecodeASCII(string ascii) => !string.IsNullOrEmpty(TranslatorHelper.ASCIIToText(ascii));

    public bool DecodeBase64(string base64) => !string.IsNullOrEmpty(TranslatorHelper.Base64ToText(base64));

    public string HashToString() => string.Join(Environment.NewLine,
        $"CRC-32: {CRC32}",
        $"MD5: {MD5}",
        $"SHA-1: {SHA1}",
        $"SHA-256: {SHA256}",
        $"SHA-384: {SHA384}",
        $"SHA-512: {SHA512}");

    public override string ToString()
    {
        return string.Join(Environment.NewLine,
            $"Text: {Text}",
            $"Binary: {BinaryText}",
            $"Hexadecimal: {HexadecimalText}",
            $"ASCII: {ASCIIText}",
            $"Base64: {Base64}",
            HashToString());
    }

}

