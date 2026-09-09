using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
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

        var blocks = new List<BlockDocumentValueObject>();
        foreach (var blockInput in command.Blocks)
        {
            var exercises = new List<BlockExerciseDocumentValueObject>();
            foreach (var exerciseInput in blockInput.Exercises)
            {
                var exercise = await exerciseRepository.GetByIdAsync(exerciseInput.ExerciseId, cancellationToken);
                if (exercise is null)
                    return Result<WorkoutTemplateResponse>.Failure(TrainingErrors.ExerciseNotFound(exerciseInput.ExerciseId));

                var mapped = CreateExerciseHandler.MapResponse(exercise);
                exercises.Add(new BlockExerciseDocumentValueObject
                {
                    ExerciseId = mapped.Id,
                    ExerciseName = mapped.Name,
                    Sets = exerciseInput.Sets
                        .Select(s => new PlannedSetDocumentValueObject
                        {
                            Repetitions = s.Repetitions,
                            Load = s.Load,
                            LoadUnit = s.LoadUnit,
                            SetType = s.SetType,
                            Technique = s.Technique,
                            Intensity = s.Intensity is null ? null : new IntensityDocumentValueObject { Type = s.Intensity.Type, Value = s.Intensity.Value },
                            RestSeconds = s.RestSeconds
                        })
                        .ToList()
                });
            }

            blocks.Add(new BlockDocumentValueObject
            {
                Type = blockInput.Type,
                Exercises = exercises,
                TimeCapSeconds = blockInput.TimeCapSeconds,
                IntervalSeconds = blockInput.IntervalSeconds,
                TotalRounds = blockInput.TotalRounds,
                RestAfterSeconds = blockInput.RestAfterSeconds
            });
        }

        template.Name = command.Name.Trim();
        template.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
        template.DurationInWeeks = command.DurationInWeeks;
        template.Phase = command.Phase.Trim();
        template.Difficulty = command.Difficulty;
        template.UpdatedAtUtc = DateTime.UtcNow;
        template.Blocks = blocks;

        await workoutTemplateRepository.UpdateAsync(template, cancellationToken);
        return Result<WorkoutTemplateResponse>.Success(template.ToResponse());
    }
}

