using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Workouts.CreateWorkoutSession;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.GetWorkoutSessionsByUser;

public class GetWorkoutSessionsByUserHandler(IWorkoutSessionRepository workoutSessionRepository)
{
    public async Task<Result<KeysetPageResponse<WorkoutSessionResponse>>> HandleAsync(
        GetWorkoutSessionsByUserQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (query.TargetUserId != actorUserId)
            return Result<KeysetPageResponse<WorkoutSessionResponse>>.Failure(CommonErrors.Forbidden("You can only list your own workout sessions."));

        DateTime? startedBeforeUtc = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded))
                return Result<KeysetPageResponse<WorkoutSessionResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));

            startedBeforeUtc = DateTime.FromBinary(decoded);
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var sessions = await workoutSessionRepository.GetByTargetUserKeysetAsync(query.TargetUserId, startedBeforeUtc, pageSize, cancellationToken);

        var items = sessions.Select(CreateWorkoutSessionHandler.Map).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].StartedAtUtc.ToBinary());
        return Result<KeysetPageResponse<WorkoutSessionResponse>>.Success(new KeysetPageResponse<WorkoutSessionResponse>(items, nextCursor));
    }
}