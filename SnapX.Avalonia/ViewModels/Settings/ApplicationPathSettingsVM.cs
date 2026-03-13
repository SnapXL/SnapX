using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapX.Core;
using SnapX.Core.Utils;

namespace SnapX.Avalonia.ViewModels.Settings;

public partial class ApplicationPathSettingsVM : ViewModelBase
{
    private readonly ApplicationConfig _config;
    private IStorageService? _storageService;

    public void SetStorageService(IStorageService service)
    {
        _storageService = service;
    }
    [ObservableProperty]
    private string _personalFolderPath;

    [ObservableProperty]
    private bool _useCustomScreenshotsFolder;

    [ObservableProperty]
    private string _customScreenshotsPath;

    [ObservableProperty]
    private string _subFolderPattern;

    [ObservableProperty]
    private string _windowSubFolderPattern;

    public string SubFolderPreviewPath => Path.Combine(
        UseCustomScreenshotsFolder ? CustomScreenshotsPath : PersonalFolderPath,
        "Screenshots",
        DateTime.Now.ToString(SubFolderPattern ?? "yyyy-MM"));

    public ApplicationPathSettingsVM()
    {
        _config = SnapXL.Settings;

        _personalFolderPath = SnapXL.PersonalFolder;
        _useCustomScreenshotsFolder = _config.UseCustomScreenshotsPath;
        _customScreenshotsPath = _config.CustomScreenshotsPath;
        _subFolderPattern = _config.SaveImageSubFolderPattern;
        _windowSubFolderPattern = _config.SaveImageSubFolderPatternWindow;
    }

    partial void OnPersonalFolderPathChanged(string value) => SnapXL.CustomPersonalPath = value;
    partial void OnUseCustomScreenshotsFolderChanged(bool value)
    {
        _config.UseCustomScreenshotsPath = value;
        OnPropertyChanged(nameof(SubFolderPreviewPath));
    }
    partial void OnCustomScreenshotsPathChanged(string value)
    {
        _config.CustomScreenshotsPath = value;
        OnPropertyChanged(nameof(SubFolderPreviewPath));
    }
    partial void OnSubFolderPatternChanged(string value)
    {
        _config.SaveImageSubFolderPattern = value;
        OnPropertyChanged(nameof(SubFolderPreviewPath));
    }
    partial void OnWindowSubFolderPatternChanged(string value) => _config.SaveImageSubFolderPatternWindow = value;

    [RelayCommand]
    private async Task BrowsePersonalFolder()
    {
        if (_storageService == null) return;

        var path = await _storageService.SelectFolderAsync("Select Personal Folder", PersonalFolderPath);
        if (!string.IsNullOrEmpty(path))
        {
            PersonalFolderPath = path;
        }
    }

    [RelayCommand]
    private void ApplyPersonalFolder()
    {
        // idk
    }

    [RelayCommand]
    private void OpenPersonalFolder() => OpenFolder(PersonalFolderPath);

    [RelayCommand]
    private async Task BrowseCustomScreenshots(Visual visual)
    {
        var path = await ShowFolderPicker(visual, "Select Screenshots Folder", CustomScreenshotsPath);
        if (!string.IsNullOrEmpty(path)) CustomScreenshotsPath = path;
    }

    [RelayCommand]
    private void OpenSubFolder() => OpenFolder(SubFolderPreviewPath);

    private async Task<string?> ShowFolderPicker(Visual visual, string title, string startPath)
    {
        var topLevel = TopLevel.GetTopLevel(visual);
        if (topLevel == null) return null;

        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (Directory.Exists(startPath))
        {
            options.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startPath);
        }

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    private void OpenFolder(string path)
    {
        FileHelpers.OpenFolder(path);
    }
}
public interface IStorageService
{
    Task<string?> SelectFolderAsync(string title, string startPath);
}

public class StorageService(TopLevel topLevel) : IStorageService
{
    public async Task<string?> SelectFolderAsync(string title, string startPath)
    {
        var options = new FolderPickerOpenOptions { Title = title, AllowMultiple = false };
        if (Directory.Exists(startPath))
            options.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startPath);

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }
}
