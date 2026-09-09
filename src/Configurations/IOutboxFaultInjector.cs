namespace ShapeUp.Configurations;

public interface IOutboxFaultInjector
{
    Task AfterPublishAsync(CancellationToken cancellationToken);
}

public sealed class NoOpOutboxFaultInjector : IOutboxFaultInjector
{
    public Task AfterPublishAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
