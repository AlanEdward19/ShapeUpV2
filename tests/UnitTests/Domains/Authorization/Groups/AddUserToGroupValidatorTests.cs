using ShapeUp.Features.Authorization.Groups.AddUserToGroup;

namespace UnitTests.Features.Authorization.Groups;

public class AddUserToGroupValidatorTests
{
    private readonly AddUserToGroupValidator _validator;

    public AddUserToGroupValidatorTests()
    {
        _validator = new AddUserToGroupValidator();
    }

    [Theory]
    [InlineData(1, 1, "Member")]
    [InlineData(5, 10, "Owner")]
    [InlineData(100, 50, "Administrator")]
    public async Task Validate_ValidCommand_HasNoErrors(int userId, int groupId, string role)
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: userId, GroupId: groupId, Role: role);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("Administrator")]
    [InlineData("Member")]
    [InlineData("owner")]
    [InlineData("ADMINISTRATOR")]
    public async Task Validate_ValidRoles_IsValid(string role)
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 1, GroupId: 1, Role: role);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("Guest")]
    [InlineData("SuperAdmin")]
    [InlineData("User")]
    public async Task Validate_InvalidRole_HasError(string role)
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 1, GroupId: 1, Role: role);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Role");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_EmptyOrWhitespaceRole_HasError(string role)
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 1, GroupId: 1, Role: role);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Role");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Validate_ZeroOrNegativeUserId_HasError(int userId)
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: userId, GroupId: 1, Role: "Member");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public async Task Validate_ZeroOrNegativeGroupId_HasError(int groupId)
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: 1, GroupId: groupId, Role: "Member");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "GroupId");
    }

    [Theory]
    [InlineData(0, 0, "")]
    [InlineData(-1, -1, "Guest")]
    public async Task Validate_MultipleErrors_HasAllErrors(int userId, int groupId, string role)
    {
        // Arrange
        var command = new AddUserToGroupCommand(UserId: userId, GroupId: groupId, Role: role);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 2);
    }
}

