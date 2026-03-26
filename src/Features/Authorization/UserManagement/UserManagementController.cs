using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;
using ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.UserManagement;

[ApiController]
[Route("api/users")]
public class UserManagementController(
    GetOrCreateUserHandler handler,
    RevokeCurrentTokenHandler revokeCurrentTokenHandler,
    IValidator<GetOrCreateUserCommand> validator) : ControllerBase
{
    [HttpPost("get-or-create")]
    public async Task<IActionResult> GetOrCreateUser(
        CancellationToken cancellationToken)
    {
        var userContext = HttpContext.GetUserContext();
        if (userContext is null)
            return this.ToActionResult(Result<GetOrCreateUserResponse>.Failure(CommonErrors.Unauthorized("User context not found.")));

        var command = new GetOrCreateUserCommand(
            userContext.FirebaseUid,
            userContext.Email,
            userContext.DisplayName);

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var failure = Result<GetOrCreateUserResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
            return this.ToActionResult(failure);
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userContext = HttpContext.GetUserContext();
        if (userContext is null)
            return this.ToActionResult(Result<RevokeCurrentTokenResponse>.Failure(CommonErrors.Unauthorized("User context not found.")));

        var command = new RevokeCurrentTokenCommand(userContext.UserId, userContext.FirebaseUid);
        var result = await revokeCurrentTokenHandler.HandleAsync(command, cancellationToken);

        return this.ToActionResult(result);
    }
}
