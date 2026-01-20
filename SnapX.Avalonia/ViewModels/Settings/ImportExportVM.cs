using System.Diagnostics;
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
            "SnapX needs to restart to propagate changes! Hopefully this restart works!";
        DebugHelper.WriteLine(message);

        var dialog = new ContentDialog
        {
            Title = "Restart Required",
            Content = message + "\n\nRestarting automatically in 5 seconds...",
            PrimaryButtonText = "OK",
            DefaultButton = ContentDialogButton.Primary,
        };
        var desktop =
            Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

        if (desktop?.Windows != null)
        {
            foreach (var win in desktop.Windows)
            {
                win.IsEnabled = false;
            }
        }
        if (App.MyMainWindow is { } window)
        {
            // Start a timer to close the dialog automatically
            var autoCloseTask = Task.Delay(TimeSpan.FromSeconds(5))
                .ContinueWith(_ =>
                {
                    Dispatcher.UIThread.Post(() => dialog.Hide());
                });

            await dialog.ShowAsync();
        }
         var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
            return;

        var psi = new ProcessStartInfo
        {
            FileName = processPath,
            UseShellExecute = true,
            WorkingDirectory = AppContext.BaseDirectory,
        };

        var args = Environment.GetCommandLineArgs().Skip(1);
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }
        Process.Start(psi);

        foreach (System.Collections.DictionaryEntry de in Environment.GetEnvironmentVariables())
        {
            var key = de.Key.ToString();
            var val = de.Value?.ToString();
            if (!string.IsNullOrEmpty(key) && !psi.Environment.ContainsKey(key))
            {
                psi.EnvironmentVariables[key] = val;
            }
        }

        if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
        {
            lifetime.Shutdown(0);
        }
        else
        {
            Environment.Exit(0);
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
            await RestartApp();
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
