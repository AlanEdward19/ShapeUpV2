using FluentValidation;
using ShapeUp.Features.Training.ExerciseRequests.Shared;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.ExerciseRequests.CreateExerciseRequest;

public record CreateExerciseRequestCommand(
    string Name,
    string Kind,
    ExerciseType ExerciseType,
    string[]? MuscleGroups,
    int[]? EquipmentIds,
    string? EquipmentOther,
    string? Notes,
    string? ReferenceUrl);

public class CreateExerciseRequestCommandValidator : AbstractValidator<CreateExerciseRequestCommand>
{
    public CreateExerciseRequestCommandValidator()
    {
        RuleFor(x => x.Name).Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Name is required.")
            .Must(n => (n?.Trim().Length ?? 0) <= 160).WithMessage("Name must have at most 160 characters.");

        RuleFor(x => x.Kind)
            .Must(k => ExerciseRequestCatalog.NormalizeKind(k) is not null)
            .WithMessage($"Kind must be one of: {string.Join(", ", ExerciseRequestCatalog.Kinds)}.");

        RuleFor(x => x.ExerciseType).IsInEnum();
        RuleFor(x => x).Must(x => ExerciseRequestCatalog.NormalizeKind(x.Kind) is not { } kind
                || ExerciseRequestCatalog.ExerciseTypeFor(kind) == x.ExerciseType)
            .WithMessage("Exercise type must be weightBased for kind 'weight' and timeBased for every other kind.");

        RuleFor(x => x.MuscleGroups).Must(g => (g ?? []).All(m => ExerciseRequestCatalog.NormalizeMuscleGroup(m) is not null))
            .WithMessage($"Muscle groups must be among: {string.Join(", ", ExerciseRequestCatalog.MuscleGroups)}.");
        RuleFor(x => x).Must(x => (x.MuscleGroups?.Length ?? 0) > 0
                || string.Equals(ExerciseRequestCatalog.NormalizeKind(x.Kind), ExerciseRequestCatalog.KindCardio, StringComparison.Ordinal))
            .WithMessage("At least one muscle group is required unless the kind is cardio.");

        RuleFor(x => x.EquipmentIds).Must(ids => (ids ?? []).All(id => id > 0))
            .WithMessage("Equipment ids must be positive.");
        RuleFor(x => x.EquipmentOther).Must(v => (v?.Trim().Length ?? 0) <= 120)
            .WithMessage("EquipmentOther must have at most 120 characters.");
        RuleFor(x => x.Notes).Must(v => (v?.Trim().Length ?? 0) <= 1000)
            .WithMessage("Notes must have at most 1000 characters.");
        RuleFor(x => x.ReferenceUrl)
            .Must(v => (v?.Trim().Length ?? 0) <= 300).WithMessage("ReferenceUrl must have at most 300 characters.")
            .Must(BeHttpUrlOrEmpty).WithMessage("ReferenceUrl must be an http or https URL.");
    }

    private static bool BeHttpUrlOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
}
