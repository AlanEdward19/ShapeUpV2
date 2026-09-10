using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Documents;

namespace ShapeUp.Features.Nutrition.Foods.Shared;

public static class FoodVersionResolver
{
    public static FoodResponse Resolve(FoodDocument publicFood, FoodOverrideDocument? activeOverride)
    {
        if (activeOverride is { IsActive: true })
        {
            return new FoodResponse(
                publicFood.Id,
                publicFood.Name,
                publicFood.Barcode,
                FoodMapper.ToMacroResponse(activeOverride.MacrosPer100),
                activeOverride.MicrosPer100 is null ? null : FoodMapper.ToMicroResponse(activeOverride.MicrosPer100),
                publicFood.Measure is null ? null : FoodMapper.ToMeasureResponse(publicFood.Measure),
                publicFood.CreatedByUserId,
                publicFood.CreatedAtUtc,
                IsPersonalOverride: true,
                OverrideId: activeOverride.Id);
        }

        return FoodMapper.ToResponse(publicFood);
    }
}
