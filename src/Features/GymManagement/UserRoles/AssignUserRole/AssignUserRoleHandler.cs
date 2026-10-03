namespace ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;

using FluentValidation;
using Shared.Abstractions;
using Shared.Entities;
using Shared.Errors;
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

        if (command.Role is PlatformRoleType.Client or PlatformRoleType.GymClient)
            return Result<AssignUserRoleResponse>.Failure(GymManagementErrors.RoleCannotBeAssignedManually(command.Role.ToString()));

        var existing = await roleRepository.GetByUserIdAndRoleAsync(command.UserId, command.Role, cancellationToken);
        // A role granted by a professional credential can be adopted by the admin; any other existing role is a conflict.
        if (existing is { GrantedByCredentialId: null })
            return Result<AssignUserRoleResponse>.Failure(GymManagementErrors.UserAlreadyHasRole(command.UserId, command.Role.ToString()));

        if (command.PlatformTierId.HasValue)
        {
            var tier = await tierRepository.GetByIdAsync(command.PlatformTierId.Value, cancellationToken);
            if (tier is null)
                return Result<AssignUserRoleResponse>.Failure(GymManagementErrors.PlatformTierNotFound(command.PlatformTierId.Value));

            if (tier.TargetRole != command.Role)
                return Result<AssignUserRoleResponse>.Failure(
                    GymManagementErrors.PlatformTierRoleMismatch(command.PlatformTierId.Value, tier.TargetRole.ToString(), command.Role.ToString()));
        }

        if (existing is not null)
        {
            // Adopt: the role no longer depends on the credential, so expiry/suspension/revocation will not withdraw it.
            existing.GrantedByCredentialId = null;
            existing.IsActive = true;
            existing.PlatformTier = null;
            if (command.PlatformTierId.HasValue)
                existing.PlatformTierId = command.PlatformTierId;

            await roleRepository.UpdateAsync(existing, cancellationToken);
            return Result<AssignUserRoleResponse>.Success(
                new AssignUserRoleResponse(existing.Id, existing.UserId, existing.Role.ToString(), existing.PlatformTierId));
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

