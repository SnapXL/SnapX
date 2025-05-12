
// SPDX-License-Identifier: GPL-3.0-or-later



using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
#if WINDOWS
using Esatto.Win32.Registry;
#endif
using Microsoft.Extensions.Configuration;
using SnapX.Core.History;
using SnapX.Core.Hotkey;
using SnapX.Core.Job;
using SnapX.Core.Upload;
using SnapX.Core.Upload.Zip;
using SnapX.Core.Utils;

namespace SnapX.Core;
internal static class SettingManager
{
    private const string ApplicationConfigFileName = "ApplicationConfig.json";

    private static string ApplicationConfigFilePath
    {
        get
        {
            if (SnapX.Sandbox) return "";

            return Path.Combine(SnapX.ConfigFolder, ApplicationConfigFileName);
        }
    }

    private const string UploadersConfigFileName = "UploadersConfig.json";

    private static string UploadersConfigFilePath
    {
        get
        {
            if (SnapX.Sandbox) return "";

            string uploadersConfigFolder;

            if (!string.IsNullOrEmpty(Settings.CustomUploadersConfigPath))
            {
                uploadersConfigFolder = FileHelpers.ExpandFolderVariables(Settings.CustomUploadersConfigPath);
            }
            else
            {
                uploadersConfigFolder = SnapX.ConfigFolder;
            }

            return Path.Combine(uploadersConfigFolder, UploadersConfigFileName);
        }
    }

    private const string HotkeysConfigFileName = "HotkeysConfig.json";

    private static string HotkeysConfigFilePath
    {
        get
        {
            if (SnapX.Sandbox) return "";

            string hotkeysConfigFolder;

            if (!string.IsNullOrEmpty(Settings.CustomHotkeysConfigPath))
            {
                hotkeysConfigFolder = FileHelpers.ExpandFolderVariables(Settings.CustomHotkeysConfigPath);
            }
            else
            {
                hotkeysConfigFolder = SnapX.ConfigFolder;
            }

            return Path.Combine(hotkeysConfigFolder, HotkeysConfigFileName);
        }
    }

    public static string SnapshotFolder => Path.Combine(SnapX.PersonalFolder, "Snapshots");

    private static RootConfiguration Settings { get => SnapX.Settings; set => SnapX.Settings = value; }
    private static TaskSettings DefaultTaskSettings { get => SnapX.DefaultTaskSettings; set => SnapX.DefaultTaskSettings = value; }
    private static UploadersConfig UploadersConfig { get => SnapX.UploadersConfig; set => SnapX.UploadersConfig = value; }
    private static HotkeysConfig HotkeysConfig { get => SnapX.HotkeysConfig; set => SnapX.HotkeysConfig = value; }

    private static ManualResetEvent uploadersConfigResetEvent = new(false);
    private static ManualResetEvent hotkeysConfigResetEvent = new(false);

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static void LoadSettings()
    {
        LoadApplicationConfig();
        LoadUploadersConfig();
        LoadHotkeysConfig();
    }

    public static void WaitUploadersConfig()
    {
        if (UploadersConfig == null)
        {
            uploadersConfigResetEvent.WaitOne();
        }
    }

    public static void WaitHotkeysConfig()
    {
        if (HotkeysConfig == null)
        {
            hotkeysConfigResetEvent.WaitOne();
        }
    }

    [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    public static void LoadApplicationConfig(bool fallbackSupport = true)
    {
        var configurationBuilder = new ConfigurationBuilder();
        if (!SnapX.Sandbox)
        {
            // configurationBuilder.AddJsonFile(ApplicationConfigFilePath, optional: true, reloadOnChange: true);
            configurationBuilder.AddSqliteSettings(SnapX.DbConnection);
        }

        configurationBuilder
#if WINDOWS
        .AddRegistry(@"Software\BrycensRanch\SnapX")
#endif
        .AddCommandLine(Environment.GetCommandLineArgs())

        // .AddInMemoryCollection()
        // Allows ALL settings to be managed via the Windows Registry.
        // This call does nothing on non-Windows Operating Systems
        .AddEnvironmentVariables(prefix: "SNAPX_");
        SnapX.Configuration = configurationBuilder.Build();
        foreach (var kv in SnapX.Configuration.AsEnumerable())
        {
            DebugHelper.WriteLine($"{kv.Key} = {kv.Value}");
        }
        var settings = new RootConfiguration();
        SnapX.Configuration.Bind(settings);
        Settings = settings;
        if (string.IsNullOrWhiteSpace(Settings.SQLitePath))
            Settings.SQLitePath = Path.Combine(SnapX.DefaultPersonalFolder, "SnapX.db");
        ApplicationConfigBackwardCompatibilityTasks();
        MigrateHistoryFile();
    }

    [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    public static void LoadUploadersConfig(bool fallbackSupport = true)
    {
        var configurationBuilder = new ConfigurationBuilder()
            // .AddInMemoryCollection()
            // Allows ALL settings to be managed via the Windows Registry.
            // This call does nothing on non-Windows Operating Systems
#if WINDOWS
            .AddRegistry(@"Software\BrycensRanch\SnapX")
#endif
            .AddEnvironmentVariables(prefix: "SNAPX_")
            .AddCommandLine(Environment.GetCommandLineArgs());
        if (!SnapX.Sandbox)
        {
            // configurationBuilder.AddJsonFile(UploadersConfigFilePath, optional: true, reloadOnChange: true);
        }
        var BuiltConfig = configurationBuilder.Build();
        UploadersConfig = new UploadersConfig();
        BuiltConfig.Bind(UploadersConfig);
        UploadersConfigBackwardCompatibilityTasks();
    }

    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    public static void LoadHotkeysConfig(bool fallbackSupport = true)
    {
        var configurationBuilder = new ConfigurationBuilder()
            // .AddInMemoryCollection()
            // Allows ALL settings to be managed via the Windows Registry.
            // This call does nothing on non-Windows Operating Systems
#if WINDOWS
            .AddRegistry(@"Software\BrycensRanch\SnapX")
#endif
            .AddEnvironmentVariables(prefix: "SNAPX_")
            .AddCommandLine(Environment.GetCommandLineArgs());
        if (!SnapX.Sandbox)
        {
            // configurationBuilder.AddJsonFile(HotkeysConfigFilePath, optional: true, reloadOnChange: true);
        }
        var BuiltConfig = configurationBuilder.Build();
        HotkeysConfig = new HotkeysConfig();
        BuiltConfig.Bind(HotkeysConfig);
        HotkeysConfigBackwardCompatibilityTasks();
    }

    private static void ApplicationConfigBackwardCompatibilityTasks()
    {
        // if (Settings.IsUpgradeFrom("16.0.2"))
        // {
        //     if (Settings.CheckPreReleaseUpdates)
        //     {
        //         Settings.UpdateChannel = UpdateChannel.PreRelease;
        //     }
        // }
    }
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private static async Task MigrateHistoryFile()
    {
        if (!File.Exists(SnapX.HistoryFilePathOld)) return;

        TaskManager.InitHistoryManager();
        DebugHelper.WriteLine($"JSON -> SQLite Migration: Migrating history ({FileHelpers.GetFileSizeReadable(SnapX.HistoryFilePathOld)})");

        FileHelpers.BackupFileMonthly(SnapX.HistoryFilePathOld, SnapshotFolder);

        try
        {
            var json = await File.ReadAllTextAsync(SnapX.HistoryFilePathOld);
            if (!json.StartsWith('[')) json = "[" + json + "]";

            if (string.IsNullOrEmpty(json) || json == "{}" || json == "[{}]")
            {
                DebugHelper.WriteLine("JSON -> SQLite Migration: Old history file is empty. Deleting it to prevent the migration running again.");
                File.Delete(SnapX.HistoryFilePathOld);
            }

            var historyItems = JsonSerializer.Deserialize<List<HistoryItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = HistoryContext.Default,
            });
            var shouldRenameFile = true;
            if (historyItems?.Count > 0)
            {
                DebugHelper.WriteLine($"JSON -> SQLite Migration: Found {historyItems.Count:N0} history items. First one is {historyItems[0].FilePath} and the last one is {historyItems[^1].FilePath}");
                if (!TaskManager.History.AppendHistoryItems(historyItems))
                {
                    DebugHelper.WriteLine("JSON -> SQLite Migration: Failed to migrate history items.");
                    shouldRenameFile = false;
                }
                else
                {
                    DebugHelper.WriteLine("JSON -> SQLite Migration: Migration complete! Welcome to the future! 🚀");
                }
            }
            else
            {
                DebugHelper.WriteLine("JSON -> SQLite Migration: No history items found");
            }

            if (shouldRenameFile)
            {
                var migratedPath = SnapX.HistoryFilePathOld + ".migrated";
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                migratedPath = SnapX.HistoryFilePathOld + $"{timestamp}.migrated";
                File.Move(SnapX.HistoryFilePathOld, migratedPath);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"JSON -> SQLite Migration: Migration failed!!!");
            DebugHelper.WriteException(ex);
        }
    }

    private static void UploadersConfigBackwardCompatibilityTasks()
    {
        if (UploadersConfig.CustomUploadersList != null)
        {
            foreach (var cui in UploadersConfig.CustomUploadersList)
            {
                cui.CheckBackwardCompatibility();
            }
        }
    }

    private static void HotkeysConfigBackwardCompatibilityTasks()
    {

        // if (Settings.IsUpgradeFrom("15.0.1"))
        // {
        //     foreach (var taskSettings in HotkeysConfig.Hotkeys.Select(x => x.TaskSettings))
        //     {
        //         if (tasktaskSettings.CaptureSettings != null)
        //         {
        //             // taskSettings.CaptureSettings.ScrollingCaptureOptions = new ScrollingCaptureOptions();
        //             // taskSettings.CaptureSettings.FFmpegOptions.FixSources();
        //         }
        //     }
        // }
    }

    public static void CleanupHotkeysConfig()
    {
        foreach (var taskSettings in HotkeysConfig.Hotkeys.Select(x => x.TaskSettings))
        {
            taskSettings.Cleanup();
        }
    }

    public static void SaveAllSettings()
    {
        // Settings.Save(ApplicationConfigFilePath);
        // UploadersConfig.Save(UploadersConfigFilePath);
        // CleanupHotkeysConfig();
        // HotkeysConfig.Save(HotkeysConfigFilePath);
    }

    public static void SaveApplicationConfigAsync()
    {
        if (Settings != null)
        {
            // Settings.SaveAsync(ApplicationConfigFilePath);
        }
    }

    public static void SaveUploadersConfigAsync()
    {
        // UploadersConfig.SaveAsync(UploadersConfigFilePath);
    }

    public static void SaveHotkeysConfigAsync()
    {
        CleanupHotkeysConfig();
        // HotkeysConfig.SaveAsync(HotkeysConfigFilePath);
    }

    public static void SaveAllSettingsAsync()
    {
        SaveApplicationConfigAsync();
        SaveUploadersConfigAsync();
        SaveHotkeysConfigAsync();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static void ResetSettings()
    {
        if (File.Exists(ApplicationConfigFilePath)) File.Delete(ApplicationConfigFilePath);
        LoadApplicationConfig(false);

        if (File.Exists(UploadersConfigFilePath)) File.Delete(UploadersConfigFilePath);
        LoadUploadersConfig(false);

        if (File.Exists(HotkeysConfigFilePath)) File.Delete(HotkeysConfigFilePath);
        LoadHotkeysConfig(false);
    }

    public static bool Export(string archivePath, bool settings, bool history)
    {
        MemoryStream msApplicationConfig = null, msUploadersConfig = null, msHotkeysConfig = null;

        try
        {
            var entries = new List<ZipEntryInfo>();

            if (settings)
            {
                // msApplicationConfig = Settings.SaveToMemoryStream(false);
                // entries.Add(new ZipEntryInfo(msApplicationConfig, ApplicationConfigFileName));
                //
                // msUploadersConfig = UploadersConfig.SaveToMemoryStream(false);
                // entries.Add(new ZipEntryInfo(msUploadersConfig, UploadersConfigFileName));
                //
                // msHotkeysConfig = HotkeysConfig.SaveToMemoryStream(false);
                // entries.Add(new ZipEntryInfo(msHotkeysConfig, HotkeysConfigFileName));
            }

            if (history)
            {
                entries.Add(new ZipEntryInfo(SnapX.DBPath));
            }

            ZipManager.Compress(archivePath, entries);
            return true;
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }
        finally
        {
            msApplicationConfig?.Dispose();
            msUploadersConfig?.Dispose();
            msHotkeysConfig?.Dispose();
        }

        return false;
    }

    public static bool Import(string archivePath)
    {
        try
        {
            ZipManager.Extract(archivePath, SnapX.ConfigFolder, true, entry =>
            {
                return FileHelpers.CheckExtension(entry.Name, new[] { "db" });
            }, 1_000_000_000);

            return true;
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);
        }

        return false;
    }

    // This method validates the setting before restoration to ensure it fits the defined type
    private static bool IsValidValue(string value, string dataType)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false; // Prevent null or empty values.

        switch (dataType)
        {
            case "int":
                return int.TryParse(value, out _);
            case "double":
                return double.TryParse(value, out _);
            case "bool":
                return bool.TryParse(value, out _);
            case "string":
                return true;
            default:
                DebugHelper.WriteLine($"Unknown data type: {dataType}");
                return false;
        }
    }
}

