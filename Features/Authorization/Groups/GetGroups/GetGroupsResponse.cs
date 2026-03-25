using ShapeUp.Features.Authorization.Groups.GetGroups.Dtos;

namespace ShapeUp.Features.Authorization.Groups.GetGroups;

public record GetGroupsResponse(int GroupId, string Name, string? Description, DateTime CreatedAt, GroupMemberDto[] Members);