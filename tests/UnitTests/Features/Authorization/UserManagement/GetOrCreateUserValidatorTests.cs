using ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

namespace UnitTests.Features.Authorization.UserManagement;

public class GetOrCreateUserValidatorTests
{
    private readonly GetOrCreateUserValidator _validator;

    public GetOrCreateUserValidatorTests()
    {
        _validator = new GetOrCreateUserValidator();
    }

    [Theory]
    [InlineData("firebase-uid-123", "test@example.com", "Test User", true)]
    [InlineData("firebase-uid-456", "another@example.com", "Another User", true)]
    [InlineData("uid-789", "user789@example.com", "User 789", true)]
    [InlineData("firebase-uid-123", "test@example.com", null, true)]
    public async Task Validate_ValidCommand_HasNoErrors(string firebaseUid, string email, string? displayName, bool expectedValid)
    {
        // Arrange
        var command = new GetOrCreateUserCommand(firebaseUid, email, displayName);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.Equal(expectedValid, result.IsValid);
        if (expectedValid)
            Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "test@example.com", "Test User")]
    [InlineData("", "valid@example.com", null)]
    [InlineData("  ", "test@example.com", "User")]
    public async Task Validate_EmptyOrWhitespaceFirebaseUid_HasError(string firebaseUid, string email, string? displayName)
    {
        // Arrange
        var command = new GetOrCreateUserCommand(firebaseUid, email, displayName);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FirebaseUid");
    }

    [Theory]
    [InlineData("firebase-uid-123", "invalid-email", "Test User")]
    [InlineData("firebase-uid-456", "no-at-sign", "Another User")]
    [InlineData("uid-789", "user@", "User 789")]
    [InlineData("firebase-uid-123", "@example.com", null)]
    public async Task Validate_InvalidEmail_HasError(string firebaseUid, string email, string? displayName)
    {
        // Arrange
        var command = new GetOrCreateUserCommand(firebaseUid, email, displayName);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("firebase-uid-123", "")]
    [InlineData("firebase-uid-456", "  ")]
    public async Task Validate_EmptyOrWhitespaceEmail_HasError(string firebaseUid, string email)
    {
        // Arrange
        var command = new GetOrCreateUserCommand(firebaseUid, email, "Test User");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("@nodomain")]
    public async Task Validate_MultipleFieldsInvalid_HasMultipleErrors(string email)
    {
        // Arrange
        var command = new GetOrCreateUserCommand("", email, "Test User");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
    }
}

