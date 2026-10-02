using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Workouts.GetTodayWorkoutSession;

public record GetTodayWorkoutSessionResponse(bool HasSession, TodayWorkoutSessionDto? Session);

public record TodayWorkoutSessionDto(
    string SessionId,
    string? WorkoutPlanId,
    DateTime StartedAtUtc,
    TodayExerciseDto[] Exercises);

public record TodayExerciseDto(
    int ExerciseId,
    string ExerciseName,
    ExerciseType ExerciseType,
    TodaySetDto[] Sets);

/// <summary>
/// A set of today's session as planned by the sheet, plus what the user registered last time for that exercise.
/// <see cref="LastLoad"/> and <see cref="LastRepetitions"/> are null when the exercise was never performed: no example numbers.
/// </summary>
public record TodaySetDto(
    int? Repetitions,
    decimal? Load,
    LoadUnit LoadUnit,
    SetType SetType,
    int RestSeconds,
    int? DurationSeconds,
    decimal? DistanceMeters,
    decimal? LastLoad,
    int? LastRepetitions);
