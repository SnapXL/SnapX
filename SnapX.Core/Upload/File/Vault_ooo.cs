// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;
public class Vault_oooFileUploaderService : FileUploaderService
{
    public override FileDestination EnumValue => FileDestination.Vault_ooo;

    public override bool CheckConfig(UploadersConfig config)
    {
        return true;
    }

    public override GenericUploader CreateUploader(UploadersConfig config, TaskReferenceHelper taskInfo)
    {
        return new Vault_ooo();
    }
}
[JsonSerializable(typeof(Vault_ooo.Vault_oooMetaInfo))]
internal partial class Vault_oooContext : JsonSerializerContext;
public sealed class Vault_ooo : FileUploader
{
    private const string? APIURL = "https://vault.ooo";
    private const int PBKDF2_ITERATIONS = 10000;
    private const int AES_KEY_SIZE = 256; // Bits
    private const int AES_BLOCK_SIZE = 128; // Bits
    private const int BYTE_CHUNK_SIZE = 256 * 1024 * 381; // Copied from web client (99 MB)
    private static readonly DateTime ORIGIN_TIME = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public override UploadResult Upload(Stream stream, string? fileName)
    {
        #region Calculating sizes

        var fileSize = stream.Length;
        var chunks = (int)System.Math.Ceiling((double)fileSize / BYTE_CHUNK_SIZE);
        long fullUploadSize = 16; // 16 bytes header

        var uploadSizes = new List<long> { 0 };

        LoopStartEnd((chunkStart, chunkEnd, i) =>
        {
            var chunkLength = chunkEnd - chunkStart;
            fullUploadSize += chunkLength + 16 - (chunkLength % 16); // Adding chunk size and adjusting for padding
            uploadSizes.Add(fullUploadSize);
        }, chunks, fileSize);

        #endregion

        var randomKey = GenerateRandomKey();
        var randomKeyBytes = Encoding.UTF8.GetBytes(randomKey);
        var cryptoData = DeriveCryptoData(randomKeyBytes);

        #region Building filename

        var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
        string encryptedFileName;

        // Encrypt the file name with the derived key and salt
        using (var ms = new MemoryStream())
        {
            ms.Write(cryptoData.Salt, 0, cryptoData.Salt.Length);
            var encryptedFn = EncryptBytes(cryptoData, fileNameBytes);
            ms.Write(encryptedFn, 0, encryptedFn.Length);
            encryptedFileName = Helpers.BytesToHex(ms.ToArray());
        }

        // Convert sizes and expiry time to hex format for filename
        var bytesLengthHex = fullUploadSize.ToString("X4"); // To Hex
        var expiryTime = DateTime.UtcNow.AddDays(30); // Defaults from the web client
        var expiryTimeHex = ((long)(expiryTime - ORIGIN_TIME).TotalSeconds).ToString("X4"); // Expiry date in UNIX seconds in hex

        var fullFileName = $"{expiryTimeHex}-b-{bytesLengthHex}-{encryptedFileName}".ToLower();

        #endregion

        // Prepare for the initial POST request
        var requestHeaders = new Dictionary<string, string?>
        {
        { "X-Get-Raw-File", "1" }
    };

        var postRequestJson = new Dictionary<string, long>
    {
        { "chunks", chunks },
        { "fileLength", fullUploadSize }
    };

        var postResult = SendRequest(HttpMethod.Post, URLHelpers.CombineURL(APIURL, fullFileName), JsonSerializer.Serialize(postRequestJson), RequestHelpers.ContentTypeJSON, requestHeaders);
        var metaInfo = JsonSerializer.Deserialize<Vault_oooMetaInfo>(postResult);

        if (string.IsNullOrEmpty(metaInfo.UrlPathName))
            throw new InvalidOperationException("No correct metaInfo returned");

        #region Upload in chunks

        var dumpStash = new List<byte>();
        LoopStartEnd((chunkStart, chunkEnd, i) =>
        {
            var chunkLength = chunkEnd - chunkStart;
            var plainBytes = new byte[chunkLength];
            stream.ReadExactly(plainBytes, 0, chunkLength);

            var encryptedBytes = EncryptBytes(cryptoData, plainBytes);
            var prependSize = 0;

            // Prepend previously cached chunk part to the current chunk if any
            if (dumpStash.Count > 0)
            {
                using (var ms = new MemoryStream())
                {
                    ms.Write(dumpStash.ToArray(), 0, dumpStash.Count);
                    ms.Write(encryptedBytes, 0, encryptedBytes.Length);
                    encryptedBytes = ms.ToArray();
                }

                prependSize = dumpStash.Count;
                dumpStash.Clear();
            }

            // Handle chunk overflow, store leftover part for the next chunk
            if (encryptedBytes.Length + (i == 0 ? 16 : 0) > BYTE_CHUNK_SIZE)
            {
                dumpStash.AddRange(encryptedBytes.Skip(BYTE_CHUNK_SIZE - (i == 0 ? 16 : 0)));
                encryptedBytes = encryptedBytes.Take(BYTE_CHUNK_SIZE - (i == 0 ? 16 : 0)).ToArray();
            }

            // Prepare the chunk data for uploading
            using (var chunkStream = new MemoryStream())
            {
                if (i == 0)
                {
                    chunkStream.Write(Encoding.UTF8.GetBytes("Salted__"), 0, 8); // Salted header
                    chunkStream.Write(cryptoData.Salt, 0, cryptoData.Salt.Length); // 8 bytes salt
                }

                chunkStream.Write(encryptedBytes, 0, encryptedBytes.Length);

                // Set headers and upload the chunk
                var headers = new NameValueCollection
                {
                { "X-Get-Raw-File", "1" },
                { "X-Put-Chunk-Start", (uploadSizes[i] - prependSize).ToString() },
                { "X-Put-Chunk-End", (uploadSizes[i] - prependSize + chunkStream.Length).ToString() },
                { "X-Put-JWT", metaInfo.Token }
                };

                SendRequest(HttpMethod.Put, URLHelpers.CombineURL(APIURL, metaInfo.UrlPathName), chunkStream, "application/octet-stream", null, headers);
            }

        }, chunks, fileSize);

        #endregion

        return new UploadResult
        {
            IsURLExpected = true,
            URL = $"{URLHelpers.CombineURL(APIURL, metaInfo.UrlPathName)}#{randomKey}"
        };
    }



    private delegate void StartEndCallback(int chunkStart, int chunkEnd, int i);
    private static void LoopStartEnd(StartEndCallback callback, int chunks, long fileSize)
    {
        var lastChunkEnd = 0;

        for (int i = 0; i < chunks; i++)
        {
            var chunkStart = i == 0 ? 0 : lastChunkEnd;
            var chunkEnd = (int)System.Math.Min(fileSize, lastChunkEnd + BYTE_CHUNK_SIZE);

            lastChunkEnd = chunkEnd;

            callback(chunkStart, chunkEnd, i);
        }
    }


    private static string GenerateRandomKey() => Guid.NewGuid().ToString(); // The web client uses random uuids as keys

    private static Vault_oooCryptoData DeriveCryptoData(byte[] key)
    {
        var salt = new byte[8]; // 8 bytes salt like in the web client
        using var rng = RandomNumberGenerator.Create(); // Cryptographically secure
        rng.GetBytes(salt);

        using var rfcDeriver = new Rfc2898DeriveBytes(key, salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);

        return new Vault_oooCryptoData
        {
            Salt = salt,
            Key = rfcDeriver.GetBytes(AES_KEY_SIZE / 8), // Derive the bytes from the rfcDeriver; Divide by 8 to input byte count
            IV = rfcDeriver.GetBytes(AES_BLOCK_SIZE / 8)
        };
    }


    private static byte[] EncryptBytes(Vault_oooCryptoData crypto, byte[] bytes)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.KeySize = AES_KEY_SIZE;
        aes.BlockSize = AES_BLOCK_SIZE;

        aes.Key = crypto.Key;
        aes.IV = crypto.IV;

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(crypto.Key, crypto.IV), CryptoStreamMode.Write);
        cs.Write(bytes, 0, bytes.Length); // Write all bytes into the CryptoStream
        cs.Close();
        return ms.ToArray();
    }

    private class Vault_oooCryptoData
    {
        public byte[] Salt { get; set; }
        public byte[] Key { get; set; }
        public byte[] IV { get; set; }
    }

    public class Vault_oooMetaInfo
    {
        [JsonPropertyName("urlPathName")]
        public string? UrlPathName { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}

