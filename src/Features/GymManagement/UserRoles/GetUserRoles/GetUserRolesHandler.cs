namespace ShapeUp.Features.GymManagement.UserRoles.GetUserRoles;

using Shared.Abstractions;
using ShapeUp.Shared.Results;

public class GetUserRolesHandler(IUserPlatformRoleRepository repository)
{
    public async Task<Result<IReadOnlyList<GetUserRoleResponse>>> HandleAsync(GetUserRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await repository.GetByUserIdAsync(query.UserId, cancellationToken);
        var response = roles.Select(r => new GetUserRoleResponse(
            r.Id, r.UserId, r.Role.ToString(), r.PlatformTierId, r.PlatformTier?.Name)).ToList();
        return Result<IReadOnlyList<GetUserRoleResponse>>.Success(response);
    }
}

