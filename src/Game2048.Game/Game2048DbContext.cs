using Microsoft.EntityFrameworkCore;

namespace Game2048.Game;

public class Game2048DbContext : DbContext
{
    public DbSet<LeaderboardEntryEntity> LeaderboardEntries => Set<LeaderboardEntryEntity>();
    public DbSet<SavedGameEntity> SavedGames => Set<SavedGameEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={Game2048.GetConfiguredDatabasePath()}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaderboardEntryEntity>(entity =>
        {
            entity.HasIndex(item => item.PlayerName).IsUnique();
            entity.Property(item => item.PlayerName).IsRequired();
        });

        modelBuilder.Entity<SavedGameEntity>(entity =>
        {
            entity.HasIndex(item => item.SlotKey).IsUnique();
            entity.Property(item => item.SlotKey).IsRequired();
            entity.Property(item => item.BoardJson).IsRequired();
        });
    }
}
