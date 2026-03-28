namespace ShapeUp.Features.Training.Shared.Abstractions;

public interface ITrainingAccessPolicy
{
    Task<bool> CanCreateWorkoutForAsync(int actorUserId, int targetUserId, string[] actorScopes, CancellationToken cancellationToken);
}

