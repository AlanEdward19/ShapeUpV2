using MassTransit;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using ShapeUp.Features.Authorization.Shared.Data;

namespace ShapeUp.Features.PlatformFeatureFlags.Health;

public class DependencyHealthChecker(
    AuthorizationDbContext sqlContext,
    IMongoDatabase mongoDatabase,
    IBusControl busControl) : IDependencyHealthChecker
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    public async Task<IReadOnlyDictionary<string, bool>> CheckAsync(CancellationToken cancellationToken)
    {
        var sql = ProbeAsync(ct => sqlContext.Database.CanConnectAsync(ct), cancellationToken);
        var mongo = ProbeAsync(async ct =>
        {
            await mongoDatabase.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
            return true;
        }, cancellationToken);
        var rabbit = ProbeAsync(async ct => busControl.CheckHealth().Status == BusHealthStatus.Healthy, cancellationToken);

        return new Dictionary<string, bool>
        {
            [HealthDependencies.SqlServer] = await sql,
            [HealthDependencies.Mongo] = await mongo,
            [HealthDependencies.RabbitMq] = await rabbit
        };
    }

    // Any failure or timeout means "unreachable"; the exception is never surfaced to the caller or the response.
    private static async Task<bool> ProbeAsync(Func<CancellationToken, Task<bool>> probe, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);
        try
        {
            return await probe(timeout.Token);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
