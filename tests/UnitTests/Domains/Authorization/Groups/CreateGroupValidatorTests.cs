using ShapeUp.Features.Authorization.Groups.CreateGroup;

namespace UnitTests.Domains.Authorization.Groups;

public class CreateGroupValidatorTests
{
    private readonly CreateGroupValidator _validator;

    public CreateGroupValidatorTests()
    {
        _validator = new CreateGroupValidator();
    }

    [Theory]
    [InlineData("Engineering Team", "Team description", true)]
    [InlineData("QA Team", "Quality Assurance", true)]
    [InlineData("DevOps", null, true)]
    [InlineData("A", "Single letter", true)]
    public async Task Validate_ValidCommand_HasNoErrors(string name, string? description, bool expectedValid)
    {
        // Arrange
        var command = new CreateGroupCommand(name, description);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.Equal(expectedValid, result.IsValid);
        if (expectedValid)
            Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task Validate_EmptyOrWhitespaceName_HasError(string name)
    {
        // Arrange
        var command = new CreateGroupCommand(name, "Team description");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    [InlineData(200)]
    public async Task Validate_NameExceedsMaxLength_HasError(int length)
    {
        // Arrange
        var longName = new string('A', length);
        var command = new CreateGroupCommand(longName, "Description");

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Validate_NameWithinMaxLength_IsValid(int length)
    {
        // Arrange
        var validName = new string('A', length);
        var command = new CreateGroupCommand(validName, "Description");

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
        var command = new CreateGroupCommand("Valid Team Name", description);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }
}

