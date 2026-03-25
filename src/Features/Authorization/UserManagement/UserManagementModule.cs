using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;
using ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;

namespace ShapeUp.Features.Authorization.UserManagement;

public static class UserManagementModule
{
    public static void AddUserManagementServices(this IServiceCollection services)
    {
        services
            .AddAuthorizationDependencies();
    }

    private static void AddAuthorizationDependencies(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<GetOrCreateUserHandler>();
        services.AddScoped<RevokeCurrentTokenHandler>();
    }
}