namespace ShapeUp.Features.Safety.Shared;

/// <summary>Asked by flows that connect two people (invites, links) so a block is honored in both directions.</summary>
public interface IUserBlockLookup
{
    Task<bool> IsBlockedEitherWayAsync(int userA, int userB, CancellationToken cancellationToken);
}
