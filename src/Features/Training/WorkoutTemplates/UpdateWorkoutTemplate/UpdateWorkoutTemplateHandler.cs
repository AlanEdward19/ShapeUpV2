using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutTemplates.Shared;
using ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutTemplates.UpdateWorkoutTemplate;

public class UpdateWorkoutTemplateHandler(
    IWorkoutTemplateRepository workoutTemplateRepository,
    IExerciseRepository exerciseRepository,
    IValidator<UpdateWorkoutTemplateCommand> validator)
{
    public async Task<Result<WorkoutTemplateResponse>> HandleAsync(
        UpdateWorkoutTemplateCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutTemplateResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var template = await workoutTemplateRepository.GetByIdAsync(command.GetTemplateId(), cancellationToken);
        if (template is null)
            return Result<WorkoutTemplateResponse>.Failure(TrainingErrors.WorkoutTemplateNotFound(command.GetTemplateId()));

        if (template.CreatedByUserId != actorUserId)
            return Result<WorkoutTemplateResponse>.Failure(TrainingErrors.WorkoutTemplateNotOwned(command.GetTemplateId(), actorUserId));

        var exerciseMaps = new List<(ExerciseResponse Exercise, WorkoutExerciseDto Input)>();
        foreach (var exerciseInput in command.Exercises)
        {
            var exercise = await exerciseRepository.GetByIdAsync(exerciseInput.ExerciseId, cancellationToken);
            if (exercise is null)
                return Result<WorkoutTemplateResponse>.Failure(TrainingErrors.ExerciseNotFound(exerciseInput.ExerciseId));

            exerciseMaps.Add((CreateExerciseHandler.MapResponse(exercise), exerciseInput));
        }

        template.Name = command.Name.Trim();
        template.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
        template.DurationInWeeks = command.DurationInWeeks;
        template.Phase = command.Phase.Trim();
        template.Difficulty = command.Difficulty;
        template.UpdatedAtUtc = DateTime.UtcNow;
        template.Exercises = exerciseMaps
            .Select(x => new PlannedExerciseDocumentValueObject
            {
                ExerciseId = x.Exercise.Id,
                ExerciseName = x.Exercise.Name,
                Sets = x.Input.Sets
                    .Select(s => new PlannedSetDocumentValueObject
                    {
                        Repetitions = s.Repetitions,
                        Load = s.Load,
                        LoadUnit = s.LoadUnit,
                        SetType = s.SetType,
                        Technique = s.Technique,
                        Rpe = s.Rpe,
                        RestSeconds = s.RestSeconds
                    })
                    .ToList()
            })
            .ToList();

        await workoutTemplateRepository.UpdateAsync(template, cancellationToken);
        return Result<WorkoutTemplateResponse>.Success(template.ToResponse());
    }
}

