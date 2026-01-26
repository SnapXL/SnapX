using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DotNext.Threading;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.Views.Settings;
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
using HttpClientFactory = SnapX.Core.Utils.Miscellaneous.HttpClientFactory;

namespace SnapX.Avalonia.ViewModels;

public partial class CustomUploaderVM : ViewModelBase
{
    private readonly AsyncLock _collectionLock = new();
    private bool _isDisposed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUiEnabled))]
    private bool _isInitializing = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUiEnabled))]
    private bool _isLoading = true;

    [ObservableProperty] private CustomUploaderItem? _selectedFileUploader;

    [ObservableProperty] private CustomUploaderItem? _selectedImageUploader;

    [ObservableProperty] private CustomUploaderItem? _selectedSharingUploader;

    [ObservableProperty] private CustomUploaderItem? _selectedShortenerUploader;

    [ObservableProperty] private CustomUploaderItem? _selectedTextUploader;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DynamicWatermark))]
    [NotifyPropertyChangedFor(nameof(ImageUploaders))]
    [NotifyPropertyChangedFor(nameof(FileUploaders))]
    [NotifyPropertyChangedFor(nameof(TextUploaders))]
    [NotifyPropertyChangedFor(nameof(ShortenerUploaders))]
    [NotifyPropertyChangedFor(nameof(SharingUploaders))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private CustomUploaderItem? selectedUploader;

    public CustomUploaderVM()
    {
        IsInitializing = true;
        IsLoading = true;
        var UploadersConfig = App.SnapX.GetUploadersConfig();
        UploadersConfig.SettingsSaved += UploadersConfig_SettingsSaved;
    }

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
        HttpMethod.Connect
    ];

    public bool IsUiEnabled => !IsLoading && !IsInitializing;

    public string DynamicWatermark =>
        !string.IsNullOrWhiteSpace(SelectedUploader?.RequestURL)
            ? URLHelpers.GetHostName(SelectedUploader.RequestURL)
            : "Name";

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
            DefaultButton = ContentDialogButton.Primary
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
                await Task.Run(() => TaskHelpers.ImportCustomUploaderJson(validJson));
            }

            IsLoading = false;
            TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
        }
    }

    private async Task<string?> DownloadUploaderJson(string url)
    {
        try
        {
            var client = HttpClientFactory.Get();
            return await client.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            DebugHelper.Logger?.Error("Failed to download uploader json");
            DebugHelper.WriteException(ex);
            return null;
        }
    }

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
            var oldIndex = Uploaders.IndexOf(SelectedUploader!);
            var oldCount = Uploaders.Count;
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
                if (Uploaders.Count > oldCount)
                {
                    // Likely an import occurred
                    SelectedUploader = Uploaders.LastOrDefault();
                    DebugHelper.WriteLine($"[Propagate] Collection grew. Selected last item: '{SelectedUploader?.Name}'");
                }
                else if (oldIndex >= 0 && oldIndex < Uploaders.Count)
                {
                    // Routine refresh, keep the user on the same row
                    SelectedUploader = Uploaders[oldIndex];
                    DebugHelper.WriteLine($"[Propagate] Restored selection to index {oldIndex}: '{SelectedUploader?.Name}'");
                }
                else
                {
                    SelectedUploader = Uploaders.LastOrDefault();
                    DebugHelper.WriteLine("[Propagate] Index invalid or item removed. Defaulting to LastOrDefault.");
                }
                DebugHelper.WriteLine($"Propagated {Uploaders.Count} custom uploaders.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnUploadersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isDisposed)
            return;
        using (await _collectionLock.AcquireAsync(CancellationToken.None))
        {
            var configList = App.SnapX.GetUploadersConfig().CustomUploadersList;
            DebugHelper.Logger?.Debug(
                "CustomUploaderVM: Uploaders collection change: "
                + e.Action
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
                        var insertIndex =
                            e.NewStartingIndex >= 0 && e.NewStartingIndex <= configList.Count
                                ? e.NewStartingIndex
                                : configList.Count;

                        for (var i = 0; i < e.NewItems.Count; i++)
                        {
                            configList.Insert(insertIndex + i, (CustomUploaderItem)e.NewItems[i]!);
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (
                        e is { OldItems: not null, OldStartingIndex: >= 0 }
                        && e.OldStartingIndex < configList.Count
                    )
                    {
                        for (var i = 0; i < e.OldItems.Count; i++)
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
                        && e is { OldItems: not null, OldStartingIndex: >= 0 }
                        && e.OldStartingIndex < configList.Count
                    )
                    {
                        for (var i = 0; i < e.NewItems.Count; i++)
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
                        e is { OldItems: not null, OldStartingIndex: >= 0 }
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
                    configList.AddRange(Uploaders);

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
                targetIndex >= 0 && targetIndex < Uploaders.Count
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
                    MaxWidth = 400
                },
                PrimaryButtonText = "Copy",
                CloseButtonText = "Close"
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

    // ReSharper disable once AsyncVoidMethod
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
        var newItem = new CustomUploaderItem
        {
            Headers = [],
            Parameters = []
        };
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
                        MimeTypes = ["application/json"]
                    }
                ]
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
            await Task.Run(() => TaskHelpers.ImportCustomUploader(files.Select(f => f.Path.LocalPath)));
            IsLoading = false;
            TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
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
                    MaxWidth = 400
                },
                CloseButtonText = "Close"
            };
            await dialog.ShowAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportUploader(CustomUploaderItem item)
    {
        await InternalExportLogic(item);
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
                CloseButtonText = "OK"
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
                CloseButtonText = "OK"
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
                            MimeTypes = ["application/json"]
                        }
                    ]
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
                    MaxWidth = 400
                },
                CloseButtonText = "Close"
            };
            await dialog.ShowAsync();
        }
    }

    [RelayCommand]
    private void ToggleDestinationType(CustomUploaderDestinationType flag)
    {
        DebugHelper.WriteLine("Toggling destination type: " + flag);
        if (SelectedUploader == null)
            return;

        SelectedUploader.DestinationType ^= flag;
        OnPropertyChanged(nameof(SelectedUploader.DestinationType));
        OnPropertyChanged(nameof(SelectedUploader));
        OnPropertyChanged(nameof(ImageUploaders));
        OnPropertyChanged(nameof(FileUploaders));
        OnPropertyChanged(nameof(TextUploaders));
        OnPropertyChanged(nameof(ShortenerUploaders));
        OnPropertyChanged(nameof(SharingUploaders));
    }

    public bool IsTypeSelected(CustomUploaderDestinationType flag)
    {
        DebugHelper.WriteLine("Checking if type is selected: " + flag);
        return SelectedUploader?.DestinationType.HasFlag(flag) ?? false;
    }

    [RelayCommand]
    private async Task TestUploader(CustomUploaderItem item)
    {
        var DestinationType = item.DestinationType;
        var testSummary = new List<(CustomUploaderDestinationType Type, UploadResult Result)>();
        DebugHelper.WriteLine($"Testing {item.Name ?? item.GetFileName()} ({DestinationType})");
        WeakReferenceMessenger.Default.Send(
            new NotificationMessage("Test Started", "Bombs away!", NotificationType.Information)
        );

        try
        {
            var flags = Enum.GetValues<CustomUploaderDestinationType>()
                .Where(f =>
                    item.DestinationType.HasFlag(f) && f != CustomUploaderDestinationType.None
                );

            foreach (var type in flags)
            {
                UploadResult? result = null;
                switch (type)
                {
                    case CustomUploaderDestinationType.ImageUploader:
                        {
                            var imageUploader = new CustomImageUploader(item);
                            await using var logo = Resources.LogoStream;
                            result = await Task.Run(() =>
                            {
                                var res = imageUploader.Upload(logo, "Test.png");
                                res.Errors.Add(imageUploader.Errors);
                                return res;
                            });
                            break;
                        }
                    case CustomUploaderDestinationType.TextUploader:
                        {
                            var textUploader = new CustomTextUploader(item);
                            var textBox = new TextBox
                            {
                                AcceptsReturn = true,
                                TextWrapping = TextWrapping.Wrap,
                                Height = 200,
                                Text = "This is a test text upload from SnapX!",
                                Watermark = "Enter text to upload..."
                            };

                            var TextUploadDialog = new ContentDialog
                            {
                                Title = "SnapX text upload test",
                                Content = textBox,
                                PrimaryButtonText = "OK",
                                CloseButtonText = "Cancel",
                                DefaultButton = ContentDialogButton.Primary
                            };

                            var dialogResult = await TextUploadDialog.ShowAsync();

                            if (dialogResult == ContentDialogResult.Primary)
                            {
                                var text = textBox.Text;

                                if (!string.IsNullOrEmpty(text))
                                {
                                    result = await Task.Run(() =>
                                    {
                                        var res = textUploader.UploadText(text, "Test.txt");
                                        res.Errors.Add(textUploader.Errors);
                                        return res;
                                    });
                                }
                            }

                            break;
                        }
                    case CustomUploaderDestinationType.FileUploader:
                        {
                            var fileUploader = new CustomFileUploader(item);
                            await using var logo = Resources.LogoStream;
                            result = await Task.Run(() =>
                            {
                                var res = fileUploader.Upload(logo, "Test.png");
                                res.Errors.Add(fileUploader.Errors);
                                return res;
                            });
                            break;
                        }
                    case CustomUploaderDestinationType.URLShortener:
                        {
                            var urlShortener = new CustomURLShortener(item);
                            result = await Task.Run(() =>
                            {
                                var res = urlShortener.ShortenURL(Links.Website);
                                res.Errors.Add(urlShortener.Errors);
                                return res;
                            });
                            break;
                        }
                    case CustomUploaderDestinationType.URLSharingService:
                        {
                            var urlSharer = new CustomURLSharer(item);
                            result = await Task.Run(() =>
                            {
                                var res = urlSharer.ShareURL(Links.Website);
                                res.Errors.Add(urlSharer.Errors);
                                return res;
                            });
                            break;
                        }
                }

                if (result is not null)
                {
                    testSummary.Add((type, result));
                }
            }
        }
        catch (Exception e)
        {
            if (!testSummary.Any()) testSummary.Add((item.DestinationType, new UploadResult()));
            var result = testSummary.LastOrDefault();
            result.Result.Errors.Add(e.Message);
            // Do not use WriteException to avoid sending data to Sentry
            DebugHelper.Logger?.Error("Error during test upload: " + e);
        }

        await ShowDetailedSummary(testSummary);
    }

    private async Task ShowDetailedSummary(List<(CustomUploaderDestinationType Type, UploadResult Result)> summary)
    {
        var mainStack = new StackPanel { Spacing = 12, Width = 450 };

        foreach (var (type, res) in summary)
        {
            var isSuccess = res is { IsError: false, ResponseInfo.IsSuccess: true };

            var sectionStack = new StackPanel { Spacing = 4 };

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            headerStack.Children.Add(new SymbolIcon
            {
                Symbol = isSuccess ? Symbol.Checkmark : Symbol.Dismiss,
                Foreground = isSuccess ? Brushes.LightGreen : Brushes.OrangeRed,
                FontSize = 18
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = type.ToString(),
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });

            sectionStack.Children.Add(headerStack);

            var infoBorder = new Border
            {
                Padding = new Thickness(12, 8),
                Background = new SolidColorBrush(Color.Parse("#1aFFFFFF")),
                CornerRadius = new CornerRadius(4),
                Child = CreateResultDetailGrid(res)
            };

            sectionStack.Children.Add(infoBorder);
            mainStack.Children.Add(sectionStack);

            if (!isSuccess)
            {
                WeakReferenceMessenger.Default.Send(new NotificationMessage("Test Upload Fail",
                    "Sometimes, success means being the first to fail.", NotificationType.Error));
                TaskHelpers.PlayNotificationSoundAsync(NotificationSound.Error);
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new NotificationMessage("Successful", "Upload complete.",
                    NotificationType.Success));
                TaskHelpers.PlayNotificationSoundAsync(NotificationSound.ActionCompleted);
            }
        }

        var dialog = new ContentDialog
        {
            Title = "Test Upload Summary",
            Content = new ScrollViewer { MaxHeight = 600, Content = mainStack },
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close
        };

        await dialog.ShowAsync();
    }

    private Control CreateResultDetailGrid(UploadResult res)
    {
        var stack = new StackPanel { Spacing = 2 };
        AddDetailRow(stack, "Result", res.ToSummaryString());
        if (res.IsError)
        {
            AddDetailRow(stack, "Errors", res.ErrorsToString());
        }

        AddDetailRow(stack, "Response", res.ResponseInfo.ToReadableString(true));

        return stack;
    }

    private void AddDetailRow(StackPanel parent, string label, string value)
    {
        var panel = new DockPanel { LastChildFill = true };
        panel.Children.Add(new TextBlock
        {
            Text = $"{label}: ",
            Width = 85,
            Opacity = 0.6,
            FontSize = 12
        });
        panel.Children.Add(new SelectableTextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        });
        parent.Children.Add(panel);
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
        var originalName =
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

    [RelayCommand]
    private async Task OpenUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return;
        URLHelpers.OpenURL(url);
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

    [GeneratedRegex(@"(.+?)(?: - Copy(?: \(\d+\))?)$")]
    private static partial Regex CopyNameRegex();
}
