using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.ExerciseRequests.Shared;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.ExerciseRequests.ApproveExerciseRequest;

public record ApproveExerciseRequestCommand(int RequestId, ApproveExerciseRequestBody Body);

public class ApproveExerciseRequestCommandValidator : AbstractValidator<ApproveExerciseRequestCommand>
{
    public ApproveExerciseRequestCommandValidator() =>
        RuleFor(x => x.Body.AdminNote).Must(n => (n?.Trim().Length ?? 0) <= ExerciseRequestCatalog.MaxAdminNoteLength)
            .WithMessage($"AdminNote must have at most {ExerciseRequestCatalog.MaxAdminNoteLength} characters.");
}

/// <summary>
/// Validates the full exercise form with the same rules as <c>POST exercises</c>, then creates the exercise and approves
/// the request in one transaction (one SaveChanges), linking both ways. A request that is not pending is a 409, including
/// the loser of two concurrent approvals (row version).
/// </summary>
public class ApproveExerciseRequestHandler(
    IExerciseRequestRepository requestRepository,
    IEquipmentRepository equipmentRepository,
    IValidator<CreateExerciseCommand> exerciseValidator,
    IValidator<ApproveExerciseRequestCommand> validator,
    ExerciseRequestResponseBuilder responseBuilder,
    IExerciseRequestSideEffects sideEffects)
{
    public async Task<Result<ExerciseRequestResponse>> HandleAsync(
        ApproveExerciseRequestCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<ExerciseRequestResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var request = await requestRepository.GetByIdAsync(command.RequestId, cancellationToken);
        if (request is null)
            return Result<ExerciseRequestResponse>.Failure(TrainingErrors.ExerciseRequestNotFound(command.RequestId));
        if (request.Status != ExerciseRequest.StatusPending)
            return Result<ExerciseRequestResponse>.Failure(TrainingErrors.ExerciseRequestAlreadyReviewed(request.Id));

        var body = command.Body;
        var create = new CreateExerciseCommand(
            body.Name, body.NamePt, body.Description, body.VideoUrl, body.Muscles ?? [], body.EquipmentIds ?? [], body.Steps, body.ExerciseType);
        var exerciseValidation = await exerciseValidator.ValidateAsync(create, cancellationToken);
        if (!exerciseValidation.IsValid)
            return Result<ExerciseRequestResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", exerciseValidation.Errors.Select(x => x.ErrorMessage))));

        foreach (var equipmentId in create.EquipmentIds.Distinct())
        {
            if (await equipmentRepository.GetByIdAsync(equipmentId, cancellationToken) is null)
                return Result<ExerciseRequestResponse>.Failure(TrainingErrors.EquipmentNotFound(equipmentId));
        }

        var exercise = CreateExerciseHandler.BuildExercise(create);
        exercise.ExerciseRequestId = request.Id;

        var nowUtc = DateTime.UtcNow;
        request.Status = ExerciseRequest.StatusApproved;
        request.ReviewedByUserId = actorUserId;
        request.ReviewedAtUtc = nowUtc;
        request.ApprovedExercise = exercise;
        request.ApprovedExerciseName = exercise.Name;
        request.AdminNote = ExerciseRequestCatalog.NormalizeOptional(body.AdminNote);

        try
        {
            await requestRepository.SaveAsync(exercise, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ExerciseRequestResponse>.Failure(TrainingErrors.ExerciseRequestAlreadyReviewed(request.Id));
        }

        await sideEffects.AuditAsync("approved", actorUserId, request.Id, exercise.Id, request.AdminNote, cancellationToken);
        await sideEffects.NotifyApprovedAsync(request.RequestedByUserId, exercise.Name, cancellationToken);

        var response = (await responseBuilder.BuildAsync([request], includeReviewer: true, cancellationToken))[0];
        return Result<ExerciseRequestResponse>.Success(response);
    }
}
