namespace ShapeUp.Features.Memberships;

using Shared.Abstractions;
using Infrastructure;

public static class MembershipsModule
{
    public static IServiceCollection AddMembershipsServices(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipAdapter>();

        return services;
    }
}
