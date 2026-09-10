using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.Foods.CreateFood;

public record CreateFoodCommand(
    string Name,
    string? Barcode,
    MacroInputDto MacrosPer100,
    MicroInputDto? MicrosPer100,
    HouseholdMeasureInputDto? Measure);
