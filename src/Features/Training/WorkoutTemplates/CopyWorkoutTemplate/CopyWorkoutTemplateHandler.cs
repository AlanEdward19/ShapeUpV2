using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutTemplates.Shared;
using ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutTemplates.CopyWorkoutTemplate;

public class CopyWorkoutTemplateHandler(
    IWorkoutTemplateRepository workoutTemplateRepository,
    IValidator<CopyWorkoutTemplateCommand> validator)
{
    public async Task<Result<WorkoutTemplateResponse>> HandleAsync(CopyWorkoutTemplateCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutTemplateResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var source = await workoutTemplateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
        if (source is null)
            return Result<WorkoutTemplateResponse>.Failure(TrainingErrors.WorkoutTemplateNotFound(command.TemplateId));

        if (source.CreatedByUserId != actorUserId)
            return Result<WorkoutTemplateResponse>.Failure(CommonErrors.Forbidden("You can only copy templates created by you."));

        var nowUtc = DateTime.UtcNow;
        var copy = new WorkoutTemplateDocument
        {
            CreatedByUserId = actorUserId,
            Name = string.IsNullOrWhiteSpace(command.Name) ? $"{source.Name} (copy)" : command.Name.Trim(),
            Notes = source.Notes,
            DurationInWeeks = source.DurationInWeeks,
            Phase = source.Phase,
            Difficulty = source.Difficulty,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Exercises = source.Exercises
                .Select(e => new PlannedExerciseDocumentValueObject
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName,
                    Sets = e.Sets
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
                .ToList()
        };

        await workoutTemplateRepository.AddAsync(copy, cancellationToken);
        return Result<WorkoutTemplateResponse>.Success(copy.ToResponse());
    }
}

