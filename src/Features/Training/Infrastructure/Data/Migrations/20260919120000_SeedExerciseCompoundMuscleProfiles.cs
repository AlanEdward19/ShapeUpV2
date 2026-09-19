using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShapeUp.Features.Training.Infrastructure.Data;

#nullable disable

namespace ShapeUp.Features.Training.Infrastructure.Data.Migrations;

[DbContext(typeof(TrainingDbContext))]
[Migration("20260919120000_SeedExerciseCompoundMuscleProfiles")]
public partial class SeedExerciseCompoundMuscleProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO [ExerciseMuscleProfiles] ([ExerciseId], [MuscleGroup], [ActivationPercent])
            SELECT e.[Id], v.[MuscleGroup], v.[ActivationPercent]
            FROM [Exercises] e
            INNER JOIN (VALUES
                (N'Barbell Bench Press', CAST(1 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Bench Press', CAST(8 AS bigint), CAST(55.00 AS decimal(5,2))),
                (N'Barbell Bench Press', CAST(64 AS bigint), CAST(45.00 AS decimal(5,2))),
                (N'Barbell Back Squat', CAST(131072 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Back Squat', CAST(524288 AS bigint), CAST(55.00 AS decimal(5,2))),
                (N'Barbell Row', CAST(8192 AS bigint), CAST(70.00 AS decimal(5,2))),
                (N'Barbell Row', CAST(2048 AS bigint), CAST(55.00 AS decimal(5,2))),
                (N'Pull-Up', CAST(8192 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Pull-Up', CAST(16 AS bigint), CAST(55.00 AS decimal(5,2))),
                (N'Lat Pulldown', CAST(8192 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Lat Pulldown', CAST(16 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Barbell Overhead Press', CAST(64 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Overhead Press', CAST(8 AS bigint), CAST(55.00 AS decimal(5,2))),
                (N'Barbell Romanian Deadlift', CAST(262144 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Barbell Romanian Deadlift', CAST(524288 AS bigint), CAST(60.00 AS decimal(5,2))),
                (N'Barbell Hip Thrust', CAST(524288 AS bigint), CAST(85.00 AS decimal(5,2))),
                (N'Barbell Hip Thrust', CAST(262144 AS bigint), CAST(50.00 AS decimal(5,2))),
                (N'Close-Grip Bench Press', CAST(8 AS bigint), CAST(80.00 AS decimal(5,2))),
                (N'Close-Grip Bench Press', CAST(1 AS bigint), CAST(55.00 AS decimal(5,2)))
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
            INNER JOIN (VALUES
                (N'Barbell Bench Press', CAST(1 AS bigint)),
                (N'Barbell Bench Press', CAST(8 AS bigint)),
                (N'Barbell Bench Press', CAST(64 AS bigint)),
                (N'Barbell Back Squat', CAST(131072 AS bigint)),
                (N'Barbell Back Squat', CAST(524288 AS bigint)),
                (N'Barbell Row', CAST(8192 AS bigint)),
                (N'Barbell Row', CAST(2048 AS bigint)),
                (N'Pull-Up', CAST(8192 AS bigint)),
                (N'Pull-Up', CAST(16 AS bigint)),
                (N'Lat Pulldown', CAST(8192 AS bigint)),
                (N'Lat Pulldown', CAST(16 AS bigint)),
                (N'Barbell Overhead Press', CAST(64 AS bigint)),
                (N'Barbell Overhead Press', CAST(8 AS bigint)),
                (N'Barbell Romanian Deadlift', CAST(262144 AS bigint)),
                (N'Barbell Romanian Deadlift', CAST(524288 AS bigint)),
                (N'Barbell Hip Thrust', CAST(524288 AS bigint)),
                (N'Barbell Hip Thrust', CAST(262144 AS bigint)),
                (N'Close-Grip Bench Press', CAST(8 AS bigint)),
                (N'Close-Grip Bench Press', CAST(1 AS bigint))
            ) v([Name], [MuscleGroup]) ON e.[Name] = v.[Name] AND p.[MuscleGroup] = v.[MuscleGroup]
            WHERE p.[ExerciseId] = e.[Id];
            """);
    }
}
