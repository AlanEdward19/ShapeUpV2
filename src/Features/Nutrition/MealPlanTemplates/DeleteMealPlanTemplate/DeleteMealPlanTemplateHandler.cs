using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.DeleteMealPlanTemplate;

public class DeleteMealPlanTemplateHandler(IMealPlanTemplateRepository templateRepository)
{
    public async Task<Result> HandleAsync(string templateId, int actorUserId, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
            return Result.Failure(NutritionErrors.MealPlanTemplateNotFound(templateId));

        if (template.CreatedByUserId != actorUserId)
            return Result.Failure(CommonErrors.Forbidden("You can only delete templates created by you."));

        await templateRepository.DeleteAsync(templateId, cancellationToken);
        return Result.Success();
    }
}
