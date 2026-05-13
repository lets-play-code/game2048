using System.Reflection;
using Game2048Class = Game2048.Game.Game2048;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Game2048.Tests;

internal sealed class Game2048PersistenceScope : IDisposable
{
    private static readonly string defaultServerConnectionString = "Server=127.0.0.1;Port=53306;User ID=game2048;Password=game2048;SslMode=None;AllowPublicKeyRetrieval=True;";

    public Game2048PersistenceScope()
    {
        DatabaseName = "game2048_tests_" + Guid.NewGuid().ToString("N");
        ConnectionString = BuildDatabaseConnectionString(DatabaseName);
    }

    public string DatabaseName { get; }
    public string ConnectionString { get; }

    public void InitializeDatabase()
    {
        EnsureFreshDatabase();
        InvokeStaticGameMethod("ConfigurePersistence", ConnectionString);
        InvokeStaticGameMethod("EnsureDatabaseReady");
    }

    public Game2048WebApplicationFactory CreateFactory()
    {
        return new Game2048WebApplicationFactory(ConnectionString);
    }

    public MySqlConnection CreateConnection()
    {
        return new MySqlConnection(ConnectionString);
    }

    public bool DatabaseExists()
    {
        using MySqlConnection connection = CreateServerConnection();
        connection.Open();
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM information_schema.schemata WHERE schema_name = @databaseName LIMIT 1;";
        command.Parameters.AddWithValue("@databaseName", DatabaseName);
        return command.ExecuteScalar() != null;
    }

    public async Task<List<string>> GetExistingTableNamesAsync(params string[] tableNames)
    {
        await using MySqlConnection connection = CreateConnection();
        await connection.OpenAsync();
        await using MySqlCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT TABLE_NAME
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND TABLE_NAME IN ('LeaderboardEntries', 'SavedGames', '__EFMigrationsHistory')
            ORDER BY TABLE_NAME;";

        List<string> existingTableNames = new List<string>();
        await using MySqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            existingTableNames.Add(reader.GetString(0));
        }

        return existingTableNames;
    }

    public void Dispose()
    {
        try
        {
            try
            {
                InvokeStaticGameMethod("ConfigureGeneratedTileValue", new object[] { null });
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentException)
            {
            }

            DropDatabaseIfExists();
        }
        catch (MySqlException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string BuildDatabaseConnectionString(string databaseName)
    {
        MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(GetServerConnectionString())
        {
            Database = databaseName
        };
        return builder.ConnectionString;
    }

    private static string GetServerConnectionString()
    {
        string configured = Environment.GetEnvironmentVariable("GAME2048_TEST_MYSQL_SERVER_CONNECTION_STRING");
        string connectionString = string.IsNullOrWhiteSpace(configured) ? defaultServerConnectionString : configured;
        MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder(connectionString)
        {
            Database = string.Empty
        };
        return builder.ConnectionString;
    }

    private MySqlConnection CreateServerConnection()
    {
        return new MySqlConnection(GetServerConnectionString());
    }

    private void EnsureFreshDatabase()
    {
        DropDatabaseIfExists();

        using MySqlConnection connection = CreateServerConnection();
        connection.Open();
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;";
        command.ExecuteNonQuery();
    }

    private void DropDatabaseIfExists()
    {
        using MySqlConnection connection = CreateServerConnection();
        connection.Open();
        using MySqlCommand command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS `{DatabaseName}`;";
        command.ExecuteNonQuery();
    }

    private static void InvokeStaticGameMethod(string methodName, params object[] args)
    {
        MethodInfo method = typeof(Game2048Class).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        try
        {
            method.Invoke(null, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}

internal sealed class Game2048WebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString;

    public Game2048WebApplicationFactory(string connectionString)
    {
        this.connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Game2048:ConnectionString", connectionString);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Game2048:ConnectionString"] = connectionString,
                ["Game2048:EnableTestApi"] = "true"
            });
        });
    }
}
