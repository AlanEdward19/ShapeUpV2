namespace ShapeUp.Features.Gamification.GetGamificationProfile;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
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
}
