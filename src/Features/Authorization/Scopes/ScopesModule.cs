using ShapeUp.Features.Authorization.Groups.AssignScopeToGroup;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Scopes.AssignScopeToUser;
using ShapeUp.Features.Authorization.Scopes.CreateScope;
using ShapeUp.Features.Authorization.Scopes.GetScopes;
using ShapeUp.Features.Authorization.Scopes.GetUserScopes;
using ShapeUp.Features.Authorization.Scopes.RemoveScopeFromUser;
using ShapeUp.Features.Authorization.Shared.Abstractions;

namespace ShapeUp.Features.Authorization.Scopes;

public static class ScopesModule
{
    public static IServiceCollection AddScopeServices(this IServiceCollection services)
    {
        services
            .AddAuthorizationDependencies();
        
        return services;
    }

    private static void AddAuthorizationDependencies(this IServiceCollection services)
    {
        services.AddScoped<IScopeRepository, ScopeRepository>();
        
        services.AddScoped<CreateScopeHandler>();
        services.AddScoped<GetScopesHandler>();
        services.AddScoped<GetUserScopesHandler>();
        services.AddScoped<AssignScopeToUserHandler>();
        services.AddScoped<RemoveScopeFromUserHandler>();
        services.AddScoped<AssignScopeToGroupHandler>();
    }
}