namespace ShapeUp.Features.GymManagement.Gyms;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using CreateGym;
using DeleteGym;
using GetGyms;
using UpdateGym;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/gyms")]
public class GymsController : ControllerBase
{
    // SPEC_DEVIATION (AD-003-adjacent, T8): GetAll and Create are NOT migrated to
    // [Authorize(Policy="capability:...")] -- CapabilityResolver only grants via membership
    // (needs a {gymId} route value), relationship, credential, or entitlement, and denies by
    // default (AUTHZ-05). Neither endpoint has an existing gym to check membership against
    // (GetAll lists across gyms, Create makes a brand-new one), so no source would ever grant
    // access and every caller would be denied. Deciding who may list/create gyms without gym
    // context is a product decision out of scope for this mechanical controller migration --
    // both endpoints stay on the legacy RequireScopesAttribute until that decision is made.
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:read" }])]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor, [FromQuery] int? pageSize, [FromQuery] int? ownerId,
        [FromServices] GetGymsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGymsQuery(cursor, pageSize, ownerId), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{gymId:int}")]
    [Authorize(Policy = "capability:gym.read")]
    public async Task<IActionResult> GetById(
        int gymId,
        [FromServices] GetGymsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.GetByIdAsync(gymId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:create" }])]
    public async Task<IActionResult> Create(
        [FromBody] CreateGymCommand command,
        [FromServices] CreateGymHandler handler,
        CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        var result = await handler.HandleAsync(command, currentUserId, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { gymId = success.Id }, success));
    }

    [HttpPut("{gymId:int}")]
    [Authorize(Policy = "capability:gym.update")]
    public async Task<IActionResult> Update(
        int gymId,
        [FromBody] UpdateGymCommand command,
        [FromServices] UpdateGymHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { GymId = gymId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{gymId:int}")]
    [Authorize(Policy = "capability:gym.delete")]
    public async Task<IActionResult> Delete(
        int gymId,
        [FromServices] DeleteGymHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGymCommand(gymId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
