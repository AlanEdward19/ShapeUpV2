using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IFoodRepository
{
    Task CreateAsync(FoodDocument food, CancellationToken cancellationToken);
    Task<FoodDocument?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<FoodDocument?> GetByIdIncludingDeletedAsync(string id, CancellationToken cancellationToken);
    Task<FoodDocument?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken);
    Task<(IReadOnlyList<FoodDocument> Items, string? NextCursor)> SearchAsync(
        string query,
        int pageSize,
        string? cursor,
        CancellationToken cancellationToken);
    Task ApplyApprovedOverrideAsync(FoodOverrideDocument overrideDocument, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(string id, int deletedByUserId, DateTime deletedAtUtc, CancellationToken cancellationToken);
}
