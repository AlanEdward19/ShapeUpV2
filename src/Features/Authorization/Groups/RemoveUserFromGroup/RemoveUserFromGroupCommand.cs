namespace ShapeUp.Features.Authorization.Groups.RemoveUserFromGroup;

public record RemoveUserFromGroupCommand(int UserId, int GroupId);