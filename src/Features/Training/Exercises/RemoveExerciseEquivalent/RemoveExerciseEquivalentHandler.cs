using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.RemoveExerciseEquivalent;

public class RemoveExerciseEquivalentHandler(
    IExerciseEquivalentRepository equivalentRepository,
    IValidator<RemoveExerciseEquivalentCommand> validator)
{
    public async Task<Result> HandleAsync(RemoveExerciseEquivalentCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        await equivalentRepository.RemoveEquivalentAsync(command.ExerciseId, command.EquivalentExerciseId, cancellationToken);
        return Result.Success();
    }
}
