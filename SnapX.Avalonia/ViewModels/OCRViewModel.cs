using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SnapX.Core.History;
using SnapX.Core.Job;
using SnapX.Core.Utils;
using Image = SixLabors.ImageSharp.Image;

namespace SnapX.Avalonia.ViewModels;

public partial class OCRViewModel : ViewModelBase
{
    public static readonly (string Display, string Code)[] _languages =
    [
        ("English", "eng"),
        ("Chinese (Simplified)", "chi_sim"),
        ("Chinese (Traditional)", "chi_tra"),
        ("Spanish", "spa"),
        ("Arabic", "ara"),
        ("French", "fra"),
        ("Russian", "rus"),
        ("German", "deu"),
        ("Japanese", "jpn"),
        ("Portuguese", "por"),
        ("Korean", "kor"),
        ("Telugu", "tel"),
        ("Hindi (Devanagari)", "hin"),
        ("Kannada", "kan"),
        ("Tamil", "tam"),
        ("Turkish", "tur"),
    ];

    [ObservableProperty]
    public int selectedLanguageIndex;
    public ObservableCollection<string> LanguageDisplayNames { get; } = new(_languages.Select(l => l.Display));

    public string GetLanguageCode(int index) => _languages[index].Code;

    public async Task<string> RunOCRAsync(HistoryItem? Item = null, string? languageCode = null)
    {
        return await Task.Factory.StartNew(async () =>
            {
                Image? img = null;

                if (Item?.FilePath is null && Item?.BestImageSource is not null)
                {
                    img = await WebHelpers.DownloadImageAsync(Item.BestImageSource);
                }

                if (img is null && Item?.FilePath is not null)
                {
                    return await TaskHelpers.OCRImage(null, Item.FilePath, TaskSettings.GetDefaultTaskSettings(), languageCode);
                }

                return await TaskHelpers.OCRImage(img, null, TaskSettings.GetDefaultTaskSettings(), languageCode);
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).GetAwaiter().GetResult();
    }

}
