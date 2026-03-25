namespace ShapeUp.Features.Authorization.Groups.CreateGroup;

public record CreateGroupResponse(int GroupId, string Name, string? Description, DateTime CreatedAt);