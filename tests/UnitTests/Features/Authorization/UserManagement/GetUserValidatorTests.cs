using ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

namespace UnitTests.Features.Authorization.UserManagement;

public class GetUserValidatorTests
{
    private readonly GetUserValidator _validator;

    public GetUserValidatorTests()
    {
        _validator = new GetUserValidator();
    }

    // -------------------------------------------------------------------------
    // Valid Id cases
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(999)]
    [InlineData(int.MaxValue)]
    public async Task Validate_ValidId_HasNoErrors(int id)
    {
        // Arrange
        var query = new GetUserQuery(id);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // -------------------------------------------------------------------------
    // Invalid Id cases (zero — NotEmpty fails for int)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    public async Task Validate_ZeroId_HasError(int id)
    {
        // Arrange
        var query = new GetUserQuery(id);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }
}
