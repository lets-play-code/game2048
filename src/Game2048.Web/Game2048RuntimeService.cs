using System.Collections.Concurrent;
using Game2048Model = Game2048.Game.Game2048;
using Game2048StateModel = Game2048.Game.Game2048State;
using LeaderboardEntryModel = Game2048.Game.LeaderboardEntry;
using SaveGameSummaryModel = Game2048.Game.SaveGameSummary;
using Volo.Abp.DependencyInjection;

namespace Game2048.Web;

public class Game2048RuntimeService : ISingletonDependency
{
    private readonly ConcurrentDictionary<string, Game2048Model> games = new();
    private readonly ConcurrentQueue<string> controlledGameIds = new();
    private readonly IHostApplicationLifetime appLifetime;

    public Game2048RuntimeService(IHostApplicationLifetime appLifetime)
    {
        this.appLifetime = appLifetime;
    }

    public List<LeaderboardEntryModel> GetLeaderboardEntries()
    {
        return Game2048Model.getLeaderboardEntries();
    }

    public List<SaveGameSummaryModel> GetSaveSummaries()
    {
        return Game2048Model.getSaveSummaries();
    }

    public GameCreatedResponse CreateGame()
    {
        string id = controlledGameIds.TryDequeue(out string controlledId)
            ? controlledId
            : Guid.NewGuid().ToString("N");
        Game2048Model game2048 = new Game2048Model();
        games[id] = game2048;
        return new GameCreatedResponse(id, game2048.getGameState());
    }

    public Game2048StateModel GetGameState(string id)
    {
        Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
        lock (game2048)
        {
            return game2048.getGameState();
        }
    }

    public Game2048StateModel MoveGame(string id, string direction)
    {
        Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
        lock (game2048)
        {
            game2048.keyPressed(direction);
            game2048.saveGame("auto");
            return game2048.getGameState();
        }
    }

    public Game2048StateModel ResetGame(string id)
    {
        Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
        lock (game2048)
        {
            game2048.resetGame();
            return game2048.getGameState();
        }
    }

    public Game2048StateModel SaveGame(string id, string slotKey)
    {
        Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
        lock (game2048)
        {
            game2048.saveGame(slotKey);
            return game2048.getGameState();
        }
    }

    public Game2048StateModel LoadGame(string id, string slotKey)
    {
        Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
        lock (game2048)
        {
            game2048.loadGame(slotKey);
            return game2048.getGameState();
        }
    }

    public Game2048StateModel SaveLeaderboardRecord(string id, string playerName)
    {
        Game2048Model game2048 = games.GetOrAdd(id, _ => new Game2048Model());
        lock (game2048)
        {
            game2048.saveLeaderboardRecord(playerName);
            return game2048.getGameState();
        }
    }

    public void EnqueueControlledGameId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Game id is required.", nameof(id));
        }

        controlledGameIds.Enqueue(id);
    }

    public void ConfigureGeneratedTileValue(string value)
    {
        Game2048Model.ConfigureGeneratedTileValue(value);
    }

    public void Shutdown()
    {
        Task.Run(appLifetime.StopApplication);
    }

    public void ClearCache()
    {
        games.Clear();
        controlledGameIds.Clear();
    }

    public void SeedExistingGame(string id, SeedExistingGameRequest request)
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
        }
    }
}
