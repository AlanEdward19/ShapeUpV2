using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IFoodModerationRepository
{
    Task<IReadOnlyList<FoodModerationRequestDocument>> GetPendingAsync(
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken);
    Task<bool> DecideAsync(
        string requestId,
        string decision,
        int decidedByUserId,
        DateTime decidedAtUtc,
        CancellationToken cancellationToken);
    Task CreateAsync(FoodModerationRequestDocument request, CancellationToken cancellationToken);
}
