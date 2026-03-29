using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.GetWorkoutSessionById;

public class GetWorkoutSessionByIdHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IWorkoutSessionResponseMapper workoutSessionResponseMapper)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(GetWorkoutSessionByIdQuery query, int actorUserId, CancellationToken cancellationToken)
    {
        var session = await workoutSessionRepository.GetByIdAsync(query.SessionId, cancellationToken);
        if (session is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutSessionNotFound(query.SessionId));

        if (session.TargetUserId != actorUserId && session.TrainerUserId != actorUserId)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Forbidden("You are not allowed to access this workout session."));

        return Result<WorkoutSessionResponse>.Success(workoutSessionResponseMapper.Map(session));
    }
}