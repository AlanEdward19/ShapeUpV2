using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Training.Muscles.CreateMuscle;
using ShapeUp.Features.Training.Muscles.DeleteMuscle;
using ShapeUp.Features.Training.Muscles.GetMuscleById;
using ShapeUp.Features.Training.Muscles.GetMuscles;
using ShapeUp.Features.Training.Muscles.UpdateMuscle;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Muscles;

[ApiController]
[Route("api/training/muscles")]
public class MusclesController : ControllerBase
{
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:muscles:read" }])]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetMusclesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMusclesQuery(cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{muscleId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:muscles:read" }])]
    public async Task<IActionResult> GetById(
        int muscleId,
        [FromServices] GetMuscleByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMuscleByIdQuery(muscleId), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:muscles:create" }])]
    public async Task<IActionResult> Create(
        [FromBody] CreateMuscleCommand command,
        [FromServices] CreateMuscleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { muscleId = success.Id }, success));
    }

    [HttpPut("{muscleId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:muscles:update" }])]
    public async Task<IActionResult> Update(
        int muscleId,
        [FromBody] UpdateMuscleCommand command,
        [FromServices] UpdateMuscleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { MuscleId = muscleId }, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{muscleId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:muscles:delete" }])]
    public async Task<IActionResult> Delete(
        int muscleId,
        [FromServices] DeleteMuscleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteMuscleCommand(muscleId), cancellationToken);
        return this.ToActionResult(result);
    }
}

