namespace ShapeUp.Features.PlatformFeatureFlags.Health;

public static class FeatureHealthStatus
{
    public const string Healthy = "healthy";
    public const string Unhealthy = "unhealthy";
    public const string Disabled = "disabled";
}

public record FeatureHealthResponse(string Status, IReadOnlyDictionary<string, string> Features);
