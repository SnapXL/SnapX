using System.ComponentModel;
using System.Runtime.CompilerServices;
using SnapX.Core;
using SnapX.Core.History;
using SnapX.Core.Media.Services;

namespace SnapX.Avalonia.Models;

public class ListTaskTemplate(Type modelType, HistoryItem task) : INotifyPropertyChanged
{
    public Type ModelType { get; init; } = modelType;
    public HistoryItem task { get; init; } = task;

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

    public override bool Equals(object? obj)
    {
        if (obj is ListTaskTemplate other)
        {
            return EqualityComparer<Type>.Default.Equals(ModelType, other.ModelType)
                && EqualityComparer<HistoryItem>.Default.Equals(task, other.task);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ModelType, task);
    }
}
