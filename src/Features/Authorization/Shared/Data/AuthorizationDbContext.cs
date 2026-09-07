namespace ShapeUp.Features.Authorization.Shared.Data;

using Microsoft.EntityFrameworkCore;
using Entities;

public class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.FirebaseUid).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.FirebaseUid).IsRequired();
        });
    }
}
