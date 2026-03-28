namespace ShapeUp.Features.GymManagement.TrainerPlans.UpdateTrainerPlan;

using FluentValidation;
using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Results;

public record UpdateTrainerPlanCommand(int PlanId, string Name, string? Description, decimal Price, int DurationDays, bool IsActive);
public record UpdateTrainerPlanResponse(int Id, int TrainerId, string Name, string? Description, decimal Price, int DurationDays, bool IsActive);

public class UpdateTrainerPlanValidator : AbstractValidator<UpdateTrainerPlanCommand>
{
    public UpdateTrainerPlanValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationDays).GreaterThan(0);
    }
}

public class UpdateTrainerPlanHandler(ITrainerPlanRepository repository, IValidator<UpdateTrainerPlanCommand> validator)
{
    public async Task<Result<UpdateTrainerPlanResponse>> HandleAsync(UpdateTrainerPlanCommand command, int trainerId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<UpdateTrainerPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var plan = await repository.GetByIdAsync(command.PlanId, cancellationToken);
        if (plan is null) return Result<UpdateTrainerPlanResponse>.Failure(GymManagementErrors.TrainerPlanNotFound(command.PlanId));
        if (plan.TrainerId != trainerId) return Result<UpdateTrainerPlanResponse>.Failure(GymManagementErrors.TrainerPlanDoesNotBelongToTrainer(command.PlanId, trainerId));

        plan.Name = command.Name;
        plan.Description = command.Description;
        plan.Price = command.Price;
        plan.DurationDays = command.DurationDays;
        plan.IsActive = command.IsActive;

        await repository.UpdateAsync(plan, cancellationToken);
        return Result<UpdateTrainerPlanResponse>.Success(new UpdateTrainerPlanResponse(plan.Id, plan.TrainerId, plan.Name, plan.Description, plan.Price, plan.DurationDays, plan.IsActive));
    }
}

