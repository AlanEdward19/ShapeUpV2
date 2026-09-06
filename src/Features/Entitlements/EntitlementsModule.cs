namespace ShapeUp.Features.Entitlements;

using Shared.Abstractions;
using Infrastructure;

public static class EntitlementsModule
{
    public static IServiceCollection AddEntitlementsServices(this IServiceCollection services)
    {
        services.AddScoped<IEntitlementRepository, EntitlementAdapter>();

        return services;
    }
}
