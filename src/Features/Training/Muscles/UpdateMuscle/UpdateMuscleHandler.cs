using FluentValidation;
using ShapeUp.Features.Training.Muscles.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Muscles.UpdateMuscle;

public class UpdateMuscleHandler(IMuscleRepository muscleRepository, IValidator<UpdateMuscleCommand> validator)
{
    public async Task<Result<MuscleResponse>> HandleAsync(UpdateMuscleCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MuscleResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var muscle = await muscleRepository.GetByIdAsync(command.MuscleId, cancellationToken);
        if (muscle is null)
            return Result<MuscleResponse>.Failure(TrainingErrors.MuscleNotFound(command.MuscleId));

        muscle.Name = command.Name.Trim();
        muscle.NamePt = command.NamePt.Trim();

        await muscleRepository.UpdateAsync(muscle, cancellationToken);
        return Result<MuscleResponse>.Success(new MuscleResponse(muscle.Id, muscle.Name, muscle.NamePt));
    }
}