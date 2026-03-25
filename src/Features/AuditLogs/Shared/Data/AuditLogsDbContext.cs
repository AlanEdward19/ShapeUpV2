namespace ShapeUp.Features.AuditLogs.Shared.Data;

using Entities;
using Microsoft.EntityFrameworkCore;

public class AuditLogsDbContext(DbContextOptions<AuditLogsDbContext> options) : DbContext(options)
{
    public DbSet<AuditLogEntry> AuditLogEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.HttpMethod).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Endpoint).HasMaxLength(512).IsRequired();
            entity.Property(x => x.UserEmail).HasMaxLength(320);
            entity.Property(x => x.TraceId).HasMaxLength(128);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(512);
            entity.Property(x => x.QueryParametersJson).HasMaxLength(4000);
            entity.Property(x => x.RequestBodyJson).HasMaxLength(4000);

            entity.HasIndex(x => x.OccurredAtUtc);
            entity.HasIndex(x => x.UserEmail);
            entity.HasIndex(x => x.Endpoint);
        });
    }
}

