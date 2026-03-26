using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;
using ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.UserManagement;

[ApiController]
[Route("api/users")]
public class UserManagementController : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserInfo([FromServices] GetUserHandler handler,
        [FromServices] IValidator<GetUserQuery> validator, int id, CancellationToken cancellationToken)
    {
        var userContext = HttpContext.GetUserContext();
        if (userContext is null)
            return this.ToActionResult(
                Result<GetUserResponse>.Failure(CommonErrors.Unauthorized("User context not found.")));

        var query = new GetUserQuery(id);

        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var failure = Result<GetUserResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
            return this.ToActionResult(failure);
        }

        var result = await handler.HandleAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromServices] RevokeCurrentTokenHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = HttpContext.GetUserContext();
        if (userContext is null)
            return this.ToActionResult(
                Result<RevokeCurrentTokenResponse>.Failure(CommonErrors.Unauthorized("User context not found.")));

        var command = new RevokeCurrentTokenCommand(userContext.UserId, userContext.FirebaseUid);
        var result = await handler.HandleAsync(command, cancellationToken);

        return this.ToActionResult(result);
    }
}