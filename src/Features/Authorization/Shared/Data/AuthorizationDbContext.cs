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
                },
                new Scope
                {
                    Id = 8,
                    Name = "scopes:management:sync",
                    Domain = "scopes",
                    Subdomain = "management",
                    Action = "sync",
                    Description = "Synchronize user scopes to Firebase claims",
                    CreatedAt = new DateTime(2026, 03, 24, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 9,
                    Name = "training:exercises:create",
                    Domain = "training",
                    Subdomain = "exercises",
                    Action = "create",
                    Description = "Create exercises",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 10,
                    Name = "training:exercises:read",
                    Domain = "training",
                    Subdomain = "exercises",
                    Action = "read",
                    Description = "Read exercises",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 11,
                    Name = "training:exercises:update",
                    Domain = "training",
                    Subdomain = "exercises",
                    Action = "update",
                    Description = "Update exercises",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 12,
                    Name = "training:exercises:delete",
                    Domain = "training",
                    Subdomain = "exercises",
                    Action = "delete",
                    Description = "Delete exercises",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 13,
                    Name = "training:exercises:suggest",
                    Domain = "training",
                    Subdomain = "exercises",
                    Action = "suggest",
                    Description = "Suggest exercises",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 14,
                    Name = "training:equipments:create",
                    Domain = "training",
                    Subdomain = "equipments",
                    Action = "create",
                    Description = "Create equipments",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 15,
                    Name = "training:equipments:read",
                    Domain = "training",
                    Subdomain = "equipments",
                    Action = "read",
                    Description = "Read equipments",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 16,
                    Name = "training:equipments:update",
                    Domain = "training",
                    Subdomain = "equipments",
                    Action = "update",
                    Description = "Update equipments",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 17,
                    Name = "training:equipments:delete",
                    Domain = "training",
                    Subdomain = "equipments",
                    Action = "delete",
                    Description = "Delete equipments",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 18,
                    Name = "training:workouts:create",
                    Domain = "training",
                    Subdomain = "workouts",
                    Action = "create",
                    Description = "Create workout sessions",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 19,
                    Name = "training:workouts:read",
                    Domain = "training",
                    Subdomain = "workouts",
                    Action = "read",
                    Description = "Read workout sessions",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 20,
                    Name = "training:workouts:complete",
                    Domain = "training",
                    Subdomain = "workouts",
                    Action = "complete",
                    Description = "Complete workout sessions",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 21,
                    Name = "training:dashboard:read",
                    Domain = "training",
                    Subdomain = "dashboard",
                    Action = "read",
                    Description = "Read training dashboard",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 22,
                    Name = "training:workouts:create:self",
                    Domain = "training",
                    Subdomain = "workouts",
                    Action = "create_self",
                    Description = "Create workouts for self",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 23,
                    Name = "training:workouts:create:trainer",
                    Domain = "training",
                    Subdomain = "workouts",
                    Action = "create_trainer",
                    Description = "Create workouts as trainer for trainer-client links",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 24,
                    Name = "training:workouts:create",
                    Domain = "training",
                    Subdomain = "workouts",
                    Action = "create_gym_staff",
                    Description = "Create workouts as gym trainer staff",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 25,
                    Name = "training:muscles:create",
                    Domain = "training",
                    Subdomain = "muscles",
                    Action = "create",
                    Description = "Create muscles",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 26,
                    Name = "training:muscles:read",
                    Domain = "training",
                    Subdomain = "muscles",
                    Action = "read",
                    Description = "Read muscles",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 27,
                    Name = "training:muscles:update",
                    Domain = "training",
                    Subdomain = "muscles",
                    Action = "update",
                    Description = "Update muscles",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 28,
                    Name = "training:muscles:delete",
                    Domain = "training",
                    Subdomain = "muscles",
                    Action = "delete",
                    Description = "Delete muscles",
                    CreatedAt = new DateTime(2026, 03, 26, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 29,
                    Name = "groups:management:read",
                    Domain = "groups",
                    Subdomain = "management",
                    Action = "read",
                    Description = "Read groups",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 30,
                    Name = "scopes:management:read",
                    Domain = "scopes",
                    Subdomain = "management",
                    Action = "read",
                    Description = "Read scopes",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 31,
                    Name = "users:profile:read",
                    Domain = "users",
                    Subdomain = "profile",
                    Action = "read",
                    Description = "Read user profile",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 32,
                    Name = "gym:clients:read",
                    Domain = "gym",
                    Subdomain = "clients",
                    Action = "read_gym_staff",
                    Description = "Read gym clients as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 33,
                    Name = "gym:clients:create",
                    Domain = "gym",
                    Subdomain = "clients",
                    Action = "create_gym_staff",
                    Description = "Enroll gym clients as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 34,
                    Name = "gym:clients:assign_trainer",
                    Domain = "gym",
                    Subdomain = "clients",
                    Action = "assign_trainer_gym_staff",
                    Description = "Assign gym client trainer as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 35,
                    Name = "gym:plans:read",
                    Domain = "gym",
                    Subdomain = "plans",
                    Action = "read_gym_plan",
                    Description = "Read gym plans as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 36,
                    Name = "gym:plans:create",
                    Domain = "gym",
                    Subdomain = "plans",
                    Action = "create_gym_plan",
                    Description = "Create gym plans as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 37,
                    Name = "gym:plans:update",
                    Domain = "gym",
                    Subdomain = "plans",
                    Action = "update_gym_plan",
                    Description = "Update gym plans as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 38,
                    Name = "gym:plans:delete",
                    Domain = "gym",
                    Subdomain = "plans",
                    Action = "delete_gym_plan",
                    Description = "Delete gym plans as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 39,
                    Name = "gym:read",
                    Domain = "gym",
                    Subdomain = "gyms",
                    Action = "read_gym_staff",
                    Description = "Read gyms as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 40,
                    Name = "gym:create",
                    Domain = "gym",
                    Subdomain = "gyms",
                    Action = "create_gym_staff",
                    Description = "Create gyms as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 41,
                    Name = "gym:update",
                    Domain = "gym",
                    Subdomain = "gyms",
                    Action = "update_gym_staff",
                    Description = "Update gyms as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 42,
                    Name = "gym:delete",
                    Domain = "gym",
                    Subdomain = "gyms",
                    Action = "delete_gym_staff",
                    Description = "Delete gyms as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 43,
                    Name = "gym:staff:read",
                    Domain = "gym",
                    Subdomain = "staff",
                    Action = "read_gym_staff",
                    Description = "Read gym staff as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 44,
                    Name = "gym:staff:create",
                    Domain = "gym",
                    Subdomain = "staff",
                    Action = "create_gym_staff",
                    Description = "Add gym staff as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 45,
                    Name = "gym:staff:delete",
                    Domain = "gym",
                    Subdomain = "staff",
                    Action = "delete_gym_staff",
                    Description = "Remove gym staff as gym owner or staff",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 46,
                    Name = "gym:platform_tiers:read",
                    Domain = "gym",
                    Subdomain = "platform_tiers",
                    Action = "read",
                    Description = "Read platform tiers",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 47,
                    Name = "gym:platform_tiers:create",
                    Domain = "gym",
                    Subdomain = "platform_tiers",
                    Action = "create",
                    Description = "Create platform tiers",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 48,
                    Name = "gym:platform_tiers:update",
                    Domain = "gym",
                    Subdomain = "platform_tiers",
                    Action = "update",
                    Description = "Update platform tiers",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 49,
                    Name = "gym:platform_tiers:delete",
                    Domain = "gym",
                    Subdomain = "platform_tiers",
                    Action = "delete",
                    Description = "Delete platform tiers",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 50,
                    Name = "gym:trainer_clients:read",
                    Domain = "gym",
                    Subdomain = "trainer_clients",
                    Action = "read",
                    Description = "Read trainer clients",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 51,
                    Name = "gym:trainer_clients:create",
                    Domain = "gym",
                    Subdomain = "trainer_clients",
                    Action = "create",
                    Description = "Create trainer clients",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 52,
                    Name = "gym:trainer_clients:transfer",
                    Domain = "gym",
                    Subdomain = "trainer_clients",
                    Action = "transfer",
                    Description = "Transfer trainer clients",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 53,
                    Name = "gym:trainer_plans:read",
                    Domain = "gym",
                    Subdomain = "trainer_plans",
                    Action = "read",
                    Description = "Read trainer plans",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 54,
                    Name = "gym:trainer_plans:create",
                    Domain = "gym",
                    Subdomain = "trainer_plans",
                    Action = "create",
                    Description = "Create trainer plans",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 55,
                    Name = "gym:trainer_plans:update",
                    Domain = "gym",
                    Subdomain = "trainer_plans",
                    Action = "update",
                    Description = "Update trainer plans",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 56,
                    Name = "gym:trainer_plans:delete",
                    Domain = "gym",
                    Subdomain = "trainer_plans",
                    Action = "delete",
                    Description = "Delete trainer plans",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 57,
                    Name = "gym:user_roles:read",
                    Domain = "gym",
                    Subdomain = "user_roles",
                    Action = "read",
                    Description = "Read user roles",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
                },
                new Scope
                {
                    Id = 58,
                    Name = "gym:user_roles:assign",
                    Domain = "gym",
                    Subdomain = "user_roles",
                    Action = "assign",
                    Description = "Assign user roles",
                    CreatedAt = new DateTime(2026, 03, 27, 0, 0, 0, DateTimeKind.Utc)
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

