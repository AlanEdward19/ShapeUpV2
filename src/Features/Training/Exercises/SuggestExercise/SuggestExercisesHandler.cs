using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.SuggestExercise;

public class SuggestExercisesHandler(IExerciseRepository exerciseRepository, IValidator<SuggestExercisesQuery> validator)
{
    public async Task<Result<ExerciseResponse[]>> HandleAsync(SuggestExercisesQuery query, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<ExerciseResponse[]>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var limit = query.Limit ?? 10;
        var suggestions = await exerciseRepository.SuggestAsync(
            query.Name,
            query.MuscleIds.Distinct().ToArray(),
            query.EquipmentIds.Distinct().ToArray(),
            limit,
            cancellationToken);

        return Result<ExerciseResponse[]>.Success(suggestions.Select(CreateExerciseHandler.MapResponse).ToArray());
    }
}