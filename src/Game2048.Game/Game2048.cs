using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Game2048.Game;

public class Game2048
{
    private static readonly string BG_COLOR = "#bbada0";
    private static readonly string FONT_NAME = "Arial";
    private static readonly int TILE_SIZE = 64;
    private static readonly int TILES_MARGIN = 16;
    public static readonly int PANEL_WIDTH = 4;
    public static readonly int PANEL_HEIGHT = 4;

    private static readonly Random random = new Random();
    private static readonly object leaderboardLock = new object();
    private static readonly Dictionary<string, int> leaderboardScoresByPlayer = new Dictionary<string, int>();

    private Tile[] myTiles;
    private bool myScoreRecorded = false;
    public bool myWin = false;
    public bool myLose = false;
    public int myScore = 0;

    public Game2048()
    {
        resetGame();
    }

    public void resetGame()
    {
        myScore = 0;
        myWin = false;
        myLose = false;
        myScoreRecorded = false;
        myTiles = new Tile[PANEL_WIDTH * PANEL_HEIGHT];
        for (int i = 0; i < myTiles.Length; i++)
        {
            myTiles[i] = new Tile();
        }
        addTile();
        addTile();
    }

    public void left()
    {
        bool needAddTile = false;
        for (int i = 0; i < PANEL_HEIGHT; i++)
        {
            Tile[] line = getLine(i);
            Tile[] merged = mergeLine(moveLine(line));
            setLine(i, merged);
            if (!needAddTile && !compare(line, merged))
            {
                needAddTile = true;
            }
        }

        if (needAddTile)
        {
            addTile();
        }
    }

    public void right()
    {
        myTiles = rotate(180);
        left();
        myTiles = rotate(180);
    }

    public void up()
    {
        myTiles = rotate(270);
        left();
        myTiles = rotate(90);
    }

    public void down()
    {
        myTiles = rotate(90);
        left();
        myTiles = rotate(270);
    }

    public void keyPressed(string keyCode)
    {
        if (keyCode == "escape")
        {
            resetGame();
        }
        if (!canMove())
        {
            myLose = true;
        }

        if (!myWin && !myLose)
        {
            switch (keyCode)
            {
                case "left":
                    left();
                    break;
                case "right":
                    right();
                    break;
                case "down":
                    down();
                    break;
                case "up":
                    up();
                    break;
            }
        }

        if (!myWin && !canMove())
        {
            myLose = true;
        }
    }

    public void saveLeaderboardRecord(string playerName)
    {
        string playerNameToRecord = playerName ?? string.Empty;
        if (playerNameToRecord.Length == 0)
        {
            throw new ArgumentException("Player name is required.", nameof(playerName));
        }
        if (!myWin && !myLose)
        {
            throw new InvalidOperationException("Leaderboard records can only be saved after the game is over.");
        }
        if (myScoreRecorded)
        {
            throw new InvalidOperationException("This game's score has already been recorded.");
        }

        lock (leaderboardLock)
        {
            if (!leaderboardScoresByPlayer.TryGetValue(playerNameToRecord, out int bestScore) || myScore > bestScore)
            {
                leaderboardScoresByPlayer[playerNameToRecord] = myScore;
            }
        }

        string message = playerNameToRecord + " scored " + myScore + " points in LEGACY 2048!";
        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        HttpResponseMessage response = client.PostAsync(
            "http://7k7k6666.com/api/wall",
            new StringContent(message, Encoding.UTF8, "text/plain")).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        myScoreRecorded = true;
    }

    public static List<LeaderboardEntry> getLeaderboardEntries()
    {
        lock (leaderboardLock)
        {
            return buildLeaderboardEntries(getSortedLeaderboardScores());
        }
    }

    public static int getPositionOfPlayer(string playerName)
    {
        string playerNameToFind = playerName ?? string.Empty;
        List<LeaderboardEntry> entries = getLeaderboardEntries();
        LeaderboardEntry entry = entries.FirstOrDefault(item => item.PlayerName.Equals(playerNameToFind, StringComparison.Ordinal));
        return entry != null ? entry.Rank : entries.Count + 1;
    }

    private static List<KeyValuePair<string, int>> getSortedLeaderboardScores()
    {
        return leaderboardScoresByPlayer
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<LeaderboardEntry> buildLeaderboardEntries(List<KeyValuePair<string, int>> scores)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>(scores.Count);
        int position = 1;
        for (int i = 0; i < scores.Count; i++)
        {
            if (i > 0 && scores[i].Value < scores[i - 1].Value)
            {
                position = i + 1;
            }
            entries.Add(new LeaderboardEntry
            {
                Rank = position,
                PlayerName = scores[i].Key,
                Score = scores[i].Value
            });
        }
        return entries;
    }

    private Tile tileAt(int x, int y)
    {
        return myTiles[x + y * PANEL_WIDTH];
    }

    private void addTile()
    {
        List<Tile> list = availableSpace();
        if (availableSpace().Count != 0)
        {
            int index = (int)(random.NextDouble() * list.Count) % list.Count;
            Tile emptyTime = list[index];
            emptyTime.value = new string[] { "2", "4" }[random.NextDouble() < 0.9 ? 0 : 1];
        }
    }

    private List<Tile> availableSpace()
    {
        List<Tile> list = new List<Tile>(16);
        foreach (Tile t in myTiles)
        {
            if (t.isEmpty())
            {
                list.Add(t);
            }
        }
        return list;
    }

    private bool isFull()
    {
        return availableSpace().Count == 0;
    }

    public bool canMove()
    {
        if (!isFull())
        {
            return true;
        }
        for (int x = 0; x < PANEL_WIDTH; x++)
        {
            for (int y = 0; y < PANEL_HEIGHT; y++)
            {
                Tile t = tileAt(x, y);
                if ((x < 3 && t.Equals(tileAt(x + 1, y)))
                    || ((y < 3) && t.Equals(tileAt(x, y + 1))))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool compare(Tile[] line1, Tile[] line2)
    {
        if (line1 == line2)
        {
            return true;
        }
        else if (line1.Length != line2.Length)
        {
            return false;
        }

        for (int i = 0; i < line1.Length; i++)
        {
            if (!line1[i].Equals(line2[i]))
            {
                return false;
            }
        }
        return true;
    }

    private Tile[] rotate(int angle)
    {
        Tile[] newTiles = new Tile[PANEL_WIDTH * PANEL_HEIGHT];
        int offsetX = 3, offsetY = 3;
        if (angle == 90)
        {
            offsetY = 0;
        }
        else if (angle == 270)
        {
            offsetX = 0;
        }

        double rad = Math.PI * angle / 180.0;
        int cos = (int)Math.Cos(rad);
        int sin = (int)Math.Sin(rad);
        for (int x = 0; x < PANEL_WIDTH; x++)
        {
            for (int y = 0; y < PANEL_HEIGHT; y++)
            {
                int newX = (x * cos) - (y * sin) + offsetX;
                int newY = (x * sin) + (y * cos) + offsetY;
                newTiles[(newX) + (newY) * PANEL_WIDTH] = tileAt(x, y);
            }
        }
        return newTiles;
    }

    private Tile[] moveLine(Tile[] oldLine)
    {
        LinkedList<Tile> l = new LinkedList<Tile>();
        for (int i = 0; i < PANEL_HEIGHT; i++)
        {
            if (!oldLine[i].isEmpty())
            {
                l.AddLast(oldLine[i]);
            }
        }
        if (l.Count == 0)
        {
            return oldLine;
        }
        else
        {
            Tile[] newLine = new Tile[PANEL_WIDTH];
            ensureSize(l, PANEL_WIDTH);
            for (int i = 0; i < PANEL_WIDTH; i++)
            {
                newLine[i] = l.First!.Value;
                l.RemoveFirst();
            }
            return newLine;
        }
    }

    private Tile[] mergeLine(Tile[] oldLine)
    {
        LinkedList<Tile> list = new LinkedList<Tile>();
        for (int i = 0; i < PANEL_WIDTH && !oldLine[i].isEmpty(); i++)
        {
            string num = oldLine[i].value;
            if (i < 3 && oldLine[i].Equals(oldLine[i + 1]))
            {
                myScore += int.Parse(num) * 2;
                num = (int.Parse(num) * 2).ToString();
                string ourTarget = "2048";
                if (num.Equals(ourTarget))
                {
                    myWin = true;
                }
                i++;
            }
            list.AddLast(new Tile(num));
        }
        if (list.Count == 0)
        {
            return oldLine;
        }
        else
        {
            ensureSize(list, PANEL_WIDTH);
            return list.ToArray();
        }
    }

    private static void ensureSize(ICollection<Tile> l, int s)
    {
        while (l.Count != s)
        {
            l.Add(new Tile());
        }
    }

    private Tile[] getLine(int index)
    {
        Tile[] result = new Tile[PANEL_WIDTH];
        for (int i = 0; i < PANEL_WIDTH; i++)
        {
            result[i] = tileAt(i, index);
        }
        return result;
    }

    private void setLine(int index, Tile[] re)
    {
        Array.Copy(re, 0, myTiles, index * PANEL_WIDTH, PANEL_HEIGHT);
    }

    public Game2048State getGameState()
    {
        return paint();
    }

    public Game2048State paint()
    {
        Game2048State state = new Game2048State();
        state.BoardBackground = BG_COLOR;
        state.PanelWidth = PANEL_WIDTH;
        state.PanelHeight = PANEL_HEIGHT;
        state.TileSize = TILE_SIZE;
        state.TilesMargin = TILES_MARGIN;
        state.Win = myWin;
        state.Lose = myLose;
        state.GameOver = myWin || myLose;
        state.Score = myScore;
        state.CanMove = canMove();
        state.CanSaveRecord = (myWin || myLose) && !myScoreRecorded;
        state.RecordSaved = myScoreRecorded;
        for (int y = 0; y < PANEL_HEIGHT; y++)
        {
            for (int x = 0; x < PANEL_WIDTH; x++)
            {
                drawTile(state, myTiles[x + y * PANEL_WIDTH], x, y);
            }
        }
        return state;
    }

    private void drawTile(Game2048State state, Tile tile, int x, int y)
    {
        int xOffset = offsetCoors(x);
        int yOffset = offsetCoors(y);
        int size = 40 - (int)Math.Pow(2, tile.value.Length);

        state.Tiles.Add(new TileState
        {
            X = x,
            Y = y,
            XOffset = xOffset,
            YOffset = yOffset,
            Value = tile.value,
            Background = tile.getBackground(),
            Foreground = tile.getForeground(),
            FontName = FONT_NAME,
            FontSize = size,
            IsEmpty = tile.isEmpty()
        });

        if (myWin || myLose)
        {
            state.Overlay = true;
            if (myWin)
            {
                state.Messages.Add("You won!");
            }
            if (myLose)
            {
                state.Messages.Add("Game over!");
                state.Messages.Add("You lose!");
            }
            if (myWin || myLose)
            {
                state.Messages.Add("Press ESC to play again");
            }
        }
        state.ScoreText = "Score: " + myScore;
        state.ScoreTextDrawCount++;
    }

    private static int offsetCoors(int arg)
    {
        return arg * (TILES_MARGIN + TILE_SIZE) + TILES_MARGIN;
    }

    public class Tile
    {
        public static Dictionary<string, string> colorMap = new Dictionary<string, string>()
        {
            { "2", "#eee4da" },
            { "4", "#ede0c8" },
            { "8", "#f2b179" },
            { "16", "#f59563" },
            { "32", "#f67c5f" },
            { "64", "#f65e3b" },
            { "128", "#edcf72" },
            { "256", "#edcc61" },
            { "512", "#edc850" },
            { "1024", "#edc53f" },
            { "2048", "#edc22e" },
            { "4096", "#edc000" },
            { "8192", "#edab32" },
        };

        public string value;

        public Tile()
            : this("")
        {
        }

        public Tile(string value)
        {
            this.value = value;
        }

        public bool isEmpty()
        {
            return value.Length == 0;
        }

        public string getForeground()
        {
            return value.Length == 1 ? "#776e65" : "#f9f6f2";
        }

        public string getBackground()
        {
            string color;
            if (!colorMap.TryGetValue(value, out color)) return "#cdc1b4";
            return color;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Tile)
                return value.Equals(((Tile)obj).value);
            return false;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }
}

public class Game2048State
{
    public string BoardBackground { get; set; }
    public int PanelWidth { get; set; }
    public int PanelHeight { get; set; }
    public int TileSize { get; set; }
    public int TilesMargin { get; set; }
    public bool Win { get; set; }
    public bool Lose { get; set; }
    public bool GameOver { get; set; }
    public bool CanMove { get; set; }
    public bool CanSaveRecord { get; set; }
    public bool RecordSaved { get; set; }
    public int Score { get; set; }
    public string ScoreText { get; set; }
    public int ScoreTextDrawCount { get; set; }
    public bool Overlay { get; set; }
    public List<string> Messages { get; set; } = new List<string>();
    public List<TileState> Tiles { get; set; } = new List<TileState>();
}

public class LeaderboardEntry
{
    public int Rank { get; set; }
    public string PlayerName { get; set; }
    public int Score { get; set; }
}

public class TileState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int XOffset { get; set; }
    public int YOffset { get; set; }
    public string Value { get; set; }
    public string Background { get; set; }
    public string Foreground { get; set; }
    public string FontName { get; set; }
    public int FontSize { get; set; }
    public bool IsEmpty { get; set; }
}
