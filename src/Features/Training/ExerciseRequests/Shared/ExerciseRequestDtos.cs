using ShapeUp.Features.Training.Exercises.Shared.Dtos;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.ExerciseRequests.Shared;

public record ExerciseRequestResponse(
    int Id,
    string Status,
    string Name,
    string Kind,
    ExerciseType ExerciseType,
    string[] MuscleGroups,
    int[] EquipmentIds,
    string[] EquipmentNames,
    string? EquipmentOther,
    string? Notes,
    string? ReferenceUrl,
    int RequestedByUserId,
    string? RequestedByName,
    string RequestedByRole,
    DateTime CreatedAtUtc,
    int? ReviewedByUserId,
    string? ReviewedByName,
    DateTime? ReviewedAtUtc,
    int? ApprovedExerciseId,
    string? ExerciseName,
    string? RejectionReason,
    string? AdminNote);

/// <summary>Body of <c>POST exercise-requests/{id}/approve</c>: the create-exercise form plus an optional admin note.</summary>
public record ApproveExerciseRequestBody(
    string Name,
    string NamePt,
    string? Description,
    string? VideoUrl,
    ExerciseMuscleDto[] Muscles,
    int[] EquipmentIds,
    ExerciseStepDto[]? Steps,
    ExerciseType ExerciseType = ExerciseType.WeightBased,
    string? AdminNote = null);

public record RejectExerciseRequestBody(string? Reason);
