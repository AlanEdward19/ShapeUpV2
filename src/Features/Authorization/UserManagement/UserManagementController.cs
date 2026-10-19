using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ShapeUp.Features.Authorization.UserManagement.MobileExperience;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Authorization.UserManagement.DeleteAccount;
using ShapeUp.Features.Authorization.UserManagement.GetUser;
using ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.UserManagement;

[ApiController]
[Route("api/users")]
public class UserManagementController : ControllerBase
{
    // Viewing ANOTHER user's profile by id is a platform-admin action -- self-service lives at GET /me.
    [HttpGet("{id:int}")]
    [Authorize(Policy = "capability:platform.users.read")]
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

    /// <summary>
    /// Deletes the signed-in user's account and all their data for good (App Store 5.1.1(v)). 409 when the user still owns a gym
    /// with other members; 503 when a module failed (call again: it resumes).
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteCurrentAccount(
        [FromServices] DeleteAccountHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken));

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserInfo(
        [FromServices] GetUserHandler handler,
        [FromServices] IValidator<GetUserQuery> validator,
        CancellationToken cancellationToken)
    {
        var userContext = HttpContext.GetUserContext();
        if (userContext is null)
            return this.ToActionResult(
                Result<GetUserResponse>.Failure(CommonErrors.Unauthorized("User context not found.")));

        var query = new GetUserQuery(userContext.UserId);

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
    [HttpPost("me/mobile-experience")]
    [EnableRateLimiting(MobileExperienceRateLimit.Policy)]
    public async Task<IActionResult> RegisterMobileExperience(
        [FromBody] RegisterMobileExperienceCommand command,
        [FromServices] RegisterMobileExperienceHandler handler,
        CancellationToken cancellationToken)
    {
        var user = HttpContext.GetUserContext();
        if (user is null)
            return this.ToActionResult(Result<MobileExperienceResponse>.Failure(CommonErrors.Unauthorized("Sessão necessária.")));
        return this.ToActionResult(await handler.HandleAsync(command, user.UserId, cancellationToken));
    }

}