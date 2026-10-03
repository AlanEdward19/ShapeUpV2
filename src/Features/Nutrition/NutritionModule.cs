using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Clients.AcceptInvite;
using ShapeUp.Features.Nutrition.Clients.EndRelationship;
using ShapeUp.Features.Nutrition.Clients.ClientsAdherence;
using ShapeUp.Features.Nutrition.Clients.ListNutritionists;
using ShapeUp.Features.Nutrition.Comments.AddComment;
using ShapeUp.Features.Nutrition.Comments.GetComments;
using ShapeUp.Features.Nutrition.MealPlans.GetActiveMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.GetMealPlanById;
using ShapeUp.Features.Nutrition.MealPlans.GetMealPlans;
using ShapeUp.Features.Nutrition.MealPlanTemplates.AssignMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.CreateMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.DeleteMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.GetMealPlanTemplateById;
using ShapeUp.Features.Nutrition.MealPlanTemplates.GetMealPlanTemplates;
using ShapeUp.Features.Nutrition.MealPlanTemplates.UpdateMealPlanTemplate;
using ShapeUp.Features.Nutrition.Measurements.AddMeasurement;
using ShapeUp.Features.Nutrition.Measurements.GetMeasurements;
using ShapeUp.Features.Nutrition.Profile.SetDietaryRestrictions;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Clients.InviteClient;
using ShapeUp.Features.Nutrition.Clients.ListClients;
using ShapeUp.Features.Nutrition.Clients.ListInvites;
using ShapeUp.Features.Nutrition.Clients.RevokeInvite;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Nutrition.Fasting.CancelOverride;
using ShapeUp.Features.Nutrition.Fasting.EndOverrideEarly;
using ShapeUp.Features.Nutrition.Fasting.GetClock;
using ShapeUp.Features.Nutrition.Fasting.GetHistory;
using ShapeUp.Features.Nutrition.Fasting.PutAgenda;
using ShapeUp.Features.Nutrition.Fasting.SetRecommendation;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.StartOverride;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Hydration.GetHydrationDay;
using ShapeUp.Features.Nutrition.Hydration.GetHydrationRange;
using ShapeUp.Features.Nutrition.Hydration.SetHydrationDay;
using ShapeUp.Features.Nutrition.Infrastructure.Mongo;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Foods.CreateFood;
using ShapeUp.Features.Nutrition.Foods.CreateFoodOverride;
using ShapeUp.Features.Nutrition.Foods.DeleteFood;
using ShapeUp.Features.Nutrition.Foods.GetFoodByBarcode;
using ShapeUp.Features.Nutrition.Foods.SearchFoods;
using ShapeUp.Features.Nutrition.Foods.SetActiveFoodVersion;
using ShapeUp.Features.Nutrition.Diary.AddDiaryEntry;
using ShapeUp.Features.Nutrition.Diary.GetDiaryDay;
using ShapeUp.Features.Nutrition.Diary.RemoveDiaryEntry;
using ShapeUp.Features.Nutrition.Diary.SubstituteDiaryItem;
using ShapeUp.Features.Nutrition.Diary.SuggestSubstitute;
using ShapeUp.Features.Nutrition.MealPlans.ActivateMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;
using ShapeUp.Features.Nutrition.Moderation.DecideModeration;
using ShapeUp.Features.Nutrition.Moderation.GetPendingModerations;
using ShapeUp.Features.Nutrition.Moderation.Shared.Options;
using ShapeUp.Features.Nutrition.Profile.CompleteOnboarding;
using ShapeUp.Features.Nutrition.Profile.GetNutritionProfile;
using ShapeUp.Features.Nutrition.Profile.SetManualGoal;
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

        services.TryAddSingleton<IUtcClock, SystemUtcClock>();
        services.AddSingleton<FastingClockCalculator>();
        services.AddScoped<FastingFeatureGuard>();

        services.AddScoped<PutFastingAgendaHandler>();
        services.AddScoped<IValidator<PutFastingAgendaCommand>, PutFastingAgendaCommandValidator>();
        services.AddScoped<GetFastingClockHandler>();
        services.AddScoped<StartFastingOverrideHandler>();
        services.AddScoped<EndFastingOverrideEarlyHandler>();
        services.AddScoped<CancelFastingOverrideHandler>();
        services.AddScoped<SetFastingRecommendationHandler>();
        services.AddScoped<IValidator<SetFastingRecommendationCommand>, SetFastingRecommendationCommandValidator>();
        services.AddScoped<GetFastingHistoryHandler>();
        services.AddScoped<IValidator<GetFastingHistoryQuery>, GetFastingHistoryQueryValidator>();

        services.Configure<NutritionMongoOptions>(configuration.GetSection(NutritionMongoOptions.SectionName));
        services.Configure<NutritionModerationEmailOptions>(configuration.GetSection(NutritionModerationEmailOptions.SectionName));
        services.TryAddSingleton<IMongoClient>(_ =>
        {
            var mongoConnection = configuration[$"{NutritionMongoOptions.SectionName}:ConnectionString"]
                ?? configuration[$"{Training.Infrastructure.Mongo.TrainingMongoOptions.SectionName}:ConnectionString"];
            return new MongoClient(mongoConnection);
        });

        services.AddScoped<IWeightTrackingRepository, MongoWeightTrackingRepository>();
        services.AddScoped<IHydrationRepository, MongoHydrationRepository>();
        services.AddScoped<IFoodRepository, MongoFoodRepository>();
        services.AddScoped<IFoodOverrideRepository, MongoFoodOverrideRepository>();
        services.AddScoped<IFoodModerationRepository, MongoFoodModerationRepository>();
        services.AddScoped<IMealPlanRepository, MongoMealPlanRepository>();
        services.AddScoped<IMealPlanTemplateRepository, MongoMealPlanTemplateRepository>();

        services.AddScoped<UpsertTargetWeightHandler>();
        services.AddScoped<IValidator<UpsertTargetWeightCommand>, UpsertTargetWeightCommandValidator>();
        services.AddScoped<UpsertDailyWeightRegisterHandler>();
        services.AddScoped<IValidator<UpsertDailyWeightRegisterCommand>, UpsertDailyWeightRegisterCommandValidator>();
        services.AddScoped<SetHydrationDayHandler>();
        services.AddScoped<IValidator<SetHydrationDayCommand>, SetHydrationDayCommandValidator>();
        services.AddScoped<GetHydrationDayHandler>();
        services.AddScoped<GetHydrationRangeHandler>();
        services.AddScoped<IValidator<GetHydrationRangeQuery>, GetHydrationRangeQueryValidator>();
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

        services.AddScoped<GetNutritionProfileHandler>();
        services.AddScoped<CompleteOnboardingHandler>();
        services.AddScoped<IValidator<CompleteOnboardingCommand>, CompleteOnboardingCommandValidator>();
        services.AddScoped<SetManualGoalHandler>();
        services.AddScoped<IValidator<SetManualGoalCommand>, SetManualGoalCommandValidator>();

        services.AddScoped<AddDiaryEntryHandler>();
        services.AddScoped<IValidator<AddDiaryEntryCommand>, AddDiaryEntryCommandValidator>();
        services.AddScoped<RemoveDiaryEntryHandler>();
        services.AddScoped<IValidator<RemoveDiaryEntryCommand>, RemoveDiaryEntryCommandValidator>();
        services.AddScoped<GetDiaryDayHandler>();
        services.AddScoped<IValidator<GetDiaryDayQuery>, GetDiaryDayQueryValidator>();

        services.AddScoped<CreateMealPlanHandler>();
        services.AddScoped<IValidator<CreateMealPlanCommand>, CreateMealPlanCommandValidator>();
        services.AddScoped<ActivateMealPlanHandler>();
        services.AddScoped<IValidator<ActivateMealPlanCommand>, ActivateMealPlanCommandValidator>();

        services.AddScoped<INutritionAccessPolicy, NutritionAccessPolicy>();
        services.AddScoped<NutritionClientAccess>();
        services.AddScoped<ListNutritionClientsHandler>();
        services.AddScoped<InviteNutritionClientHandler>();
        services.AddScoped<AcceptNutritionInviteHandler>();
        services.AddScoped<ListNutritionInvitesHandler>();
        services.AddScoped<RevokeNutritionInviteHandler>();
        services.AddScoped<EndNutritionRelationshipHandler>();
        services.AddScoped<ListMyNutritionistsHandler>();
        services.AddScoped<GetClientsAdherenceHandler>();

        services.AddScoped<GetMealPlansHandler>();
        services.AddScoped<GetMealPlanByIdHandler>();
        services.AddScoped<GetActiveMealPlanHandler>();

        services.AddScoped<CreateMealPlanTemplateHandler>();
        services.AddScoped<UpdateMealPlanTemplateHandler>();
        services.AddScoped<IValidator<SaveMealPlanTemplateCommand>, SaveMealPlanTemplateCommandValidator>();
        services.AddScoped<GetMealPlanTemplatesHandler>();
        services.AddScoped<GetMealPlanTemplateByIdHandler>();
        services.AddScoped<DeleteMealPlanTemplateHandler>();
        services.AddScoped<AssignMealPlanTemplateHandler>();

        services.AddScoped<SetDietaryRestrictionsHandler>();
        services.AddScoped<IValidator<SetDietaryRestrictionsCommand>, SetDietaryRestrictionsCommandValidator>();

        services.AddScoped<AddMeasurementHandler>();
        services.AddScoped<IValidator<AddMeasurementCommand>, AddMeasurementCommandValidator>();
        services.AddScoped<GetMeasurementsHandler>();

        services.AddScoped<AddDiaryCommentHandler>();
        services.AddScoped<IValidator<AddDiaryCommentCommand>, AddDiaryCommentCommandValidator>();
        services.AddScoped<GetDiaryCommentsHandler>();

        services.AddScoped<SuggestSubstituteHandler>();
        services.AddScoped<IValidator<SuggestSubstituteQuery>, SuggestSubstituteQueryValidator>();
        services.AddScoped<SubstituteDiaryItemHandler>();
        services.AddScoped<IValidator<SubstituteDiaryItemCommand>, SubstituteDiaryItemCommandValidator>();

        return services;
    }
}
