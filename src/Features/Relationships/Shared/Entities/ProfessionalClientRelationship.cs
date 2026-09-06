namespace ShapeUp.Features.Relationships.Shared.Entities;

public class ProfessionalClientRelationship
{
    public int Id { get; set; }

    public int ProfessionalUserId { get; set; }

    public int ClientUserId { get; set; }

    public required string RelationshipType { get; set; }

    public RelationshipStatus Status { get; set; } = RelationshipStatus.Active;

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }
}
