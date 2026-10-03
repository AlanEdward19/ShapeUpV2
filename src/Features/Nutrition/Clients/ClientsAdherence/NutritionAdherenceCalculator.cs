using ShapeUp.Features.Nutrition.GoalEvaluation;
using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace ShapeUp.Features.Nutrition.Clients.ClientsAdherence;

public record NutritionAdherence(int DaysLogged, int DaysWithinGoal, MacroValueObject? AverageConsumed);

public static class NutritionAdherenceCalculator
{
    /// <summary>
    /// <paramref name="days"/> are the diary days of one client inside the window; days without entries are ignored.
    /// Reuses the goal tolerance rule of the end-of-day evaluation.
    /// </summary>
    public static NutritionAdherence Calculate(IEnumerable<DiaryDay> days, MacroValueObject? goal)
    {
        var totals = days
            .Where(d => d.Entries.Count > 0)
            .Select(d => MacroCalculator.Sum(d.Entries.Select(e => e.ComputedMacros)))
            .ToList();

        if (totals.Count == 0)
            return new NutritionAdherence(0, 0, null);

        var within = goal is null
            ? 0
            : totals.Count(t => NutritionGoalToleranceCalculator.AreMacrosWithinTolerance(t, goal));

        var average = new MacroValueObject
        {
            Kcal = (int)Math.Round(totals.Average(t => t.Kcal)),
            ProteinG = (int)Math.Round(totals.Average(t => t.ProteinG)),
            CarbG = (int)Math.Round(totals.Average(t => t.CarbG)),
            FatG = (int)Math.Round(totals.Average(t => t.FatG))
        };

        return new NutritionAdherence(totals.Count, within, average);
    }
}
