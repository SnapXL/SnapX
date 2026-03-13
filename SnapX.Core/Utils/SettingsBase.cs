using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Utils.Converters;
using SnapX.Core.Utils.Extensions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

namespace SnapX.Core.Utils;

public abstract partial class SettingsBase<T>
    where T : SettingsBase<T>, new()
{
    public delegate void SettingsSavedEventHandler(T settings, string filePath, bool result);
    public event SettingsSavedEventHandler SettingsSaved;

    public delegate void SettingsSaveFailedEventHandler(Exception e);
    public event SettingsSaveFailedEventHandler SettingsSaveFailed;
    [Browsable(false), JsonIgnore, YamlIgnore]
    private static readonly Lazy<SettingsYAMLContext> _aotContext = new(() => new());
    [Browsable(false), JsonIgnore, YamlIgnore]
    private static SettingsYAMLContext settingsYamlContext => _aotContext.Value;

    [Browsable(false), JsonIgnore, YamlIgnore]
    public string FilePath { get; private set; }

    [Browsable(false)]
    public string ApplicationVersion { get; set; }

    [Browsable(false), JsonIgnore, YamlIgnore]
    public bool IsFirstTimeRun { get; private set; }

    [Browsable(false), JsonIgnore, YamlIgnore]
    public bool IsUpgrade { get; private set; }

    [Browsable(false), JsonIgnore, YamlIgnore]
    public string BackupFolder { get; set; }

    [Browsable(false), JsonIgnore, YamlIgnore]
    public bool CreateBackup { get; set; }

    [Browsable(false), JsonIgnore, YamlIgnore]
    public bool CreateWeeklyBackup { get; set; }

    [Browsable(false), JsonIgnore, YamlIgnore]
    public bool UseEncryption { get; set; } = !OperatingSystem.IsFreeBSD();

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

    public MemoryStream SaveToMemoryStream(bool useEncryption = true)
    {
        ApplicationVersion = Helpers.GetApplicationVersion();

        MemoryStream ms = new MemoryStream();
        SaveToStream(ms, useEncryption, true);
        return ms;
    }

    private bool SaveInternal(string filePath)
    {
        var typeName = GetType().Name;
        DebugHelper.WriteLine($"{typeName} save started");

        var isSuccess = false;

        try
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            lock (this)
            {
                EnsureDirectoryExists(filePath);

                var tempFilePath = WriteTempFile(filePath);

                ReplaceFileWithBackup(filePath, tempFilePath);

                if (CreateWeeklyBackup && !string.IsNullOrEmpty(BackupFolder))
                    FileHelpers.BackupFileWeekly(filePath, BackupFolder);

                isSuccess = true;
            }
        }
        catch (Exception e)
        {
            e.ShowError(true, $"Error saving {typeName}");
            OnSettingsSaveFailed(e);
        }
        finally
        {
            LogSaveResult(typeName, isSuccess);
        }

        return isSuccess;
    }

    private void EnsureDirectoryExists(string filePath)
    {
        FileHelpers.CreateDirectoryFromFilePath(filePath);
    }

    private string WriteTempFile(string filePath)
    {
        var tempFilePath = filePath + ".temp";

        using var fileStream = new FileStream(
            tempFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            4096,
            FileOptions.WriteThrough
        );

        SaveToStream(fileStream, UseEncryption);

        return tempFilePath;
    }

    private void ReplaceFileWithBackup(string filePath, string tempFilePath)
    {
        if (File.Exists(filePath))
        {
            string backupFilePath = null;

            if (CreateBackup && !string.IsNullOrEmpty(BackupFolder))
            {
                FileHelpers.CreateDirectory(BackupFolder);
                var fileName = Path.GetFileName(filePath);
                backupFilePath = Path.Combine(BackupFolder, fileName);
            }

            File.Replace(tempFilePath, filePath, backupFilePath, true);
        }
        else
        {
            File.Move(tempFilePath, filePath);
        }
    }

    private void LogSaveResult(string typeName, bool isSuccess)
    {
        if (isSuccess)
            DebugHelper.Logger?.Information("{typeName} save successful", typeName);
        else
            DebugHelper.Logger?.Error("{typeName} save failed", typeName);
    }


    private static ISerializer BuildYamlSerializer(SecurePropertyStore store, bool useEncryption = true)
    {
        return new StaticSerializerBuilder(settingsYamlContext)
            .WithTypeInspector(inner => new ReadableAndWritablePropertiesTypeInspector(inner), loc => loc.OnBottom())
            .WithTypeInspector(inner => new EncryptionTypeInspector(inner, store, useEncryption))
            .WithTypeInspector(inner => new PreferredIdentityTypeInspector(inner))
            .WithTypeConverter(new UIFontYamlTypeConverter())
            .WithTypeConverter(new ImageSharpYamlTypeConverter())
            .WithTypeConverter(new TimeZoneInfoYamlTypeConverter())
            .WithTypeConverter(new HeaderCollectionYamlConverter(store, () => CustomUploaderItem.SensitiveKeys, useEncryption))
            .WithTypeConverter(new HttpMethodYamlConverter())
            .WithEnumNamingConvention(NullNamingConvention.Instance)
            .WithIndentedSequences()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();
    }

    private string SerializeToYaml(T obj, bool useEncryption = true)
    {
        var serializer = BuildYamlSerializer(SecurePropertyStore.Instance, useEncryption);
        return serializer.Serialize(obj);
    }

    private static void WriteStringToStream(string content, Stream stream, bool leaveOpen = false)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(false, true), 4096, leaveOpen);
        writer.Write(content);
        writer.Flush();
    }

    private void SaveToStream(Stream stream, bool useEncryption = true, bool leaveOpen = false)
    {
        var yaml = SerializeToYaml((T)this, useEncryption);
        WriteStringToStream(yaml, stream, leaveOpen);
    }


    public static T Load(string filePath, string backupFolder = null, bool fallbackSupport = true)
    {
        var fallbackFilePaths = GetFallbackFilePaths(filePath, backupFolder, fallbackSupport);
        var setting = LoadInternal(filePath, fallbackFilePaths);

        if (setting == null)
            return setting;

        setting.FilePath = filePath;
        setting.BackupFolder = backupFolder;
        setting.IsFirstTimeRun = string.IsNullOrEmpty(setting.ApplicationVersion);
        setting.IsUpgrade = !setting.IsFirstTimeRun
                            && Helpers.CompareApplicationVersion(setting.ApplicationVersion) < 0;

        return setting;
    }

    private static List<string> GetFallbackFilePaths(string filePath, string backupFolder, bool fallbackSupport)
    {
        var fallbackFilePaths = new List<string>();

        if (!fallbackSupport || string.IsNullOrEmpty(filePath))
            return fallbackFilePaths;

        fallbackFilePaths.Add(filePath + ".temp");

        if (string.IsNullOrEmpty(backupFolder) || !Directory.Exists(backupFolder)) return fallbackFilePaths;
        var fileName = Path.GetFileName(filePath);
        fallbackFilePaths.Add(Path.Combine(backupFolder, fileName));

        var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
        var lastWeeklyBackup = Directory
            .GetFiles(backupFolder, fileNameNoExt + "-*")
            .OrderBy(x => x)
            .LastOrDefault();

        if (!string.IsNullOrEmpty(lastWeeklyBackup))
            fallbackFilePaths.Add(lastWeeklyBackup);

        return fallbackFilePaths;
    }

    private static IDeserializer BuildYamlDeserializer(SecurePropertyStore store)
    {
        return new StaticDeserializerBuilder(settingsYamlContext)
            .WithTypeConverter(new UIFontYamlTypeConverter())
            .WithTypeConverter(new ImageSharpYamlTypeConverter())
            .WithTypeConverter(new TimeZoneInfoYamlTypeConverter())
            .WithTypeConverter(new HeaderCollectionYamlConverter(store, () => CustomUploaderItem.SensitiveKeys))
            .WithTypeConverter(new HttpMethodYamlConverter())
            .WithTypeInspector(inner => new ReadableAndWritablePropertiesTypeInspector(inner), loc => loc.OnBottom())
            .WithTypeInspector(inner => new EncryptionTypeInspector(inner, store))
            .WithTypeInspector(inner => new PreferredIdentityTypeInspector(inner))
            .WithEnumNamingConvention(NullNamingConvention.Instance)
            // .WithAttemptingUnquotedStringTypeDeserialization()
            .WithCaseInsensitivePropertyMatching()
            .WithDuplicateKeyChecking()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();
    }

    private static T LoadInternal(string filePath, List<string> fallbackFilePaths = null)
    {
        var typeName = typeof(T).Name;

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            DebugHelper.WriteLine($"{typeName} load started");

            try
            {
                var settings = DeserializeFromFile(filePath);

                if (settings == null)
                    throw new Exception($"{typeName} object is null.");

                DebugHelper.WriteLine($"{typeName} load finished");
                return settings;
            }
            catch (Exception e)
            {
                e.ShowError(true, $"{typeName} load failed: {filePath}");
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

    private static T DeserializeFromFile(string filePath)
    {
        var rawYaml = ReadFile(filePath);
        return DeserializeYaml(rawYaml);
    }

    private static string ReadFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found.", filePath);

        return File.ReadAllText(filePath);
    }

    private static T DeserializeYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            throw new InvalidDataException("YAML content is empty.");
        // if (yaml.Contains("$DPAPIEncrypted")) throw new SecurityException($"SnapX detected a secret encrypted by ShareX within the configuration of {T}. It cannot decrypt strings encrypted by ShareX. Please use ShareX's export function to extract decrypted files.")

        var deserializer = BuildYamlDeserializer(SecurePropertyStore.Instance);
        var obj = deserializer.Deserialize<T>(yaml);

        if (obj == null)
            throw new InvalidDataException($"Failed to deserialize YAML into {typeof(T).Name}.");

        return obj;
    }



    [GeneratedRegex(@"\""\$type\""\s*:\s*\""(?:[\w\.]+\.)?(\w+)(?:,\s*[\w\.]+)?\""")]
    private static partial Regex AssemblyTypeRegex();
}
