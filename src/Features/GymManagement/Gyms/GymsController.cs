namespace ShapeUp.Features.GymManagement.Gyms;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    // Resolved (AD-006 follow-up): neither GetAll nor Create fits [Authorize(Policy=...)] the way
    // GetById/Update/Delete do -- there's no {gymId} to check membership against (GetAll lists
    // across gyms, Create makes a brand-new one) and neither is a platform-admin action either.
    // GetAll is a public directory read (PRD sec. 54, gym discovery/map) -- any authenticated user
    // browses it, same category as ExercisesController.GetAll.
    [HttpGet]
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

    // Self-scoped by construction: CreateGymHandler always assigns HttpContext.GetUserId() as the
    // new gym's OwnerId and grants that same user the GymOwner role -- there is no other actor this
    // could ever apply to, same category as WeightTrackingController.
    [HttpPost]
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
