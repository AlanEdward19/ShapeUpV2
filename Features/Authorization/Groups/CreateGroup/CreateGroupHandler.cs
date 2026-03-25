namespace ShapeUp.Features.Authorization.Groups.CreateGroup;

using Shared.Abstractions;
using Shared.Entities;
using ShapeUp.Shared.Results;

public class CreateGroupHandler(IGroupRepository groupRepository)
{
    public async Task<Result<CreateGroupResponse>> HandleAsync(
        CreateGroupCommand command,
        int createdById,
        CancellationToken cancellationToken)
    {
        var group = new Group
        {
            Name = command.Name,
            Description = command.Description,
            CreatedById = createdById
        };

        await groupRepository.AddAsync(group, cancellationToken);
        await groupRepository.AddUserToGroupAsync(createdById, group.Id, GroupRole.Owner, cancellationToken);

        var response = new CreateGroupResponse(group.Id, group.Name, group.Description, group.CreatedAt);
        return Result<CreateGroupResponse>.Success(response);
    }
}
