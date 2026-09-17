using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.GetExerciseEquivalents;

public class GetExerciseEquivalentsHandler(
    IExerciseRepository exerciseRepository,
    IExerciseEquivalentRepository equivalentRepository)
{
    public async Task<Result<ExerciseResponse[]>> HandleAsync(
        GetExerciseEquivalentsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ExerciseId <= 0)
            return Result<ExerciseResponse[]>.Failure(CommonErrors.Validation("ExerciseId must be greater than zero."));

        var exercise = await exerciseRepository.GetByIdAsync(query.ExerciseId, cancellationToken);
        if (exercise is null)
            return Result<ExerciseResponse[]>.Failure(TrainingErrors.ExerciseNotFound(query.ExerciseId));

        var equivalents = await equivalentRepository.GetEquivalentsAsync(query.ExerciseId, cancellationToken);
        return Result<ExerciseResponse[]>.Success(equivalents.Select(CreateExerciseHandler.MapResponse).ToArray());
    }
}
