namespace IntegrationTests.Domains.Messaging;

using System.Collections.Concurrent;
using MassTransit;

public sealed record BrokerDownProbeMessage(Guid Id, string Payload);

public sealed class BrokerDownProbeConsumer : IConsumer<BrokerDownProbeMessage>
{
    public static ConcurrentBag<BrokerDownProbeMessage> Received { get; } = [];

    public Task Consume(ConsumeContext<BrokerDownProbeMessage> context)
    {
        Received.Add(context.Message);
        return Task.CompletedTask;
    }

    public static void Reset() => Received.Clear();
}
