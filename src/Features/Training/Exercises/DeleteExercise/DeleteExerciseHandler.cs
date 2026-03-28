using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.DeleteExercise;

public class DeleteExerciseHandler(IExerciseRepository exerciseRepository)
{
    public async Task<Result> HandleAsync(DeleteExerciseCommand command, CancellationToken cancellationToken)
    {
        var exercise = await exerciseRepository.GetByIdAsync(command.ExerciseId, cancellationToken);
        if (exercise is null)
            return Result.Failure(TrainingErrors.ExerciseNotFound(command.ExerciseId));

        await exerciseRepository.DeleteAsync(exercise, cancellationToken);
        return Result.Success();
    }
}