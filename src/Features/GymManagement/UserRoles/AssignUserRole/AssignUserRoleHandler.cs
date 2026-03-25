namespace ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;

using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Results;

public class AssignUserRoleHandler(
    IUserPlatformRoleRepository roleRepository,
    IPlatformTierRepository tierRepository,
    IValidator<AssignUserRoleCommand> validator)
{
    public async Task<Result<AssignUserRoleResponse>> HandleAsync(AssignUserRoleCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<AssignUserRoleResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var existing = await roleRepository.GetByUserIdAndRoleAsync(command.UserId, command.Role, cancellationToken);
        if (existing != null)
            return Result<AssignUserRoleResponse>.Failure(GymManagementErrors.UserAlreadyHasRole(command.UserId, command.Role.ToString()));

        if (command.PlatformTierId.HasValue)
        {
            var tier = await tierRepository.GetByIdAsync(command.PlatformTierId.Value, cancellationToken);
            if (tier is null)
                return Result<AssignUserRoleResponse>.Failure(GymManagementErrors.PlatformTierNotFound(command.PlatformTierId.Value));
        }

        var userRole = new UserPlatformRole
        {
            UserId = command.UserId,
            Role = command.Role,
            PlatformTierId = command.PlatformTierId
        };

        await roleRepository.AddAsync(userRole, cancellationToken);
        return Result<AssignUserRoleResponse>.Success(
            new AssignUserRoleResponse(userRole.Id, userRole.UserId, userRole.Role.ToString(), userRole.PlatformTierId));
    }
}

