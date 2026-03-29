using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutTemplates.Shared;
using ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutTemplates.AssignWorkoutTemplate;

public class AssignWorkoutTemplateHandler(
    IWorkoutTemplateRepository workoutTemplateRepository,
    IWorkoutPlanRepository workoutPlanRepository,
    ITrainingAccessPolicy accessPolicy,
    IValidator<AssignWorkoutTemplateCommand> validator)
{
    public async Task<Result<WorkoutPlanResponse>> HandleAsync(
        AssignWorkoutTemplateCommand command,
        int actorUserId,
        string[] actorScopes,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var template = await workoutTemplateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
        if (template is null)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.WorkoutTemplateNotFound(command.TemplateId));

        if (template.CreatedByUserId != actorUserId)
            return Result<WorkoutPlanResponse>.Failure(CommonErrors.Forbidden("You can only assign templates created by you."));

        var canCreate = await accessPolicy.CanCreateWorkoutForAsync(actorUserId, command.TargetUserId, actorScopes, cancellationToken);
        if (!canCreate)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.CannotCreateWorkoutForTarget(actorUserId, command.TargetUserId));

        var nowUtc = DateTime.UtcNow;
        var plan = new WorkoutPlanDocument
        {
            TargetUserId = command.TargetUserId,
            CreatedByUserId = actorUserId,
            TrainerUserId = actorUserId == command.TargetUserId ? null : actorUserId,
            Name = string.IsNullOrWhiteSpace(command.PlanName) ? template.Name : command.PlanName.Trim(),
            Notes = template.Notes,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Exercises = template.Exercises
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
                            Rpe = s.Rpe,
                            RestSeconds = s.RestSeconds
                        })
                        .ToList()
                })
                .ToList()
        };

        await workoutPlanRepository.AddAsync(plan, cancellationToken);
        return Result<WorkoutPlanResponse>.Success(plan.ToPlanResponse());
    }
}

