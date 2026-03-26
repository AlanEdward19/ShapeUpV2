namespace ShapeUp.Features.Authorization.Scopes.RemoveScopeFromUser;

using FluentValidation;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Scopes.Shared;
using ShapeUp.Features.Authorization.Shared.Errors;
using ShapeUp.Shared.Results;

public class RemoveScopeFromUserHandler(
    IScopeRepository scopeRepository,
    IUserScopeClaimsSyncService userScopeClaimsSyncService,
    IUserRepository userRepository,
    IValidator<RemoveScopeFromUserCommand> validator)
{
    public async Task<Result<RemoveScopeFromUserResponse>> HandleAsync(
        int userId,
        RemoveScopeFromUserCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<RemoveScopeFromUserResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
        }

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            return Result<RemoveScopeFromUserResponse>.Failure(AuthorizationErrors.UserNotFound(userId));

        var scope = await scopeRepository.GetByIdAsync(command.ScopeId, cancellationToken);
        if (scope == null)
            return Result<RemoveScopeFromUserResponse>.Failure(AuthorizationErrors.ScopeNotFound(command.ScopeId));

        await scopeRepository.RemoveScopeFromUserAsync(userId, command.ScopeId, cancellationToken);

        var syncResult = await userScopeClaimsSyncService.SyncAsync(userId, cancellationToken);
        if (syncResult.IsFailure)
            return Result<RemoveScopeFromUserResponse>.Failure(syncResult.Error!);

        var response = new RemoveScopeFromUserResponse(userId, command.ScopeId, "Scope removed from user successfully.");
        return Result<RemoveScopeFromUserResponse>.Success(response);
    }
}
