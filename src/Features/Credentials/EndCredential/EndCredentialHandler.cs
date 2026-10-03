namespace ShapeUp.Features.Credentials.EndCredential;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using Shared.Lifecycle;
using ShapeUp.Shared.Results;

/// <summary>Admin suspends (temporary) or revokes (definitive) a Verified credential; the role it granted is withdrawn.</summary>
public class EndCredentialHandler(
    IProfessionalCredentialRepository repository,
    ICredentialLifecycle lifecycle,
    CredentialRequesterEnricher enricher,
    IValidator<EndCredentialCommand> validator)
{
    public Task<Result<CredentialResponse>> SuspendAsync(
        int credentialId, EndCredentialCommand command, int adminUserId, CancellationToken cancellationToken) =>
        EndAsync(credentialId, CredentialStatus.Suspended, command, adminUserId, cancellationToken);

    public Task<Result<CredentialResponse>> RevokeAsync(
        int credentialId, EndCredentialCommand command, int adminUserId, CancellationToken cancellationToken) =>
        EndAsync(credentialId, CredentialStatus.Revoked, command, adminUserId, cancellationToken);

    private async Task<Result<CredentialResponse>> EndAsync(
        int credentialId, CredentialStatus target, EndCredentialCommand command, int adminUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CredentialResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var credential = await repository.GetByIdAsync(credentialId, cancellationToken);
        if (credential is null)
            return Result<CredentialResponse>.Failure(CredentialErrors.NotFound(credentialId));

        var result = await lifecycle.EndAsync(
            credential, target, DateTime.UtcNow, cancellationToken, command.Reason.Trim(), adminUserId);
        return result.IsFailure
            ? Result<CredentialResponse>.Failure(result.Error!)
            : Result<CredentialResponse>.Success(await enricher.ToResponseAsync(credential, cancellationToken));
    }
}
