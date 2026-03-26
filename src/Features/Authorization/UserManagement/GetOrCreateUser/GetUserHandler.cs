using ShapeUp.Features.Authorization.Shared.Errors;

namespace ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

using Shared.Abstractions;
using Shared.Entities;
using ShapeUp.Shared.Results;

public class GetUserHandler(IUserRepository userRepository, IScopeRepository scopeRepository)
{
    public async Task<Result<GetUserResponse>> HandleAsync(
        GetUserQuery query,
        CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByIdAsync(query.Id, cancellationToken);

        if (existingUser != null)
        {
            var scopes = await scopeRepository.GetUserScopesAsync(existingUser.Id, cancellationToken);
            return Result<GetUserResponse>.Success(MapToResponse(existingUser, scopes));
        }

        return Result<GetUserResponse>.Failure(AuthorizationErrors.UserNotFound(query.Id));
    }

    private static GetUserResponse MapToResponse(User user, IReadOnlyList<Scope> scopes) =>
        new(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            Scopes: scopes.Select(s => s.Name).ToArray()
        );
}
