namespace IntegrationTests.Domains.Messaging;

using System.Collections.Concurrent;
using MassTransit;

public sealed record SpikeTestMessage(Guid Id, string Payload);

public sealed class SpikeTestDocument
{
    public string Id { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
}

public sealed class SpikeTestConsumer : IConsumer<SpikeTestMessage>
{
    public static ConcurrentBag<SpikeTestMessage> Received { get; } = [];

    public Task Consume(ConsumeContext<SpikeTestMessage> context)
    {
        Received.Add(context.Message);
        return Task.CompletedTask;
    }

    public static void Reset() => Received.Clear();
}
