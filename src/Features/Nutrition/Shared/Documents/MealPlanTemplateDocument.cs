using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShapeUp.Features.Nutrition.Shared.Documents;

/// <summary>A reusable meal plan owned by a nutritionist, handed to clients with one call.</summary>
public class MealPlanTemplateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<MealPlanItemDocument> Items { get; set; } = [];
}
