using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IFoodModerationRepository
{
    Task<(IReadOnlyList<FoodModerationRequestDocument> Items, string? NextCursor)> GetPendingAsync(
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken);
    Task<FoodModerationRequestDocument?> GetByIdAsync(string requestId, CancellationToken cancellationToken);
    Task<bool> DecideAsync(
        string requestId,
        string decision,
        int decidedByUserId,
        DateTime decidedAtUtc,
        CancellationToken cancellationToken);
    Task CreateAsync(FoodModerationRequestDocument request, CancellationToken cancellationToken);
}
