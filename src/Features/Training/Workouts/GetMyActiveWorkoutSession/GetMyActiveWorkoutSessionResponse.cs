using ShapeUp.Features.Training.Workouts.Shared.ViewModels;

namespace ShapeUp.Features.Training.Workouts.GetMyActiveWorkoutSession;

public record GetMyActiveWorkoutSessionResponse(bool HasActiveWorkout, WorkoutSessionResponse? Session);

