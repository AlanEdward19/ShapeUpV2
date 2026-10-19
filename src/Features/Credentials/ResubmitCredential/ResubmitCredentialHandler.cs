namespace ShapeUp.Features.Credentials.ResubmitCredential;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using Shared.Lifecycle;
using Shared.Notifications;
using Shared.Verification;
using ShapeUp.Shared.Results;

/// <summary>The owner corrects a credential the reviewer sent back (NeedsChanges) and sends it through verification again.</summary>
public class ResubmitCredentialHandler(
    IProfessionalCredentialRepository repository,
    ICredentialLifecycle lifecycle,
    IEnumerable<ICredentialVerifier> verifiers,
    IValidator<ResubmitCredentialCommand> validator,
    ICredentialNotifier? notifier = null)
{
    public async Task<Result<CredentialResponse>> HandleAsync(
        int credentialId, ResubmitCredentialCommand command, int userId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CredentialResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var credential = await repository.GetByIdAsync(credentialId, cancellationToken);
        // Someone else's credential looks the same as a missing one.
        if (credential is null || credential.UserId != userId)
            return Result<CredentialResponse>.Failure(CredentialErrors.NotFound(credentialId));

        var number = command.CredentialNumber.Trim().ToUpperInvariant();
        var changedRegistration = number != credential.CredentialNumber || command.IssuingRegion != credential.IssuingRegion;
        if (changedRegistration
            && credential.Status == CredentialStatus.NeedsChanges
            && await repository.IsRegistrationInUseAsync(credential.IssuingAuthority, command.IssuingRegion, number, cancellationToken))
            return Result<CredentialResponse>.Failure(CredentialErrors.RegistrationInUse());

        var resubmitted = await lifecycle.ResubmitAsync(credential, number, command.IssuingRegion, cancellationToken);
        if (resubmitted.IsFailure)
            return Result<CredentialResponse>.Failure(resubmitted.Error!);

        var flow = await CredentialVerificationFlow.RunAsync(credential, verifiers, lifecycle, notifier, cancellationToken);
        return flow.IsFailure
            ? Result<CredentialResponse>.Failure(flow.Error!)
            : Result<CredentialResponse>.Success(CredentialResponse.From(credential));
    }
}
