using Microsoft.Data.Sqlite;

namespace SnapX.Core;

using Microsoft.Extensions.Configuration;

public class SqliteConfigurationSource : IConfigurationSource
{
    private readonly SqliteConnection _dbConnection;

    public SqliteConfigurationSource(SqliteConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SqliteConfigurationProvider(_dbConnection);
    }
}
