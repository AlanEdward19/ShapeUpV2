using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Training.WorkoutPlans.CopyWorkoutPlan;
using ShapeUp.Features.Training.WorkoutPlans.CreateWorkoutPlan;
using ShapeUp.Features.Training.WorkoutPlans.GetWorkoutPlanById;
using ShapeUp.Features.Training.WorkoutPlans.GetWorkoutPlansByUser;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutPlans;

[ApiController]
[Route("api/training/workout-plans")]
public class WorkoutPlansController : ControllerBase
{
    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-plans:create" }])]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkoutPlanCommand command,
        [FromServices] CreateWorkoutPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), HttpContext.GetUserScopes(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { planId = success.PlanId }, success));
    }

    [HttpPost("{planId}/copy")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-plans:copy" }])]
    public async Task<IActionResult> Copy(
        string planId,
        [FromBody] CopyWorkoutPlanCommand command,
        [FromServices] CopyWorkoutPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { PlanId = planId }, HttpContext.GetUserId(), HttpContext.GetUserScopes(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { planId = success.PlanId }, success));
    }

    [HttpGet("{planId}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-plans:read" }])]
    public async Task<IActionResult> GetById(
        string planId,
        [FromServices] GetWorkoutPlanByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkoutPlanByIdQuery(planId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("user/{targetUserId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workout-plans:read" }])]
    public async Task<IActionResult> GetByUser(
        int targetUserId,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetWorkoutPlansByUserHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkoutPlansByUserQuery(targetUserId, cursor, pageSize), HttpContext.GetUserId(), HttpContext.GetUserScopes(), cancellationToken);
        return this.ToActionResult(result);
    }
}
