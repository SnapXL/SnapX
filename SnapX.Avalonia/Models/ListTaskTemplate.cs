using System.ComponentModel;
using System.Runtime.CompilerServices;
using SnapX.Core;
using SnapX.Core.History;
using SnapX.Core.Media.Services;

namespace SnapX.Avalonia.Models;

public record ListTaskTemplate(Type ModelType, HistoryItem task) : INotifyPropertyChanged
{
    private string? _uiDisplaySource;
    private bool _isLoading;

    public string? UIDisplaySource
    {
        get
        {
            if (_uiDisplaySource == null && !_isLoading)
            {
                _ = LoadSourceAsync();
                return task.BestImageSource;
            }
            return _uiDisplaySource;
        }
    }

    private async Task LoadSourceAsync()
    {
        _isLoading = true;
        string? originalSource = task.BestImageSource;

        try
        {
            // DebugHelper.Logger?.Debug($"ListTaskTemplate: Processing source: {originalSource}");

            var result = await ThumbnailService.GetCompatibleSourceAsync(originalSource);

            if (result != _uiDisplaySource)
            {
                _uiDisplaySource = result;
                // DebugHelper.Logger?.Debug($"ListTaskTemplate: Successfully resolved source to: {result}");
                OnPropertyChanged(nameof(UIDisplaySource));
            }
        }
        catch (Exception ex)
        {
            // DebugHelper.Logger?.Debug($"ListTaskTemplate: Error resolving image source: {ex.Message}");
            _uiDisplaySource = originalSource;
        }
        finally
        {
            _isLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
