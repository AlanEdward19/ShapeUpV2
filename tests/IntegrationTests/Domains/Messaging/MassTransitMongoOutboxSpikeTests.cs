namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

[Collection("Messaging")]
public sealed class MassTransitMongoOutboxSpikeTests(MessagingInfraFixture _) : IAsyncLifetime
{
    private MassTransitSpikeHost? _host;

    public async Task InitializeAsync()
    {
        SpikeTestConsumer.Reset();
        _host = new MassTransitSpikeHost(MessagingInfraFixture.MongoConnectionString, MessagingInfraFixture.RabbitHost);
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();
    }

    [Fact]
    public async Task PublishConsumeRoundTrip_WorksOnNet10_WithRealRabbitMqAndMongoReplicaSet()
    {
        var host = _host ?? throw new InvalidOperationException("Spike host was not initialized.");
        var messageId = Guid.NewGuid();
        var payload = $"round-trip-{messageId:N}";

        await using (var scope = host.CreateScope())
        {
            var mongoDbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var documents = scope.ServiceProvider.GetRequiredService<IMongoDatabase>()
                .GetCollection<SpikeTestDocument>(MassTransitSpikeHost.DocumentsCollectionName);

            await mongoDbContext.StartSession(CancellationToken.None);
            await mongoDbContext.BeginTransaction(CancellationToken.None);

            await documents.InsertOneAsync(
                mongoDbContext.Session,
                new SpikeTestDocument { Id = messageId.ToString("N"), Payload = payload },
                cancellationToken: CancellationToken.None);

            await publishEndpoint.Publish(new SpikeTestMessage(messageId, payload), CancellationToken.None);
            await mongoDbContext.CommitTransaction(CancellationToken.None);
        }

        var received = await WaitForMessageAsync(messageId, TimeSpan.FromSeconds(30));
        Assert.Equal(payload, received.Payload);
    }

    [Fact]
    public async Task ForcedFailureBeforeCommit_RollsBackDocumentAndOutboxEntry()
    {
        var host = _host ?? throw new InvalidOperationException("Spike host was not initialized.");
        var messageId = Guid.NewGuid();
        var payload = $"rollback-{messageId:N}";

        await using (var scope = host.CreateScope())
        {
            var mongoDbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var documents = scope.ServiceProvider.GetRequiredService<IMongoDatabase>()
                .GetCollection<SpikeTestDocument>(MassTransitSpikeHost.DocumentsCollectionName);

            await mongoDbContext.StartSession(CancellationToken.None);
            await mongoDbContext.BeginTransaction(CancellationToken.None);

            await documents.InsertOneAsync(
                mongoDbContext.Session,
                new SpikeTestDocument { Id = messageId.ToString("N"), Payload = payload },
                cancellationToken: CancellationToken.None);

            await publishEndpoint.Publish(new SpikeTestMessage(messageId, payload), CancellationToken.None);

            // Force failure after document write and outbox enqueue, before commit.
            await mongoDbContext.AbortTransaction(CancellationToken.None);
        }

        var database = host.GetDatabase();
        var document = await database
            .GetCollection<SpikeTestDocument>(MassTransitSpikeHost.DocumentsCollectionName)
            .Find(d => d.Id == messageId.ToString("N"))
            .FirstOrDefaultAsync();

        Assert.Null(document);

        var outboxCount = await database
            .GetCollection<MongoDB.Bson.BsonDocument>(MassTransitSpikeHost.OutboxMessagesCollectionName)
            .CountDocumentsAsync(
                MongoDB.Bson.BsonDocument.Parse($"{{ \"messageId\": \"{messageId}\" }}"));

        Assert.Equal(0, outboxCount);
        Assert.DoesNotContain(SpikeTestConsumer.Received, m => m.Id == messageId);
    }

    private static async Task<SpikeTestMessage> WaitForMessageAsync(Guid messageId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var match = SpikeTestConsumer.Received.FirstOrDefault(m => m.Id == messageId);
            if (match is not null)
                return match;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"Consumer did not receive message {messageId} within {timeout.TotalSeconds}s.");
    }
}

[CollectionDefinition("Messaging")]
public sealed class MessagingCollection : ICollectionFixture<MessagingInfraFixture>;
