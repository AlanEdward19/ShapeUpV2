namespace ShapeUp.Features.Nutrition.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using Shared.ValueObjects;

public class NutritionDbContext(DbContextOptions<NutritionDbContext> options) : DbContext(options)
{
    public DbSet<NutritionProfile> Profiles { get; set; }
    public DbSet<WeightTarget> WeightTargets { get; set; }
    public DbSet<WeightRegister> WeightRegisters { get; set; }
    public DbSet<DiaryDay> DiaryDays { get; set; }
    public DbSet<DiaryEntry> DiaryEntries { get; set; }
    public DbSet<NutritionGoalEvaluation> GoalEvaluations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NutritionProfile>(entity =>
        {
            entity.ToTable("NutritionProfiles");
            entity.HasKey(p => p.UserId);
            entity.Property(p => p.UserId).ValueGeneratedNever();
            entity.Property(p => p.BiologicalSex).HasMaxLength(16);
            entity.Property(p => p.ActivityLevel).HasMaxLength(32);
            entity.Property(p => p.UpdatedAtUtc).IsRequired();

            entity.OwnsOne(p => p.ActiveGoal, goal =>
            {
                goal.Property(g => g.Kcal).HasColumnName("ActiveGoal_Kcal");
                goal.Property(g => g.ProteinG).HasColumnName("ActiveGoal_ProteinG");
                goal.Property(g => g.CarbG).HasColumnName("ActiveGoal_CarbG");
                goal.Property(g => g.FatG).HasColumnName("ActiveGoal_FatG");
            });
        });

        modelBuilder.Entity<WeightTarget>(entity =>
        {
            entity.ToTable("NutritionWeightTargets");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TargetWeight).HasPrecision(6, 2);
            entity.Property(t => t.UpdatedAtUtc).IsRequired();
            entity.HasIndex(t => t.UserId).IsUnique();
        });

        modelBuilder.Entity<WeightRegister>(entity =>
        {
            entity.ToTable("NutritionWeightRegisters");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Weight).HasPrecision(6, 2);
            entity.HasIndex(r => new { r.UserId, r.Date }).IsUnique();
        });

        modelBuilder.Entity<DiaryDay>(entity =>
        {
            entity.ToTable("NutritionDiaryDays");
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => new { d.UserId, d.Date }).IsUnique();
            entity.HasMany(d => d.Entries)
                .WithOne(e => e.DiaryDay)
                .HasForeignKey(e => e.DiaryDayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiaryEntry>(entity =>
        {
            entity.ToTable("NutritionDiaryEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(24);
            entity.Property(e => e.MealSlot).IsRequired().HasMaxLength(32);
            entity.Property(e => e.FoodId).IsRequired().HasMaxLength(24);
            entity.Property(e => e.QuantityGramsOrMl).HasPrecision(10, 2);

            entity.OwnsOne(e => e.ComputedMacros, macros =>
            {
                macros.Property(m => m.Kcal).HasColumnName("ComputedMacros_Kcal");
                macros.Property(m => m.ProteinG).HasColumnName("ComputedMacros_ProteinG");
                macros.Property(m => m.CarbG).HasColumnName("ComputedMacros_CarbG");
                macros.Property(m => m.FatG).HasColumnName("ComputedMacros_FatG");
            });
        });

        modelBuilder.Entity<NutritionGoalEvaluation>(entity =>
        {
            entity.ToTable("NutritionGoalEvaluations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EvaluatedAtUtc).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
        });
    }
}
