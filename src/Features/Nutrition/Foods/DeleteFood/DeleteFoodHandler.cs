using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Foods.DeleteFood;

public class DeleteFoodHandler(IFoodRepository foodRepository)
{
    public async Task<Result> HandleAsync(
        DeleteFoodCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        await foodRepository.SoftDeleteAsync(command.FoodId, actorUserId, DateTime.UtcNow, cancellationToken);
        return Result.Success();
    }
}
