using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Authorization.UserManagement.GetUser;

namespace ShapeUp.Features.GymManagement.UserRoles;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using AssignUserRole;
using GetUserRoles;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/user-roles")]
public class UserRolesController : ControllerBase
{
    [HttpGet("{userId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:user_roles:read" }])]
    public async Task<IActionResult> GetUserRolesById(
        int userId,
        [FromServices] GetUserRolesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUserRolesQuery(userId), cancellationToken);
        return this.ToActionResult(result);
    }
    
    [HttpGet("me")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:user_roles:read" }])]
    public async Task<IActionResult> GetOwnUserRoles(
        [FromServices] GetUserRolesHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = HttpContext.GetUserContext();
        if (userContext is null)
            return this.ToActionResult(
                Result<GetUserResponse>.Failure(CommonErrors.Unauthorized("User context not found.")));
        
        var result = await handler.HandleAsync(new GetUserRolesQuery(userContext.UserId), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:user_roles:assign" }])]
    public async Task<IActionResult> Assign(
        [FromBody] AssignUserRoleCommand command,
        [FromServices] AssignUserRoleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetUserRolesById), new { userId = success.UserId }, success));
    }
}
