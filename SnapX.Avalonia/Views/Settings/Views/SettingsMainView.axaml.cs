using System.Collections;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Upload;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class SettingsMainView : UserControl
{
    private readonly SettingsMainViewVM? _vm;
    public SettingsMainView() : this(new SettingsMainViewVM()) { }
    public SettingsMainView(SettingsMainViewVM viewModel)
    {
        DataContext = viewModel;
        _vm = viewModel;
        InitializeComponent();
    }

    private void FindURLOnDescendant(ILogical control)
    {
        foreach (var child in control.GetLogicalChildren())
        {
            var toolTip = child.FindLogicalDescendantOfType<ToolTip>(true);
            if (toolTip is null)
            {
                FindURLOnDescendant(child);
            }

            var url = toolTip?.Content as string ?? string.Empty;
            if (!string.IsNullOrEmpty(url)) URLHelpers.OpenURL(url);
        }
    }
    private void DynamicURL_OnPointerPressed(object? Sender, PointerPressedEventArgs E)
    {
        DebugHelper.WriteLine($"{nameof(DynamicURL_OnPointerPressed)}: {Sender} {E.Source}");
        if (Sender is Control control)
        {
            // The ToolTip class has a storage of loaded tooltips, however, when a user clicks without hovering for a second the button didn't work.
            // So I added the second if-clause.
            if (ToolTip.GetTip(control) is string url)
            {
                URLHelpers.OpenURL(url);
                return;
            }

            FindURLOnDescendant(control);
        }
        else
        {
            DebugHelper.WriteLine(
                $"{nameof(DynamicURL_OnPointerPressed)} called with {Sender} which is not a Control!!");
        }
    }
    private void DynamicFolder_OnPointerPressed(object? Sender, PointerPressedEventArgs E)
    {
        DebugHelper.WriteLine($"{nameof(DynamicFolder_OnPointerPressed)}: {Sender} {E.Source}");
        if (Sender is Control control)
        {
            // The ToolTip class has a storage of loaded tooltips, however, when a user clicks without hovering for a second the button didn't work.
            // So I added the second if-clause.
            if (ToolTip.GetTip(control) is string path)
            {
                FileHelpers.OpenFolder(path);
                return;
            }

            FindPathOnDescendant(control);
        }
        else
        {
            DebugHelper.WriteLine(
                $"{nameof(DynamicFolder_OnPointerPressed)} called with {Sender} which is not a Control!!"
            );
        }
    }
    private void FindPathOnDescendant(ILogical control)
    {
        foreach (var child in control.GetLogicalChildren())
        {
            var toolTip = child.FindLogicalDescendantOfType<ToolTip>(true);
            if (toolTip is null)
            {
                FindPathOnDescendant(child);
            }

            var path = toolTip?.Content as string ?? string.Empty;
            if (!string.IsNullOrEmpty(path))
                FileHelpers.OpenFolder(path);
        }
    }

    public void RefreshDestinationChecks(MenuFlyout flyout)
    {
        var config = SnapXL.Settings;
        if (config == null || flyout == null) return;

        var settings = config.DefaultTaskSettings;

        foreach (var item in flyout.Items)
        {
            if (item is MenuItem categoryItem)
            {
                foreach (var subObject in categoryItem.Items)
                {
                    if (subObject is MenuItem subItem && subItem.Tag != null)
                    {
                        bool isSelected = subItem.Tag switch
                        {
                            ImageDestination img => settings.ImageDestination == img,
                            TextDestination text => settings.TextDestination == text,
                            FileDestination file => settings.FileDestination == file,
                            UrlShortenerType shortener => settings.URLShortenerDestination == shortener,
                            URLSharingServices sharing => settings.URLSharingServiceDestination == sharing,
                            _ => false
                        };

                        subItem.Icon = isSelected ? new SymbolIcon { Symbol = Symbol.Accept, FontSize = 16 } : null;
                    }
                }
            }
        }
    }
    public void PopulateDestinations(MenuFlyout flyout)
    {
        if (flyout == null)
        {
            DebugHelper.WriteLine("Populating destinations: Flyout is null, aborting.");
            return;
        }

        flyout.Items.Clear();

        try
        {
            flyout.Items.Add(CreateCategory("Image Uploaders", UploaderFactory.ImageUploaderServices));
            flyout.Items.Add(CreateCategory("Text Uploaders", UploaderFactory.TextUploaderServices));
            flyout.Items.Add(CreateCategory("File Uploaders", UploaderFactory.FileUploaderServices));
            flyout.Items.Add(CreateCategory("URL Shorteners", UploaderFactory.URLShortenerServices));
            flyout.Items.Add(CreateCategory("URL Sharing", UploaderFactory.URLSharingServices));
            DebugHelper.WriteLine("Populating destinations: Completed successfully");
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Populating destinations: Failed to populate menu. Error: {ex.Message}");
        }
    }

    private MenuItem CreateCategory<TKey, TService>(string header, Dictionary<TKey, TService> services) where TKey : Enum
    {

        var category = new MenuItem { Header = header };
        var settings = SnapXL.Settings?.DefaultTaskSettings;

        if (settings == null)
        {
            DebugHelper.WriteLine($"CreateCategory: Settings or DefaultTaskSettings is null for '{header}'");
        }

        if (services == null || services.Count == 0)
        {
            DebugHelper.WriteLine($"CreateCategory: No services found for category '{header}'");
            return category;
        }

        foreach (var kvp in services)
        {
            var description = kvp.Key.GetDescription();
            var item = new MenuItem
            {
                Header = description,
                Tag = kvp.Key
            };

            item.Click += OnDestinationClick;

            var isSelected = kvp.Key switch
            {
                ImageDestination img => settings?.ImageDestination == img,
                TextDestination text => settings?.TextDestination == text,
                FileDestination file => settings?.FileDestination == file,
                UrlShortenerType shortener => settings?.URLShortenerDestination == shortener,
                URLSharingServices sharing => settings?.URLSharingServiceDestination == sharing,
                _ => false
            };

            if (isSelected)
            {
                DebugHelper.WriteLine($"CreateCategory: Marking '{description}' as selected");
                item.Icon = new SymbolIcon { Symbol = Symbol.Accept, FontSize = 16 };
            }

            category.Items.Add(item);
        }

        return category;
    }


    private void OnDestinationClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || menuItem.Tag == null) return;
        var config = SnapXL.Settings;
        if (config == null) return;

        var destination = menuItem.Tag;

        switch (destination)
        {
            case ImageDestination img:
                config.DefaultTaskSettings.ImageDestination = img;
                break;
            case TextDestination text:
                config.DefaultTaskSettings.TextDestination = text;
                break;
            case FileDestination file:
                config.DefaultTaskSettings.FileDestination = file;
                break;
            case UrlShortenerType shortener:
                config.DefaultTaskSettings.URLShortenerDestination = shortener;
                break;
            case URLSharingServices sharing:
                config.DefaultTaskSettings.URLSharingServiceDestination = sharing;
                break;
        }

        UpdateMenuSelectionFeedback(menuItem);
    }

    private void UpdateMenuSelectionFeedback(MenuItem selectedItem)
    {
        if (selectedItem.Parent is not MenuItem parentGroup) return;
        foreach (var item in parentGroup.Items.OfType<MenuItem>())
        {
            item.Icon = null;
        }

        selectedItem.Icon = new SymbolIcon { Symbol = Symbol.Accept, FontSize = 16 };
    }
    private NavigationViewItem? GetParentItem(IEnumerable? items, NavigationViewItem target)
    {
        if (items == null) return null;

        foreach (var obj in items)
        {
            if (obj is not NavigationViewItem parent) continue;
            if (parent.MenuItems.Cast<object>().Contains(target))
            {
                return parent;
            }
            var found = GetParentItem(parent.MenuItems, target);
            if (found != null) return found;
        }

        return null;
    }
    private void NavigationView_OnSelectionChanged(object? Sender, NavigationViewSelectionChangedEventArgs E)
    {
        try
        {
            if (Sender is not NavigationView navigationView) return;
            navigationView.IsBackEnabled = _vm?.CanGoBack ?? true;
            if (E.SelectedItem is not NavigationViewItem item)
            {
                DebugHelper.WriteLine("NavigationView_OnSelectionChanged.Sender.SelectedItem is null");
                return;
            }
            DebugHelper.WriteLine($"{nameof(NavigationView_OnSelectionChanged)}: {item}");
            var ItemTag = item.Tag?.ToString();
            if (item.Tag is Enum and not UploaderCategory) ItemTag = "!" + item.Tag;

            if (string.IsNullOrEmpty(ItemTag))
            {
                DebugHelper.WriteLine("NavigationView_OnSelectionChanged: Tag is null or empty");
                return;
            }
            var parentItem = GetParentItem(SettingsNavigationView.MenuItems, item);
            var categoryTag = parentItem?.Tag?.ToString();
            _vm?.Navigate(categoryTag, ItemTag);
            WeakReferenceMessenger.Default.Send(new ChangeWindowTitleRequest(_vm?.PageTitle));
            navigationView.IsBackEnabled = _vm?.CanGoBack ?? true;
        }
        catch (Exception e)
        {
            e.ShowError();
        }
    }

    private void NavigationView_OnBackRequested(object? Sender, NavigationViewBackRequestedEventArgs E)
    {
        _vm?.Back();
        if (Sender is not NavigationView MyNavigationView) return;

        if (_vm?.CurrentPage is not null &&
            _vm.TryGetPage(_vm.CurrentPage.GetType().Name, out var targetType))
        {
            var item = MyNavigationView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(x =>
                    x.Tag is string tag &&
                    _vm.TryGetPage(tag, out var type) &&
                    type == targetType);

            MyNavigationView.SelectedItem = item ?? null;
        }
        MyNavigationView.IsBackEnabled = _vm?.CanGoBack ?? true;
    }

    private void PopupFlyoutBase_OnClosing(object? sender, CancelEventArgs e)
    {
        DebugHelper.WriteLine($"{sender} OnClosing");

        if (sender is not FlyoutBase fb) return;
        var topLevel = TopLevel.GetTopLevel(fb.Target);
        var focus = topLevel?.FocusManager?.GetFocusedElement();

        if (focus is Control { IsPointerOver: true } c && (c is MenuItem || c is ToggleMenuFlyoutItem))
        {
            var tag = c.Tag?.ToString();
            if (!string.IsNullOrWhiteSpace(tag))
            {
                e.Cancel = true;
                DebugHelper.WriteLine($"{nameof(PopupFlyoutBase_OnClosing)} cancelled - keeping menu open.");
                return; // user is still clicking
            }
        }

        if (_saveCancellation != null)
        {
            _saveCancellation.Cancel();
            _saveCancellation = null;

            DebugHelper.WriteLine("Menu closing: Flushing pending save.");
            SnapXL.Settings?.SaveAsync();
        }

        DebugHelper.WriteLine($"{nameof(PopupFlyoutBase_OnClosing)} NOT cancelled - menu closing.");
    }
    private CancellationTokenSource? _saveCancellation;
    private async void DynamicSettingItemOnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not ToggleMenuFlyoutItem toggle) return;
            var key = toggle.Tag?.ToString();
            if (string.IsNullOrWhiteSpace(key)) return;
            if (key.StartsWith('!')) key = key[1..];

            var changed = false;

            if (Enum.TryParse(key, out AfterCaptureTasks flagCapture) && flagCapture != AfterCaptureTasks.None)
            {
                var currentlyHasFlag = (SnapXL.Settings?.DefaultTaskSettings.AfterCaptureJob & flagCapture) == flagCapture;

                if (currentlyHasFlag)
                    SnapXL.Settings?.DefaultTaskSettings.AfterCaptureJob &= ~flagCapture;
                else
                    SnapXL.Settings?.DefaultTaskSettings.AfterCaptureJob |= flagCapture;

                // toggle.IsChecked = !currentlyHasFlag;
                SnapXL.Settings?.DefaultTaskSettings.UseDefaultAfterCaptureJob = false;
                changed = true;
                DebugHelper.WriteLine($"Updated AfterCaptureJob: {key} toggled to {toggle.IsChecked}");
            }
            else if (Enum.TryParse(key, out AfterUploadTasks flagUpload) && flagUpload != AfterUploadTasks.None)
            {
                var currentlyHasFlag = (SnapXL.Settings?.DefaultTaskSettings.AfterUploadJob & flagUpload) == flagUpload;

                if (currentlyHasFlag)
                    SnapXL.Settings?.DefaultTaskSettings.AfterUploadJob &= ~flagUpload;
                else
                    SnapXL.Settings?.DefaultTaskSettings.AfterUploadJob |= flagUpload;

                // toggle.IsChecked = !currentlyHasFlag;
                SnapXL.Settings?.DefaultTaskSettings.UseDefaultAfterUploadJob = false;
                changed = true;
                DebugHelper.WriteLine($"Updated AfterUploadJob: {key} toggled to {toggle.IsChecked}");
            }

            if (!changed) return;
            // Using async here will trigger bugs.
            // ReSharper disable once MethodHasAsyncOverload
            _saveCancellation?.Cancel();
            _saveCancellation = new CancellationTokenSource();
            var token = _saveCancellation.Token;

            try
            {
                await Task.Delay(5000, token);
                SnapXL.Settings?.SaveAsync();
                DebugHelper.WriteLine("Debounce complete. Settings saved.");
            }
            catch (TaskCanceledException)
            {
                DebugHelper.WriteLine("Save debounced.");
            }
        }
        catch (Exception ex)
        {
            ex.ShowError();
        }
    }
    private void OnDestinationsDropDownClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is DropDownButton ddb && ddb.Flyout is MenuFlyout flyout)
        {
            PopulateDestinations(flyout);
        }
    }
    private void PopupFlyoutBase_OnOpening(object? sender, EventArgs e)
    {
        if (sender is MenuFlyout menuFlyout)
        {
            RefreshDestinationChecks(menuFlyout);
        }
        if (sender is not FAMenuFlyout flyout) return;
        foreach (var item in flyout.Items)
        {
            InitializeMenuItem(item);
        }
    }

    private void InitializeMenuItem(object? item)
    {
        switch (item)
        {
            case ToggleMenuFlyoutItem toggle:
                {
                    var key = toggle.Tag?.ToString();
                    if (key?.StartsWith('!') ?? false) key = key[1..];
                    if (string.IsNullOrWhiteSpace(key)) return;

                    var isEnabled = false;

                    if (Enum.TryParse(key, out AfterCaptureTasks flagCapture) && flagCapture != AfterCaptureTasks.None)
                    {
                        isEnabled = (SnapXL.Settings?.DefaultTaskSettings.AfterCaptureJob & flagCapture) == flagCapture;
                    }
                    else if (Enum.TryParse(key, out AfterUploadTasks flagUpload) && flagUpload != AfterUploadTasks.None)
                    {
                        isEnabled = (SnapXL.Settings?.DefaultTaskSettings.AfterUploadJob & flagUpload) == flagUpload;
                    }

                    toggle.IsChecked = isEnabled;
                    DebugHelper.WriteLine($"Syncing {key}: {isEnabled}");
                    break;
                }
            case MenuItem menuItem:
                {
                    foreach (var subItem in menuItem.Items)
                    {
                        InitializeMenuItem(subItem);
                    }

                    break;
                }
        }
    }
    private void StyledElement_OnInitialized(object? Sender, EventArgs E)
    {

    }
}

public class ChangeWindowTitleRequest(string? Title)
{
    public string? Title { get; set; } = Title;
}

