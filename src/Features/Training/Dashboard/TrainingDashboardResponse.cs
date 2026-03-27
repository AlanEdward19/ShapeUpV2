namespace ShapeUp.Features.Training.Dashboard;

public record TrainingDashboardResponse(
    decimal WeeklyVolume,
    int ConsecutiveTrainingDays,
    int SessionsCompletedThisWeek,
    int SessionsTargetPerWeek,
    decimal SessionsCompletionRate,
    int PersonalRecordsThisWeek,
    decimal WeeklyVolumeProgressPercent);