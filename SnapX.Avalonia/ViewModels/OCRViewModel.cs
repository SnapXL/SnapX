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
    public int GetIndexFromWindowsCode(string windowsCode)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "af", "afr" }, { "am", "amh" }, { "ar", "ara" }, { "az", "aze" },
        { "be", "bel" }, { "bg", "bul" }, { "bn", "ben" }, { "bs", "bos" },
        { "ca", "cat" }, { "cs", "ces" }, { "cy", "cym" }, { "da", "dan" },
        { "de", "deu" }, { "el", "ell" }, { "en", "eng" }, { "es", "spa" },
        { "et", "est" }, { "eu", "eus" }, { "fa", "fas" }, { "fi", "fin" },
        { "fr", "fra" }, { "ga", "gle" }, { "gl", "glg" }, { "gu", "guj" },
        { "hi", "hin" }, { "hr", "hrv" }, { "hu", "hun" }, { "hy", "hye" },
        { "id", "ind" }, { "is", "isl" }, { "it", "ita" }, { "iw", "heb" },
        { "ja", "jpn" }, { "ka", "kat" }, { "kk", "kaz" }, { "km", "khm" },
        { "kn", "kan" }, { "ko", "kor" }, { "lt", "lit" }, { "lv", "lav" },
        { "mk", "mkd" }, { "ml", "mal" }, { "mn", "mon" }, { "mr", "mar" },
        { "ms", "msl" }, { "mt", "mlt" }, { "my", "mya" }, { "ne", "nep" },
        { "nl", "nld" }, { "no", "nor" }, { "pl", "pol" }, { "pt", "por" },
        { "ro", "ron" }, { "ru", "rus" }, { "sk", "slk" }, { "sl", "slv" },
        { "sq", "sqi" }, { "sr", "srp" }, { "sv", "swe" }, { "sw", "swa" },
        { "ta", "tam" }, { "te", "tel" }, { "th", "tha" }, { "tr", "tur" },
        { "uk", "ukr" }, { "ur", "urd" }, { "uz", "uzb" }, { "vi", "vie" },
        { "zh-CN", "chi_sim" }, { "zh-TW", "chi_tra" }
    };

        if (!mapping.TryGetValue(windowsCode, out var internalCode)) return 0;
        for (var i = 0; i < _languages.Length; i++)
        {
            if (_languages[i].Code.Equals(internalCode, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        // We've failed, fuck.
        return 0;
    }

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
