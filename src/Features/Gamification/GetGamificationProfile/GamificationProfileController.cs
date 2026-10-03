namespace ShapeUp.Features.Gamification.GetGamificationProfile;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Gamification.GetWorkoutXp;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gamification")]
public class GamificationProfileController : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(
        [FromServices] GetGamificationProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetGamificationProfileQuery(HttpContext.GetUserId()),
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("me/workout-xp")]
    public async Task<IActionResult> GetMyWorkoutXp(
        [FromQuery] string[] sessionIds,
        [FromServices] GetWorkoutXpHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetWorkoutXpQuery(HttpContext.GetUserId(), sessionIds),
            cancellationToken);

        return this.ToActionResult(result);
    }
}
