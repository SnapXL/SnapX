
// SPDX-License-Identifier: GPL-3.0-or-later


using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapX.Core.CLI;
using SnapX.Core.Upload.Custom;
using SnapX.Core.Utils.Miscellaneous;

namespace SnapX.Core.Utils;

[JsonSerializable(typeof(NativeMessagingInput))]
[JsonSerializable(typeof(CustomUploaderItem))]

internal partial class JsonHelpersContext : JsonSerializerContext;

public static class JsonHelpers
{
    public static readonly JsonSerializerOptions defaultOptions = new()
    {
        TypeInfoResolver = JsonHelpersContext.Default,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter(), new HttpMethodConverter() },
        WriteIndented = true
    };

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public static void Serialize<T>(T obj, TextWriter textWriter, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;
        if (textWriter == null) return;
        using var memoryStream = new MemoryStream();
        JsonSerializer.Serialize(memoryStream, obj, options);
        // Convert to string and write to TextWriter
        textWriter.Write(Encoding.UTF8.GetString(memoryStream.ToArray()));
    }

    [RequiresDynamicCode("Uploader")]
    [RequiresUnreferencedCode("Uploader")]
    public static string SerializeToString<T>(T obj, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;

        return JsonSerializer.Serialize(obj, options);
    }

    [RequiresUnreferencedCode("Uploader")]
    public static void SerializeToStream<T>(T obj, Stream stream, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;
        if (stream == null) return;

        JsonSerializer.Serialize(stream, obj, options);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static MemoryStream SerializeToMemoryStream<T>(T obj, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;
        var memoryStream = new MemoryStream();
        SerializeToStream(obj, memoryStream, options);
        return memoryStream;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static void SerializeToFile<T>(T obj, string filePath, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;
        if (string.IsNullOrEmpty(filePath)) return;
        var directory = Path.GetDirectoryName(filePath);
        if (directory == null) return;
        Directory.CreateDirectory(directory);

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        SerializeToStream(obj, fileStream, options);
    }

    [RequiresUnreferencedCode("Uploader")]
    public static T Deserialize<T>(TextReader textReader, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;
        if (textReader == null) return default;

        var json = textReader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json, options);
    }

    [RequiresUnreferencedCode("Uploader")]
    public static T DeserializeFromString<T>(string json, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;
        return !string.IsNullOrEmpty(json) ? JsonSerializer.Deserialize<T>(json, options) : default;
    }
    [RequiresUnreferencedCode("Uploader")]
    public static T DeserializeFromStream<T>(Stream stream, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;
        if (stream == null) return default;

        return JsonSerializer.Deserialize<T>(stream, options);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static T DeserializeFromFile<T>(string filePath, JsonSerializerOptions? options = null)
    {
        options ??= defaultOptions;
        if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return default;

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return DeserializeFromStream<T>(fileStream, options);
    }

    public static bool QuickVerifyJsonFile(string filePath)
    {
        try
        {
            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fileStream.Length > 1 && fileStream.ReadByte() == (byte)'{')
                {
                    fileStream.Seek(-1, SeekOrigin.End);
                    return fileStream.ReadByte() == (byte)'}';
                }
            }
        }
        catch
        {
            // I acknowledge I am swallowing the error....
        }

        return false;
    }
}

