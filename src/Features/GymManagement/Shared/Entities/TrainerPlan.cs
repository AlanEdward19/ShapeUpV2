namespace ShapeUp.Features.GymManagement.Shared.Entities;

public class TrainerPlan
{
    public int Id { get; set; }
    public int TrainerId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<TrainerClient> Clients { get; set; } = [];
}

