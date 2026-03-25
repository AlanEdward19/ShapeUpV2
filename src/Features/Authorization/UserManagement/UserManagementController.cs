using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.UserManagement;

[ApiController]
[Route("api/users")]
public class UserManagementController(
    GetOrCreateUserHandler handler,
    IValidator<GetOrCreateUserCommand> validator) : ControllerBase
{
    [HttpPost("get-or-create")]
    public async Task<IActionResult> GetOrCreateUser(
        [FromBody] GetOrCreateUserCommand command,
        CancellationToken cancellationToken)
    {
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
}
