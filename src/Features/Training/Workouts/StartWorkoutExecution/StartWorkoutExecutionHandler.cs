using FluentValidation;
using MongoDB.Bson;
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
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutSessionResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var plan = await workoutPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.WorkoutPlanNotFound(command.PlanId));

        var canCreate = await accessPolicy.CanCreateWorkoutForAsync(actorUserId, plan.TargetUserId, cancellationToken);
        if (!canCreate)
            return Result<WorkoutSessionResponse>.Failure(TrainingErrors.CannotCreateWorkoutForTarget(actorUserId, plan.TargetUserId));

        var executedByUserId = command.ExecutedByUserId ?? actorUserId;
        var session = new WorkoutSessionDocument
        {
            // Client-correlated id (see StartWorkoutExecutionCommand.Id) so an offline client
            // can keep using this session id for state sync/finish/cancel before this request
            // itself has synced. Falls back to a fresh server-generated id when omitted.
            Id = command.Id ?? ObjectId.GenerateNewId().ToString(),
            WorkoutPlanId = plan.Id,
            TargetUserId = plan.TargetUserId,
            ExecutedByUserId = executedByUserId,
            TrainerUserId = actorUserId == plan.TargetUserId ? null : actorUserId,
            StartedAtUtc = command.StartedAtUtc,
            LastSavedAtUtc = command.StartedAtUtc,
            IsCompleted = false,
            IsCancelled = false,
            Exercises = plan.Blocks
                .SelectMany(b => b.Exercises)
                .Select(e => new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName,
                    Sets = e.Sets
                        .Select(s => new ExecutedSetDocumentValueObject
                        {
                            Repetitions = s.Repetitions ?? 0,
                            Load = s.Load,
                            LoadUnit = s.LoadUnit,
                            SetType = s.SetType,
                            Technique = s.Technique,
                            Intensity = s.Intensity is null ? null : new IntensityDocumentValueObject { Type = s.Intensity.Type, Value = s.Intensity.Value },
                            RestSeconds = s.RestSeconds ?? 0,
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

