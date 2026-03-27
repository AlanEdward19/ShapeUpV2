using FluentValidation;
using ShapeUp.Features.Training.Exercises.Shared.Dtos;
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
            exercise.Steps.Select(x => x.Description).ToArray());

    private static string GetMuscleName(EMuscleGroup muscle) => muscle.ToString();
    
    private static string GetMuscleNamePt(EMuscleGroup muscle) => muscle switch
    {
        EMuscleGroup.MiddleChest => "Peito Médio",
        EMuscleGroup.UpperChest => "Peito Superior",
        EMuscleGroup.LowerChest => "Peito Inferior",
        EMuscleGroup.Triceps => "Tríceps",
        EMuscleGroup.Biceps => "Bíceps",
        EMuscleGroup.Forearms => "Antebraços",
        EMuscleGroup.DeltoidAnterior => "Deltóide Anterior",
        EMuscleGroup.DeltoidLateral => "Deltóide Lateral",
        EMuscleGroup.DeltoidPosterior => "Deltóide Posterior",
        EMuscleGroup.Traps => "Trapézio",
        EMuscleGroup.UpperBack => "Costas Superior",
        EMuscleGroup.MiddleBack => "Costas Média",
        EMuscleGroup.LowerBack => "Costas Inferior",
        EMuscleGroup.Lats => "Latíssimo",
        EMuscleGroup.AbsUpper => "Abdômen Superior",
        EMuscleGroup.AbsLower => "Abdômen Inferior",
        EMuscleGroup.AbsObliques => "Oblíquos",
        EMuscleGroup.Quadriceps => "Quadríceps",
        EMuscleGroup.Hamstrings => "Isquiotibiais",
        EMuscleGroup.Glutes => "Glúteos",
        EMuscleGroup.Calves => "Panturrilhas",
        EMuscleGroup.HipFlexors => "Flexores de Quadril",
        _ => muscle.ToString()
    };
}