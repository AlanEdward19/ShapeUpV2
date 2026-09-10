namespace ShapeUp.Features.Nutrition.Shared.Entities;

public class WeightRegister
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Weight { get; set; }
    public DateOnly Date { get; set; }
}
