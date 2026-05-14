using System.Collections.Concurrent;
using System.Text.Json;
using Game2048Model = Game2048.Game.Game2048;
using Game2048StateModel = Game2048.Game.Game2048State;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

string connectionString = builder.Configuration["Game2048:ConnectionString"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = Game2048Model.GetDefaultConnectionString();
}

Game2048Model.ConfigurePersistence(connectionString);
Game2048Model.EnsureDatabaseReady();
Game2048Model.ConfigureGeneratedTileValue(builder.Configuration["Game2048:ForcedGeneratedTileValue"]);
Game2048Model.ConfigureLeaderboardWallUrl(builder.Configuration["Game2048:LeaderboardWallUrl"]);

var app = builder.Build();
var games = new ConcurrentDictionary<string, Game2048Model>();
var controlledGameIds = new ConcurrentQueue<string>();
bool enableTestApi = builder.Configuration.GetValue<bool>("Game2048:EnableTestApi");

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/leaderboard", () => Results.Ok(Game2048Model.getLeaderboardEntries()));
app.MapGet("/api/saves", () => Results.Ok(Game2048Model.getSaveSummaries()));

app.MapPost("/api/games", () =>
{
    string id = controlledGameIds.TryDequeue(out string controlledId)
        ? controlledId
        : Guid.NewGuid().ToString("N");
    Game2048Model game2048 = new Game2048Model();
    games[id] = game2048;
    return Results.Ok(new GameCreatedResponse(id, game2048.getGameState()));
});

app.MapGet("/api/games/{id}", (string id) =>
{
    Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
    lock (game2048)
    {
        return Results.Ok(game2048.getGameState());
    }
});

app.MapPost("/api/games/{id}/move", (string id, MoveRequest request) =>
{
    Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
    lock (game2048)
    {
        game2048.keyPressed(request.Direction);
        game2048.saveGame("auto");
        return Results.Ok(game2048.getGameState());
    }
});

app.MapPost("/api/games/{id}/reset", (string id) =>
{
    Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
    lock (game2048)
    {
        game2048.resetGame();
        return Results.Ok(game2048.getGameState());
    }
});

app.MapPost("/api/games/{id}/save/{slotKey}", (string id, string slotKey) =>
{
    Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
    lock (game2048)
    {
        try
        {
            game2048.saveGame(slotKey);
            return Results.Ok(game2048.getGameState());
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }
});

app.MapPost("/api/games/{id}/load/{slotKey}", (string id, string slotKey) =>
{
    Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
    lock (game2048)
    {
        try
        {
            game2048.loadGame(slotKey);
            return Results.Ok(game2048.getGameState());
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }
});

app.MapPost("/api/games/{id}/leaderboard", (string id, SaveLeaderboardRecordRequest request) =>
{
    Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
    lock (game2048)
    {
        try
        {
            game2048.saveLeaderboardRecord(request.PlayerName);
            return Results.Ok(game2048.getGameState());
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    }
});

if (enableTestApi)
{
    app.MapPost("/api/test/games/next-id", (ControlledGameIdRequest request) =>
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return Results.BadRequest(new ErrorResponse("Game id is required."));
        }

        controlledGameIds.Enqueue(request.Id);
        return Results.NoContent();
    });

    app.MapPost("/api/test/generated-tile-value", (GeneratedTileValueRequest request) =>
    {
        try
        {
            Game2048Model.ConfigureGeneratedTileValue(request.Value);
            return Results.NoContent();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    });

    app.MapPost("/api/test/games/clear-cache", () =>
    {
        games.Clear();
        controlledGameIds.Clear();
        return Results.NoContent();
    });

    app.MapPost("/api/test/games/{id}", (string id, SeedExistingGameRequest request) =>
    {
        try
        {
            Game2048Model game2048 = new Game2048Model();
            lock (game2048)
            {
                game2048.restoreForTesting(
                    request.BoardJson,
                    request.Score,
                    request.Win,
                    request.Lose,
                    request.ScoreRecorded,
                    request.LeakedShouldAddTile);
                games[id] = game2048;
                return Results.NoContent();
            }
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ErrorResponse(ex.Message));
        }
    });
}

app.MapFallbackToFile("index.html");

app.Run();

public record MoveRequest(string Direction);
public record SaveLeaderboardRecordRequest(string PlayerName);
public record ControlledGameIdRequest(string Id);
public record GeneratedTileValueRequest(string Value);
public record SeedExistingGameRequest(string BoardJson, int Score, bool Win, bool Lose, bool ScoreRecorded, bool LeakedShouldAddTile);
public record ErrorResponse(string Error);
public record GameCreatedResponse(string Id, Game2048StateModel State);
public partial class Program { }
