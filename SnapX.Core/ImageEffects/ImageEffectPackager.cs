
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Text;
using SnapX.Core.Upload.Zip;
using SnapX.Core.Utils;

namespace SnapX.Core.ImageEffects;

public static class ImageEffectPackager
{
    private const string? ConfigFileName = "Config.json";

    public static string? Package(string? outputFilePath, string configJson, string assetsFolderPath)
    {
        if (!string.IsNullOrEmpty(outputFilePath))
        {
            List<ZipEntryInfo> entries = [];

            byte[] bytes = Encoding.UTF8.GetBytes(configJson);
            MemoryStream ms = new MemoryStream(bytes);
            entries.Add(new ZipEntryInfo(ms, ConfigFileName));

            if (!string.IsNullOrEmpty(assetsFolderPath) && Directory.Exists(assetsFolderPath))
            {
                string parentFolderPath = Directory.GetParent(assetsFolderPath).FullName;
                int entryNamePosition = parentFolderPath.Length + 1;

                foreach (string? assetPath in Directory.EnumerateFiles(assetsFolderPath, "*.*", SearchOption.AllDirectories).Where(x => FileHelpers.IsImageFile(x)))
                {
                    string? entryName = assetPath.Substring(entryNamePosition);
                    entries.Add(new ZipEntryInfo(assetPath, entryName));
                }
            }

            ZipManager.Compress(outputFilePath, entries);

            return outputFilePath;
        }

        return null;
    }

    public static string ExtractPackage(string packageFilePath, string? destination)
    {
        string configJson = null;

        if (!string.IsNullOrEmpty(packageFilePath) && File.Exists(packageFilePath) && !string.IsNullOrEmpty(destination))
        {
            ZipManager.Extract(packageFilePath, destination, true, entry =>
            {
                if (FileHelpers.IsImageFile(entry.Name))
                {
                    return true;
                }

                if (configJson == null && entry.FullName.Equals(ConfigFileName, StringComparison.OrdinalIgnoreCase))
                {
                    using (Stream stream = entry.Open())
                    using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
                    {
                        configJson = streamReader.ReadToEnd();
                    }
                }

                return false;
            }, 100_000_000);
        }

        return configJson;
    }
}
