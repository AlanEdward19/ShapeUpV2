namespace IntegrationTests.Domains.Messaging;

using System.Collections.Concurrent;
using MassTransit;

public sealed record RestartProbeMessage(Guid Id, string Payload);

public sealed class RestartProbeConsumer : IConsumer<RestartProbeMessage>
{
    public static ConcurrentBag<RestartProbeMessage> Received { get; } = [];

    public Task Consume(ConsumeContext<RestartProbeMessage> context)
    {
        Received.Add(context.Message);
        return Task.CompletedTask;
    }

    public static void Reset() => Received.Clear();
}
