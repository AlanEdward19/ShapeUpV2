namespace ShapeUp.Features.Nutrition.Shared.Entities;

/// <summary>A nutritionist's comment on a client's diary day (or on one entry of that day).</summary>
public class NutritionDiaryComment
{
    public int Id { get; set; }
    public int ClientUserId { get; set; }
    public DateOnly Date { get; set; }
    public string? EntryId { get; set; }
    public int AuthorUserId { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
