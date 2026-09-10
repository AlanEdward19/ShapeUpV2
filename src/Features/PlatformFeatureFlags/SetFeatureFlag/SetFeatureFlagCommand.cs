namespace ShapeUp.Features.PlatformFeatureFlags.SetFeatureFlag;

public sealed record SetFeatureFlagCommand(string Key, bool Enabled, int UpdatedByUserId);
