using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.CreateWorkoutSession;

public record CreateWorkoutSessionCommand(
    int TargetUserId,
    int ExecutedByUserId,
    DateTime StartedAtUtc,
    WorkoutExerciseDto[] Exercises);