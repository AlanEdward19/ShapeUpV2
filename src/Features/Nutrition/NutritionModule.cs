using FluentValidation;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Infrastructure.Mongo;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.WeightTracking.GetWeightRegisters;
using ShapeUp.Features.Nutrition.WeightTracking.UpsertDailyWeightRegister;
using ShapeUp.Features.Nutrition.WeightTracking.UpsertTargetWeight;

namespace ShapeUp.Features.Nutrition;

public static class NutritionModule
{
    public static IServiceCollection AddNutritionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NutritionMongoOptions>(configuration.GetSection(NutritionMongoOptions.SectionName));
        services.AddSingleton<IMongoClient>(_ =>
        {
            var mongoConnection = configuration[$"{NutritionMongoOptions.SectionName}:ConnectionString"];
            return new MongoClient(mongoConnection);
        });

        services.AddScoped<IWeightTrackingRepository, MongoWeightTrackingRepository>();

        services.AddScoped<UpsertTargetWeightHandler>();
        services.AddScoped<IValidator<UpsertTargetWeightCommand>, UpsertTargetWeightCommandValidator>();
        services.AddScoped<UpsertDailyWeightRegisterHandler>();
        services.AddScoped<IValidator<UpsertDailyWeightRegisterCommand>, UpsertDailyWeightRegisterCommandValidator>();
        services.AddScoped<GetWeightRegistersHandler>();
        services.AddScoped<IValidator<GetWeightRegistersQuery>, GetWeightRegistersQueryValidator>();

        return services;
    }
}
