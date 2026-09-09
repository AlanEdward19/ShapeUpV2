using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.WorkoutPlans.CreateWorkoutPlan;

/// <summary>
/// <paramref name="Id"/> is an optional client-supplied plan id (24-hex-char, Mongo ObjectId
/// format) -- lets an offline client generate the id up front and reference it immediately
/// (edit, delete, copy, start a workout from it) before the create request itself has synced.
/// Falls back to a server-generated id when omitted.
/// </summary>
public record CreateWorkoutPlanCommand(
    int TargetUserId,
    string Name,
    string? Notes,
    int DurationInWeeks,
    string Phase,
    Difficulty Difficulty,
    BlockDto[] Blocks,
    string? Id = null);

