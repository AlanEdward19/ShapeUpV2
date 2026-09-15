namespace IntegrationTests.Domains.Messaging;

using IntegrationTests.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ShapeUp.Configurations;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data;
using ShapeUp.Features.Relationships.Shared.Data;
using ShapeUp.Features.Training.Infrastructure.Data;

public sealed class MessagingIntegrationWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqlServerFixture _sqlFixture;
    private readonly string _mongoDatabaseName = $"shapeup-messaging-{Guid.NewGuid():N}";
    private readonly string _endpointPrefix = $"msg-{Guid.NewGuid():N}"[..16];

    public MessagingIntegrationWebApplicationFactory(SqlServerFixture sqlFixture)
    {
        _sqlFixture = sqlFixture;
    }

    public string MongoDatabaseName => _mongoDatabaseName;

    public TestOutboxFaultInjector FaultInjector { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:DisableMigrationsOnStartup", bool.TrueString);
        builder.UseSetting("ConnectionStrings:DefaultConnection", _sqlFixture.ConnectionString);
        builder.UseSetting("RabbitMQ:Host", MessagingInfraFixture.RabbitHost);
        builder.UseSetting("RabbitMQ:Port", MessagingInfraFixture.RabbitPort.ToString());
        builder.UseSetting("Mongo:Training:ConnectionString", MessagingInfraFixture.MongoConnectionString);
        builder.UseSetting("Mongo:Training:DatabaseName", _mongoDatabaseName);
        builder.UseSetting("Mongo:Nutrition:ConnectionString", MessagingInfraFixture.MongoConnectionString);
        builder.UseSetting("Mongo:Nutrition:DatabaseName", _mongoDatabaseName);
        builder.UseSetting("Messaging:EndpointPrefix", _endpointPrefix);
        builder.UseSetting("Messaging:EnableNutritionGoalJob", bool.FalseString);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:DisableMigrationsOnStartup"] = bool.TrueString,
                ["ConnectionStrings:DefaultConnection"] = _sqlFixture.ConnectionString,
                ["Firebase:ProjectId"] = "shapeup-integration-tests",
                ["Notifications:Resend:ApiToken"] = "integration-test-token",
                ["Notifications:Resend:FromEmail"] = "notifications@integration.test",
                ["Notifications:Resend:FromName"] = "ShapeUp Integration",
                ["Mongo:Training:ConnectionString"] = MessagingInfraFixture.MongoConnectionString,
                ["Mongo:Training:DatabaseName"] = _mongoDatabaseName,
                ["Mongo:Training:WorkoutSessionsCollectionName"] = "workout_sessions",
                ["Mongo:Nutrition:ConnectionString"] = MessagingInfraFixture.MongoConnectionString,
                ["Mongo:Nutrition:DatabaseName"] = _mongoDatabaseName,
                ["RabbitMQ:Host"] = MessagingInfraFixture.RabbitHost,
                ["RabbitMQ:Port"] = MessagingInfraFixture.RabbitPort.ToString(),
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest",
                ["Messaging:EndpointPrefix"] = _endpointPrefix,
                // Keep Messaging E2E hosts off the nutrition recurring job saga; that path is covered by
                // NutritionGoalEvaluationJobConsumerIntegrationTests and hangs suite teardown when every
                // Messaging factory pays for job-service start/stop over a real RabbitMQ container.
                ["Messaging:EnableNutritionGoalJob"] = bool.FalseString
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddOptions<MassTransitHostOptions>()
                .Configure(options =>
                {
                    options.WaitUntilStarted = true;
                    options.StartTimeout = TimeSpan.FromSeconds(30);
                    options.StopTimeout = TimeSpan.FromSeconds(30);
                });
            services.Configure<HostOptions>(options =>
            {
                options.ShutdownTimeout = TimeSpan.FromSeconds(30);
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });

            services.RemoveAll(typeof(DbContextOptions<AuthorizationDbContext>));
            services.RemoveAll(typeof(DbContextOptions<AuditLogsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<GymManagementDbContext>));
            services.RemoveAll(typeof(DbContextOptions<TrainingDbContext>));
            services.RemoveAll(typeof(DbContextOptions<RelationshipsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<GamificationDbContext>));
            services.RemoveAll(typeof(DbContextOptions<NutritionDbContext>));
            services.RemoveAll(typeof(DbContextOptions<PlatformFeatureFlagsDbContext>));
            services.RemoveAll<IFirebaseService>();
            services.RemoveAll<IEmailNotificationSender>();
            services.RemoveAll<IOutboxFaultInjector>();
            services.RemoveAll<IMongoClient>();
            services.AddSingleton<IMongoClient>(_ => new MongoClient(MessagingInfraFixture.MongoConnectionString));

            services.AddDbContext<AuthorizationDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<AuditLogsDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<GymManagementDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<TrainingDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<RelationshipsDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<GamificationDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<NutritionDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<PlatformFeatureFlagsDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddSingleton<IFirebaseService, TestFirebaseService>();
            services.AddSingleton<TestEmailNotificationSender>();
            services.AddSingleton<IEmailNotificationSender>(sp => sp.GetRequiredService<TestEmailNotificationSender>());
            services.AddSingleton<IOutboxFaultInjector>(FaultInjector);
            services.AddSingleton<ILoggerProvider, WorkoutFinishedConsumerLogCapture>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:DisableMigrationsOnStartup"] = bool.TrueString,
                ["ConnectionStrings:DefaultConnection"] = _sqlFixture.ConnectionString,
                ["RabbitMQ:Host"] = MessagingInfraFixture.RabbitHost,
                ["RabbitMQ:Port"] = MessagingInfraFixture.RabbitPort.ToString(),
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest",
                ["Mongo:Training:ConnectionString"] = MessagingInfraFixture.MongoConnectionString,
                ["Mongo:Training:DatabaseName"] = _mongoDatabaseName,
                ["Mongo:Nutrition:ConnectionString"] = MessagingInfraFixture.MongoConnectionString,
                ["Mongo:Nutrition:DatabaseName"] = _mongoDatabaseName,
                ["Messaging:EndpointPrefix"] = _endpointPrefix,
                ["Messaging:EnableNutritionGoalJob"] = bool.FalseString
            });
        });

        return base.CreateHost(builder);
    }

    private static bool IsKnownMassTransitTeardownFault(Exception ex) =>
        ex is NullReferenceException or TaskCanceledException
        && ex.StackTrace?.Contains("MassTransit", StringComparison.Ordinal) == true;

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        catch (Exception ex) when (IsKnownMassTransitTeardownFault(ex))
        {
            // MassTransit InMemory/Rabbit + Mongo outbox can fault during hosted-service stop in test teardown.
        }
    }

    public new async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        catch (Exception ex) when (IsKnownMassTransitTeardownFault(ex))
        {
            // MassTransit InMemory/Rabbit + Mongo outbox can fault during hosted-service stop in test teardown.
        }
    }
}
