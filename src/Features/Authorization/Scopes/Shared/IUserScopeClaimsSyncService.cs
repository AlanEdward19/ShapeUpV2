namespace ShapeUp.Features.Authorization.Scopes.Shared;

using ShapeUp.Shared.Results;

public interface IUserScopeClaimsSyncService
{
    Task<Result<UserScopeClaimsSyncResult>> SyncAsync(int userId, CancellationToken cancellationToken);
}

public record UserScopeClaimsSyncResult(int UserId, int ScopeCount);

