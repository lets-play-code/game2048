using Microsoft.EntityFrameworkCore.Design;

namespace Game2048.Game;

public class Game2048DesignTimeDbContextFactory : IDesignTimeDbContextFactory<Game2048DbContext>
{
    public Game2048DbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("Game2048__ConnectionString");
        Game2048.ConfigurePersistence(string.IsNullOrWhiteSpace(connectionString)
            ? Game2048.GetDefaultConnectionString()
            : connectionString);
        return new Game2048DbContext();
    }
}
