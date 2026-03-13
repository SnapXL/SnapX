// SPDX-License-Identifier: GPL-3.0-or-later


using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using NeoSolve.ImageSharp.AVIF;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using SnapX.Core.Capture;
using SnapX.Core.CLI;
using SnapX.Core.ImageEffects;
using SnapX.Core.Media;
using SnapX.Core.ScreenCapture;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Upload.SharingServices;
using SnapX.Core.Utils;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using SnapX.Core.Utils.Parsers;
using Xdg.Directories;
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp.Rendering;
using ZXing.QrCode;
using ResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using Size = SixLabors.ImageSharp.Size;
#if !DISABLE_OCR
using RapidOcrNet;
#endif

namespace SnapX.Core.Job;

public static class TaskHelpers
{
    public static async Task ExecuteJob(HotkeyType job, CLICommand command = null)
    {
        await ExecuteJob(SnapXL.DefaultTaskSettings, job, command);
    }

    public static async Task ExecuteJob(TaskSettings taskSettings)
    {
        await ExecuteJob(taskSettings, taskSettings.Job);
    }

    public static async Task ExecuteJob(
        TaskSettings taskSettings,
        HotkeyType job,
        CLICommand command = null
    )
    {
        if (job == HotkeyType.None)
            return;

        DebugHelper.WriteAlways("Executing: " + job.GetLocalizedDescription());

        var safeTaskSettings = TaskSettings.GetSafeTaskSettings(taskSettings);

        switch (job)
        {
            // Upload
            case HotkeyType.FileUpload:
                UploadManager.UploadFile(safeTaskSettings);
                break;
            case HotkeyType.FolderUpload:
                UploadManager.UploadFolder(safeTaskSettings);
                break;
            case HotkeyType.ClipboardUpload:
                UploadManager.ClipboardUpload(safeTaskSettings);
                break;
            case HotkeyType.ClipboardUploadWithContentViewer:
                DebugHelper.WriteException(
                    "HotkeyType.ClipboardUploadWithContentViewer is NOT implemented."
                );
                // UploadManager.ClipboardUploadWithContentViewer(safeTaskSettings);
                break;
            case HotkeyType.UploadText:
                DebugHelper.WriteException("HotkeyType.UploadText is NOT implemented.");
                // UploadManager.ShowTextUploadDialog(safeTaskSettings);

                break;
            case HotkeyType.UploadURL:
                UploadManager.UploadURL(safeTaskSettings);
                break;
            case HotkeyType.DragDropUpload:
                DebugHelper.WriteException("HotkeyType.DragDropUpload is NOT implemented.");
                // OpenDropWindow(safeTaskSettings);
                break;
            case HotkeyType.ShortenURL:
                DebugHelper.WriteException("HotkeyType.ShortenURL is NOT implemented.");
                // UploadManager.ShowShortenURLDialog(safeTaskSettings);
                break;
            case HotkeyType.TweetMessage:
                TweetMessage();
                break;
            case HotkeyType.StopUploads:
                TaskManager.StopAllTasks();
                break;
            // Screen capture
            case HotkeyType.PrintScreen:
                new CaptureFullscreen().Capture(safeTaskSettings);
                break;
            case HotkeyType.ActiveWindow:
                new CaptureActiveWindow().Capture(safeTaskSettings);
                break;
            case HotkeyType.ActiveMonitor:
                new CaptureActiveMonitor().Capture(safeTaskSettings);
                break;
            case HotkeyType.RectangleRegion:
                new CaptureRegion().Capture(safeTaskSettings);
                break;
            case HotkeyType.RectangleLight:
                new CaptureRegion(RegionCaptureType.Light).Capture(safeTaskSettings);
                break;
            case HotkeyType.RectangleTransparent:
                new CaptureRegion(RegionCaptureType.Transparent).Capture(safeTaskSettings);
                break;
            case HotkeyType.CustomRegion:
                new CaptureCustomRegion().Capture(safeTaskSettings);
                break;
            case HotkeyType.CustomWindow:
                new CaptureCustomWindow().Capture(safeTaskSettings);
                break;
            case HotkeyType.LastRegion:
                new CaptureLastRegion().Capture(safeTaskSettings);
                break;
            case HotkeyType.ScrollingCapture:
                DebugHelper.WriteException("HotkeyType.ScrollingCapture is NOT implemented.");
                // await OpenScrollingCapture(safeTaskSettings);
                break;
            case HotkeyType.AutoCapture:
                DebugHelper.WriteException("HotkeyType.AutoCapture is NOT implemented.");
                // OpenAutoCapture(safeTaskSettings);
                break;
            case HotkeyType.StartAutoCapture:
                DebugHelper.WriteException("HotkeyType.StartAutoCapture is NOT implemented.");
                // StartAutoCapture(safeTaskSettings);
                break;
            // Screen record
            case HotkeyType.ScreenRecorder:
                DebugHelper.WriteException("HotkeyType.ScreenRecorder is NOT implemented.");
                // StartScreenRecording(ScreenRecordOutput.FFmpeg, ScreenRecordStartMethod.Region, safeTaskSettings);
                break;
            case HotkeyType.ScreenRecorderActiveWindow:
                DebugHelper.WriteException(
                    "HotkeyType.ScreenRecorderActiveWindow is NOT implemented."
                );
                // StartScreenRecording(ScreenRecordOutput.FFmpeg, ScreenRecordStartMethod.ActiveWindow, safeTaskSettings);
                break;
            case HotkeyType.ScreenRecorderCustomRegion:
                DebugHelper.WriteException(
                    "HotkeyType.ScreenRecorderCustomRegion is NOT implemented."
                );
                // StartScreenRecording(ScreenRecordOutput.FFmpeg, ScreenRecordStartMethod.CustomRegion, safeTaskSettings);
                break;
            case HotkeyType.StartScreenRecorder:
                DebugHelper.WriteException("HotkeyType.StartScreenRecorder is NOT implemented.");
                // StartScreenRecording(ScreenRecordOutput.FFmpeg, ScreenRecordStartMethod.LastRegion, safeTaskSettings);
                break;
            case HotkeyType.ScreenRecorderGIF:
                DebugHelper.WriteException("HotkeyType.ScreenRecorderGIF is NOT implemented.");
                // StartScreenRecording(ScreenRecordOutput.GIF, ScreenRecordStartMethod.Region, safeTaskSettings);
                break;
            case HotkeyType.ScreenRecorderGIFActiveWindow:
                DebugHelper.WriteException(
                    "HotkeyType.ScreenRecorderGIFActiveWindow is NOT implemented."
                );
                // StartScreenRecording(ScreenRecordOutput.GIF, ScreenRecordStartMethod.ActiveWindow, safeTaskSettings);
                break;
            case HotkeyType.ScreenRecorderGIFCustomRegion:
                DebugHelper.WriteException(
                    "HotkeyType.ScreenRecorderGIFCustomRegion is NOT implemented."
                );
                // StartScreenRecording(ScreenRecordOutput.GIF, ScreenRecordStartMethod.CustomRegion, safeTaskSettings);
                break;
            case HotkeyType.StartScreenRecorderGIF:
                DebugHelper.WriteException("HotkeyType.StartScreenRecorderGIF is NOT implemented.");
                // StartScreenRecording(ScreenRecordOutput.GIF, ScreenRecordStartMethod.LastRegion, safeTaskSettings);
                break;
            case HotkeyType.StopScreenRecording:
                StopScreenRecording();
                break;
            case HotkeyType.PauseScreenRecording:
                PauseScreenRecording();
                break;
            case HotkeyType.AbortScreenRecording:
                AbortScreenRecording();
                break;
            // Tools
            case HotkeyType.ColorPicker:
                DebugHelper.WriteException("HotkeyType.ColorPicker is NOT implemented.");
                // ShowScreenColorPickerDialog(safeTaskSettings);
                break;
            case HotkeyType.ScreenColorPicker:
                DebugHelper.WriteException("HotkeyType.ScreenColorPicker is NOT implemented.");
                // OpenScreenColorPicker(safeTaskSettings);
                break;
            case HotkeyType.Ruler:
                DebugHelper.WriteException("HotkeyType.Ruler is NOT implemented.");
                // OpenRuler(safeTaskSettings);
                break;
            case HotkeyType.PinToScreen:
                DebugHelper.WriteException("HotkeyType.PinToScreen is NOT implemented.");
                PinToScreen(safeTaskSettings);
                break;
            case HotkeyType.PinToScreenFromScreen:
                DebugHelper.WriteException("HotkeyType.PinToScreenFromScreen is NOT implemented.");
                // PinToScreenFromScreen(safeTaskSettings);
                break;
            case HotkeyType.PinToScreenFromClipboard:
                DebugHelper.WriteException(
                    "HotkeyType.PinToScreenFromClipboard is NOT implemented."
                );
                // PinToScreenFromClipboard(safeTaskSettings);
                break;
            case HotkeyType.PinToScreenFromFile:
                DebugHelper.WriteException("HotkeyType.PinToScreenFromFile is NOT implemented.");
                // PinToScreenFromFile(safeTaskSettings);
                break;
            case HotkeyType.PinToScreenCloseAll:
                DebugHelper.WriteException("HotkeyType.PinToScreenCloseAll is NOT implemented.");
                // PinToScreenCloseAll(safeTaskSettings);
                break;
            case HotkeyType.ImageEditor:
                throw new NotImplementedException("ImageEditor not implemented");
            case HotkeyType.ImageBeautifier:
                throw new NotImplementedException("ImageBeautifier not implemented");
            case HotkeyType.ImageEffects:
                throw new NotImplementedException("ImageEffects not implemented");
            case HotkeyType.ImageViewer:
                throw new NotImplementedException("ImageViewer not implemented");
            case HotkeyType.ImageCombiner:
                DebugHelper.WriteException("HotkeyType.ImageCombiner is NOT implemented.");
                // OpenImageCombiner(null, safeTaskSettings);
                break;
            case HotkeyType.ImageSplitter:
                DebugHelper.WriteException("HotkeyType.ImageSplitter is NOT implemented.");
                // OpenImageSplitter();
                break;
            case HotkeyType.ImageThumbnailer:
                DebugHelper.WriteException("HotkeyType.ImageThumbnailer is NOT implemented.");
                // OpenImageThumbnailer();
                break;
            case HotkeyType.VideoConverter:
                DebugHelper.WriteException("HotkeyType.VideoConverter is NOT implemented.");
                // OpenVideoConverter(safeTaskSettings);
                break;
            case HotkeyType.VideoThumbnailer:
                DebugHelper.WriteException("HotkeyType.VideoThumbnailer is NOT implemented.");
                // OpenVideoThumbnailer(safeTaskSettings);
                break;
            case HotkeyType.OCR:
                await OCRImage(command.Parameter);
                break;
            case HotkeyType.QRCode:
                DebugHelper.WriteException("HotkeyType.QRCode is NOT implemented.");
                // OpenQRCode();
                break;
            case HotkeyType.QRCodeDecodeFromScreen:
                DebugHelper.WriteException("HotkeyType.QRCodeDecodeFromScreen is NOT implemented.");
                // OpenQRCodeDecodeFromScreen();
                break;
            case HotkeyType.HashCheck:
                DebugHelper.WriteException("HotkeyType.HashCheck is NOT implemented.");
                // OpenHashCheck();
                break;
            case HotkeyType.IndexFolder:
                DebugHelper.WriteException("HotkeyType.IndexFolder is NOT implemented.");
                // UploadManager.IndexFolder();
                break;
            case HotkeyType.ClipboardViewer:
                DebugHelper.WriteException("HotkeyType.ClipboardViewer is NOT implemented.");
                // OpenClipboardViewer();
                break;
            case HotkeyType.BorderlessWindow:
                DebugHelper.WriteException("HotkeyType.BorderlessWindow is NOT implemented.");
                // OpenBorderlessWindow(safeTaskSettings);
                break;
            case HotkeyType.ActiveWindowBorderless:
                DebugHelper.WriteException("HotkeyType.ActiveWindowBorderless is NOT implemented.");
                // MakeActiveWindowBorderless(safeTaskSettings);
                break;
            case HotkeyType.ActiveWindowTopMost:
                DebugHelper.WriteException("HotkeyType.ActiveWindowTopMost is NOT implemented.");
                // MakeActiveWindowTopMost(safeTaskSettings);
                break;
            case HotkeyType.InspectWindow:
                DebugHelper.WriteException("HotkeyType.InspectWindow is NOT implemented.");
                // OpenInspectWindow();
                break;
            case HotkeyType.MonitorTest:
                DebugHelper.WriteException("HotkeyType.MonitorTest is NOT implemented.");
                // OpenMonitorTest();
                break;
            case HotkeyType.DNSChanger:
                DebugHelper.WriteException("HotkeyType.DNSChanger is NOT implemented.");
                // OpenDNSChanger();
                break;
            // Other
            case HotkeyType.DisableHotkeys:
                ToggleHotkeys();
                break;
            case HotkeyType.OpenMainWindow:
                DebugHelper.WriteException("HotkeyType.OpenMainWindow is NOT implemented.");
                // SnapX.MainForm.ForceActivate();
                break;
            case HotkeyType.OpenScreenshotsFolder:
                OpenScreenshotsFolder();
                break;
            case HotkeyType.OpenHistory:
                DebugHelper.WriteException("HotkeyType.OpenHistory is NOT implemented.");
                // OpenHistory();
                break;
            case HotkeyType.OpenImageHistory:
                DebugHelper.WriteException("HotkeyType.OpenImageHistory is NOT implemented.");
                // OpenImageHistory();
                break;
            case HotkeyType.ToggleActionsToolbar:
                DebugHelper.WriteException("HotkeyType.ToggleActionsToolbar is NOT implemented.");
                // ToggleActionsToolbar();
                break;
            case HotkeyType.ToggleTrayMenu:
                DebugHelper.WriteException("HotkeyType.ToggleTrayMenu is NOT implemented.");
                // ToggleTrayMenu();
                break;
            case HotkeyType.ExitShareX:
                SnapXL.quit();
                break;
        }
    }

    public static ImageData PrepareImage(Image img, TaskSettings taskSettings)
    {
        var imageData = new ImageData();
        imageData.ImageStream = SaveImageAsStream(
            img,
            taskSettings.ImageSettings.ImageFormat,
            taskSettings
        );
        imageData.ImageFormat = taskSettings.ImageSettings.ImageFormat;

        if (
            taskSettings.ImageSettings.ImageAutoUseJPEG
            && taskSettings.ImageSettings.ImageFormat != EImageFormat.JPEG
            && imageData.ImageStream.Length > taskSettings.ImageSettings.ImageAutoUseJPEGSize * 1000
        )
        {
            imageData.ImageStream.Dispose();

            img.Mutate((ctx) => ctx.BackgroundColor(Color.White));
            if (taskSettings.ImageSettings.ImageAutoJPEGQuality)
            {
                imageData.ImageStream = ImageHelpers.SaveJPEGAutoQuality(
                    img,
                    taskSettings.ImageSettings.ImageAutoUseJPEGSize * 1000,
                    2,
                    70,
                    100
                );
            }
            else
            {
                imageData.ImageStream = ImageHelpers.SaveJPEG(
                    img,
                    taskSettings.ImageSettings.ImageJPEGQuality
                );
            }

            imageData.ImageFormat = EImageFormat.JPEG;
        }

        return imageData;
    }

    public static string? CreateThumbnail(
        Image image,
        string folder,
        string fileName,
        TaskSettings taskSettings
    )
    {
        var settings = taskSettings.ImageSettings;

        if (
            settings is { ThumbnailWidth: <= 0, ThumbnailHeight: <= 0 }
            || (
                settings.ThumbnailCheckSize
                && (
                    image.Width <= settings.ThumbnailWidth
                    || image.Height <= settings.ThumbnailHeight
                )
            )
        )
            return null;
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var thumbnailFileName = Path.ChangeExtension(
            $"{baseName}{settings.ThumbnailName}",
            ".webp"
        );
        var thumbnailFilePath = HandleExistsFile(folder, thumbnailFileName, taskSettings);

        if (string.IsNullOrEmpty(thumbnailFilePath))
            return null;
        using var clone = image.Clone(ctx =>
        {
            ctx.Resize(
                new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(settings.ThumbnailWidth, settings.ThumbnailHeight),
                }
            );
        });
        var encoder = new WebpEncoder { Quality = 90 };
        clone.Save(thumbnailFilePath, encoder);
        return thumbnailFilePath;
    }

    public static MemoryStream SaveImageAsStream(
        Image img,
        EImageFormat imageFormat,
        TaskSettings taskSettings
    )
    {
        return SaveImageAsStream(
            img,
            imageFormat,
            taskSettings.ImageSettings.ImagePNGBitDepth,
            taskSettings.ImageSettings.ImageJPEGQuality,
            taskSettings.ImageSettings.ImageGIFQuality
        );
    }

    public static MemoryStream SaveImageAsStream(
        Image image,
        EImageFormat imageFormat,
        PNGBitDepth pngBitDepth = PNGBitDepth.Automatic,
        int jpegQuality = 90,
        GIFQuality gifQuality = GIFQuality.Default
    )
    {
        var ms = new MemoryStream();
        DebugHelper.WriteLine(imageFormat.ToString());
        try
        {
            IImageEncoder encoder = imageFormat switch
            {
                EImageFormat.PNG => new PngEncoder() { },
                EImageFormat.JPEG => new JpegEncoder() { Quality = jpegQuality },
                EImageFormat.GIF => new GifEncoder() { Quantizer = GetGifQuantizer(gifQuality) },
                EImageFormat.BMP => new BmpEncoder(),
                EImageFormat.TIFF => new TiffEncoder(),
                EImageFormat.WEBP => new WebpEncoder() { Quality = jpegQuality },
                EImageFormat.AVIF => new AVIFEncoder() { CQLevel = 10 },
                _ => throw new NotSupportedException(
                    $"Unsupported image format: {imageFormat} {typeof(EImageFormat)}"
                ),
            };
            if (SnapXL.Settings.PNGStripColorSpaceInformation)
            {
                image.Metadata.IccProfile = null;
            }

            image.Save(ms, encoder);
            DebugHelper.WriteLine(image.ToString());
            ms.Position = 0;
        }
        catch (Exception e)
        {
            e.ShowError();
        }

        return ms;
    }

    public static IQuantizer? GetGifQuantizer(GIFQuality quality)
    {
        QuantizerOptions options = new QuantizerOptions();
        // The default GIF quantizer is Octree for ImageSharp.
        // The same one ShareX uses! UNACCEPTABLE!!!
        // This one is higher quality.
        IQuantizer quantizer = new WuQuantizer(options);
        switch (quality)
        {
            case GIFQuality.Bit8:
            case GIFQuality.Default:
                break;

            case GIFQuality.Bit4:
                options.MaxColors = 16;
                quantizer = new WuQuantizer(options);
                break;

            case GIFQuality.Grayscale:
                var grayPalette = CreateGrayscalePalette(256);

                quantizer = new PaletteQuantizer(grayPalette);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
        }

        return quantizer;
    }

    private static ReadOnlyMemory<Color> CreateGrayscalePalette(int maxGrayLevels)
    {
        // Create a list of grayscale colors (from black to white)
        var grayscaleColors = new Color[maxGrayLevels];

        for (var i = 0; i < maxGrayLevels; i++)
        {
            var grayValue = i * 255 / (maxGrayLevels - 1);
            grayscaleColors[i] = new Color(new Rgba32(grayValue, grayValue, grayValue, 255));
        }

        return new ReadOnlyMemory<Color>(grayscaleColors);
    }

    public static void SaveImageAsFile(
        Image img,
        TaskSettings taskSettings,
        bool overwriteFile = false
    )
    {
        using var imageData = PrepareImage(img, taskSettings);
        var screenshotsFolder = GetScreenshotsFolder(taskSettings);
        var fileName = GetFileName(taskSettings, imageData.ImageFormat.GetDescription(), img);
        var filePath = Path.Combine(screenshotsFolder, fileName);

        if (!overwriteFile)
        {
            filePath = HandleExistsFile(filePath, taskSettings);
        }

        if (string.IsNullOrEmpty(filePath))
            return;
        imageData.Write(filePath);
        DebugHelper.WriteLine("Image saved to file: " + filePath);
    }

    public static string? HandleExistsFile(string? filePath, TaskSettings taskSettings)
    {
        if (!File.Exists(filePath))
            return filePath;
        switch (taskSettings.ImageSettings.FileExistAction)
        {
            case FileExistAction.Ask:
                new NotImplementedException("FileExistAction.Ask not implemented").ShowError();
                break;
            case FileExistAction.UniqueName:
                filePath = FileHelpers.GetUniqueFilePath(filePath);
                break;
            case FileExistAction.Cancel:
                filePath = "";
                break;
            case FileExistAction.Overwrite:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return filePath;
    }

    public static string? HandleExistsFile(
        string? folderPath,
        string? fileName,
        TaskSettings taskSettings
    )
    {
        var filePath = Path.Combine(folderPath, fileName);
        return HandleExistsFile(filePath, taskSettings);
    }

    public static string? GetFileName(TaskSettings taskSettings, string extension, Image bmp)
    {
        var metadata = new TaskMetadata(bmp);
        return GetFileName(taskSettings, extension, metadata);
    }

    public static string? GetFileName(
        TaskSettings taskSettings,
        string extension = null,
        TaskMetadata metadata = null
    )
    {
        string? fileName;

        NameParser nameParser = new NameParser(NameParserType.FileName)
        {
            AutoIncrementNumber = SnapXL.Settings.NameParserAutoIncrementNumber,
            MaxNameLength = taskSettings.AdvancedSettings.NamePatternMaxLength,
            MaxTitleLength = taskSettings.AdvancedSettings.NamePatternMaxTitleLength,
            CustomTimeZone = taskSettings.UploadSettings.UseCustomTimeZone
                ? taskSettings.UploadSettings.CustomTimeZone
                : null,
        };

        if (metadata != null)
        {
            if (metadata.Image != null)
            {
                nameParser.ImageWidth = metadata.Image.Width;
                nameParser.ImageHeight = metadata.Image.Height;
            }

            nameParser.WindowText = metadata.WindowTitle;
            nameParser.ProcessName = metadata.ProcessName;
        }

        if (
            !string.IsNullOrEmpty(taskSettings.UploadSettings.NameFormatPatternActiveWindow)
            && !string.IsNullOrEmpty(nameParser.WindowText)
        )
        {
            fileName = nameParser.Parse(taskSettings.UploadSettings.NameFormatPatternActiveWindow);
        }
        else
        {
            fileName = nameParser.Parse(taskSettings.UploadSettings.NameFormatPattern);
        }

        SnapXL.Settings.NameParserAutoIncrementNumber = nameParser.AutoIncrementNumber;

        if (!string.IsNullOrEmpty(extension))
        {
            fileName += "." + extension.TrimStart('.');
        }

        return fileName;
    }

    public static string? GetScreenshotsFolder(
        TaskSettings taskSettings = null,
        TaskMetadata metadata = null,
        DateTime? date = null
    )
    {
        date ??= DateTime.Now;
        var dt = date.Value;
        string? screenshotsFolder;

        NameParser nameParser = new NameParser(NameParserType.FilePath);

        if (metadata != null)
        {
            if (metadata.Image != null)
            {
                nameParser.ImageWidth = metadata.Image.Width;
                nameParser.ImageHeight = metadata.Image.Height;
            }

            nameParser.WindowText = metadata.WindowTitle;
            nameParser.ProcessName = metadata.ProcessName;
        }

        if (
            taskSettings != null
            && taskSettings.OverrideScreenshotsFolder
            && !string.IsNullOrEmpty(taskSettings.ScreenshotsFolder)
        )
        {
            screenshotsFolder = nameParser.Parse(taskSettings.ScreenshotsFolder);
        }
        else
        {
            string? subFolderPattern;

            if (
                !string.IsNullOrEmpty(SnapXL.Settings.SaveImageSubFolderPatternWindow)
                && !string.IsNullOrEmpty(nameParser.WindowText)
            )
            {
                subFolderPattern = SnapXL.Settings.SaveImageSubFolderPatternWindow;
            }
            else
            {
                subFolderPattern = SnapXL.Settings.SaveImageSubFolderPattern;
            }

            string? subFolderPath = nameParser.Parse(subFolderPattern, dt);
            screenshotsFolder = Path.Combine(SnapXL.ScreenshotsParentFolder, subFolderPath);
        }

        return FileHelpers.GetAbsolutePath(screenshotsFolder);
    }

    public static void AddDefaultExternalPrograms(TaskSettings taskSettings)
    {
        if (taskSettings.ExternalPrograms == null)
        {
            taskSettings.ExternalPrograms = [];
        }

        AddExternalProgramFromRegistry(taskSettings, "Paint", "mspaint.exe");
        AddExternalProgramFromRegistry(taskSettings, "Paint.NET", "PaintDotNet.exe");
        AddExternalProgramFromRegistry(taskSettings, "Adobe Photoshop", "Photoshop.exe");
        AddExternalProgramFromRegistry(taskSettings, "IrfanView", "i_view32.exe");
        AddExternalProgramFromRegistry(taskSettings, "XnView", "xnview.exe");
    }

    private static void AddExternalProgramFromRegistry(
        TaskSettings taskSettings,
        string name,
        string fileName
    )
    {
        // if (!taskSettings.ExternalPrograms.Exists(x => x.Name == name))
        // {
        //     try
        //     {
        //         string filePath = RegistryHelpers.SearchProgramPath(fileName);
        //
        //         if (!string.IsNullOrEmpty(filePath))
        //         {
        //             ExternalProgram externalProgram = new ExternalProgram(name, filePath);
        //             taskSettings.ExternalPrograms.Add(externalProgram);
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         DebugHelper.WriteException(e);
        //     }
        // }
    }

    public static void StartScreenRecording(
        ScreenRecordOutput outputType,
        ScreenRecordStartMethod startMethod,
        TaskSettings taskSettings = null
    )
    {
        if (taskSettings == null)
            taskSettings = TaskSettings.GetDefaultTaskSettings();

        ScreenRecordManager.StartStopRecording(outputType, startMethod, taskSettings);
    }

    public static void StopScreenRecording()
    {
        ScreenRecordManager.StopRecording();
    }

    public static void PauseScreenRecording()
    {
        ScreenRecordManager.PauseScreenRecording();
    }

    public static void AbortScreenRecording()
    {
        ScreenRecordManager.AbortRecording();
    }

    public static void OpenScreenshotsFolder()
    {
        string? screenshotsFolder = GetScreenshotsFolder();

        if (Directory.Exists(screenshotsFolder))
        {
            FileHelpers.OpenFolder(screenshotsFolder);
        }
        else
        {
            FileHelpers.OpenFolder(SnapXL.ScreenshotsParentFolder);
        }
    }

    [RequiresAssemblyFiles()]
    public static void RunShareXAsAdmin(string arguments = null)
    {
        try
        {
            using var process = new Process();
            var exePath = Assembly.GetExecutingAssembly().Location;
            var isWindows = OperatingSystem.IsWindows();
            var isLinux = OperatingSystem.IsLinux();
            var isMacOS = OperatingSystem.IsMacOS();
            var isFreeBSD = OperatingSystem.IsFreeBSD();

            ProcessStartInfo psi;

            if (isWindows)
            {
                psi = new ProcessStartInfo()
                {
                    FileName = exePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas",
                };
            }
            else if (isLinux)
            {
                psi = new ProcessStartInfo()
                {
                    FileName = "pkexec",
                    ArgumentList = { exePath },
                    UseShellExecute = false,
                };
                if (!string.IsNullOrEmpty(arguments))
                    psi.ArgumentList.Add(arguments);
            }
            else if (isMacOS)
            {
                psi = new ProcessStartInfo()
                {
                    FileName = "osascript",
                    ArgumentList =
                    {
                        "-e",
                        $"do shell script \"'{exePath}' {(arguments ?? "")}\" with administrator privileges",
                    },
                    UseShellExecute = false,
                };
            }
            else if (isFreeBSD)
            {
                psi = new ProcessStartInfo()
                {
                    FileName = "doas",
                    ArgumentList = { exePath },
                    UseShellExecute = false,
                };
                if (!string.IsNullOrEmpty(arguments))
                    psi.ArgumentList.Add(arguments);
            }
            else
            {
                return;
            }

            process.StartInfo = psi;
            process.Start();
        }
        catch { }
    }

    public static void SearchImageUsingGoogleLens(string? url)
    {
        new GoogleLensSharingService().CreateSharer(null, null).ShareURL(url);
    }

    public static void SearchImageUsingBing(string? url)
    {
        new BingVisualSearchSharingService().CreateSharer(null, null).ShareURL(url);
    }

    public static async Task<string> OCRImage(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return string.Empty;
        var img = await Image.LoadAsync(filePath);
        return await OCRImage(img, TaskSettings.GetDefaultTaskSettings());
    }

    public static async Task<string> OCRImage(TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();
        var img = RegionCaptureTasks.GetRegionImage(taskSettings.CaptureSettings.SurfaceOptions);
        return await OCRImage(img, taskSettings);
    }

    public static async Task<string> OCRImage(Image image, TaskSettings? taskSettings = null)
    {
        return await OCRImage(image, null, taskSettings);
    }

#if !DISABLE_OCR
    public async static Task<OcrModel> GetModelForLanguage(string languageCode)
    {
        return languageCode switch
        {
            "eng" => OnnxModels.V5.EnglishMobile,
            "chi_sim" => OnnxModels.V5.ChineseMobile,
            "chi_tra" => OnnxModels.V4.ChineseTraditional,
            "jpn" or "ja" => OnnxModels.V4.JapaneseMobile,
            "kor" => OnnxModels.V5.KoreanMobile,
            "tel" => OnnxModels.V5.TeluguMobile,
            "kan" => OnnxModels.V4.KannadaMobile,
            "tam" => OnnxModels.V5.TamilMobile,
            "ara" or "fas" or "urd" => OnnxModels.V5.ArabicMobile,
            "hin" or "mar" or "nep" => OnnxModels.V5.DevanagariMobile,
            "rus" or "ukr" or "bel" or "eslav" =>
                OnnxModels.V5.EastSlavicMobile,
            "bul" =>
                OnnxModels.V5.CyrillicMobile,
            "ell" =>
                OnnxModels.V5.GreekMobile,
            "tha" =>
                OnnxModels.V5.ThaiMobile,
            _ => OnnxModels.V5.LatinMobile,
        };
    }
#endif


    public class OcrResponse : IDisposable, IAsyncDisposable
    {
        public string FullText { get; set; } = string.Empty;
        public Image? AnnotatedImage { get; set; }
        public List<OcrTextLine> Lines { get; set; } = new();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                AnnotatedImage?.Dispose();
            }

            AnnotatedImage = null;
        }

        protected virtual ValueTask DisposeAsyncCore()
        {
            if (AnnotatedImage is IAsyncDisposable asyncDisposable)
            {
                return asyncDisposable.DisposeAsync();
            }

            AnnotatedImage?.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    public class OcrTextLine
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }

        // The raw data from the engine: [[x,y], [x,y], [x,y], [x,y]]
        public float[][] BoundingBox { get; set; } = Array.Empty<float[]>();

        // Returns the 4 points as ImageSharp PointF objects for drawing
        public PointF[] BoxPoints => BoundingBox
            .Select(p => new PointF(p[0], p[1]))
            .ToArray();

        // Helper properties for absolute positioning on a Canvas
        public float MinX => BoundingBox.Length > 0 ? BoundingBox.Min(p => p[0]) : 0;
        public float MaxX => BoundingBox.Length > 0 ? BoundingBox.Max(p => p[0]) : 0;
        public float MinY => BoundingBox.Length > 0 ? BoundingBox.Min(p => p[1]) : 0;
        public float MaxY => BoundingBox.Length > 0 ? BoundingBox.Max(p => p[1]) : 0;

        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
    }

#if !DISABLE_OCR
    public record OCRProgress(double Percent, string Status);
    // Internal helper to convert RapidOcr types to our clean DTOs
    private static OcrResponse MapToResponse(OcrResult result, Image visualImage)
    {
        return new OcrResponse
        {
            FullText = result.StrRes,
            AnnotatedImage = visualImage,
            Lines = result.TextBlocks.Select(block => new OcrTextLine
            {
                Text = block.GetText(),
                Confidence = block.BoxScore,
                BoundingBox = block.BoxPoints.Select(p => new[] { p.X, p.Y }).ToArray()
            }).ToList()
        };
    }
#endif

    public static async Task<OcrResponse> OCRImageDetailed(
    Image? image = null,
    string? filePath = null,
    TaskSettings? taskSettings = null,
    string? languageCode = null,
    IProgress<OCRProgress>? progress = null,
    CancellationToken cts = default
)
    {
#if DISABLE_OCR
    DebugHelper.WriteException(new Exception("This build of SnapX was built with DISABLE_OCR build time constant."));
    return new OcrResponse { FullText = "OCR Disabled in this build." };
#else
        progress?.Report(new(5, "Initializing OCR engine..."));

        var imageConfig = Configuration.Default;
        imageConfig.ImageFormatsManager.SetEncoder(AVIFFormat.Instance, AVIFEncoder.Instance);
        imageConfig.ImageFormatsManager.SetDecoder(AVIFFormat.Instance, AVIFDecoder.Instance);
        imageConfig.ImageFormatsManager.AddImageFormatDetector(new PatchedAVIFImageFormatDetector());

        try
        {
            if (filePath is not null && image is null)
            {
                progress?.Report(new(10, "Reading image from disk..."));
                image = await Image.LoadAsync(filePath, cts);
            }
        }
        catch (Exception ex)
        {
            var issue = "Failed to load image for OCR";
            DebugHelper.Logger?.Warning(issue);
            DebugHelper.WriteException(ex);
            return new OcrResponse { FullText = $"{issue}{Environment.NewLine}{ex.Message}" };
        }

        if (image is null) return new OcrResponse { FullText = "SNAPX ERROR: PASSED NULL IMAGE AND NULL FILEPATH." };

        var modelDir = Path.Combine(BaseDirectory.CacheHome, SnapXL.AppName, "PaddleOCRModels");
        var model = await GetModelForLanguage(languageCode ?? "eng");
        var ocrEngine = new RapidOcr();

        var progressValue = 15;
        var fileUrls = new ConcurrentDictionary<string, int>();
        var fileProgressValues = new ConcurrentDictionary<string, int>();

        EventHandler<HttpProgressEventArgs> progressHandler = async (sender, args) =>
        {
            var url = args.UserState as string;
            if (string.IsNullOrEmpty(url)) return;
            if (!url.Contains("PP-OCR", StringComparison.OrdinalIgnoreCase) &&
                !url.Contains("ppocr", StringComparison.OrdinalIgnoreCase)) return;

            fileUrls.TryAdd(url, fileUrls.Count);

            var fileIndex = fileUrls[url];
            var fileCount = Math.Max(1, fileUrls.Count);
            var perFileRange = 70 / fileCount;

            var fileStartPct = 15 + (fileIndex * perFileRange);
            var fileEndPct = fileStartPct + perFileRange;

            var currentFileProgress = fileStartPct + (int)((args.ProgressPercentage / 100.0) * (fileEndPct - fileStartPct));
            var lastFileProgress = fileProgressValues.GetOrAdd(url, 0);

            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var filename = query["filename"] ?? query["name"] ?? query["response-content-disposition"];

            if (string.IsNullOrEmpty(filename)) filename = Path.GetFileName(uri.LocalPath);
            if (!string.IsNullOrEmpty(filename) && filename.Contains("filename="))
                filename = filename.Split("filename=").Last().Trim('"');

            filename ??= "model.onnx";

            var displayProgress = Math.Max(progressValue, currentFileProgress);
            progress?.Report(new(displayProgress, $"Downloading {filename} ({args.ProgressPercentage}%)..."));

            if (currentFileProgress > lastFileProgress)
            {
                fileProgressValues[url] = currentFileProgress;
                if (currentFileProgress > progressValue) progressValue = currentFileProgress;
            }
            await Task.Yield();
        };

        if (HttpClientFactory._ph != null) HttpClientFactory._ph.HttpReceiveProgress += progressHandler;

        try
        {
            progress?.Report(new(15, "Checking and loading OCR models..."));
            await ocrEngine.LoadModelAsync(model, modelDir, HttpClientFactory.Get(), ct: cts);
        }
        finally
        {
            if (HttpClientFactory._ph != null) HttpClientFactory._ph.HttpReceiveProgress -= progressHandler;
        }

        progress?.Report(new(85, "Preprocessing image for Paddle..."));
        var originalLongSide = Math.Max(image.Width, image.Height);
        var targetLimit = 1504;
        var finalResize = originalLongSide < targetLimit ? (int)(Math.Ceiling(originalLongSide / 32.0) * 32) : targetLimit;

        progress?.Report(new(90, "Performing Text Detection & Recognition..."));
        var ocrResult = ocrEngine.Detect(image, new RapidOcrOptions
        {
            BoxScoreThresh = 0.5f,
            BoxThresh = 0.3f,
            DoAngle = true,
            MostAngle = true,
            UnClipRatio = 1.6f,
            ImgResize = finalResize,
            Padding = 10,
        });

        var visualDebugImage = image.Clone(_ => { });
        foreach (var block in ocrResult.TextBlocks)
        {
            visualDebugImage.Mutate(ctx => { ctx.DrawPolygon(Color.Red, 4f, block.BoxPoints); });
        }

        progress?.Report(new(98, "Finalizing results..."));
        return MapToResponse(ocrResult, visualDebugImage);
#endif
    }

    // Keep API Compatibility
    public static async Task<string> OCRImage(
        Image? image = null,
        string? filePath = null,
        TaskSettings? taskSettings = null,
        string? languageCode = null,
        IProgress<OCRProgress>? progress = null)
    {
        var result = await OCRImageDetailed(image, filePath, taskSettings, languageCode, progress);
        return result.FullText;
    }

    public static void OCRImageUI(Image? image, TaskSettings? taskSettings = null)
    {
        if (image == null) return;

        taskSettings ??= TaskSettings.GetDefaultTaskSettings();

        SnapXL.EventAggregator.Publish(new NeedOCRWindowEvent(image, taskSettings));
    }

    public static void PinToScreen(TaskSettings taskSettings = null)
    {
        throw new NotImplementedException("PinToScreen is not implemented");
    }

    public static void TweetMessage()
    {
        throw new NotImplementedException("TweetMessage is not implemented");
    }

    public static EDataType FindDataType(string? filePath, TaskSettings taskSettings)
    {
        if (FileHelpers.CheckExtension(filePath, taskSettings.AdvancedSettings.ImageExtensions))
        {
            return EDataType.Image;
        }

        if (FileHelpers.CheckExtension(filePath, taskSettings.AdvancedSettings.TextExtensions))
        {
            return EDataType.Text;
        }

        return EDataType.File;
    }

    public static bool ToggleHotkeys()
    {
        bool disableHotkeys = !SnapXL.Settings.DisableHotkeys;
        ToggleHotkeys(disableHotkeys);
        return disableHotkeys;
    }

    public static void ToggleHotkeys(bool disableHotkeys)
    {
        SnapXL.Settings.DisableHotkeys = disableHotkeys;
        SnapXL.HotkeyManager.ToggleHotkeys(disableHotkeys);
    }

    public static bool CheckFFmpeg(TaskSettings taskSettings)
    {
        return true;
    }

    public static Screenshot GetScreenshot(TaskSettings taskSettings = null)
    {
        if (taskSettings == null)
            taskSettings = TaskSettings.GetDefaultTaskSettings();

        var screenshot = new Screenshot()
        {
            CaptureCursor = taskSettings.CaptureSettings.ShowCursor,
            CaptureClientArea = taskSettings.CaptureSettings.CaptureClientArea,
            RemoveOutsideScreenArea = true,
            CaptureShadow = taskSettings.CaptureSettings.CaptureShadow,
            ShadowOffset = taskSettings.CaptureSettings.CaptureShadowOffset,
            AutoHideTaskbar = taskSettings.CaptureSettings.CaptureAutoHideTaskbar,
        };

        return screenshot;
    }

    public static void ImportCustomUploader(string? filePath)
    {
        ImportCustomUploader([filePath]);
    }

    public static void ImportCustomUploader(IEnumerable<string?> filePaths)
    {
        var contents = filePaths
            .Where(p => !string.IsNullOrEmpty(p) && File.Exists(p))
            .Select(File.ReadAllText);

        ImportCustomUploaderJson(contents);
    }

    public static void ImportCustomUploaderJson(IEnumerable<string> jsonContents)
    {
        if (SnapXL.UploadersConfig == null)
            return;

        foreach (var json in jsonContents)
        {
            if (string.IsNullOrEmpty(json))
                continue;

            try
            {
                CustomUploaderItem cui = JsonHelpers.DeserializeFromString<CustomUploaderItem>(json);
                if (cui != null)
                {
                    bool activate = false;
                    if (cui.DestinationType != CustomUploaderDestinationType.None)
                    {
                        List<string> destinations = [];
                        if (
                            cui.DestinationType.HasFlag(CustomUploaderDestinationType.ImageUploader)
                        )
                            destinations.Add("images");
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.TextUploader))
                            destinations.Add("texts");
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.FileUploader))
                            destinations.Add("files");
                        if (
                            cui.DestinationType.HasFlag(CustomUploaderDestinationType.URLShortener)
                            || cui.DestinationType.HasFlag(
                                CustomUploaderDestinationType.URLSharingService
                            )
                        )
                            destinations.Add("urls");

                        activate = true;
                    }

                    cui.CheckBackwardCompatibility();
                    SnapXL.UploadersConfig.CustomUploadersList.Add(cui);

                    if (activate)
                    {
                        int index = SnapXL.UploadersConfig.CustomUploadersList.Count - 1;
                        if (
                            cui.DestinationType.HasFlag(CustomUploaderDestinationType.ImageUploader)
                        )
                        {
                            SnapXL.UploadersConfig.CustomImageUploaderSelected = index;
                            SnapXL.DefaultTaskSettings.ImageDestination =
                                ImageDestination.CustomImageUploader;
                        }
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.TextUploader))
                        {
                            SnapXL.UploadersConfig.CustomTextUploaderSelected = index;
                            SnapXL.DefaultTaskSettings.TextDestination =
                                TextDestination.CustomTextUploader;
                        }
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.FileUploader))
                        {
                            SnapXL.UploadersConfig.CustomFileUploaderSelected = index;
                            SnapXL.DefaultTaskSettings.FileDestination =
                                FileDestination.CustomFileUploader;
                        }
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.URLShortener))
                        {
                            SnapXL.UploadersConfig.CustomURLShortenerSelected = index;
                            SnapXL.DefaultTaskSettings.URLShortenerDestination =
                                UrlShortenerType.CustomURLShortener;
                        }
                        if (
                            cui.DestinationType.HasFlag(
                                CustomUploaderDestinationType.URLSharingService
                            )
                        )
                        {
                            SnapXL.UploadersConfig.CustomURLSharingServiceSelected = index;
                            SnapXL.DefaultTaskSettings.URLSharingServiceDestination =
                                URLSharingServices.CustomURLSharingService;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.ShowError();
            }
        }
        SettingManager.SaveUploadersConfigAsync();
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "<Pending>"
    )]
    public static void ImportImageEffect(string? json)
    {
        ImageEffectPreset preset = null;

        try
        {
            preset = JsonHelpers.DeserializeFromString<ImageEffectPreset>(json);
        }
        catch (Exception e)
        {
            e.ShowError();
        }

        if (preset != null && preset.Effects.Count > 0)
        {
            throw new NotImplementedException(
                "ImportImageEffect is not implemented. It relies on SnapX.ImageEffectsLib which is not ported yet."
            );
        }
    }

    public static async Task HandleNativeMessagingInput(
        string? filePath,
        TaskSettings taskSettings = null
    )
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            NativeMessagingInput nativeMessagingInput = null;

            try
            {
                nativeMessagingInput = JsonHelpers.DeserializeFromFile<NativeMessagingInput>(
                    filePath
                );
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e);
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }

            if (nativeMessagingInput != null)
            {
                if (taskSettings == null)
                    taskSettings = TaskSettings.GetDefaultTaskSettings();

                PlayNotificationSoundAsync(NotificationSound.ActionCompleted, taskSettings);

                switch (nativeMessagingInput.Action)
                {
                    // TEMP: For backward compatibility
                    default:
                        if (!string.IsNullOrEmpty(nativeMessagingInput.URL))
                        {
                            UploadManager.DownloadAndUploadFile(
                                nativeMessagingInput.URL,
                                taskSettings
                            );
                        }
                        else if (!string.IsNullOrEmpty(nativeMessagingInput.Text))
                        {
                            UploadManager.UploadText(nativeMessagingInput.Text, taskSettings);
                        }
                        break;
                    case NativeMessagingAction.UploadImage:
                        if (!string.IsNullOrEmpty(nativeMessagingInput.URL))
                        {
                            var image = await WebHelpers.DataURLToImage(nativeMessagingInput.URL);

                            if (
                                image == null
                                && taskSettings.AdvancedSettings.ProcessImagesDuringExtensionUpload
                            )
                            {
                                try
                                {
                                    image = await WebHelpers.DownloadImageAsync(
                                        nativeMessagingInput.URL
                                    );
                                }
                                catch
                                {
                                    // I must acknowledge I am swallowing errors. FUCK
                                }
                            }

                            if (image != null)
                            {
                                UploadManager.RunImageTask(image, taskSettings);
                            }
                            else
                            {
                                UploadManager.DownloadAndUploadFile(
                                    nativeMessagingInput.URL,
                                    taskSettings
                                );
                            }
                        }
                        break;
                    case NativeMessagingAction.UploadVideo:
                    case NativeMessagingAction.UploadAudio:
                        if (!string.IsNullOrEmpty(nativeMessagingInput.URL))
                        {
                            UploadManager.DownloadAndUploadFile(
                                nativeMessagingInput.URL,
                                taskSettings
                            );
                        }
                        break;
                    case NativeMessagingAction.UploadText:
                        if (!string.IsNullOrEmpty(nativeMessagingInput.Text))
                        {
                            UploadManager.UploadText(nativeMessagingInput.Text, taskSettings);
                        }
                        break;
                    case NativeMessagingAction.ShortenURL:
                        if (!string.IsNullOrEmpty(nativeMessagingInput.URL))
                        {
                            UploadManager.ShortenURL(nativeMessagingInput.URL, taskSettings);
                        }
                        break;
                }
            }
        }
    }

    public static Image? GenerateQRCode(string text, int size)
    {
        var result = GenerateQRCodeWithMatrix(text, size);
        return result.Image;
    }

    public static (Image? Image, BitMatrix? Matrix) GenerateQRCodeWithMatrix(string text, int size)
    {
        if (!CheckQRCodeContent(text))
            return (null, null);

        try
        {
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = size,
                    Height = size,
                    CharacterSet = "UTF-8",
                    PureBarcode = true,
                    NoPadding = false,
                    Margin = 1,
                },
                Renderer = new ImageSharpRenderer<Rgba32>(),
            };

            var matrix = writer.Encode(text);
            var image = writer.Write(matrix);

            return (image, matrix);
        }
        catch (Exception e)
        {
            e.ShowError();
        }

        return (null, null);
    }

    public static string[] BarcodeScan(Image<Rgba32> img, bool scanQRCodeOnly = false)
    {
        try
        {
            var barcodeReader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
            {
                AutoRotate = true,
                Options = new DecodingOptions { TryHarder = true, TryInverted = true },
            };

            if (scanQRCodeOnly)
            {
                barcodeReader.Options.PossibleFormats = [BarcodeFormat.QR_CODE];
            }

            Result[] results = barcodeReader.DecodeMultiple(img);

            if (results != null)
            {
                return results
                    .Where(x => x != null && !string.IsNullOrEmpty(x.Text))
                    .Select(x => x.Text)
                    .ToArray();
            }
        }
        catch (Exception e)
        {
            e.ShowError();
        }

        return null;
    }

    public static bool CheckQRCodeContent(string content)
    {
        return !string.IsNullOrEmpty(content) && Encoding.UTF8.GetByteCount(content) <= 2952;
    }

    public static bool IsUploadAllowed()
    {
        if (SnapXL.Settings.DisableUpload)
        {
            return false;
        }

        return true;
    }

    public static async Task PlaySound(Stream stream)
    {
        DebugHelper.WriteLine(
            $"PlaySound {stream.Length} bytes {stream.Position} {stream.CanSeek} {stream.CanRead}"
        );
        var tempFilePath = Path.GetTempFileName();
        stream.Seek(0, SeekOrigin.Begin);
        stream.WriteToFile(tempFilePath);
        var psi = new ProcessStartInfo
        {
            FileName = "ffplay", // Even on Windows, we expect ffplay to be in the $PATH. https://winstall.app/apps/Gyan.FFmpeg
            Arguments = $"-nodisp -autoexit -hide_banner -loglevel warning \"{tempFilePath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is not null)
                await process.WaitForExitAsync();
        }
        catch (Exception e)
        {
            DebugHelper.Logger?.Warning(
                "Failed to play sound using ffplay. Did you install FFmpeg? Error: " + e.Message
            );
        }
        finally
        {
            try
            {
                File.Delete(tempFilePath);
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static async Task PlaySound(string filePath) =>
        await PlaySound(File.OpenRead(filePath));

    // Coding nerds, please, forgive me for this mortal sin.
    // The code here is instance dependent thus cannot be called from static stuff yada yada yada.
    public static void PlayNotificationSoundAsync(
        NotificationSound notificationSound,
        TaskSettings? taskSettings = null
    )
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();
        switch (notificationSound)
        {
            case NotificationSound.Capture:
                if (taskSettings.GeneralSettings.PlaySoundAfterCapture)
                {
                    if (
                        taskSettings.GeneralSettings.UseCustomCaptureSound
                        && !string.IsNullOrEmpty(
                            taskSettings.GeneralSettings.CustomCaptureSoundPath
                        )
                    )
                    {
                        PlaySound(taskSettings.GeneralSettings.CustomCaptureSoundPath);
                    }
                    else
                    {
                        PlaySound(Resources.Resources.CaptureSound);
                    }
                }
                break;
            case NotificationSound.TaskCompleted:
                if (taskSettings.GeneralSettings.PlaySoundAfterUpload)
                {
                    if (
                        taskSettings.GeneralSettings.UseCustomTaskCompletedSound
                        && !string.IsNullOrEmpty(
                            taskSettings.GeneralSettings.CustomTaskCompletedSoundPath
                        )
                    )
                    {
                        PlaySound(taskSettings.GeneralSettings.CustomTaskCompletedSoundPath);
                    }
                    else
                    {
                        PlaySound(Resources.Resources.TaskCompletedSound);
                    }
                }
                break;
            case NotificationSound.ActionCompleted:
                if (taskSettings.GeneralSettings.PlaySoundAfterAction)
                {
                    if (
                        taskSettings.GeneralSettings.UseCustomActionCompletedSound
                        && !string.IsNullOrEmpty(
                            taskSettings.GeneralSettings.CustomActionCompletedSoundPath
                        )
                    )
                    {
                        PlaySound(taskSettings.GeneralSettings.CustomActionCompletedSoundPath);
                    }
                    else
                    {
                        PlaySound(Resources.Resources.ActionCompletedSound);
                    }
                }
                break;
            case NotificationSound.Error:
                if (taskSettings.GeneralSettings.PlaySoundAfterUpload)
                {
                    if (
                        taskSettings.GeneralSettings.UseCustomErrorSound
                        && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomErrorSoundPath)
                    )
                    {
                        PlaySound(taskSettings.GeneralSettings.CustomErrorSoundPath);
                    }
                    else
                    {
                        PlaySound(Resources.Resources.ErrorSound);
                    }
                }
                break;
        }
    }
}
