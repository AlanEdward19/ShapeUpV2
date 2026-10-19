using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ShapeUp.Features.Training.Infrastructure.Data.Migrations;

[DbContext(typeof(TrainingDbContext))]
[Migration("20261018190000_ExpandExerciseCatalog")]
public class ExpandExerciseCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @Catalog TABLE ([Name] nvarchar(160), [NamePt] nvarchar(160),
                [Equipment] nvarchar(120), [EquipmentPt] nvarchar(120),
                [PrimaryMuscle] bigint, [SecondaryMuscle] bigint, [Type] int,
                [Step1] nvarchar(500), [Step2] nvarchar(500));
            INSERT INTO @Catalog VALUES
                (N'Concentration Curl', N'Rosca concentrada', N'Dumbbells', N'Halteres', 16, 32, 1, N'Sente-se e apoie o braço na parte interna da coxa, segurando um halter.', N'Flexione o cotovelo sem mover o ombro e desça com controle.'),
                (N'Incline Dumbbell Curl', N'Rosca no banco inclinado', N'Dumbbells', N'Halteres', 16, 32, 1, N'Apoie as costas em um banco inclinado e deixe os braços ao lado do corpo.', N'Flexione os cotovelos mantendo os ombros estáveis; retorne sem balanço.'),
                (N'Dumbbell Hammer Curl', N'Rosca martelo', N'Dumbbells', N'Halteres', 16, 32, 1, N'Fique em pé com os halteres ao lado do corpo e as palmas voltadas para dentro.', N'Flexione os cotovelos mantendo a pegada neutra e desça lentamente.'),
                (N'EZ Bar Preacher Curl', N'Rosca Scott com barra W', N'EZ Bar', N'Barra W', 16, 32, 1, N'Ajuste o banco Scott para apoiar a parte posterior dos braços no suporte.', N'Flexione os cotovelos sem tirar os braços do apoio e retorne sem travá-los.'),
                (N'Cable Hammer Curl', N'Rosca martelo na polia', N'Cable Machine', N'Polia', 16, 32, 1, N'Prenda a corda na polia baixa e segure as pontas com pegada neutra.', N'Flexione os cotovelos junto ao tronco e retorne com controle.'),
                (N'Reverse Barbell Curl', N'Rosca inversa com barra', N'Barbell', N'Barra', 32, 16, 1, N'Segure a barra à frente das coxas com as palmas voltadas para baixo.', N'Flexione os cotovelos sem dobrar os punhos e desça sem balanço.'),
                (N'Decline Barbell Bench Press', N'Supino declinado com barra', N'Barbell', N'Barra', 4, 8, 1, N'Deite no banco declinado, fixe os pés e segure a barra com segurança.', N'Desça a barra de forma controlada em direção ao peito e empurre sem perder o apoio.'),
                (N'Incline Cable Fly', N'Crucifixo inclinado na polia', N'Cable Machine', N'Polia', 2, 64, 1, N'Posicione um banco inclinado entre as polias baixas e segure os pegadores.', N'Aproxime as mãos à frente do peito mantendo leve flexão dos cotovelos.'),
                (N'Low To High Cable Fly', N'Crucifixo de baixo para cima na polia', N'Cable Machine', N'Polia', 2, 64, 1, N'Segure os pegadores das polias baixas e estabilize o tronco com um pé à frente.', N'Aproxime as mãos para cima e à frente do peito, sem elevar os ombros.'),
                (N'High To Low Cable Fly', N'Crucifixo de cima para baixo na polia', N'Cable Machine', N'Polia', 4, 8, 1, N'Segure os pegadores das polias altas e mantenha o tronco estável.', N'Aproxime as mãos para baixo e à frente do tronco com cotovelos levemente flexionados.'),
                (N'Neutral Grip Pull Up', N'Barra fixa com pegada neutra', N'Pull Up Bar', N'Barra fixa', 8192, 16, 1, N'Segure os apoios paralelos da barra fixa com as palmas voltadas uma para a outra.', N'Puxe o corpo levando os cotovelos para baixo e desça com controle.'),
                (N'Reverse Grip Lat Pulldown', N'Puxada supinada na polia', N'Cable Machine', N'Polia', 8192, 16, 1, N'Ajuste o apoio das coxas e segure a barra com as palmas voltadas para você.', N'Puxe em direção à parte superior do peito sem jogar o tronco para trás.'),
                (N'Straight Arm Pulldown', N'Pulldown com braços estendidos', N'Cable Machine', N'Polia', 8192, 8, 1, N'Segure a barra na polia alta e incline levemente o tronco, mantendo as costas alinhadas.', N'Leve a barra em direção às coxas com cotovelos quase estendidos e retorne devagar.'),
                (N'T Bar Row', N'Remada cavalinho', N'Landmine', N'Barra com apoio landmine', 2048, 8192, 1, N'Apoie a barra no suporte próprio e incline o tronco mantendo as costas alinhadas.', N'Puxe o pegador em direção ao tronco e retorne sem arredondar as costas.'),
                (N'Cable Face Pull', N'Face pull na polia', N'Cable Machine', N'Polia', 256, 512, 1, N'Fixe a corda na altura do rosto e segure as pontas com os braços à frente.', N'Puxe a corda em direção ao rosto, separando as mãos e mantendo o tronco estável.'),
                (N'Reverse Pec Deck', N'Crucifixo inverso na máquina', N'Reverse Fly Machine', N'Máquina de crucifixo inverso', 256, 2048, 1, N'Ajuste o banco e apoie o peito no encosto, segurando os pegadores à frente.', N'Afaste os braços sem elevar os ombros e retorne com controle.'),
                (N'Dumbbell Front Raise', N'Elevação frontal com halteres', N'Dumbbells', N'Halteres', 64, 0, 1, N'Fique em pé com os halteres à frente das coxas e o tronco estável.', N'Eleve os braços até a altura dos ombros e desça sem balanço.'),
                (N'Overhead Cable Triceps Extension', N'Tríceps acima da cabeça na polia', N'Cable Machine', N'Polia', 8, 0, 1, N'Segure a corda com os braços elevados e cotovelos flexionados, mantendo o tronco firme.', N'Estenda os cotovelos sem mover os braços e retorne com controle.'),
                (N'Dumbbell Triceps Kickback', N'Tríceps coice com halter', N'Dumbbells', N'Halteres', 8, 0, 1, N'Apoie uma mão no banco e mantenha o braço do halter junto ao tronco inclinado.', N'Estenda o cotovelo sem mover o ombro e retorne lentamente.'),
                (N'Leg Extension', N'Cadeira extensora', N'Leg Extension Machine', N'Cadeira extensora', 131072, 0, 1, N'Ajuste o encosto e o rolo acima dos tornozelos, alinhando os joelhos ao eixo da máquina.', N'Estenda os joelhos e retorne com controle, sem tirar o quadril do banco.'),
                (N'Seated Leg Curl', N'Cadeira flexora', N'Seated Leg Curl Machine', N'Cadeira flexora', 262144, 0, 1, N'Sente-se e ajuste os apoios para estabilizar as coxas com os joelhos alinhados ao eixo.', N'Flexione os joelhos levando o rolo para baixo e retorne sem impulso.'),
                (N'Lying Leg Curl', N'Mesa flexora deitada', N'Lying Leg Curl Machine', N'Mesa flexora', 262144, 0, 1, N'Deite de barriga para baixo e ajuste o rolo acima dos calcanhares.', N'Flexione os joelhos sem elevar o quadril e retorne lentamente.'),
                (N'Standing Calf Raise', N'Panturrilha em pé na máquina', N'Standing Calf Machine', N'Máquina de panturrilha em pé', 1048576, 0, 1, N'Apoie a parte da frente dos pés na plataforma e ajuste o suporte nos ombros.', N'Eleve os calcanhares e desça com controle pela amplitude confortável.'),
                (N'Seated Calf Raise', N'Panturrilha sentada', N'Seated Calf Machine', N'Máquina de panturrilha sentada', 1048576, 0, 1, N'Sente-se e posicione o apoio sobre as coxas, com a parte da frente dos pés na plataforma.', N'Eleve e abaixe os calcanhares sem perder o apoio dos pés.'),
                (N'Cable Crunch', N'Abdominal na polia', N'Cable Machine', N'Polia', 16384, 0, 1, N'Ajoelhe-se diante da polia alta, segurando a corda junto à cabeça.', N'Flexione o tronco aproximando as costelas do quadril, sem puxar com os braços.'),
                (N'Hanging Knee Raise', N'Elevação de joelhos na barra', N'Pull Up Bar', N'Barra fixa', 32768, 2097152, 1, N'Segure a barra e estabilize o corpo, evitando balanço.', N'Eleve os joelhos em direção ao tronco e abaixe com controle.'),
                (N'Side Plank', N'Prancha lateral', N'Bodyweight', N'Peso corporal', 65536, 524288, 2, N'Deite de lado e apoie o antebraço sob o ombro, mantendo as pernas alinhadas.', N'Eleve o quadril e sustente o corpo alinhado, respirando normalmente.'),
                (N'Dead Bug', N'Dead bug alternado', N'Bodyweight', N'Peso corporal', 32768, 16384, 2, N'Deite de costas com quadris e joelhos flexionados e braços apontados para cima.', N'Estenda braço e perna opostos sem perder o controle do tronco e alterne os lados.');

            DECLARE @Added TABLE ([Id] int, [Name] nvarchar(160));
            INSERT INTO [Exercises] ([Name], [NamePt], [Description], [ExerciseType], [CreatedAtUtc])
            OUTPUT inserted.[Id], inserted.[Name] INTO @Added
            SELECT c.[Name], c.[NamePt], c.[Step1] + N' ' + c.[Step2], c.[Type], '2026-10-18T19:00:00Z'
            FROM @Catalog c
            WHERE NOT EXISTS (SELECT 1 FROM [Exercises] e WHERE e.[Name] = c.[Name] OR e.[NamePt] = c.[NamePt]);

            INSERT INTO [Equipments] ([Name], [NamePt], [CreatedAtUtc])
            SELECT DISTINCT c.[Equipment], c.[EquipmentPt], '2026-10-18T19:00:00Z' FROM @Catalog c
            WHERE NOT EXISTS (SELECT 1 FROM [Equipments] e WHERE e.[Name] = c.[Equipment] OR e.[NamePt] = c.[EquipmentPt]);

            INSERT INTO [ExerciseEquipments] ([ExerciseId], [EquipmentId])
            SELECT a.[Id], eq.[Id] FROM @Added a JOIN @Catalog c ON c.[Name] = a.[Name]
            CROSS APPLY (SELECT TOP (1) e.[Id] FROM [Equipments] e
                WHERE e.[Name] = c.[Equipment] OR e.[NamePt] = c.[EquipmentPt] ORDER BY e.[Id]) eq;

            INSERT INTO [ExerciseSteps] ([ExerciseId], [Description])
            SELECT a.[Id], step.[Description] FROM @Added a JOIN @Catalog c ON c.[Name] = a.[Name]
            CROSS APPLY (VALUES (c.[Step1]), (c.[Step2])) step([Description]);

            -- Editorial display weights, not measured physiological percentages.
            INSERT INTO [ExerciseMuscleProfiles] ([ExerciseId], [MuscleGroup], [ActivationPercent])
            SELECT a.[Id], m.[Group], m.[Weight] FROM @Added a JOIN @Catalog c ON c.[Name] = a.[Name]
            CROSS APPLY (VALUES (c.[PrimaryMuscle], CAST(80 AS decimal(5,2))),
                (c.[SecondaryMuscle], CAST(40 AS decimal(5,2)))) m([Group], [Weight])
            WHERE m.[Group] > 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Preserve exercises referenced by historical routines/sessions on rollback.
        // This data-only migration intentionally leaves additive catalogue entries.
    }
}
