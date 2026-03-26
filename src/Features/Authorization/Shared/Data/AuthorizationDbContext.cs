namespace ShapeUp.Features.Authorization.Shared.Data;

using Microsoft.EntityFrameworkCore;
using Entities;

public class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    public DbSet<Group> Groups { get; set; }

    public DbSet<UserGroup> UserGroups { get; set; }

    public DbSet<Scope> Scopes { get; set; }

    public DbSet<UserScope> UserScopes { get; set; }

    public DbSet<GroupScope> GroupScopes { get; set; }

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

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).IsRequired();
            entity.HasMany(g => g.Members)
                .WithOne(ug => ug.Group)
                .HasForeignKey(ug => ug.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(g => g.Scopes)
                .WithOne(gs => gs.Group)
                .HasForeignKey(gs => gs.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserGroup configuration (composite key)
        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.HasKey(ug => new { ug.UserId, ug.GroupId });
            entity.HasOne(ug => ug.User)
                .WithMany(u => u.Groups)
                .HasForeignKey(ug => ug.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ug => ug.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(ug => ug.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Scope configuration
        modelBuilder.Entity<Scope>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired();
            entity.Property(s => s.Domain).IsRequired();
            entity.Property(s => s.Subdomain).IsRequired();
            entity.Property(s => s.Action).IsRequired();
            entity.HasIndex(s => new { s.Domain, s.Subdomain, s.Action }).IsUnique();
            entity.HasData([
                new Scope
                {
                    Id = 1,
                    Name = "groups:management:create",
                    Domain = "groups",
                    Subdomain = "management",
                    Action = "create",
                    Description = "Create groups",
                    CreatedAt = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 2,
                    Name = "groups:management:delete",
                    Domain = "groups",
                    Subdomain = "management",
                    Action = "delete",
                    Description = "Delete groups",
                    CreatedAt = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 3,
                    Name = "groups:management:manage_members",
                    Domain = "groups",
                    Subdomain = "management",
                    Action = "manage_members",
                    Description = "Manage group members",
                    CreatedAt = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 4,
                    Name = "groups:management:manage_scopes",
                    Domain = "groups",
                    Subdomain = "management",
                    Action = "manage_scopes",
                    Description = "Manage group scopes",
                    CreatedAt = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 5,
                    Name = "scopes:management:create",
                    Domain = "scopes",
                    Subdomain = "management",
                    Action = "create",
                    Description = "Create scopes",
                    CreatedAt = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 6,
                    Name = "audit:logs:read",
                    Domain = "audit",
                    Subdomain = "logs",
                    Action = "read",
                    Description = "Read audit logs",
                    CreatedAt = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 7,
                    Name = "scopes:management:assign",
                    Domain = "scopes",
                    Subdomain = "management",
                    Action = "assign",
                    Description = "Assign scope",
                    CreatedAt = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc)
                }
            ]);
            entity.HasMany(s => s.Users)
                .WithOne(us => us.Scope)
                .HasForeignKey(us => us.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(s => s.Groups)
                .WithOne(gs => gs.Scope)
                .HasForeignKey(gs => gs.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserScope configuration (composite key)
        modelBuilder.Entity<UserScope>(entity =>
        {
            entity.HasKey(us => new { us.UserId, us.ScopeId });
            entity.HasOne(us => us.User)
                .WithMany(u => u.Scopes)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(us => us.Scope)
                .WithMany(s => s.Users)
                .HasForeignKey(us => us.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GroupScope configuration (composite key)
        modelBuilder.Entity<GroupScope>(entity =>
        {
            entity.HasKey(gs => new { gs.GroupId, gs.ScopeId });
            entity.HasOne(gs => gs.Group)
                .WithMany(g => g.Scopes)
                .HasForeignKey(gs => gs.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(gs => gs.Scope)
                .WithMany(s => s.Groups)
                .HasForeignKey(gs => gs.ScopeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}

