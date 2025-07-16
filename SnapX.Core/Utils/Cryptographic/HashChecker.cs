// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Security.Cryptography;

namespace SnapX.Core.Utils.Cryptographic;

public class HashChecker
{
    public bool IsWorking { get; private set; }

    public delegate void ProgressChanged(float progress);
    public event ProgressChanged FileCheckProgressChanged;

    private CancellationTokenSource cts;

    private void OnProgressChanged(float percentage)
    {
        FileCheckProgressChanged?.Invoke(percentage);
    }

    public async Task<string> Start(string filePath, HashType hashType)
    {
        string result = null;

        if (!IsWorking && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            IsWorking = true;

            Progress<float> progress = new Progress<float>(OnProgressChanged);

            using (cts = new CancellationTokenSource())
            {
                result = await Task.Run(() =>
                {
                    try
                    {
                        return HashCheckThread(filePath, hashType, progress, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    return null;
                }, cts.Token);
            }

            IsWorking = false;
        }

        return result;
    }

    public void Stop()
    {
        cts?.Cancel();
    }

    private string HashCheckThread(string filePath, HashType hashType, IProgress<float> progress, CancellationToken ct)
    {
        using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (HashAlgorithm hash = GetHashAlgorithm(hashType))
        using (CryptoStream cs = new CryptoStream(stream, hash, CryptoStreamMode.Read))
        {
            long bytesRead, totalRead = 0;
            byte[] buffer = new byte[8192];
            Stopwatch timer = Stopwatch.StartNew();

            while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0 && !ct.IsCancellationRequested)
            {
                totalRead += bytesRead;

                if (timer.ElapsedMilliseconds > 200)
                {
                    float percentage = (float)totalRead / stream.Length * 100;
                    progress.Report(percentage);

                    timer.Reset();
                    timer.Start();
                }
            }

            if (ct.IsCancellationRequested)
            {
                progress.Report(0);

                ct.ThrowIfCancellationRequested();
            }
            else
            {
                progress.Report(100);

                string[] hex = TranslatorHelper.BytesToHexadecimal(hash.Hash);
                return string.Concat(hex);
            }
        }

        return null;
    }

    public static HashAlgorithm GetHashAlgorithm(HashType hashType) =>
        hashType switch
        {
            HashType.CRC32 => new Crc32(),
            HashType.MD5 => MD5.Create(),
            HashType.SHA1 => SHA1.Create(),
            HashType.SHA256 => SHA256.Create(),
            HashType.SHA384 => SHA384.Create(),
            HashType.SHA512 => SHA512.Create(),
            _ => null
        };
}

