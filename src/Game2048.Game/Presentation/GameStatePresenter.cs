using System;
using Game2048.Game.Core;

namespace Game2048.Game.Presentation;

internal sealed class GameStatePresenter
{
    private const string BackgroundColor = "#bbada0";
    private const string FontName = "Arial";
    private const int TileSize = 64;
    private const int TilesMargin = 16;

    public Game2048State Present(GameBoard board, bool scoreRecorded)
    {
        Game2048State state = new Game2048State
        {
            BoardBackground = BackgroundColor,
            PanelWidth = Game2048.PANEL_WIDTH,
            PanelHeight = Game2048.PANEL_HEIGHT,
            TileSize = TileSize,
            TilesMargin = TilesMargin,
            Win = board.Win,
            Lose = board.Lose,
            GameOver = board.Win || board.Lose,
            Score = board.Score,
            CanMove = board.CanMove(),
            CanSaveRecord = (board.Win || board.Lose) && !scoreRecorded,
            RecordSaved = scoreRecorded
        };

        for (int y = 0; y < Game2048.PANEL_HEIGHT; y++)
        {
            for (int x = 0; x < Game2048.PANEL_WIDTH; x++)
            {
                DrawTile(state, board.TileAt(x, y), x, y);
            }
        }

        return state;
    }

    private static void DrawTile(Game2048State state, BoardTile tile, int x, int y)
    {
        state.Tiles.Add(new TileState
        {
            X = x,
            Y = y,
            XOffset = OffsetCoordinates(x),
            YOffset = OffsetCoordinates(y),
            Value = tile.Value,
            Background = tile.GetBackground(),
            Foreground = tile.GetForeground(),
            FontName = FontName,
            FontSize = 40 - (int)Math.Pow(2, tile.Value.Length),
            IsEmpty = tile.IsEmpty()
        });

        if (state.Win || state.Lose)
        {
            state.Overlay = true;
            if (state.Win)
            {
                state.Messages.Add("You won!");
            }
            if (state.Lose)
            {
                state.Messages.Add("Game over!");
                state.Messages.Add("You lose!");
            }
            if (state.Win || state.Lose)
            {
                state.Messages.Add("Press ESC to play again");
            }
        }

        state.ScoreText = "Score: " + state.Score;
        state.ScoreTextDrawCount++;
    }

    private static int OffsetCoordinates(int coordinate)
    {
        return coordinate * (TilesMargin + TileSize) + TilesMargin;
    }
}
