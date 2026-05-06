using System.Reflection;
using Game2048.Game.Core;
using Game2048Class = Game2048.Game.Game2048;

namespace Game2048.Tests.TestSupport;

internal static class Game2048TestSupport
{
    public static void ResetLeaderboardState()
    {
        object store = typeof(Game2048Class)
            .GetField("leaderboardStore", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;

        object scoresByPlayer = store.GetType()
            .GetField("scoresByPlayer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(store)!;

        scoresByPlayer.GetType().GetMethod("Clear", Type.EmptyTypes)!.Invoke(scoresByPlayer, null);
    }

    public static void ConfigureGame(
        Game2048Class game,
        string[] values,
        int score = 0,
        bool win = false,
        bool lose = false,
        bool scoreRecorded = false)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (values.Length != Game2048Class.PANEL_WIDTH * Game2048Class.PANEL_HEIGHT)
        {
            throw new ArgumentException("A full 4x4 board is required.", nameof(values));
        }

        object board = GetBoard(game);
        SetBoardTiles(board, values);
        SetBoardField(board, "score", score);
        SetBoardField(board, "win", win);
        SetBoardField(board, "lose", lose);
        game.myScore = score;
        game.myWin = win;
        game.myLose = lose;
        GetScoreRecordedField().SetValue(game, scoreRecorded);
    }

    public static void SeedFinishedGame(Game2048Class game, int score)
    {
        ConfigureGame(game, new[]
        {
            "2", "4", "2", "4",
            "4", "2", "4", "2",
            "2", "4", "2", "4",
            "4", "2", "4", "2"
        }, score: score);

        game.keyPressed("left");
    }

    private static object GetBoard(Game2048Class game)
    {
        return typeof(Game2048Class).GetField("myBoard", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(game)!;
    }

    private static void SetBoardTiles(object board, IEnumerable<string> values)
    {
        BoardTile[] tiles = values.Select(value => new BoardTile(value)).ToArray();
        board.GetType().GetField("tiles", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(board, tiles);
    }

    private static void SetBoardField(object board, string fieldName, object value)
    {
        board.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(board, value);
    }

    private static FieldInfo GetScoreRecordedField()
    {
        return typeof(Game2048Class).GetField("myScoreRecorded", BindingFlags.Instance | BindingFlags.NonPublic)!;
    }
}
