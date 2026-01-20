namespace SnapX.Avalonia.Converters;

using System.Globalization;
using global::Avalonia.Data.Converters;

public class HeaderSecurityBlurConverter : IValueConverter
{
    private static readonly string[] SensitiveKeys =
    {
        "password",
        "upload_password",
        "upload-password",
        "passwd",
        "pass",
        "pwd",
        "api",
        "apikey",
        "api-key",
        "api_key",
        "x-api-key",
        "api key",
        "key",
        "keyid",
        "key-id",
        "email",
        "user",
        "k", // puush.me uses 'k' as their API key header
        "p", // short for password
        "username",
        "user-name",
        "user name",
        "credential",
        "creds",
        "cred",
        "token",
        "secret",
        "auth",
        "authorization",
        "x-authorization",
        "x-auth-token",
        "access-token",
        "access token",
        "bearer",
        "session",
        "jwt",
        "cookie",
        "priv",
        "sid",
        "uuid",
        "guid",
        "salt",
        "nonce",
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? key = value as string;
        if (string.IsNullOrWhiteSpace(key))
            return 0.0;

        bool isSensitive = SensitiveKeys.Any(s =>
            key.Equals(s, StringComparison.OrdinalIgnoreCase)
        );

        return isSensitive ? 20.0 : 0.0;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotImplementedException();
}
