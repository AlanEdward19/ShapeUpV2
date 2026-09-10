using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IntegrationTests.Domains.Messaging;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Data;
using ShapeUp.Features.Training.Infrastructure.Data;

namespace IntegrationTests.Infrastructure;

public sealed class IntegrationWebApplicationFactory(SqlServerFixture fixture) : WebApplicationFactory<Program>
{
    private readonly string _mongoDatabaseName = $"shapeup-integration-{Guid.NewGuid():N}";
    private readonly string _endpointPrefix = $"int-{Guid.NewGuid():N}"[..16];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Messaging:Transport", "InMemory");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = bool.FalseString,
                ["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString,
                ["Firebase:ProjectId"] = "shapeup-integration-tests",
                ["Notifications:Resend:ApiToken"] = "integration-test-token",
                ["Notifications:Resend:FromEmail"] = "notifications@integration.test",
                ["Notifications:Resend:FromName"] = "ShapeUp Integration",
                ["Mongo:Training:ConnectionString"] = fixture.MongoConnectionString,
                ["Mongo:Training:DatabaseName"] = _mongoDatabaseName,
                ["Mongo:Training:WorkoutSessionsCollectionName"] = "workout_sessions",
                ["Mongo:Nutrition:ConnectionString"] = fixture.MongoConnectionString,
                ["Mongo:Nutrition:DatabaseName"] = _mongoDatabaseName,
                ["Mongo:Nutrition:WeightTargetsCollectionName"] = "weight_targets",
                ["Mongo:Nutrition:WeightRegistersCollectionName"] = "weight_registers",
                ["Mongo:Nutrition:FoodsCollectionName"] = "foods",
                ["Mongo:Nutrition:FoodOverridesCollectionName"] = "food_overrides",
                ["Mongo:Nutrition:FoodModerationRequestsCollectionName"] = "food_moderation_requests",
                ["Mongo:Nutrition:MealPlansCollectionName"] = "meal_plans",
                ["Messaging:Transport"] = "InMemory",
                ["Messaging:EndpointPrefix"] = _endpointPrefix
            });
        });

        builder.ConfigureServices(services =>
        {
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

            services.AddDbContext<AuthorizationDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<AuditLogsDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<GymManagementDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<TrainingDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            // native-authorization-model Phase 3: RelationshipsDbContext backs ITrainingAccessPolicy's
            // cross-user checks. Earlier Phase 2 controllers only ever exercised self-access paths over
            // HTTP, so this override was never needed until WorkoutPlans/WorkoutTemplates/Workouts tests
            // started hitting a real professional-client relationship lookup — without it, the app fell
            // back to whatever connection string ApplyMigrationsOnStartup/appsettings has for
            // DefaultConnection, which doesn't point at the test container.
            services.AddDbContext<RelationshipsDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<GamificationDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<NutritionDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<PlatformFeatureFlagsDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddSingleton<IFirebaseService, TestFirebaseService>();
            services.AddSingleton<TestEmailNotificationSender>();
            services.AddSingleton<IEmailNotificationSender>(sp => sp.GetRequiredService<TestEmailNotificationSender>());
            services.AddSingleton<ILoggerProvider, WorkoutFinishedConsumerLogCapture>();

            services.AddOptions<MassTransitHostOptions>()
                .Configure(options =>
                {
                    options.WaitUntilStarted = true;
                    options.StartTimeout = TimeSpan.FromSeconds(30);
                    options.StopTimeout = TimeSpan.FromSeconds(30);
                });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Transport"] = "InMemory",
                ["Messaging:EndpointPrefix"] = _endpointPrefix,
                ["Mongo:Training:ConnectionString"] = fixture.MongoConnectionString,
                ["Mongo:Training:DatabaseName"] = _mongoDatabaseName,
                ["Mongo:Nutrition:ConnectionString"] = fixture.MongoConnectionString,
                ["Mongo:Nutrition:DatabaseName"] = _mongoDatabaseName
            });
        });

        var host = base.CreateHost(builder);
        host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        catch (NullReferenceException ex) when (ex.StackTrace?.Contains("BusDepotAgentSupervisor", StringComparison.Ordinal) == true)
        {
            // MassTransit 9.x InMemory + Mongo outbox can NRE during hosted-service stop in test teardown.
        }
    }

    public new async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        catch (NullReferenceException ex) when (ex.StackTrace?.Contains("BusDepotAgentSupervisor", StringComparison.Ordinal) == true)
        {
            // MassTransit 9.x InMemory + Mongo outbox can NRE during hosted-service stop in test teardown.
        }
    }
}
