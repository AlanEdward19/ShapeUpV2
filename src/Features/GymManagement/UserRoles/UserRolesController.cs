namespace ShapeUp.Features.GymManagement.UserRoles;

using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;
using ShapeUp.Features.GymManagement.UserRoles.GetUserRoles;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/user-roles")]
public class UserRolesController : ControllerBase
{
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetByUser(
        int userId,
        [FromServices] GetUserRolesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUserRolesQuery(userId), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Assign(
        [FromBody] AssignUserRoleCommand command,
        [FromServices] AssignUserRoleHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetByUser), new { userId = success.UserId }, success));
    }
}
