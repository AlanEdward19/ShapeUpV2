namespace ShapeUp.Features.GymManagement.PlatformTiers;

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using CreatePlatformTier;
using DeletePlatformTier;
using GetPlatformTiers;
using UpdatePlatformTier;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/platform-tiers")]
public class PlatformTiersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] GetPlatformTiersHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPlatformTiersQuery(cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlatformTierCommand command,
        [FromServices] CreatePlatformTierHandler handler,
        [FromServices] IValidator<CreatePlatformTierCommand> validator,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { id = success.Id }, success));
    }

    [HttpPut("{id:int}")]
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
    public async Task<IActionResult> Delete(
        int id,
        [FromServices] DeletePlatformTierHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePlatformTierCommand(id), cancellationToken);
        return this.ToActionResult(result);
    }
}

