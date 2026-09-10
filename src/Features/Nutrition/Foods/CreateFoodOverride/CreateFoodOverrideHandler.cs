using FluentValidation;
using MongoDB.Bson;
using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Foods.CreateFoodOverride;

public class CreateFoodOverrideHandler(
    IFoodRepository foodRepository,
    IFoodOverrideRepository foodOverrideRepository,
    IFoodModerationRepository foodModerationRepository,
    IValidator<CreateFoodOverrideCommand> validator)
{
    public async Task<Result<FoodResponse>> HandleAsync(
        CreateFoodOverrideCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<FoodResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var food = await foodRepository.GetByIdAsync(command.FoodId, cancellationToken);
        if (food is null)
            return Result<FoodResponse>.Failure(NutritionErrors.FoodNotFound(command.FoodId));

        var macros = FoodMapper.ToMacroValueObject(command.MacrosPer100);
        var micros = FoodMapper.ToMicroValueObject(command.MicrosPer100);
        var existingOverride = await foodOverrideRepository.GetForUserAsync(command.FoodId, actorUserId, cancellationToken);

        FoodOverrideDocument overrideDocument;
        if (existingOverride is null)
        {
            overrideDocument = new FoodOverrideDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FoodId = command.FoodId,
                UserId = actorUserId,
                MacrosPer100 = macros,
                MicrosPer100 = micros,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            await foodOverrideRepository.CreateAsync(overrideDocument, cancellationToken);
        }
        else
        {
            existingOverride.MacrosPer100 = macros;
            existingOverride.MicrosPer100 = micros;
            existingOverride.IsActive = true;
            await foodOverrideRepository.UpdateAsync(existingOverride, cancellationToken);
            overrideDocument = existingOverride;
        }

        var moderationRequest = new FoodModerationRequestDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FoodId = command.FoodId,
            FoodOverrideId = overrideDocument.Id,
            RequestedByUserId = actorUserId,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };
        await foodModerationRepository.CreateAsync(moderationRequest, cancellationToken);

        return Result<FoodResponse>.Success(FoodVersionResolver.Resolve(food, overrideDocument));
    }
}
