using System.Reflection;
using Game2048Class = Game2048.Game.Game2048;
using Xunit;

namespace Game2048.Tests;

public class Game2048LegacyBehaviorTest
{
    [Fact(Skip = "legacy practice fixture")]
    public void invalid_repeat_move_after_a_real_move_can_still_grow_the_board()
    {
        Game2048Class game2048 = new Game2048Class();
        SetTiles(game2048, new[]
        {
            "", "2", "4", "8",
            "", "16", "32", "64",
            "2", "4", "8", "16",
            "4", "8", "16", "32"
        });

        game2048.keyPressed("left");
        int tilesBeforeInvalidMove = CountNonEmptyTiles(game2048);
        string boardBeforeInvalidMove = string.Join(",", GetTileValues(game2048));

        game2048.keyPressed("left");

        Assert.Equal(15, tilesBeforeInvalidMove);
        Assert.NotEqual(boardBeforeInvalidMove, string.Join(",", GetTileValues(game2048)));
    }

    [Fact(Skip = "legacy practice fixture")]
    public void tile_generation_can_replace_a_number_instead_of_using_the_only_empty_cell()
    {
        bool overwriteObserved = false;
        for (int attempt = 0; attempt < 24 && !overwriteObserved; attempt++)
        {
            Game2048Class game2048 = new Game2048Class();
            SetTiles(game2048, new[]
            {
                "", "2", "4", "8",
                "16", "32", "64", "128",
                "256", "512", "1024", "2048",
                "2", "4", "8", "64"
            });

            InvokeAddTile(game2048);
            overwriteObserved = GetTileValues(game2048)[15] != "64";
        }

        Assert.True(overwriteObserved);
    }

    [Fact(Skip = "legacy practice fixture")]
    public void some_stuck_boards_do_not_flip_to_lose()
    {
        Game2048Class game2048 = new Game2048Class();
        SetTiles(game2048, new[]
        {
            "2", "4", "8", "16",
            "16", "32", "64", "128",
            "256", "512", "1024", "2",
            "4", "8", "16", "32"
        });

        game2048.keyPressed("left");

        Assert.False(game2048.myLose);
        Assert.True(game2048.canMove());
    }

    private static void InvokeAddTile(Game2048Class game2048)
    {
        MethodInfo method = typeof(Game2048Class).GetMethod("addTile", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(game2048, Array.Empty<object>());
    }

    private static int CountNonEmptyTiles(Game2048Class game2048)
    {
        return GetTileValues(game2048).Count(value => value.Length > 0);
    }

    private static string[] GetTileValues(Game2048Class game2048)
    {
        FieldInfo myTiles = typeof(Game2048Class).GetField("myTiles", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array tiles = Assert.IsAssignableFrom<Array>(myTiles.GetValue(game2048));
        return tiles.Cast<Game2048Class.Tile>().Select(tile => tile.value).ToArray();
    }

    private static void SetTiles(Game2048Class game2048, string[] values)
    {
        FieldInfo myTiles = typeof(Game2048Class).GetField("myTiles", BindingFlags.Instance | BindingFlags.NonPublic)!;
        myTiles.SetValue(game2048, values.Select(value => new Game2048Class.Tile(value)).ToArray());
    }
}
