namespace ShapeUp.Features.Nutrition.Diary.SuggestSubstitute;

public record SubstituteSuggestionDto(
    string FoodId,
    string Name,
    double Distance,
    Shared.ViewModels.MacroTotalsDto MacrosPer100);

public record SuggestSubstituteResponse(SubstituteSuggestionDto[] Suggestions);
