using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;
using SnapX.Core;
using SnapX.Core.Utils;

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

    private void NavigationView_OnSelectionChanged(object? Sender, NavigationViewSelectionChangedEventArgs E)
    {
        if (Sender is not NavigationView navigationView) return;
        navigationView.IsBackEnabled = _vm?.CanGoBack ?? true;
        if (navigationView.SelectedItem is not NavigationViewItem item)
        {
            DebugHelper.WriteLine("NavigationView_OnSelectionChanged.Sender.SelectedItem is null");
            return;
        }
        DebugHelper.WriteLine($"{nameof(NavigationView_OnSelectionChanged)}: {item}");
        if (item.Tag is not string ItemTag)
        {
            DebugHelper.WriteLine("NavigationView_OnSelectionChanged.Tag is null");
            return;
        }
        _vm?.Navigate(ItemTag);
        navigationView.IsBackEnabled = _vm?.CanGoBack ?? true;
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

            if (item is not null)
                MyNavigationView.SelectedItem = item;
            else MyNavigationView.SelectedItem = null;
        }
        MyNavigationView.IsBackEnabled = _vm?.CanGoBack ?? true;
    }
}

