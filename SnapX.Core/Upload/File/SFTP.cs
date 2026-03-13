
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Runtime.InteropServices;
using SnapX.Core.Utils;
using Tmds.Ssh;

namespace SnapX.Core.Upload.File;

public sealed class SFTP : FtpBase
{
    public FTPAccount Account { get; private set; }

    public bool IsValidAccount => (!string.IsNullOrEmpty(Account.Keypath) && System.IO.File.Exists(Account.Keypath)) || !string.IsNullOrEmpty(Account.Password);

    public override bool IsConnected => client != null;

    private SftpClient? client;

    public SFTP(FTPAccount account) : base(account)
    {
        Account = account;
    }

    public override UploadResult Upload(Stream stream, string? fileName)
    {
        UploadResult result = new UploadResult();

        string? subFolderPath = Account.GetSubFolderPath();
        string? path = URLHelpers.CombineURL(subFolderPath, fileName);
        string? url = Account.GetUriPath(fileName, subFolderPath);

        OnEarlyURLCopyRequested(url);

        try
        {
            IsUploading = true;
            if (!IsConnected) Connect();
            bool uploadResult = UploadStream(stream, path, true).GetAwaiter().GetResult();

            if (uploadResult && !StopUploadRequested && !IsError)
            {
                result.URL = url;
            }
        }
        finally
        {
            Dispose();

            IsUploading = false;
        }

        return result;
    }

    public override void StopUpload()
    {
        if (IsUploading && !StopUploadRequested)
        {
            StopUploadRequested = true;

            try
            {
                Disconnect();
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }
        }
    }
    public string ConstructSshDestination(string user, string host, int port = 22)
    {
        return $"{user}@{host}:{port}";
    }
    public override bool Connect()
    {
        var sshDestination = ConstructSshDestination(Account.Username, Account.Host, Account.Port);

        var settings = new SshClientSettings(sshDestination)
        {
            AutoConnect = true,
            AutoReconnect = true,
        };
        string? keyPath = Account.Keypath;

        if (string.IsNullOrWhiteSpace(keyPath))
        {
            var home = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : Environment.GetEnvironmentVariable("HOME") ?? string.Empty;

            var defaultKeys = new[]
            {
                "id_ed25519",
                "id_rsa",
                "id_ecdsa",
                "id_dsa"
            };

            foreach (var key in defaultKeys)
            {
                var path = Path.Combine(home, ".ssh", key);
                if (!System.IO.File.Exists(path)) continue;
                keyPath = path;
                break;
            }
        }
        if (!string.IsNullOrEmpty(keyPath)) settings.Credentials.Add(new PrivateKeyCredential(keyPath, Account.Passphrase));
        if (!string.IsNullOrEmpty(Account.Password)) settings.Credentials.Add(new PasswordCredential(Account.Password));
        settings.Credentials.Add(new SshAgentCredentials());

        client = new SftpClient(settings);
        return client is not null;
    }

    public override void Disconnect()
    {
        if (client != null)
        {
            client.Dispose();
            client = null;
        }
    }

    public void ChangeDirectory(string? path, bool autoCreateDirectory = false)
    {
        if (Connect())
        {
            try
            {
                client.GetDirectory(path);
            }
            catch (SftpException) when (autoCreateDirectory)
            {
                CreateDirectory(path, true);
                ChangeDirectory(path);
            }
        }
    }

    public bool DirectoryExists(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            var attributes = client.GetAttributesAsync(path, followLinks: true, filter: null).ConfigureAwait(false).GetAwaiter().GetResult();
            return attributes is { FileType: UnixFileType.Directory };
        }
        catch (SftpException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async void CreateDirectory(string? path, bool createMultiDirectory = false)
    {
        if (Connect())
        {
            try
            {
                await client.CreateDirectoryAsync(path);

                DebugHelper.WriteLine($"SFTP directory created: {path}");
            }
            catch (SftpException e) when (createMultiDirectory)
            {
                if (e.Error == SftpError.PermissionDenied) return;
                await client.CreateDirectoryAsync(path, true);

            }
        }
    }

    private async Task<bool> UploadStream(Stream stream, string? remotePath, bool autoCreateDirectory = false)
    {
        try
        {
            await using var remoteStream = await client.OpenOrCreateFileAsync(remotePath, FileAccess.Write, null).ConfigureAwait(false);
            return await TransferDataAsync(stream, remoteStream);
        }
        catch (SftpException e) when (autoCreateDirectory)
        {
            var code = e.Error;
            if (code is not SftpError.NoSuchFile) return false;
            CreateDirectory(URLHelpers.GetDirectoryPath(remotePath), true);
            return await UploadStream(stream, remotePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
            return false;
        }
    }


    public override void Dispose()
    {
        if (client != null)
        {
            try
            {
                client.Dispose();
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }
        }
    }

    public List<string?> CreateMultiDirectory(string? remotePath)
    {
        List<string?> paths = URLHelpers.GetPaths(remotePath);

        foreach (string? path in paths)
        {
            CreateDirectory(path);
            DebugHelper.WriteLine($"FTP directory created: {path}");
        }

        return paths;
    }
}
