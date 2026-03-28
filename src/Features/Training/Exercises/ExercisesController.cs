using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.DeleteExercise;
using ShapeUp.Features.Training.Exercises.GetExerciseById;
using ShapeUp.Features.Training.Exercises.GetExercises;
using ShapeUp.Features.Training.Exercises.SuggestExercise;
using ShapeUp.Features.Training.Exercises.UpdateExercise;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises;

[ApiController]
[Route("api/training/exercises")]
public class ExercisesController : ControllerBase
{
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:exercises:read" }])]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetExercisesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetExercisesQuery(cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{exerciseId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:exercises:read" }])]
    public async Task<IActionResult> GetById(
        int exerciseId,
        [FromServices] GetExerciseByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetExerciseByIdQuery(exerciseId), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:exercises:create" }])]
    public async Task<IActionResult> Create(
        [FromBody] CreateExerciseCommand command,
        [FromServices] CreateExerciseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { exerciseId = success.Id }, success));
    }

    [HttpPut("{exerciseId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:exercises:update" }])]
    public async Task<IActionResult> Update(
        int exerciseId,
        [FromBody] UpdateExerciseCommand command,
        [FromServices] UpdateExerciseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { ExerciseId = exerciseId }, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{exerciseId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:exercises:delete" }])]
    public async Task<IActionResult> Delete(
        int exerciseId,
        [FromServices] DeleteExerciseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteExerciseCommand(exerciseId), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("suggest")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:exercises:suggest" }])]
    public async Task<IActionResult> Suggest(
        [FromBody] SuggestExercisesQuery query,
        [FromServices] SuggestExercisesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }
}

