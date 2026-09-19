using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Features.PlatformFeatureFlags.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting.Shared;

public sealed class FastingFeatureGuard(IFeatureFlagReader featureFlagReader)
{
    public const string FeatureKey = "nutrition.intermittent-fasting";

    public async Task<Result> EnsureEnabledAsync(CancellationToken cancellationToken)
    {
        if (await featureFlagReader.IsEnabledAsync(FeatureKey, cancellationToken))
            return Result.Success();

        return Result.Failure(NutritionErrors.FastingDisabled());
    }
}
