using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Shared;

public static class MacroCalculator
{
    public static MacroValueObject CalculateFromPer100(MacroValueObject macrosPer100, decimal quantityGramsOrMl)
    {
        var factor = quantityGramsOrMl / 100m;

        return new MacroValueObject
        {
            Kcal = ScaleInt(macrosPer100.Kcal, factor),
            ProteinG = ScaleInt(macrosPer100.ProteinG, factor),
            CarbG = ScaleInt(macrosPer100.CarbG, factor),
            FatG = ScaleInt(macrosPer100.FatG, factor)
        };
    }

    public static MacroValueObject Sum(IEnumerable<MacroValueObject> macros) =>
        macros.Aggregate(
            new MacroValueObject(),
            (total, current) => new MacroValueObject
            {
                Kcal = total.Kcal + current.Kcal,
                ProteinG = total.ProteinG + current.ProteinG,
                CarbG = total.CarbG + current.CarbG,
                FatG = total.FatG + current.FatG
            });

    private static int ScaleInt(int value, decimal factor) =>
        (int)Math.Round(value * factor, MidpointRounding.AwayFromZero);
}
