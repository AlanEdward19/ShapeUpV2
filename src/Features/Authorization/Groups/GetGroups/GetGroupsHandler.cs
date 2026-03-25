using ShapeUp.Features.Authorization.Groups.GetGroups.Dtos;

namespace ShapeUp.Features.Authorization.Groups.GetGroups;

using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetGroupsHandler(IGroupRepository groupRepository)
{
    public async Task<Result<KeysetPageResponse<GetGroupsResponse>>> HandleAsync(
        int userId,
        string? cursor,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        int? lastGroupId = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(cursor, out var decoded) || decoded is <= 0 or > int.MaxValue)
                return Result<KeysetPageResponse<GetGroupsResponse>>.Failure(CommonErrors.Validation("Invalid keyset cursor."));

            lastGroupId = (int)decoded;
        }

        var request = new KeysetPageRequest(cursor, pageSize);
        var normalizedPageSize = request.NormalizePageSize();

        var groups = await groupRepository.GetByUserIdKeysetAsync(userId, lastGroupId, normalizedPageSize, cancellationToken);

        var result = new List<GetGroupsResponse>();

        foreach (var group in groups)
        {
            var members = await groupRepository.GetGroupMembersAsync(group.Id, cancellationToken);
            var memberDtos = members.Select(m => new GroupMemberDto(
                m.UserId,
                m.Email,
                m.DisplayName,
                m.Role.ToString()
            )).ToArray();

            result.Add(new GetGroupsResponse(
                group.Id,
                group.Name,
                group.Description,
                group.CreatedAt,
                memberDtos
            ));
        }

        var items = result.ToArray();
        var nextCursor = items.Length < normalizedPageSize
            ? null
            : KeysetCursorCodec.EncodeLong(items[^1].GroupId);

        return Result<KeysetPageResponse<GetGroupsResponse>>.Success(new KeysetPageResponse<GetGroupsResponse>(items, nextCursor));
    }

    public async Task<Result<GetGroupsResponse>> GetGroupByIdAsync(int groupId, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group == null)
            return Result<GetGroupsResponse>.Failure(AuthorizationErrors.GroupNotFound(groupId));

        var members = await groupRepository.GetGroupMembersAsync(groupId, cancellationToken);
        var memberDtos = members.Select(m => new GroupMemberDto(
            m.UserId,
            m.Email,
            m.DisplayName,
            m.Role.ToString()
        )).ToArray();

        var response = new GetGroupsResponse(
            group.Id,
            group.Name,
            group.Description,
            group.CreatedAt,
            memberDtos
        );

        return Result<GetGroupsResponse>.Success(response);
    }
}
