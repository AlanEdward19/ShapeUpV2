using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutPlans.Shared;
using ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutPlans.CopyWorkoutPlan;

public class CopyWorkoutPlanHandler(
    IWorkoutPlanRepository workoutPlanRepository,
    ITrainingAccessPolicy accessPolicy,
    IValidator<CopyWorkoutPlanCommand> validator)
{
    public async Task<Result<WorkoutPlanResponse>> HandleAsync(
        CopyWorkoutPlanCommand command,
        int actorUserId,
        string[] actorScopes,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<WorkoutPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var source = await workoutPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (source is null)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.WorkoutPlanNotFound(command.PlanId));

        if (source.TargetUserId != actorUserId && source.CreatedByUserId != actorUserId && source.TrainerUserId != actorUserId)
            return Result<WorkoutPlanResponse>.Failure(CommonErrors.Forbidden("You are not allowed to copy this workout plan."));

        var targetUserId = command.TargetUserId ?? source.TargetUserId;
        var canCreate = await accessPolicy.CanCreateWorkoutForAsync(actorUserId, targetUserId, actorScopes, cancellationToken);
        if (!canCreate)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.CannotCreateWorkoutForTarget(actorUserId, targetUserId));

        var nowUtc = DateTime.UtcNow;
        var copyName = string.IsNullOrWhiteSpace(command.Name) ? $"{source.Name} (copy)" : command.Name.Trim();
        var copy = source.Clone(targetUserId, actorUserId, copyName, nowUtc);

        await workoutPlanRepository.AddAsync(copy, cancellationToken);
        return Result<WorkoutPlanResponse>.Success(copy.ToResponse());
    }
}

