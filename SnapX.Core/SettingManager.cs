
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SnapX.Core;

[JsonSerializable(typeof(RootConfiguration))]
[JsonSerializable(typeof(IConfiguration))]
[JsonSerializable(typeof(UploadersConfig))]
[JsonSerializable(typeof(HotkeysConfig))]

internal partial class SettingsContext : JsonSerializerContext;
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

            string? uploadersConfigFolder;

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

            string? hotkeysConfigFolder;

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

    public static string? SnapshotFolder => Path.Combine(SnapX.PersonalFolder, "Snapshots");

    private static RootConfiguration Settings { get => SnapX.Settings; set => SnapX.Settings = value; }
    private static TaskSettings DefaultTaskSettings { get => SnapX.DefaultTaskSettings; set => SnapX.DefaultTaskSettings = value; }
    private static UploadersConfig UploadersConfig { get => SnapX.UploadersConfig; set => SnapX.UploadersConfig = value; }
    private static HotkeysConfig HotkeysConfig { get => SnapX.HotkeysConfig; set => SnapX.HotkeysConfig = value; }
    private static VersionEnforcer theLaw;

    private static ManualResetEvent uploadersConfigResetEvent = new(false);
    private static ManualResetEvent hotkeysConfigResetEvent = new(false);

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static void LoadSettings()
    {
        theLaw = new VersionEnforcer(SnapX.LockDirectory);
        theLaw.Enforce();
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
        ApplicationConfigBackwardCompatibilityTasks();
        var configurationBuilder = new ConfigurationBuilder();
        if (!SnapX.Sandbox)
        {
            configurationBuilder.AddJsonFile(ApplicationConfigFilePath, optional: true, reloadOnChange: true);
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
            // DebugHelper.WriteLine($"{kv.Key} = {kv.Value}");
        }
        var settings = new RootConfiguration();
        SnapX.Configuration.Bind(settings);
        Settings = settings;
        if (string.IsNullOrWhiteSpace(Settings.SQLitePath))
            Settings.SQLitePath = Path.Combine(SnapX.DefaultPersonalFolder, "SnapX.db");
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
            configurationBuilder.AddJsonFile(UploadersConfigFilePath, optional: true, reloadOnChange: true);
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
            configurationBuilder.AddJsonFile(HotkeysConfigFilePath, optional: true, reloadOnChange: true);
        }
        var BuiltConfig = configurationBuilder.Build();
        HotkeysConfig = new HotkeysConfig();
        BuiltConfig.Bind(HotkeysConfig);
        HotkeysConfigBackwardCompatibilityTasks();
    }

    public static void SaveApplicationConfig()
    {
        if (SnapX.Sandbox)
            return;

        var configFilePath = ApplicationConfigFilePath;

        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = SettingsContext.Default,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            });
            File.WriteAllText(configFilePath, json);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Failed to save config file: {ex}");
        }
    }
    public static void SaveUploadConfig()
    {
        if (SnapX.Sandbox)
            return;

        var configFilePath = UploadersConfigFilePath;

        try
        {
            var json = JsonSerializer.Serialize(UploadersConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = SettingsContext.Default
            });
            File.WriteAllText(configFilePath, json);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Failed to save config file: {ex}");
        }
    }
    public static void SaveHotkeysConfig()
    {
        if (SnapX.Sandbox)
            return;

        var configFilePath = HotkeysConfigFilePath;

        try
        {
            var json = JsonSerializer.Serialize(HotkeysConfig, new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = SettingsContext.Default
            });
            File.WriteAllText(configFilePath, json);
        }
        catch (Exception ex)
        {
            DebugHelper.WriteLine($"Failed to save config file: {ex}");
        }
    }
    [DapperAot]
    private static void ApplicationConfigBackwardCompatibilityTasks()
    {
        // if (File.Exists(ApplicationConfigFilePath)) MigrateApplicationConfigJSONToSQLite();

        var assembly = Assembly.GetExecutingAssembly();

        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.Contains("Migrations") && name.EndsWith(".sql"))
            .OrderBy(name => name) // Ensure migrations are processed in numerical order
            .ToList();

        // Ensure MigrationLog table exists for the checks below
        // This is crucial if it's the very first time running migrations.
        SnapX.DbConnection.Execute(@"
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
        var currentMigrationsInDb = SnapX.DbConnection.QueryFirstOrDefault<string>(
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
                // For now, let's just log and proceed carefully, or throw a more specific error.
                DebugHelper.WriteLine($"Warning: Could not parse migration version from '{currentMigrationsInDb}'. Proceeding carefully.");
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
                var alreadyApplied = SnapX.DbConnection.ExecuteScalar<int>(
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

            using var tx = SnapX.DbConnection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                SnapX.DbConnection.Execute(sql, transaction: tx);
                SnapX.DbConnection.Execute("INSERT INTO MigrationLog (FileName) VALUES (@FileName)", new { FileName = fileName }, transaction: tx);
                tx.Commit();
                DebugHelper.WriteLine($"Applied SQL Migration: {fileName}");
            }
            catch (Exception ex)
            {
                tx.Rollback(); // Rollback on error
                DebugHelper.WriteException(ex);
                throw new InvalidOperationException($"Failed to apply migration '{fileName}'. Transaction rolled back.", ex);
            }
        }

        SnapX.DbConnection.Execute("PRAGMA optimize;");
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    private static async Task MigrateHistoryFile()
    {
        if (SnapX.Sandbox)
            return;
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
    }

    public static void Dispose() => theLaw.Dispose();
    public static void CleanupHotkeysConfig()
    {
        foreach (var taskSettings in HotkeysConfig.Hotkeys.Select(x => x.TaskSettings))
        {
            taskSettings.Cleanup();
        }
    }

    public static void SaveAllSettings()
    {
        SaveApplicationConfig();
        SaveUploadConfig();
        SaveHotkeysConfig();
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

    public static bool Export(string? archivePath, bool settings, bool history)
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
}

