namespace ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;

using FluentValidation;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Errors;
using ShapeUp.Shared.Results;

public class RevokeCurrentTokenHandler(
    IUserRepository userRepository,
    IFirebaseService firebaseService,
    IValidator<RevokeCurrentTokenCommand> validator)
{
    public async Task<Result<RevokeCurrentTokenResponse>> HandleAsync(
        RevokeCurrentTokenCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<RevokeCurrentTokenResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));
        }

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return Result<RevokeCurrentTokenResponse>.Failure(AuthorizationErrors.UserNotFound(command.UserId));

        if (!string.Equals(user.FirebaseUid, command.FirebaseUid, StringComparison.Ordinal))
            return Result<RevokeCurrentTokenResponse>.Failure(CommonErrors.Forbidden("Authenticated user mismatch."));

        var revokeResult = await firebaseService.RevokeRefreshTokensAsync(command.FirebaseUid, cancellationToken);
        if (revokeResult.IsFailure)
            return Result<RevokeCurrentTokenResponse>.Failure(revokeResult.Error!);

        return Result<RevokeCurrentTokenResponse>.Success(
            new RevokeCurrentTokenResponse(command.UserId, DateTime.UtcNow));
    }
}
