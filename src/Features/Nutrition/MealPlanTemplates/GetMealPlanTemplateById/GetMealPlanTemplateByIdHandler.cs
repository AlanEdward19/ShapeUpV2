using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared;
using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.GetMealPlanTemplateById;

public class GetMealPlanTemplateByIdHandler(IMealPlanTemplateRepository templateRepository)
{
    public async Task<Result<MealPlanTemplateResponse>> HandleAsync(string templateId, int actorUserId, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetByIdAsync(templateId, cancellationToken);
        if (template is null)
            return Result<MealPlanTemplateResponse>.Failure(NutritionErrors.MealPlanTemplateNotFound(templateId));

        if (template.CreatedByUserId != actorUserId)
            return Result<MealPlanTemplateResponse>.Failure(CommonErrors.Forbidden("You can only read templates created by you."));

        return Result<MealPlanTemplateResponse>.Success(MealPlanTemplateMapper.ToResponse(template));
    }
}
