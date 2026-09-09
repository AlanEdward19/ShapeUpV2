namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

public sealed class MessagingRestartTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IBusControl _bus;

    public string DatabaseName { get; } = $"messaging_restart_{Guid.NewGuid():N}";

    public MessagingRestartTestHost(string mongoConnectionString, string rabbitHost = "127.0.0.1")
    {
        var endpointPrefix = $"restart-{Guid.NewGuid():N}"[..20];

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton<IMongoDatabase>(provider =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(DatabaseName));

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<RestartProbeConsumer>();

            bus.AddMongoDbOutbox(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
                outbox.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
                outbox.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
                outbox.UseBusOutbox(delivery => delivery.MessageDeliveryLimit = 20);
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", host =>
                {
                    host.Username("guest");
                    host.Password("guest");
                });

                cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter($"{endpointPrefix}-", false));
            });
        });

        _provider = services.BuildServiceProvider();
        _bus = _provider.GetRequiredService<IBusControl>();
    }

    public bool IsStarted { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsStarted)
            return;

        await _bus.StartAsync(cancellationToken);
        IsStarted = true;
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsStarted)
            return;

        await _bus.StopAsync(cancellationToken);
        IsStarted = false;
    }

    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public IMongoDatabase GetDatabase() => _provider.GetRequiredService<IMongoDatabase>();

    public async ValueTask DisposeAsync()
    {
        if (IsStarted)
            await _bus.StopAsync();

        await _provider.DisposeAsync();
    }
}
