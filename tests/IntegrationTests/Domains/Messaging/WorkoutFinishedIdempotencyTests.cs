namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.DependencyInjection;

[Collection("Messaging")]
public sealed class WorkoutFinishedIdempotencyTests(MessagingInfraFixture _) : IAsyncLifetime
{
    private MessagingIdempotencyTestHost? _host;

    public async Task InitializeAsync()
    {
        IdempotencyProbeConsumer.Reset();
        _host = new MessagingIdempotencyTestHost(MessagingInfraFixture.MongoConnectionString, MessagingInfraFixture.RabbitHost);
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();
    }

    [Fact]
    public async Task DuplicateDelivery_WithSameMessageId_ProcessesConsumerEffectOnce()
    {
        var host = _host ?? throw new InvalidOperationException("Idempotency test host was not initialized.");
        var messageId = Guid.NewGuid();
        var payload = $"idem-probe-{messageId:N}";
        var message = new IdempotencyProbeMessage(messageId, payload);

        await using (var scope = host.CreateScope())
        {
            var mongoDbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            for (var attempt = 0; attempt < 2; attempt++)
            {
                await mongoDbContext.StartSession(CancellationToken.None);
                await mongoDbContext.BeginTransaction(CancellationToken.None);

                await publishEndpoint.Publish(
                    message,
                    context =>
                    {
                        context.MessageId = messageId;
                        context.CorrelationId = messageId;
                    },
                    CancellationToken.None);

                await mongoDbContext.CommitTransaction(CancellationToken.None);
            }
        }

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline && IdempotencyProbeConsumer.ProcessedCount < 1)
            await Task.Delay(TimeSpan.FromMilliseconds(250));

        Assert.Equal(1, IdempotencyProbeConsumer.ProcessedCount);
    }
}
