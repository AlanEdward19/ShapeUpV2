using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShapeUp.Features.Training.Infrastructure.Data;

#nullable disable

namespace ShapeUp.Features.Training.Infrastructure.Data.Migrations;

[DbContext(typeof(TrainingDbContext))]
[Migration("20260918040000_SeedBasicExercises")]
public partial class SeedBasicExercises : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [Exercises])
            BEGIN
                DECLARE @SeededExercises TABLE ([Id] int, [Name] nvarchar(160));

                INSERT INTO [Exercises] ([Name], [NamePt], [Description], [VideoUrl], [ExerciseType], [CreatedAtUtc])
                OUTPUT INSERTED.[Id], INSERTED.[Name] INTO @SeededExercises ([Id], [Name])
                VALUES
                    (N'Barbell Back Squat', N'Agachamento Livre com Barra', N'Agachamento com barra livre apoiada nas costas.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Smith Machine Squat', N'Agachamento no Smith', N'Agachamento guiado na máquina Smith.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Hack Squat', N'Agachamento Hack', N'Agachamento na máquina hack.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Leg Press', N'Leg Press', N'Extensão de quadril e joelhos no leg press.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Barbell Romanian Deadlift', N'Terra Romeno com Barra', N'Extensão de quadril com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Romanian Deadlift', N'Terra Romeno com Halteres', N'Extensão de quadril com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Smith Machine Romanian Deadlift', N'Terra Romeno no Smith', N'Extensão de quadril guiada no Smith.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Cable Pull-Through', N'Pull-Through no Cabo', N'Extensão de quadril usando cabo baixo.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Barbell Hip Thrust', N'Elevação Pélvica com Barra', N'Extensão de quadril com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Smith Machine Hip Thrust', N'Elevação Pélvica no Smith', N'Extensão de quadril guiada no Smith.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Hip Thrust Machine', N'Elevação Pélvica na Máquina', N'Extensão de quadril em máquina específica.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Glute Bridge', N'Ponte de Glúteos com Halter', N'Extensão de quadril no chão com halter.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Barbell Bulgarian Split Squat', N'Agachamento Búlgaro com Barra', N'Agachamento unilateral com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Bulgarian Split Squat', N'Agachamento Búlgaro com Halteres', N'Agachamento unilateral com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Smith Machine Split Squat', N'Agachamento Unilateral no Smith', N'Agachamento unilateral guiado no Smith.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Single-Leg Press', N'Leg Press Unilateral', N'Leg press executado com uma perna.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Barbell Bench Press', N'Supino Reto com Barra', N'Pressão horizontal com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Bench Press', N'Supino Reto com Halteres', N'Pressão horizontal com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Chest Press Machine', N'Supino na Máquina', N'Pressão horizontal em máquina guiada.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Push-Up', N'Flexão de Braços', N'Pressão horizontal usando o peso corporal.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Incline Barbell Bench Press', N'Supino Inclinado com Barra', N'Pressão inclinada com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Incline Dumbbell Bench Press', N'Supino Inclinado com Halteres', N'Pressão inclinada com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Incline Chest Press Machine', N'Supino Inclinado na Máquina', N'Pressão inclinada em máquina guiada.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Feet-Elevated Push-Up', N'Flexão com Pés Elevados', N'Flexão inclinada usando o peso corporal.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Fly', N'Crucifixo com Halteres', N'Adução horizontal com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Cable Crossover', N'Crossover no Cabo', N'Adução horizontal usando cabos.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Pec Deck Fly', N'Crucifixo na Máquina', N'Adução horizontal na máquina peck deck.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Band Chest Fly', N'Crucifixo com Elástico', N'Adução horizontal usando faixa elástica.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Barbell Overhead Press', N'Desenvolvimento com Barra', N'Pressão vertical com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Shoulder Press', N'Desenvolvimento com Halteres', N'Pressão vertical com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Shoulder Press Machine', N'Desenvolvimento na Máquina', N'Pressão vertical em máquina guiada.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Smith Machine Shoulder Press', N'Desenvolvimento no Smith', N'Pressão vertical guiada no Smith.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Pull-Up', N'Barra Fixa Pronada', N'Puxada vertical com o peso corporal.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Lat Pulldown', N'Puxada Alta no Cabo', N'Puxada vertical na polia alta.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Plate-Loaded Lat Pulldown', N'Puxada Alta Articulada', N'Puxada vertical em máquina com anilhas.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Band Lat Pulldown', N'Puxada Alta com Elástico', N'Puxada vertical usando faixa elástica.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Barbell Row', N'Remada Curvada com Barra', N'Remada horizontal com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'One-Arm Dumbbell Row', N'Remada Unilateral com Halter', N'Remada horizontal com halter.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Seated Cable Row', N'Remada Baixa no Cabo', N'Remada horizontal usando cabo.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Chest-Supported Row Machine', N'Remada Máquina com Apoio', N'Remada horizontal em máquina com apoio peitoral.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Barbell Biceps Curl', N'Rosca Direta com Barra', N'Flexão dos cotovelos com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Biceps Curl', N'Rosca Direta com Halteres', N'Flexão dos cotovelos com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Cable Biceps Curl', N'Rosca Direta no Cabo', N'Flexão dos cotovelos usando cabo.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Biceps Curl Machine', N'Rosca Bíceps na Máquina', N'Flexão dos cotovelos em máquina guiada.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Close-Grip Bench Press', N'Supino Fechado', N'Extensão dos cotovelos com barra livre.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Skull Crusher', N'Tríceps Testa com Halteres', N'Extensão dos cotovelos com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Triceps Pushdown', N'Tríceps na Polia', N'Extensão dos cotovelos usando cabo.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Triceps Extension Machine', N'Tríceps na Máquina', N'Extensão dos cotovelos em máquina guiada.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Dumbbell Lateral Raise', N'Elevação Lateral com Halteres', N'Abdução dos ombros com halteres.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Cable Lateral Raise', N'Elevação Lateral no Cabo', N'Abdução dos ombros usando cabo.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Lateral Raise Machine', N'Elevação Lateral na Máquina', N'Abdução dos ombros em máquina guiada.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Band Lateral Raise', N'Elevação Lateral com Elástico', N'Abdução dos ombros usando faixa elástica.', NULL, 1, '2026-09-18T04:00:00Z'),
                    (N'Treadmill Run', N'Corrida na Esteira', N'Corrida contínua ou intervalada na esteira.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Outdoor Run', N'Corrida ao Ar Livre', N'Corrida contínua ou intervalada ao ar livre.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Stationary Bike', N'Bicicleta Ergométrica', N'Condicionamento aeróbico em bicicleta estacionária.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Rowing Machine', N'Remo Ergométrico', N'Condicionamento aeróbico em remo estacionário.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Jump Rope', N'Pular Corda', N'Condicionamento intervalado com corda.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Elliptical Trainer', N'Transport Elíptico', N'Condicionamento aeróbico no elíptico.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Stair Climber', N'Escada Ergométrica', N'Condicionamento aeróbico na escada ergométrica.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Air Bike', N'Bicicleta de Ar', N'Condicionamento intervalado em bicicleta de ar.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Hamstring Stretch', N'Alongamento de Posteriores', N'Alongamento sustentado da parte posterior das coxas.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Quadriceps Stretch', N'Alongamento de Quadríceps', N'Alongamento sustentado da parte anterior das coxas.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Hip Flexor Stretch', N'Alongamento de Flexores do Quadril', N'Alongamento sustentado da região anterior do quadril.', NULL, 2, '2026-09-18T04:00:00Z'),
                    (N'Glute Stretch', N'Alongamento de Glúteos', N'Alongamento sustentado dos glúteos e quadril.', NULL, 2, '2026-09-18T04:00:00Z');

                ;WITH ExerciseFamilies AS
                (
                    SELECT * FROM (VALUES
                        (N'Barbell Back Squat', 1), (N'Smith Machine Squat', 1), (N'Hack Squat', 1), (N'Leg Press', 1),
                        (N'Barbell Romanian Deadlift', 2), (N'Dumbbell Romanian Deadlift', 2), (N'Smith Machine Romanian Deadlift', 2), (N'Cable Pull-Through', 2),
                        (N'Barbell Hip Thrust', 3), (N'Smith Machine Hip Thrust', 3), (N'Hip Thrust Machine', 3), (N'Dumbbell Glute Bridge', 3),
                        (N'Barbell Bulgarian Split Squat', 4), (N'Dumbbell Bulgarian Split Squat', 4), (N'Smith Machine Split Squat', 4), (N'Single-Leg Press', 4),
                        (N'Barbell Bench Press', 5), (N'Dumbbell Bench Press', 5), (N'Chest Press Machine', 5), (N'Push-Up', 5),
                        (N'Incline Barbell Bench Press', 6), (N'Incline Dumbbell Bench Press', 6), (N'Incline Chest Press Machine', 6), (N'Feet-Elevated Push-Up', 6),
                        (N'Dumbbell Fly', 7), (N'Cable Crossover', 7), (N'Pec Deck Fly', 7), (N'Band Chest Fly', 7),
                        (N'Barbell Overhead Press', 8), (N'Dumbbell Shoulder Press', 8), (N'Shoulder Press Machine', 8), (N'Smith Machine Shoulder Press', 8),
                        (N'Pull-Up', 9), (N'Lat Pulldown', 9), (N'Plate-Loaded Lat Pulldown', 9), (N'Band Lat Pulldown', 9),
                        (N'Barbell Row', 10), (N'One-Arm Dumbbell Row', 10), (N'Seated Cable Row', 10), (N'Chest-Supported Row Machine', 10),
                        (N'Barbell Biceps Curl', 11), (N'Dumbbell Biceps Curl', 11), (N'Cable Biceps Curl', 11), (N'Biceps Curl Machine', 11),
                        (N'Close-Grip Bench Press', 12), (N'Dumbbell Skull Crusher', 12), (N'Triceps Pushdown', 12), (N'Triceps Extension Machine', 12),
                        (N'Dumbbell Lateral Raise', 13), (N'Cable Lateral Raise', 13), (N'Lateral Raise Machine', 13), (N'Band Lateral Raise', 13),
                        (N'Treadmill Run', 14), (N'Outdoor Run', 14), (N'Stationary Bike', 14), (N'Rowing Machine', 14),
                        (N'Jump Rope', 15), (N'Elliptical Trainer', 15), (N'Stair Climber', 15), (N'Air Bike', 15),
                        (N'Hamstring Stretch', 16), (N'Quadriceps Stretch', 16), (N'Hip Flexor Stretch', 16), (N'Glute Stretch', 16)
                    ) family ([ExerciseName], [FamilyId])
                )
                INSERT INTO [ExerciseEquivalents] ([ExerciseId], [EquivalentExerciseId], [CreatedAtUtc])
                SELECT CASE WHEN a.[Id] < b.[Id] THEN a.[Id] ELSE b.[Id] END,
                       CASE WHEN a.[Id] < b.[Id] THEN b.[Id] ELSE a.[Id] END,
                       '2026-09-18T04:00:00Z'
                FROM ExerciseFamilies x
                JOIN ExerciseFamilies y ON y.[FamilyId] = x.[FamilyId] AND y.[ExerciseName] > x.[ExerciseName]
                JOIN @SeededExercises a ON a.[Name] = x.[ExerciseName]
                JOIN @SeededExercises b ON b.[Name] = y.[ExerciseName];

                IF EXISTS
                (
                    SELECT seeded.[Id]
                    FROM @SeededExercises seeded
                    LEFT JOIN [ExerciseEquivalents] equivalent
                        ON equivalent.[ExerciseId] = seeded.[Id]
                        OR equivalent.[EquivalentExerciseId] = seeded.[Id]
                    GROUP BY seeded.[Id]
                    HAVING COUNT(equivalent.[ExerciseId]) < 3
                )
                    THROW 51000, 'Every seeded exercise must have at least three equivalents.', 1;
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM [ExerciseEquivalents]
            WHERE [CreatedAtUtc] = '2026-09-18T04:00:00Z';

            DELETE FROM [Exercises]
            WHERE [CreatedAtUtc] = '2026-09-18T04:00:00Z';
            """);
    }
}
