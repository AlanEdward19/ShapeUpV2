namespace ShapeUp.Features.Gamification.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Shared.Entities;

public class GamificationDbContext(DbContextOptions<GamificationDbContext> options) : DbContext(options)
{
    public DbSet<GamificationProfile> Profiles { get; set; }
    public DbSet<WorkoutEvaluation> Evaluations { get; set; }
    public DbSet<GamificationNutritionEvaluation> NutritionEvaluations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<GamificationProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.Property(p => p.UserId).ValueGeneratedNever();
            entity.Property(p => p.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<WorkoutEvaluation>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(24);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Classification).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.EvaluatedAtUtc).IsRequired();

            entity.HasIndex(e => new { e.UserId, e.EvaluatedAtUtc });
        });

        modelBuilder.Entity<GamificationNutritionEvaluation>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Date });
            entity.Property(e => e.CreditedAtUtc).IsRequired();
        });
    }
}
