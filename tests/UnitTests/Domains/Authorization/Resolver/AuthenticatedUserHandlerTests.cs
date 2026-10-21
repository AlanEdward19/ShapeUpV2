using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Resolver;

namespace UnitTests.Domains.Authorization.Resolver;

public class AuthenticatedUserHandlerTests
{
    private readonly DefaultHttpContext _httpContext = new();
    private readonly AuthenticatedUserHandler _handler;

    public AuthenticatedUserHandlerTests()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(_httpContext);
        _handler = new AuthenticatedUserHandler(accessor.Object);
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserContextPresent_Succeeds()
    {
        _httpContext.Items["User"] = new UserContext(4, "uid", "user@example.com", "User");
        var requirement = new AuthenticatedUserRequirement();
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity()), null);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WhenUserContextMissing_DoesNotSucceed()
    {
        var requirement = new AuthenticatedUserRequirement();
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity()), null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
