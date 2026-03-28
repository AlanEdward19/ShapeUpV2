using ShapeUp.Features.AuditLogs.GetAuditLogs;
using ShapeUp.Features.AuditLogs.Shared.Abstractions;
using ShapeUp.Features.AuditLogs.Shared.Entities;
using ShapeUp.Shared.Pagination;

namespace UnitTests.Domains.AuditLogs;

public class GetAuditLogsHandlerTests
{
    private readonly Mock<IAuditLogRepository> _mockRepository;
    private readonly GetAuditLogsHandler _handler;

    public GetAuditLogsHandlerTests()
    {
        _mockRepository = new Mock<IAuditLogRepository>();
        _handler = new GetAuditLogsHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidQuery_ReturnsAuditLogs()
    {
        // Arrange
        var query = new GetAuditLogsQuery(Cursor: null, PageSize: 10, Endpoint: null, Method: null, UserEmail: null);
        var cancellationToken = CancellationToken.None;

        var auditLogs = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                Id = 1,
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 150,
                TraceId = "trace-1"
            },
            new AuditLogEntry
            {
                Id = 2,
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-10),
                UserEmail = "user2@example.com",
                HttpMethod = "POST",
                Endpoint = "/api/groups",
                StatusCode = 201,
                DurationMs = 250,
                TraceId = "trace-2"
            }
        };

        _mockRepository
            .Setup(x => x.GetPageAsync(null, 10, null, null, null, cancellationToken))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Items.Length);
        Assert.Equal("user1@example.com", result.Value.Items[0].UserEmail);
        Assert.Equal("GET", result.Value.Items[0].HttpMethod);

        _mockRepository.Verify(
            x => x.GetPageAsync(null, 10, null, null, null, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_FilterByEndpoint_ReturnsFilteredLogs()
    {
        // Arrange
        var query = new GetAuditLogsQuery(Cursor: null, PageSize: 10, Endpoint: "/api/users", Method: null, UserEmail: null);
        var cancellationToken = CancellationToken.None;

        var auditLogs = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                Id = 1,
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 150,
                TraceId = "trace-1"
            }
        };

        _mockRepository
            .Setup(x => x.GetPageAsync(null, 10, "/api/users", null, null, cancellationToken))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("/api/users", result.Value.Items[0].Endpoint);
    }

    [Fact]
    public async Task HandleAsync_FilterByMethod_ReturnsFilteredLogs()
    {
        // Arrange
        var query = new GetAuditLogsQuery(Cursor: null, PageSize: 10, Endpoint: null, Method: "POST", UserEmail: null);
        var cancellationToken = CancellationToken.None;

        var auditLogs = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                Id = 2,
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user2@example.com",
                HttpMethod = "POST",
                Endpoint = "/api/groups",
                StatusCode = 201,
                DurationMs = 250,
                TraceId = "trace-2"
            }
        };

        _mockRepository
            .Setup(x => x.GetPageAsync(null, 10, null, "POST", null, cancellationToken))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("POST", result.Value.Items[0].HttpMethod);
    }

    [Fact]
    public async Task HandleAsync_FilterByUserEmail_ReturnsFilteredLogs()
    {
        // Arrange
        var query = new GetAuditLogsQuery(Cursor: null, PageSize: 10, Endpoint: null, Method: null, UserEmail: "user1@example.com");
        var cancellationToken = CancellationToken.None;

        var auditLogs = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                Id = 1,
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user1@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/users",
                StatusCode = 200,
                DurationMs = 150,
                TraceId = "trace-1"
            }
        };

        _mockRepository
            .Setup(x => x.GetPageAsync(null, 10, null, null, "user1@example.com", cancellationToken))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("user1@example.com", result.Value.Items[0].UserEmail);
    }

    [Fact]
    public async Task HandleAsync_ValidCursor_DecodesAndUsesCursor()
    {
        // Arrange
        var cursor = KeysetCursorCodec.EncodeLong(100);
        var query = new GetAuditLogsQuery(Cursor: cursor, PageSize: 10, Endpoint: null, Method: null, UserEmail: null);
        var cancellationToken = CancellationToken.None;

        var auditLogs = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                Id = 99,
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user@example.com",
                HttpMethod = "GET",
                Endpoint = "/api/test",
                StatusCode = 200,
                DurationMs = 100,
                TraceId = "trace-99"
            }
        };

        _mockRepository
            .Setup(x => x.GetPageAsync(100, 10, null, null, null, cancellationToken))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Items);

        _mockRepository.Verify(
            x => x.GetPageAsync(100, 10, null, null, null, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidCursor_ReturnsValidationError()
    {
        // Arrange
        var query = new GetAuditLogsQuery(Cursor: "invalid-cursor", PageSize: 10, Endpoint: null, Method: null, UserEmail: null);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Error?.StatusCode);
        Assert.Contains("cursor", result.Error?.Message ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_EmptyPageSize_UsesDefault()
    {
        // Arrange
        var query = new GetAuditLogsQuery(Cursor: null, PageSize: null, Endpoint: null, Method: null, UserEmail: null);
        var cancellationToken = CancellationToken.None;

        var auditLogs = new List<AuditLogEntry>();

        _mockRepository
            .Setup(x => x.GetPageAsync(null, It.IsAny<int>(), null, null, null, cancellationToken))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task HandleAsync_MultipleFilters_AppliesAllFilters()
    {
        // Arrange
        var query = new GetAuditLogsQuery(
            Cursor: null,
            PageSize: 10,
            Endpoint: "/api/users",
            Method: "POST",
            UserEmail: "user1@example.com");
        var cancellationToken = CancellationToken.None;

        var auditLogs = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                Id = 5,
                OccurredAtUtc = DateTime.UtcNow,
                UserEmail = "user1@example.com",
                HttpMethod = "POST",
                Endpoint = "/api/users",
                StatusCode = 201,
                DurationMs = 300,
                TraceId = "trace-5"
            }
        };

        _mockRepository
            .Setup(x => x.GetPageAsync(null, 10, "/api/users", "POST", "user1@example.com", cancellationToken))
            .ReturnsAsync(auditLogs);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("/api/users", result.Value.Items[0].Endpoint);
        Assert.Equal("POST", result.Value.Items[0].HttpMethod);
        Assert.Equal("user1@example.com", result.Value.Items[0].UserEmail);

        _mockRepository.Verify(
            x => x.GetPageAsync(null, 10, "/api/users", "POST", "user1@example.com", cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NoLogs_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetAuditLogsQuery(Cursor: null, PageSize: 10, Endpoint: null, Method: null, UserEmail: null);
        var cancellationToken = CancellationToken.None;

        var emptyList = new List<AuditLogEntry>();

        _mockRepository
            .Setup(x => x.GetPageAsync(null, 10, null, null, null, cancellationToken))
            .ReturnsAsync(emptyList);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
    }
}









