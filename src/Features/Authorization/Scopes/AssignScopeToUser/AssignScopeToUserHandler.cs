namespace ShapeUp.Features.Authorization.Scopes.AssignScopeToUser;

using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Results;

public class AssignScopeToUserHandler(IScopeRepository scopeRepository, IFirebaseService firebaseService, IUserRepository userRepository)
{
    public async Task<Result<AssignScopeToUserResponse>> HandleAsync(
        int userId,
        AssignScopeToUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<AssignScopeToUserResponse>.Failure(AuthorizationErrors.UserNotFound(userId));

        var scope = await scopeRepository.GetByIdAsync(command.ScopeId, cancellationToken);
        if (scope == null)
            return Result<AssignScopeToUserResponse>.Failure(AuthorizationErrors.ScopeNotFound(command.ScopeId));

        await scopeRepository.AssignScopeToUserAsync(userId, command.ScopeId, cancellationToken);

        var syncResult = await SyncUserScopesToFirebaseAsync(user.FirebaseUid, userId, cancellationToken);
        if (syncResult.IsFailure)
            return Result<AssignScopeToUserResponse>.Failure(syncResult.Error!);

        var response = new AssignScopeToUserResponse(userId, command.ScopeId, scope.Name);
        return Result<AssignScopeToUserResponse>.Success(response);
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
