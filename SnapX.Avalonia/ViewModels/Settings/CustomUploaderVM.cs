using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnapX.Core;
using SnapX.Core.Upload.Custom;

namespace SnapX.Avalonia.ViewModels;

public partial class CustomUploaderVM : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<CustomUploaderItem> uploaders = [];
    [ObservableProperty] private CustomUploaderItem? selectedUploader;
    partial void OnSelectedUploaderChanged(CustomUploaderItem? oldValue, CustomUploaderItem? newValue)
    {
        DebugHelper.WriteLine("OnSelectedUploaderChanged");
        if (newValue == null)
        {
            Uploaders.Remove(oldValue);
            return;
        }

        // If newValue is not in the collection yet, do nothing
        if (!Uploaders.Contains(newValue))
        {
            return;
        }

        // Raise collection notification by "refreshing" the item
        var index = Uploaders.IndexOf(newValue);
        if (index >= 0)
        {
            // Replace it with itself to trigger ObservableCollection change
            Uploaders[index] = newValue;
        }
    }
    public CustomUploaderVM()
    {
        var UploadersConfig = App.SnapX.GetUploadersConfig();
        foreach (var uploader in UploadersConfig.CustomUploadersList)
        {
            Uploaders.Add(uploader);
        }
        Uploaders.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (CustomUploaderItem item in e.NewItems)
                    UploadersConfig.CustomUploadersList.Add(item);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (CustomUploaderItem item in e.OldItems)
                    UploadersConfig.CustomUploadersList.Remove(item);
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                UploadersConfig.CustomUploadersList.Clear();
                foreach (var item in Uploaders)
                    UploadersConfig.CustomUploadersList.Add(item);
            }
        };
        if (UploadersConfig.CustomImageUploaderSelected > -1)
        {
            SelectedUploader = Uploaders[UploadersConfig.CustomImageUploaderSelected] ?? null;
        }
        else
        {
            SelectedUploader = Uploaders.FirstOrDefault();
        }
    }

    public async Task InitializeAsync()
    {
    }
    [RelayCommand]
    private void RemoveUploader(string uploaderName)
    {
        var uploader = Uploaders.FirstOrDefault(u => u.Name == uploaderName);
        if (uploader is not null)
        {
            Uploaders.Remove(uploader);
        }
    }

    [RelayCommand]
    private void RemoveUploaderItem()
    {
        if (selectedUploader is not null)
            Uploaders.Remove(selectedUploader);
    }

    [RelayCommand]
    private void DuplicateUploader()
    {
        if (selectedUploader is not null)
            Uploaders.Add(new CustomUploaderItem
            {
                Name = selectedUploader.Name + " Copy",
                RequestURL = selectedUploader.RequestURL,
                Headers = selectedUploader.Headers != null ? new Dictionary<string, string?>(selectedUploader.Headers) : new Dictionary<string, string?>(),
                Parameters = selectedUploader.Parameters != null ? new Dictionary<string, string?>(selectedUploader.Parameters) : new Dictionary<string, string?>(),
                Arguments = selectedUploader.Arguments != null ? new Dictionary<string, string?>(selectedUploader.Arguments) : new Dictionary<string, string?>(),
                Body = selectedUploader.Body,
                RequestMethod = selectedUploader.RequestMethod
            });
    }
    [RelayCommand]
    private void AddHeader()
    {
        if (selectedUploader == null) return;
        selectedUploader.Headers ??= new();
        selectedUploader.Headers["New-Header"] = "";
    }

    [RelayCommand]
    private void AddParameter()
    {
        if (selectedUploader == null) return;
        selectedUploader.Parameters ??= new();
        selectedUploader.Parameters["newParam"] = "";
    }


}
