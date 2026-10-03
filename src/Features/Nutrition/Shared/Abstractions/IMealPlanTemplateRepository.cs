using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface IMealPlanTemplateRepository
{
    Task CreateAsync(MealPlanTemplateDocument template, CancellationToken cancellationToken);
    Task<MealPlanTemplateDocument?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MealPlanTemplateDocument>> GetByCreatorAsync(int createdByUserId, CancellationToken cancellationToken);
    Task UpdateAsync(MealPlanTemplateDocument template, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
