namespace ShapeUp.Features.Authorization.Scopes.SyncCurrentUserScopes;

using FluentValidation;
using ShapeUp.Features.Authorization.Scopes.Shared;
using ShapeUp.Shared.Results;

public class SyncCurrentUserScopesHandler(
    IUserScopeClaimsSyncService userScopeClaimsSyncService,
    IValidator<SyncCurrentUserScopesCommand> validator)
{
    public async Task<Result<SyncCurrentUserScopesResponse>> HandleAsync(
        SyncCurrentUserScopesCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<SyncCurrentUserScopesResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
        }

        var syncResult = await userScopeClaimsSyncService.SyncAsync(command.UserId, cancellationToken);
        if (syncResult.IsFailure)
            return Result<SyncCurrentUserScopesResponse>.Failure(syncResult.Error!);

        var response = new SyncCurrentUserScopesResponse(
            syncResult.Value!.UserId,
            syncResult.Value.ScopeCount,
            "Current user scopes synchronized successfully.");

        return Result<SyncCurrentUserScopesResponse>.Success(response);
    }
}

