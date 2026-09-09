using FluentValidation;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutTemplates.Shared;
using ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutTemplates.CreateWorkoutTemplate;

public class CreateWorkoutTemplateHandler(
    IWorkoutTemplateRepository workoutTemplateRepository,
    IExerciseRepository exerciseRepository,
    IValidator<CreateWorkoutTemplateCommand> validator)
{
    public async Task<Result<WorkoutTemplateResponse>> HandleAsync(CreateWorkoutTemplateCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutTemplateResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

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

        var nowUtc = DateTime.UtcNow;
        var template = new WorkoutTemplateDocument
        {
            CreatedByUserId = actorUserId,
            Name = command.Name.Trim(),
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            DurationInWeeks = command.DurationInWeeks,
            Phase = command.Phase.Trim(),
            Difficulty = command.Difficulty,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Blocks = blocks
        };

        await workoutTemplateRepository.AddAsync(template, cancellationToken);
        return Result<WorkoutTemplateResponse>.Success(template.ToResponse());
    }
}

