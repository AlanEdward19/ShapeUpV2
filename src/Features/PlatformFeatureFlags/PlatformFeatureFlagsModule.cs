namespace ShapeUp.Features.PlatformFeatureFlags;

using FluentValidation;
using GetFeatureFlags;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using SetFeatureFlag;
using Shared.Abstractions;

public static class PlatformFeatureFlagsModule
{
    public static IServiceCollection AddPlatformFeatureFlagsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<PlatformFeatureFlagsDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IFeatureFlagReader, FeatureFlagReader>();
        services.AddScoped<GetFeatureFlagsHandler>();
        services.AddScoped<SetFeatureFlagHandler>();
        services.AddScoped<IValidator<SetFeatureFlagCommand>, SetFeatureFlagCommandValidator>();

        return services;
    }
}
