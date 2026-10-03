using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared;
using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.GetMealPlanTemplates;

public class GetMealPlanTemplatesHandler(IMealPlanTemplateRepository templateRepository)
{
    public async Task<Result<MealPlanTemplateResponse[]>> HandleAsync(int actorUserId, CancellationToken cancellationToken)
    {
        var templates = await templateRepository.GetByCreatorAsync(actorUserId, cancellationToken);
        return Result<MealPlanTemplateResponse[]>.Success(templates.Select(MealPlanTemplateMapper.ToResponse).ToArray());
    }
}
