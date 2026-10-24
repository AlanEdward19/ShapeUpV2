using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Training.ExerciseRequests.Shared;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.ExerciseRequests.RejectExerciseRequest;

public record RejectExerciseRequestCommand(int RequestId, string? Reason);

public class RejectExerciseRequestCommandValidator : AbstractValidator<RejectExerciseRequestCommand>
{
    public RejectExerciseRequestCommandValidator() =>
        RuleFor(x => x.Reason)
            .Must(r => !string.IsNullOrWhiteSpace(r)).WithMessage("Reason is required.")
            .Must(r => (r?.Trim().Length ?? 0) <= ExerciseRequestCatalog.MaxReasonLength)
            .WithMessage($"Reason must have at most {ExerciseRequestCatalog.MaxReasonLength} characters.");
}

public class RejectExerciseRequestHandler(
    IExerciseRequestRepository requestRepository,
    ExerciseRequestResponseBuilder responseBuilder,
    IExerciseRequestSideEffects sideEffects,
    IValidator<RejectExerciseRequestCommand> validator)
{
    public async Task<Result<ExerciseRequestResponse>> HandleAsync(
        RejectExerciseRequestCommand command,
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

        var reason = command.Reason!.Trim();
        request.Status = ExerciseRequest.StatusRejected;
        request.ReviewedByUserId = actorUserId;
        request.ReviewedAtUtc = DateTime.UtcNow;
        request.RejectionReason = reason;

        try
        {
            await requestRepository.SaveAsync(null, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<ExerciseRequestResponse>.Failure(TrainingErrors.ExerciseRequestAlreadyReviewed(request.Id));
        }

        await sideEffects.AuditAsync("rejected", actorUserId, request.Id, null, reason, cancellationToken);
        await sideEffects.NotifyRejectedAsync(request.RequestedByUserId, request.Name, reason, cancellationToken);

        var response = (await responseBuilder.BuildAsync([request], includeReviewer: true, cancellationToken))[0];
        return Result<ExerciseRequestResponse>.Success(response);
    }
}
