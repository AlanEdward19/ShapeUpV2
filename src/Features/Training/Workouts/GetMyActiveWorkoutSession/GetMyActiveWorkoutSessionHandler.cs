using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.GetMyActiveWorkoutSession;

using Shared;

public class GetMyActiveWorkoutSessionHandler(
    IWorkoutSessionRepository workoutSessionRepository,
    IWorkoutSessionResponseMapper workoutSessionResponseMapper)
{
    public async Task<Result<GetMyActiveWorkoutSessionResponse>> HandleAsync(int actorUserId, CancellationToken cancellationToken)
    {
        var activeSession = await workoutSessionRepository.GetActiveByTargetUserIdAsync(actorUserId, cancellationToken);
        if (activeSession is null)
            return Result<GetMyActiveWorkoutSessionResponse>.Success(new GetMyActiveWorkoutSessionResponse(false, null));

        return Result<GetMyActiveWorkoutSessionResponse>.Success(
            new GetMyActiveWorkoutSessionResponse(true, workoutSessionResponseMapper.Map(activeSession)));
    }
}

