namespace ShapeUp.Features.GymManagement.GymPlans;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using CreateGymPlan;
using DeleteGymPlan;
using GetGymPlans;
using UpdateGymPlan;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/gyms/{gymId:int}/plans")]
public class GymPlansController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(int gymId, [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] GetGymPlansHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGymPlansQuery(gymId, cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int gymId, [FromBody] CreateGymPlanCommand command,
        [FromServices] CreateGymPlanHandler handler, CancellationToken cancellationToken)
    {
        if (gymId != command.GymId)
            return this.ToActionResult(Result<CreateGymPlanResponse>.Failure(CommonErrors.Validation("Route gymId must match body gymId.")));
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { gymId }, success));
    }

    [HttpPut("{planId:int}")]
    public async Task<IActionResult> Update(int gymId, int planId, [FromBody] UpdateGymPlanCommand command,
        [FromServices] UpdateGymPlanHandler handler, CancellationToken cancellationToken)
    {
        if (gymId != command.GymId || planId != command.PlanId)
            return this.ToActionResult(Result<UpdateGymPlanResponse>.Failure(CommonErrors.Validation("Route ids must match body ids.")));
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{planId:int}")]
    public async Task<IActionResult> Delete(int gymId, int planId,
        [FromServices] DeleteGymPlanHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteGymPlanCommand(gymId, planId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}

