namespace ShapeUp.Features.Safety.Shared;

using Microsoft.EntityFrameworkCore;

public class SafetyDbContext(DbContextOptions<SafetyDbContext> options) : DbContext(options)
{
    public DbSet<UserBlock> UserBlocks { get; set; }

    public DbSet<ContentReport> ContentReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserBlock>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.BlockerUserId, x.BlockedUserId }).IsUnique();
            entity.HasIndex(x => x.BlockedUserId);
        });

        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TargetType).HasMaxLength(16).IsRequired();
            entity.Property(x => x.TargetId).HasMaxLength(64);
            entity.Property(x => x.Reason).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(16).IsRequired();
            entity.Property(x => x.ResolutionNote).HasMaxLength(500);
            entity.HasIndex(x => new { x.Status, x.Id });
            entity.HasIndex(x => x.ReporterUserId);
        });
    }
}
