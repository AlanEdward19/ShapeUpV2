namespace ShapeUp.Features.Authorization.Groups.AddUserToGroup;

public record AddUserToGroupCommand(int UserId, int GroupId, string Role);

