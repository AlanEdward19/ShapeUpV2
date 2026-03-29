using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutPlans.Shared;
using ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutPlans.GetWorkoutPlanById;

public class GetWorkoutPlanByIdHandler(IWorkoutPlanRepository workoutPlanRepository)
{
    public async Task<Result<WorkoutPlanResponse>> HandleAsync(GetWorkoutPlanByIdQuery query, int actorUserId, CancellationToken cancellationToken)
    {
        var plan = await workoutPlanRepository.GetByIdAsync(query.PlanId, cancellationToken);
        if (plan is null)
            return Result<WorkoutPlanResponse>.Failure(TrainingErrors.WorkoutPlanNotFound(query.PlanId));

        if (plan.TargetUserId != actorUserId && plan.CreatedByUserId != actorUserId && plan.TrainerUserId != actorUserId)
            return Result<WorkoutPlanResponse>.Failure(CommonErrors.Forbidden("You are not allowed to access this workout plan."));

        return Result<WorkoutPlanResponse>.Success(plan.ToResponse());
    }
}

