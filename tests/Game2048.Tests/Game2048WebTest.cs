using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
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

    [Fact]
    public async Task backend_logs_each_request_during_ui_flows()
    {
        using Game2048PersistenceScope scope = new Game2048PersistenceScope();
        TestLogSink logSink = new TestLogSink();
        await using WebApplicationFactory<Program> factory = scope.CreateFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(new TestLogCollectorLoggerProvider(logSink)));
        });
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage homeResponse = await client.GetAsync("/");
        homeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage createGameResponse = await client.PostAsync("/api/games", content: null);
        createGameResponse.EnsureSuccessStatusCode();

        IReadOnlyList<string> messages = logSink.Snapshot();
        Assert.Contains(messages, message => message.Contains("LegacyRequest GET / => 200"));
        Assert.Contains(messages, message => message.Contains("LegacyRequest POST /api/games => 200"));
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

internal sealed class TestLogSink
{
    private readonly List<string> messages = new();
    private readonly object gate = new();

    public void Add(string message)
    {
        lock (gate)
        {
            messages.Add(message);
        }
    }

    public IReadOnlyList<string> Snapshot()
    {
        lock (gate)
        {
            return messages.ToList();
        }
    }
}

internal sealed class TestLogCollectorLoggerProvider : ILoggerProvider
{
    private readonly TestLogSink sink;

    public TestLogCollectorLoggerProvider(TestLogSink sink)
    {
        this.sink = sink;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogCollectorLogger(categoryName, sink);
    }

    public void Dispose()
    {
    }
}

internal sealed class TestLogCollectorLogger : ILogger
{
    private readonly string categoryName;
    private readonly TestLogSink sink;

    public TestLogCollectorLogger(string categoryName, TestLogSink sink)
    {
        this.categoryName = categoryName;
        this.sink = sink;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NoOpDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        sink.Add($"{categoryName}: {formatter(state, exception)}");
    }
}

internal sealed class NoOpDisposable : IDisposable
{
    public static readonly NoOpDisposable Instance = new();

    private NoOpDisposable()
    {
    }

    public void Dispose()
    {
    }
}
