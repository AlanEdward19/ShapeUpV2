namespace ShapeUp.Features.Gamification.GetRanking;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gamification/ranking")]
public class GamificationRankingController : ControllerBase
{
    // Public directory read for any authenticated user — same category as GymsController.GetAll.
    [HttpGet]
    public async Task<IActionResult> GetRanking(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetRankingHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetRankingQuery(cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }
}
