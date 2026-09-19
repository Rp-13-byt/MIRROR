using Microsoft.Data.Sqlite;

namespace Mirror.Persistence;

public interface ISqliteConnectionFactory
{
    string DatabasePath { get; }
    SqliteConnection CreateConnection();
}

public class SqliteConnectionFactory : ISqliteConnectionFactory
{
    public string DatabasePath { get; }

    public SqliteConnectionFactory(string? customDbPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customDbPath))
        {
            DatabasePath = customDbPath;
        }
        else
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string mirrorDir = Path.Combine(localAppData, "Mirror", "Data");
            Directory.CreateDirectory(mirrorDir);
            DatabasePath = Path.Combine(mirrorDir, "mirror.db");
        }
    }

    public SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;
                PRAGMA foreign_keys = ON;
                PRAGMA busy_timeout = 5000;
            ";
            cmd.ExecuteNonQuery();
        }

        return connection;
    }
}
