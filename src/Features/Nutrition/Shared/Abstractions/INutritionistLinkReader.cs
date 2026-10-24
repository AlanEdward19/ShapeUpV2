using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface INutritionistLinkReader
{
    /// <summary>The nutritionist linked to <paramref name="userId"/>, or null when the user has none.</summary>
    Task<NutritionistLinkDto?> GetAsync(int userId, CancellationToken cancellationToken);
}
