using ShapeUp.Features.PlatformAdmin.GetOverview;

namespace ShapeUp.Features.PlatformAdmin;

public static class PlatformAdminModule
{
    public static IServiceCollection AddPlatformAdminServices(this IServiceCollection services)
    {
        services.AddScoped<GetAdminOverviewHandler>();
        return services;
    }
}
