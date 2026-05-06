using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Game2048.Game.Core;
using Game2048.Game.Leaderboard;
using Game2048.Game.Presentation;

namespace Game2048.Game;

public class Game2048
{
    public static readonly int PANEL_WIDTH = 4;
    public static readonly int PANEL_HEIGHT = 4;

    private static readonly Random random = new Random();
    private static readonly InMemoryLeaderboardStore leaderboardStore = new InMemoryLeaderboardStore();
    private static readonly LeaderboardService leaderboardService = new LeaderboardService(leaderboardStore);

    private readonly GameBoard myBoard;
    private readonly GameStatePresenter presenter = new GameStatePresenter();
    private bool myScoreRecorded = false;
    public bool myWin = false;
    public bool myLose = false;
    public int myScore = 0;

    public Game2048()
    {
        myBoard = new GameBoard(() => nextRandomDouble());
        resetGame();
    }

    protected virtual double nextRandomDouble()
    {
        return random.NextDouble();
    }

    protected virtual void postLeaderboardWallMessage(string message)
    {
        HttpClient client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        HttpResponseMessage response = client.PostAsync(
            "http://7k7k6666.com/api/wall",
            new StringContent(message, Encoding.UTF8, "text/plain")).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
    }

    public void resetGame()
    {
        myScoreRecorded = false;
        myBoard.Reset();
        syncBoardState();
    }

    public void left()
    {
        myBoard.Left();
        syncBoardState();
    }

    public void right()
    {
        myBoard.Right();
        syncBoardState();
    }

    public void up()
    {
        myBoard.Up();
        syncBoardState();
    }

    public void down()
    {
        myBoard.Down();
        syncBoardState();
    }

    public void keyPressed(string keyCode)
    {
        if (keyCode == "escape")
        {
            resetGame();
        }
        if (!canMove())
        {
            myBoard.UpdateLoseState();
            syncBoardState();
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
            myBoard.UpdateLoseState();
            syncBoardState();
        }
    }

    public void saveLeaderboardRecord(string playerName)
    {
        leaderboardService.SaveRecord(
            playerName,
            myScore,
            myWin || myLose,
            myScoreRecorded,
            postLeaderboardWallMessage);

        myScoreRecorded = true;
    }

    public static List<LeaderboardEntry> getLeaderboardEntries()
    {
        return leaderboardService.GetEntries();
    }

    public static int getPositionOfPlayer(string playerName)
    {
        return leaderboardService.GetPositionOfPlayer(playerName);
    }

    public bool canMove()
    {
        return myBoard.CanMove();
    }

    public Game2048State getGameState()
    {
        return paint();
    }

    public Game2048State paint()
    {
        return presenter.Present(myBoard, myScoreRecorded);
    }

    private void syncBoardState()
    {
        myScore = myBoard.Score;
        myWin = myBoard.Win;
        myLose = myBoard.Lose;
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
