using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SnapX.Core;
using Xdg.Directories;

namespace SnapX.Avalonia.ViewModels;

public partial class ImportExportVM : ViewModelBase
{
    [RelayCommand]
    public async Task ImportConfig()
    {
        var topLevel = App.MyMainWindow;

        if (topLevel == null)
            return;
        var startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(
            UserDirectory.DocumentsDir
        );
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Import SnapX Config",
                SuggestedStartLocation = startLocation,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("SnapX Backup Config")
                    {
                        Patterns = ["*.sxb"],
                        MimeTypes = ["application/octet-stream"],
                    },
                ],
            }
        );

        var selectedFile = files.FirstOrDefault();
        if (selectedFile != null)
        {
            var importPath = selectedFile.Path.LocalPath;
            DebugHelper.WriteLine($"Import started: {importPath}");

            await Task.Run(() =>
            {
                SettingManager.Import(importPath);
                SettingManager.LoadInitialSettings();
            });
            DebugHelper.WriteLine($"Import completed: {importPath}");
            await RestartApp();
        }
    }
    private async Task RestartApp()
    {
        string message =

            "SnapX needs to restart to propagate changes! The app used to auto restart, until the stupid app developer corrupted his database doing so. So enjoy the auto shutdown! :DDD";

        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        // Silently murder MyMainWindow 💔
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            desktop.MainWindow = App.MySettingsWindow;
            if (App.MyMainWindow == null) return;
            App.MyMainWindow.Content = null;
            App.MyMainWindow.DataContext = null;
            App.MyMainWindow.Opacity = 0;
            App.MyMainWindow.IsEnabled = false;
            // App.MyMainWindow.Hide();
        });

        var dialog = new ContentDialog
        {
            Title = "Restart Required",
            Content = message,
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
        };

        if (desktop?.Windows != null)
        {
            foreach (var win in desktop.Windows)
            {
                win.IsEnabled = false;
            }
        }

        using var cts = new CancellationTokenSource();
        _ = Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
            Dispatcher.UIThread.Post(dialog.Hide), cts.Token);

        try
        {
            await Dispatcher.UIThread.InvokeAsync(async () => { await dialog.ShowAsync(App.MySettingsWindow); });
        }
        catch (Exception ex)
        {
            DebugHelper.WriteAlways($"Dialog failed: {ex.Message}");
        }
        finally
        {
            await cts.CancelAsync();
            Dispatcher.UIThread.Invoke(() => desktop?.Shutdown());
        }
    }
    [RelayCommand]
    public async Task ExportConfig()
    {
        var topLevel = App.MyMainWindow;

        if (topLevel == null)
            return;
        var startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(
            UserDirectory.DocumentsDir
        );
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export SnapX Config",
                SuggestedFileName = $"SnapX_Backup_{DateTime.Now:yyyyMMdd}",
                SuggestedStartLocation = startLocation,
                DefaultExtension = "sxb",
                FileTypeChoices =
                [
                    new FilePickerFileType("SnapX Backup Config")
                    {
                        Patterns = ["*.sxb"],
                        MimeTypes = ["application/octet-stream"],
                    },
                ],
            }
        );

        if (file != null)
        {
            var exportPath = file.Path.LocalPath;
            DebugHelper.WriteLine($"Export started: {exportPath}");

            await Task.Run(() =>
            {
                SettingManager.Export(exportPath, true, true);
            });

            DebugHelper.WriteLine($"Export completed: {exportPath}");
        }
    }
    [RelayCommand]
    public async Task ResetSettings()
    {
        // Wow, so convenient!
        SettingManager.ResetSettings();
        await RestartApp();
    }
}
