using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.Shared.ViewModels;

public record WorkoutSessionResponse(
    string SessionId,
    string? WorkoutPlanId,
    int TargetUserId,
    int ExecutedByUserId,
    int? TrainerUserId,
    DateTime StartedAtUtc,
    DateTime? LastSavedAtUtc,
    DateTime? EndedAtUtc,
    int? DurationSeconds,
    int? PerceivedExertion,
    bool IsCompleted,
    bool IsCancelled,
    DateTime? CancelledAtUtc,
    ExecutedExerciseDto[] Exercises,
    WorkoutPrDto[] PersonalRecords);