namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

public sealed class MassTransitSpikeHost : IAsyncDisposable
{
    public const string DocumentsCollectionName = "spike_test_documents";
    public const string OutboxMessagesCollectionName = "outbox.messages";

    private readonly ServiceProvider _provider;
    private readonly IBusControl _bus;

    public string DatabaseName { get; } = $"messaging_spike_{Guid.NewGuid():N}";

    public MassTransitSpikeHost(string mongoConnectionString, string rabbitHost = "localhost")
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton<IMongoDatabase>(provider =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(DatabaseName));

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<SpikeTestConsumer>();
            bus.AddMongoDbOutbox(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
                outbox.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
                outbox.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
                outbox.UseBusOutbox(delivery => delivery.MessageDeliveryLimit = 20);
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                MessagingInfraFixture.ConfigureRabbitMqHost(cfg);
                cfg.ConfigureEndpoints(context);
            });
        });

        _provider = services.BuildServiceProvider();
        _bus = _provider.GetRequiredService<IBusControl>();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _bus.StartAsync(cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public IMongoDatabase GetDatabase() => _provider.GetRequiredService<IMongoDatabase>();

    public async ValueTask DisposeAsync()
    {
        await _bus.StopAsync();
        await _provider.DisposeAsync();
    }
}
