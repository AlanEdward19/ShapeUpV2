namespace ShapeUp.Features.GymManagement.TrainerPlans;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using CreateTrainerPlan;
using DeleteTrainerPlan;
using GetTrainerPlans;
using UpdateTrainerPlan;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/trainers/{trainerId:int}/plans")]
public class TrainerPlansController : ControllerBase
{
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:trainer_plans:read" }])]
    public async Task<IActionResult> GetAll(int trainerId, [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] GetTrainerPlansHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTrainerPlansQuery(trainerId, cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:trainer_plans:create" }])]
    public async Task<IActionResult> Create(int trainerId, [FromBody] CreateTrainerPlanCommand command,
        [FromServices] CreateTrainerPlanHandler handler, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        if (trainerId != currentUserId)
            return this.ToActionResult(Result<CreateTrainerPlanResponse>.Failure(CommonErrors.Forbidden("You can only create plans for yourself.")));
        var result = await handler.HandleAsync(command, trainerId, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { trainerId }, success));
    }

    [HttpPut("{planId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:trainer_plans:update" }])]
    public async Task<IActionResult> Update(int trainerId, int planId, [FromBody] UpdateTrainerPlanCommand command,
        [FromServices] UpdateTrainerPlanHandler handler, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        if (trainerId != currentUserId)
            return this.ToActionResult(Result<UpdateTrainerPlanResponse>.Failure(CommonErrors.Forbidden("You can only update your own plans.")));
        var result = await handler.HandleAsync(command with { PlanId = planId }, trainerId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{planId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:trainer_plans:delete" }])]
    public async Task<IActionResult> Delete(int trainerId, int planId,
        [FromServices] DeleteTrainerPlanHandler handler, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        if (trainerId != currentUserId)
            return this.ToActionResult(Result.Failure(CommonErrors.Forbidden("You can only delete your own plans.")));
        var result = await handler.HandleAsync(new DeleteTrainerPlanCommand(planId, trainerId), cancellationToken);
        return this.ToActionResult(result);
    }
}

