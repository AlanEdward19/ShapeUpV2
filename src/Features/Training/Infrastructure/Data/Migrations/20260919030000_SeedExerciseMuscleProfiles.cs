using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShapeUp.Features.Training.Infrastructure.Data;

#nullable disable

namespace ShapeUp.Features.Training.Infrastructure.Data.Migrations;

[DbContext(typeof(TrainingDbContext))]
[Migration("20260919030000_SeedExerciseMuscleProfiles")]
public partial class SeedExerciseMuscleProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO [ExerciseMuscleProfiles] ([ExerciseId], [MuscleGroup], [ActivationPercent])
            SELECT e.[Id], v.[MuscleGroup], v.[ActivationPercent]
            FROM [Exercises] e
            INNER JOIN (VALUES
                (N'Barbell Back Squat', CAST(131072 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Back Squat', CAST(524288 AS bigint), CAST(55.00 AS decimal(5,2))),
                (N'Smith Machine Squat', CAST(131072 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Hack Squat', CAST(131072 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Leg Press', CAST(131072 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Romanian Deadlift', CAST(262144 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Romanian Deadlift', CAST(524288 AS bigint), CAST(60.00 AS decimal(5,2))),
                (N'Dumbbell Romanian Deadlift', CAST(262144 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Smith Machine Romanian Deadlift', CAST(262144 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Cable Pull-Through', CAST(524288 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Hip Thrust', CAST(524288 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Smith Machine Hip Thrust', CAST(524288 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Hip Thrust Machine', CAST(524288 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Dumbbell Glute Bridge', CAST(524288 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Bulgarian Split Squat', CAST(131072 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Dumbbell Bulgarian Split Squat', CAST(131072 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Smith Machine Split Squat', CAST(131072 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Single-Leg Press', CAST(131072 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Bench Press', CAST(7 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Dumbbell Bench Press', CAST(7 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Chest Press Machine', CAST(7 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Push-Up', CAST(7 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Incline Barbell Bench Press', CAST(2 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Incline Dumbbell Bench Press', CAST(2 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Incline Chest Press Machine', CAST(2 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Feet-Elevated Push-Up', CAST(2 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Dumbbell Fly', CAST(7 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Cable Crossover', CAST(7 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Pec Deck Fly', CAST(7 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Band Chest Fly', CAST(7 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Barbell Overhead Press', CAST(448 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Dumbbell Shoulder Press', CAST(448 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Shoulder Press Machine', CAST(448 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Smith Machine Shoulder Press', CAST(448 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Pull-Up', CAST(8192 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Lat Pulldown', CAST(8192 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Plate-Loaded Lat Pulldown', CAST(8192 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Band Lat Pulldown', CAST(8192 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Barbell Row', CAST(8192 AS bigint), CAST(70.00 AS decimal(5,2))),
                (N'Barbell Row', CAST(2048 AS bigint), CAST(55.00 AS decimal(5,2))),
                (N'One-Arm Dumbbell Row', CAST(8192 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Seated Cable Row', CAST(2048 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Chest-Supported Row Machine', CAST(2048 AS bigint), CAST(75.00 AS decimal(5,2))),
                (N'Barbell Biceps Curl', CAST(16 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Dumbbell Biceps Curl', CAST(16 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Cable Biceps Curl', CAST(16 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Biceps Curl Machine', CAST(16 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Close-Grip Bench Press', CAST(8 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Dumbbell Skull Crusher', CAST(8 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Triceps Pushdown', CAST(8 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Triceps Extension Machine', CAST(8 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Dumbbell Lateral Raise', CAST(128 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Cable Lateral Raise', CAST(128 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Lateral Raise Machine', CAST(128 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Band Lateral Raise', CAST(128 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Treadmill Run', CAST(131072 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Outdoor Run', CAST(131072 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Stationary Bike', CAST(131072 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Rowing Machine', CAST(8192 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Jump Rope', CAST(1048576 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Elliptical Trainer', CAST(131072 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Stair Climber', CAST(131072 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Air Bike', CAST(131072 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Hamstring Stretch', CAST(262144 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Quadriceps Stretch', CAST(131072 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Hip Flexor Stretch', CAST(2097152 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Glute Stretch', CAST(524288 AS bigint), CAST(50.00 AS decimal(5,2)))
            ) v([Name], [MuscleGroup], [ActivationPercent]) ON e.[Name] = v.[Name]
            WHERE NOT EXISTS (
                SELECT 1
                FROM [ExerciseMuscleProfiles] p
                WHERE p.[ExerciseId] = e.[Id] AND p.[MuscleGroup] = v.[MuscleGroup]
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE p
            FROM [ExerciseMuscleProfiles] p
            INNER JOIN [Exercises] e ON e.[Id] = p.[ExerciseId]
            WHERE e.[CreatedAtUtc] = '2026-09-18T04:00:00Z';
            """);
    }
}
