using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Relationships.Infrastructure.Repositories;
using ShapeUp.Features.Relationships.Shared.Data;
using ShapeUp.Features.Relationships.Shared.Entities;

namespace UnitTests.Domains.Relationships;

public class ProfessionalClientRelationshipRepositoryTests
{
    private readonly RelationshipsDbContext _context;
    private readonly ProfessionalClientRelationshipRepository _repository;

    public ProfessionalClientRelationshipRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<RelationshipsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new RelationshipsDbContext(options);
        _repository = new ProfessionalClientRelationshipRepository(_context);
    }

    private static ProfessionalClientRelationship BuildRelationship(
        int professionalUserId = 1,
        int clientUserId = 2,
        string relationshipType = "Training",
        RelationshipStatus status = RelationshipStatus.Active,
        DateTime? endedAt = null) => new()
    {
        ProfessionalUserId = professionalUserId,
        ClientUserId = clientUserId,
        RelationshipType = relationshipType,
        Status = status,
        StartedAt = DateTime.UtcNow.AddDays(-10),
        EndedAt = endedAt
    };

    [Fact]
    public async Task GetActiveAsync_ActiveRelationshipExists_ReturnsIt()
    {
        var relationship = BuildRelationship();
        await _context.AddAsync(relationship);
        await _context.SaveChangesAsync();

        var result = await _repository.GetActiveAsync(1, 2, "Training", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(RelationshipStatus.Active, result.Status);
    }

    [Fact]
    public async Task GetActiveAsync_RelationshipEnded_ReturnsNull()
    {
        var relationship = BuildRelationship(status: RelationshipStatus.Ended, endedAt: DateTime.UtcNow.AddDays(-1));
        await _context.AddAsync(relationship);
        await _context.SaveChangesAsync();

        var result = await _repository.GetActiveAsync(1, 2, "Training", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveAsync_DifferentRelationshipType_ReturnsNull()
    {
        var relationship = BuildRelationship(relationshipType: "Nutrition");
        await _context.AddAsync(relationship);
        await _context.SaveChangesAsync();

        var result = await _repository.GetActiveAsync(1, 2, "Training", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveAsync_DifferentClientUser_ReturnsNull()
    {
        var relationship = BuildRelationship(clientUserId: 99);
        await _context.AddAsync(relationship);
        await _context.SaveChangesAsync();

        var result = await _repository.GetActiveAsync(1, 2, "Training", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveAsync_NoRelationshipAtAll_ReturnsNull()
    {
        var result = await _repository.GetActiveAsync(1, 2, "Training", CancellationToken.None);

        Assert.Null(result);
    }
}
