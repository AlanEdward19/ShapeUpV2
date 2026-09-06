namespace ShapeUp.Features.Credentials.Shared.Data;

using Entities;
using Microsoft.EntityFrameworkCore;

public class CredentialsDbContext(DbContextOptions<CredentialsDbContext> options) : DbContext(options)
{
    public DbSet<ProfessionalCredential> ProfessionalCredentials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProfessionalCredential>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProfessionType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CredentialNumber).HasMaxLength(64).IsRequired();
            entity.Property(x => x.IssuingAuthority).HasMaxLength(64).IsRequired();
            entity.Property(x => x.IssuingRegion).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Country).HasMaxLength(64).IsRequired();

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.ProfessionType, x.Status });
        });
    }
}
