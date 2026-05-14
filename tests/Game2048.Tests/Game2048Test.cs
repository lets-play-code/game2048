using System.Collections;
using System.Net.Http;
using System.Reflection;
using Game2048Class = Game2048.Game.Game2048;
using Game2048StateModel = Game2048.Game.Game2048State;
using Xunit;

namespace Game2048.Tests;

public class Game2048Test
{
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

}
