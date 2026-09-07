using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moq;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Resolver;

namespace UnitTests.Domains.Authorization.Resolver;

public class CapabilityAuthorizationHandlerTests
{
    private readonly Mock<ICapabilityResolver> _resolver = new();
    private readonly Mock<IAuthorizationAuditWriter> _auditWriter = new();
    private readonly DefaultHttpContext _httpContext = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly CapabilityAuthorizationHandler _handler;

    public CapabilityAuthorizationHandlerTests()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(_httpContext);
        _handler = new CapabilityAuthorizationHandler(_httpContextAccessor.Object, _resolver.Object, _auditWriter.Object);
    }

    private void SetUser(int userId)
    {
        _httpContext.Items["User"] = new UserContext(userId, "firebase-uid", "user@example.com", "User", []);
    }

    private static AuthorizationHandlerContext BuildContext(CapabilityRequirement requirement) =>
        new([requirement], new ClaimsPrincipal(new ClaimsIdentity()), resource: null);

    [Fact]
    public async Task HandleRequirementAsync_ResolverAllows_SucceedsAndAudits()
    {
        SetUser(1);
        var requirement = new CapabilityRequirement("gym.staff.manage");
        _resolver.Setup(r => r.ResolveAsync(1, "gym.staff.manage", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CapabilityResult.Allow());
        var context = BuildContext(requirement);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        _auditWriter.Verify(a => a.RecordAsync(1, "gym.staff.manage", true, null, It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRequirementAsync_ResolverDenies_FailsAndAudits()
    {
        SetUser(1);
        var requirement = new CapabilityRequirement("gym.staff.manage");
        _resolver.Setup(r => r.ResolveAsync(1, "gym.staff.manage", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CapabilityResult.Deny("no membership"));
        var context = BuildContext(requirement);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        _auditWriter.Verify(a => a.RecordAsync(1, "gym.staff.manage", false, "no membership", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRequirementAsync_ResolverThrows_NeverSucceedsAuditsAndRethrows()
    {
        SetUser(1);
        var requirement = new CapabilityRequirement("gym.staff.manage");
        _resolver.Setup(r => r.ResolveAsync(1, "gym.staff.manage", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db unavailable"));
        var context = BuildContext(requirement);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(context));

        Assert.False(context.HasSucceeded);
        _auditWriter.Verify(a => a.RecordAsync(1, "gym.staff.manage", false, "internal_error", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleRequirementAsync_ExtractsGymIdFromRouteValues()
    {
        SetUser(1);
        _httpContext.Request.RouteValues = new RouteValueDictionary { ["gymId"] = "42" };
        var requirement = new CapabilityRequirement("gym.staff.manage");
        AuthorizationContext? capturedContext = null;
        _resolver.Setup(r => r.ResolveAsync(1, "gym.staff.manage", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .Callback<int, string, AuthorizationContext, CancellationToken>((_, _, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(CapabilityResult.Allow());

        await _handler.HandleAsync(BuildContext(requirement));

        Assert.NotNull(capturedContext);
        Assert.Equal(42, capturedContext.GymId);
    }

    [Fact]
    public async Task HandleRequirementAsync_PlatformPrefixedCapability_SetsRequiresPlatformAdminIgnoringRoute()
    {
        SetUser(1);
        _httpContext.Request.RouteValues = new RouteValueDictionary { ["gymId"] = "42" };
        var requirement = new CapabilityRequirement("platform.exercises.manage");
        AuthorizationContext? capturedContext = null;
        _resolver.Setup(r => r.ResolveAsync(1, "platform.exercises.manage", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .Callback<int, string, AuthorizationContext, CancellationToken>((_, _, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(CapabilityResult.Allow());

        await _handler.HandleAsync(BuildContext(requirement));

        Assert.NotNull(capturedContext);
        Assert.True(capturedContext.RequiresPlatformAdmin);
        Assert.Null(capturedContext.GymId);
    }

    [Fact]
    public async Task HandleRequirementAsync_ExtractsTrainerIdFromRouteValuesAsTargetUserId()
    {
        SetUser(1);
        _httpContext.Request.RouteValues = new RouteValueDictionary { ["trainerId"] = "7" };
        var requirement = new CapabilityRequirement("gym.trainer_plans.read");
        AuthorizationContext? capturedContext = null;
        _resolver.Setup(r => r.ResolveAsync(1, "gym.trainer_plans.read", It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()))
            .Callback<int, string, AuthorizationContext, CancellationToken>((_, _, ctx, _) => capturedContext = ctx)
            .ReturnsAsync(CapabilityResult.Allow());

        await _handler.HandleAsync(BuildContext(requirement));

        Assert.NotNull(capturedContext);
        Assert.Equal(7, capturedContext.TargetUserId);
    }

    [Fact]
    public async Task HandleRequirementAsync_NoUserContext_DoesNotSucceedAndDoesNotCallResolver()
    {
        var requirement = new CapabilityRequirement("gym.staff.manage");
        var context = BuildContext(requirement);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        _resolver.Verify(r => r.ResolveAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<AuthorizationContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
