using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ShapeUp.Features.Authorization.Groups.AddUserToGroup;
using ShapeUp.Features.Authorization.Groups.AssignScopeToGroup;
using ShapeUp.Features.Authorization.Groups.CreateGroup;
using ShapeUp.Features.Authorization.Groups.DeleteGroup;
using ShapeUp.Features.Authorization.Groups.GetGroups;
using ShapeUp.Features.Authorization.Groups.RemoveUserFromGroup;
using ShapeUp.Features.Authorization.Groups.UpdateUserRole;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.Groups;

[ApiController]
[Route("api/groups")]
public class GroupController : ControllerBase
{
    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "groups:management:create" }])]
    public async Task<IActionResult> CreateGroup(
        [FromBody] CreateGroupCommand command,
        [FromServices] CreateGroupHandler handler,
        [FromServices] IValidator<CreateGroupCommand> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var failure = Result<CreateGroupResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
            return this.ToActionResult(failure);
        }

        var currentUserId = HttpContext.GetUserId();
        var result = await handler.HandleAsync(command, currentUserId, cancellationToken);

        return this.ToActionResult(
            result,
            success => CreatedAtAction(nameof(CreateGroup), new { id = success.GroupId }, success));
    }

    [HttpDelete("{groupId}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "groups:management:delete" }])]
    public async Task<IActionResult> DeleteGroup(
        int groupId,
        [FromServices] DeleteGroupHandler handler,
        CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();

        var command = new DeleteGroupCommand(groupId);
        var result = await handler.HandleAsync(command, currentUserId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{groupId}/members")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "groups:management:manage_members" }])]
    public async Task<IActionResult> AddUserToGroup(
        int groupId,
        [FromBody] AddUserToGroupCommand command,
        [FromServices] AddUserToGroupHandler handler,
        [FromServices] IValidator<AddUserToGroupCommand> validator,
        CancellationToken cancellationToken)
    {
        if (groupId != command.GroupId)
        {
            var mismatch = Result<AddUserToGroupResponse>.Failure(
                CommonErrors.Validation("Route groupId must match request groupId."));
            return this.ToActionResult(mismatch);
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var failure = Result<AddUserToGroupResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage))));
            return this.ToActionResult(failure);
        }

        var currentUserId = HttpContext.GetUserId();
        var result = await handler.HandleAsync(command, currentUserId, cancellationToken);

        return this.ToActionResult(
            result,
            success => CreatedAtAction(nameof(AddUserToGroup), new { groupId = success.GroupId }, success));
    }

    [HttpPost("{groupId}/scopes")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "groups:management:manage_scopes" }])]
    public async Task<IActionResult> AssignScopeToGroup(
        int groupId,
        [FromBody] AssignScopeToGroupCommand command,
        [FromServices] AssignScopeToGroupHandler handler,
        CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        var result = await handler.HandleAsync(groupId, command, currentUserId, cancellationToken);

        return this.ToActionResult(
            result,
            success => CreatedAtAction(nameof(AssignScopeToGroup), new { groupId = success.GroupId }, success));
    }

    [HttpGet]
    public async Task<IActionResult> GetUserGroups(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetGroupsHandler handler,
        CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        var result = await handler.HandleAsync(currentUserId, cursor, pageSize, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroupById(
        int groupId,
        [FromServices] GetGroupsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.GetGroupByIdAsync(groupId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{groupId}/members/{userId}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "groups:management:manage_members" }])]
    public async Task<IActionResult> RemoveUserFromGroup(int groupId, int userId,
        [FromServices] RemoveUserFromGroupHandler handler,
        CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();

        var command = new RemoveUserFromGroupCommand(userId, groupId);
        var result = await handler.HandleAsync(command, currentUserId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{groupId}/members/{userId}/role")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "groups:management:manage_members" }])]
    public async Task<IActionResult> UpdateUserRole(
        int groupId,
        int userId,
        [FromBody] UpdateUserRoleCommand command,
        [FromServices] UpdateUserRoleHandler handler,
        CancellationToken cancellationToken)
    {
        if (userId != command.UserId)
        {
            var mismatch = Result<UpdateUserRoleResponse>.Failure(
                CommonErrors.Validation("Route userId must match request userId."));
            return this.ToActionResult(mismatch);
        }

        var currentUserId = HttpContext.GetUserId();
        var result = await handler.HandleAsync(groupId, command, currentUserId, cancellationToken);
        return this.ToActionResult(result);
    }
}