using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using ShapeUp.Features.Training.Dashboard.GetTrainingDashboard;
using ShapeUp.Features.Training.Equipments.CreateEquipment;
using ShapeUp.Features.Training.Equipments.DeleteEquipment;
using ShapeUp.Features.Training.Equipments.GetEquipmentById;
using ShapeUp.Features.Training.Equipments.GetEquipments;
using ShapeUp.Features.Training.Equipments.UpdateEquipment;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.DeleteExercise;
using ShapeUp.Features.Training.Exercises.GetExerciseById;
using ShapeUp.Features.Training.Exercises.GetExercises;
using ShapeUp.Features.Training.Exercises.SuggestExercise;
using ShapeUp.Features.Training.Exercises.UpdateExercise;
using ShapeUp.Features.Training.Infrastructure.Data;
using ShapeUp.Features.Training.Infrastructure.Mongo;
using ShapeUp.Features.Training.Infrastructure.Policies;
using ShapeUp.Features.Training.Infrastructure.Repositories;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.WorkoutPlans.CopyWorkoutPlan;
using ShapeUp.Features.Training.WorkoutPlans.CreateWorkoutPlan;
using ShapeUp.Features.Training.WorkoutPlans.DeleteWorkoutPlan;
using ShapeUp.Features.Training.WorkoutPlans.GetWorkoutPlanById;
using ShapeUp.Features.Training.WorkoutPlans.GetWorkoutPlansByUser;
using ShapeUp.Features.Training.WorkoutPlans.UpdateWorkoutPlan;
using ShapeUp.Features.Training.WorkoutTemplates.AssignWorkoutTemplate;
using ShapeUp.Features.Training.WorkoutTemplates.CopyWorkoutTemplate;
using ShapeUp.Features.Training.WorkoutTemplates.CreateWorkoutTemplate;
using ShapeUp.Features.Training.WorkoutTemplates.DeleteWorkoutTemplate;
using ShapeUp.Features.Training.WorkoutTemplates.GetWorkoutTemplateById;
using ShapeUp.Features.Training.WorkoutTemplates.GetWorkoutTemplates;
using ShapeUp.Features.Training.WorkoutTemplates.UpdateWorkoutTemplate;
using ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionById;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionsByUser;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.StartWorkoutExecution;
using ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

namespace ShapeUp.Features.Training;

public static class TrainingModule
{
    public static IServiceCollection AddTrainingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<TrainingDbContext>(options => options.UseSqlServer(connectionString));

        services.Configure<TrainingMongoOptions>(configuration.GetSection(TrainingMongoOptions.SectionName));
        services.AddSingleton<IMongoClient>(_ =>
        {
            var mongoConnection = configuration[$"{TrainingMongoOptions.SectionName}:ConnectionString"];
            return new MongoClient(mongoConnection);
        });

        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<IWorkoutSessionRepository, MongoWorkoutSessionRepository>();
        services.AddScoped<IWorkoutPlanRepository, MongoWorkoutPlanRepository>();
        services.AddScoped<IWorkoutTemplateRepository, MongoWorkoutTemplateRepository>();
        services.AddScoped<ITrainingAccessPolicy, TrainingAccessPolicy>();


        #region Exercises

        services.AddScoped<CreateExerciseHandler>();
        services.AddScoped<IValidator<CreateExerciseCommand>, CreateExerciseCommandValidator>();
        services.AddScoped<UpdateExerciseHandler>();
        services.AddScoped<IValidator<UpdateExerciseCommand>, UpdateExerciseCommandValidator>();
        services.AddScoped<DeleteExerciseHandler>();
        services.AddScoped<GetExercisesHandler>();
        services.AddScoped<GetExerciseByIdHandler>();
        services.AddScoped<SuggestExercisesHandler>();
        services.AddScoped<IValidator<SuggestExercisesQuery>, SuggestExercisesQueryValidator>();

        #endregion

        #region Equipments

        services.AddScoped<CreateEquipmentHandler>();
        services.AddScoped<IValidator<CreateEquipmentCommand>, CreateEquipmentCommandValidator>();
        services.AddScoped<UpdateEquipmentHandler>();
        services.AddScoped<IValidator<UpdateEquipmentCommand>, UpdateEquipmentCommandValidator>();
        services.AddScoped<DeleteEquipmentHandler>();
        services.AddScoped<GetEquipmentsHandler>();
        services.AddScoped<GetEquipmentByIdHandler>();

        #endregion

        #region WorkoutPlans

        services.AddScoped<CreateWorkoutPlanHandler>();
        services.AddScoped<IValidator<CreateWorkoutPlanCommand>, CreateWorkoutPlanCommandValidator>();
        services.AddScoped<UpdateWorkoutPlanHandler>();
        services.AddScoped<IValidator<UpdateWorkoutPlanCommand>, UpdateWorkoutPlanCommandValidator>();
        services.AddScoped<DeleteWorkoutPlanHandler>();
        services.AddScoped<CopyWorkoutPlanHandler>();
        services.AddScoped<IValidator<CopyWorkoutPlanCommand>, CopyWorkoutPlanCommandValidator>();
        services.AddScoped<GetWorkoutPlanByIdHandler>();
        services.AddScoped<GetWorkoutPlansByUserHandler>();

        #endregion

        #region WorkoutTemplates

        services.AddScoped<CreateWorkoutTemplateHandler>();
        services.AddScoped<IValidator<CreateWorkoutTemplateCommand>, CreateWorkoutTemplateCommandValidator>();
        services.AddScoped<UpdateWorkoutTemplateHandler>();
        services.AddScoped<IValidator<UpdateWorkoutTemplateCommand>, UpdateWorkoutTemplateCommandValidator>();
        services.AddScoped<DeleteWorkoutTemplateHandler>();
        services.AddScoped<CopyWorkoutTemplateHandler>();
        services.AddScoped<IValidator<CopyWorkoutTemplateCommand>, CopyWorkoutTemplateCommandValidator>();
        services.AddScoped<AssignWorkoutTemplateHandler>();
        services.AddScoped<IValidator<AssignWorkoutTemplateCommand>, AssignWorkoutTemplateCommandValidator>();
        services.AddScoped<GetWorkoutTemplateByIdHandler>();
        services.AddScoped<GetWorkoutTemplatesHandler>();

        #endregion

        #region Workouts

        services.AddScoped<IWorkoutSessionResponseMapper, WorkoutSessionResponseMapper>();

        services.AddScoped<StartWorkoutExecutionHandler>();
        services.AddScoped<IValidator<StartWorkoutExecutionCommand>, StartWorkoutExecutionCommandValidator>();
        services.AddScoped<UpdateWorkoutExecutionStateHandler>();
        services.AddScoped<IValidator<UpdateWorkoutExecutionStateCommand>, UpdateWorkoutExecutionStateCommandValidator>();
        services.AddScoped<FinishWorkoutExecutionHandler>();
        services.AddScoped<IValidator<FinishWorkoutExecutionCommand>, FinishWorkoutExecutionCommandValidator>();

        services.AddScoped<GetWorkoutSessionByIdHandler>();
        services.AddScoped<GetWorkoutSessionsByUserHandler>();

        #endregion

        #region Dashboard

        services.AddScoped<GetTrainingDashboardHandler>();

        #endregion

        return services;
    }
}
