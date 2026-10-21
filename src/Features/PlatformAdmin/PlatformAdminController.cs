using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.PlatformAdmin.GetOverview;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.PlatformAdmin;

[ApiController]
[Route("api/admin")]
public class PlatformAdminController : ControllerBase
{
    [HttpGet("overview")]
    [Authorize(Policy = "capability:platform.metrics.read")]
    public async Task<IActionResult> Overview(
        [FromServices] GetAdminOverviewHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(new GetAdminOverviewQuery(), cancellationToken));
}
