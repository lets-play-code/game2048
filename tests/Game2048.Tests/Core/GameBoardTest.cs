using System.Reflection;
using Game2048.Game.Core;
using Xunit;

namespace Game2048.Tests.Core;

public class GameBoardTest
{
    [Fact]
    public void left_merges_tiles_updates_score_and_adds_random_tile()
    {
        GameBoard board = CreateBoard(new[]
        {
            "2", "", "2", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        }, randomValues: new[] { 0.0, 0.0 });

        board.Left();

        Assert.Equal(4, board.Score);
        Assert.False(board.Win);
        Assert.Equal("4", board.TileAt(0, 0).Value);
        Assert.Equal("2", board.TileAt(1, 0).Value);
    }

    [Fact]
    public void update_lose_state_marks_board_without_moves_as_lost()
    {
        GameBoard board = CreateBoard(new[]
        {
            "2", "4", "2", "4",
            "4", "2", "4", "2",
            "2", "4", "2", "4",
            "4", "2", "4", "2"
        });

        board.UpdateLoseState();

        Assert.False(board.CanMove());
        Assert.True(board.Lose);
    }

    [Fact]
    public void board_tile_equality_returns_false_for_other_object_types()
    {
        Assert.False(new BoardTile("2").Equals(new object()));
    }

    private static GameBoard CreateBoard(string[] values, double[] randomValues = null, int score = 0, bool win = false, bool lose = false)
    {
        Queue<double> queue = new Queue<double>(randomValues ?? Array.Empty<double>());
        GameBoard board = new GameBoard(() => queue.Count > 0 ? queue.Dequeue() : 0.0);

        SetField(board, "tiles", values.Select(value => new BoardTile(value)).ToArray());
        SetField(board, "score", score);
        SetField(board, "win", win);
        SetField(board, "lose", lose);
        return board;
    }

    private static void SetField(GameBoard board, string fieldName, object value)
    {
        typeof(GameBoard).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(board, value);
    }
}
