using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Training.ExerciseRequests.ApproveExerciseRequest;
using ShapeUp.Features.Training.ExerciseRequests.CreateExerciseRequest;
using ShapeUp.Features.Training.ExerciseRequests.GetExerciseRequestById;
using ShapeUp.Features.Training.ExerciseRequests.ListExerciseRequests;
using ShapeUp.Features.Training.ExerciseRequests.RejectExerciseRequest;
using ShapeUp.Features.Training.ExerciseRequests.Shared;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.ExerciseRequests;

/// <summary>Users ask for exercises missing from the library; platform admins review them and create the exercise from the request.</summary>
[ApiController]
[Route("api/training/exercise-requests")]
public class ExerciseRequestsController : ControllerBase
{
    private const string ManagePolicy = "capability:platform.exercises.manage";

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateExerciseRequestCommand command,
        [FromServices] CreateExerciseRequestHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { requestId = success.Id }, success));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] ListExerciseRequestsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(
            new ListExerciseRequestsQuery(null, cursor, pageSize), HttpContext.GetUserId(), ownerOnly: true, cancellationToken));

    [HttpGet]
    [Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] ListExerciseRequestsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(
            new ListExerciseRequestsQuery(status, cursor, pageSize), HttpContext.GetUserId(), ownerOnly: false, cancellationToken));

    [HttpGet("{requestId:int}")]
    public async Task<IActionResult> GetById(
        int requestId,
        [FromServices] GetExerciseRequestByIdHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(new GetExerciseRequestByIdQuery(requestId), HttpContext.GetUserId(), cancellationToken));

    [HttpPost("{requestId:int}/approve")]
    [Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Approve(
        int requestId,
        [FromBody] ApproveExerciseRequestBody body,
        [FromServices] ApproveExerciseRequestHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(
            new ApproveExerciseRequestCommand(requestId, body), HttpContext.GetUserId(), cancellationToken));

    [HttpPost("{requestId:int}/reject")]
    [Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Reject(
        int requestId,
        [FromBody] RejectExerciseRequestBody body,
        [FromServices] RejectExerciseRequestHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(
            new RejectExerciseRequestCommand(requestId, body.Reason), HttpContext.GetUserId(), cancellationToken));
}
