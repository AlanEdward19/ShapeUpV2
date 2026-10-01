namespace ShapeUp.Features.PlatformFeatureFlags.Health;

public static class HealthDependencies
{
    public const string SqlServer = "sql-server";
    public const string Mongo = "mongo";
    public const string RabbitMq = "rabbitmq";
}

public interface IDependencyHealthChecker
{
    /// <summary>Returns, per dependency name (see <see cref="HealthDependencies"/>), whether it is reachable. Never throws.</summary>
    Task<IReadOnlyDictionary<string, bool>> CheckAsync(CancellationToken cancellationToken);
}
