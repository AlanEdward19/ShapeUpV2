namespace ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Shared.Entities;

public class PlatformFeatureFlagsDbContext(DbContextOptions<PlatformFeatureFlagsDbContext> options) : DbContext(options)
{
    public DbSet<PlatformFeatureFlag> Flags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlatformFeatureFlag>(entity =>
        {
            entity.ToTable("PlatformFeatureFlags");
            entity.HasKey(f => f.Key);
            entity.Property(f => f.Key).HasMaxLength(128);
            entity.Property(f => f.UpdatedAtUtc).IsRequired();

            entity.HasData(
                new PlatformFeatureFlag
                {
                    Key = "notifications.email-enabled",
                    Enabled = true,
                    UpdatedAtUtc = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new PlatformFeatureFlag
                {
                    Key = "nutrition.intermittent-fasting",
                    Enabled = true,
                    UpdatedAtUtc = new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc)
                },
                new PlatformFeatureFlag { Key = "features.nutrition", Enabled = true, UpdatedAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PlatformFeatureFlag { Key = "features.training", Enabled = true, UpdatedAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PlatformFeatureFlag { Key = "features.gamification", Enabled = true, UpdatedAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PlatformFeatureFlag { Key = "features.gym-management", Enabled = true, UpdatedAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PlatformFeatureFlag { Key = "features.notifications", Enabled = true, UpdatedAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) });
        });
    }
}
