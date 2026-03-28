using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.Shared.ViewModels;

public record WorkoutSessionResponse(
    string SessionId,
    int TargetUserId,
    int ExecutedByUserId,
    int? TrainerUserId,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int? DurationSeconds,
    int? PerceivedExertion,
    bool IsCompleted,
    ExecutedExerciseDto[] Exercises,
    WorkoutPrDto[] PersonalRecords);