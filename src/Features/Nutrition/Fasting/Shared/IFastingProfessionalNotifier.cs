namespace ShapeUp.Features.Nutrition.Fasting.Shared;

/// <summary>Tells the client's professionals (recommender and linked nutritionists) what the client did with fasting.</summary>
public interface IFastingProfessionalNotifier
{
    Task NotifyRecommendationDeclinedAsync(int clientUserId, int? recommendedByUserId, string? reason, CancellationToken cancellationToken);

    Task NotifyEndedEarlyAsync(int clientUserId, int? recommendedByUserId, string? reason, CancellationToken cancellationToken);
}
