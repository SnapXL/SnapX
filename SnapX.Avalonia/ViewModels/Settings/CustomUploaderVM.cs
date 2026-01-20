using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DotNext.Threading;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.Views.Settings.Views;
using SnapX.Core;
using SnapX.Core.Job;
using SnapX.Core.Resources;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Upload.File;
using SnapX.Core.Upload.Img;
using SnapX.Core.Upload.SharingServices;
using SnapX.Core.Upload.Text;
using SnapX.Core.Upload.URL;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Avalonia.ViewModels;

public partial class CustomUploaderVM : ViewModelBase
{
    public ObservableCollection<CustomUploaderItem> Uploaders { get; } = new();
    public IEnumerable<CustomUploaderItem> ImageUploaders =>
        Uploaders.Where(x =>
            x.DestinationType.HasFlag(CustomUploaderDestinationType.ImageUploader)
        );

    public IEnumerable<CustomUploaderItem> FileUploaders =>
        Uploaders.Where(x => x.DestinationType.HasFlag(CustomUploaderDestinationType.FileUploader));

    public IEnumerable<CustomUploaderItem> TextUploaders =>
        Uploaders.Where(x => x.DestinationType.HasFlag(CustomUploaderDestinationType.TextUploader));

    public IEnumerable<CustomUploaderItem> ShortenerUploaders =>
        Uploaders.Where(x => x.DestinationType.HasFlag(CustomUploaderDestinationType.URLShortener));

    public IEnumerable<CustomUploaderItem> SharingUploaders =>
        Uploaders.Where(x =>
            x.DestinationType.HasFlag(CustomUploaderDestinationType.URLSharingService)
        );

    [ObservableProperty]
    private CustomUploaderItem? _selectedImageUploader;

    [ObservableProperty]
    private CustomUploaderItem? _selectedFileUploader;

    [ObservableProperty]
    private CustomUploaderItem? _selectedTextUploader;

    [ObservableProperty]
    private CustomUploaderItem? _selectedShortenerUploader;

    [ObservableProperty]
    private CustomUploaderItem? _selectedSharingUploader;
    private readonly AsyncLock _collectionLock = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicWatermark))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private CustomUploaderItem? selectedUploader;
    public bool IsEmpty => Uploaders.Count == 0;

    public CustomUploaderDestinationType[] AllDestinationTypes { get; } =
        Enum.GetValues<CustomUploaderDestinationType>()
            .Where(t => t != CustomUploaderDestinationType.None)
            .ToArray();
    public CustomUploaderBody[] AllBodyTypes { get; } = Enum.GetValues<CustomUploaderBody>();
    public HttpMethod[] AllHttpMethods { get; } =
    [
        HttpMethod.Get,
        HttpMethod.Post,
        HttpMethod.Put,
        HttpMethod.Patch,
        HttpMethod.Delete,
        HttpMethod.Head,
        HttpMethod.Options,
        HttpMethod.Trace,
        HttpMethod.Connect,
    ];

    public CustomUploaderVM()
    {
        IsInitializing = true;
        IsLoading = true;
        var UploadersConfig = App.SnapX.GetUploadersConfig();
        UploadersConfig.SettingsSaved += UploadersConfig_SettingsSaved;
    }

    [RelayCommand]
    private async Task OpenCatalog()
    {
        var vm = new CustomUploaderCatalogVM();
        await vm.LoadCatalogAsync();

        var dialog = new ContentDialog
        {
            Title = "Uploader Catalog",
            Content = new CustomUploaderCatalogView(vm) { DataContext = vm },
            PrimaryButtonText = "Import Selected",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var selectedItems = vm.AvailableUploaders.Where(x => x.IsSelected).ToList();
            IsLoading = true;

            var downloadTasks = selectedItems
                .Where(x => !string.IsNullOrEmpty(x.DownloadUrl))
                .Select(x => DownloadUploaderJson(x.DownloadUrl!));

            var jsonContents = await Task.WhenAll(downloadTasks);

            var validJson = jsonContents.Where(json => !string.IsNullOrEmpty(json)).ToList();

            if (validJson.Count > 0)
            {
                TaskHelpers.ImportCustomUploaderJson(validJson);
            }
            IsLoading = false;
            TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);

            SelectedUploader = Uploaders.LastOrDefault();
        }
    }

    private async Task<string?> DownloadUploaderJson(string url)
    {
        try
        {
            var client = Core.Utils.Miscellaneous.HttpClientFactory.Get();
            return await client.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            DebugHelper.Logger?.Error("Failed to download uploader json");
            DebugHelper.WriteException(ex);
            return null;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUiEnabled))]
    private bool _isInitializing = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUiEnabled))]
    private bool _isLoading = true;

    public bool IsUiEnabled => !IsLoading && !IsInitializing;
    private bool _isDisposed;

    async partial void OnSelectedUploaderChanged(
        CustomUploaderItem? oldValue,
        CustomUploaderItem? newValue
    )
    {
        DebugHelper.WriteLine(
            $"Selected uploader changed to: {newValue?.Name ?? URLHelpers.GetHostName(newValue?.RequestURL)}"
        );
        if (newValue == null)
        {
            DebugHelper.WriteLine($"New selected uploader ({nameof(SelectedUploader)}) is null.");
        }
    }

    private async Task PropagateData(CancellationToken cts = default)
    {
        try
        {
            IsLoading = true;
            DebugHelper.WriteLine("Propagating custom uploader data...");
            var UploadersConfig = App.SnapX.GetUploadersConfig();
            using (await _collectionLock.AcquireAsync(cts))
            {
                // There is no telling what happens if interrupted while modifying the collection
                Uploaders.CollectionChanged -= OnUploadersChanged;
                Uploaders.Clear();
                var i = 0;
                foreach (var uploader in UploadersConfig.CustomUploadersList)
                {
                    i++;
                    DebugHelper.Logger?.Debug(
                        "Propagating uploader: " + uploader.GetFileName() + $" ({i})"
                    );
                    Uploaders.Add(uploader);
                }
                Uploaders.CollectionChanged += OnUploadersChanged;
                OnPropertyChanged(nameof(ImageUploaders));
                OnPropertyChanged(nameof(FileUploaders));
                OnPropertyChanged(nameof(TextUploaders));
                OnPropertyChanged(nameof(ShortenerUploaders));
                OnPropertyChanged(nameof(SharingUploaders));
                OnPropertyChanged(nameof(IsEmpty));
                SelectedImageUploader = GetUploaderByIndex(
                    UploadersConfig.CustomImageUploaderSelected
                );
                SelectedFileUploader = GetUploaderByIndex(
                    UploadersConfig.CustomFileUploaderSelected
                );
                SelectedTextUploader = GetUploaderByIndex(
                    UploadersConfig.CustomTextUploaderSelected
                );
                SelectedShortenerUploader = GetUploaderByIndex(
                    UploadersConfig.CustomURLShortenerSelected
                );
                SelectedSharingUploader = GetUploaderByIndex(
                    UploadersConfig.CustomURLSharingServiceSelected
                );
                DebugHelper.WriteLine($"Propagated {Uploaders.Count} custom uploaders.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnUploadersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isDisposed)
            return;
        using (await _collectionLock.AcquireAsync(new CancellationToken()))
        {
            var configList = App.SnapX.GetUploadersConfig().CustomUploadersList;
            DebugHelper.Logger?.Debug(
                "CustomUploaderVM: Uploaders collection change: "
                    + e.Action.ToString()
                    + "\n\n"
                    + e.NewItems?.Count
                    + " new items, "
                    + e.OldItems?.Count
                    + " old items."
            );
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        int insertIndex =
                            e.NewStartingIndex >= 0 && e.NewStartingIndex <= configList.Count
                                ? e.NewStartingIndex
                                : configList.Count;

                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            configList.Insert(insertIndex + i, (CustomUploaderItem)e.NewItems[i]!);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (
                        e.OldItems != null
                        && e.OldStartingIndex >= 0
                        && e.OldStartingIndex < configList.Count
                    )
                    {
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            if (configList.Count > e.OldStartingIndex)
                            {
                                configList.RemoveAt(e.OldStartingIndex);
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (
                        e.NewItems != null
                        && e.OldItems != null
                        && e.OldStartingIndex >= 0
                        && e.OldStartingIndex < configList.Count
                    )
                    {
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            if (e.OldStartingIndex + i < configList.Count)
                            {
                                configList[e.OldStartingIndex + i] = (CustomUploaderItem)
                                    e.NewItems[i]!;
                            }
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (
                        e.OldItems != null
                        && e.OldStartingIndex >= 0
                        && e.OldStartingIndex < configList.Count
                    )
                    {
                        if (e.NewStartingIndex >= 0 && e.NewStartingIndex <= configList.Count)
                        {
                            var movedItem = configList[e.OldStartingIndex];
                            configList.RemoveAt(e.OldStartingIndex);
                            configList.Insert(e.NewStartingIndex, movedItem);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    configList.Clear();
                    foreach (var item in Uploaders)
                    {
                        configList.Add(item);
                    }
                    break;
            }
        }
    }

    public async Task InitializeAsync(CancellationToken cts = default)
    {
        var UploadersConfig = App.SnapX.GetUploadersConfig();
        try
        {
            if (Uploaders.Count > 0)
            {
                DebugHelper.WriteLine("CustomUploaderVM already initialized.");
                return;
            }
            await PropagateData(cts);
            var targetIndex = UploadersConfig.CustomImageUploaderSelected;

            SelectedUploader =
                (targetIndex >= 0 && targetIndex < Uploaders.Count)
                    ? Uploaders[targetIndex]
                    : Uploaders.FirstOrDefault();
            IsInitializing = false;
            IsLoading = false;
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            var dialog = new ContentDialog
            {
                Title = $"{nameof(CustomUploaderVM)} - Initialization Error",
                Content = new SelectableTextBlock
                {
                    Text =
                        $"Failed to initialize CustomUploadersVM.\nYou will not be able to interact with this view without a reload.\nDetails: {e}",
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400,
                },
                PrimaryButtonText = "Copy",
                CloseButtonText = "Close",
            };
            IsInitializing = false;
            // Prevent UI from being usable in this state
            IsLoading = true;
            TaskHelpers.PlayNotificationSoundAsync(NotificationSound.Error);
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var topLevel = TopLevel.GetTopLevel(
                    App.MyMainWindow is not null ? App.MyMainWindow : dialog
                );
                await topLevel?.Clipboard?.SetTextAsync(e.ToString());
            }
            return;
        }
    }

    private CustomUploaderItem? GetUploaderByIndex(int index)
    {
        if (index >= 0 && index < Uploaders.Count)
            return Uploaders[index];
        return null;
    }

    partial void OnSelectedImageUploaderChanged(CustomUploaderItem? value)
    {
        if (value == null || IsLoading)
            return;
        var config = App.SnapX.GetUploadersConfig();
        var newIndex = Uploaders.IndexOf(value);
        if (config.CustomImageUploaderSelected != newIndex)
        {
            config.CustomImageUploaderSelected = newIndex;
        }
    }

    partial void OnSelectedTextUploaderChanged(CustomUploaderItem? value)
    {
        if (value == null || IsLoading)
            return;
        var config = App.SnapX.GetUploadersConfig();
        var newIndex = Uploaders.IndexOf(value);
        if (config.CustomTextUploaderSelected != newIndex)
        {
            config.CustomTextUploaderSelected = newIndex;
        }
    }

    partial void OnSelectedFileUploaderChanged(CustomUploaderItem? value)
    {
        if (value == null || IsLoading)
            return;
        var config = App.SnapX.GetUploadersConfig();
        var newIndex = Uploaders.IndexOf(value);
        if (config.CustomFileUploaderSelected != newIndex)
        {
            config.CustomFileUploaderSelected = newIndex;
        }
    }

    partial void OnSelectedShortenerUploaderChanged(CustomUploaderItem? value)
    {
        if (value == null || IsLoading)
            return;
        var config = App.SnapX.GetUploadersConfig();
        var newIndex = Uploaders.IndexOf(value);
        if (config.CustomURLShortenerSelected != newIndex)
        {
            config.CustomURLShortenerSelected = newIndex;
        }
    }

    partial void OnSelectedSharingUploaderChanged(CustomUploaderItem? value)
    {
        if (value == null || IsLoading)
            return;
        var config = App.SnapX.GetUploadersConfig();
        var newIndex = Uploaders.IndexOf(value);
        if (config.CustomURLSharingServiceSelected != newIndex)
        {
            config.CustomURLSharingServiceSelected = newIndex;
        }
    }

    private async void UploadersConfig_SettingsSaved(
        UploadersConfig settings,
        string filePath,
        bool result
    )
    {
        if (Core.SnapX.CloseSequenceStarted || IsInitializing)
            return;
        DebugHelper.WriteLine("CustomUploaderVM detected settings saved, re-propagating data...");
        await PropagateData();
    }

    [RelayCommand]
    private void AddUploader()
    {
        var newItem = new CustomUploaderItem();
        newItem.Headers = [];
        newItem.Parameters = [];
        Uploaders.Add(newItem);
        SelectedUploader = newItem;
    }

    [RelayCommand]
    private async Task ImportUploader()
    {
        var topLevel = TopLevel.GetTopLevel(
            Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null
        );
        if (topLevel == null)
        {
            DebugHelper.Logger?.Error("Failed to get top-level window for file picker.");
            return;
        }
        // 2. Open the file picker
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Import SnapX/ShareX Custom Uploader",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType("SnapX/ShareX Custom Uploader")
                    {
                        Patterns = ["*.sxcu"],
                        MimeTypes = ["application/json"],
                    },
                ],
            }
        );
        if (files.Count == 0)
        {
            DebugHelper.WriteLine("No files selected for import.");
            return;
        }
        try
        {
            foreach (var file in files)
            {
                DebugHelper.WriteLine("Selected file for import: " + file.Path.LocalPath);
            }
            IsLoading = true;
            TaskHelpers.ImportCustomUploader(files.Select(f => f.Path.LocalPath));
            IsLoading = false;
            TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
            SelectedUploader = Uploaders.LastOrDefault();
        }
        catch (Exception ex)
        {
            DebugHelper.WriteException(ex);
            var dialog = new ContentDialog
            {
                Title = "SnapX - Import Error",
                Content = new SelectableTextBlock
                {
                    Text = $"Failed to import uploader(s). Details:\n{ex.Message}",
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400,
                },
                CloseButtonText = "Close",
            };
            await dialog.ShowAsync();
            return;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportUploader(CustomUploaderItem item)
    {
        await InternalExportLogic(item, null);
    }

    private async Task InternalExportLogic(CustomUploaderItem item, string? filePath = null)
    {
        if (string.IsNullOrWhiteSpace(item.RequestURL))
        {
            DebugHelper.Logger?.Error("Cannot export uploader with null RequestURL.");
            var dialog = new ContentDialog
            {
                Title = "Invalid Uploader",
                Content = "This uploader cannot be exported because the Request URL is empty.",
                CloseButtonText = "OK",
            };

            await dialog.ShowAsync();
            return;
        }
        if (item.DestinationType == CustomUploaderDestinationType.None)
        {
            DebugHelper.Logger?.Error("Cannot export uploader with None destination type.");
            var dialog = new ContentDialog
            {
                Title = "Invalid Uploader",
                Content =
                    "This uploader cannot be exported because the Destination Type is not yet configured.",
                CloseButtonText = "OK",
            };

            await dialog.ShowAsync();
            return;
        }
        if (filePath == null)
        {
            var topLevel = TopLevel.GetTopLevel(
                Application.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null
            );
            if (topLevel == null)
            {
                DebugHelper.Logger?.Error("Failed to get top-level window for file picker.");
                return;
            }
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = "Export SnapX/ShareX Custom Uploader",
                    DefaultExtension = ".sxcu",
                    SuggestedFileName = item.GetFileName(),
                    FileTypeChoices =
                    [
                        new FilePickerFileType("SnapX/ShareX Custom Uploader")
                        {
                            Patterns = ["*.sxcu"],
                            MimeTypes = ["application/json"],
                        },
                    ],
                }
            );

            if (file != null)
            {
                filePath = file.Path.LocalPath;
            }
        }
        try
        {
            JsonHelpers.SerializeToFile(item, filePath);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
            var dialog = new ContentDialog
            {
                Title = "SnapX - Export Error",
                Content = new SelectableTextBlock
                {
                    Text = $"Failed to export. Details:\n{e.Message}",
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400,
                },
                CloseButtonText = "Close",
            };
            await dialog.ShowAsync();
            return;
        }
    }

    [RelayCommand]
    private void ToggleDestinationType(CustomUploaderDestinationType flag)
    {
        DebugHelper.WriteLine("Toggling destination type: " + flag.ToString());
        if (SelectedUploader == null)
            return;

        SelectedUploader.DestinationType ^= flag;

        OnPropertyChanged(nameof(SelectedUploader));
        OnPropertyChanged(nameof(ImageUploaders));
        OnPropertyChanged(nameof(FileUploaders));
        OnPropertyChanged(nameof(TextUploaders));
        OnPropertyChanged(nameof(ShortenerUploaders));
        OnPropertyChanged(nameof(SharingUploaders));
    }

    public string DynamicWatermark =>
        !string.IsNullOrWhiteSpace(SelectedUploader?.RequestURL)
            ? URLHelpers.GetHostName(SelectedUploader.RequestURL)
            : "Name";

    public bool IsTypeSelected(CustomUploaderDestinationType flag)
    {
        DebugHelper.WriteLine("Checking if type is selected: " + flag.ToString());
        return SelectedUploader?.DestinationType.HasFlag(flag) ?? false;
    }

    [RelayCommand]
    private async Task TestUploader(CustomUploaderItem item)
    {
        var type = item.DestinationType;
        UploadResult result = new UploadResult();
        DebugHelper.WriteLine($"Testing {item.Name ?? item.GetFileName()} ({type})");
        // Doing requests on the UI thread can cause freezes, so run in background
        await Task.Run(() =>
        {
            try
            {
                var flags = Enum.GetValues<CustomUploaderDestinationType>()
                    .Where(f =>
                        item.DestinationType.HasFlag(f) && f != CustomUploaderDestinationType.None
                    );

                foreach (var type in flags)
                {
                    switch (type)
                    {
                        case CustomUploaderDestinationType.ImageUploader:

                            CustomImageUploader imageUploader = new CustomImageUploader(item);
                            imageUploader.Upload(Resources.LogoStream, "Test.png");
                            result.Errors.Add(imageUploader.Errors);
                            break;
                        case CustomUploaderDestinationType.TextUploader:
                            CustomTextUploader textUploader = new CustomTextUploader(item);
                            textUploader.UploadText(
                                "This is a test text upload from SnapX!",
                                "Test.txt"
                            );
                            result.Errors.Add(textUploader.Errors);

                            break;
                        case CustomUploaderDestinationType.FileUploader:

                            CustomFileUploader fileUploader = new CustomFileUploader(item);
                            fileUploader.Upload(Resources.LogoStream, "Test.png");
                            result.Errors.Add(fileUploader.Errors);
                            break;
                        case CustomUploaderDestinationType.URLShortener:
                            CustomURLShortener urlShortener = new CustomURLShortener(item);
                            urlShortener.ShortenURL(Links.Website);
                            result.Errors.Add(urlShortener.Errors);
                            break;
                        case CustomUploaderDestinationType.URLSharingService:
                            CustomURLSharer urlSharer = new CustomURLSharer(item);
                            urlSharer.ShareURL(Links.Website);
                            result.Errors.Add(urlSharer.Errors);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                result.Errors.Add(e.Message);
                // Do not use WriteException to avoid sending data to Sentry
                DebugHelper.Logger?.Error("Error during test upload: " + e.ToString());
            }
        });
        if (result?.Errors.Count != 0)
        {
            TaskHelpers.PlayNotificationSoundAsync(NotificationSound.Error);

            var dialog = new ContentDialog
            {
                Title = "Test Upload Errors",
                Content = new ScrollViewer
                {
                    MaxHeight = 300,
                    Content = new SelectableTextBlock
                    {
                        Text = result?.Errors.ToString(),
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 450,
                    },
                },
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,
            };
            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    private void RemoveUploader(CustomUploaderItem uploader)
    {
        Uploaders.Remove(uploader);
    }

    [RelayCommand]
    private void RemoveHeader(HeaderItem header)
    {
        if (SelectedUploader == null || SelectedUploader.Headers == null)
            return;
        SelectedUploader.Headers.Remove(header);
    }

    [RelayCommand]
    private void RemoveParameter(HeaderItem header)
    {
        if (SelectedUploader == null || SelectedUploader.Parameters == null)
            return;
        SelectedUploader.Parameters.Remove(header);
    }

    [RelayCommand]
    private void RemoveArgument(HeaderItem header)
    {
        if (SelectedUploader == null || SelectedUploader.Arguments == null)
            return;
        SelectedUploader.Arguments.Remove(header);
    }

    [RelayCommand]
    private void DuplicateUploader(CustomUploaderItem uploader)
    {
        string originalName =
            uploader.Name ?? URLHelpers.GetHostName(uploader?.RequestURL) ?? "Custom Uploader";

        var match = CopyNameRegex().Match(originalName);

        var rootName = match.Success ? match.Groups[1].Value : originalName;

        var baseNameWithCopy = $"{rootName} - Copy";
        var newName = baseNameWithCopy;
        var counter = 2;

        while (
            Uploaders.Any(x => x.Name?.Equals(newName, StringComparison.OrdinalIgnoreCase) ?? false)
        )
        {
            newName = $"{baseNameWithCopy} ({counter})";
            counter++;
        }
        var json = JsonHelpers.SerializeToString(uploader);
        var newItem = JsonHelpers.DeserializeFromString<CustomUploaderItem>(json);
        newItem.Name = newName;
        Uploaders.Add(newItem);
        SelectedUploader = newItem;
    }

    [RelayCommand]
    private void AddHeader()
    {
        if (SelectedUploader == null)
            return;
        SelectedUploader.Headers ??= [];
        // Crucial: If the UI was bound to a null collection, it needs to know
        // that the 'Headers' property itself is now a real object.
        OnPropertyChanged(nameof(SelectedUploader));
        SelectedUploader.Headers.Add("", "");
    }

    [RelayCommand]
    private void AddParameter()
    {
        if (SelectedUploader == null)
            return;
        SelectedUploader.Parameters ??= [];
        SelectedUploader.Parameters.Add("", "");
    }

    [RelayCommand]
    private void AddArgument()
    {
        if (SelectedUploader == null)
            return;
        SelectedUploader.Arguments ??= [];
        SelectedUploader.Arguments.Add("", "");
    }

    public void Cleanup()
    {
        if (_isDisposed)
            return;
        // _isDisposed = true;
        // DebugHelper.Logger?.Debug("Cleaning up CustomUploaderVM...");
        // var UploadersConfig = App.SnapX.GetUploadersConfig();
        // UploadersConfig.SettingsSaved -= UploadersConfig_SettingsSaved;
        // Uploaders.CollectionChanged -= OnUploadersChanged;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"(.+?)(?: - Copy(?: \(\d+\))?)$")]
    private static partial System.Text.RegularExpressions.Regex CopyNameRegex();
}
