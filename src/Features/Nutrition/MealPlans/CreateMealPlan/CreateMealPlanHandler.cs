using FluentValidation;
using MongoDB.Bson;
using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;

public class CreateMealPlanHandler(
    IMealPlanRepository mealPlanRepository,
    IFoodRepository foodRepository,
    IValidator<CreateMealPlanCommand> validator)
{
    public async Task<Result<MealPlanResponse>> HandleAsync(
        CreateMealPlanCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MealPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        foreach (var item in command.Items)
        {
            if (await foodRepository.GetByIdAsync(item.FoodId, cancellationToken) is null)
                return Result<MealPlanResponse>.Failure(NutritionErrors.FoodNotFound(item.FoodId));
        }

        var nowUtc = DateTime.UtcNow;
        var plan = new MealPlanDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = actorUserId,
            Name = command.Name.Trim(),
            PrescribedByRelationshipId = null,
            IsActive = false,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Items = command.Items
                .Select(i => new MealPlanItemDocument
                {
                    MealSlot = NormalizeMealSlot(i.MealSlot),
                    FoodId = i.FoodId,
                    QuantityGramsOrMl = i.QuantityGramsOrMl
                })
                .ToList()
        };

        await mealPlanRepository.CreateAsync(plan, cancellationToken);

        return Result<MealPlanResponse>.Success(MealPlanMapper.ToResponse(plan));
    }

    private static string NormalizeMealSlot(string mealSlot) =>
        mealSlot.ToLowerInvariant() switch
        {
            "breakfast" => "breakfast",
            "lunch" => "lunch",
            "dinner" => "dinner",
            _ => "snack"
        };
}
