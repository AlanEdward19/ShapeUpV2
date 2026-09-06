namespace ShapeUp.Features.Relationships.Shared.Data;

using Entities;
using Microsoft.EntityFrameworkCore;

public class RelationshipsDbContext(DbContextOptions<RelationshipsDbContext> options) : DbContext(options)
{
    public DbSet<ProfessionalClientRelationship> ProfessionalClientRelationships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProfessionalClientRelationship>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RelationshipType).HasMaxLength(32).IsRequired();

            entity.HasIndex(x => new { x.ProfessionalUserId, x.ClientUserId, x.RelationshipType })
                .IsUnique()
                .HasFilter($"[{nameof(ProfessionalClientRelationship.Status)}] = 0");

            entity.HasIndex(x => x.ClientUserId);
        });
    }
}
