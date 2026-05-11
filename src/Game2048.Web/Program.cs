using System.Collections.Concurrent;
using System.Text.Json;
using Game2048Model = Game2048.Game.Game2048;
using Game2048StateModel = Game2048.Game.Game2048State;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

string databasePath = builder.Configuration["Game2048:DatabasePath"];
if (string.IsNullOrWhiteSpace(databasePath))
{
    databasePath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "game2048.db");
}

string databaseDirectory = Path.GetDirectoryName(databasePath) ?? builder.Environment.ContentRootPath;
Directory.CreateDirectory(databaseDirectory);
Game2048Model.ConfigurePersistence(databasePath);
Game2048Model.EnsureDatabaseReady();

var app = builder.Build();
var games = new ConcurrentDictionary<string, Game2048Model>();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/leaderboard", () => Results.Ok(Game2048Model.getLeaderboardEntries()));
app.MapGet("/api/saves", () => Results.Ok(Game2048Model.getSaveSummaries()));

app.MapPost("/api/games", () =>
{
    string id = Guid.NewGuid().ToString("N");
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

app.MapFallbackToFile("index.html");

app.Run();

public record MoveRequest(string Direction);
public record SaveLeaderboardRecordRequest(string PlayerName);
public record ErrorResponse(string Error);
public record GameCreatedResponse(string Id, Game2048StateModel State);
public partial class Program { }
