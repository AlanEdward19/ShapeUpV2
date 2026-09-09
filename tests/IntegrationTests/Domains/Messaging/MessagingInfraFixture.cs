namespace IntegrationTests.Domains.Messaging;

using IntegrationTests.Infrastructure;
using MassTransit;
using RabbitMQ.Client;

public sealed class MessagingInfraFixture : IAsyncLifetime
{
    public static string MongoConnectionString => IntegrationTestContainers.MongoConnectionString;

    public static string RabbitHost => IntegrationTestContainers.RabbitHost;

    public static ushort RabbitPort => IntegrationTestContainers.RabbitMappedPort;

    public async Task InitializeAsync()
    {
        await IntegrationTestContainers.AcquireMongoAsync();
        await IntegrationTestContainers.AcquireRabbitAsync();
    }

    public async Task DisposeAsync()
    {
        await IntegrationTestContainers.ReleaseRabbitAsync();
        await IntegrationTestContainers.ReleaseMongoAsync();
    }

    public static void ConfigureRabbitMqHost(IRabbitMqBusFactoryConfigurator cfg)
    {
        cfg.Host(RabbitHost, RabbitPort, "/", host =>
        {
            host.Username("guest");
            host.Password("guest");
        });
    }

    public static ConnectionFactory CreateRabbitConnectionFactory() =>
        IntegrationTestContainers.CreateRabbitConnectionFactory();
}
