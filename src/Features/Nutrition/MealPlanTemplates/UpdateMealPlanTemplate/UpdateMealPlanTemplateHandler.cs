using FluentValidation;
using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlanTemplates.CreateMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared;
using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.UpdateMealPlanTemplate;

public class UpdateMealPlanTemplateHandler(
    IMealPlanTemplateRepository templateRepository,
    IFoodRepository foodRepository,
    IValidator<SaveMealPlanTemplateCommand> validator)
{
    public async Task<Result<MealPlanTemplateResponse>> HandleAsync(
        string templateId,
        SaveMealPlanTemplateCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MealPlanTemplateResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var template = await templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
            return Result<MealPlanTemplateResponse>.Failure(NutritionErrors.MealPlanTemplateNotFound(templateId));

        if (template.CreatedByUserId != actorUserId)
            return Result<MealPlanTemplateResponse>.Failure(CommonErrors.Forbidden("You can only change templates created by you."));

        foreach (var item in command.Items)
        {
            if (await foodRepository.GetByIdAsync(item.FoodId, cancellationToken) is null)
                return Result<MealPlanTemplateResponse>.Failure(NutritionErrors.FoodNotFound(item.FoodId));
        }

        template.Name = command.Name.Trim();
        template.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
        template.UpdatedAtUtc = DateTime.UtcNow;
        template.Items = command.Items
            .Select(i => new MealPlanItemDocument
            {
                MealSlot = MealPlanMapper.NormalizeMealSlot(i.MealSlot),
                FoodId = i.FoodId,
                QuantityGramsOrMl = i.QuantityGramsOrMl
            })
            .ToList();

        await templateRepository.UpdateAsync(template, cancellationToken);
        return Result<MealPlanTemplateResponse>.Success(MealPlanTemplateMapper.ToResponse(template));
    }
}
