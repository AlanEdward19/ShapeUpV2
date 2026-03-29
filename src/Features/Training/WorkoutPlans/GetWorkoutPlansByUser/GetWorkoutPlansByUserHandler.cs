using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.WorkoutPlans.Shared;
using ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutPlans.GetWorkoutPlansByUser;

public class GetWorkoutPlansByUserHandler(
    IWorkoutPlanRepository workoutPlanRepository,
    ITrainingAccessPolicy accessPolicy)
{
    public async Task<Result<KeysetPageResponse<WorkoutPlanResponse>>> HandleAsync(
        GetWorkoutPlansByUserQuery query,
        int actorUserId,
        string[] actorScopes,
        CancellationToken cancellationToken)
    {
        if (query.TargetUserId != actorUserId)
        {
            var canAccess = await accessPolicy.CanCreateWorkoutForAsync(actorUserId, query.TargetUserId, actorScopes, cancellationToken);
            if (!canAccess)
                return Result<KeysetPageResponse<WorkoutPlanResponse>>.Failure(CommonErrors.Forbidden("You are not allowed to list this user's workout plans."));
        }

        DateTime? createdBeforeUtc = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded))
                return Result<KeysetPageResponse<WorkoutPlanResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));

            createdBeforeUtc = DateTime.FromBinary(decoded);
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var plans = await workoutPlanRepository.GetByTargetUserKeysetAsync(query.TargetUserId, createdBeforeUtc, pageSize, cancellationToken);

        var items = plans.Select(x => x.ToResponse()).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].CreatedAtUtc.ToBinary());
        return Result<KeysetPageResponse<WorkoutPlanResponse>>.Success(new KeysetPageResponse<WorkoutPlanResponse>(items, nextCursor));
    }
}
