namespace ShapeUp.Features.Training.Workouts.StartWorkoutExecution;

/// <summary>
/// <paramref name="Id"/> is an optional client-supplied session id (24-hex-char, Mongo
/// ObjectId format) -- lets an offline client generate the id up front and use it locally
/// for the whole session (state sync, finish, cancel) before the start request itself has
/// synced. Falls back to a server-generated id when omitted.
/// </summary>
public record StartWorkoutExecutionCommand(string PlanId, DateTime StartedAtUtc, int? ExecutedByUserId, string? Id = null);

