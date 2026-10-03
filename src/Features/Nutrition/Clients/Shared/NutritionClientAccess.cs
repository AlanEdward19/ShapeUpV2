using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients.Shared;

/// <summary>
/// Runs an existing "own data" nutrition handler against a client's data, after the access policy allows it.
/// </summary>
public class NutritionClientAccess(INutritionAccessPolicy accessPolicy)
{
    public async Task<Result<T>> RunAsync<T>(
        int actorUserId,
        int targetUserId,
        Func<int, Task<Result<T>>> action,
        CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<T>.Failure(CommonErrors.Forbidden("You are not allowed to access this user's nutrition data."));

        return await action(targetUserId);
    }
}
