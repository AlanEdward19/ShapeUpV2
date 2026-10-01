namespace ShapeUp.Features.Training.Dashboard;

public record WeeklyReadingResponse(
    DateTime WeekStartUtc,
    int DaysWithWork,
    IReadOnlyList<ExerciseLoadTrendResponse> LoadTrends);

public record ExerciseLoadTrendResponse(
    int ExerciseId,
    string ExerciseName,
    decimal CurrentMaxLoad,
    decimal? PreviousMaxLoad,
    string Trend);

public static class LoadTrend
{
    public const string Up = "up";
    public const string Same = "same";
    public const string Down = "down";
    public const string NoPrevious = "noPrevious";
}
