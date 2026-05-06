using System.Linq;
using Game2048Class = Game2048.Game.Game2048;
using Game2048StateModel = Game2048.Game.Game2048State;
using Game2048.Tests.TestDoubles;
using Game2048.Tests.TestSupport;
using Xunit;

namespace Game2048.Tests;

public class Game2048FacadeTest
{
    public Game2048FacadeTest()
    {
        Game2048TestSupport.ResetLeaderboardState();
    }

    [Fact]
    public void reset_game_uses_overridable_randomness_to_place_expected_tiles()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        game.SetRandomValues(
            0.0, 0.0,
            0.0, 0.95);

        game.resetGame();

        Game2048StateModel state = game.paint();
        var filledTiles = state.Tiles
            .Where(tile => !tile.IsEmpty)
            .OrderBy(tile => tile.Y)
            .ThenBy(tile => tile.X)
            .ToArray();

        Assert.Equal(2, filledTiles.Length);
        Assert.Equal((0, 0, "2"), (filledTiles[0].X, filledTiles[0].Y, filledTiles[0].Value));
        Assert.Equal((1, 0, "4"), (filledTiles[1].X, filledTiles[1].Y, filledTiles[1].Value));
    }

    [Fact]
    public void save_leaderboard_record_uses_overridable_wall_posting()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, 128);

        game.saveLeaderboardRecord("Alice");

        Assert.Equal(new[] { "Alice scored 128 points in LEGACY 2048!" }, game.PostedMessages);
        Assert.True(game.getGameState().RecordSaved);
        Assert.Equal(1, Game2048Class.getPositionOfPlayer("Alice"));
    }
}
