namespace ShapeUp.Features.Training.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Shared.Entities;

public class TrainingDbContext(DbContextOptions<TrainingDbContext> options) : DbContext(options)
{
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<ExerciseStep> ExerciseSteps { get; set; }
    public DbSet<ExerciseMuscleProfile> ExerciseMuscleProfiles { get; set; }
    public DbSet<Equipment> Equipments { get; set; }
    public DbSet<ExerciseEquipment> ExerciseEquipments { get; set; }
    public DbSet<ExerciseEquivalent> ExerciseEquivalents { get; set; }
    public DbSet<ExerciseRequest> ExerciseRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(160);
            entity.Property(x => x.NamePt).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.VideoUrl).HasMaxLength(1024);
            entity.Property(x => x.ExerciseType).HasDefaultValue(Shared.Enums.ExerciseType.WeightBased);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.NamePt);
            entity.HasIndex(x => x.ExerciseRequestId);
            entity.HasOne<ExerciseRequest>()
                .WithMany()
                .HasForeignKey(x => x.ExerciseRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExerciseRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(16);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Kind).IsRequired().HasMaxLength(16);
            entity.Property(x => x.MuscleGroups).IsRequired().HasMaxLength(400);
            entity.Property(x => x.EquipmentIds).IsRequired().HasMaxLength(400);
            entity.Property(x => x.EquipmentOther).HasMaxLength(120);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.ReferenceUrl).HasMaxLength(300);
            entity.Property(x => x.RequestedByRole).IsRequired().HasMaxLength(32);
            entity.Property(x => x.ApprovedExerciseName).HasMaxLength(160);
            entity.Property(x => x.RejectionReason).HasMaxLength(280);
            entity.Property(x => x.AdminNote).HasMaxLength(500);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne(x => x.ApprovedExercise)
                .WithMany()
                .HasForeignKey(x => x.ApprovedExerciseId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.ApprovedExerciseId);
            entity.HasIndex(x => new { x.RequestedByUserId, x.Id });
            entity.HasIndex(x => new { x.Status, x.Id });
        });

        modelBuilder.Entity<ExerciseStep>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(500);
            entity.HasOne(x => x.Exercise)
                .WithMany(x => x.Steps)
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExerciseMuscleProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActivationPercent).HasPrecision(5, 2);
            entity.Property(x => x.MuscleGroup);
            entity.HasIndex(x => new { x.ExerciseId, x.MuscleGroup }).IsUnique();
            entity.HasOne(x => x.Exercise)
                .WithMany(x => x.MuscleProfiles)
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(120);
            entity.Property(x => x.NamePt).IsRequired().HasMaxLength(120);
            entity.Property(x => x.Description).HasMaxLength(600);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.NamePt).IsUnique();
        });

        modelBuilder.Entity<ExerciseEquipment>(entity =>
        {
            entity.HasKey(x => new { x.ExerciseId, x.EquipmentId });
            entity.HasOne(x => x.Exercise)
                .WithMany(x => x.ExerciseEquipments)
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Equipment)
                .WithMany(x => x.ExerciseEquipments)
                .HasForeignKey(x => x.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExerciseEquivalent>(entity =>
        {
            entity.HasKey(x => new { x.ExerciseId, x.EquivalentExerciseId });
            entity.HasIndex(x => x.EquivalentExerciseId);
        });
    }
}
