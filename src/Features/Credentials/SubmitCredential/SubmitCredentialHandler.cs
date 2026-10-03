namespace ShapeUp.Features.Credentials.SubmitCredential;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using Shared.Lifecycle;
using Shared.Verification;
using ShapeUp.Shared.Results;

public class SubmitCredentialHandler(
    IProfessionalCredentialRepository repository,
    ICredentialLifecycle lifecycle,
    IEnumerable<ICredentialVerifier> verifiers,
    IValidator<SubmitCredentialCommand> validator)
{
    public async Task<Result<CredentialResponse>> HandleAsync(
        SubmitCredentialCommand command,
        int userId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CredentialResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        if (await repository.HasOpenOrVerifiedAsync(userId, command.ProfessionType, cancellationToken))
            return Result<CredentialResponse>.Failure(CredentialErrors.AlreadyInProgressOrVerified(command.ProfessionType));

        // The same registration cannot back two accounts at once (the unique index below also guards the race).
        var number = command.CredentialNumber.Trim().ToUpperInvariant();
        if (await repository.IsRegistrationInUseAsync(command.IssuingAuthority, command.IssuingRegion, number, cancellationToken))
            return Result<CredentialResponse>.Failure(CredentialErrors.RegistrationInUse());

        var credential = new ProfessionalCredential
        {
            UserId = userId,
            ProfessionType = command.ProfessionType,
            CredentialNumber = number,
            IssuingAuthority = command.IssuingAuthority,
            IssuingRegion = command.IssuingRegion,
            Country = command.Country,
            Status = CredentialStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };
        try
        {
            await repository.AddAsync(credential, cancellationToken);
        }
        catch (CredentialConflictException ex)
        {
            return Result<CredentialResponse>.Failure(ex.ToError(command.ProfessionType));
        }

        var verifier = verifiers.FirstOrDefault(v => v.Supports(credential.IssuingAuthority));
        var verification = verifier is null
            ? CredentialVerificationResult.ForManualReview()
            : await verifier.VerifyAsync(credential, cancellationToken);

        // Submitted -> UnderReview always comes first; the state machine has no shortcut to Verified/Rejected.
        var review = await lifecycle.SendToReviewAsync(credential, cancellationToken);
        if (review.IsFailure)
            return Result<CredentialResponse>.Failure(review.Error!);

        var decision = verification.Outcome switch
        {
            CredentialVerificationOutcome.Verified => await lifecycle.ApproveAsync(credential, null, cancellationToken),
            CredentialVerificationOutcome.Rejected => await lifecycle.RejectAsync(
                credential, verification.Reason ?? "Registration not confirmed by the council.", null, cancellationToken),
            _ => Result.Success()
        };
        if (decision.IsFailure)
            return Result<CredentialResponse>.Failure(decision.Error!);

        return Result<CredentialResponse>.Success(CredentialResponse.From(credential));
    }
}
