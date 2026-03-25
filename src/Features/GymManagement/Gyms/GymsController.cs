namespace ShapeUp.Features.GymManagement.Gyms;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.GymManagement.Gyms.CreateGym;
using ShapeUp.Features.GymManagement.Gyms.GetGyms;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/gyms")]
public class GymsController : ControllerBase
{
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
    public async Task<IActionResult> GetById(
        int gymId,
        [FromServices] GetGymsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.GetByIdAsync(gymId, cancellationToken);
        return this.ToActionResult(result);
    }

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
}
