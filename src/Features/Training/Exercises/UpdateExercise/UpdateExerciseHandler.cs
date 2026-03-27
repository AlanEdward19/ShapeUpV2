using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.UpdateExercise;

public class UpdateExerciseHandler(
    IExerciseRepository exerciseRepository,
    IEquipmentRepository equipmentRepository,
    IMuscleRepository muscleRepository,
    IValidator<UpdateExerciseCommand> validator)
{
    public async Task<Result<ExerciseResponse>> HandleAsync(UpdateExerciseCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<ExerciseResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var exercise = await exerciseRepository.GetByIdAsync(command.ExerciseId, cancellationToken);
        if (exercise is null)
            return Result<ExerciseResponse>.Failure(TrainingErrors.ExerciseNotFound(command.ExerciseId));

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

        exercise.Name = command.Name;
        exercise.NamePt = command.NamePt;
        exercise.Description = command.Description;
        exercise.VideoUrl = command.VideoUrl;

        exercise.MuscleProfiles.Clear();
        foreach (var input in command.Muscles)
            exercise.MuscleProfiles.Add(new ExerciseMuscleProfile { ExerciseId = exercise.Id, MuscleId = input.MuscleId, ActivationPercent = input.ActivationPercent });

        exercise.ExerciseEquipments.Clear();
        foreach (var equipmentId in command.EquipmentIds.Distinct())
            exercise.ExerciseEquipments.Add(new ExerciseEquipment { ExerciseId = exercise.Id, EquipmentId = equipmentId });

        exercise.Steps.Clear();
        if (command.Steps is not null)
        {
            foreach (var step in command.Steps)
                exercise.Steps.Add(new ExerciseStep { ExerciseId = exercise.Id, Description = step.Description.Trim() });
        }

        await exerciseRepository.UpdateAsync(exercise, cancellationToken);
        var persisted = await exerciseRepository.GetByIdAsync(exercise.Id, cancellationToken);
        return Result<ExerciseResponse>.Success(CreateExerciseHandler.MapResponse(persisted!));
    }
}