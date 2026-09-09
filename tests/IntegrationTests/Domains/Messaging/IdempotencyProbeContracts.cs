namespace IntegrationTests.Domains.Messaging;

using System.Collections.Concurrent;
using MassTransit;

public sealed record IdempotencyProbeMessage(Guid Id, string Payload);

public sealed class IdempotencyProbeConsumer : IConsumer<IdempotencyProbeMessage>
{
    private static int _processedCount;

    public static int ProcessedCount => _processedCount;

    private static readonly ConcurrentDictionary<Guid, byte> ProcessedMessageIds = new();

    public Task Consume(ConsumeContext<IdempotencyProbeMessage> context)
    {
        var messageId = context.MessageId ?? Guid.Empty;
        if (messageId == Guid.Empty || !ProcessedMessageIds.TryAdd(messageId, 0))
            return Task.CompletedTask;

        Interlocked.Increment(ref _processedCount);
        return Task.CompletedTask;
    }

    public static void Reset()
    {
        _processedCount = 0;
        ProcessedMessageIds.Clear();
    }
}
