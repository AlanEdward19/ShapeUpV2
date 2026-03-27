using ShapeUp.Features.Training.Muscles.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Muscles.GetMuscleById;

public class GetMuscleByIdHandler(IMuscleRepository muscleRepository)
{
    public async Task<Result<MuscleResponse>> HandleAsync(GetMuscleByIdQuery query, CancellationToken cancellationToken)
    {
        var muscle = await muscleRepository.GetByIdAsync(query.MuscleId, cancellationToken);
        if (muscle is null)
            return Result<MuscleResponse>.Failure(TrainingErrors.MuscleNotFound(query.MuscleId));

        return Result<MuscleResponse>.Success(new MuscleResponse(muscle.Id, muscle.Name, muscle.NamePt));
    }
}

