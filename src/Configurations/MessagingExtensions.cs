namespace ShapeUp.Configurations;

using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Gamification.WorkoutFinished;
using ShapeUp.Features.Nutrition.GoalEvaluation;
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

        var enableNutritionGoalJob = configuration.GetValue<bool?>("Messaging:EnableNutritionGoalJob")
            ?? !useInMemoryTransport;

        if (!useInMemoryTransport && enableNutritionGoalJob)
            services.AddHostedService<NutritionGoalEvaluationJobRegistrationHostedService>();

        // T1 spike (feature `nutrition`) proved the recurring Job Consumer API works here — findings:
        //   - AddConsumer<T>() + IJobConsumer<T>.Run(JobContext<T>)
        //   - AddDelayedMessageScheduler() (bus-level) + cfg.UseDelayedMessageScheduler() (transport-level)
        //   - SetInMemorySagaRepositoryProvider() + AddJobSagaStateMachines() for the job-service saga
        //   - IPublishEndpoint.AddOrUpdateRecurringJob(name, message, schedule => schedule.Every(...))
        //   - IPublishEndpoint.RunRecurringJob<T>(name) to trigger an immediate run after registration
        //   - RabbitMQ needs the rabbitmq_delayed_message_exchange plugin for UseDelayedMessageScheduler
        // The spike's own wiring (job saga state machines + hosted service registering it) was REMOVED here —
        // registering it unconditionally on every AddMassTransit call made every IntegrationWebApplicationFactory
        // instance (one per integration test class, ~50+) pay for job-saga startup/teardown, which is what caused
        // the test suite to hang for 46+ minutes (and again, worse, for the Domains/Messaging/* classes that use a
        // REAL RabbitMQ Testcontainer instead of the InMemory transport — the spike hosted service only activates
        // when !useInMemoryTransport, so those specific classes paid for a real job-service round-trip over a real
        // broker, on top of container startup). T18 re-adds this wiring for the REAL job consumer, and MUST confirm
        // the full test-suite duration stays normal afterward, including the Domains/Messaging/* RabbitMQ classes —
        // do not repeat this mistake twice.
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<GamificationWorkoutFinishedConsumer>();

            if (!useInMemoryTransport && enableNutritionGoalJob)
            {
                bus.AddConsumer<NutritionGoalEvaluationJobConsumer>();
                bus.AddDelayedMessageScheduler();
                bus.SetInMemorySagaRepositoryProvider();
                bus.AddJobSagaStateMachines(options => options.SlotWaitTime = TimeSpan.FromSeconds(10));
            }

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

                    if (enableNutritionGoalJob)
                        cfg.UseDelayedMessageScheduler();

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
