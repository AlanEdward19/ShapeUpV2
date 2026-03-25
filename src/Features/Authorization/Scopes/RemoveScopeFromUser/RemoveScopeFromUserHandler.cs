namespace ShapeUp.Features.Authorization.Scopes.RemoveScopeFromUser;

using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class RemoveScopeFromUserHandler(IScopeRepository scopeRepository, IFirebaseService firebaseService, IUserRepository userRepository)
{
    public async Task<Result<RemoveScopeFromUserResponse>> HandleAsync(
        int userId,
        RemoveScopeFromUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<RemoveScopeFromUserResponse>.Failure(AuthorizationErrors.UserNotFound(userId));

        var scope = await scopeRepository.GetByIdAsync(command.ScopeId, cancellationToken);
        if (scope == null)
            return Result<RemoveScopeFromUserResponse>.Failure(AuthorizationErrors.ScopeNotFound(command.ScopeId));

        await scopeRepository.RemoveScopeFromUserAsync(userId, command.ScopeId, cancellationToken);

        var syncResult = await SyncUserScopesToFirebaseAsync(user.FirebaseUid, userId, cancellationToken);
        if (syncResult.IsFailure)
            return Result<RemoveScopeFromUserResponse>.Failure(syncResult.Error!);

        var response = new RemoveScopeFromUserResponse(userId, command.ScopeId, "Scope removed from user successfully.");
        return Result<RemoveScopeFromUserResponse>.Success(response);
    }

    private async Task<Result> SyncUserScopesToFirebaseAsync(string firebaseUid, int userId, CancellationToken cancellationToken)
    {
        var scopes = await scopeRepository.GetUserScopesAsync(userId, cancellationToken);
        var scopeNames = scopes.Select(s => s.Name).ToArray();

        var claims = new Dictionary<string, object>
        {
            { "scopes", scopeNames }
        };

        return await firebaseService.SetCustomClaimsAsync(firebaseUid, claims, cancellationToken);
    }
}
