namespace ShapeUp.Features.Gamification;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.AntiCheat;

public static class GamificationModule
{
    public static IServiceCollection AddGamificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<GamificationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAntiCheatClassifier, AntiCheatClassifier>();

        return services;
    }
}
