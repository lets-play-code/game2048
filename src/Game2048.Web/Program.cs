using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Hosting;
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
string listeningUrlFilePath = builder.Configuration["Game2048:PortFilePath"];

string databaseDirectory = Path.GetDirectoryName(databasePath) ?? builder.Environment.ContentRootPath;
Directory.CreateDirectory(databaseDirectory);
Game2048Model.ConfigurePersistence(databasePath);
Game2048Model.EnsureDatabaseReady();
Game2048Model.ConfigureGeneratedTileValue(builder.Configuration["Game2048:ForcedGeneratedTileValue"]);
Game2048Model.ConfigureLeaderboardWallUrl(builder.Configuration["Game2048:LeaderboardWallUrl"]);

var app = builder.Build();
var games = new ConcurrentDictionary<string, Game2048Model>();
bool enableTestApi = builder.Configuration.GetValue<bool>("Game2048:EnableTestApi");

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

if (enableTestApi)
{
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

    app.MapPost("/api/test/shutdown", (IHostApplicationLifetime applicationLifetime) =>
    {
        applicationLifetime.StopApplication();
        return Results.NoContent();
    });
}

app.MapFallbackToFile("index.html");

if (string.IsNullOrWhiteSpace(listeningUrlFilePath))
{
    app.Run();
}
else
{
    string listeningUrlDirectory = Path.GetDirectoryName(listeningUrlFilePath) ?? builder.Environment.ContentRootPath;
    Directory.CreateDirectory(listeningUrlDirectory);
    if (File.Exists(listeningUrlFilePath))
    {
        File.Delete(listeningUrlFilePath);
    }

    await app.StartAsync();
    await File.WriteAllTextAsync(listeningUrlFilePath, Program.GetListeningUrl(app));
    await app.WaitForShutdownAsync();
}

public record MoveRequest(string Direction);
public record SaveLeaderboardRecordRequest(string PlayerName);
public record GeneratedTileValueRequest(string Value);
public record SeedExistingGameRequest(string BoardJson, int Score, bool Win, bool Lose, bool ScoreRecorded, bool LeakedShouldAddTile);
public record ErrorResponse(string Error);
public record GameCreatedResponse(string Id, Game2048StateModel State);
public partial class Program
{
    internal static string GetListeningUrl(WebApplication app)
    {
        IServer server = app.Services.GetRequiredService<IServer>();
        IServerAddressesFeature addressesFeature = server.Features.Get<IServerAddressesFeature>();
        string listeningUrl = addressesFeature?.Addresses.FirstOrDefault(address => !address.EndsWith(":0", StringComparison.Ordinal))
            ?? app.Urls.FirstOrDefault(address => !address.EndsWith(":0", StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(listeningUrl))
        {
            throw new InvalidOperationException("The web app did not publish a listening URL.");
        }

        return listeningUrl;
    }
}
