using Game2048.Game;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Game2048.Web;

[ApiController]
[IgnoreAntiforgeryToken]
public class Game2048ApiController : AbpControllerBase
{
    private readonly Game2048RuntimeService runtimeService;

    public Game2048ApiController(Game2048RuntimeService runtimeService)
    {
        this.runtimeService = runtimeService;
    }

    [HttpGet("/api/leaderboard")]
    public ActionResult<List<LeaderboardEntry>> GetLeaderboard()
    {
        return Ok(runtimeService.GetLeaderboardEntries());
    }

    [HttpGet("/api/saves")]
    public ActionResult<List<SaveGameSummary>> GetSaves()
    {
        return Ok(runtimeService.GetSaveSummaries());
    }

    [HttpPost("/api/games")]
    public ActionResult<GameCreatedResponse> CreateGame()
    {
        return Ok(runtimeService.CreateGame());
    }

    [HttpGet("/api/games/{id}")]
    public ActionResult<Game2048State> GetGame(string id)
    {
        return Ok(runtimeService.GetGameState(id));
    }

    [HttpPost("/api/games/{id}/move")]
    public ActionResult<Game2048State> MoveGame(string id, [FromBody] MoveRequest request)
    {
        return Ok(runtimeService.MoveGame(id, request.Direction));
    }

    [HttpPost("/api/games/{id}/reset")]
    public ActionResult<Game2048State> ResetGame(string id)
    {
        return Ok(runtimeService.ResetGame(id));
    }

    [HttpPost("/api/games/{id}/save/{slotKey}")]
    public ActionResult<Game2048State> SaveGame(string id, string slotKey)
    {
        try
        {
            return Ok(runtimeService.SaveGame(id, slotKey));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("/api/games/{id}/load/{slotKey}")]
    public ActionResult<Game2048State> LoadGame(string id, string slotKey)
    {
        try
        {
            return Ok(runtimeService.LoadGame(id, slotKey));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("/api/games/{id}/leaderboard")]
    public ActionResult<Game2048State> SaveLeaderboardRecord(string id, [FromBody] SaveLeaderboardRecordRequest request)
    {
        try
        {
            return Ok(runtimeService.SaveLeaderboardRecord(id, request.PlayerName));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }
}
