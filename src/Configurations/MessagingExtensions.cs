namespace ShapeUp.Configurations;

using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Training.Infrastructure.Mongo;
using ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMongoDatabase>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<TrainingMongoOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
        });

        var rabbitHost = configuration["RabbitMQ:Host"]
                         ?? throw new InvalidOperationException("RabbitMQ:Host not configured.");
        var rabbitUsername = configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPassword = configuration["RabbitMQ:Password"] ?? "guest";
        var license = configuration["MassTransit:License"];
        var licensePath = configuration["MassTransit:LicensePath"];

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<WorkoutFinishedConsumer>();

            bus.AddMongoDbOutbox(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromMilliseconds(250);
                outbox.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
                outbox.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
                outbox.UseBusOutbox(delivery => delivery.MessageDeliveryLimit = 20);
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                if (!string.IsNullOrWhiteSpace(license))
                    cfg.SetLicense(license);
                else if (!string.IsNullOrWhiteSpace(licensePath))
                    cfg.SetLicenseLocation(licensePath);

                cfg.Host(rabbitHost, "/", host =>
                {
                    host.Username(rabbitUsername);
                    host.Password(rabbitPassword);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
