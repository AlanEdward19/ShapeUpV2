namespace ShapeUp.Features.GymManagement.TrainerPlans.DeleteTrainerPlan;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public record DeleteTrainerPlanCommand(int PlanId, int TrainerId);

public class DeleteTrainerPlanHandler(ITrainerPlanRepository repository)
{
    public async Task<Result> HandleAsync(DeleteTrainerPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await repository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null) return Result.Failure(GymManagementErrors.TrainerPlanNotFound(command.PlanId));
        if (plan.TrainerId != command.TrainerId) return Result.Failure(GymManagementErrors.TrainerPlanDoesNotBelongToTrainer(command.PlanId, command.TrainerId));

        await repository.DeleteAsync(command.PlanId, cancellationToken);
        return Result.Success();
    }
}

