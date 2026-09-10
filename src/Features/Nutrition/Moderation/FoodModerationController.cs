using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Moderation.DecideModeration;
using ShapeUp.Features.Nutrition.Moderation.GetPendingModerations;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Moderation;

[ApiController]
[Route("api/nutrition/food-moderation")]
public class FoodModerationController : ControllerBase
{
    [HttpGet("pending")]
    [Authorize(Policy = "capability:platform.nutrition_foods.moderate")]
    public async Task<IActionResult> GetPending(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetPendingModerationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPendingModerationsQuery(cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{requestId}/decide")]
    [Authorize(Policy = "capability:platform.nutrition_foods.moderate")]
    public async Task<IActionResult> Decide(
        string requestId,
        [FromBody] DecideModerationBody body,
        [FromServices] DecideModerationHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new DecideModerationCommand(requestId, body.Decision),
            HttpContext.GetUserId(),
            cancellationToken);
        return this.ToActionResult(result);
    }

    public record DecideModerationBody(string Decision);
}
