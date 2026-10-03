namespace ShapeUp.Features.Credentials.ReviewCredential;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
using Shared.Lifecycle;
using ShapeUp.Shared.Results;

/// <summary>Admin decision on a credential in the manual review queue.</summary>
public class ReviewCredentialHandler(
    IProfessionalCredentialRepository repository,
    ICredentialLifecycle lifecycle,
    IValidator<RejectCredentialCommand> rejectValidator)
{
    public async Task<Result<CredentialResponse>> ApproveAsync(int credentialId, int reviewerUserId, CancellationToken cancellationToken)
    {
        var credential = await repository.GetByIdAsync(credentialId, cancellationToken);
        if (credential is null)
            return Result<CredentialResponse>.Failure(CredentialErrors.NotFound(credentialId));

        var result = await lifecycle.ApproveAsync(credential, reviewerUserId, cancellationToken);
        return result.IsFailure
            ? Result<CredentialResponse>.Failure(result.Error!)
            : Result<CredentialResponse>.Success(CredentialResponse.From(credential));
    }

    public async Task<Result<CredentialResponse>> RejectAsync(
        int credentialId, RejectCredentialCommand command, int reviewerUserId, CancellationToken cancellationToken)
    {
        var validation = await rejectValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CredentialResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var credential = await repository.GetByIdAsync(credentialId, cancellationToken);
        if (credential is null)
            return Result<CredentialResponse>.Failure(CredentialErrors.NotFound(credentialId));

        var result = await lifecycle.RejectAsync(credential, command.Reason.Trim(), reviewerUserId, cancellationToken);
        return result.IsFailure
            ? Result<CredentialResponse>.Failure(result.Error!)
            : Result<CredentialResponse>.Success(CredentialResponse.From(credential));
    }
}
