
// SPDX-License-Identifier: GPL-3.0-or-later


using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using SnapX.Core.Upload.BaseUploaders;
using SnapX.Core.Utils;

namespace SnapX.Core.Upload.File;

public sealed class SFTP : FileUploader, IDisposable
{
    public FTPAccount Account { get; private set; }

    public bool IsValidAccount => (!string.IsNullOrEmpty(Account.Keypath) && System.IO.File.Exists(Account.Keypath)) || !string.IsNullOrEmpty(Account.Password);

    public bool IsConnected => client != null && client.IsConnected;

    private SftpClient client;

    public SFTP(FTPAccount account)
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

            bool uploadResult = UploadStream(stream, path, true);

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

    public bool Connect()
    {
        if (client == null)
        {
            if (!string.IsNullOrEmpty(Account.Keypath))
            {
                if (!System.IO.File.Exists(Account.Keypath))
                {
                    throw new FileNotFoundException("Key file does not exist.", Account.Keypath);
                }

                PrivateKeyFile keyFile;

                if (string.IsNullOrEmpty(Account.Passphrase))
                {
                    keyFile = new PrivateKeyFile(Account.Keypath);
                }
                else
                {
                    keyFile = new PrivateKeyFile(Account.Keypath, Account.Passphrase);
                }

                client = new SftpClient(Account.Host, Account.Port, Account.Username, keyFile);
            }
            else if (!string.IsNullOrEmpty(Account.Password))
            {
                client = new SftpClient(Account.Host, Account.Port, Account.Username, Account.Password);
            }

            if (client != null)
            {
                client.BufferSize = (uint)BufferSize;
            }
        }

        if (client != null && !client.IsConnected)
        {
            client.Connect();
        }

        return IsConnected;
    }

    public void Disconnect()
    {
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
    }

    public void ChangeDirectory(string? path, bool autoCreateDirectory = false)
    {
        if (Connect())
        {
            try
            {
                client.ChangeDirectory(path);
            }
            catch (SftpPathNotFoundException) when (autoCreateDirectory)
            {
                CreateDirectory(path, true);
                ChangeDirectory(path);
            }
        }
    }

    public bool DirectoryExists(string? path)
    {
        if (Connect())
        {
            return client.Exists(path);
        }

        return false;
    }

    public void CreateDirectory(string? path, bool createMultiDirectory = false)
    {
        if (Connect())
        {
            try
            {
                client.CreateDirectory(path);

                DebugHelper.WriteLine($"SFTP directory created: {path}");
            }
            catch (SftpPathNotFoundException) when (createMultiDirectory)
            {
                CreateMultiDirectory(path);
            }
            catch (SftpPermissionDeniedException)
            {
            }
        }
    }

    public List<string?> CreateMultiDirectory(string? path)
    {
        List<string?> directoryList = [];

        List<string?> paths = URLHelpers.GetPaths(path);

        foreach (string? directory in paths)
        {
            if (!DirectoryExists(directory))
            {
                CreateDirectory(directory);
                directoryList.Add(directory);
            }
        }

        return directoryList;
    }

    private bool UploadStream(Stream stream, string? remotePath, bool autoCreateDirectory = false)
    {
        if (Connect())
        {
            try
            {
                using (SftpFileStream sftpStream = client.Create(remotePath))
                {
                    return TransferData(stream, sftpStream);
                }
            }
            catch (SftpPathNotFoundException) when (autoCreateDirectory)
            {
                // Happens when directory not exist, create directory and retry uploading

                CreateDirectory(URLHelpers.GetDirectoryPath(remotePath), true);
                return UploadStream(stream, remotePath);
            }
            catch (NullReferenceException)
            {
                // Happens when disconnect while uploading
            }
        }

        return false;
    }

    public void Dispose()
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
}
