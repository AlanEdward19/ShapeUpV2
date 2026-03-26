namespace ShapeUp.Features.Authorization.Scopes.Shared;

using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Errors;
using ShapeUp.Shared.Results;

public class UserScopeClaimsSyncService(
    IUserRepository userRepository,
    IScopeRepository scopeRepository,
    IFirebaseService firebaseService) : IUserScopeClaimsSyncService
{
    public async Task<Result<UserScopeClaimsSyncResult>> SyncAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<UserScopeClaimsSyncResult>.Failure(AuthorizationErrors.UserNotFound(userId));

        var scopes = await scopeRepository.GetUserScopesAsync(userId, cancellationToken);
        var scopeNames = scopes.Select(s => s.Name).ToArray();

        var claims = new Dictionary<string, object>
        {
            { "scopes", scopeNames }
        };

        var firebaseResult = await firebaseService.SetCustomClaimsAsync(user.FirebaseUid, claims, cancellationToken);
        if (firebaseResult.IsFailure)
            return Result<UserScopeClaimsSyncResult>.Failure(firebaseResult.Error!);

        return Result<UserScopeClaimsSyncResult>.Success(new UserScopeClaimsSyncResult(userId, scopeNames.Length));
    }
}

