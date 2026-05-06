using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game2048.Game;
using Game2048.Tests.TestDoubles;
using Game2048.Tests.TestSupport;

namespace Game2048.Tests;

internal static class Game2048ApprovalFixture
{
    public static string RenderFullJourney()
    {
        Game2048TestSupport.ResetLeaderboardState();
        DeterministicGame2048 game = new DeterministicGame2048();
        StringBuilder output = new StringBuilder();

        game.SetRandomValues(0.0, 0.0, 0.0, 0.95);
        game.resetGame();
        AppendGameStep(output, "new game", game, "Alice");

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "2", "", "2", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        });
        game.SetRandomValues(0.0, 0.0);
        game.left();
        AppendGameStep(output, "after left merge", game, "Alice");

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "", "", "", "",
            "2", "", "", "",
            "2", "", "", "",
            "", "", "", ""
        });
        game.SetRandomValues(0.0, 0.95);
        game.up();
        AppendGameStep(output, "after up merge", game, "Alice");

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "2", "", "2", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        });
        game.SetRandomValues(0.0, 0.0);
        game.right();
        AppendGameStep(output, "after right merge", game, "Alice");

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "2", "", "", "",
            "2", "", "", "",
            "", "", "", "",
            "", "", "", ""
        });
        game.SetRandomValues(0.0, 0.0);
        game.down();
        AppendGameStep(output, "after down merge", game, "Alice");

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "2", "4", "", "",
            "8", "16", "", "",
            "32", "64", "", "",
            "128", "256", "", ""
        }, score: 42);
        AppendGameStep(output, "before left no-op", game, "Alice");
        game.left();
        AppendGameStep(output, "after left no-op", game, "Alice");

        Game2048TestSupport.SeedFinishedGame(game, 42);
        game.keyPressed("left");
        AppendGameStep(output, "after lose via keyPressed", game, "Alice");

        game.saveLeaderboardRecord("Alice");
        AppendGameStep(output, "after saving leaderboard", game, "Alice");

        game.SetRandomValues(0.0, 0.0, 0.0, 0.0);
        game.keyPressed("escape");
        AppendGameStep(output, "after escape reset", game, "Alice");

        return output.ToString().TrimEnd();
    }

    public static string RenderWinJourney()
    {
        Game2048TestSupport.ResetLeaderboardState();
        DeterministicGame2048 game = new DeterministicGame2048();
        StringBuilder output = new StringBuilder();

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "1024", "1024", "", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        }, score: 100);
        AppendGameStep(output, "before winning move", game, "Winner");

        game.SetRandomValues(0.0, 0.0);
        game.left();
        AppendGameStep(output, "after winning move", game, "Winner");

        game.saveLeaderboardRecord("Winner");
        AppendGameStep(output, "after first save", game, "Winner");

        string secondSaveResult;
        try
        {
            game.saveLeaderboardRecord("Winner");
            secondSaveResult = "no exception";
        }
        catch (Exception ex)
        {
            secondSaveResult = ex.GetType().Name + ": " + ex.Message;
        }

        output.AppendLine("== second save attempt ==");
        output.AppendLine(secondSaveResult);

        return output.ToString().TrimEnd();
    }

    public static string RenderLeaderboardJourney()
    {
        Game2048TestSupport.ResetLeaderboardState();
        StringBuilder output = new StringBuilder();
        List<string> wallMessages = new List<string>();

        SaveFinishedGame(output, wallMessages, "Alice", 128, "after Alice 128");
        SaveFinishedGame(output, wallMessages, "Alice", 256, "after Alice 256");
        SaveFinishedGame(output, wallMessages, "Alice", 64, "after Alice 64");
        SaveFinishedGame(output, wallMessages, "Bob", 256, "after Bob 256");
        SaveFinishedGame(output, wallMessages, "Cara", 128, "after Cara 128");

        output.AppendLine("== final positions ==");
        AppendPlayerPosition(output, "Alice");
        AppendPlayerPosition(output, "Bob");
        AppendPlayerPosition(output, "Cara");
        AppendPlayerPosition(output, "Dana");

        return output.ToString().TrimEnd();
    }

    private static void SaveFinishedGame(StringBuilder output, List<string> wallMessages, string playerName, int score, string stepName)
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, score);
        game.saveLeaderboardRecord(playerName);
        wallMessages.AddRange(game.PostedMessages);

        output.AppendLine("== " + stepName + " ==");
        output.AppendLine("wallMessages:");
        foreach (string message in wallMessages)
        {
            output.AppendLine("- " + message);
        }
        AppendLeaderboard(output);
    }

    private static void AppendGameStep(StringBuilder output, string stepName, DeterministicGame2048 game, string playerName)
    {
        Game2048State state = game.paint();

        output.AppendLine("== " + stepName + " ==");
        output.AppendLine(RenderBoard(state));
        output.AppendLine($"score={state.Score} win={state.Win} lose={state.Lose} gameOver={state.GameOver} canMove={state.CanMove} canSaveRecord={state.CanSaveRecord} recordSaved={state.RecordSaved}");
        output.AppendLine($"overlay={state.Overlay}");
        output.AppendLine("messages=" + FormatMessages(state.Messages));
        output.AppendLine("scoreText=" + state.ScoreText);
        output.AppendLine("scoreTextDrawCount=" + state.ScoreTextDrawCount);
        output.AppendLine("wallMessages=" + FormatMessages(game.PostedMessages));
        output.AppendLine("playerPosition=" + Game2048.Game.Game2048.getPositionOfPlayer(playerName));
        AppendLeaderboard(output);
    }

    private static string RenderBoard(Game2048State state)
    {
        StringBuilder board = new StringBuilder();
        board.AppendLine("board:");
        for (int y = 0; y < state.PanelHeight; y++)
        {
            IEnumerable<string> values = state.Tiles
                .Where(tile => tile.Y == y)
                .OrderBy(tile => tile.X)
                .Select(tile => string.IsNullOrEmpty(tile.Value) ? "." : tile.Value.PadLeft(4));
            board.AppendLine("  [" + string.Join(" | ", values) + "]");
        }
        return board.ToString().TrimEnd();
    }

    private static void AppendLeaderboard(StringBuilder output)
    {
        List<LeaderboardEntry> entries = Game2048.Game.Game2048.getLeaderboardEntries();
        output.AppendLine("leaderboard:");
        if (entries.Count == 0)
        {
            output.AppendLine("- <empty>");
            return;
        }

        foreach (LeaderboardEntry entry in entries)
        {
            output.AppendLine($"- #{entry.Rank} {entry.PlayerName} => {entry.Score}");
        }
    }

    private static void AppendPlayerPosition(StringBuilder output, string playerName)
    {
        output.AppendLine($"- {playerName}: {Game2048.Game.Game2048.getPositionOfPlayer(playerName)}");
    }

    private static string FormatMessages(IEnumerable<string> messages)
    {
        string[] values = messages.ToArray();
        return values.Length == 0 ? "<none>" : string.Join(" | ", values);
    }
}
