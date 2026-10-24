using FluentValidation;
using ShapeUp.Features.Training.Shared.Entities;

namespace ShapeUp.Features.Training.ExerciseRequests.ListExerciseRequests;

/// <summary><paramref name="Status"/>: Pending | Approved | Rejected (case-insensitive), absent = all.</summary>
public record ListExerciseRequestsQuery(string? Status, string? Cursor, int? PageSize);

public class ListExerciseRequestsQueryValidator : AbstractValidator<ListExerciseRequestsQuery>
{
    public static readonly string[] Statuses =
        [ExerciseRequest.StatusPending, ExerciseRequest.StatusApproved, ExerciseRequest.StatusRejected];

    public ListExerciseRequestsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || Statuses.Contains(s.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Status must be one of: {string.Join(", ", Statuses)}.");
    }

    public static string? Normalize(string? status) =>
        Statuses.FirstOrDefault(s => string.Equals(s, status?.Trim(), StringComparison.OrdinalIgnoreCase));
}
