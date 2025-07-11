using Avalonia.Controls;
using Avalonia.Media;
using SnapX.CommonUI.ViewModels;
using SnapX.Core;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class SettingsHomePageView : UserControl
{

    public SettingsHomePageView(SettingsHomePageViewVM vm)
    {
        DataContext = vm;
        InitializeComponent();
        var itemsAsList = FontManager.Current.SystemFonts
            .OrderBy<FontFamily, string>(font => font.Name)
            .ToList();
        FontComboBox.ItemsSource = itemsAsList;
        DebugHelper.WriteLine($"FontComboBox.ItemsSource: {itemsAsList.Count}");
        var defaultFontFamilyName = FontManager.Current.DefaultFontFamily.Name;


        var defaultFontIndex = itemsAsList
            .FindIndex(item => item.Name == defaultFontFamilyName);

        if (defaultFontIndex != -1)
        {
            FontComboBox.SelectedIndex = defaultFontIndex;
            DebugHelper.WriteLine($"Default Font ('{defaultFontFamilyName}') selected at index: {defaultFontIndex}");
        }
        else
        {
            DebugHelper.WriteLine($"Default Font ('{defaultFontFamilyName}') not found in SystemFonts collection.");
        }
    }
    public SettingsHomePageView() : this(new SettingsHomePageViewVM())
    {
    }

    private void FontComboBox_OnSelectionChanged(object? Sender, SelectionChangedEventArgs E)
    {
    }
}

