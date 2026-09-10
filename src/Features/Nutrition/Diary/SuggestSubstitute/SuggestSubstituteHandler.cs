using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Diary.Shared;
using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Diary.SuggestSubstitute;

public class SuggestSubstituteHandler(
    NutritionDbContext dbContext,
    IFoodRepository foodRepository,
    IFoodOverrideRepository foodOverrideRepository,
    IValidator<SuggestSubstituteQuery> validator)
{
    private const int CandidatePageSize = 100;

    public async Task<Result<SuggestSubstituteResponse>> HandleAsync(
        SuggestSubstituteQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<SuggestSubstituteResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var day = await dbContext.DiaryDays
            .AsNoTracking()
            .Include(d => d.Entries)
            .FirstOrDefaultAsync(d => d.UserId == actorUserId && d.Date == query.Date, cancellationToken);

        var entry = day?.Entries.FirstOrDefault(e => e.Id == query.EntryId);
        if (entry is null)
            return Result<SuggestSubstituteResponse>.Failure(NutritionErrors.DiaryEntryNotFound(query.EntryId));

        var originalFood = await foodRepository.GetByIdAsync(entry.FoodId, cancellationToken);
        if (originalFood is null)
            return Result<SuggestSubstituteResponse>.Failure(NutritionErrors.FoodNotFound(entry.FoodId));

        var originalOverride = await foodOverrideRepository.GetActiveForUserAsync(entry.FoodId, actorUserId, cancellationToken);
        var originalResolved = FoodVersionResolver.Resolve(originalFood, originalOverride);
        var originalMacros = new MacroValueObject
        {
            Kcal = originalResolved.MacrosPer100.Kcal,
            ProteinG = originalResolved.MacrosPer100.ProteinG,
            CarbG = originalResolved.MacrosPer100.CarbG,
            FatG = originalResolved.MacrosPer100.FatG
        };

        var candidates = new List<(FoodDocument Food, FoodOverrideDocument? Override, double Distance)>();
        string? cursor = null;

        do
        {
            var (items, nextCursor) = await foodRepository.SearchAsync(string.Empty, CandidatePageSize, cursor, cancellationToken);
            cursor = nextCursor;

            var overrides = await foodOverrideRepository.GetActiveForUserByFoodIdsAsync(
                actorUserId,
                items.Select(x => x.Id),
                cancellationToken);
            var overrideByFoodId = overrides.ToDictionary(x => x.FoodId, x => x);

            foreach (var food in items.Where(f => f.Id != entry.FoodId))
            {
                overrideByFoodId.TryGetValue(food.Id, out var activeOverride);
                var resolved = FoodVersionResolver.Resolve(food, activeOverride);
                var candidateMacros = new MacroValueObject
                {
                    Kcal = resolved.MacrosPer100.Kcal,
                    ProteinG = resolved.MacrosPer100.ProteinG,
                    CarbG = resolved.MacrosPer100.CarbG,
                    FatG = resolved.MacrosPer100.FatG
                };

                var distance = MacroSimilarityCalculator.CalculateNormalizedDistance(originalMacros, candidateMacros);
                candidates.Add((food, activeOverride, distance));
            }
        }
        while (cursor is not null);

        var suggestions = candidates
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Food.Name, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Select(x =>
            {
                var resolved = FoodVersionResolver.Resolve(x.Food, x.Override);
                return new SubstituteSuggestionDto(
                    x.Food.Id,
                    x.Food.Name,
                    x.Distance,
                    DiaryMapper.ToTotalsDto(new MacroValueObject
                    {
                        Kcal = resolved.MacrosPer100.Kcal,
                        ProteinG = resolved.MacrosPer100.ProteinG,
                        CarbG = resolved.MacrosPer100.CarbG,
                        FatG = resolved.MacrosPer100.FatG
                    }));
            })
            .ToArray();

        return Result<SuggestSubstituteResponse>.Success(new SuggestSubstituteResponse(suggestions));
    }
}
