namespace ShapeUp.Features.Nutrition.Shared.Abstractions;

public interface INutritionAccessPolicy
{
    /// <summary>
    /// Self-access is always allowed. Otherwise the actor needs the nutrition capability
    /// (active Nutritionist role or verified Nutritionist credential) and an active
    /// "Nutrition" relationship with the target.
    /// </summary>
    Task<bool> CanManageNutritionForAsync(int actorUserId, int targetUserId, CancellationToken cancellationToken);
}
