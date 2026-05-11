namespace Game2048.Game;

public class LeaderboardEntryEntity
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int BestScore { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class SavedGameEntity
{
    public int Id { get; set; }
    public string SlotKey { get; set; } = string.Empty;
    public string BoardJson { get; set; } = "[]";
    public int Score { get; set; }
    public bool Win { get; set; }
    public bool Lose { get; set; }
    public bool ScoreRecorded { get; set; }
    public bool LeakedShouldAddTile { get; set; }
    public DateTime SavedAtUtc { get; set; }
}
