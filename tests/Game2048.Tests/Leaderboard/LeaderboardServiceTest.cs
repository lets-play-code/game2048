using Game2048.Game;
using Game2048.Game.Leaderboard;
using Xunit;

namespace Game2048.Tests.Leaderboard;

public class LeaderboardServiceTest
{
    [Fact]
    public void constructor_requires_a_store()
    {
        Assert.Throws<ArgumentNullException>(() => new LeaderboardService(null));
    }

    [Fact]
    public void save_record_keeps_the_best_score_and_publishes_each_message()
    {
        InMemoryLeaderboardStore store = new InMemoryLeaderboardStore();
        LeaderboardService service = new LeaderboardService(store);
        List<string> publishedMessages = new List<string>();

        service.SaveRecord("Alice", 128, gameOver: true, alreadyRecorded: false, publishedMessages.Add);
        service.SaveRecord("Alice", 256, gameOver: true, alreadyRecorded: false, publishedMessages.Add);
        service.SaveRecord("Alice", 64, gameOver: true, alreadyRecorded: false, publishedMessages.Add);

        LeaderboardEntry entry = Assert.Single(service.GetEntries());
        Assert.Equal(256, entry.Score);
        Assert.Equal(new[]
        {
            "Alice scored 128 points in LEGACY 2048!",
            "Alice scored 256 points in LEGACY 2048!",
            "Alice scored 64 points in LEGACY 2048!"
        }, publishedMessages);
    }

    [Fact]
    public void get_position_of_player_uses_shared_ranks_for_ties_and_appends_unknown_players()
    {
        InMemoryLeaderboardStore store = new InMemoryLeaderboardStore();
        LeaderboardService service = new LeaderboardService(store);

        service.SaveRecord("Alice", 256, gameOver: true, alreadyRecorded: false, _ => { });
        service.SaveRecord("Bob", 256, gameOver: true, alreadyRecorded: false, _ => { });
        service.SaveRecord("Cara", 128, gameOver: true, alreadyRecorded: false, _ => { });

        Assert.Equal(1, service.GetPositionOfPlayer("Alice"));
        Assert.Equal(1, service.GetPositionOfPlayer("Bob"));
        Assert.Equal(3, service.GetPositionOfPlayer("Cara"));
        Assert.Equal(4, service.GetPositionOfPlayer("Dana"));
    }
}
