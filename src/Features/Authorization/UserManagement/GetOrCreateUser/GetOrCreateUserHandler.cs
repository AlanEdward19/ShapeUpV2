namespace ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

using Shared.Abstractions;
using Shared.Entities;
using ShapeUp.Shared.Results;

public class GetOrCreateUserHandler(IUserRepository userRepository, IScopeRepository scopeRepository)
{
    public async Task<Result<GetOrCreateUserResponse>> HandleAsync(
        GetOrCreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByFirebaseUidAsync(command.FirebaseUid, cancellationToken);

        if (existingUser != null)
        {
            var scopes = await scopeRepository.GetUserScopesAsync(existingUser.Id, cancellationToken);
            return Result<GetOrCreateUserResponse>.Success(MapToResponse(existingUser, scopes));
        }

        var newUser = new User
        {
            FirebaseUid = command.FirebaseUid,
            Email = command.Email,
            DisplayName = command.DisplayName,
            IsActive = true
        };

        await userRepository.AddAsync(newUser, cancellationToken);

        return Result<GetOrCreateUserResponse>.Success(MapToResponse(newUser, []));
    }

    private static GetOrCreateUserResponse MapToResponse(User user, IReadOnlyList<Scope> scopes) =>
        new(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            Scopes: scopes.Select(s => s.Name).ToArray()
        );
}
