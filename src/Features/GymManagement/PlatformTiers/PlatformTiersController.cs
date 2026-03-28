namespace ShapeUp.Features.GymManagement.PlatformTiers;

using Microsoft.AspNetCore.Mvc;
using CreatePlatformTier;
using DeletePlatformTier;
using GetPlatformTiers;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using UpdatePlatformTier;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/platform-tiers")]
public class PlatformTiersController : ControllerBase
{
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:platform_tiers:read" }])]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] GetPlatformTiersHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPlatformTiersQuery(cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:platform_tiers:create" }])]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlatformTierCommand command,
        [FromServices] CreatePlatformTierHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { id = success.Id }, success));
    }

    [HttpPut("{id:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:platform_tiers:update" }])]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdatePlatformTierCommand command,
        [FromServices] UpdatePlatformTierHandler handler,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return this.ToActionResult(Result<UpdatePlatformTierResponse>.Failure(CommonErrors.Validation("Route id must match body id.")));

        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:platform_tiers:delete" }])]
    public async Task<IActionResult> Delete(
        int id,
        [FromServices] DeletePlatformTierHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePlatformTierCommand(id), cancellationToken);
        return this.ToActionResult(result);
    }
}

