using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Game2048.Tests;

public class Game2048WebTest
{

    [Fact]
    public async Task get_saves_returns_auto_and_three_manual_slots()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        await using Game2048WebApplicationFactory factory = scope.CreateFactory();
        using HttpClient client = factory.CreateClient();

        List<SaveSummaryResponse> saves = await client.GetFromJsonAsync<List<SaveSummaryResponse>>("/api/saves");

        Assert.NotNull(saves);
        Assert.Collection(saves,
            auto => AssertEmptySlot(auto, "auto"),
            slot1 => AssertEmptySlot(slot1, "slot1"),
            slot2 => AssertEmptySlot(slot2, "slot2"),
            slot3 => AssertEmptySlot(slot3, "slot3"));
    }

    private static void AssertEmptySlot(SaveSummaryResponse slot, string slotKey)
    {
        Assert.Equal(slotKey, slot.SlotKey);
        Assert.False(slot.HasData);
        Assert.Null(slot.Score);
        Assert.Null(slot.SavedAtUtc);
    }


    public class SaveSummaryResponse
    {
        public string SlotKey { get; set; } = string.Empty;
        public bool HasData { get; set; }
        public int? Score { get; set; }
        public DateTime? SavedAtUtc { get; set; }
    }
}
