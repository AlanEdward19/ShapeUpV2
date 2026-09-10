namespace ShapeUp.Features.PlatformFeatureFlags.Shared.Abstractions;

public interface IFeatureFlagReader
{
    Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken);
}
