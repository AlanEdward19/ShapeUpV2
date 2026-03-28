namespace ShapeUp.Features.Training.Shared.Errors;

using ShapeUp.Shared.Results;

public static class TrainingErrors
{
    public static Error ExerciseNotFound(int exerciseId) =>
        CommonErrors.NotFound($"Exercise '{exerciseId}' was not found.");

    public static Error EquipmentNotFound(int equipmentId) =>
        CommonErrors.NotFound($"Equipment '{equipmentId}' was not found.");

    public static Error MuscleNotFound(int muscleId) =>
        CommonErrors.NotFound($"Muscle '{muscleId}' was not found.");

    public static Error WorkoutSessionNotFound(string sessionId) =>
        CommonErrors.NotFound($"Workout session '{sessionId}' was not found.");

    public static Error WorkoutSessionAlreadyCompleted(string sessionId) =>
        CommonErrors.Conflict($"Workout session '{sessionId}' is already completed.");

    public static Error CannotCreateWorkoutForTarget(int actorId, int targetUserId) =>
        CommonErrors.Forbidden($"User '{actorId}' cannot create workout session for user '{targetUserId}'.");
}
