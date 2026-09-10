namespace ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;

public record MacroInputDto(int Kcal, int ProteinG, int CarbG, int FatG);

public record MicroInputDto(
    decimal? VitaminAMcg,
    decimal? VitaminCMg,
    decimal? VitaminDIu,
    decimal? CalciumMg,
    decimal? IronMg,
    decimal? SodiumMg);

public record HouseholdMeasureInputDto(string Label, decimal GramsOrMl);

public record MacroResponseDto(int Kcal, int ProteinG, int CarbG, int FatG);

public record MicroResponseDto(
    decimal? VitaminAMcg,
    decimal? VitaminCMg,
    decimal? VitaminDIu,
    decimal? CalciumMg,
    decimal? IronMg,
    decimal? SodiumMg);

public record HouseholdMeasureResponseDto(string Label, decimal GramsOrMl);

public record FoodResponse(
    string Id,
    string Name,
    string? Barcode,
    MacroResponseDto MacrosPer100,
    MicroResponseDto? MicrosPer100,
    HouseholdMeasureResponseDto? Measure,
    int CreatedByUserId,
    DateTime CreatedAtUtc);
