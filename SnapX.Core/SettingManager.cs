
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Dapper;
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
using SnapX.Core.Utils.Converters;
using SnapX.Core.Utils.Extensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SnapX.Core;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]

[JsonSerializable(typeof(ApplicationConfig))]
[JsonSerializable(typeof(UploadersConfig))]
[JsonSerializable(typeof(HotkeysConfig))]
[JsonSerializable(typeof(SettingsBase<ApplicationConfig>))]
[JsonSerializable(typeof(SettingsBase<HotkeysConfig>))]
[JsonSerializable(typeof(SettingsBase<UploadersConfig>))]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(Array))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
// [JsonSerializable(typeof(Keys))]
// [JsonSerializable(typeof(SafeEnumConverter<Keys>))]
// [JsonSerializable(typeof(SafeEnumConverter<Modifiers>))]
// [JsonSerializable(typeof(SafeEnumConverter<HotkeyType>))]
// [JsonSerializable(typeof(SafeEnumConverter<HotkeyStatus>))]

internal partial class SettingsContext : JsonSerializerContext;

public static class SettingManager
{
    private const string ApplicationConfigFileName = "ApplicationConfig.yaml";

    private static string ApplicationConfigFilePath
    {
        get
        {
            if (SnapXL.Sandbox) return "";

            return Path.Combine(SnapXL.ConfigFolder, ApplicationConfigFileName);
        }
    }

    private const string UploadersConfigFileName = "UploadersConfig.yaml";

    private static string UploadersConfigFilePath
    {
        get
        {
            if (SnapXL.Sandbox) return "";

            string? uploadersConfigFolder;

            if (!string.IsNullOrEmpty(Settings.CustomUploadersConfigPath))
            {
                uploadersConfigFolder = FileHelpers.ExpandFolderVariables(Settings.CustomUploadersConfigPath);
            }
            else
            {
                uploadersConfigFolder = SnapXL.ConfigFolder;
            }

            return Path.Combine(uploadersConfigFolder, UploadersConfigFileName);
        }
    }

    private const string HotkeysConfigFileName = "HotkeysConfig.yaml";

    private static string HotkeysConfigFilePath
    {
        get
        {
            if (SnapXL.Sandbox) return "";

            string? hotkeysConfigFolder;

            if (!string.IsNullOrEmpty(Settings.CustomHotkeysConfigPath))
            {
                hotkeysConfigFolder = FileHelpers.ExpandFolderVariables(Settings.CustomHotkeysConfigPath);
            }
            else
            {
                hotkeysConfigFolder = SnapXL.ConfigFolder;
            }

            return Path.Combine(hotkeysConfigFolder, HotkeysConfigFileName);
        }
    }

    public static string? SnapshotFolder => Path.Combine(SnapXL.PersonalFolder, "Snapshots");

    private static ApplicationConfig Settings { get => SnapXL.Settings; set => SnapXL.Settings = value; }
    private static TaskSettings DefaultTaskSettings { get => SnapXL.DefaultTaskSettings; set => SnapXL.DefaultTaskSettings = value; }
    private static UploadersConfig UploadersConfig { get => SnapXL.UploadersConfig; set => SnapXL.UploadersConfig = value; }
    private static HotkeysConfig HotkeysConfig { get => SnapXL.HotkeysConfig; set => SnapXL.HotkeysConfig = value; }
    private static VersionEnforcer theLaw;

    private static ManualResetEvent uploadersConfigResetEvent = new(false);
    private static ManualResetEvent hotkeysConfigResetEvent = new(false);

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static void LoadInitialSettings()
    {
        theLaw = new VersionEnforcer(SnapXL.LockDirectory);
        theLaw.Enforce();
        LoadApplicationConfig();

        Task.Run(() =>
        {
            LoadUploadersConfig();
            uploadersConfigResetEvent.Set();

            LoadHotkeysConfig();
            hotkeysConfigResetEvent.Set();
        });
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
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    public static void LoadApplicationConfig(bool fallbackSupport = true)
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder
#if WINDOWS
        .AddRegistry(@"Software\SnapXL\SnapX")
#endif
        .AddCommandLine(Environment.GetCommandLineArgs())

        // .AddInMemoryCollection()
        // Allows ALL settings to be managed via the Windows Registry.
        // This call does nothing on non-Windows Operating Systems
        .AddEnvironmentVariables(prefix: "SNAPX_");
        SnapXL.Configuration = configurationBuilder.Build();
        Settings = ApplicationConfig.Load(ApplicationConfigFilePath, SnapshotFolder, fallbackSupport);
        Settings.CreateBackup = true;
        Settings.CreateWeeklyBackup = true;
        Settings.SettingsSaveFailed += Settings_SettingsSaveFailed;
        SnapXL.Configuration.Bind(Settings, Options => Options.BindNonPublicProperties = true);
        DefaultTaskSettings = Settings.DefaultTaskSettings;
        ApplicationConfigBackwardCompatibilityTasks();

        if (string.IsNullOrWhiteSpace(Settings.SQLitePath))
            Settings.SQLitePath = Path.Combine(SnapXL.DefaultPersonalFolder, "SnapX.db");
        MigrateHistoryFile();
    }
    private static void Settings_SettingsSaveFailed(Exception e)
    {
        string message;

        if (e is UnauthorizedAccessException || e is FileNotFoundException)
        {
            message = "YourAntiVirusSoftwareOrTheControlledFolderAccessFeatureInWindowsCouldBeBlockingShareX";
        }
        else
        {
            message = e.Message;
        }
        DebugHelper.WriteLine($"ShareX - {message} failed to save settings");
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
            .AddRegistry(@"Software\SnapXL\SnapX")
#endif
            .AddEnvironmentVariables(prefix: "SNAPX_")
            .AddCommandLine(Environment.GetCommandLineArgs());
        var BuiltConfig = configurationBuilder.Build();
        UploadersConfig = UploadersConfig.Load(UploadersConfigFilePath, SnapshotFolder, fallbackSupport);
        foreach (var ftpAccount in UploadersConfig.FTPAccountList)
        {
            DebugHelper.WriteLine($"{ftpAccount.Username}@{ftpAccount.Host}:{ftpAccount.Port}");
        }
        UploadersConfig.CreateBackup = true;
        UploadersConfig.CreateWeeklyBackup = true;
        UploadersConfig.UseEncryption = true;
        BuiltConfig.Bind(UploadersConfig, Options => Options.BindNonPublicProperties = true);
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
            .AddRegistry(@"Software\SnapXL\SnapX")
#endif
            .AddEnvironmentVariables(prefix: "SNAPX_")
            .AddCommandLine(Environment.GetCommandLineArgs());
        var BuiltConfig = configurationBuilder.Build();
        HotkeysConfig = HotkeysConfig.Load(HotkeysConfigFilePath, SnapshotFolder, fallbackSupport);
        HotkeysConfig.CreateBackup = true;
        HotkeysConfig.CreateWeeklyBackup = true;
        if (HotkeysConfig.Hotkeys.Count <= 0) HotkeysConfig.Hotkeys = HotkeyManager.GetDefaultHotkeyList();

        BuiltConfig.Bind(HotkeysConfig, Options => Options.BindNonPublicProperties = true);
        HotkeysConfigBackwardCompatibilityTasks();
    }

    [DapperAot]
    private static void ApplicationConfigBackwardCompatibilityTasks()
    {
        if (File.Exists(SnapXL.ApplicationConfigPathOld)) Settings = MigrateJsonConfig<ApplicationConfig>(SnapXL.ApplicationConfigPathOld);

        if (File.Exists(ApplicationConfigFilePath)) MigrateApplicationConfig();

        var assembly = Assembly.GetExecutingAssembly();

        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains("Migrations") && name.EndsWith(".sql"))
            .OrderBy(name => name) // Ensure migrations are processed in numerical order
            .ToList();

        // Ensure MigrationLog table exists for the checks below
        // This is crucial if it's the very first time running migrations.
        SnapXL.DbConnection.Execute(@"
        CREATE TABLE IF NOT EXISTS MigrationLog (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FileName TEXT NOT NULL UNIQUE,
            AppliedOn TEXT DEFAULT CURRENT_TIMESTAMP NOT NULL
        );
    ");

        // Track the highest migration version number that has been applied.
        // Initialize with -1 to handle the case where no migrations have been applied yet.
        long lastAppliedMigrationVersion = -1;

        // First, load the last applied migration version from the database, if MigrationLog exists and has entries.
        // If MigrationLog is empty or doesn't exist, lastAppliedMigrationVersion remains -1.
        var currentMigrationsInDb = SnapXL.DbConnection.QueryFirstOrDefault<string>(
            "SELECT FileName FROM MigrationLog ORDER BY FileName DESC LIMIT 1;"
        );

        if (currentMigrationsInDb != null)
        {
            // Extract the numerical prefix (e.g., "001" from "001_initial_schema.sql")
            var lastAppliedPrefix = currentMigrationsInDb.Split('_').FirstOrDefault();
            if (long.TryParse(lastAppliedPrefix, out long parsedVersion))
            {
                lastAppliedMigrationVersion = parsedVersion;
            }
            else
            {
                // Handle case where migration file name doesn't follow expected format
                // This might indicate a malformed log entry, or an unexpected file name.
                throw new FormatException(
                    $"Unable to verify database version from '{currentMigrationsInDb}'. " +
                    "To protect your data, migrations will not run. " +
                    "Please ensure you are using the latest version of SnapX or contact support if the issue persists. " +
                    "Manual intervention is required."
                );
            }
        }


        foreach (var resourceName in resourceNames)
        {
            var fileName = resourceName.Split('.').Reverse().Take(2).Reverse().Aggregate((a, b) => $"{a}.{b}");

            // Extract the numerical prefix for the current migration file
            var currentMigrationPrefix = fileName.Split('_').FirstOrDefault();
            if (!long.TryParse(currentMigrationPrefix, out long currentMigrationVersion))
            {
                throw new InvalidOperationException($"Migration file '{fileName}' does not have a valid numerical prefix (e.g., '001_'). Please rename the file.");
            }

            if (currentMigrationVersion < 0)
            {
                throw new InvalidOperationException($"Migration file '{fileName}' has a migration version less than  zero. Please rename the file.");
            }

            // Check for out-of-order migration
            if (currentMigrationVersion <= lastAppliedMigrationVersion)
            {
                // Check if this specific migration is already applied.
                // If it is, and its version is <= lastApplied, then it's either already processed
                // or an older one found after newer ones.
                var alreadyApplied = SnapXL.DbConnection.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM MigrationLog WHERE FileName = @FileName",
                    new { FileName = fileName }
                ) > 0;

                if (alreadyApplied)
                {
                    // If it's already applied, and its version is <= lastAppliedMigrationVersion,
                    // it means it was applied correctly in its turn or a duplicate check.
                    // We just continue to the next one.
                    continue;
                }
                // but its version number is LESS THAN or EQUAL TO the last applied version,
                // it means an out-of-order or duplicate migration is being attempted.
                throw new InvalidOperationException(
                    $"Out-of-order migration detected! Attempted to apply '{fileName}' (version {currentMigrationVersion}), " +
                    $"but a newer migration (version {lastAppliedMigrationVersion}) has already been applied. " +
                    "Please ensure to NAG developer that migrations are applied in strictly increasing numerical order."
                );
            }
            if (currentMigrationVersion - lastAppliedMigrationVersion > 1 && lastAppliedMigrationVersion != -1)
            {
                throw new InvalidOperationException(
                    $"Cannot apply migration version {currentMigrationVersion} because the previous applied version is {lastAppliedMigrationVersion}. " +
                    $"You must apply intermediate migrations in order to avoid data loss."
                );
            }

            // If we reach here, the current migration version is > lastAppliedMigrationVersion,
            // so it's a valid next migration. Update lastAppliedMigrationVersion.
            lastAppliedMigrationVersion = currentMigrationVersion;


            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream!);
            var sql = reader.ReadToEnd();

            using var tx = SnapXL.DbConnection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                SnapXL.DbConnection.Execute(sql, transaction: tx);
                SnapXL.DbConnection.Execute("INSERT INTO MigrationLog (FileName) VALUES (@FileName)", new { FileName = fileName }, transaction: tx);
                tx.Commit();
                DebugHelper.WriteLine($"Applied SQL Migration: {fileName}");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                DebugHelper.WriteException(ex);
                throw new InvalidOperationException($"Failed to apply migration '{fileName}'. Transaction rolled back.", ex);
            }
        }

        SnapXL.DbConnection.Execute("PRAGMA optimize;");
    }

    private static T MigrateJsonConfig<T>(string path)
    {

        var typeName = typeof(T).Name;
        // if (!File.Exists(path)) return default;
        FileHelpers.BackupFileMonthly(path, SnapshotFolder);
        DebugHelper.WriteAlways($"Config JSON -> YAML Migration: Migrating {typeName}.json ({path})");
        var rawJSON = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<T>(
            rawJSON,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = SettingsContext.Default.WithAddedModifier(typeInfo => JsonEncryptionResolver.CreateModifier(typeInfo, SecurePropertyStore.Instance)),
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                AllowTrailingCommas = true,
                Converters =
                {
                    new JsonRectangleConverter(),
                    new JsonPointConverter(),
                    new JsonPaddingConverter(),
                    new JsonTimeZoneInfoConverter(),
                    new JsonSizeConverter(),
                    new UtcDateTimeConverter(),
                    new JsonColorConverter(),
                    new JsonFontConverter(),
                    new JsonStringEnumConverter(),
                },
            }
        );
        if (settings is null) return settings ?? throw new Exception($"{typeName} object is null.");
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var migratedPath = path + $"{timestamp}.migrated";
        File.Move(path, migratedPath);
        return settings;
    }

    private static void MigrateApplicationConfig()
    {
        if (Settings.DisableUpload)
        {
            DefaultTaskSettings.AfterCaptureJob = DefaultTaskSettings.AfterCaptureJob.Remove(AfterCaptureTasks.UploadImageToHost);
        }
    }

    private static async Task MigrateHistoryFile()
    {
        if (SnapXL.Sandbox)
            return;
        if (!File.Exists(SnapXL.HistoryFilePathOld)) return;

        TaskManager.InitHistoryManager();
        DebugHelper.WriteAlways($"JSON -> SQLite Migration: Migrating history ({FileHelpers.GetFileSizeReadable(SnapXL.HistoryFilePathOld)})");

        FileHelpers.BackupFileMonthly(SnapXL.HistoryFilePathOld, SnapshotFolder);

        try
        {
            var json = await File.ReadAllTextAsync(SnapXL.HistoryFilePathOld);
            if (!json.StartsWith('[')) json = "[" + json + "]";

            if (string.IsNullOrEmpty(json) || json == "{}" || json == "[{}]")
            {
                DebugHelper.WriteAlways("JSON -> SQLite Migration: Old history file is empty. Deleting it to prevent the migration running again.");
                File.Delete(SnapXL.HistoryFilePathOld);
            }

            var historyItems = JsonSerializer.Deserialize<List<HistoryItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                TypeInfoResolver = HistoryContext.Default,
            });
            var shouldRenameFile = true;
            if (historyItems?.Count > 0)
            {
                DebugHelper.WriteAlways($"JSON -> SQLite Migration: Found {historyItems.Count:N0} history items. First one is {historyItems[0].FilePath} and the last one is {historyItems[^1].FilePath}");
                if (!TaskManager.History.AppendHistoryItems(historyItems))
                {
                    DebugHelper.WriteAlways("JSON -> SQLite Migration: Failed to migrate history items.");
                    shouldRenameFile = false;
                }
                else
                {
                    DebugHelper.WriteAlways("JSON -> SQLite Migration: Migration complete! Welcome to the future! 🚀");
                }
            }
            else
            {
                DebugHelper.WriteAlways("JSON -> SQLite Migration: No history items found");
            }

            if (shouldRenameFile)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var migratedPath = $"{SnapXL.HistoryFilePathOld}.{timestamp}.migrated";
                File.Move(SnapXL.HistoryFilePathOld, migratedPath);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.WriteAlways($"JSON -> SQLite Migration: Migration failed!!!");
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

        if (File.Exists(SnapXL.UploadersConfigPathOld)) UploadersConfig = MigrateJsonConfig<UploadersConfig>(SnapXL.UploadersConfigPathOld);
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
        if (File.Exists(SnapXL.HotkeyConfigPathOld)) HotkeysConfig = MigrateJsonConfig<HotkeysConfig>(SnapXL.HotkeyConfigPathOld);

    }

    public static void Dispose() => theLaw.Dispose();
    public static void CleanupHotkeysConfig()
    {
        foreach (var settings in HotkeysConfig.Hotkeys)
        {
            settings.TaskSettings?.Cleanup();
        }
    }

    public static void SaveAllSettings()
    {
        if (Settings != null)
        {
            Settings.Save(ApplicationConfigFilePath);
        }

        if (UploadersConfig != null)
        {
            UploadersConfig.Save(UploadersConfigFilePath);
        }

        if (HotkeysConfig != null)
        {
            CleanupHotkeysConfig();
            HotkeysConfig.Save(HotkeysConfigFilePath);
        }
    }

    public static void SaveApplicationConfigAsync()
    {
        if (Settings != null)
        {
            Settings.SaveAsync(ApplicationConfigFilePath);
        }
    }

    public static void SaveUploadersConfigAsync()
    {
        if (UploadersConfig != null)
        {
            UploadersConfig.SaveAsync(UploadersConfigFilePath);
        }
    }


    public static void SaveHotkeysConfigAsync()
    {
        CleanupHotkeysConfig();
        HotkeysConfig.SaveAsync(HotkeysConfigFilePath);
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

    public static bool Export(string? archivePath, bool settings, bool history)
    {
        MemoryStream msApplicationConfig = null, msUploadersConfig = null, msHotkeysConfig = null, msSqlite = null;

        try
        {
            var entries = new List<ZipEntryInfo>();

            if (settings)
            {
                msApplicationConfig = Settings.SaveToMemoryStream(false);
                entries.Add(new ZipEntryInfo(msApplicationConfig, ApplicationConfigFileName));
                msUploadersConfig = UploadersConfig.SaveToMemoryStream(false);
                entries.Add(new ZipEntryInfo(msUploadersConfig, UploadersConfigFileName));
                msHotkeysConfig = HotkeysConfig.SaveToMemoryStream(false);
                entries.Add(new ZipEntryInfo(msHotkeysConfig, HotkeysConfigFileName));
            }

            if (history)
            {
                string tempDb = Path.Combine(Path.GetTempPath(), $"{SnapXL.AppName}_DB_Backup_{Guid.NewGuid()}.db");

                using (var cmd = SnapXL.DbConnection.CreateCommand())
                {
                    cmd.CommandText = $"VACUUM INTO '{tempDb}'";
                    cmd.ExecuteNonQuery();
                }

                var dbBytes = File.ReadAllBytes(tempDb);
                msSqlite = new MemoryStream(dbBytes);

                File.Delete(tempDb);

                entries.Add(new ZipEntryInfo(msSqlite, Path.GetFileName(SnapXL.DBPath)));
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
            msSqlite?.Dispose();
        }

        return false;
    }

    public static bool Import(string archivePath)
    {
        try
        {
            ZipManager.Extract(
                archivePath,
                SnapXL.PersonalFolder,
                true,
                entry =>
                {
                    return FileHelpers.CheckExtension(entry.Name, new[] { "db" });
                },
                1_000_000_000
            );
            ZipManager.Extract(archivePath, SnapXL.ConfigFolder, true, entry =>
            {
                return FileHelpers.CheckExtension(entry.Name, new[] { "json", "xml" });
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

