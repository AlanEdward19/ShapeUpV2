using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Scopes.AssignScopeToUser;
using ShapeUp.Features.Authorization.Scopes.CreateScope;
using ShapeUp.Features.Authorization.Scopes.GetScopes;
using ShapeUp.Features.Authorization.Scopes.GetUserScopes;
using ShapeUp.Features.Authorization.Scopes.RemoveScopeFromUser;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.Scopes;

[ApiController]
[Route("api/scopes")]
public class ScopesController : ControllerBase
{
    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "scopes:management:create" }])]
    public async Task<IActionResult> CreateScope(
        [FromBody] CreateScopeCommand command,
        [FromServices] CreateScopeHandler handler,
        [FromServices] IValidator<CreateScopeCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var failure = Result<CreateScopeResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
            return this.ToActionResult(failure);
        }

        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(
            result,
            success => CreatedAtAction(nameof(CreateScope), new { id = success.ScopeId }, success));
    }

    [HttpPost("assign-to-user/{userId}")]
    public async Task<IActionResult> AssignScopeToUser(
        int userId,
        [FromBody] AssignScopeToUserCommand command,
        [FromServices] AssignScopeToUserHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(userId, command, cancellationToken);
        return this.ToActionResult(
            result,
            success => CreatedAtAction(nameof(AssignScopeToUser), new { userId = success.UserId }, success));
    }

    [HttpGet]
    public async Task<IActionResult> GetScopes(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetScopesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cursor, pageSize, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserScopes(
        int userId,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetUserScopesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(userId, cursor, pageSize, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("remove-from-user/{userId}")]
    public async Task<IActionResult> RemoveScopeFromUser(
        int userId,
        [FromBody] RemoveScopeFromUserCommand command,
        [FromServices] RemoveScopeFromUserHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(userId, command, cancellationToken);
        return this.ToActionResult(result);
    }
}