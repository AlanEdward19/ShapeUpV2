namespace ShapeUp.Configurations;

using MassTransit.MongoDbIntegration;
using MongoDB.Driver;

public interface IWorkoutOutboxTransaction
{
    Task ExecuteAsync(Func<IClientSessionHandle, CancellationToken, Task> action, CancellationToken cancellationToken);
}

public sealed class MassTransitWorkoutOutboxTransaction(MongoDbContext mongoDbContext) : IWorkoutOutboxTransaction
{
    public async Task ExecuteAsync(Func<IClientSessionHandle, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        await mongoDbContext.StartSession(cancellationToken);
        await mongoDbContext.BeginTransaction(cancellationToken);

        try
        {
            await action(mongoDbContext.Session, cancellationToken);
            await mongoDbContext.CommitTransaction(cancellationToken);
        }
        catch
        {
            await mongoDbContext.AbortTransaction(cancellationToken);
            throw;
        }
    }
}
