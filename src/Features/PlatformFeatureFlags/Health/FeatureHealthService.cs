using ShapeUp.Features.PlatformFeatureFlags.Shared.Abstractions;

namespace ShapeUp.Features.PlatformFeatureFlags.Health;

public class FeatureHealthService(IFeatureFlagReader featureFlagReader, IDependencyHealthChecker dependencyHealthChecker)
{
    private static readonly TimeSpan FlagReadBudget = TimeSpan.FromSeconds(3);

    // Feature key (as exposed by GET /health) -> flag key and the dependencies the feature cannot work without.
    private static readonly (string Feature, string FlagKey, string[] Dependencies)[] Features =
    [
        ("nutrition", "features.nutrition", [HealthDependencies.Mongo]),
        ("training", "features.training", [HealthDependencies.Mongo]),
        ("gamification", "features.gamification", [HealthDependencies.SqlServer, HealthDependencies.RabbitMq]),
        ("gym-management", "features.gym-management", [HealthDependencies.SqlServer]),
        ("notifications", "features.notifications", []),
        ("fasting", "nutrition.intermittent-fasting", [HealthDependencies.Mongo])
    ];

    public async Task<FeatureHealthResponse> GetAsync(CancellationToken cancellationToken)
    {
        var dependencies = await dependencyHealthChecker.CheckAsync(cancellationToken);

        // One shared budget for every flag read, so a dead SQL Server cannot stretch the response past the probe timeout.
        using var flagBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        flagBudget.CancelAfter(FlagReadBudget);

        var features = new Dictionary<string, string>();
        foreach (var (feature, flagKey, requiredDependencies) in Features)
        {
            if (!await IsEnabledSafeAsync(flagKey, flagBudget.Token, cancellationToken))
                features[feature] = FeatureHealthStatus.Disabled;
            else if (requiredDependencies.All(d => dependencies.GetValueOrDefault(d)))
                features[feature] = FeatureHealthStatus.Healthy;
            else
                features[feature] = FeatureHealthStatus.Unhealthy;
        }

        // `status` is the process: it answers, so it is healthy. A feature that is down shows in `features`.
        return new FeatureHealthResponse(FeatureHealthStatus.Healthy, features);
    }

    // Flags live in SQL Server: when it is down the flag is unknown, so it fails open like IFeatureFlagReader does for a missing row.
    private async Task<bool> IsEnabledSafeAsync(string flagKey, CancellationToken budgetToken, CancellationToken requestToken)
    {
        try
        {
            return await featureFlagReader.IsEnabledAsync(flagKey, budgetToken);
        }
        catch (Exception) when (!requestToken.IsCancellationRequested)
        {
            return true;
        }
    }
}
