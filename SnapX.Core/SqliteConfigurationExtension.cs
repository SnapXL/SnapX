using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace SnapX.Core;

using Microsoft.Extensions.Configuration;

public static class SqliteConfigurationExtensions
{
    public static IConfigurationBuilder AddSqliteSettings(this IConfigurationBuilder builder, SqliteConnection connection)
    {
        return builder.Add(new SqliteConfigurationSource(connection));
    }
}

