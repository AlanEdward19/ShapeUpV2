using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Foods.Shared;

internal static class FoodMapper
{
    internal static FoodResponse ToResponse(FoodDocument food) =>
        new(
            food.Id,
            food.Name,
            food.Barcode,
            ToMacroResponse(food.MacrosPer100),
            food.MicrosPer100 is null ? null : ToMicroResponse(food.MicrosPer100),
            food.Measure is null ? null : ToMeasureResponse(food.Measure),
            food.CreatedByUserId,
            food.CreatedAtUtc,
            IsPersonalOverride: false,
            OverrideId: null);

    internal static MacroResponseDto ToMacroResponse(MacroValueObject macros) =>
        new(macros.Kcal, macros.ProteinG, macros.CarbG, macros.FatG);

    internal static MicroResponseDto ToMicroResponse(MicroValueObject micros) =>
        new(
            micros.VitaminAMcg,
            micros.VitaminCMg,
            micros.VitaminDIu,
            micros.CalciumMg,
            micros.IronMg,
            micros.SodiumMg);

    internal static HouseholdMeasureResponseDto ToMeasureResponse(HouseholdMeasure measure) =>
        new(measure.Label, measure.GramsOrMl);

    internal static MacroValueObject ToMacroValueObject(MacroInputDto dto) =>
        new()
        {
            Kcal = dto.Kcal,
            ProteinG = dto.ProteinG,
            CarbG = dto.CarbG,
            FatG = dto.FatG
        };

    internal static MicroValueObject? ToMicroValueObject(MicroInputDto? dto)
    {
        if (dto is null)
            return null;

        return new MicroValueObject
        {
            VitaminAMcg = dto.VitaminAMcg,
            VitaminCMg = dto.VitaminCMg,
            VitaminDIu = dto.VitaminDIu,
            CalciumMg = dto.CalciumMg,
            IronMg = dto.IronMg,
            SodiumMg = dto.SodiumMg
        };
    }

    internal static HouseholdMeasure? ToHouseholdMeasure(HouseholdMeasureInputDto? dto)
    {
        if (dto is null)
            return null;

        return new HouseholdMeasure
        {
            Label = dto.Label.Trim(),
            GramsOrMl = dto.GramsOrMl
        };
    }

}
