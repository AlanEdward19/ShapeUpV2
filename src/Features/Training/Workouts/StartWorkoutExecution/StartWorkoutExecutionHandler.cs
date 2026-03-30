using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Workouts.StartWorkoutExecution;

public class StartWorkoutExecutionHandler(
    IWorkoutPlanRepository workoutPlanRepository,
    IWorkoutSessionRepository workoutSessionRepository,
    ITrainingAccessPolicy accessPolicy,
    IWorkoutSessionResponseMapper workoutSessionResponseMapper,
    IValidator<StartWorkoutExecutionCommand> validator)
{
    public async Task<Result<WorkoutSessionResponse>> HandleAsync(
        StartWorkoutExecutionCommand command,
        int actorUserId,
        string[] actorScopes,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var plan = await workoutPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutPlanNotFound(command.PlanId));

        var canCreate = await accessPolicy.CanCreateWorkoutForAsync(actorUserId, plan.TargetUserId, actorScopes, cancellationToken);
        if (!canCreate)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.CannotCreateWorkoutForTarget(actorUserId, plan.TargetUserId));

        var executedByUserId = command.ExecutedByUserId ?? actorUserId;
        var session = new WorkoutSessionDocument
        {
            WorkoutPlanId = plan.Id,
            TargetUserId = plan.TargetUserId,
            ExecutedByUserId = executedByUserId,
            TrainerUserId = actorUserId == plan.TargetUserId ? null : actorUserId,
            StartedAtUtc = command.StartedAtUtc,
            LastSavedAtUtc = command.StartedAtUtc,
            IsCompleted = false,
            Exercises = plan.Exercises
                .Select(e => new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName,
                    Sets = e.Sets
                        .Select(s => new ExecutedSetDocumentValueObject
                        {
                            Repetitions = s.Repetitions,
                            Load = s.Load,
                            LoadUnit = s.LoadUnit,
                            SetType = s.SetType,
                            Technique = s.Technique,
                            Rpe = s.Rpe,
                            RestSeconds = s.RestSeconds,
                            IsExtra = false
                        })
                        .ToList()
                })
                .ToList()
        };

        await workoutSessionRepository.AddAsync(session, cancellationToken);
        return Result<WorkoutSessionResponse>.Success(workoutSessionResponseMapper.Map(session));
    }
}

