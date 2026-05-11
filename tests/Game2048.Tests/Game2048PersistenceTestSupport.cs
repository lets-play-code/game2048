using System.Reflection;
using Game2048Class = Game2048.Game.Game2048;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Game2048.Tests;

internal sealed class Game2048PersistenceScope : IDisposable
{
    private readonly string rootDirectory;

    public Game2048PersistenceScope()
    {
        rootDirectory = Path.Combine(Path.GetTempPath(), "game2048-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootDirectory);
        DatabasePath = Path.Combine(rootDirectory, "game2048.db");
    }

    public string DatabasePath { get; }

    public void InitializeDatabase()
    {
        InvokeStaticGameMethod("ConfigurePersistence", DatabasePath);
        InvokeStaticGameMethod("EnsureDatabaseReady");
    }

    public Game2048WebApplicationFactory CreateFactory()
    {
        return new Game2048WebApplicationFactory(DatabasePath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
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
    private readonly string databasePath;

    public Game2048WebApplicationFactory(string databasePath)
    {
        this.databasePath = databasePath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Game2048:DatabasePath", databasePath);
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Game2048:DatabasePath"] = databasePath
            });
        });
    }
}
