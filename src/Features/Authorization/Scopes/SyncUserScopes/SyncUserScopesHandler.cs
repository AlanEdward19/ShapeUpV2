namespace ShapeUp.Features.Authorization.Scopes.SyncUserScopes;

using FluentValidation;
using Shared;
using ShapeUp.Shared.Results;

public class SyncUserScopesHandler(
    IUserScopeClaimsSyncService userScopeClaimsSyncService,
    IValidator<SyncUserScopesCommand> validator)
{
    public async Task<Result<SyncUserScopesResponse>> HandleAsync(
        SyncUserScopesCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<SyncUserScopesResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
        }

        var syncResult = await userScopeClaimsSyncService.SyncAsync(command.UserId, cancellationToken);
        if (syncResult.IsFailure)
            return Result<SyncUserScopesResponse>.Failure(syncResult.Error!);

        var response = new SyncUserScopesResponse(
            syncResult.Value!.UserId,
            syncResult.Value.ScopeCount,
            "User scopes synchronized successfully.");

        return Result<SyncUserScopesResponse>.Success(response);
    }
}

