namespace IntegrationTests.Domains.Messaging;

using IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ShapeUp.Configurations;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.Notifications.Shared.Abstractions;
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

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = bool.FalseString,
                ["ConnectionStrings:DefaultConnection"] = _sqlFixture.ConnectionString,
                ["Firebase:ProjectId"] = "shapeup-integration-tests",
                ["Notifications:Resend:ApiToken"] = "integration-test-token",
                ["Notifications:Resend:FromEmail"] = "notifications@integration.test",
                ["Notifications:Resend:FromName"] = "ShapeUp Integration",
                ["Mongo:Training:ConnectionString"] = MessagingInfraFixture.MongoConnectionString,
                ["Mongo:Training:DatabaseName"] = _mongoDatabaseName,
                ["Mongo:Training:WorkoutSessionsCollectionName"] = "workout_sessions",
                ["RabbitMQ:Host"] = MessagingInfraFixture.RabbitHost,
                ["RabbitMQ:Username"] = "guest",
                ["RabbitMQ:Password"] = "guest",
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
            services.RemoveAll<IFirebaseService>();
            services.RemoveAll<IEmailNotificationSender>();
            services.RemoveAll<IOutboxFaultInjector>();

            services.AddDbContext<AuthorizationDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<AuditLogsDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<GymManagementDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<TrainingDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddDbContext<RelationshipsDbContext>(options => options.UseSqlServer(_sqlFixture.ConnectionString));
            services.AddSingleton<IFirebaseService, TestFirebaseService>();
            services.AddSingleton<TestEmailNotificationSender>();
            services.AddSingleton<IEmailNotificationSender>(sp => sp.GetRequiredService<TestEmailNotificationSender>());
            services.AddSingleton<IOutboxFaultInjector>(FaultInjector);
            services.AddSingleton<ILoggerProvider, WorkoutFinishedConsumerLogCapture>();
        });
    }
}
