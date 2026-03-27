using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.AuditLogs.Infrastructure.Repositories;
using ShapeUp.Features.AuditLogs.Shared.Data;
using ShapeUp.Features.AuditLogs.Shared.Entities;

namespace UnitTests.Domains.AuditLogs;

public class AuditLogRepositoryTests
{
    private readonly AuditLogsDbContext _context;
    private readonly AuditLogRepository _repository;

    public AuditLogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuditLogsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AuditLogsDbContext(options);
        _repository = new AuditLogRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ValidEntry_SavesSuccessfully()
    {
        // Arrange
        var entry = new AuditLogEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            UserEmail = "test@example.com",
            HttpMethod = "GET",
            Endpoint = "/api/users",
            StatusCode = 200,
            DurationMs = 150,
            TraceId = "trace-123"
        };

        // Act
        await _repository.AddAsync(entry, CancellationToken.None);

        // Assert
        var savedEntry = await _context.Set<AuditLogEntry>().FirstOrDefaultAsync();
        Assert.NotNull(savedEntry);
        Assert.Equal("test@example.com", savedEntry.UserEmail);
        Assert.Equal("GET", savedEntry.HttpMethod);
        Assert.Equal("/api/users", savedEntry.Endpoint);
    }

    [Fact]
    public async Task GetPageAsync_NoFilters_ReturnsAllEntries()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-20),
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-1"
            },
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-10),
                UserEmail = "user2@example.com",
                HttpMethod = "POST",
                Endpoint = "/api/groups",
                StatusCode = 201,
                DurationMs = 200,
                TraceId = "trace-2"
            },
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user1@example.com",
                HttpMethod = "DELETE",
                Endpoint = "/api/users/1",
                StatusCode = 204,
                DurationMs = 80,
                TraceId = "trace-3"
            }
        };

        await _context.Set<AuditLogEntry>().AddRangeAsync(entries);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPageAsync(null, 10, null, null, null, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        // Results should be ordered by Id descending (newest first)
        Assert.Equal("trace-3", result[0].TraceId);
        Assert.Equal("trace-2", result[1].TraceId);
        Assert.Equal("trace-1", result[2].TraceId);
    }

    [Fact]
    public async Task GetPageAsync_WithPageSize_RespectsPageSize()
    {
        // Arrange
        var entries = new List<AuditLogEntry>();
        for (int i = 0; i < 5; i++)
        {
            entries.Add(new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-i),
                UserEmail = $"user{i}@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/test",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = $"trace-{i}"
            });
        }

        await _context.Set<AuditLogEntry>().AddRangeAsync(entries);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPageAsync(null, 2, null, null, null, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPageAsync_FilterByEndpoint_ReturnsMatchingEntries()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-10),
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-1"
            },
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user2@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/groups",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-2"
            }
        };

        await _context.Set<AuditLogEntry>().AddRangeAsync(entries);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPageAsync(null, 10, "/api/users", null, null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("/api/users", result[0].Endpoint);
    }

    [Fact]
    public async Task GetPageAsync_FilterByMethod_ReturnsMatchingEntries()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-10),
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-1"
            },
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user2@example.com",
                HttpMethod = "POST",
                Endpoint = "/api/users",
                StatusCode = 201,
                DurationMs = 200,
                TraceId = "trace-2"
            }
        };

        await _context.Set<AuditLogEntry>().AddRangeAsync(entries);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPageAsync(null, 10, null, "POST", null, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("POST", result[0].HttpMethod);
    }

    [Fact]
    public async Task GetPageAsync_FilterByUserEmail_ReturnsMatchingEntries()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-10),
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-1"
            },
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user2@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-2"
            }
        };

        await _context.Set<AuditLogEntry>().AddRangeAsync(entries);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPageAsync(null, 10, null, null, "user1@example.com", CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("user1@example.com", result[0].UserEmail);
    }

    [Fact]
    public async Task GetPageAsync_WithCursor_SkipsEntriesBeforeCursor()
    {
        // Arrange
        var entries = new List<AuditLogEntry>();
        for (int i = 1; i <= 5; i++)
        {
            entries.Add(new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-i),
                UserEmail = $"user{i}@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/test",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = $"trace-{i}"
            });
        }

        await _context.Set<AuditLogEntry>().AddRangeAsync(entries);
        await _context.SaveChangesAsync();

        var allEntries = await _context.Set<AuditLogEntry>().AsNoTracking().ToListAsync();
        var cursorId = allEntries[2].Id; // Get Id of third entry

        // Act
        var result = await _repository.GetPageAsync(cursorId, 10, null, null, null, CancellationToken.None);

        // Assert
        // Should only return entries with Id < cursorId
        Assert.DoesNotContain(result, x => x.Id >= cursorId);
    }

    [Fact]
    public async Task GetPageAsync_MultipleFilters_AppliesAllFilters()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-10),
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-1"
            },
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-5),
                UserEmail = "user1@example.com",
                HttpMethod = "POST",
                Endpoint = "/api/users",
                StatusCode = 201,
                DurationMs = 200,
                TraceId = "trace-2"
            },
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user2@example.com",
                HttpMethod = "POST",
                Endpoint = "/api/users",
                StatusCode = 201,
                DurationMs = 200,
                TraceId = "trace-3"
            }
        };

        await _context.Set<AuditLogEntry>().AddRangeAsync(entries);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPageAsync(null, 10, "/api/users", "POST", "user1@example.com", CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("/api/users", result[0].Endpoint);
        Assert.Equal("POST", result[0].HttpMethod);
        Assert.Equal("user1@example.com", result[0].UserEmail);
    }

    [Fact]
    public async Task GetPageAsync_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-1"
            }
        };

        await _context.Set<AuditLogEntry>().AddRangeAsync(entries);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPageAsync(null, 10, "/api/nonexistent", null, null, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}

