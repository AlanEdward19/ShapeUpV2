namespace ShapeUp.Features.GymManagement.TrainerPlans.CreateTrainerPlan;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using ShapeUp.Shared.Results;

public class CreateTrainerPlanHandler(ITrainerPlanRepository repository, IValidator<CreateTrainerPlanCommand> validator)
{
    public async Task<Result<CreateTrainerPlanResponse>> HandleAsync(CreateTrainerPlanCommand command, int trainerId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CreateTrainerPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var plan = new TrainerPlan { TrainerId = trainerId, Name = command.Name, Description = command.Description, Price = command.Price, DurationDays = command.DurationDays };
        await repository.AddAsync(plan, cancellationToken);
        return Result<CreateTrainerPlanResponse>.Success(new CreateTrainerPlanResponse(plan.Id, plan.TrainerId, plan.Name, plan.Description, plan.Price, plan.DurationDays));
    }
}

