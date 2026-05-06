using System;
using System.Linq;
using Game2048.Game;
using Game2048.Tests.TestDoubles;
using Game2048.Tests.TestSupport;
using Xunit;

namespace Game2048.Tests;

public class Game2048GuardTest
{
    public Game2048GuardTest()
    {
        Game2048TestSupport.ResetLeaderboardState();
    }

    [Fact]
    public void save_leaderboard_record_throws_when_game_is_not_over()
    {
        DeterministicGame2048 game = new DeterministicGame2048();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => game.saveLeaderboardRecord("Alice"));

        Assert.Equal("Leaderboard records can only be saved after the game is over.", ex.Message);
    }

    [Fact]
    public void save_leaderboard_record_throws_when_player_name_is_null()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, 64);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => game.saveLeaderboardRecord(null));

        Assert.Equal("playerName", ex.ParamName);
    }

    [Fact]
    public void save_leaderboard_record_throws_when_player_name_is_empty()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, 64);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => game.saveLeaderboardRecord(string.Empty));

        Assert.Equal("playerName", ex.ParamName);
    }

    [Fact]
    public void save_leaderboard_record_allows_whitespace_only_names()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, 64);

        game.saveLeaderboardRecord("   ");

        Assert.Equal(new[] { "    scored 64 points in LEGACY 2048!" }, game.PostedMessages);
        LeaderboardEntry entry = Assert.Single(Game2048.Game.Game2048.getLeaderboardEntries());
        Assert.Equal("   ", entry.PlayerName);
    }

    [Fact]
    public void save_leaderboard_record_throws_when_called_twice_for_same_game()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, 128);
        game.saveLeaderboardRecord("Alice");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => game.saveLeaderboardRecord("Alice"));

        Assert.Equal("This game's score has already been recorded.", ex.Message);
    }

    [Fact]
    public void get_position_of_player_returns_entry_count_plus_one_for_unknown_player()
    {
        SaveFinishedGame("Alice", 256);
        SaveFinishedGame("Bob", 128);

        Assert.Equal(3, Game2048.Game.Game2048.getPositionOfPlayer("Dana"));
        Assert.Equal(3, Game2048.Game.Game2048.getPositionOfPlayer(null));
    }

    [Fact]
    public void game_state_marks_finished_game_as_saveable_until_record_is_saved()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, 256);

        game.keyPressed("left");
        Game2048State beforeSave = game.getGameState();
        game.saveLeaderboardRecord("Alice");
        Game2048State afterSave = game.getGameState();

        Assert.True(beforeSave.GameOver);
        Assert.True(beforeSave.CanSaveRecord);
        Assert.False(beforeSave.RecordSaved);
        Assert.True(afterSave.GameOver);
        Assert.False(afterSave.CanSaveRecord);
        Assert.True(afterSave.RecordSaved);
    }

    [Fact]
    public void winning_game_state_sets_overlay_messages()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "1024", "1024", "", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        }, score: 10);
        game.SetRandomValues(0.0, 0.0);

        game.left();
        Game2048State state = game.paint();

        Assert.True(state.Win);
        Assert.True(state.GameOver);
        Assert.True(state.Overlay);
        Assert.Equal(new[] { "You won!", "Press ESC to play again" }, state.Messages.Distinct().ToArray());
        Assert.Equal("Score: 2058", state.ScoreText);
    }

    [Fact]
    public void losing_game_state_sets_overlay_messages()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, 10);

        game.keyPressed("left");
        Game2048State state = game.paint();

        Assert.True(state.Lose);
        Assert.True(state.GameOver);
        Assert.True(state.Overlay);
        Assert.Equal(new[] { "Game over!", "You lose!", "Press ESC to play again" }, state.Messages.Distinct().ToArray());
        Assert.Equal("Score: 10", state.ScoreText);
    }

    [Fact]
    public void can_move_is_true_when_full_board_has_an_adjacent_match()
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "2", "2", "4", "8",
            "16", "32", "64", "128",
            "256", "512", "1024", "2",
            "4", "8", "16", "32"
        });

        Assert.True(game.canMove());
    }

    [Fact]
    public void key_pressed_dispatches_supported_directions()
    {
        DeterministicGame2048 game = new DeterministicGame2048();

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "2", "", "2", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        });
        game.SetRandomValues(0.0, 0.0);
        game.keyPressed("right");
        Assert.Equal(4, game.myScore);
        Assert.False(game.myLose);

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "2", "", "", "",
            "2", "", "", "",
            "", "", "", "",
            "", "", "", ""
        });
        game.SetRandomValues(0.0, 0.0);
        game.keyPressed("down");
        Assert.Equal(4, game.myScore);
        Assert.False(game.myLose);

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "", "", "", "",
            "2", "", "", "",
            "2", "", "", "",
            "", "", "", ""
        });
        game.SetRandomValues(0.0, 0.0);
        game.keyPressed("up");
        Assert.Equal(4, game.myScore);
        Assert.False(game.myLose);

        Game2048TestSupport.ConfigureGame(game, new[]
        {
            "2", "", "", "",
            "", "", "", "",
            "", "", "", "",
            "", "", "", ""
        });
        game.keyPressed("noop");
        Assert.Equal(0, game.myScore);
        Assert.False(game.myLose);
    }

    [Fact]
    public void public_tile_preserves_legacy_color_and_equality_rules()
    {
        Game2048.Game.Game2048.Tile singleDigit = new Game2048.Game.Game2048.Tile("2");
        Game2048.Game.Game2048.Tile multiDigit = new Game2048.Game.Game2048.Tile("128");
        Game2048.Game.Game2048.Tile unknown = new Game2048.Game.Game2048.Tile("?");

        Assert.Equal("#776e65", singleDigit.getForeground());
        Assert.Equal("#f9f6f2", multiDigit.getForeground());
        Assert.Equal("#eee4da", singleDigit.getBackground());
        Assert.Equal("#cdc1b4", unknown.getBackground());
        Assert.True(singleDigit.Equals(new Game2048.Game.Game2048.Tile("2")));
        Assert.False(singleDigit.Equals(new object()));
        Assert.False(singleDigit.Equals(null));
    }

    private static void SaveFinishedGame(string playerName, int score)
    {
        DeterministicGame2048 game = new DeterministicGame2048();
        Game2048TestSupport.SeedFinishedGame(game, score);
        game.saveLeaderboardRecord(playerName);
    }
}
