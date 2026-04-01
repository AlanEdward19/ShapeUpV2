using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.CancelWorkoutSession;

public class CancelWorkoutSessionHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IWorkoutSessionResponseMapper workoutSessionResponseMapper,
    IValidator<CancelWorkoutSessionCommand> validator)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(
        CancelWorkoutSessionCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var session = await workoutSessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionNotFound(command.SessionId));

        if (session.TargetUserId != actorUserId && session.ExecutedByUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Forbidden("You are not allowed to cancel this workout session."));

        if (session.IsCompleted)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCompleted(command.SessionId));

        if (session.IsCancelled)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionAlreadyCancelled(command.SessionId));

        var cancelledAtUtc = DateTime.UtcNow;
        var durationSeconds = (int)Math.Max(0, (cancelledAtUtc - session.StartedAtUtc).TotalSeconds);

        await workoutSessionRepository.CancelAsync(command.SessionId, cancelledAtUtc, durationSeconds, cancellationToken);

        session.IsCancelled = true;
        session.CancelledAtUtc = cancelledAtUtc;
        session.EndedAtUtc = cancelledAtUtc;
        session.LastSavedAtUtc = cancelledAtUtc;
        session.DurationSeconds = durationSeconds;

        return Result<WorkoutSessionResponse>.Success(workoutSessionResponseMapper.Map(session));
    }
}

