namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

[Collection("Messaging")]
public sealed class WorkoutFinishedBrokerDownResilienceTests(MessagingInfraFixture _) : IAsyncLifetime
{
    private const string RabbitContainerName = "shapeup-rabbitmq";

    private MessagingBrokerDownTestHost? _host;

    public async Task InitializeAsync()
    {
        BrokerDownProbeConsumer.Reset();
        await EnsureRabbitMqRunningAsync();
        _host = new MessagingBrokerDownTestHost(MessagingInfraFixture.MongoConnectionString, MessagingInfraFixture.RabbitHost);
        await _host.StartBusAsync();
    }

    public async Task DisposeAsync()
    {
        try
        {
            await EnsureRabbitMqRunningAsync();
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

        await StopRabbitMqAsync();

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

        await EnsureRabbitMqRunningAsync();
        await WaitForRabbitMqAsync();

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

    private static Task StopRabbitMqAsync() =>
        RunDockerAsync($"stop {RabbitContainerName}");

    private static Task EnsureRabbitMqRunningAsync() =>
        RunDockerAsync($"start {RabbitContainerName}");

    private static async Task RunDockerAsync(string arguments)
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException($"Failed to start docker {arguments}.");

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"docker {arguments} failed: {error}");
        }
    }

    private static async Task WaitForRabbitMqAsync()
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var factory = new RabbitMQ.Client.ConnectionFactory
                {
                    HostName = MessagingInfraFixture.RabbitHost,
                    UserName = "guest",
                    Password = "guest"
                };

                await using var connection = await factory.CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();
                return;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        throw new InvalidOperationException("RabbitMQ did not become ready after recovery.");
    }
}
