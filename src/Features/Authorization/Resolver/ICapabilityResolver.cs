namespace ShapeUp.Features.Authorization.Resolver;

public interface ICapabilityResolver
{
    Task<CapabilityResult> ResolveAsync(
        int userId,
        string capability,
        AuthorizationContext context,
        CancellationToken cancellationToken);
}
