using ShapeUp.Features.Training.Workouts.CompleteWorkoutSession;
using ShapeUp.Features.Training.Workouts.CreateWorkoutSession;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionById;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionsByUser;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts;

[ApiController]
[Route("api/training/workouts")]
public class WorkoutsController : ControllerBase
{
    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workouts:create" }])]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkoutSessionCommand command,
        [FromServices] CreateWorkoutSessionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), HttpContext.GetUserScopes(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { sessionId = success.SessionId }, success));
    }

    [HttpPost("{sessionId}/complete")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workouts:complete" }])]
    public async Task<IActionResult> Complete(
        string sessionId,
        [FromBody] CompleteWorkoutSessionCommand command,
        [FromServices] CompleteWorkoutSessionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { SessionId = sessionId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{sessionId}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workouts:read" }])]
    public async Task<IActionResult> GetById(
        string sessionId,
        [FromServices] GetWorkoutSessionByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWorkoutSessionByIdQuery(sessionId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("user/{targetUserId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:workouts:read" }])]
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
}

