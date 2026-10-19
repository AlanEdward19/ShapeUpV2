namespace ShapeUp.Features.Credentials.SubmitCredential;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using Shared.Lifecycle;
using Shared.Notifications;
using Shared.Verification;
using ShapeUp.Shared.Results;

public class SubmitCredentialHandler(
    IProfessionalCredentialRepository repository,
    ICredentialLifecycle lifecycle,
    IEnumerable<ICredentialVerifier> verifiers,
    IValidator<SubmitCredentialCommand> validator,
    ICredentialNotifier? notifier = null)
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

        var flow = await CredentialVerificationFlow.RunAsync(credential, verifiers, lifecycle, notifier, cancellationToken);
        if (flow.IsFailure)
            return Result<CredentialResponse>.Failure(flow.Error!);

        return Result<CredentialResponse>.Success(CredentialResponse.From(credential));
    }
}
