using ShapeUp.Features.Authorization.Scopes.CreateScope;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace UnitTests.Features.Authorization.Scopes;

public class CreateScopeHandlerTests
{
    private readonly Mock<IScopeRepository> _mockRepository;
    private readonly CreateScopeHandler _handler;

    public CreateScopeHandlerTests()
    {
        _mockRepository = new Mock<IScopeRepository>();
        _handler = new CreateScopeHandler(_mockRepository.Object);
    }

    public static IEnumerable<object[]> ValidScopeCases =>
        new List<object[]>
        {
            new[] { (object)"users", "profile", "read", (object)"Read user profile" },
            new[] { (object)"groups", "management", "create", (object)"Create group" },
            new[] { (object)"orders", "items", "update", (object)"Update order items" },
            new[] { (object)"admin", "audit_logs", "export", (object?)null! },
        };

    [Theory]
    [MemberData(nameof(ValidScopeCases))]
    public async Task HandleAsync_ValidCommand_CreatesScopeSuccessfully(
        string domain, string subdomain, string action, string? description)
    {
        // Arrange
        var command = new CreateScopeCommand(domain, subdomain, action, description);
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetByScopeFormatAsync(domain, subdomain, action, cancellationToken))
            .ReturnsAsync((Scope?)null);

        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Scope>(), cancellationToken))
            .Callback<Scope, CancellationToken>((scope, _) => scope.Id = 1)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal($"{domain}:{subdomain}:{action}", result.Value.Name);
        Assert.Equal(domain, result.Value.Domain);
        Assert.Equal(subdomain, result.Value.Subdomain);
        Assert.Equal(action, result.Value.Action);
        Assert.Equal(description, result.Value.Description);

        _mockRepository.Verify(
            x => x.AddAsync(It.Is<Scope>(s => s.Name == $"{domain}:{subdomain}:{action}"), cancellationToken),
            Times.Once);
    }

    public static IEnumerable<object[]> DuplicateScopeCases =>
        new List<object[]>
        {
            new[] { (object)"groups", "management", "create", (object)"Create group" },
            new[] { (object)"users", "profile", "read", (object)"Read profile" },
            new[] { (object)"orders", "items", "delete", (object?)null! },
        };

    [Theory]
    [MemberData(nameof(DuplicateScopeCases))]
    public async Task HandleAsync_ScopeAlreadyExists_ReturnsConflictError(
        string domain, string subdomain, string action, string? description)
    {
        // Arrange
        var command = new CreateScopeCommand(domain, subdomain, action, description);
        var cancellationToken = CancellationToken.None;

        var existingScope = new Scope
        {
            Id = 1,
            Name = $"{domain}:{subdomain}:{action}",
            Domain = domain,
            Subdomain = subdomain,
            Action = action,
            Description = description
        };

        _mockRepository
            .Setup(x => x.GetByScopeFormatAsync(domain, subdomain, action, cancellationToken))
            .ReturnsAsync(existingScope);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.Error?.StatusCode);
        Assert.Contains("already exists", result.Error?.Message ?? "", StringComparison.OrdinalIgnoreCase);

        _mockRepository.Verify(x => x.AddAsync(It.IsAny<Scope>(), cancellationToken), Times.Never);
    }

    public static IEnumerable<object[]> ComplexScopeNameCases =>
        new List<object[]>
        {
            new[] { (object)"admin", "audit_logs", "export", (object)"Export audit logs" },
            new[] { (object)"api_v2", "account_info", "view_profile", (object)"View profile info" },
            new[] { (object)"system_admin", "config_mgmt", "update_settings", (object?)null! },
        };

    [Theory]
    [MemberData(nameof(ComplexScopeNameCases))]
    public async Task HandleAsync_ComplexScopeName_FormattedCorrectly(
        string domain, string subdomain, string action, string? description)
    {
        // Arrange
        var command = new CreateScopeCommand(domain, subdomain, action, description);
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetByScopeFormatAsync(domain, subdomain, action, cancellationToken))
            .ReturnsAsync((Scope?)null);

        Scope? capturedScope = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Scope>(), cancellationToken))
            .Callback<Scope, CancellationToken>((scope, _) =>
            {
                capturedScope = scope;
                scope.Id = 1;
            })
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedScope);
        Assert.Equal($"{domain}:{subdomain}:{action}", capturedScope.Name);
        Assert.Equal(domain, capturedScope.Domain);
        Assert.Equal(subdomain, capturedScope.Subdomain);
        Assert.Equal(action, capturedScope.Action);
    }
}

