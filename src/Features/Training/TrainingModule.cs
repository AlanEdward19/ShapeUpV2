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
using ShapeUp.Features.Training.Workouts.CompleteWorkoutSession;
using ShapeUp.Features.Training.Workouts.CreateWorkoutSession;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionById;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionsByUser;

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

        #region Workouts

        services.AddScoped<CreateWorkoutSessionHandler>();
        services.AddScoped<IValidator<CreateWorkoutSessionCommand>, CreateWorkoutSessionCommandValidator>();
        services.AddScoped<CompleteWorkoutSessionHandler>();
        services.AddScoped<IValidator<CompleteWorkoutSessionCommand>, CompleteWorkoutSessionCommandValidator>();
        services.AddScoped<GetWorkoutSessionByIdHandler>();
        services.AddScoped<GetWorkoutSessionsByUserHandler>();

        #endregion

        #region Dashboard

        services.AddScoped<GetTrainingDashboardHandler>();

        #endregion

        return services;
    }
}
