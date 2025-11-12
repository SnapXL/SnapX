
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using NeoSolve.ImageSharp.AVIF;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR.Models.Local;
using SixLabors.ImageSharp;
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
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp.Rendering;
using ZXing.QrCode;
using ResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using Size = SixLabors.ImageSharp.Size;

namespace SnapX.Core.Job;

public static class TaskHelpers
{
    public static async Task ExecuteJob(HotkeyType job, CLICommand command = null)
    {
        await ExecuteJob(SnapX.DefaultTaskSettings, job, command);
    }

    public static async Task ExecuteJob(TaskSettings taskSettings)
    {
        await ExecuteJob(taskSettings, taskSettings.Job);
    }

    public static async Task ExecuteJob(TaskSettings taskSettings, HotkeyType job, CLICommand command = null)
    {
        if (job == HotkeyType.None) return;

        DebugHelper.WriteLine("Executing: " + job.GetLocalizedDescription());

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
                DebugHelper.WriteException("HotkeyType.ClipboardUploadWithContentViewer is NOT implemented.");
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
                DebugHelper.WriteException("HotkeyType.ScreenRecorderActiveWindow is NOT implemented.");
                // StartScreenRecording(ScreenRecordOutput.FFmpeg, ScreenRecordStartMethod.ActiveWindow, safeTaskSettings);
                break;
            case HotkeyType.ScreenRecorderCustomRegion:
                DebugHelper.WriteException("HotkeyType.ScreenRecorderCustomRegion is NOT implemented.");
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
                DebugHelper.WriteException("HotkeyType.ScreenRecorderGIFActiveWindow is NOT implemented.");
                // StartScreenRecording(ScreenRecordOutput.GIF, ScreenRecordStartMethod.ActiveWindow, safeTaskSettings);
                break;
            case HotkeyType.ScreenRecorderGIFCustomRegion:
                DebugHelper.WriteException("HotkeyType.ScreenRecorderGIFCustomRegion is NOT implemented.");
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
                DebugHelper.WriteException("HotkeyType.PinToScreenFromClipboard is NOT implemented.");
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
                DebugHelper.WriteException("HotkeyType.PinToScreenFromScreen is NOT implemented.");
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
                SnapX.quit();
                break;
        }
    }

    public static ImageData PrepareImage(Image img, TaskSettings taskSettings)
    {
        var imageData = new ImageData();
        imageData.ImageStream = SaveImageAsStream(img, taskSettings.ImageSettings.ImageFormat, taskSettings);
        imageData.ImageFormat = taskSettings.ImageSettings.ImageFormat;

        if (taskSettings.ImageSettings.ImageAutoUseJPEG && taskSettings.ImageSettings.ImageFormat != EImageFormat.JPEG &&
            imageData.ImageStream.Length > taskSettings.ImageSettings.ImageAutoUseJPEGSize * 1000)
        {
            imageData.ImageStream.Dispose();

            img.Mutate((ctx) => ctx.BackgroundColor(Color.White));
            if (taskSettings.ImageSettings.ImageAutoJPEGQuality)
            {
                imageData.ImageStream = ImageHelpers.SaveJPEGAutoQuality(img, taskSettings.ImageSettings.ImageAutoUseJPEGSize * 1000, 2, 70, 100);
            }
            else
            {
                imageData.ImageStream = ImageHelpers.SaveJPEG(img, taskSettings.ImageSettings.ImageJPEGQuality);
            }

            imageData.ImageFormat = EImageFormat.JPEG;
        }

        return imageData;
    }

    public static string? CreateThumbnail(Image image, string folder, string fileName, TaskSettings taskSettings)
    {
        var settings = taskSettings.ImageSettings;

        if (settings is { ThumbnailWidth: <= 0, ThumbnailHeight: <= 0 } ||
            (settings.ThumbnailCheckSize &&
             (image.Width <= settings.ThumbnailWidth || image.Height <= settings.ThumbnailHeight))) return null;
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var thumbnailFileName = Path.ChangeExtension($"{baseName}{settings.ThumbnailName}", ".webp");
        var thumbnailFilePath = HandleExistsFile(folder, thumbnailFileName, taskSettings);

        if (string.IsNullOrEmpty(thumbnailFilePath)) return null;
        using var clone = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(settings.ThumbnailWidth, settings.ThumbnailHeight)
            });
        });
        var encoder = new WebpEncoder { Quality = 90 };
        clone.Save(thumbnailFilePath, encoder);
        return thumbnailFilePath;

    }
    public static MemoryStream SaveImageAsStream(Image img, EImageFormat imageFormat, TaskSettings taskSettings)
    {
        return SaveImageAsStream(img, imageFormat, taskSettings.ImageSettings.ImagePNGBitDepth,
            taskSettings.ImageSettings.ImageJPEGQuality, taskSettings.ImageSettings.ImageGIFQuality);
    }

    public static MemoryStream SaveImageAsStream(Image image, EImageFormat imageFormat, PNGBitDepth pngBitDepth = PNGBitDepth.Automatic,
        int jpegQuality = 90, GIFQuality gifQuality = GIFQuality.Default)
    {
        var ms = new MemoryStream();
        DebugHelper.WriteLine(imageFormat.ToString());
        try
        {
            IImageEncoder encoder = imageFormat switch
            {
                EImageFormat.PNG => new PngEncoder()
                {
                },
                EImageFormat.JPEG => new JpegEncoder()
                {
                    Quality = jpegQuality
                },
                EImageFormat.GIF => new GifEncoder()
                {
                    Quantizer = GetGifQuantizer(gifQuality)
                },
                EImageFormat.BMP => new BmpEncoder(),
                EImageFormat.TIFF => new TiffEncoder(),
                EImageFormat.WEBP => new WebpEncoder()
                {
                    Quality = jpegQuality,
                },
                EImageFormat.AVIF => new AVIFEncoder()
                {
                    CQLevel = 10
                },
                _ => throw new NotSupportedException($"Unsupported image format: {imageFormat} {typeof(EImageFormat)}")
            };
            if (SnapX.Settings.PNGStripColorSpaceInformation)
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
    public static void SaveImageAsFile(Image img, TaskSettings taskSettings, bool overwriteFile = false)
    {
        using var imageData = PrepareImage(img, taskSettings);
        var screenshotsFolder = GetScreenshotsFolder(taskSettings);
        var fileName = GetFileName(taskSettings, imageData.ImageFormat.GetDescription(), img);
        var filePath = Path.Combine(screenshotsFolder, fileName);

        if (!overwriteFile)
        {
            filePath = HandleExistsFile(filePath, taskSettings);
        }

        if (string.IsNullOrEmpty(filePath)) return;
        imageData.Write(filePath);
        DebugHelper.WriteLine("Image saved to file: " + filePath);
    }
    public static string? HandleExistsFile(string? filePath, TaskSettings taskSettings)
    {
        if (!File.Exists(filePath)) return filePath;
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
    public static string? HandleExistsFile(string? folderPath, string? fileName, TaskSettings taskSettings)
    {
        var filePath = Path.Combine(folderPath, fileName);
        return HandleExistsFile(filePath, taskSettings);
    }
    public static string? GetFileName(TaskSettings taskSettings, string extension, Image bmp)
    {
        var metadata = new TaskMetadata(bmp);
        return GetFileName(taskSettings, extension, metadata);
    }

    public static string? GetFileName(TaskSettings taskSettings, string extension = null, TaskMetadata metadata = null)
    {
        string? fileName;

        NameParser nameParser = new NameParser(NameParserType.FileName)
        {
            AutoIncrementNumber = SnapX.Settings.NameParserAutoIncrementNumber,
            MaxNameLength = taskSettings.AdvancedSettings.NamePatternMaxLength,
            MaxTitleLength = taskSettings.AdvancedSettings.NamePatternMaxTitleLength,
            CustomTimeZone = taskSettings.UploadSettings.UseCustomTimeZone ? taskSettings.UploadSettings.CustomTimeZone : null
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

        if (!string.IsNullOrEmpty(taskSettings.UploadSettings.NameFormatPatternActiveWindow) && !string.IsNullOrEmpty(nameParser.WindowText))
        {
            fileName = nameParser.Parse(taskSettings.UploadSettings.NameFormatPatternActiveWindow);
        }
        else
        {
            fileName = nameParser.Parse(taskSettings.UploadSettings.NameFormatPattern);
        }

        SnapX.Settings.NameParserAutoIncrementNumber = nameParser.AutoIncrementNumber;

        if (!string.IsNullOrEmpty(extension))
        {
            fileName += "." + extension.TrimStart('.');
        }

        return fileName;
    }

    public static string? GetScreenshotsFolder(TaskSettings taskSettings = null, TaskMetadata metadata = null, DateTime? date = null)
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

        if (taskSettings != null && taskSettings.OverrideScreenshotsFolder && !string.IsNullOrEmpty(taskSettings.ScreenshotsFolder))
        {
            screenshotsFolder = nameParser.Parse(taskSettings.ScreenshotsFolder);
        }
        else
        {
            string? subFolderPattern;

            if (!string.IsNullOrEmpty(SnapX.Settings.SaveImageSubFolderPatternWindow) && !string.IsNullOrEmpty(nameParser.WindowText))
            {
                subFolderPattern = SnapX.Settings.SaveImageSubFolderPatternWindow;
            }
            else
            {
                subFolderPattern = SnapX.Settings.SaveImageSubFolderPattern;
            }

            string? subFolderPath = nameParser.Parse(subFolderPattern, dt);
            screenshotsFolder = Path.Combine(SnapX.ScreenshotsParentFolder, subFolderPath);
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

    private static void AddExternalProgramFromRegistry(TaskSettings taskSettings, string name, string fileName)
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

    public static void StartScreenRecording(ScreenRecordOutput outputType, ScreenRecordStartMethod startMethod, TaskSettings taskSettings = null)
    {
        if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

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
            FileHelpers.OpenFolder(SnapX.ScreenshotsParentFolder);
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
                    Verb = "runas"
                };
            }
            else if (isLinux)
            {
                psi = new ProcessStartInfo()
                {
                    FileName = "pkexec",
                    ArgumentList = { exePath },
                    UseShellExecute = false
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
                        "-e", $"do shell script \"'{exePath}' {(arguments ?? "")}\" with administrator privileges"
                    },
                    UseShellExecute = false
                };
            }
            else if (isFreeBSD)
            {
                psi = new ProcessStartInfo()
                {
                    FileName = "doas",
                    ArgumentList = { exePath },
                    UseShellExecute = false
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
        catch
        {
        }
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
        if (string.IsNullOrEmpty(filePath)) return string.Empty;
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
    public static FullOcrModel GetModelForLanguage(string languageCode)
    {
        return languageCode switch
        {
            "eng" => new FullOcrModel(LocalDetectionModel.ChineseV5, LocalClassificationModel.ChineseMobileV2, LocalRecognizationModel.EnglishV4),
            "chi_sim" => LocalFullModels.ChineseV5,
            "chi_tra" => LocalFullModels.TraditionalChineseV3,
            "jpn" => LocalFullModels.JapanV4,
            "kor" => LocalFullModels.KoreanV4,
            "tel" => LocalFullModels.TeluguV4,
            "kan" => LocalFullModels.KannadaV4,
            "tam" => LocalFullModels.TamilV4,
            "ara" => LocalFullModels.ArabicV4,
            "hin" => LocalFullModels.DevanagariV4,
            _ => new FullOcrModel(
                LocalDetectionModel.ChineseV5,
                LocalClassificationModel.ChineseMobileV2,
                GetClosestRecognitionModel(languageCode)
            )
        };
    }
    private static LocalRecognizationModel GetClosestRecognitionModel(string languageCode)
    {
        return languageCode switch
        {
            "spa" => LocalRecognizationModel.EnglishV4,          // Spanish → Latin script
            "fra" => LocalRecognizationModel.EnglishV4,          // French → Latin
            "deu" => LocalRecognizationModel.EnglishV4,          // German → Latin
            "por" => LocalRecognizationModel.EnglishV4,          // Portuguese → Latin
            "tur" => LocalRecognizationModel.EnglishV4,          // Turkish → Latin with diacritics
            "rus" => LocalRecognizationModel.CyrillicV3,         // Russian → Cyrillic
            _ => LocalRecognizationModel.EnglishV4           // Fallback to English
        };
    }

    public static async Task<string> OCRImage(Image? image = null, string? filePath = null, TaskSettings? taskSettings = null, string? languageCode = null)
    {
#if DISABLE_OCR
        DebugHelper.WriteException(new Exception("This build of SnapX was built with DISABLE_OCR build time constant."));
        return string.Empty;
#endif
        var model = GetModelForLanguage(languageCode ?? "eng");
        using var ms = new MemoryStream();

        var imageConfig = Configuration.Default;
        imageConfig.ImageFormatsManager.SetEncoder(AVIFFormat.Instance, AVIFEncoder.Instance);
        imageConfig.ImageFormatsManager.SetDecoder(AVIFFormat.Instance, AVIFDecoder.Instance);
        imageConfig.ImageFormatsManager.AddImageFormatDetector(new PatchedAVIFImageFormatDetector());

        try
        {
            if (filePath is not null && image is null)
            {
                image = await Image.LoadAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            var issue = "Failed to load image for OCR";
            DebugHelper.Logger.Warning(issue);
            DebugHelper.WriteException(ex);
            return issue + Environment.NewLine + ex.Message;
        }
        if (image is null) return "SNAPX ERROR: PASSED NULL IMAGE AND NULL FILEPATH.";
        using (image)
        {
            await image.SaveAsWebpAsync(ms, new WebpEncoder()
            {
                Method = 0
            });
        }
        DebugHelper.WriteLine(filePath);

        // macOS ARM64 does not support ONNX yet.
        var config = model.DetectionModel.Version == ModelVersion.V4 &&
                     !(OperatingSystem.IsMacOS() && RuntimeInformation.OSArchitecture == Architecture.Arm64)
            ? PaddleDevice.Onnx()
            : PaddleDevice.Blas();

        using var all = new PaddleOcrAll(model, config)
        {
            AllowRotateDetection = false,
            Enable180Classification = false,
        };
        // Load local file by following code:
        // using (Mat src2 = Cv2.ImRead(@"C:\test.jpg"))
        DebugHelper.WriteLine($"OCR image bytes: {ms.Length}");
        using var src = Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
        var result = all.Run(src);

        DebugHelper.WriteLine("Detected all texts: \n" + result.Text);
        foreach (var region in result.Regions)
        {
            DebugHelper.WriteLine($"Text: {region.Text}, Score: {region.Score}, RectCenter: {region.Rect.Center}, RectSize:    {region.Rect.Size}, Angle: {region.Rect.Angle}");
        }
        return result.Text;
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
        bool disableHotkeys = !SnapX.Settings.DisableHotkeys;
        ToggleHotkeys(disableHotkeys);
        return disableHotkeys;
    }

    public static void ToggleHotkeys(bool disableHotkeys)
    {
        SnapX.Settings.DisableHotkeys = disableHotkeys;
        SnapX.HotkeyManager.ToggleHotkeys(disableHotkeys);
    }

    public static bool CheckFFmpeg(TaskSettings taskSettings)
    {
        return true;
    }

    public static Screenshot GetScreenshot(TaskSettings taskSettings = null)
    {
        if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

        var screenshot = new Screenshot()
        {
            CaptureCursor = taskSettings.CaptureSettings.ShowCursor,
            CaptureClientArea = taskSettings.CaptureSettings.CaptureClientArea,
            RemoveOutsideScreenArea = true,
            CaptureShadow = taskSettings.CaptureSettings.CaptureShadow,
            ShadowOffset = taskSettings.CaptureSettings.CaptureShadowOffset,
            AutoHideTaskbar = taskSettings.CaptureSettings.CaptureAutoHideTaskbar
        };

        return screenshot;
    }

    public static void ImportCustomUploader(string? filePath)
    {
        if (SnapX.UploadersConfig != null)
        {
            try
            {
                CustomUploaderItem cui = JsonHelpers.DeserializeFromFile<CustomUploaderItem>(filePath);

                if (cui != null)
                {
                    bool activate = false;

                    if (cui.DestinationType == CustomUploaderDestinationType.None)
                    {
                    }
                    else
                    {
                        List<string> destinations = [];
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.ImageUploader)) destinations.Add("images");
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.TextUploader)) destinations.Add("texts");
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.FileUploader)) destinations.Add("files");
                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.URLShortener) ||
                            cui.DestinationType.HasFlag(CustomUploaderDestinationType.URLSharingService)) destinations.Add("urls");

                        string destinationsText = string.Join("/", destinations);
                        DebugHelper.WriteLine($"Set \"{cui}\" as the active custom uploader for {destinationsText}");
                        activate = true;
                    }

                    cui.CheckBackwardCompatibility();
                    SnapX.UploadersConfig.CustomUploadersList.Add(cui);

                    if (activate)
                    {
                        int index = SnapX.UploadersConfig.CustomUploadersList.Count - 1;

                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.ImageUploader))
                        {
                            SnapX.UploadersConfig.CustomImageUploaderSelected = index;
                            SnapX.DefaultTaskSettings.ImageDestination = ImageDestination.CustomImageUploader;
                        }

                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.TextUploader))
                        {
                            SnapX.UploadersConfig.CustomTextUploaderSelected = index;
                            SnapX.DefaultTaskSettings.TextDestination = TextDestination.CustomTextUploader;
                        }

                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.FileUploader))
                        {
                            SnapX.UploadersConfig.CustomFileUploaderSelected = index;
                            SnapX.DefaultTaskSettings.FileDestination = FileDestination.CustomFileUploader;
                        }

                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.URLShortener))
                        {
                            SnapX.UploadersConfig.CustomURLShortenerSelected = index;
                            SnapX.DefaultTaskSettings.URLShortenerDestination = UrlShortenerType.CustomURLShortener;
                        }

                        if (cui.DestinationType.HasFlag(CustomUploaderDestinationType.URLSharingService))
                        {
                            SnapX.UploadersConfig.CustomURLSharingServiceSelected = index;
                            SnapX.DefaultTaskSettings.URLSharingServiceDestination = URLSharingServices.CustomURLSharingService;
                        }
                    }
                    SettingManager.SaveUploadersConfigAsync();
                }
            }
            catch (Exception e)
            {
                e.ShowError();
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
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
            throw new NotImplementedException("ImportImageEffect is not implemented. It relies on SnapX.ImageEffectsLib which is not ported yet.");
        }
    }

    public static async Task HandleNativeMessagingInput(string? filePath, TaskSettings taskSettings = null)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            NativeMessagingInput nativeMessagingInput = null;

            try
            {
                nativeMessagingInput = JsonHelpers.DeserializeFromFile<NativeMessagingInput>(filePath);
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
                if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();

                PlayNotificationSoundAsync(NotificationSound.ActionCompleted, taskSettings);

                switch (nativeMessagingInput.Action)
                {
                    // TEMP: For backward compatibility
                    default:
                        if (!string.IsNullOrEmpty(nativeMessagingInput.URL))
                        {
                            UploadManager.DownloadAndUploadFile(nativeMessagingInput.URL, taskSettings);
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

                            if (image == null && taskSettings.AdvancedSettings.ProcessImagesDuringExtensionUpload)
                            {
                                try
                                {
                                    image = await WebHelpers.DownloadImageAsync(nativeMessagingInput.URL);
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
                                UploadManager.DownloadAndUploadFile(nativeMessagingInput.URL, taskSettings);
                            }
                        }
                        break;
                    case NativeMessagingAction.UploadVideo:
                    case NativeMessagingAction.UploadAudio:
                        if (!string.IsNullOrEmpty(nativeMessagingInput.URL))
                        {
                            UploadManager.DownloadAndUploadFile(nativeMessagingInput.URL, taskSettings);
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
    public static Image GenerateQRCode(string text, int size)
    {
        if (!CheckQRCodeContent(text)) return null;
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
                    Margin = 1
                },
                Renderer = new ImageSharpRenderer<Rgba32>()
            };

            return writer.Write(text);
        }
        catch (Exception e)
        {
            e.ShowError();
        }

        return null;
    }

    public static string[] BarcodeScan(Image<Rgba32> img, bool scanQRCodeOnly = false)
    {
        try
        {
            var barcodeReader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true
                }
            };

            if (scanQRCodeOnly)
            {
                barcodeReader.Options.PossibleFormats = [BarcodeFormat.QR_CODE];
            }

            Result[] results = barcodeReader.DecodeMultiple(img);

            if (results != null)
            {
                return results.Where(x => x != null && !string.IsNullOrEmpty(x.Text)).Select(x => x.Text).ToArray();
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
        if (SnapX.Settings.DisableUpload)
        {

            return false;
        }

        return true;
    }
    public static async Task PlaySound(Stream stream)
    {
        DebugHelper.WriteLine($"PlaySound {stream.Length} bytes {stream.Position} {stream.CanSeek} {stream.CanRead}");
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
            CreateNoWindow = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is not null) await process.WaitForExitAsync();
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
    private static async Task PlaySound(string filePath) => await PlaySound(File.OpenRead(filePath));
    // Coding nerds, please, forgive me for this mortal sin.
    // The code here is instance dependent thus cannot be called from static stuff yada yada yada.
    public static void PlayNotificationSoundAsync(NotificationSound notificationSound, TaskSettings? taskSettings = null)
    {
        taskSettings ??= TaskSettings.GetDefaultTaskSettings();
        switch (notificationSound)
        {
            case NotificationSound.Capture:
                if (taskSettings.GeneralSettings.PlaySoundAfterCapture)
                {
                    if (taskSettings.GeneralSettings.UseCustomCaptureSound && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomCaptureSoundPath))
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
                    if (taskSettings.GeneralSettings.UseCustomTaskCompletedSound && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomTaskCompletedSoundPath))
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
                    if (taskSettings.GeneralSettings.UseCustomActionCompletedSound && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomActionCompletedSoundPath))
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
                    if (taskSettings.GeneralSettings.UseCustomErrorSound && !string.IsNullOrEmpty(taskSettings.GeneralSettings.CustomErrorSoundPath))
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
