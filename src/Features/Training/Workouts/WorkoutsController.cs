using ShapeUp.Features.Training.Workouts.CancelWorkoutSession;
using ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;
using ShapeUp.Features.Training.Workouts.GetLatestCompletedWorkoutSessionByPlanId;
using ShapeUp.Features.Training.Workouts.GetMyActiveWorkoutSession;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionById;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionsByUser;
using ShapeUp.Features.Training.Workouts.StartWorkoutExecution;
using ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts;

[ApiController]
[Route("api/training/workouts")]
public class WorkoutsController : ControllerBase
{

    [HttpPost("start")]
    public async Task<IActionResult> Start(
        [FromBody] StartWorkoutExecutionCommand command,
        [FromServices] StartWorkoutExecutionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { sessionId = success.SessionId }, success));
    }

    [HttpPut("{sessionId}/state")]
    public async Task<IActionResult> SaveState(
        string sessionId,
        [FromBody] UpdateWorkoutExecutionStateCommand command,
        [FromServices] UpdateWorkoutExecutionStateHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { SessionId = sessionId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{sessionId}/finish")]
    public async Task<IActionResult> Finish(
        string sessionId,
        [FromBody] FinishWorkoutExecutionCommand command,
        [FromServices] FinishWorkoutExecutionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { SessionId = sessionId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{sessionId}/cancel")]
    public async Task<IActionResult> Cancel(
        string sessionId,
        [FromServices] CancelWorkoutSessionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CancelWorkoutSessionCommand(sessionId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetById(
        string sessionId,
        [FromServices] GetWorkoutSessionByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkoutSessionByIdQuery(sessionId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("user/{targetUserId:int}")]
    public async Task<IActionResult> GetByUser(
        int targetUserId,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetWorkoutSessionsByUserHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkoutSessionsByUserQuery(targetUserId, cursor, pageSize), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("plan/{workoutPlanId}/latest")]
    public async Task<IActionResult> GetLatestByWorkoutPlanId(
        string workoutPlanId,
        [FromServices] GetLatestCompletedWorkoutSessionByPlanIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLatestCompletedWorkoutSessionByPlanIdQuery(workoutPlanId),
            HttpContext.GetUserId(),
            cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("me/active")]
    public async Task<IActionResult> GetMyActive(
        [FromServices] GetMyActiveWorkoutSessionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
