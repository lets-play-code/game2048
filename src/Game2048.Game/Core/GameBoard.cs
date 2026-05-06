using System;
using System.Collections.Generic;
using System.Linq;

namespace Game2048.Game.Core;

internal sealed class GameBoard
{
    private readonly Func<double> nextRandomDouble;
    private BoardTile[] tiles = Array.Empty<BoardTile>();
    private int score;
    private bool win;
    private bool lose;

    public GameBoard(Func<double> nextRandomDouble)
    {
        this.nextRandomDouble = nextRandomDouble ?? throw new ArgumentNullException(nameof(nextRandomDouble));
    }

    public int Score => score;
    public bool Win => win;
    public bool Lose => lose;

    public void Reset()
    {
        score = 0;
        win = false;
        lose = false;
        tiles = new BoardTile[Game2048.PANEL_WIDTH * Game2048.PANEL_HEIGHT];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = new BoardTile();
        }

        AddTile();
        AddTile();
    }

    public void Left()
    {
        bool needAddTile = false;
        for (int i = 0; i < Game2048.PANEL_HEIGHT; i++)
        {
            BoardTile[] line = GetLine(i);
            BoardTile[] merged = MergeLine(MoveLine(line));
            SetLine(i, merged);
            if (!needAddTile && !Compare(line, merged))
            {
                needAddTile = true;
            }
        }

        if (needAddTile)
        {
            AddTile();
        }
    }

    public void Right()
    {
        tiles = Rotate(180);
        Left();
        tiles = Rotate(180);
    }

    public void Up()
    {
        tiles = Rotate(270);
        Left();
        tiles = Rotate(90);
    }

    public void Down()
    {
        tiles = Rotate(90);
        Left();
        tiles = Rotate(270);
    }

    public bool CanMove()
    {
        if (!IsFull())
        {
            return true;
        }

        for (int x = 0; x < Game2048.PANEL_WIDTH; x++)
        {
            for (int y = 0; y < Game2048.PANEL_HEIGHT; y++)
            {
                BoardTile tile = TileAt(x, y);
                if ((x < 3 && tile.Equals(TileAt(x + 1, y)))
                    || (y < 3 && tile.Equals(TileAt(x, y + 1))))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void UpdateLoseState()
    {
        lose = !CanMove();
    }

    public BoardTile TileAt(int x, int y)
    {
        return tiles[x + y * Game2048.PANEL_WIDTH];
    }

    private void AddTile()
    {
        List<BoardTile> availableTiles = AvailableSpace();
        if (availableTiles.Count == 0)
        {
            return;
        }

        int index = (int)(nextRandomDouble() * availableTiles.Count) % availableTiles.Count;
        BoardTile emptyTile = availableTiles[index];
        emptyTile.Value = nextRandomDouble() < 0.9 ? "2" : "4";
    }

    private List<BoardTile> AvailableSpace()
    {
        List<BoardTile> availableTiles = new List<BoardTile>(Game2048.PANEL_WIDTH * Game2048.PANEL_HEIGHT);
        foreach (BoardTile tile in tiles)
        {
            if (tile.IsEmpty())
            {
                availableTiles.Add(tile);
            }
        }

        return availableTiles;
    }

    private bool IsFull()
    {
        return AvailableSpace().Count == 0;
    }

    private static bool Compare(BoardTile[] left, BoardTile[] right)
    {
        if (left == right)
        {
            return true;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (int i = 0; i < left.Length; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private BoardTile[] Rotate(int angle)
    {
        BoardTile[] rotated = new BoardTile[Game2048.PANEL_WIDTH * Game2048.PANEL_HEIGHT];
        int offsetX = 3;
        int offsetY = 3;
        if (angle == 90)
        {
            offsetY = 0;
        }
        else if (angle == 270)
        {
            offsetX = 0;
        }

        double radians = Math.PI * angle / 180.0;
        int cos = (int)Math.Cos(radians);
        int sin = (int)Math.Sin(radians);
        for (int x = 0; x < Game2048.PANEL_WIDTH; x++)
        {
            for (int y = 0; y < Game2048.PANEL_HEIGHT; y++)
            {
                int newX = (x * cos) - (y * sin) + offsetX;
                int newY = (x * sin) + (y * cos) + offsetY;
                rotated[newX + newY * Game2048.PANEL_WIDTH] = TileAt(x, y);
            }
        }

        return rotated;
    }

    private BoardTile[] MoveLine(BoardTile[] oldLine)
    {
        LinkedList<BoardTile> movedTiles = new LinkedList<BoardTile>();
        for (int i = 0; i < Game2048.PANEL_HEIGHT; i++)
        {
            if (!oldLine[i].IsEmpty())
            {
                movedTiles.AddLast(oldLine[i]);
            }
        }

        if (movedTiles.Count == 0)
        {
            return oldLine;
        }

        BoardTile[] newLine = new BoardTile[Game2048.PANEL_WIDTH];
        EnsureSize(movedTiles, Game2048.PANEL_WIDTH);
        for (int i = 0; i < Game2048.PANEL_WIDTH; i++)
        {
            newLine[i] = movedTiles.First!.Value;
            movedTiles.RemoveFirst();
        }

        return newLine;
    }

    private BoardTile[] MergeLine(BoardTile[] oldLine)
    {
        LinkedList<BoardTile> mergedTiles = new LinkedList<BoardTile>();
        for (int i = 0; i < Game2048.PANEL_WIDTH && !oldLine[i].IsEmpty(); i++)
        {
            string value = oldLine[i].Value;
            if (i < 3 && oldLine[i].Equals(oldLine[i + 1]))
            {
                score += int.Parse(value) * 2;
                value = (int.Parse(value) * 2).ToString();
                if (value.Equals("2048"))
                {
                    win = true;
                }
                i++;
            }

            mergedTiles.AddLast(new BoardTile(value));
        }

        if (mergedTiles.Count == 0)
        {
            return oldLine;
        }

        EnsureSize(mergedTiles, Game2048.PANEL_WIDTH);
        return mergedTiles.ToArray();
    }

    private static void EnsureSize(ICollection<BoardTile> tilesToFill, int size)
    {
        while (tilesToFill.Count != size)
        {
            tilesToFill.Add(new BoardTile());
        }
    }

    private BoardTile[] GetLine(int index)
    {
        BoardTile[] result = new BoardTile[Game2048.PANEL_WIDTH];
        for (int i = 0; i < Game2048.PANEL_WIDTH; i++)
        {
            result[i] = TileAt(i, index);
        }

        return result;
    }

    private void SetLine(int index, BoardTile[] line)
    {
        Array.Copy(line, 0, tiles, index * Game2048.PANEL_WIDTH, Game2048.PANEL_HEIGHT);
    }
}
