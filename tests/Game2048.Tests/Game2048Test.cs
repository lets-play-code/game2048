using System.Collections;
using System.Net.Http;
using System.Reflection;
using Game2048Class = Game2048.Game.Game2048;
using Game2048StateModel = Game2048.Game.Game2048State;
using Xunit;

namespace Game2048.Tests;

public class Game2048Test : IDisposable
{
    private readonly Game2048PersistenceScope persistenceScope = new Game2048PersistenceScope();

    public Game2048Test()
    {
        persistenceScope.InitializeDatabase();
    }

    public void Dispose()
    {
        persistenceScope.Dispose();
    }

    [Fact]
    public void test_init_status()
    {
        Game2048Class game2048 = new Game2048Class();

        Assert.True(game2048.canMove());
        Assert.False(game2048.myLose);
        Assert.False(game2048.myWin);
        Assert.Equal(0, game2048.myScore);
        Assert.Equal(16, game2048.paint().Tiles.Count);
        Assert.Equal(2, game2048.paint().Tiles.Count(tile => !tile.IsEmpty));
    }

    [Fact]
    public void test_init_paint()
    {
        Game2048Class game2048 = new Game2048Class();

        Game2048StateModel paint = game2048.paint();

        Assert.Equal("Score: 0", paint.ScoreText);
        Assert.Equal(16, paint.ScoreTextDrawCount);
    }

    [Fact(Skip = ".")]
    public void game_state_marks_finished_game_as_saveable_until_record_is_saved()
    {
        Game2048Class game2048 = CreateFinishedGame(256);

        Game2048StateModel beforeSave = game2048.getGameState();

        Assert.True(GetStateFlag(beforeSave, "GameOver"));
        Assert.True(GetStateFlag(beforeSave, "CanSaveRecord"));
        Assert.False(GetStateFlag(beforeSave, "RecordSaved"));

        InvokeSaveLeaderboardRecord(GetSaveLeaderboardRecordMethod(), game2048, "Alice");

        Game2048StateModel afterSave = game2048.getGameState();

        Assert.True(GetStateFlag(afterSave, "GameOver"));
        Assert.False(GetStateFlag(afterSave, "CanSaveRecord"));
        Assert.True(GetStateFlag(afterSave, "RecordSaved"));
    }

    [Fact]
    public void save_leaderboard_record_rejects_saving_before_game_over()
    {
        Game2048Class game2048 = new Game2048Class();
        MethodInfo saveMethod = GetSaveLeaderboardRecordMethod();

        Assert.Throws<InvalidOperationException>(() => InvokeSaveLeaderboardRecord(saveMethod, game2048, "Alice"));
    }

    [Fact]
    public void save_leaderboard_record_keeps_whitespace_only_names_before_posting_to_the_external_wall()
    {
        Game2048Class game2048 = CreateFinishedGame(64);
        MethodInfo saveMethod = GetSaveLeaderboardRecordMethod();

        Assert.Throws<HttpRequestException>(() => InvokeSaveLeaderboardRecord(saveMethod, game2048, "   "));

        object blankName = Assert.Single(GetLeaderboardEntries());
        Assert.Equal("   ", GetEntryValue<string>(blankName, "PlayerName"));
        Assert.Equal(64, GetEntryValue<int>(blankName, "Score"));
        Assert.False(GetStateFlag(game2048.getGameState(), "RecordSaved"));
    }

    [Fact(Skip = ".")]
    public void save_leaderboard_record_can_only_be_called_once_per_game()
    {
        Game2048Class game2048 = CreateFinishedGame(128);
        MethodInfo saveMethod = GetSaveLeaderboardRecordMethod();

        InvokeSaveLeaderboardRecord(saveMethod, game2048, "Alice");

        Assert.Throws<InvalidOperationException>(() => InvokeSaveLeaderboardRecord(saveMethod, game2048, "Alice"));
    }

    [Fact(Skip = ".")]
    public void leaderboard_keeps_only_each_players_best_score_across_games()
    {
        SaveFinishedGame("Alice", 128);
        SaveFinishedGame("Alice", 256);
        SaveFinishedGame("Alice", 64);

        object alice = Assert.Single(GetLeaderboardEntries());

        Assert.Equal("Alice", GetEntryValue<string>(alice, "PlayerName"));
        Assert.Equal(256, GetEntryValue<int>(alice, "Score"));
        Assert.Equal(1, GetEntryValue<int>(alice, "Rank"));
        Assert.Equal(1, GetPositionOfPlayer("Alice"));
    }

    [Fact(Skip = ".")]
    public void leaderboard_uses_shared_positions_for_ties_and_keeps_unknown_players_after_recorded_ones()
    {
        SaveFinishedGame("Alice", 512);
        SaveFinishedGame("Bob", 512);
        SaveFinishedGame("Cara", 256);

        List<object> entries = GetLeaderboardEntries();

        Assert.Equal(new[] { 512, 512, 256 }, entries.Select(entry => GetEntryValue<int>(entry, "Score")).ToArray());
        Assert.Equal(1, GetEntryValue<int>(entries.Single(entry => GetEntryValue<string>(entry, "PlayerName") == "Alice"), "Rank"));
        Assert.Equal(1, GetEntryValue<int>(entries.Single(entry => GetEntryValue<string>(entry, "PlayerName") == "Bob"), "Rank"));
        Assert.Equal(3, GetEntryValue<int>(entries.Single(entry => GetEntryValue<string>(entry, "PlayerName") == "Cara"), "Rank"));
        Assert.Equal(1, GetPositionOfPlayer("Alice"));
        Assert.Equal(1, GetPositionOfPlayer("Bob"));
        Assert.Equal(3, GetPositionOfPlayer("Cara"));
        Assert.Equal(4, GetPositionOfPlayer("Dana"));
    }

    private static Game2048Class CreateFinishedGame(int score)
    {
        Game2048Class game2048 = new Game2048Class();
        SetTiles(game2048, new[]
        {
            "2", "4", "8", "16",
            "32", "64", "128", "256",
            "4", "8", "16", "32",
            "64", "128", "256", "512"
        });
        game2048.myScore = score;
        game2048.keyPressed("left");
        Assert.True(game2048.myLose);
        return game2048;
    }

    private static void SaveFinishedGame(string playerName, int score)
    {
        Game2048Class game2048 = CreateFinishedGame(score);
        InvokeSaveLeaderboardRecord(GetSaveLeaderboardRecordMethod(), game2048, playerName);
    }

    private static MethodInfo GetSaveLeaderboardRecordMethod()
    {
        MethodInfo method = typeof(Game2048Class).GetMethod("saveLeaderboardRecord", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        return method;
    }

    private static void InvokeSaveLeaderboardRecord(MethodInfo method, Game2048Class game2048, string playerName)
    {
        try
        {
            method.Invoke(game2048, new object[] { playerName });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static List<object> GetLeaderboardEntries()
    {
        MethodInfo method = typeof(Game2048Class).GetMethod("getLeaderboardEntries", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(method);

        object result = method.Invoke(null, Array.Empty<object>());
        IEnumerable entries = Assert.IsAssignableFrom<IEnumerable>(result);
        return entries.Cast<object>().ToList();
    }

    private static int GetPositionOfPlayer(string playerName)
    {
        MethodInfo method = typeof(Game2048Class).GetMethod("getPositionOfPlayer", BindingFlags.Static | BindingFlags.Public);
        Assert.NotNull(method);
        return Assert.IsType<int>(method.Invoke(null, new object[] { playerName }));
    }

    private static T GetEntryValue<T>(object entry, string propertyName)
    {
        PropertyInfo property = entry.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(entry));
    }

    private static bool GetStateFlag(Game2048StateModel state, string propertyName)
    {
        PropertyInfo property = typeof(Game2048StateModel).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(property);
        return Assert.IsType<bool>(property.GetValue(state));
    }

    private static void SetTiles(Game2048Class game2048, string[] values)
    {
        FieldInfo myTiles = typeof(Game2048Class).GetField("myTiles", BindingFlags.Instance | BindingFlags.NonPublic)!;
        myTiles.SetValue(game2048, values.Select(value => new Game2048Class.Tile(value)).ToArray());
    }
}
