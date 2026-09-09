namespace ShapeUp.Configurations;

using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Gamification.WorkoutFinished;
using ShapeUp.Features.Training.Infrastructure.Mongo;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOutboxFaultInjector, NoOpOutboxFaultInjector>();
        services.AddScoped<IWorkoutOutboxTransaction, MassTransitWorkoutOutboxTransaction>();
        services.AddSingleton<MessagingReceiveFaultLogger>();

        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TrainingMongoOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
        });

        var transport = configuration["Messaging:Transport"] ?? "RabbitMQ";
        var useInMemoryTransport = transport.Equals("InMemory", StringComparison.OrdinalIgnoreCase);
        var rabbitHost = configuration["RabbitMQ:Host"];
        var rabbitPort = ushort.TryParse(configuration["RabbitMQ:Port"], out var parsedPort) ? parsedPort : (ushort)5672;
        var rabbitUsername = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPassword = configuration["RabbitMQ:Password"] ?? "guest";
        var license = configuration["MassTransit:License"];
        var licensePath = configuration["MassTransit:LicensePath"];

        if (!useInMemoryTransport && string.IsNullOrWhiteSpace(rabbitHost))
            throw new InvalidOperationException("RabbitMQ:Host not configured.");

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<GamificationWorkoutFinishedConsumer>();

            bus.AddMongoDbOutbox(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
                outbox.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
                outbox.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
                outbox.UseBusOutbox(delivery => delivery.MessageDeliveryLimit = 20);
            });

            if (useInMemoryTransport)
            {
                bus.UsingInMemory((context, cfg) =>
                {
                    cfg.ConnectReceiveObserver(context.GetRequiredService<MessagingReceiveFaultLogger>());

                    var endpointPrefix = configuration["Messaging:EndpointPrefix"];
                    if (!string.IsNullOrWhiteSpace(endpointPrefix))
                        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter($"{endpointPrefix}-", false));
                    else
                        cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                bus.UsingRabbitMq((context, cfg) =>
                {
                    if (!string.IsNullOrWhiteSpace(license))
                        cfg.SetLicense(license);
                    else if (!string.IsNullOrWhiteSpace(licensePath))
                        cfg.SetLicenseLocation(licensePath);

                    cfg.Host(rabbitHost!, rabbitPort, "/", host =>
                    {
                        host.Username(rabbitUsername);
                        host.Password(rabbitPassword);
                    });

                    cfg.ConnectReceiveObserver(context.GetRequiredService<MessagingReceiveFaultLogger>());

                    var endpointPrefix = configuration["Messaging:EndpointPrefix"];
                    if (!string.IsNullOrWhiteSpace(endpointPrefix))
                        cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter($"{endpointPrefix}-", false));
                    else
                        cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}
