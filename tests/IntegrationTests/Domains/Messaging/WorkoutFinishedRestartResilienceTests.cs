namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.DependencyInjection;

[Collection("Messaging")]
public sealed class WorkoutFinishedRestartResilienceTests(MessagingInfraFixture _) : IAsyncLifetime
{
    private MessagingRestartTestHost? _host;

    public Task InitializeAsync()
    {
        RestartProbeConsumer.Reset();
        _host = new MessagingRestartTestHost(MessagingInfraFixture.MongoConnectionString, MessagingInfraFixture.RabbitHost);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();
    }

    [Fact]
    public async Task PendingOutboxEntry_DeliveredAfterBusRestart()
    {
        var host = _host ?? throw new InvalidOperationException("Restart test host was not initialized.");
        var messageId = Guid.NewGuid();
        var payload = $"restart-probe-{messageId:N}";

        await host.StartAsync();
        await host.StopAsync();

        await using (var scope = host.CreateScope())
        {
            var mongoDbContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await mongoDbContext.StartSession(CancellationToken.None);
            await mongoDbContext.BeginTransaction(CancellationToken.None);
            await publishEndpoint.Publish(new RestartProbeMessage(messageId, payload), CancellationToken.None);
            await mongoDbContext.CommitTransaction(CancellationToken.None);
        }

        await host.StartAsync();

        var received = await WaitForMessageAsync(messageId, TimeSpan.FromSeconds(45));
        Assert.Equal(payload, received.Payload);

        await OutboxRelayAssertions.AssertOutboxMessageRelayedAsync(
            host.GetDatabase(),
            TimeSpan.FromSeconds(15));
    }

    private static async Task<RestartProbeMessage> WaitForMessageAsync(Guid messageId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var match = RestartProbeConsumer.Received.FirstOrDefault(m => m.Id == messageId);
            if (match is not null)
                return match;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"Consumer did not receive restart probe {messageId} within {timeout.TotalSeconds}s.");
    }
}
