using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using SnapX.Core;
using SnapX.Core.Upload;
using SnapX.Core.Upload.BaseServices;
using SnapX.Core.Upload.Utils;

namespace SnapX.Avalonia.ViewModels;
/// <summary>
/// A message for deep-linking into specific uploader settings.
/// </summary>
/// <param name="Value">The general category (e.g., ImageUploaders).</param>
/// <param name="TargetUploader">The specific uploader ID (e.g., "Imgur"). Defaults to null.</param>
public class NavigationMessage(UploaderCategory Value, string? TargetUploader = null)
    : ValueChangedMessage<UploaderCategory>(Value);
public partial class CoreUploaderVM : ViewModelBase
{

    public CoreUploaderVM()
    {
        WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) =>
        {
            CurrentCategory = m.Value;
        });
    }
    [ObservableProperty]
    private string _title;

    [ObservableProperty] private IUploaderService? _selectedUploader;
    [ObservableProperty] private object? _selectedUploaderInstance;
    [ObservableProperty]
    private UploaderCategory _currentCategory;
    [RelayCommand]
    public void NavigateToCategory(UploaderCategory category)
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(category));
        SelectedUploader = null;
        SelectedUploaderInstance = null;
    }
    [RelayCommand]
    public void SelectUploader(object? targetEnum)
    {
        if (targetEnum == null) return;
        var AvailableUploaders = UploaderFactory.AllServices;

        string targetName = targetEnum.ToString() ?? string.Empty;

        var service = AvailableUploaders.FirstOrDefault(u =>
            u.ServiceIdentifier.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
            u.EnumValueObject.Equals(targetEnum));

        if (service != null)
        {
            SelectedUploader = service;
            if (service is IGenericUploaderService genericService)
            {
                var fullUploader = genericService.CreateUploader(SnapX.Core.SnapXL.UploadersConfig, new TaskReferenceHelper());
                // If we couldn't create the uploader (no config),
                // use the service itself as the instance so we can show a UI for it!
                SelectedUploaderInstance = fullUploader is not null ? fullUploader : service;
            }
            else
            {
                SelectedUploaderInstance = targetEnum switch
                {
                    ImageDestination id => UploaderFactory.ImageUploaderServices.GetValueOrDefault(id),
                    TextDestination td => UploaderFactory.TextUploaderServices.GetValueOrDefault(td),
                    FileDestination fd => UploaderFactory.FileUploaderServices.GetValueOrDefault(fd),
                    UrlShortenerType us => UploaderFactory.URLShortenerServices.GetValueOrDefault(us),
                    URLSharingServices ur => UploaderFactory.URLSharingServices.GetValueOrDefault(ur),

                    // 2. Last resort: String-based lookup in AllServices if it's just a string/identifier
                    _ => UploaderFactory.AllServices.FirstOrDefault(u =>
                        u.ServiceIdentifier.Equals(targetEnum.ToString(), StringComparison.OrdinalIgnoreCase))
                };
            }
            DebugHelper.WriteAlways($"Selected Instance: {SelectedUploaderInstance} (Type: {SelectedUploaderInstance?.GetType().Name})");

        }
        else
        {
            DebugHelper.Logger?.Warning("Selected uploader not found, name: {SelectedUploader}", targetEnum);
        }
    }

}
