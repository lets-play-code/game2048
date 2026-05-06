using System.Reflection;
using Game2048.Game;
using Game2048.Game.Core;
using Game2048.Game.Presentation;
using Xunit;

namespace Game2048.Tests.Presentation;

public class GameStatePresenterTest
{
    [Fact]
    public void present_maps_board_state_into_game_state()
    {
        GameBoard board = CreateBoard(new[]
        {
            "2", "", "4", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        }, score: 42, lose: true);
        GameStatePresenter presenter = new GameStatePresenter();

        Game2048State state = presenter.Present(board, scoreRecorded: false);

        Assert.True(state.Lose);
        Assert.True(state.GameOver);
        Assert.True(state.CanSaveRecord);
        Assert.True(state.Overlay);
        Assert.Equal("Score: 42", state.ScoreText);
        Assert.Equal(16, state.ScoreTextDrawCount);
        Assert.Contains("Game over!", state.Messages);
        Assert.Equal("2", state.Tiles.Single(tile => tile.X == 0 && tile.Y == 0).Value);
    }

    private static GameBoard CreateBoard(string[] values, int score = 0, bool win = false, bool lose = false)
    {
        GameBoard board = new GameBoard(() => 0.0);
        typeof(GameBoard).GetField("tiles", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(board, values.Select(value => new BoardTile(value)).ToArray());
        typeof(GameBoard).GetField("score", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(board, score);
        typeof(GameBoard).GetField("win", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(board, win);
        typeof(GameBoard).GetField("lose", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(board, lose);
        return board;
    }
}
