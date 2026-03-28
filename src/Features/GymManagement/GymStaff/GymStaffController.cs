namespace ShapeUp.Features.GymManagement.GymStaff;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using AddGymStaff;
using GetGymStaff;
using RemoveGymStaff;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/gyms/{gymId:int}/staff")]
public class GymStaffController : ControllerBase
{
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:staff:read" }])]
    public async Task<IActionResult> GetAll(int gymId, [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] GetGymStaffHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGymStaffQuery(gymId, cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:staff:create" }])]
    public async Task<IActionResult> Add(int gymId, [FromBody] AddGymStaffCommand command,
        [FromServices] AddGymStaffHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { GymId = gymId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { gymId }, success));
    }

    [HttpDelete("{staffId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:staff:delete" }])]
    public async Task<IActionResult> Remove(int gymId, int staffId,
        [FromServices] RemoveGymStaffHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RemoveGymStaffCommand(gymId, staffId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
