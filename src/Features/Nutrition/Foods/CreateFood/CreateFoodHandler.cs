using FluentValidation;
using MongoDB.Bson;
using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Foods.CreateFood;

public class CreateFoodHandler(
    IFoodRepository foodRepository,
    IValidator<CreateFoodCommand> validator)
{
    public async Task<Result<FoodResponse>> HandleAsync(
        CreateFoodCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<FoodResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var normalizedBarcode = string.IsNullOrWhiteSpace(command.Barcode) ? null : command.Barcode.Trim();
        if (normalizedBarcode is not null &&
            await foodRepository.GetByBarcodeAsync(normalizedBarcode, cancellationToken) is not null)
        {
            return Result<FoodResponse>.Failure(NutritionErrors.FoodBarcodeAlreadyExists(normalizedBarcode));
        }

        var food = new FoodDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = command.Name.Trim(),
            Barcode = normalizedBarcode,
            MacrosPer100 = FoodMapper.ToMacroValueObject(command.MacrosPer100),
            MicrosPer100 = FoodMapper.ToMicroValueObject(command.MicrosPer100),
            Measure = FoodMapper.ToHouseholdMeasure(command.Measure),
            CreatedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await foodRepository.CreateAsync(food, cancellationToken);

        return Result<FoodResponse>.Success(FoodMapper.ToResponse(food));
    }
}
