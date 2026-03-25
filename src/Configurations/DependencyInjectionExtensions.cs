using ShapeUp.Features.Authorization.Groups;
using ShapeUp.Features.Authorization.Scopes;
using ShapeUp.Features.Authorization.UserManagement;

namespace ShapeUp.Configurations;

using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FluentValidation;
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

        if (!string.IsNullOrWhiteSpace(firebaseProjectId))
        {
            try
            {
                _ = FirebaseApp.DefaultInstance;
            }
            catch (InvalidOperationException)
            {
                FirebaseApp.Create(new AppOptions
                {
                    ProjectId = firebaseProjectId
                });
            }
        }

        services.AddScoped<IFirebaseService, FirebaseService>(_ =>
            new FirebaseService(FirebaseAuth.DefaultInstance));

        return services;
    }

    private static IServiceCollection AddAuthorizationDependencies(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<GetOrCreateUserValidator>();
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