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
            optionsBuilder.UseMySql(
                Game2048.GetConfiguredConnectionString(),
                new MySqlServerVersion(new Version(8, 0, 0)));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaderboardEntryEntity>(entity =>
        {
            entity.HasIndex(item => item.PlayerName).IsUnique();
            entity.Property(item => item.PlayerName).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<SavedGameEntity>(entity =>
        {
            entity.HasIndex(item => item.SlotKey).IsUnique();
            entity.Property(item => item.SlotKey).IsRequired().HasMaxLength(32);
            entity.Property(item => item.BoardJson).IsRequired();
        });
    }
}
