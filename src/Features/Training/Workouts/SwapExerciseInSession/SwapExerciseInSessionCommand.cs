using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace ShapeUp.Features.Training.Workouts.SwapExerciseInSession;

public record SwapExerciseInSessionCommand(
    string SessionId,
    int OriginalExerciseId,
    int NewExerciseId,
    WorkoutSetValueObject[] RetainedSetsForOriginal);
