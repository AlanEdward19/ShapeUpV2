namespace ShapeUp.Features.Credentials.Shared.Data;

using Entities;
using Microsoft.EntityFrameworkCore;

public class CredentialsDbContext(DbContextOptions<CredentialsDbContext> options) : DbContext(options)
{
    public const string OpenPerUserIndex = "UX_ProfessionalCredentials_OpenPerUserAndProfession";
    public const string OpenPerRegistrationIndex = "UX_ProfessionalCredentials_OpenPerRegistration";

    // Submitted = 1, UnderReview = 2, Verified = 3 (see CredentialStatus).
    private const string OpenOrVerifiedFilter = "[Status] IN (1, 2, 3)";

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

            entity.Property(x => x.RejectionReason).HasMaxLength(500);
            entity.Property(x => x.EndReason).HasMaxLength(500);
            entity.Property(x => x.RowVersion).IsRowVersion();

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.ProfessionType, x.Status });
            entity.HasIndex(x => x.Status);

            // One open/verified credential per user and profession, and one open/verified holder per council
            // registration. Both are enforced by the database so concurrent requests cannot slip past the handler checks.
            entity.HasIndex(x => new { x.UserId, x.ProfessionType })
                .IsUnique()
                .HasFilter(OpenOrVerifiedFilter)
                .HasDatabaseName(OpenPerUserIndex);
            entity.HasIndex(x => new { x.IssuingAuthority, x.IssuingRegion, x.CredentialNumber })
                .IsUnique()
                .HasFilter(OpenOrVerifiedFilter)
                .HasDatabaseName(OpenPerRegistrationIndex);
        });
    }
}
