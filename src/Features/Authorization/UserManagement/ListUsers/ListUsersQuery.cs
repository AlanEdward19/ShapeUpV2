namespace ShapeUp.Features.Authorization.UserManagement.ListUsers;

public record ListUsersQuery(string? Cursor, int? PageSize, string? Search);

public record ListedUserResponse(int UserId, string Email, string? DisplayName, bool IsActive, DateTime CreatedAt);
