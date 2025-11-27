
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using SnapX.Core.Utils.Extensions;
using SnapX.Core.Utils.Miscellaneous;
using Xdg.Directories;

namespace SnapX.Core.Utils;

public static class FileHelpers
{
    public static readonly string[] ImageFileExtensions = ["jpg", "jpeg", "png", "webp", "avif", "gif", "bmp", "ico", "tif", "tiff"];
    public static readonly string[] TextFileExtensions = [ "txt", "log", "nfo", "c", "cpp", "cc", "cxx", "h", "hpp", "hxx", "cs", "vb",
        "html", "htm", "xhtml", "xht", "xml", "css", "js", "php", "bat", "java", "lua", "py", "pl", "cfg", "ini", "dart", "go", "gohtml" ];
    public static readonly string[] VideoFileExtensions = [ "mp4", "webm", "mkv", "avi", "vob", "ogv", "ogg", "mov", "qt", "wmv", "m4p",
        "m4v", "mpg", "mp2", "mpeg", "mpe", "mpv", "m2v", "m4v", "flv", "f4v" ];

    public static string GetFileNameExtension(string? filePath, bool includeDot = false, bool checkSecondExtension = true)
    {
        var extension = "";
        if (string.IsNullOrEmpty(filePath)) return extension;

        var pos = filePath.LastIndexOf('.');
        if (pos < 0) return extension;

        extension = filePath.Substring(pos + 1);

        if (checkSecondExtension)
        {
            filePath = filePath.Remove(pos);
            var extension2 = GetFileNameExtension(filePath, false, false);

            if (!string.IsNullOrEmpty(extension2))
            {
                extension = new[] { "tar" }
                        .FirstOrDefault(knownExtension => extension2.Equals(knownExtension, StringComparison.OrdinalIgnoreCase))
                    is not null
                    ? extension2 + "." + extension
                    : extension;
            }
        }

        if (includeDot)
        {
            extension = "." + extension;
        }


        return extension;
    }

    public static string? GetFileNameSafe(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return filePath;

        var pos = filePath.LastIndexOf('\\');

        if (pos < 0)
        {
            pos = filePath.LastIndexOf('/');
        }

        return pos >= 0 ? filePath.Substring(pos + 1) : filePath;
    }

    public static string? ChangeFileNameExtension(string? fileName, string extension)
    {
        if (string.IsNullOrEmpty(fileName)) return fileName;
        if (string.IsNullOrEmpty(extension)) return fileName;

        var pos = fileName.LastIndexOf('.');

        if (pos >= 0)
        {
            fileName = fileName.Remove(pos);
        }


        pos = extension.LastIndexOf('.');

        if (pos >= 0)
        {
            extension = extension.Substring(pos + 1);
        }

        return fileName + "." + extension;
    }

    public static string AppendTextToFileName(string filePath, string text)
    {
        if (string.IsNullOrEmpty(filePath)) return filePath;

        var pos = filePath.LastIndexOf('.');

        if (pos >= 0)
        {
            return filePath.Substring(0, pos) + text + filePath.Substring(pos);
        }

        return filePath + text;
    }

    public static string? AppendExtension(string filePath, string extension)
    {
        return filePath.TrimEnd('.') + '.' + extension.TrimStart('.');
    }

    public static bool CheckExtension(string? filePath, IEnumerable<string> extensions)
    {
        var ext = GetFileNameExtension(filePath);
        if (string.IsNullOrEmpty(ext)) return false;

        return extensions.Any(x => ext.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsImageFile(string? filePath)
    {
        return CheckExtension(filePath, ImageFileExtensions);
    }

    public static bool IsTextFile(string? filePath)
    {
        return CheckExtension(filePath, TextFileExtensions);
    }

    public static bool IsVideoFile(string? filePath)
    {
        return CheckExtension(filePath, VideoFileExtensions);
    }

    public static EDataType FindDataType(string? filePath)
    {
        if (IsImageFile(filePath))
        {
            return EDataType.Image;
        }

        if (IsTextFile(filePath))
        {
            return EDataType.Text;
        }

        return EDataType.File;
    }

    public static string? GetAbsolutePath(string? path)
    {
        path = ExpandFolderVariables(path);

        if (!Path.IsPathRooted(path)) // Is relative path?
        {
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        return Path.GetFullPath(path);
    }

    public static string GetPathRoot(string? path)
    {
        var separator = path.IndexOf(":\\");
        if (separator < 0) return "";

        return path.Substring(0, separator + 2);
    }

    public static string? SanitizeFileName(string? fileName, string replaceWith = "")
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return SanitizeFileName(fileName, replaceWith, invalidChars);
    }

    private static string? SanitizeFileName(string? fileName, string replaceWith, char[] invalidChars)
    {
        fileName = fileName.Trim();

        fileName = invalidChars.Aggregate(fileName, (current, c) => current.Replace(c.ToString(), replaceWith));

        return fileName;
    }

    public static string? SanitizePath(string? path, string replaceWith = "")
    {
        var root = GetPathRoot(path);

        if (!string.IsNullOrEmpty(root))
        {
            path = path.Substring(root.Length);
        }

        var invalidChars = Path.GetInvalidFileNameChars().Except(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).ToArray();
        path = SanitizeFileName(path, replaceWith, invalidChars);

        return root + path;
    }

    public static bool OpenFile(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
        {
            try
            {
                using var process = new Process();

                var psi = new ProcessStartInfo()
                {
                    FileName = filePath,
                    UseShellExecute = true,
                };

                process.StartInfo = psi;
                process.Start();

                DebugHelper.WriteLine("File opened: " + filePath);

                return true;
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e, $"OpenFile({filePath}) failed.");
            }
        }
        else
        {
            DebugHelper.WriteLine("File does not exist: " + filePath);
        }

        return false;
    }

    public static bool OpenFolder(string? folderPath, bool allowMessageBox = true)
    {
        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
        {
            if (!folderPath.EndsWith(@"\"))
            {
                folderPath += @"\";
            }

            try
            {
                using var process = new Process();

                var psi = new ProcessStartInfo()
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                };

                process.StartInfo = psi;
                process.Start();

                DebugHelper.WriteLine("Folder opened: " + folderPath);

                return true;
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e, $"OpenFolder({folderPath}) failed.");
            }
        }
        else if (allowMessageBox)
        {
            throw new NotImplementedException("OpenFolder is not implemented.");
        }

        return false;
    }

    public static bool OpenFolderWithFile(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
        {
            try
            {

                DebugHelper.WriteLine("Folder opened with file: " + filePath);

                return true;
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e, $"OpenFolderWithFile({filePath}) failed.");
            }
        }
        else
        {
            throw new NotImplementedException("OpenFolderWithFile is not implemented.");
        }

        return false;
    }

    public static string? GetUniqueFilePath(string? filePath)
    {
        if (!System.IO.File.Exists(filePath)) return filePath;

        var folderPath = Path.GetDirectoryName(filePath);
        if (folderPath == null) return filePath;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var fileExtension = Path.GetExtension(filePath);
        var number = 1;

        var regex = Regex.Match(fileName, @"^(.+) \((\d+)\)$");

        if (regex.Success)
        {
            fileName = regex.Groups[1].Value;
            number = int.Parse(regex.Groups[2].Value);
        }

        do
        {
            number++;
            var newFileName = $"{fileName} ({number}){fileExtension}";
            filePath = Path.Combine(folderPath, newFileName);
        }
        while (System.IO.File.Exists(filePath));

        return filePath;
    }
    public static string? GetVariableFolderPath(string? path, bool supportCustomSpecialFolders = false)
    {
        if (string.IsNullOrEmpty(path)) return path;
        try
        {
            if (supportCustomSpecialFolders)
            {
                path = HelpersOptions.ShareXUserFolders
                    .Aggregate(path, (current, userFolder) =>
                        current.Replace(userFolder.Value, $"%{userFolder.Key}%", StringComparison.OrdinalIgnoreCase));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = Helpers.GetEnums<Environment.SpecialFolder>()
                    .Aggregate(path, (current, specialFolder) =>
                        current.Replace(Environment.GetFolderPath(specialFolder), $"%{specialFolder}%", StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return path;
    }

    public static string? ExpandFolderVariables(string? path, bool supportCustomSpecialFolders = false)
    {
        if (string.IsNullOrEmpty(path)) return path;

        try
        {
            path = HelpersOptions.ShareXUserFolders
                .Aggregate(path, (current, userFolder) =>
                    current.Replace($"%{userFolder.Key}%", userFolder.Value, StringComparison.OrdinalIgnoreCase));

            // May produce duplicate entries in HelpersOptions.ShareXUserFolders, it keeps compatability with Windows configurations
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                path = Helpers.GetEnums<Environment.SpecialFolder>()
                    .Aggregate(path, (current, specialFolder) =>
                        current.Replace($"%{specialFolder}%", Environment.GetFolderPath(specialFolder), StringComparison.OrdinalIgnoreCase));
            }

            path = Environment.ExpandEnvironmentVariables(path);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return path;
    }

    public static string OutputSpecialFolders()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return string.Empty;
        var sb = new StringBuilder();

        foreach (var specialFolder in Helpers.GetEnums<Environment.SpecialFolder>())
        {
            sb.AppendLine(string.Format("{0,-25}{1}", specialFolder, Environment.GetFolderPath(specialFolder)));
        }

        return sb.ToString();
    }

    public static bool IsFileLocked(string? filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            fs.Close();
        }
        catch (IOException)
        {
            return true;
        }

        return false;
    }

    public static long GetFileSize(string? filePath)
    {
        try
        {
            return new FileInfo(filePath).Length;
        }
        catch
        {
            // Suppressing the errors since 1984!
        }

        return -1;
    }

    public static string GetFileSizeReadable(string? filePath, bool binaryUnits = false)
    {
        var fileSize = GetFileSize(filePath);

        return fileSize >= 0 ? fileSize.ToSizeString(binaryUnits) : string.Empty;
    }

    public static void CreateDirectory(string? directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || Directory.Exists(directoryPath)) return;

        try
        {
            Directory.CreateDirectory(directoryPath);
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
    }

    public static void CreateDirectoryFromFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        var directoryPath = Path.GetDirectoryName(filePath);
        if (directoryPath == null) return;
        CreateDirectory(directoryPath);
    }

    public static bool IsValidFilePath(string filePath)
    {
        FileInfo fi = null;

        try
        {
            fi = new FileInfo(filePath);
        }
        catch (Exception)
        {
            //
        }

        return fi != null;
    }

    public static string CopyFile(string filePath, string? destinationFolder, bool overwrite = true)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath) || string.IsNullOrEmpty(destinationFolder))
        {
            return null;
        }

        var fileName = Path.GetFileName(filePath);
        var destinationFilePath = Path.Combine(destinationFolder, fileName);
        CreateDirectory(destinationFolder);
        System.IO.File.Copy(filePath, destinationFilePath, overwrite);
        return destinationFilePath;
    }

    public static void CopyFiles(string filePath, string destinationFolder)
    {
        CopyFiles([filePath], destinationFolder);
    }

    public static void CopyFiles(string[] files, string destinationFolder)
    {
        if (files == null || files.Length == 0) return;

        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        foreach (var filePath in files)
        {
            var fileName = Path.GetFileName(filePath);
            var destinationFilePath = Path.Combine(destinationFolder, fileName);
            System.IO.File.Copy(filePath, destinationFilePath);
        }
    }

    public static void CopyFiles(string sourceFolder, string destinationFolder, string searchPattern = "*", string[] ignoreFiles = null)
    {
        var files = Directory.GetFiles(sourceFolder, searchPattern);

        if (ignoreFiles != null)
        {
            List<string> newFiles = [];

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);

                if (ignoreFiles.All(x => !fileName.Equals(x, StringComparison.OrdinalIgnoreCase)))
                {
                    newFiles.Add(file);
                }
            }

            files = newFiles.ToArray();
        }

        CopyFiles(files, destinationFolder);
    }

    public static void CopyAll(string sourceDirectory, string targetDirectory)
    {
        var diSource = new DirectoryInfo(sourceDirectory);
        var diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        if (!Directory.Exists(target.FullName))
        {
            Directory.CreateDirectory(target.FullName);
        }

        foreach (var fi in source.GetFiles())
        {
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        foreach (var diSourceSubDir in source.GetDirectories())
        {
            var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    public static string MoveFile(string filePath, string? destinationFolder, bool overwrite = true)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath) ||
            string.IsNullOrEmpty(destinationFolder)) return null;

        var fileName = Path.GetFileName(filePath);
        var destinationFilePath = Path.Combine(destinationFolder, fileName);
        CreateDirectory(destinationFolder);

        if (overwrite && System.IO.File.Exists(destinationFilePath))
        {
            System.IO.File.Delete(destinationFilePath);
        }

        System.IO.File.Move(filePath, destinationFilePath);
        return destinationFilePath;
    }

    public static string RenameFile(string filePath, string newFileName)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return null;

            var directory = Path.GetDirectoryName(filePath);
            var newFilePath = Path.Combine(directory, newFileName);
            System.IO.File.Move(filePath, newFilePath);
            return newFilePath;
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return filePath;
    }

    public static bool DeleteFile(string filePath, bool sendToRecycleBin = false)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return false;

            if (sendToRecycleBin)
            {
                FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }
            else
            {
                System.IO.File.Delete(filePath);
            }

            return true;
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    public static string BackupFileWeekly(string filePath, string? destinationFolder)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return null;

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var dateTime = DateTime.Now;
        var extension = Path.GetExtension(filePath);
        var newFileName = string.Format("{0}-{1:yyyy-MM}-W{2:00}{3}", fileName, dateTime, dateTime.WeekOfYear(), extension);
        var newFilePath = Path.Combine(destinationFolder, newFileName);

        if (System.IO.File.Exists(newFilePath)) return null;

        CreateDirectory(destinationFolder);
        System.IO.File.Copy(filePath, newFilePath, false);
        return newFilePath;
    }

    public static void BackupFileMonthly(string? filePath, string? destinationFolder)
    {
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var newFileName = string.Format("{0}-{1:yyyy-MM}{2}", fileName, DateTime.Now, extension);
        var newFilePath = Path.Combine(destinationFolder, newFileName);

        if (System.IO.File.Exists(newFilePath)) return;

        CreateDirectory(destinationFolder);
        System.IO.File.Copy(filePath, newFilePath, false);
    }

    public static string GetTempFilePath(string extension)
    {
        var tempFolder = Path.Combine(BaseDirectory.CacheHome, SnapX.AppName);
        Directory.CreateDirectory(tempFolder);
        var tempFilePath = Path.ChangeExtension(Path.Combine(tempFolder, Path.GetRandomFileName()), extension);
        System.IO.File.Create(tempFilePath).Dispose();
        return tempFilePath;
    }

    public static void CreateEmptyFile(string filePath)
    {
        System.IO.File.Create(filePath).Dispose();
    }

    public static IEnumerable<string> GetFilesByExtensions(string? directoryPath, params string[] extensions)
    {
        return GetFilesByExtensions(new DirectoryInfo(directoryPath), extensions);
    }

    public static IEnumerable<string> GetFilesByExtensions(DirectoryInfo directoryInfo, params string[] extensions)
    {
        var allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        return directoryInfo.EnumerateFiles().Where(f => allowedExtensions.Contains(f.Extension)).Select(x => x.FullName);
    }
}

