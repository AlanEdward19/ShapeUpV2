namespace ShapeUp.Features.GymManagement.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Shared.Entities;

public class GymManagementDbContext(DbContextOptions<GymManagementDbContext> options) : DbContext(options)
{
    public DbSet<PlatformTier> PlatformTiers { get; set; }
    public DbSet<UserPlatformRole> UserPlatformRoles { get; set; }
    public DbSet<Gym> Gyms { get; set; }
    public DbSet<GymPlan> GymPlans { get; set; }
    public DbSet<GymStaff> GymStaff { get; set; }
    public DbSet<GymClient> GymClients { get; set; }
    public DbSet<TrainerPlan> TrainerPlans { get; set; }
    public DbSet<TrainerClient> TrainerClients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlatformTier>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Price).HasPrecision(10, 2);
            e.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<UserPlatformRole>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => new { u.UserId, u.Role }).IsUnique();
            e.HasOne(u => u.PlatformTier)
                .WithMany()
                .HasForeignKey(u => u.PlatformTierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Gym>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).IsRequired().HasMaxLength(200);
            e.Property(g => g.Address).HasMaxLength(500);
            e.HasOne(g => g.PlatformTier)
                .WithMany()
                .HasForeignKey(g => g.PlatformTierId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(g => g.OwnerId);
        });

        modelBuilder.Entity<GymPlan>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Price).HasPrecision(10, 2);
            e.HasOne(p => p.Gym)
                .WithMany(g => g.Plans)
                .HasForeignKey(p => p.GymId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GymStaff>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.GymId, s.UserId }).IsUnique();
            e.HasOne(s => s.Gym)
                .WithMany(g => g.Staff)
                .HasForeignKey(s => s.GymId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GymClient>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.GymId, c.UserId }).IsUnique();
            e.HasOne(c => c.Gym)
                .WithMany(g => g.Clients)
                .HasForeignKey(c => c.GymId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.GymPlan)
                .WithMany()
                .HasForeignKey(c => c.GymPlanId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Trainer)
                .WithMany(s => s.AssignedClients)
                .HasForeignKey(c => c.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrainerPlan>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.Price).HasPrecision(10, 2);
            e.HasIndex(p => p.TrainerId);
        });

        modelBuilder.Entity<TrainerClient>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.TrainerId, c.ClientId }).IsUnique();
            e.HasOne(c => c.TrainerPlan)
                .WithMany(p => p.Clients)
                .HasForeignKey(c => c.TrainerPlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

