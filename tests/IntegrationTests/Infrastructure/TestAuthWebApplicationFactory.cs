using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp;
using ShapeUp.Features.Authorization.Shared.Abstractions;

namespace IntegrationTests.Infrastructure;

public class TestAuthWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var config = new Dictionary<string, string?>
            {
                ["Database:DisableMigrationsOnStartup"] = "true",
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=shapeup-tests;Trusted_Connection=True;",
                ["Firebase:ProjectId"] = "shapeup-tests",
                ["Firebase:Credentials:type"] = "service_account",
                ["Firebase:Credentials:project_id"] = "shapeup-tests",
                ["Firebase:Credentials:private_key_id"] = "dummy",
                ["Firebase:Credentials:private_key"] = "-----BEGIN PRIVATE KEY-----\\nMIIB\n-----END PRIVATE KEY-----\\n",
                ["Firebase:Credentials:client_email"] = "tests@shapeup.local",
                ["Firebase:Credentials:client_id"] = "1234567890",
                ["Firebase:Credentials:auth_uri"] = "https://accounts.google.com/o/oauth2/auth",
                ["Firebase:Credentials:token_uri"] = "https://oauth2.googleapis.com/token",
                ["Firebase:Credentials:auth_provider_x509_cert_url"] = "https://www.googleapis.com/oauth2/v1/certs",
                ["Firebase:Credentials:client_x509_cert_url"] = "https://www.googleapis.com/robot/v1/metadata/x509/tests%40shapeup.local",
                ["Mongo:Training:ConnectionString"] = "mongodb://localhost:27017",
                ["Mongo:Training:DatabaseName"] = "shapeup_tests",
                ["Mongo:Training:WorkoutSessionsCollectionName"] = "workout_sessions_tests",
                ["Mongo:Training:WorkoutPlansCollectionName"] = "workout_plans_tests",
                ["Mongo:Training:WorkoutTemplatesCollectionName"] = "workout_templates_tests"
            };

            configBuilder.AddInMemoryCollection(config);
        });

        builder.ConfigureServices(services =>
        {
            ReplaceService<IFirebaseService, FakeFirebaseService>(services);
        });
    }

    private static void ReplaceService<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(TService));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddScoped<TService, TImplementation>();
    }
}


