using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.Workouts.Shared.Dtos;

public record BlockDto(
    BlockType Type,
    WorkoutExerciseDto[] Exercises,
    int? TimeCapSeconds = null,
    int? IntervalSeconds = null,
    int? TotalRounds = null,
    int? RestAfterSeconds = null);
