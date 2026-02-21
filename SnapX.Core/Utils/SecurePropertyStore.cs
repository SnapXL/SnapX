using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using SimpleBase;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace SnapX.Core.Utils;

[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonEncryptAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public sealed class YamlEncryptAttribute : Attribute { }
public static class JsonEncryptionResolver
{
    public static void CreateModifier(JsonTypeInfo typeInfo, SecurePropertyStore store)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        foreach (var property in typeInfo.Properties)
        {
            var hasAttr = property.AttributeProvider?.IsDefined(typeof(JsonEncryptAttribute), true) ?? false;
            if (!hasAttr) continue;

            if (property.PropertyType == typeof(string))
            {
                var originalGetter = property.Get;
                if (originalGetter is not null)
                {
                    property.Get = obj =>
                    {
                        var value = (string?)originalGetter(obj);
                        return string.IsNullOrWhiteSpace(value) ? value : store.Protect(value);
                    };
                }

                var originalSetter = property.Set;
                if (originalSetter is not null)
                {
                    property.Set = (obj, value) =>
                    {
                        var text = (string?)value;
                        var decrypted = (text?.StartsWith(SecurePropertyStore.Header) ?? false)
                            ? store.Unprotect(text)
                            : text;
                        originalSetter(obj, decrypted);
                    };
                }
            }
            else
            {
                ConfigureComplexObjectEncryption(property, store);
            }
        }
    }

    private static void ConfigureComplexObjectEncryption(JsonPropertyInfo property, SecurePropertyStore store)
    {
        var originalGetter = property.Get;
        if (originalGetter is not null)
        {
            property.Get = obj =>
            {
                var val = originalGetter(obj);
                if (val is null) return null;

                var json = JsonSerializer.Serialize(val, SettingsContext.Default.Options);
                return store.Protect(json);
            };
        }

        var originalSetter = property.Set;
        if (originalSetter is not null)
        {
            property.Set = (obj, value) =>
            {
                if (value is not string securedText || !securedText.StartsWith(SecurePropertyStore.Header))
                {
                    originalSetter(obj, value);
                    return;
                }

                var decryptedJson = store.Unprotect(securedText);
                var deserialized = JsonSerializer.Deserialize(decryptedJson, property.PropertyType, SettingsContext.Default.Options);
                originalSetter(obj, deserialized);
            };
        }
    }
}
public sealed class EncryptionTypeInspector(ITypeInspector innerInspector, SecurePropertyStore store, bool UseEncryption = true)
    : TypeInspectorSkeleton
{
    public override string GetEnumName(Type enumType, string name) => innerInspector.GetEnumName(enumType, name);
    public override string GetEnumValue(object enumValue) => innerInspector.GetEnumValue(enumValue);

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        return innerInspector.GetProperties(type, container)
            .Select(p => WrapProperty(p, type));
    }

    private IPropertyDescriptor WrapProperty(IPropertyDescriptor property, Type containerType)
    {
        if (property is null) return null!;

        var attr = property.GetCustomAttribute<YamlEncryptAttribute>() ??
                   containerType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                               ?.GetCustomAttribute<YamlEncryptAttribute>();

        return attr is not null ? new EncryptedPropertyDescriptor(property, store, UseEncryption) : property;
    }

    private sealed class EncryptedPropertyDescriptor(IPropertyDescriptor inner, SecurePropertyStore store, bool UseEncryption = true)
        : IPropertyDescriptor
    {
        public string Name => inner.Name;
        public bool CanWrite => inner.CanWrite;
        public Type Type => inner.Type;
        public Type? TypeOverride { get => inner.TypeOverride; set => inner.TypeOverride = value; }
        public int Order { get => inner.Order; set => inner.Order = value; }
        public ScalarStyle ScalarStyle { get => inner.ScalarStyle; set => inner.ScalarStyle = value; }
        public bool AllowNulls => inner.AllowNulls;
        public Type? ConverterType => inner.ConverterType;
        public bool Required => inner.Required;

        public T? GetCustomAttribute<T>() where T : Attribute => inner.GetCustomAttribute<T>();

        public IObjectDescriptor Read(object target)
        {
            var value = inner.Read(target).Value as string;
            return new ObjectDescriptor(UseEncryption ? store.Protect(value ?? string.Empty) : value ?? string.Empty, typeof(string), typeof(string));
        }

        public void Write(object target, object? value)
        {
            var stringValue = value as string;
            if (UseEncryption && stringValue?.StartsWith(SecurePropertyStore.Header) == true)
            {
                inner.Write(target, store.Unprotect(stringValue));
            }
            else
            {
                inner.Write(target, stringValue);
            }
        }
    }
}

public sealed class SecurePropertyStore(byte[] masterKey)
{
    public const string Header = "v1.z85:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    [MinLength(32)]
    [MaxLength(32)]
    private readonly byte[] _key = masterKey.Length == 32
        ? masterKey
        : throw new ArgumentException("Key must be 256-bit (32 bytes).");


    public string Protect(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;

        var input = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);

        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(new KeyParameter(_key), TagSize * 8, nonce, Encoding.UTF8.GetBytes(Header));
        cipher.Init(true, parameters);

        var outSize = cipher.GetOutputSize(input.Length);
        var result = new byte[NonceSize + outSize];

        nonce.CopyTo(result.AsSpan(0, NonceSize));

        var len = cipher.ProcessBytes(input, 0, input.Length, result, NonceSize);
        cipher.DoFinal(result, NonceSize + len);

        var padded = AddPaddingWithLength(result);
        var encryptedString = $"{Header}{Base85.Z85.Encode(padded)}";
        return encryptedString;
    }

    public string Unprotect(string securedText)
    {
        if (string.IsNullOrWhiteSpace(securedText)) return string.Empty;
        if (!securedText.StartsWith(Header)) return securedText;

        var z85Part = securedText[Header.Length..];

        byte[] paddedPayload;
        try
        {
            paddedPayload = Base85.Z85.Decode(z85Part);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Z85 decoding failed.", ex);
        }

        var payload = RemovePaddingWithLength(paddedPayload);

        if (payload.Length < NonceSize + TagSize)
            throw new CryptographicException("Payload too short.");

        var nonce = payload[..NonceSize];

        var ciphertextWithTag = payload[NonceSize..];

        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(_key),
            TagSize * 8,
            nonce,
            Encoding.UTF8.GetBytes(Header)
        );
        cipher.Init(false, parameters);

        var plainBytes = new byte[cipher.GetOutputSize(ciphertextWithTag.Length)];
        var len = cipher.ProcessBytes(ciphertextWithTag, 0, ciphertextWithTag.Length, plainBytes, 0);

        len += cipher.DoFinal(plainBytes, len);

        return Encoding.UTF8.GetString(plainBytes, 0, len);
    }

    private static byte[] AddPaddingWithLength(byte[] data)
    {
        // Esure length is multiple of 4 for Z85
        var lengthPrefixSize = 4;
        var totalDataSize = lengthPrefixSize + data.Length;
        var remainder = totalDataSize % 4;
        var paddingNeeded = remainder == 0 ? 0 : 4 - remainder;

        var result = new byte[totalDataSize + paddingNeeded];

        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(0, lengthPrefixSize), data.Length);

        data.CopyTo(result.AsSpan(lengthPrefixSize));

        return result;
    }

    private static byte[] RemovePaddingWithLength(byte[] paddedData)
    {
        var originalLength = BinaryPrimitives.ReadInt32LittleEndian(paddedData.AsSpan(0, 4));

        return paddedData.AsSpan(4, originalLength).ToArray();
    }
    private static Lazy<SecurePropertyStore>? _instance = new Lazy<SecurePropertyStore>(() => new SecurePropertyStore(MasterKeyManager.GetOrGenerateKey()));

    public static SecurePropertyStore Instance => _instance?.Value
                                                  ?? throw new InvalidOperationException("SecurePropertyStore must be initialized with a key before use.");
}
