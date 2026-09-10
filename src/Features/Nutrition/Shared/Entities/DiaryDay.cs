namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class DiaryDay
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime? EvaluatedAtUtc { get; set; }
    public List<DiaryEntry> Entries { get; set; } = [];
}
