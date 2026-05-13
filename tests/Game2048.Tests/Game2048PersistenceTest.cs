using System.Collections;
using System.Reflection;
using Game2048Class = Game2048.Game.Game2048;
using Game2048StateModel = Game2048.Game.Game2048State;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Game2048.Tests;

public class Game2048PersistenceTest : IDisposable
{
    private readonly Game2048PersistenceScope persistenceScope = new Game2048PersistenceScope();

    public Game2048PersistenceTest()
    {
        persistenceScope.InitializeDatabase();
    }

    public void Dispose()
    {
        persistenceScope.Dispose();
    }

    [Fact]
    public void save_leaderboard_record_persists_best_score_before_the_external_wall_fails()
    {
        AssertExternalWallFailure(() => InvokeSaveLeaderboardRecord(CreateFinishedGame(128), "Alice"));
        AssertExternalWallFailure(() => InvokeSaveLeaderboardRecord(CreateFinishedGame(64), "Alice"));

        AssertLeaderboardRows(("Alice", 128));

        object alice = Assert.Single(GetLeaderboardEntries());
        Assert.Equal("Alice", GetEntryValue<string>(alice, "PlayerName"));
        Assert.Equal(128, GetEntryValue<int>(alice, "Score"));
        Assert.Equal(1, GetEntryValue<int>(alice, "Rank"));
        Assert.Equal(1, GetPositionOfPlayer("Alice"));
    }

    [Fact]
    public void leaderboard_and_player_positions_are_loaded_from_sqlite()
    {
        AssertExternalWallFailure(() => InvokeSaveLeaderboardRecord(CreateFinishedGame(128), "Alice"));
        AssertExternalWallFailure(() => InvokeSaveLeaderboardRecord(CreateFinishedGame(256), "Bob"));
        AssertExternalWallFailure(() => InvokeSaveLeaderboardRecord(CreateFinishedGame(256), "Cara"));

        AssertLeaderboardRows(("Bob", 256), ("Cara", 256), ("Alice", 128));

        List<object> entries = GetLeaderboardEntries();
        Assert.Equal(new[] { 256, 256, 128 }, entries.Select(entry => GetEntryValue<int>(entry, "Score")).ToArray());
        Assert.Equal(1, GetEntryValue<int>(entries.Single(entry => GetEntryValue<string>(entry, "PlayerName") == "Bob"), "Rank"));
        Assert.Equal(1, GetEntryValue<int>(entries.Single(entry => GetEntryValue<string>(entry, "PlayerName") == "Cara"), "Rank"));
        Assert.Equal(3, GetEntryValue<int>(entries.Single(entry => GetEntryValue<string>(entry, "PlayerName") == "Alice"), "Rank"));
        Assert.Equal(1, GetPositionOfPlayer("Bob"));
        Assert.Equal(1, GetPositionOfPlayer("Cara"));
        Assert.Equal(3, GetPositionOfPlayer("Alice"));
        Assert.Equal(4, GetPositionOfPlayer("Dana"));
    }

    [Fact]
    public void manual_save_and_load_restore_board_score_and_recorded_state()
    {
        Game2048Class game2048 = new Game2048Class();
        string[] savedBoard = new[]
        {
            "2", "4", "8", "16",
            "32", "64", "128", "256",
            "512", "1024", "", "",
            "", "", "", ""
        };

        SetTiles(game2048, savedBoard);
        game2048.myScore = 512;
        game2048.myWin = true;
        game2048.myLose = false;
        SetPrivateBool(game2048, "myScoreRecorded", true);
        SetPrivateBool(game2048, "myLeakedShouldAddTile", true);

        InvokeInstanceMethod(game2048, "saveGame", "slot1");
        AssertSavedGameRow("slot1", 512, win: true, lose: false, scoreRecorded: true, leakedShouldAddTile: true);

        SetTiles(game2048, new[]
        {
            "", "", "", "",
            "", "", "", "",
            "", "", "2", "2",
            "4", "4", "8", "8"
        });
        game2048.myScore = 8;
        game2048.myWin = false;
        game2048.myLose = true;
        SetPrivateBool(game2048, "myScoreRecorded", false);
        SetPrivateBool(game2048, "myLeakedShouldAddTile", false);

        InvokeInstanceMethod(game2048, "loadGame", "slot1");
        Game2048StateModel loadedState = game2048.getGameState();

        Assert.Equal(savedBoard, GetTileValues(game2048));
        Assert.Equal(512, game2048.myScore);
        Assert.True(game2048.myWin);
        Assert.False(game2048.myLose);
        Assert.True(GetPrivateBool(game2048, "myScoreRecorded"));
        Assert.True(GetPrivateBool(game2048, "myLeakedShouldAddTile"));
        Assert.True(loadedState.RecordSaved);
        Assert.False(loadedState.CanSaveRecord);
    }

    private void AssertLeaderboardRows(params (string PlayerName, int BestScore)[] expectedRows)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={persistenceScope.DatabasePath}");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT PlayerName, BestScore
            FROM LeaderboardEntries
            ORDER BY BestScore DESC, PlayerName COLLATE NOCASE;";

        List<(string PlayerName, int BestScore)> actualRows = new List<(string PlayerName, int BestScore)>();
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            actualRows.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        Assert.Equal(expectedRows, actualRows);
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

    private static void InvokeSaveLeaderboardRecord(Game2048Class game2048, string playerName)
    {
        MethodInfo method = typeof(Game2048Class).GetMethod("saveLeaderboardRecord", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        try
        {
            method.Invoke(game2048, new object[] { playerName });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static void AssertExternalWallFailure(Action action)
    {
        Exception exception = Record.Exception(action);
        Assert.NotNull(exception);
        Assert.True(
            exception is HttpRequestException or TaskCanceledException,
            $"Expected HttpRequestException or TaskCanceledException, but got {exception.GetType().FullName}.");
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

    private void AssertSavedGameRow(string slotKey, int score, bool win, bool lose, bool scoreRecorded, bool leakedShouldAddTile)
    {
        using SqliteConnection connection = new SqliteConnection($"Data Source={persistenceScope.DatabasePath}");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Score, Win, Lose, ScoreRecorded, LeakedShouldAddTile
            FROM SavedGames
            WHERE SlotKey = $slotKey;";
        command.Parameters.AddWithValue("$slotKey", slotKey);

        using SqliteDataReader reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(score, reader.GetInt32(0));
        Assert.Equal(win, reader.GetBoolean(1));
        Assert.Equal(lose, reader.GetBoolean(2));
        Assert.Equal(scoreRecorded, reader.GetBoolean(3));
        Assert.Equal(leakedShouldAddTile, reader.GetBoolean(4));
    }

    private static object InvokeInstanceMethod(Game2048Class game2048, string methodName, params object[] args)
    {
        MethodInfo method = typeof(Game2048Class).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        try
        {
            return method.Invoke(game2048, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static bool GetPrivateBool(Game2048Class game2048, string fieldName)
    {
        FieldInfo field = typeof(Game2048Class).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<bool>(field.GetValue(game2048));
    }

    private static void SetPrivateBool(Game2048Class game2048, string fieldName, bool value)
    {
        FieldInfo field = typeof(Game2048Class).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(game2048, value);
    }

    private static string[] GetTileValues(Game2048Class game2048)
    {
        FieldInfo myTiles = typeof(Game2048Class).GetField("myTiles", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(myTiles);
        Array tiles = Assert.IsAssignableFrom<Array>(myTiles.GetValue(game2048));
        return tiles.Cast<Game2048Class.Tile>().Select(tile => tile.value).ToArray();
    }

    private static void SetTiles(Game2048Class game2048, string[] values)
    {
        FieldInfo myTiles = typeof(Game2048Class).GetField("myTiles", BindingFlags.Instance | BindingFlags.NonPublic)!;
        myTiles.SetValue(game2048, values.Select(value => new Game2048Class.Tile(value)).ToArray());
    }
}
