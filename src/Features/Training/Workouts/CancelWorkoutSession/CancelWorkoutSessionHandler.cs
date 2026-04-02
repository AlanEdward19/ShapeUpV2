using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.CancelWorkoutSession;

public class CancelWorkoutSessionHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IValidator<CancelWorkoutSessionCommand> validator)
{
    public async Task<Result> HandleAsync(
        CancelWorkoutSessionCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.TargetUserId != actorUserId && session.ExecutedByUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result.Failure(CommonErrors.Forbidden("You are not allowed to cancel this workout session."));

        if (session.IsCompleted)
            return Result.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        if (session.IsCancelled)
            return Result.Failure(TrainingErrors.WorkoutSessionAlreadyCancelled(command.SessionId));

        await workoutSessionRepository.CancelAsync(command.SessionId, cancellationToken);
        return Result.Success();
    }
}

