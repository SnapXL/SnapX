
using Dapper;
using Microsoft.Data.Sqlite;
using SnapX.Core.Models;

namespace SnapX.Core;

using Microsoft.Extensions.Configuration;

public class SqliteConfigurationProvider : ConfigurationProvider
{
    private readonly SqliteConnection _dbConnection;

    public SqliteConfigurationProvider(SqliteConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }
    [DapperAot]
    public override void Load()
    {
        var SavedConfigurationSql =
            @"SELECT ConfigSection, SettingKey, SettingValue
      FROM ApplicationConfig";
        var settings = _dbConnection.Query<SavedConfiguration>(
            SavedConfigurationSql
        ); // :contentReference[oaicite:0]{index=0}
        foreach (var setting in settings)
        {
            var key = setting.ConfigSection != null
                ? $"{setting.ConfigSection}:{setting.SettingKey}"
                : setting.SettingKey;

            Data[key] = setting.SettingValue;
        }
    }
}
