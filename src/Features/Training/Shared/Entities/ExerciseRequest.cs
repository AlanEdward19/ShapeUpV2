namespace ShapeUp.Features.Training.Shared.Entities;

using Enums;

/// <summary>A user's request for an exercise missing from the library; an admin reviews it and, on approval, creates the exercise from it.</summary>
public class ExerciseRequest
{
    public const string StatusPending = "Pending";
    public const string StatusApproved = "Approved";
    public const string StatusRejected = "Rejected";

    public int Id { get; set; }
    public string Status { get; set; } = StatusPending;
    public required string Name { get; set; }
    /// <summary>weight | cardio | isometric | stretch | mobility</summary>
    public required string Kind { get; set; }
    public ExerciseType ExerciseType { get; set; }
    /// <summary>Comma-separated library muscle group names; empty for cardio.</summary>
    public string MuscleGroups { get; set; } = string.Empty;
    /// <summary>Comma-separated equipment ids.</summary>
    public string EquipmentIds { get; set; } = string.Empty;
    public string? EquipmentOther { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceUrl { get; set; }
    public int RequestedByUserId { get; set; }
    /// <summary>Role of the requester when the request was made: Trainer, Nutritionist, IndependentClient, Client or GymClient.</summary>
    public required string RequestedByRole { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    /// <summary>The created exercise. Set to null (the name snapshot stays) if the exercise is later deleted.</summary>
    public int? ApprovedExerciseId { get; set; }
    public Exercise? ApprovedExercise { get; set; }
    public string? ApprovedExerciseName { get; set; }
    public string? RejectionReason { get; set; }
    public string? AdminNote { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
