namespace IntegrationTests.Domains.Messaging;

using ShapeUp.Configurations;

public sealed class TestOutboxFaultInjector : IOutboxFaultInjector
{
    public bool ThrowAfterPublish { get; set; }

    public Task AfterPublishAsync(CancellationToken cancellationToken)
    {
        if (ThrowAfterPublish)
            throw new InvalidOperationException("Test fault injection after outbox publish.");

        return Task.CompletedTask;
    }
}
