using FluentValidation;
using ShapeUp.Features.Training.Muscles.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Muscles.CreateMuscle;

public class CreateMuscleHandler(IMuscleRepository muscleRepository, IValidator<CreateMuscleCommand> validator)
{
    public async Task<Result<MuscleResponse>> HandleAsync(CreateMuscleCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MuscleResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var muscle = new Muscle
        {
            Name = command.Name.Trim(),
            NamePt = command.NamePt.Trim()
        };

        await muscleRepository.AddAsync(muscle, cancellationToken);
        return Result<MuscleResponse>.Success(new MuscleResponse(muscle.Id, muscle.Name, muscle.NamePt));
    }
}