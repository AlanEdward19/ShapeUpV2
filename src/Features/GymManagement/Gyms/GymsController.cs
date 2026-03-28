namespace ShapeUp.Features.GymManagement.Gyms;

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
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:read" }])]
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
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:update" }])]
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
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:delete" }])]
    public async Task<IActionResult> Delete(
        int gymId,
        [FromServices] DeleteGymHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGymCommand(gymId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
