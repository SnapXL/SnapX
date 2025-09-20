using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SnapX.Core.Utils.Converters;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SnapX.Core.Utils;



public abstract partial class SettingsBase<T> where T : SettingsBase<T>, new()
{
    public delegate void SettingsSavedEventHandler(T settings, string filePath, bool result);
    public event SettingsSavedEventHandler SettingsSaved;

    public delegate void SettingsSaveFailedEventHandler(Exception e);
    public event SettingsSaveFailedEventHandler SettingsSaveFailed;

    [Browsable(false), JsonIgnore]
    public string FilePath { get; private set; }

    [Browsable(false)]
    public string ApplicationVersion { get; set; }

    [Browsable(false), JsonIgnore]
    public bool IsFirstTimeRun { get; private set; }

    [Browsable(false), JsonIgnore]
    public bool IsUpgrade { get; private set; }

    [Browsable(false), JsonIgnore]
    public string BackupFolder { get; set; }

    [Browsable(false), JsonIgnore]
    public bool CreateBackup { get; set; }

    [Browsable(false), JsonIgnore]
    public bool CreateWeeklyBackup { get; set; }

    [Browsable(false), JsonIgnore]
    public bool SupportDPAPIEncryption { get; set; }

    public bool IsUpgradeFrom(string version)
    {
        return IsUpgrade && Helpers.CompareVersion(ApplicationVersion, version) <= 0;
    }

    protected virtual void OnSettingsSaved(string filePath, bool result)
    {
        SettingsSaved?.Invoke((T)this, filePath, result);
    }

    protected virtual void OnSettingsSaveFailed(Exception e)
    {
        SettingsSaveFailed?.Invoke(e);
    }

    public bool Save(string filePath)
    {
        FilePath = filePath;
        ApplicationVersion = Helpers.GetApplicationVersion();

        bool result = SaveInternal(FilePath);

        OnSettingsSaved(FilePath, result);

        return result;
    }

    public bool Save()
    {
        return Save(FilePath);
    }

    public void SaveAsync(string? filePath = null)
    {
        filePath ??= FilePath;
        Task.Run(() => Save(filePath));
    }

    public MemoryStream SaveToMemoryStream(bool supportDPAPIEncryption = false)
    {
        ApplicationVersion = Helpers.GetApplicationVersion();

        MemoryStream ms = new MemoryStream();
        SaveToStream(ms, supportDPAPIEncryption, true);
        return ms;
    }

    private bool SaveInternal(string filePath)
    {
        var typeName = GetType().Name;
        DebugHelper.WriteLine($"{typeName} save started: {filePath}");

        var isSuccess = false;

        try
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                lock (this)
                {
                    FileHelpers.CreateDirectoryFromFilePath(filePath);

                    var tempFilePath = filePath + ".temp";

                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
                    {
                        SaveToStream(fileStream, SupportDPAPIEncryption);
                    }

                    if (!JsonHelpers.QuickVerifyJsonFile(tempFilePath))
                    {
                        throw new Exception($"{typeName} file is corrupt: {tempFilePath}");
                    }

                    if (File.Exists(filePath))
                    {
                        string backupFilePath = null;

                        if (CreateBackup)
                        {
                            var fileName = Path.GetFileName(filePath);
                            backupFilePath = Path.Combine(BackupFolder, fileName);
                            FileHelpers.CreateDirectory(BackupFolder);
                        }

                        File.Replace(tempFilePath, filePath, backupFilePath, true);
                    }
                    else
                    {
                        File.Move(tempFilePath, filePath);
                    }

                    if (CreateWeeklyBackup && !string.IsNullOrEmpty(BackupFolder))
                    {
                        FileHelpers.BackupFileWeekly(filePath, BackupFolder);
                    }

                    isSuccess = true;
                }
            }
        }
        catch (Exception e)
        {
            DebugHelper.WriteException(e);

            OnSettingsSaveFailed(e);
        }
        finally
        {
            var status = isSuccess ? "successful" : "failed";
            DebugHelper.WriteLine($"{typeName} save {status}: {filePath}");
        }

        return isSuccess;
    }
    [RequiresDynamicCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(Object)")]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private void SaveToStream(Stream stream, bool supportDPAPIEncryption = false, bool leaveOpen = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            TypeInfoResolver = SettingsContext.Default,
            Converters = {
                new JsonRectangleConverter(),
                new JsonPointConverter(),
                new JsonSizeConverter(),
                new JsonPaddingConverter(),
                new UtcDateTimeConverter(),
                new JsonTimeZoneInfoConverter(),
                new JsonColorConverter(),
                new JsonFontConverter(),
                new SafeEnumConverterFactory()
            }
        };

        var json = JsonSerializer.Serialize((T)this, options);

        using var writer = new StreamWriter(stream, new UTF8Encoding(false, true), 1024, leaveOpen);
        writer.Write(json);
        writer.Flush();
    }

    private void LogSerializedString(string json)
    {
        DebugHelper.WriteLine("[Serialized JSON]");
        DebugHelper.WriteLine(json);
    }

    private void LogProperties(object obj)
    {
        DebugHelper.WriteLine("[Object Properties & Fields]");
        var type = obj.GetType();

        // Log public instance properties
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object? value;
            try
            {
                value = prop.GetValue(obj);
            }
            catch (Exception ex)
            {
                value = $"<Error: {ex.Message}>";
            }

            DebugHelper.WriteLine($"Property - {prop.Name}: {value}");
        }

        // Log public instance fields
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            object? value;
            try
            {
                value = field.GetValue(obj);
            }
            catch (Exception ex)
            {
                value = $"<Error: {ex.Message}>";
            }

            DebugHelper.WriteLine($"Field    - {field.Name}: {value}");
        }
    }


    public static T Load(string filePath, string backupFolder = null, bool fallbackSupport = true)
    {
        var fallbackFilePaths = new List<string>();

        if (fallbackSupport && !string.IsNullOrEmpty(filePath))
        {
            var tempFilePath = filePath + ".temp";
            fallbackFilePaths.Add(tempFilePath);

            if (!string.IsNullOrEmpty(backupFolder) && Directory.Exists(backupFolder))
            {
                var fileName = Path.GetFileName(filePath);
                var backupFilePath = Path.Combine(backupFolder, fileName);
                fallbackFilePaths.Add(backupFilePath);

                var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
                var lastWeeklyBackupFilePath = Directory.GetFiles(backupFolder, fileNameNoExt + "-*").OrderBy(x => x).LastOrDefault();
                if (!string.IsNullOrEmpty(lastWeeklyBackupFilePath))
                {
                    fallbackFilePaths.Add(lastWeeklyBackupFilePath);
                }
            }
        }

        var setting = LoadInternal(filePath, fallbackFilePaths);

        if (setting == null) return setting;
        setting.FilePath = filePath;
        setting.IsFirstTimeRun = string.IsNullOrEmpty(setting.ApplicationVersion);
        setting.IsUpgrade = !setting.IsFirstTimeRun && Helpers.CompareApplicationVersion(setting.ApplicationVersion) < 0;
        setting.BackupFolder = backupFolder;

        return setting;
    }

    private static T LoadInternal(string filePath, List<string> fallbackFilePaths = null)
    {
        var typeName = typeof(T).Name;

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            DebugHelper.WriteLine($"{typeName} load started: {filePath}");

            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var rawJson = File.ReadAllText(filePath);

                // $type: "Namespace.Type, Assembly" → $type: "Type"
                rawJson = AssemblyTypeRegex().Replace(rawJson, m => $"\"$type\": \"{m.Groups[1].Value}\"");

                var settings = JsonSerializer.Deserialize<T>(rawJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    TypeInfoResolver = SettingsContext.Default,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                    Converters = {
                        new JsonRectangleConverter(),
                        new JsonPointConverter(),
                        new JsonPaddingConverter(),
                        new JsonTimeZoneInfoConverter(),
                        new JsonSizeConverter(),
                        new UtcDateTimeConverter(),
                        new JsonColorConverter(),
                        new JsonFontConverter(),
                        new SafeEnumConverterFactory()
                    }
                });
                if (settings == null) { throw new Exception($"{typeName} object is null."); }
                DebugHelper.WriteLine($"{typeName} load finished: {filePath}");
                return settings;
            }
            catch (Exception e)
            {
                DebugHelper.WriteException(e, $"{typeName} load failed: {filePath}");
            }
        }
        else
        {
            DebugHelper.WriteLine($"{typeName} file does not exist: {filePath}");
        }

        if (fallbackFilePaths is { Count: > 0 })
        {
            filePath = fallbackFilePaths[0];
            fallbackFilePaths.RemoveAt(0);
            return LoadInternal(filePath, fallbackFilePaths);
        }

        DebugHelper.WriteLine($"Loading new {typeName} instance.");

        return new T();
    }

    private static void Serializer_Error(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
    {
        // Handle missing enum values
        if (e.ErrorContext.Error.Message.StartsWith("Error converting value"))
        {
            e.ErrorContext.Handled = true;
        }
    }

    [GeneratedRegex(@"\""\$type\""\s*:\s*\""(?:[\w\.]+\.)?(\w+)(?:,\s*[\w\.]+)?\""")]
    private static partial Regex AssemblyTypeRegex();
}
