using FluentValidation;
using ShapeUp.Features.Training.Exercises.Shared.ValueObjects;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.CreateExercise;

public class CreateExerciseHandler(
    IExerciseRepository exerciseRepository,
    IEquipmentRepository equipmentRepository,
    IValidator<CreateExerciseCommand> validator)
{
    public async Task<Result<ExerciseResponse>> HandleAsync(CreateExerciseCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<ExerciseResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        foreach (var equipmentId in command.EquipmentIds.Distinct())
        {
            if (await equipmentRepository.GetByIdAsync(equipmentId, cancellationToken) is null)
                return Result<ExerciseResponse>.Failure(TrainingErrors.EquipmentNotFound(equipmentId));
        }

        var exercise = new Exercise
        {
            Name = command.Name,
            NamePt = command.NamePt,
            Description = command.Description,
            VideoUrl = command.VideoUrl,
            ExerciseType = command.ExerciseType,
            MuscleProfiles = command.Muscles
                .Select(x => new ExerciseMuscleProfile { MuscleGroup = x.MuscleGroup, ActivationPercent = x.ActivationPercent })
                .ToList(),
            ExerciseEquipments = command.EquipmentIds
                .Distinct()
                .Select(x => new ExerciseEquipment { EquipmentId = x })
                .ToList(),
            Steps = command.Steps?.Select(x => new ExerciseStep { Description = x.Description.Trim() }).ToList() ?? []
        };

        await exerciseRepository.AddAsync(exercise, cancellationToken);

        var persisted = await exerciseRepository.GetByIdAsync(exercise.Id, cancellationToken);
        return Result<ExerciseResponse>.Success(MapResponse(persisted!));
    }

    internal static ExerciseResponse MapResponse(Exercise exercise) =>
        new(
            exercise.Id,
            exercise.Name,
            exercise.NamePt,
            exercise.Description,
            exercise.VideoUrl,
            exercise.MuscleProfiles
                .OrderByDescending(x => x.ActivationPercent)
                .Select(x => new ExerciseMuscleValueObject(
                    x.MuscleGroup,
                    GetMuscleName(x.MuscleGroup),
                    GetMuscleNamePt(x.MuscleGroup),
                    x.ActivationPercent))
                .ToArray(),
            exercise.ExerciseEquipments
                .Select(x => new ExerciseEquipmentValueObject(
                    x.EquipmentId,
                    x.Equipment?.Name ?? string.Empty,
                    x.Equipment?.NamePt ?? string.Empty))
                .ToArray(),
            exercise.Steps.Select(x => x.Description).ToArray(),
            exercise.ExerciseType);

    private static string GetMuscleName(MuscleGroup muscle) => muscle.ToString();
    
    private static string GetMuscleNamePt(MuscleGroup muscle) => muscle switch
    {
        MuscleGroup.Chest => "Peito",
        MuscleGroup.Back => "Costas",
        MuscleGroup.Shoulders => "Ombros",
        MuscleGroup.Arms => "Braços",
        MuscleGroup.Legs => "Pernas",
        MuscleGroup.Abs => "Abdômen",
        MuscleGroup.FullBody => "Corpo inteiro",
        MuscleGroup.MiddleChest => "Peito médio",
        MuscleGroup.UpperChest => "Peito superior",
        MuscleGroup.LowerChest => "Peito inferior",
        MuscleGroup.Triceps => "Tríceps",
        MuscleGroup.Biceps => "Bíceps",
        MuscleGroup.Forearms => "Antebraços",
        MuscleGroup.DeltoidAnterior => "Ombro anterior",
        MuscleGroup.DeltoidLateral => "Ombro lateral",
        MuscleGroup.DeltoidPosterior => "Ombro posterior",
        MuscleGroup.Traps => "Trapézio",
        MuscleGroup.UpperBack => "Costas superiores",
        MuscleGroup.MiddleBack => "Costas centrais",
        MuscleGroup.LowerBack => "Lombar",
        MuscleGroup.Lats => "Dorsais",
        MuscleGroup.AbsUpper => "Abdômen superior",
        MuscleGroup.AbsLower => "Abdômen inferior",
        MuscleGroup.AbsObliques => "Oblíquos",
        MuscleGroup.Quadriceps => "Quadríceps",
        MuscleGroup.Hamstrings => "Posterior de coxa",
        MuscleGroup.Glutes => "Glúteos",
        MuscleGroup.Calves => "Panturrilhas",
        MuscleGroup.HipFlexors => "Flexores do quadril",
        _ => string.Join(", ", Enum.GetValues<MuscleGroup>()
            .Where(group => ((long)group & ((long)group - 1)) == 0 && muscle.HasFlag(group))
            .Distinct()
            .Select(GetMuscleNamePt))
    };
}