namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

public sealed class MessagingBrokerDownTestHost : IAsyncDisposable
{
    public const string OutboxMessagesCollectionName = "outbox.messages";

    private readonly ServiceProvider _provider;
    private readonly IReadOnlyList<IHostedService> _hostedServices;

    public string DatabaseName { get; }

    public MessagingBrokerDownTestHost(string mongoConnectionString, string? databaseName = null)
    {
        DatabaseName = databaseName ?? $"messaging_brokerdown_{Guid.NewGuid():N}";
        var endpointPrefix = $"brokerdown-{Guid.NewGuid():N}"[..20];

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton<IMongoDatabase>(provider =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(DatabaseName));

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<BrokerDownProbeConsumer>();

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
                cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter($"{endpointPrefix}-", false));
            });
        });

        _provider = services.BuildServiceProvider();
        _hostedServices = MessagingHostedServiceLifecycle.Capture(_provider);
    }

    public bool IsStarted { get; private set; }

    public async Task StartBusAsync(CancellationToken cancellationToken = default)
    {
        if (IsStarted)
            return;

        await MessagingHostedServiceLifecycle.StartAsync(_hostedServices, cancellationToken);
        IsStarted = true;
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task StopBusAsync(CancellationToken cancellationToken = default)
    {
        if (!IsStarted)
            return;

        await MessagingHostedServiceLifecycle.StopAsync(_hostedServices, cancellationToken);
        IsStarted = false;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default) =>
        await StartBusAsync(cancellationToken);

    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public IMongoDatabase GetDatabase() => _provider.GetRequiredService<IMongoDatabase>();

    public async ValueTask DisposeAsync()
    {
        if (IsStarted)
            await MessagingHostedServiceLifecycle.StopAsync(_hostedServices);

        await _provider.DisposeAsync();
    }
}
