using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.GetLatestCompletedWorkoutSessionByPlanId;

public class GetLatestCompletedWorkoutSessionByPlanIdHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    ITrainingAccessPolicy accessPolicy,
    IWorkoutSessionResponseMapper workoutSessionResponseMapper)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(
        GetLatestCompletedWorkoutSessionByPlanIdQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.WorkoutPlanId))
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Validation("Workout plan id is required."));

        var session = await workoutSessionRepository.GetLatestCompletedByWorkoutPlanIdAsync(query.WorkoutPlanId, cancellationToken);
        if (session is null)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.NotFound("No completed workout session found for this workout plan."));

        if (session.TargetUserId != actorUserId)
        {
            var canAccess = await accessPolicy.CanCreateWorkoutForAsync(actorUserId, session.TargetUserId, cancellationToken);
            if (!canAccess)
                return Result<WorkoutSessionResponse>.Failure(CommonErrors.Forbidden("You are not allowed to access this workout session."));
        }

        return Result<WorkoutSessionResponse>.Success(workoutSessionResponseMapper.Map(session));
    }
}
