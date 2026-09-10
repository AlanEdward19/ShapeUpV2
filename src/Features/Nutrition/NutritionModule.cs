using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Infrastructure.Mongo;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Foods.CreateFood;
using ShapeUp.Features.Nutrition.Foods.CreateFoodOverride;
using ShapeUp.Features.Nutrition.Foods.DeleteFood;
using ShapeUp.Features.Nutrition.Foods.GetFoodByBarcode;
using ShapeUp.Features.Nutrition.Foods.SearchFoods;
using ShapeUp.Features.Nutrition.Foods.SetActiveFoodVersion;
using ShapeUp.Features.Nutrition.Moderation.DecideModeration;
using ShapeUp.Features.Nutrition.Moderation.GetPendingModerations;
using ShapeUp.Features.Nutrition.WeightTracking.GetWeightRegisters;
using ShapeUp.Features.Nutrition.WeightTracking.UpsertDailyWeightRegister;
using ShapeUp.Features.Nutrition.WeightTracking.UpsertTargetWeight;

namespace ShapeUp.Features.Nutrition;

public static class NutritionModule
{
    public static IServiceCollection AddNutritionServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<NutritionDbContext>(options => options.UseSqlServer(connectionString));

        services.Configure<NutritionMongoOptions>(configuration.GetSection(NutritionMongoOptions.SectionName));
        services.TryAddSingleton<IMongoClient>(_ =>
        {
            var mongoConnection = configuration[$"{NutritionMongoOptions.SectionName}:ConnectionString"]
                ?? configuration[$"{Training.Infrastructure.Mongo.TrainingMongoOptions.SectionName}:ConnectionString"];
            return new MongoClient(mongoConnection);
        });

        services.AddScoped<IWeightTrackingRepository, MongoWeightTrackingRepository>();
        services.AddScoped<IFoodRepository, MongoFoodRepository>();
        services.AddScoped<IFoodOverrideRepository, MongoFoodOverrideRepository>();
        services.AddScoped<IFoodModerationRepository, MongoFoodModerationRepository>();
        services.AddScoped<IMealPlanRepository, MongoMealPlanRepository>();

        services.AddScoped<UpsertTargetWeightHandler>();
        services.AddScoped<IValidator<UpsertTargetWeightCommand>, UpsertTargetWeightCommandValidator>();
        services.AddScoped<UpsertDailyWeightRegisterHandler>();
        services.AddScoped<IValidator<UpsertDailyWeightRegisterCommand>, UpsertDailyWeightRegisterCommandValidator>();
        services.AddScoped<GetWeightRegistersHandler>();
        services.AddScoped<IValidator<GetWeightRegistersQuery>, GetWeightRegistersQueryValidator>();

        services.AddScoped<CreateFoodHandler>();
        services.AddScoped<IValidator<CreateFoodCommand>, CreateFoodCommandValidator>();
        services.AddScoped<SearchFoodsHandler>();
        services.AddScoped<IValidator<SearchFoodsQuery>, SearchFoodsQueryValidator>();
        services.AddScoped<GetFoodByBarcodeHandler>();
        services.AddScoped<IValidator<GetFoodByBarcodeQuery>, GetFoodByBarcodeQueryValidator>();
        services.AddScoped<CreateFoodOverrideHandler>();
        services.AddScoped<IValidator<CreateFoodOverrideCommand>, CreateFoodOverrideCommandValidator>();
        services.AddScoped<SetActiveFoodVersionHandler>();
        services.AddScoped<IValidator<SetActiveFoodVersionCommand>, SetActiveFoodVersionCommandValidator>();
        services.AddScoped<DeleteFoodHandler>();
        services.AddScoped<GetPendingModerationsHandler>();
        services.AddScoped<IValidator<GetPendingModerationsQuery>, GetPendingModerationsQueryValidator>();
        services.AddScoped<DecideModerationHandler>();
        services.AddScoped<IValidator<DecideModerationCommand>, DecideModerationCommandValidator>();

        return services;
    }
}
