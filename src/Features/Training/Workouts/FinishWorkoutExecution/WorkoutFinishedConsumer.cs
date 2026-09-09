namespace ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

using MassTransit;
using ShapeUp.Features.Training.Shared.Events;

public sealed class WorkoutFinishedConsumer(ILogger<WorkoutFinishedConsumer> logger) : IConsumer<WorkoutFinished>
{
    public Task Consume(ConsumeContext<WorkoutFinished> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Workout finished for session {SessionId} (target user {TargetUserId}, executed by {ExecutedByUserId}, ended at {EndedAtUtc})",
            message.SessionId,
            message.TargetUserId,
            message.ExecutedByUserId,
            message.EndedAtUtc);

        return Task.CompletedTask;
    }
}
