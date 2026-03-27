using FluentValidation;
using ShapeUp.Features.Training.Exercises.Shared.ValueObjects;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;
using ExerciseMuscleValueObject = ShapeUp.Features.Training.Exercises.Shared.ValueObjects.ExerciseMuscleValueObject;

namespace ShapeUp.Features.Training.Exercises.CreateExercise;

public class CreateExerciseHandler(
    IExerciseRepository exerciseRepository,
    IEquipmentRepository equipmentRepository,
    IMuscleRepository muscleRepository,
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

        foreach (var input in command.Muscles)
        {
            if (await muscleRepository.GetByIdAsync(input.MuscleId, cancellationToken) is null)
                return Result<ExerciseResponse>.Failure(TrainingErrors.MuscleNotFound(input.MuscleId));
        }

        var exercise = new Exercise
        {
            Name = command.Name,
            NamePt = command.NamePt,
            Description = command.Description,
            VideoUrl = command.VideoUrl,
            MuscleProfiles = command.Muscles
                .Select(x => new ExerciseMuscleProfile { MuscleId = x.MuscleId, ActivationPercent = x.ActivationPercent })
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
                    x.MuscleId,
                    x.Muscle?.Name ?? string.Empty,
                    x.Muscle?.NamePt ?? string.Empty,
                    x.ActivationPercent))
                .ToArray(),
            exercise.ExerciseEquipments
                .Select(x => new ExerciseEquipmentValueObject(
                    x.EquipmentId,
                    x.Equipment?.Name ?? string.Empty,
                    x.Equipment?.NamePt ?? string.Empty))
                .ToArray(),
            exercise.Steps.Select(x => x.Description).ToArray());
}