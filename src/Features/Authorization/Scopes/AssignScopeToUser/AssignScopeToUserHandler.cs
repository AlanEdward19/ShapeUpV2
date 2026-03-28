namespace ShapeUp.Features.Authorization.Scopes.AssignScopeToUser;

using FluentValidation;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using Shared;
using ShapeUp.Features.Authorization.Shared.Errors;
using ShapeUp.Shared.Results;

public class AssignScopeToUserHandler(
    IScopeRepository scopeRepository,
    IUserScopeClaimsSyncService userScopeClaimsSyncService,
    IUserRepository userRepository,
    IValidator<AssignScopeToUserCommand> validator)
{
    public async Task<Result<AssignScopeToUserResponse>> HandleAsync(
        int userId,
        AssignScopeToUserCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<AssignScopeToUserResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<AssignScopeToUserResponse>.Failure(AuthorizationErrors.UserNotFound(userId));

        var scope = await scopeRepository.GetByIdAsync(command.ScopeId, cancellationToken);
        if (scope == null)
            return Result<AssignScopeToUserResponse>.Failure(AuthorizationErrors.ScopeNotFound(command.ScopeId));

        await scopeRepository.AssignScopeToUserAsync(userId, command.ScopeId, cancellationToken);

        var syncResult = await userScopeClaimsSyncService.SyncAsync(userId, cancellationToken);
        if (syncResult.IsFailure)
            return Result<AssignScopeToUserResponse>.Failure(syncResult.Error!);

        var response = new AssignScopeToUserResponse(userId, command.ScopeId, scope.Name);
        return Result<AssignScopeToUserResponse>.Success(response);
    }
}
