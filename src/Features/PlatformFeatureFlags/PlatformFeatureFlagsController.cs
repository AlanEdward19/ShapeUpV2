namespace ShapeUp.Features.PlatformFeatureFlags;

using GetFeatureFlags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SetFeatureFlag;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/platform/feature-flags")]
public class PlatformFeatureFlagsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "capability:platform.feature_flags.manage")]
    public async Task<IActionResult> GetAll(
        [FromServices] GetFeatureFlagsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{key}")]
    [Authorize(Policy = "capability:platform.feature_flags.manage")]
    public async Task<IActionResult> Set(
        string key,
        [FromBody] SetFeatureFlagRequest request,
        [FromServices] SetFeatureFlagHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var result = await handler.HandleAsync(
            new SetFeatureFlagCommand(key, request.Enabled, userId),
            cancellationToken);
        return this.ToActionResult(result);
    }
}

public sealed record SetFeatureFlagRequest(bool Enabled);
