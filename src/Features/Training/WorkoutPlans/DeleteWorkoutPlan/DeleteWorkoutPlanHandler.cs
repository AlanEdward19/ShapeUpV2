using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutPlans.DeleteWorkoutPlan;

public class DeleteWorkoutPlanHandler(IWorkoutPlanRepository workoutPlanRepository)
{
    public async Task<Result> HandleAsync(DeleteWorkoutPlanCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var plan = await workoutPlanRepository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null)
            return Result.Failure(TrainingErrors.WorkoutPlanNotFound(command.PlanId));

        if (plan.CreatedByUserId != actorUserId)
            return Result.Failure(TrainingErrors.WorkoutPlanNotOwned(command.PlanId, actorUserId));

        await workoutPlanRepository.DeleteAsync(command.PlanId, cancellationToken);
        return Result.Success();
    }
}

