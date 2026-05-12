using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Game2048.Tests;

public class Game2048WebTest
{
    [Fact]
    public async Task startup_creates_the_configured_sqlite_database_and_applies_migrations()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        Assert.False(File.Exists(scope.DatabasePath));

        await using Game2048WebApplicationFactory factory = scope.CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage homeResponse = await client.GetAsync("/");
        homeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage createGameResponse = await client.PostAsync("/api/games", content: null);
        createGameResponse.EnsureSuccessStatusCode();

        Assert.True(File.Exists(scope.DatabasePath));

        await using SqliteConnection connection = new SqliteConnection($"Data Source={scope.DatabasePath}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT name
            FROM sqlite_master
            WHERE type = 'table' AND name IN ('LeaderboardEntries', 'SavedGames', '__EFMigrationsHistory')
            ORDER BY name;";

        List<string> tables = new List<string>();
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Equal(new[] { "LeaderboardEntries", "SavedGames", "__EFMigrationsHistory" }, tables);
    }

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

    [Fact]
    public async Task move_endpoint_creates_auto_save_after_a_keypress()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        await using Game2048WebApplicationFactory factory = scope.CreateFactory();
        using HttpClient client = factory.CreateClient();

        GameCreatedResponse created = await CreateGameAsync(client);
        List<SaveSummaryResponse> beforeMove = await client.GetFromJsonAsync<List<SaveSummaryResponse>>("/api/saves");
        Assert.NotNull(beforeMove);
        Assert.False(beforeMove.Single(item => item.SlotKey == "auto").HasData);

        GameStateResponse moved = await MoveAsync(client, created.Id, "left");
        List<SaveSummaryResponse> afterMove = await client.GetFromJsonAsync<List<SaveSummaryResponse>>("/api/saves");

        Assert.NotNull(afterMove);
        SaveSummaryResponse auto = afterMove.Single(item => item.SlotKey == "auto");
        Assert.True(auto.HasData);
        Assert.Equal(moved.Score, auto.Score);
        Assert.NotNull(auto.SavedAtUtc);
    }

    [Fact]
    public async Task save_and_load_endpoints_round_trip_slot_data()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        await using Game2048WebApplicationFactory factory = scope.CreateFactory();
        using HttpClient client = factory.CreateClient();

        GameCreatedResponse created = await CreateGameAsync(client);
        GameStateResponse savedState = await MoveAsync(client, created.Id, "left");

        HttpResponseMessage saveResponse = await client.PostAsync($"/api/games/{created.Id}/save/slot1", content: null);
        saveResponse.EnsureSuccessStatusCode();
        GameStateResponse afterSave = await saveResponse.Content.ReadFromJsonAsync<GameStateResponse>();
        Assert.NotNull(afterSave);
        Assert.Equal(GetBoardSignature(savedState), GetBoardSignature(afterSave));

        HttpResponseMessage resetResponse = await client.PostAsync($"/api/games/{created.Id}/reset", content: null);
        resetResponse.EnsureSuccessStatusCode();

        HttpResponseMessage loadResponse = await client.PostAsync($"/api/games/{created.Id}/load/slot1", content: null);
        loadResponse.EnsureSuccessStatusCode();
        GameStateResponse loaded = await loadResponse.Content.ReadFromJsonAsync<GameStateResponse>();

        Assert.NotNull(loaded);
        Assert.Equal(savedState.Score, loaded.Score);
        Assert.Equal(savedState.RecordSaved, loaded.RecordSaved);
        Assert.Equal(savedState.Win, loaded.Win);
        Assert.Equal(savedState.Lose, loaded.Lose);
        Assert.Equal(GetBoardSignature(savedState), GetBoardSignature(loaded));

        List<SaveSummaryResponse> saves = await client.GetFromJsonAsync<List<SaveSummaryResponse>>("/api/saves");
        Assert.NotNull(saves);
        SaveSummaryResponse slot1 = saves.Single(item => item.SlotKey == "slot1");
        Assert.True(slot1.HasData);
        Assert.Equal(savedState.Score, slot1.Score);
    }

    [Fact]
    public async Task test_seed_endpoint_creates_existing_game_for_get_endpoint()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        await using Game2048WebApplicationFactory factory = scope.CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage seedResponse = await client.PostAsJsonAsync("/api/test/games/seeded-existing-game", new
        {
            boardJson = "[\"2\",\"4\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"16\",\"\",\"\",\"\",\"\"]",
            score = 32,
            win = false,
            lose = false,
            scoreRecorded = false,
            leakedShouldAddTile = false
        });
        seedResponse.EnsureSuccessStatusCode();

        GameStateResponse state = await client.GetFromJsonAsync<GameStateResponse>("/api/games/seeded-existing-game");

        Assert.NotNull(state);
        Assert.Equal(32, state.Score);
        Assert.Equal("2,4,.,.,.,.,.,.,.,.,.,16,.,.,.,.", GetBoardSignature(state));
    }

    [Fact]
    public async Task test_api_can_force_generated_tile_values_for_stable_api_tests()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        await using Game2048WebApplicationFactory factory = scope.CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage configureResponse = await client.PostAsJsonAsync("/api/test/generated-tile-value", new
        {
            value = "4"
        });
        configureResponse.EnsureSuccessStatusCode();

        for (int attempt = 0; attempt < 10; attempt++)
        {
            GameCreatedResponse created = await CreateGameAsync(client);
            List<string> generatedTileValues = created.State.Tiles
                .Where(tile => !string.IsNullOrEmpty(tile.Value))
                .Select(tile => tile.Value)
                .ToList();

            Assert.Equal(2, generatedTileValues.Count);
            Assert.All(generatedTileValues, value => Assert.Equal("4", value));
        }
    }

    private static void AssertEmptySlot(SaveSummaryResponse slot, string slotKey)
    {
        Assert.Equal(slotKey, slot.SlotKey);
        Assert.False(slot.HasData);
        Assert.Null(slot.Score);
        Assert.Null(slot.SavedAtUtc);
    }

    private static async Task<GameCreatedResponse> CreateGameAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsync("/api/games", content: null);
        response.EnsureSuccessStatusCode();
        GameCreatedResponse created = await response.Content.ReadFromJsonAsync<GameCreatedResponse>();
        return Assert.IsType<GameCreatedResponse>(created);
    }

    private static async Task<GameStateResponse> MoveAsync(HttpClient client, string gameId, string direction)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/games/{gameId}/move", new { direction });
        response.EnsureSuccessStatusCode();
        GameStateResponse state = await response.Content.ReadFromJsonAsync<GameStateResponse>();
        return Assert.IsType<GameStateResponse>(state);
    }

    private static string GetBoardSignature(GameStateResponse state)
    {
        return string.Join(",", state.Tiles
            .OrderBy(tile => tile.Y)
            .ThenBy(tile => tile.X)
            .Select(tile => string.IsNullOrEmpty(tile.Value) ? "." : tile.Value));
    }

    public class GameCreatedResponse
    {
        public string Id { get; set; } = string.Empty;
        public GameStateResponse State { get; set; } = new GameStateResponse();
    }

    public class GameStateResponse
    {
        public bool Win { get; set; }
        public bool Lose { get; set; }
        public bool RecordSaved { get; set; }
        public bool CanSaveRecord { get; set; }
        public int Score { get; set; }
        public List<TileResponse> Tiles { get; set; } = new List<TileResponse>();
    }

    public class TileResponse
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public class SaveSummaryResponse
    {
        public string SlotKey { get; set; } = string.Empty;
        public bool HasData { get; set; }
        public int? Score { get; set; }
        public DateTime? SavedAtUtc { get; set; }
    }
}
