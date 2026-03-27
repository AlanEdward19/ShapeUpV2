using ShapeUp.Features.Authorization.Scopes.CreateScope;

namespace UnitTests.Domains.Authorization.Scopes;

public class CreateScopeValidatorTests
{
    private readonly CreateScopeValidator _validator;

    public CreateScopeValidatorTests()
    {
        _validator = new CreateScopeValidator();
    }

    [Theory]
    [InlineData("users", "profile", "read", "Read user profile")]
    [InlineData("groups", "management", "create", "Create group")]
    [InlineData("orders", "items", "update", "Update order items")]
    [InlineData("admin", "audit_logs", "export", null)]
    public async Task Validate_ValidCommand_HasNoErrors(string domain, string subdomain, string action, string? description)
    {
        // Arrange
        var command = new CreateScopeCommand(domain, subdomain, action, description);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_EmptyOrWhitespaceDomain_HasError(string domain)
    {
        // Arrange
        var command = new CreateScopeCommand(domain, "profile", "read", "Read user profile");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Domain");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_EmptyOrWhitespaceSubdomain_HasError(string subdomain)
    {
        // Arrange
        var command = new CreateScopeCommand("users", subdomain, "read", "Read user profile");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Subdomain");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_EmptyOrWhitespaceAction_HasError(string action)
    {
        // Arrange
        var command = new CreateScopeCommand("users", "profile", action, "Read user profile");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Action");
    }

    [Theory]
    [InlineData("users-admin", "profile", "read")]
    [InlineData("users", "user-profile", "read")]
    [InlineData("users", "profile", "read-write")]
    [InlineData("users@domain", "profile", "read")]
    public async Task Validate_InvalidCharacters_HasError(string domain, string subdomain, string action)
    {
        // Arrange
        var command = new CreateScopeCommand(domain, subdomain, action, "Description");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 1);
    }

    [Theory]
    [InlineData("user_admin", "user_profile", "read_write")]
    [InlineData("admin_panel", "audit_logs", "export_data")]
    [InlineData("api_v1", "account_info", "view_profile")]
    public async Task Validate_ValidUnderscoresAndAlphanumeric_IsValid(string domain, string subdomain, string action)
    {
        // Arrange
        var command = new CreateScopeCommand(domain, subdomain, action, "Description");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Some Description")]
    [InlineData("A very long description with many characters and words")]
    public async Task Validate_VariousDescriptions_IsValid(string? description)
    {
        // Arrange
        var command = new CreateScopeCommand("users", "profile", "read", description);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("123", "456", "789")]
    [InlineData("1", "2", "3")]
    [InlineData("1000", "2000", "3000")]
    public async Task Validate_AllNumericParts_IsValid(string domain, string subdomain, string action)
    {
        // Arrange
        var command = new CreateScopeCommand(domain, subdomain, action, "Numeric scope");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }
}

