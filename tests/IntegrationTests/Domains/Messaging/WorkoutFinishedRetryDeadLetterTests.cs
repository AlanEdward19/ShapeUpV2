namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.DependencyInjection;

[Collection("Messaging")]
public sealed class WorkoutFinishedRetryDeadLetterTests(MessagingInfraFixture _) : IAsyncLifetime
{
    private MessagingRetryTestHost? _host;

    public async Task InitializeAsync()
    {
        AlwaysFailingRetryProbeConsumer.Reset();
        _host = new MessagingRetryTestHost(MessagingInfraFixture.MongoConnectionString, MessagingInfraFixture.RabbitHost);
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();
    }

    [Fact]
    public async Task ConsumerThatAlwaysThrows_RoutesMessageToErrorQueueAfterRetryExhaustion()
    {
        var host = _host ?? throw new InvalidOperationException("Retry test host was not initialized.");
        var messageId = Guid.NewGuid();
        var payload = $"retry-probe-{messageId:N}";

        await using (var scope = host.CreateScope())
        {
            var mongoDbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await mongoDbContext.StartSession(CancellationToken.None);
            await mongoDbContext.BeginTransaction(CancellationToken.None);

            await publishEndpoint.Publish(new RetryProbeMessage(messageId, payload), CancellationToken.None);
            await mongoDbContext.CommitTransaction(CancellationToken.None);
        }

        var errorQueueName = $"{host.ConsumerQueueName}_error";
        var deadline = DateTime.UtcNow.AddSeconds(45);

        while (DateTime.UtcNow < deadline)
        {
            var errorCount = await MessagingRetryTestHost.GetQueueMessageCountAsync(errorQueueName);
            if (errorCount > 0)
            {
                Assert.True(AlwaysFailingRetryProbeConsumer.Attempted.Count >= 3);
                Assert.True(errorCount >= 1);
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException(
            $"Message was not routed to dead-letter queue '{errorQueueName}' within 45s. Attempts: {AlwaysFailingRetryProbeConsumer.Attempted.Count}.");
    }
}
