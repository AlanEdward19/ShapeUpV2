using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.ExerciseRequests.Shared;

public static class ExerciseRequestCatalog
{
    public const string KindWeight = "weight";
    public const string KindCardio = "cardio";

    public static readonly string[] Kinds = ["weight", "cardio", "isometric", "stretch", "mobility"];

    public static readonly string[] MuscleGroups =
    [
        "Chest", "Back", "Shoulders", "Biceps", "Triceps", "Forearms", "Abs",
        "Quadriceps", "Hamstrings", "Glutes", "Calves", "HipFlexors"
    ];

    public const int MaxPendingPerUser = 10;
    public const int MaxReasonLength = 280;
    public const int MaxAdminNoteLength = 500;

    public static string? NormalizeKind(string? kind) =>
        Kinds.FirstOrDefault(k => string.Equals(k, kind?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string? NormalizeMuscleGroup(string? group) =>
        MuscleGroups.FirstOrDefault(g => string.Equals(g, group?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary><c>weight</c> is weight based; every other kind is time based.</summary>
    public static ExerciseType ExerciseTypeFor(string kind) =>
        kind == KindWeight ? ExerciseType.WeightBased : ExerciseType.TimeBased;

    public static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
