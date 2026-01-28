using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SnapX.Core.History;
using SnapX.Core.Job;
using SnapX.Core.Utils;
using Image = SixLabors.ImageSharp.Image;

namespace SnapX.Avalonia.ViewModels;

public partial class OCRViewModel : ViewModelBase
{
    public static readonly (string Display, string Code)[] _languages = new[]
    {
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
        ("Thai", "tha"),
        ("Greek", "ell"),
        ("Cyrillic / East Slavic", "eslav"),
        ("Latin script", "latin")
    };



    [ObservableProperty]
    public int selectedLanguageIndex;
    public ObservableCollection<string> LanguageDisplayNames { get; } =
        new(_languages.Select(l => l.Display));

    public string GetLanguageCode(int index) => _languages[index].Code;

    public async Task<TaskHelpers.OcrResponse> RunOCRAsync(
        HistoryItem? Item = null,
        Image? Image = null,
        string? languageCode = null,
        Progress<TaskHelpers.OCRProgress>? progressHandler = null,
        CancellationToken cts = default
    )
    {
        return await Task
            .Factory.StartNew(
                async () =>
                {
                    Image? img = Image;

                    if (img is null && Item?.BestImageSource is not null)
                    {
                        if (!Uri.IsWellFormedUriString(Item.BestImageSource, UriKind.Absolute))
                        {
                            // It's likely a file path, so no need to download
                        }
                        else
                        {
                            img = await WebHelpers.DownloadImageAsync(Item.BestImageSource);
                        }
                    }

                    return await TaskHelpers.OCRImageDetailed(
                        img,
                        Item?.BestImageSource,
                        TaskSettings.GetDefaultTaskSettings(),
                        languageCode, progressHandler, cts);
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            )
            .Unwrap();
    }
}
