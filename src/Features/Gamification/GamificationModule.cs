namespace ShapeUp.Features.Gamification;

using GetGamificationProfile;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.AntiCheat;

public static class GamificationModule
{
    public static IServiceCollection AddGamificationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<GamificationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAntiCheatClassifier, AntiCheatClassifier>();
        services.AddScoped<ShapeScoreCalculator>();
        services.AddScoped<GetGamificationProfileHandler>();

        return services;
    }
}
