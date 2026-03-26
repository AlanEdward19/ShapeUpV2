using ShapeUp.Features.Authorization.Groups;
using ShapeUp.Features.Authorization.Scopes;
using ShapeUp.Features.Authorization.UserManagement;
using ShapeUp.Features.GymManagement;

namespace ShapeUp.Configurations;

using System.Text.Json;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FluentValidation;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Features.AuditLogs.GetAuditLogs;
using Features.AuditLogs.Infrastructure.Auditing;
using Features.AuditLogs.Infrastructure.Repositories;
using ShapeUp.Features.AuditLogs.Shared.Abstractions;
using ShapeUp.Features.AuditLogs.Shared.Data;
using Features.Authorization.Infrastructure.Authorization;
using Features.Authorization.Infrastructure.Firebase;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Data;
using Features.Authorization.UserManagement.GetOrCreateUser;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddFirebase(configuration);
        services.AddAuthorizationDependencies();
        services.AddAuditLogsDependencies();
        services.AddGymManagementServices(configuration);

        services.AddControllers();
        services.AddOpenApi();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AuthorizationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddDbContext<AuditLogsDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    private static IServiceCollection AddFirebase(this IServiceCollection services, IConfiguration configuration)
    {
        var firebaseProjectId = configuration["Firebase:ProjectId"];
        if (string.IsNullOrWhiteSpace(firebaseProjectId))
            throw new InvalidOperationException(
                "Firebase configuration is invalid. 'Firebase:ProjectId' was not found.");

        services.AddSingleton(_ => CreateFirebaseApp(configuration, firebaseProjectId));
        services.AddSingleton(sp => FirebaseAuth.GetAuth(sp.GetRequiredService<FirebaseApp>()));
        services.AddScoped<IFirebaseService, FirebaseService>();

        return services;
    }

    private static FirebaseApp CreateFirebaseApp(IConfiguration configuration, string firebaseProjectId)
    {
        try
        {
            var appOptions = new AppOptions
            {
                ProjectId = firebaseProjectId,
                Credential = CreateGoogleCredential(configuration)
            };

            return FirebaseApp.Create(appOptions);
        }
        catch (InvalidOperationException)
        {
            var appOptions = new AppOptions
            {
                ProjectId = firebaseProjectId,
                Credential = CreateGoogleCredential(configuration)
            };

            return FirebaseApp.Create(appOptions);
        }
    }

    private static GoogleCredential CreateGoogleCredential(IConfiguration configuration)
    {
        var credentialsSection = configuration.GetSection("Firebase:Credentials");
        if (!credentialsSection.Exists())
            throw new InvalidOperationException(
                "Firebase configuration is invalid. Section 'Firebase:Credentials' was not found. Configure it in user secrets or environment settings.");

        var credentialsDict = credentialsSection
            .GetChildren()
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToDictionary(x => x.Key, x => x.Value!);

        if (credentialsDict.Count == 0)
            throw new InvalidOperationException(
                "Firebase configuration is invalid. Section 'Firebase:Credentials' is empty.");

        var credentialsJson = JsonSerializer.Serialize(credentialsDict);
        return GoogleCredential.FromJson(credentialsJson);
    }

    private static IServiceCollection AddAuthorizationDependencies(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<GetUserValidator>();
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Stop;

        services
            .AddGroupServices()
            .AddScopeServices()
            .AddUserManagementServices();

        services.AddScoped<AuthorizationMiddleware>();

        return services;
    }

    private static IServiceCollection AddAuditLogsDependencies(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<GetAuditLogsHandler>();
        services.AddScoped<AuditLoggingMiddleware>();

        return services;
    }
}