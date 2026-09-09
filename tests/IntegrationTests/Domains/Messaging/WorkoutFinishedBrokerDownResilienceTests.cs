namespace IntegrationTests.Domains.Messaging;

using IntegrationTests.Infrastructure;
using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

[Collection("Messaging")]
public sealed class WorkoutFinishedBrokerDownResilienceTests(MessagingInfraFixture _) : IAsyncLifetime
{
    private MessagingBrokerDownTestHost? _host;

    public async Task InitializeAsync()
    {
        BrokerDownProbeConsumer.Reset();
        await IntegrationTestContainers.StartRabbitAsync();
        _host = new MessagingBrokerDownTestHost(MessagingInfraFixture.MongoConnectionString);
        await _host.StartBusAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await IntegrationTestContainers.StartRabbitAsync();
        }
        finally
        {
            if (_host is not null)
                await _host.DisposeAsync();
        }
    }

    [Fact]
    public async Task BrokerDown_KeepsOutboxPending_ThenDeliversAfterRecovery()
    {
        var host = _host ?? throw new InvalidOperationException("Broker-down test host was not initialized.");
        var messageId = Guid.NewGuid();
        var payload = $"brokerdown-probe-{messageId:N}";

        await IntegrationTestContainers.StopRabbitAsync();

        await using (var scope = host.CreateScope())
        {
            var mongoDbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await mongoDbContext.StartSession(CancellationToken.None);
            await mongoDbContext.BeginTransaction(CancellationToken.None);
            await publishEndpoint.Publish(new BrokerDownProbeMessage(messageId, payload), CancellationToken.None);
            await mongoDbContext.CommitTransaction(CancellationToken.None);
        }

        var outboxCollection = host.GetDatabase().GetCollection<BsonDocument>(MessagingBrokerDownTestHost.OutboxMessagesCollectionName);
        var pendingCount = await outboxCollection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);

        Assert.True(pendingCount >= 1);
        Assert.Empty(BrokerDownProbeConsumer.Received);

        await IntegrationTestContainers.StartRabbitAsync();

        var databaseName = host.DatabaseName;
        await host.DisposeAsync();
        host = new MessagingBrokerDownTestHost(MessagingInfraFixture.MongoConnectionString, databaseName);
        _host = host;
        await host.StartBusAsync();

        var received = await WaitForMessageAsync(messageId, TimeSpan.FromSeconds(60));
        Assert.Equal(payload, received.Payload);
    }

    private static async Task<BrokerDownProbeMessage> WaitForMessageAsync(Guid messageId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var match = BrokerDownProbeConsumer.Received.FirstOrDefault(m => m.Id == messageId);
            if (match is not null)
                return match;

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException($"Consumer did not receive broker-down probe {messageId} within {timeout.TotalSeconds}s.");
    }
}
