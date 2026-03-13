using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FluentAvalonia.UI.Controls;
using SnapX.Avalonia.ViewModels;

namespace SnapX.Avalonia.Views.Settings.Views;

public partial class DatabaseView : UserControl
{
    private readonly DatabaseVM _vm;
    public DatabaseView(DatabaseVM vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
    }
    private async Task<DatabaseVM.PathReplaceResult?> CapturePathReplacePairAsync(Visual visual)
    {
        var oldPath = await CaptureFolderAsync(visual, "Select the OLD folder path to replace");
        if (string.IsNullOrWhiteSpace(oldPath)) return null;

        var suggestedPath = Core.SnapXL.ScreenshotsParentFolder;
        var newPath = await CaptureFolderAsync(visual, "Select the NEW folder path", suggestedPath);

        if (string.IsNullOrWhiteSpace(newPath)) return null;

        return new DatabaseVM.PathReplaceResult(oldPath, newPath);
    }

    private async Task<string?> CaptureFolderAsync(Visual visual, string title, string? suggestedPath = null)
    {
        var topLevel = TopLevel.GetTopLevel(visual);
        if (topLevel == null) return null;

        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        if (!string.IsNullOrWhiteSpace(suggestedPath))
        {
            try
            {
                options.SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(suggestedPath);
            }
            catch
            {
                // If the path doesn't exist yet, the picker will just open to the default system folder
            }
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

        return folders?.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    private async Task<bool> ShowConfirmationDialogAsync(DatabaseVM.PathUpdateConfirmationMessage m)
    {
        var dialog = new ContentDialog
        {
            Title = "Confirm Path Mass-Update",
            PrimaryButtonText = "Apply Changes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        var stack = new StackPanel { Spacing = 10, Width = 600 };

        stack.Children.Add(new TextBlock
        {
            Text = $"⚠️ WARNING: This will update {m.TotalCount} records.",
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.Red
        });

        stack.Children.Add(new TextBlock
        {
            Text = "There are NO BACKUPS performed by this function. Use it well, use it wisely.",
            TextWrapping = TextWrapping.Wrap
        });

        var grid = new DataGrid
        {
            ItemsSource = m.Previews,
            AutoGenerateColumns = true,
            Height = 300,
            IsReadOnly = true,
            GridLinesVisibility = DataGridGridLinesVisibility.All
        };

        stack.Children.Add(new ScrollViewer { Content = grid });
        dialog.Content = stack;

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
    private void StyledElement_OnInitialized(object? Sender, EventArgs E)
    {
        WeakReferenceMessenger.Default.Register<DatabaseView, DatabaseVM.PathReplaceRequestMessage>(this, (r, m) =>
        {
            m.Reply(CapturePathReplacePairAsync(r));
        });

        WeakReferenceMessenger.Default.Register<DatabaseView, DatabaseVM.PathUpdateConfirmationMessage>(this, (r, m) =>
        {
            m.Reply(ShowConfirmationDialogAsync(m));
        });
        _vm.RefreshCommand.Execute(E);
        ScheduleColumnUpdate();
        _vm.Items.CollectionChanged += (_, __) => ScheduleColumnUpdate();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.AllColumnNames))
                ScheduleColumnUpdate();
        };

    }
    private CancellationTokenSource? _columnUpdateCts;

    private void ScheduleColumnUpdate()
    {
        // _columnUpdateCts?.Cancel();
        // _columnUpdateCts = new CancellationTokenSource();
        // var token = _columnUpdateCts.Token;
        //
        // _ = Task.Delay(100, token).ContinueWith(async t =>
        // {
        //     if (!t.IsCanceled)
        //         await UpdateDataGridColumnsAsync(MainDataGrid);
        // }, TaskScheduler.Default);
    }

    private async Task UpdateDataGridColumnsAsync(DataGrid grid)
    {
        var allColumns = _vm.AllColumnNames.ToArray();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existingHeaders = grid.Columns
                .Select(c => c.Header?.ToString())
                .Where(h => h != null)
                .ToList()!;

            foreach (var col in grid.Columns.OfType<DataGridTextColumn>().ToList())
            {
                var header = col.Header?.ToString();
                if (header != null && !allColumns.Contains(header, StringComparer.OrdinalIgnoreCase))
                {
                    grid.Columns.Remove(col);
                }
            }

            foreach (var colName in allColumns)
            {
                if (!existingHeaders.Contains(colName, StringComparer.OrdinalIgnoreCase))
                {
                    // var binding = new CompiledBindingExtension
                    // {
                    //     Path = new PropertyPath($"Columns[{colName}]"),
                    //     // 3. Provide a strongly-typed function to get the data
                    //     // This bypasses the need for CompiledBindingPath strings entirely
                    //     Converter = new FuncValueConverter<DatabaseRow, object>(item =>
                    //     {
                    //         if (item != null && item.Columns.TryGetValue(colName, out var value))
                    //         {
                    //             return value;
                    //         }
                    //         return null;
                    //     }),
                    //     Mode = BindingMode.OneWay
                    // };
                    // grid.Columns.Add(new DataGridTextColumn
                    // {
                    //     Header = colName,
                    //     Binding = binding
                    // });
                }
            }

            if (!Equals(grid.ItemsSource, _vm.Items))
                grid.ItemsSource = _vm.Items;

            grid.SelectedItem = _vm.SelectedItem;
        });
    }




}

