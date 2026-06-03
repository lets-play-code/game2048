using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Game2048.Web;

[ApiController]
[IgnoreAntiforgeryToken]
public class Game2048TestApiController : AbpControllerBase
{
    private readonly Game2048RuntimeService runtimeService;

    public Game2048TestApiController(Game2048RuntimeService runtimeService)
    {
        this.runtimeService = runtimeService;
    }

    [HttpPost("/api/test/games/next-id")]
    public IActionResult SetNextGameId([FromBody] ControlledGameIdRequest request)
    {
        try
        {
            runtimeService.EnqueueControlledGameId(request.Id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("/api/test/generated-tile-value")]
    public IActionResult ConfigureGeneratedTileValue([FromBody] GeneratedTileValueRequest request)
    {
        try
        {
            runtimeService.ConfigureGeneratedTileValue(request.Value);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("/api/test/shutdown")]
    public IActionResult Shutdown()
    {
        runtimeService.Shutdown();
        return NoContent();
    }

    [HttpPost("/api/test/games/clear-cache")]
    public IActionResult ClearCache()
    {
        runtimeService.ClearCache();
        return NoContent();
    }

    [HttpPost("/api/test/games/{id}")]
    public IActionResult SeedExistingGame(string id, [FromBody] SeedExistingGameRequest request)
    {
        try
        {
            runtimeService.SeedExistingGame(id, request);
            return NoContent();
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
