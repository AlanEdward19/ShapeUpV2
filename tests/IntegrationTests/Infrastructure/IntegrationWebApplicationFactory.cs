using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.Training.Infrastructure.Data;

namespace IntegrationTests.Infrastructure;

public sealed class IntegrationWebApplicationFactory(SqlServerFixture fixture) : WebApplicationFactory<Program>
{
    private readonly string _mongoDatabaseName = $"shapeup-integration-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = bool.FalseString,
                ["ConnectionStrings:DefaultConnection"] = fixture.ConnectionString,
                ["Firebase:ProjectId"] = "shapeup-integration-tests",
                ["Mongo:Training:ConnectionString"] = fixture.MongoConnectionString,
                ["Mongo:Training:DatabaseName"] = _mongoDatabaseName,
                ["Mongo:Training:WorkoutSessionsCollectionName"] = "workout_sessions"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AuthorizationDbContext>));
            services.RemoveAll(typeof(DbContextOptions<AuditLogsDbContext>));
            services.RemoveAll(typeof(DbContextOptions<GymManagementDbContext>));
            services.RemoveAll(typeof(DbContextOptions<TrainingDbContext>));
            services.RemoveAll<IFirebaseService>();

            services.AddDbContext<AuthorizationDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<AuditLogsDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<GymManagementDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddDbContext<TrainingDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
            services.AddSingleton<IFirebaseService, TestFirebaseService>();
        });
    }
}

