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

    public static Error WorkoutPlanNotFound(string planId) =>
        CommonErrors.NotFound($"Workout plan '{planId}' was not found.");

    public static Error WorkoutTemplateNotFound(string templateId) =>
        CommonErrors.NotFound($"Workout template '{templateId}' was not found.");

    public static Error WorkoutSessionAlreadyCompleted(string sessionId) =>
        CommonErrors.Conflict($"Workout session '{sessionId}' is already completed.");

    public static Error WorkoutSessionAlreadyCancelled(string sessionId) =>
        CommonErrors.Conflict($"Workout session '{sessionId}' is already cancelled.");

    public static Error CannotCreateWorkoutForTarget(int actorId, int targetUserId) =>
        CommonErrors.Forbidden($"User '{actorId}' cannot create workout session for user '{targetUserId}'.");

    public static Error WorkoutPlanNotOwned(string planId, int actorId) =>
        CommonErrors.Forbidden($"User '{actorId}' does not own workout plan '{planId}'.");

    public static Error WorkoutTemplateNotOwned(string templateId, int actorId) =>
        CommonErrors.Forbidden($"User '{actorId}' does not own workout template '{templateId}'.");

    public static Error RpeRequiredForExercise(int exerciseId) =>
        CommonErrors.Validation($"Exercise '{exerciseId}' requires RPE to be filled for all sets before it can be saved as completed.");

    public static Error DurationRequiredForExercise(int exerciseId) =>
        CommonErrors.Validation($"Exercise '{exerciseId}' is time-based and requires DurationSeconds to be filled and greater than zero for all sets.");

    public static Error TechniqueNotAllowedForTimeBasedExercise(int exerciseId) =>
        CommonErrors.Validation($"Exercise '{exerciseId}' is time-based and only supports the Straight technique for its sets.");

    public static Error ExerciseAlreadyInSession(int exerciseId) =>
        CommonErrors.Validation($"Exercise '{exerciseId}' is already present in this workout session.");

    public static Error ExercisesNotEquivalent(int originalExerciseId, int newExerciseId) =>
        CommonErrors.Validation($"Exercise '{newExerciseId}' is not registered as an equivalent of '{originalExerciseId}'.");

    public static Error OriginalExerciseNotInSession(int exerciseId) =>
        CommonErrors.Validation($"Exercise '{exerciseId}' is not part of this workout session.");
}
