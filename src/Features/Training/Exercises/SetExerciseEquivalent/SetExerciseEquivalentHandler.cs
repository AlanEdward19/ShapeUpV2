using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.SetExerciseEquivalent;

public class SetExerciseEquivalentHandler(
    IExerciseRepository exerciseRepository,
    IExerciseEquivalentRepository equivalentRepository,
    IValidator<SetExerciseEquivalentCommand> validator)
{
    public async Task<Result<SetExerciseEquivalentResponse>> HandleAsync(
        SetExerciseEquivalentCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<SetExerciseEquivalentResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var exercise = await exerciseRepository.GetByIdAsync(command.ExerciseId, cancellationToken);
        if (exercise is null)
            return Result<SetExerciseEquivalentResponse>.Failure(TrainingErrors.ExerciseNotFound(command.ExerciseId));

        var other = await exerciseRepository.GetByIdAsync(command.EquivalentExerciseId, cancellationToken);
        if (other is null)
            return Result<SetExerciseEquivalentResponse>.Failure(TrainingErrors.ExerciseNotFound(command.EquivalentExerciseId));

        await equivalentRepository.SetEquivalentAsync(command.ExerciseId, command.EquivalentExerciseId, cancellationToken);

        var leftGroups = exercise.MuscleProfiles.Select(x => x.MuscleGroup).ToHashSet();
        var rightGroups = other.MuscleProfiles.Select(x => x.MuscleGroup).ToHashSet();
        var overlapWarning = !leftGroups.Overlaps(rightGroups);

        return Result<SetExerciseEquivalentResponse>.Success(new SetExerciseEquivalentResponse(overlapWarning));
    }
}
