using ShapeUp.Features.Nutrition.Comments.AddComment;
using ShapeUp.Shared.Safety;

namespace UnitTests.Domains.Safety;

public class ObjectionableTextTests
{
    [Theory]
    [InlineData("seu babaca", true)]
    [InlineData("Você é um BABACA!", true)]
    [InlineData("que merda", true)]
    [InlineData("fuck you", true)]
    [InlineData("desgraçado", true)]
    [InlineData("Ótimo progresso, continue assim", false)]
    [InlineData("a classe e o passado", false)]
    [InlineData("pútrido", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Contains_MatchesWholeWords_IgnoringAccentsAndCase(string? text, bool expected) =>
        Assert.Equal(expected, ObjectionableText.Contains(text));

    [Fact]
    public void DiaryComment_WithObjectionableText_IsInvalid()
    {
        var validator = new AddDiaryCommentCommandValidator();

        Assert.False(validator.Validate(new AddDiaryCommentCommand(new DateOnly(2026, 10, 3), "seu babaca")).IsValid);
        Assert.True(validator.Validate(new AddDiaryCommentCommand(new DateOnly(2026, 10, 3), "Bom trabalho hoje")).IsValid);
    }
}
