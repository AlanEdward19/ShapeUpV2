using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.GetExerciseById;

public class GetExerciseByIdHandler(IExerciseRepository exerciseRepository)
{
    public async Task<Result<ExerciseResponse>> HandleAsync(GetExerciseByIdQuery query, CancellationToken cancellationToken)
    {
        var exercise = await exerciseRepository.GetByIdAsync(query.ExerciseId, cancellationToken);
        if (exercise is null)
            return Result<ExerciseResponse>.Failure(TrainingErrors.ExerciseNotFound(query.ExerciseId));

        return Result<ExerciseResponse>.Success(CreateExerciseHandler.MapResponse(exercise));
    }
}