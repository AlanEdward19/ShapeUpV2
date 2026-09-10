using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.Foods.CreateFoodOverride;

public record CreateFoodOverrideCommand(
    string FoodId,
    MacroInputDto MacrosPer100,
    MicroInputDto? MicrosPer100);
