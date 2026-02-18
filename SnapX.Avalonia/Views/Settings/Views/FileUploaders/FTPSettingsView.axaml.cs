
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Upload;
using SnapX.Core.Upload.File;
using SnapX.Core.Upload.Utils;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia.Views.Settings.Views.FileUploaders;

public partial class FTPSettingsView : UserControl
{
    public FTPSettingsView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is not FtpBase daFTP) return;

        var config = SnapX.Core.SnapXL.UploadersConfig;
        ProtocolComboBox.ItemsSource = Enum.GetValues<FTPProtocol>();
        ProtocolComboBox.SelectedItem = daFTP.Account.Protocol;
        BrowserProtocolComboBox.ItemsSource = Enum.GetValues<BrowserProtocol>();
        BrowserProtocolComboBox.SelectedItem = daFTP.Account.BrowserProtocol;
        string[] transferModes = ["Active", "Passive"];
        TransferModeComboBox.ItemsSource = transferModes;
        TransferModeComboBox.SelectedItem = daFTP.Account.IsActive
            ? transferModes.First()
            : transferModes.Last();
        TransferModeComboBox.SelectionChanged += (Sender, Args) =>
            daFTP.Account.IsActive = (string)TransferModeComboBox.SelectedItem == "Active";
        // if (config.FTPAccountList.Any())
        // {
        ImageAccountsComboBox.ItemsSource = config.FTPAccountList;
        ImageAccountsComboBox.SelectedItem = config.FTPAccountList.ReturnIfValidIndex(config.FTPSelectedImage);

        ImageAccountsComboBox.SelectionChanged +=
            (Sender, Args) => config.FTPSelectedImage = ImageAccountsComboBox.SelectedIndex;
        TextAccountsComboBox.SelectionChanged +=
            (Sender, Args) => config.FTPSelectedText = TextAccountsComboBox.SelectedIndex;
        TextAccountsComboBox.ItemsSource = config.FTPAccountList;
        TextAccountsComboBox.SelectedItem = config.FTPAccountList.ReturnIfValidIndex(config.FTPSelectedText);

        FileAccountsComboBox.ItemsSource = config.FTPAccountList;
        FileAccountsComboBox.SelectedItem = config.FTPAccountList.ReturnIfValidIndex(config.FTPSelectedFile);

        FileAccountsComboBox.SelectionChanged +=
            (Sender, Args) => config.FTPSelectedFile = FileAccountsComboBox.SelectedIndex;
        ProtocolComboBox.SelectionChanged += (sender, args) =>
        {
            if (ProtocolComboBox.SelectedItem is not FTPProtocol newProtocol)
                return;

            if (DataContext is not FtpBase daFTP2)
                return;

            if (daFTP2.Account.Protocol == newProtocol)
                return;

            daFTP2.Account.Protocol = newProtocol;

            if (!UploaderFactory.FileUploaderServices.TryGetValue(FileDestination.FTP, out var baseService))
                return;

            if (baseService is not FTPFileUploaderService service)
                return;

            var config = SnapX.Core.SnapXL.UploadersConfig;

            var index = config.FTPAccountList.IndexOf(daFTP2.Account);
            if (!config.FTPAccountList.IsValidIndex(index))
                index = 0;

            var taskInfo = new TaskReferenceHelper
            {
                DataType = EDataType.Default,
                OverrideFTP = true,
                FTPIndex = index
            };

            DataContext = service.CreateUploader(config, taskInfo);
        };

        // }
    }

    private void AddFtpAccountClick(object? sender, RoutedEventArgs e)
    {
        var config = SnapX.Core.SnapXL.UploadersConfig;

        if (config.FTPAccountList == null)
            config.FTPAccountList = [];

        var newAccount = new FTPAccount();

        config.FTPAccountList.Add(newAccount);

        config.FTPSelectedFile = config.FTPAccountList.Count - 1;

        if (UploaderFactory.FileUploaderServices[FileDestination.FTP] is not FTPFileUploaderService service)
            return;


        var taskInfo = new TaskReferenceHelper
        {
            DataType = EDataType.Default,
            OverrideFTP = true,
            FTPIndex = config.FTPSelectedFile
        };

        var uploader = service.CreateUploader(config, taskInfo);

        DataContext = uploader;
    }

    private void RemoveFtpAccountButtonClick(object? sender, RoutedEventArgs e)
    {
        var config = SnapX.Core.SnapXL.UploadersConfig;

        if (config.FTPAccountList == null || config.FTPAccountList.Count == 0)
            return;

        var index = config.FTPSelectedFile;

        if (index < 0 || index >= config.FTPAccountList.Count)
            return;

        DataContext = null;

        config.FTPAccountList.RemoveAt(index);

        if (config.FTPAccountList.Count == 0)
        {
            config.FTPSelectedFile = -1;
            config.FTPSelectedImage = -1;
            config.FTPSelectedText = -1;
            return;
        }

        var newIndex = Math.Clamp(index, 0, config.FTPAccountList.Count - 1);
        config.FTPSelectedFile = newIndex;
        config.FTPSelectedImage = Math.Clamp(config.FTPSelectedImage, 0, config.FTPAccountList.Count - 1);
        config.FTPSelectedText = Math.Clamp(config.FTPSelectedText, 0, config.FTPAccountList.Count - 1);

        if (!UploaderFactory.FileUploaderServices.TryGetValue(FileDestination.FTP, out var baseService))
            return;

        if (baseService is not FTPFileUploaderService service)
            return;

        var taskInfo = new TaskReferenceHelper
        {
            DataType = EDataType.Default,
            OverrideFTP = true,
            FTPIndex = config.FTPSelectedFile
        };

        try
        {
            var uploader = service.CreateUploader(config, taskInfo);
            DataContext = uploader;
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
            DataContext = null;
        }
    }

    private void DuplicateFtpAccountButtonClick(object? sender, RoutedEventArgs e)
    {
        var config = SnapX.Core.SnapXL.UploadersConfig;

        if (config.FTPAccountList == null || config.FTPAccountList.Count == 0)
            return;

        var index = config.FTPSelectedFile;

        if (!config.FTPAccountList.IsValidIndex(index))
            return;

        var original = config.FTPAccountList[index];

        var copy = original.FastDeepClone();
        var originalName =
            original.Name ?? "FTP Account";

        var match = CustomUploaderVM.CopyNameRegex().Match(originalName);

        var rootName = match.Success ? match.Groups[1].Value : originalName;

        var baseNameWithCopy = $"{rootName} - Copy";
        copy.Name = baseNameWithCopy;
        config.FTPAccountList.Insert(index + 1, copy);

        var newIndex = index + 1;

        config.FTPSelectedFile = newIndex;
        config.FTPSelectedImage = newIndex;
        config.FTPSelectedText = newIndex;

        if (!UploaderFactory.FileUploaderServices
                .TryGetValue(FileDestination.FTP, out var baseService))
            return;

        if (baseService is not FTPFileUploaderService service)
            return;

        var taskInfo = new TaskReferenceHelper
        {
            DataType = EDataType.Default,
            OverrideFTP = true,
            FTPIndex = newIndex
        };

        DataContext = service.CreateUploader(config, taskInfo);
    }


    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FtpBase daFTP)
            return;

        var window = App.MySettingsWindow;

        if (window == null)
            return;

        try
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select Key File",
                AllowMultiple = false,
            };

            var files = await window.StorageProvider.OpenFilePickerAsync(options);
            if (files is { Count: > 0 })
            {
                daFTP.Account.Keypath = files[0].Path.LocalPath;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
        }
    }


    private void ExportButton_Click(object? Sender, RoutedEventArgs E)
    {
        throw new NotImplementedException();
    }

    private void ExportToClipboard_Click(object? Sender, RoutedEventArgs E)
    {
        throw new NotImplementedException();
    }

    private void SaveToFile_Click(object? Sender, RoutedEventArgs E)
    {
        throw new NotImplementedException();
    }

    private void UploadAsText_Click(object? Sender, RoutedEventArgs E)
    {
        throw new NotImplementedException();
    }

    private void ImportButton_Click(object? Sender, RoutedEventArgs E)
    {
        throw new NotImplementedException();
    }

    private void ImportFromClipboard_Click(object? Sender, RoutedEventArgs E)
    {
        throw new NotImplementedException();
    }

    private void LoadFromFile_Click(object? Sender, RoutedEventArgs E)
    {
        throw new NotImplementedException();
    }

    private async void TestFTPAccountClick(object? Sender, RoutedEventArgs E)
    {
        string msg = "";
        if (DataContext is not FtpBase daFTP) return;
        var account = daFTP.Account;

        var remotePath = account.GetSubFolderPath();
        var directories = new List<string>();
        await Task.Run(() =>
        {

            try
            {
                switch (account.Protocol)
                {
                    case FTPProtocol.FTP or FTPProtocol.FTPS:
                        {
                            using var ftp = new FTP(account);
                            if (ftp.Connect())
                            {
                                if (!ftp.DirectoryExists(remotePath))
                                {
                                    directories = ftp.CreateMultiDirectory(remotePath);
                                }

                                if (ftp.IsConnected)
                                {
                                    if (directories.Count > 0)
                                    {
                                        msg = "Account connected, created folders" + "\r\n" + string.Join("\r\n", directories);
                                    }
                                    else
                                    {
                                        msg = "Account connected";
                                    }
                                }
                                else
                                {
                                    msg = "Account not connected";
                                }
                            }

                            break;
                        }
                    case FTPProtocol.SFTP:
                        {
                            using var sftp = new SFTP(account);
                            if (sftp.Connect())
                            {
                                if (!sftp.DirectoryExists(remotePath))
                                {
                                    directories = sftp.CreateMultiDirectory(remotePath);
                                }

                                if (sftp.IsConnected)
                                {
                                    if (directories.Count > 0)
                                    {
                                        msg = "Account connected, created folders" + "\r\n" + string.Join("\r\n", directories);
                                    }
                                    else
                                    {
                                        msg = "Account connected";
                                    }
                                }
                                else
                                {
                                    msg = "Account not connected";
                                }
                            }

                            break;
                        }
                }
            }
            catch (Exception e)
            {
                msg = e.Message;
            }
        });

        var dialog = new ContentDialog
        {
            Title = SnapXL.AppName,
            Content = msg,
            PrimaryButtonText = Lang.Ok
        };
        _ = dialog.ShowAsync();
    }
}
