using FluentValidation;
using MongoDB.Bson;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared;
using ShapeUp.Features.Nutrition.MealPlanTemplates.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlanTemplates.CreateMealPlanTemplate;

public class CreateMealPlanTemplateHandler(
    IMealPlanTemplateRepository templateRepository,
    IFoodRepository foodRepository,
    IProfessionalCapabilityService capabilityService,
    IValidator<SaveMealPlanTemplateCommand> validator)
{
    public async Task<Result<MealPlanTemplateResponse>> HandleAsync(
        SaveMealPlanTemplateCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var capabilities = await capabilityService.GetAsync(actorUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return Result<MealPlanTemplateResponse>.Failure(CommonErrors.Forbidden("Nutrition capability is required."));

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MealPlanTemplateResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        foreach (var item in command.Items)
        {
            if (await foodRepository.GetByIdAsync(item.FoodId, cancellationToken) is null)
                return Result<MealPlanTemplateResponse>.Failure(NutritionErrors.FoodNotFound(item.FoodId));
        }

        var nowUtc = DateTime.UtcNow;
        var template = new MealPlanTemplateDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            CreatedByUserId = actorUserId,
            Name = command.Name.Trim(),
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Items = command.Items
                .Select(i => new MealPlanItemDocument
                {
                    MealSlot = MealPlanMapper.NormalizeMealSlot(i.MealSlot),
                    FoodId = i.FoodId,
                    QuantityGramsOrMl = i.QuantityGramsOrMl
                })
                .ToList()
        };

        await templateRepository.CreateAsync(template, cancellationToken);
        return Result<MealPlanTemplateResponse>.Success(MealPlanTemplateMapper.ToResponse(template));
    }
}
