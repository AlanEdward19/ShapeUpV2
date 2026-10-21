namespace ShapeUp.Features.Safety;

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared;
using ShapeUp.Shared.Privacy;

public static class SafetyModule
{
    public static IServiceCollection AddSafetyServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<SafetyDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IValidator<CreateReportCommand>, CreateReportValidator>();
        services.AddScoped<IValidator<ResolveReportCommand>, ResolveReportValidator>();
        services.AddScoped<ReportsHandler>();
        services.AddScoped<BlocksHandler>();
        services.AddScoped<IUserBlockLookup, UserBlockLookup>();
        services.AddScoped<ISafetyReportCounts, SafetyReportCounts>();
        services.AddScoped<IUserDataEraser, SafetyUserDataEraser>();
        return services;
    }
}
