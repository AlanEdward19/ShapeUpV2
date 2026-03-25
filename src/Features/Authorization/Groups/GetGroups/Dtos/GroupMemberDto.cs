namespace ShapeUp.Features.Authorization.Groups.GetGroups.Dtos;

public record GroupMemberDto(int UserId, string Email, string? DisplayName, string Role);