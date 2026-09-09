namespace IntegrationTests.Domains.Messaging;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client;
using ShapeUp.Configurations;

public sealed class MessagingRetryTestHost : IAsyncDisposable
{
    public const string OutboxMessagesCollectionName = "outbox.messages";

    private readonly ServiceProvider _provider;
    private readonly IBusControl _bus;
    private readonly string _endpointPrefix;

    public string DatabaseName { get; } = $"messaging_retry_{Guid.NewGuid():N}";

    public string ConsumerQueueName { get; }

    public MessagingRetryTestHost(string mongoConnectionString, string rabbitHost = "127.0.0.1")
    {
        _endpointPrefix = $"retry-{Guid.NewGuid():N}"[..20];
        ConsumerQueueName = $"{_endpointPrefix}-always-failing-retry-probe";

        var services = new ServiceCollection();
        services.AddSingleton<MessagingReceiveFaultLogger>();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new MessagingFaultLogCapture());
        });

        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton<IMongoDatabase>(provider =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(DatabaseName));

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<AlwaysFailingRetryProbeConsumer>(cfg =>
            {
                cfg.UseMessageRetry(retry => retry.Immediate(2));
            });

            bus.AddMongoDbOutbox(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
                outbox.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
                outbox.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
                outbox.UseBusOutbox(delivery => delivery.MessageDeliveryLimit = 20);
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.ConnectReceiveObserver(context.GetRequiredService<MessagingReceiveFaultLogger>());

                cfg.Host(rabbitHost, "/", host =>
                {
                    host.Username("guest");
                    host.Password("guest");
                });

                cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter($"{_endpointPrefix}-", false));
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

    public async Task StopAsync(CancellationToken cancellationToken = default) =>
        await _bus.StopAsync(cancellationToken);

    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public async ValueTask DisposeAsync()
    {
        await _bus.StopAsync();
        await _provider.DisposeAsync();
    }

    public static async Task<uint> GetQueueMessageCountAsync(string queueName, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = MessagingInfraFixture.RabbitHost,
            UserName = "guest",
            Password = "guest"
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        try
        {
            var declare = await channel.QueueDeclarePassiveAsync(queueName, cancellationToken: cancellationToken);
            return declare.MessageCount;
        }
        catch
        {
            return 0;
        }
    }
}
