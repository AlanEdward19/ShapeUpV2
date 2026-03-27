using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Muscles.DeleteMuscle;

public class DeleteMuscleHandler(IMuscleRepository muscleRepository)
{
    public async Task<Result> HandleAsync(DeleteMuscleCommand command, CancellationToken cancellationToken)
    {
        var muscle = await muscleRepository.GetByIdAsync(command.MuscleId, cancellationToken);
        if (muscle is null)
            return Result.Failure(TrainingErrors.MuscleNotFound(command.MuscleId));

        await muscleRepository.DeleteAsync(muscle, cancellationToken);
        return Result.Success();
    }
}