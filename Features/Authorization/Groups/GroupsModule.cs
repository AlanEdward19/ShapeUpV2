using ShapeUp.Features.Authorization.Groups.AddUserToGroup;
using ShapeUp.Features.Authorization.Groups.CreateGroup;
using ShapeUp.Features.Authorization.Groups.DeleteGroup;
using ShapeUp.Features.Authorization.Groups.GetGroups;
using ShapeUp.Features.Authorization.Groups.RemoveUserFromGroup;
using ShapeUp.Features.Authorization.Groups.UpdateUserRole;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Shared.Abstractions;

namespace ShapeUp.Features.Authorization.Groups;

public static class GroupsModule
{
    public static IServiceCollection AddGroupServices(this IServiceCollection services)
    {
        services
            .AddAuthorizationDependencies();
        
        return services;
    }

    private static void AddAuthorizationDependencies(this IServiceCollection services)
    {
        services.AddScoped<IGroupRepository, GroupRepository>();
        
        services.AddScoped<CreateGroupHandler>();
        services.AddScoped<GetGroupsHandler>();
        services.AddScoped<AddUserToGroupHandler>();
        services.AddScoped<RemoveUserFromGroupHandler>();
        services.AddScoped<UpdateUserRoleHandler>();
        services.AddScoped<DeleteGroupHandler>();
    }
}