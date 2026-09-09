namespace IntegrationTests.Domains.Messaging;

using System.Collections.Concurrent;
using MassTransit;

public sealed record RetryProbeMessage(Guid Id, string Payload);

public sealed class AlwaysFailingRetryProbeConsumer : IConsumer<RetryProbeMessage>
{
    public static ConcurrentBag<Guid> Attempted { get; } = [];
    public static ConcurrentBag<Guid> TransportMessageIds { get; } = [];

    public Task Consume(ConsumeContext<RetryProbeMessage> context)
    {
        Attempted.Add(context.Message.Id);
        if (context.MessageId.HasValue)
            TransportMessageIds.Add(context.MessageId.Value);

        throw new InvalidOperationException("Intentional consumer failure for retry/dead-letter test.");
    }

    public static void Reset()
    {
        Attempted.Clear();
        TransportMessageIds.Clear();
    }
}
