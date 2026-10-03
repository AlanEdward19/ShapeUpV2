using MongoDB.Bson;
using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.AssignMealPlanTemplate;

/// <summary>Copies the template into a new (inactive) meal plan of the target; the client activates it as any other plan.</summary>
public class AssignMealPlanTemplateHandler(
    IMealPlanTemplateRepository templateRepository,
    IMealPlanRepository mealPlanRepository,
    INutritionAccessPolicy accessPolicy)
{
    public async Task<Result<MealPlanResponse>> HandleAsync(
        string templateId,
        int targetUserId,
        AssignMealPlanTemplateCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
            return Result<MealPlanResponse>.Failure(NutritionErrors.MealPlanTemplateNotFound(templateId));

        if (template.CreatedByUserId != actorUserId)
            return Result<MealPlanResponse>.Failure(CommonErrors.Forbidden("You can only assign templates created by you."));

        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<MealPlanResponse>.Failure(CommonErrors.Forbidden("You are not allowed to create a meal plan for this user."));

        if (command.PlanName is { Length: > 200 })
            return Result<MealPlanResponse>.Failure(CommonErrors.Validation("PlanName must have at most 200 characters."));

        var nowUtc = DateTime.UtcNow;
        var plan = new MealPlanDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = targetUserId,
            PrescribedByUserId = targetUserId == actorUserId ? null : actorUserId,
            Name = string.IsNullOrWhiteSpace(command.PlanName) ? template.Name : command.PlanName.Trim(),
            IsActive = false,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Items = template.Items
                .Select(i => new MealPlanItemDocument
                {
                    MealSlot = i.MealSlot,
                    FoodId = i.FoodId,
                    QuantityGramsOrMl = i.QuantityGramsOrMl
                })
                .ToList()
        };

        await mealPlanRepository.CreateAsync(plan, cancellationToken);
        return Result<MealPlanResponse>.Success(MealPlanMapper.ToResponse(plan));
    }
}
