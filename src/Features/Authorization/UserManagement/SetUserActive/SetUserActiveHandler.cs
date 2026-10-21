using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.UserManagement.SetUserActive;

public class SetUserActiveHandler(IUserRepository userRepository)
{
    public async Task<Result<SetUserActiveResponse>> HandleAsync(
        SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        if (!command.IsActive && command.UserId == command.ActorUserId)
            return Result<SetUserActiveResponse>.Failure(
                CommonErrors.Conflict("You cannot deactivate your own account from the admin console."));

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return Result<SetUserActiveResponse>.Failure(AuthorizationErrors.UserNotFound(command.UserId));

        if (user.IsActive != command.IsActive)
        {
            var updated = await userRepository.SetActiveAsync(command.UserId, command.IsActive, cancellationToken);
            if (!updated)
                return Result<SetUserActiveResponse>.Failure(AuthorizationErrors.UserNotFound(command.UserId));
        }

        return Result<SetUserActiveResponse>.Success(
            new SetUserActiveResponse(user.Id, user.Email, user.DisplayName, command.IsActive));
    }
}
