using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Credentials.Infrastructure.Repositories;
using ShapeUp.Features.Credentials.Shared.Data;
using ShapeUp.Features.Credentials.Shared.Entities;

namespace UnitTests.Domains.Credentials;

public class ProfessionalCredentialRepositoryTests
{
    private readonly CredentialsDbContext _context;
    private readonly ProfessionalCredentialRepository _repository;
    private static readonly DateTime NowUtc = new(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);

    public ProfessionalCredentialRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CredentialsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new CredentialsDbContext(options);
        _repository = new ProfessionalCredentialRepository(_context);
    }

    private static ProfessionalCredential BuildCredential(
        int userId = 1,
        string professionType = "PersonalTrainer",
        CredentialStatus status = CredentialStatus.Verified,
        DateTime? expiresAt = null) => new()
    {
        UserId = userId,
        ProfessionType = professionType,
        CredentialNumber = "CREF-000123",
        IssuingAuthority = "CREF",
        IssuingRegion = "SP",
        Country = "BR",
        Status = status,
        VerifiedAt = status == CredentialStatus.Verified ? NowUtc.AddDays(-30) : null,
        ExpiresAt = expiresAt
    };

    [Fact]
    public async Task GetVerifiedAsync_VerifiedNotExpired_ReturnsCredential()
    {
        var credential = BuildCredential(expiresAt: NowUtc.AddDays(30));
        await _context.AddAsync(credential);
        await _context.SaveChangesAsync();

        var result = await _repository.GetVerifiedAsync(1, "PersonalTrainer", NowUtc, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CredentialStatus.Verified, result.Status);
    }

    [Fact]
    public async Task GetVerifiedAsync_VerifiedWithNoExpiry_ReturnsCredential()
    {
        var credential = BuildCredential(expiresAt: null);
        await _context.AddAsync(credential);
        await _context.SaveChangesAsync();

        var result = await _repository.GetVerifiedAsync(1, "PersonalTrainer", NowUtc, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetVerifiedAsync_StatusNotVerified_ReturnsNull()
    {
        var credential = BuildCredential(status: CredentialStatus.UnderReview);
        await _context.AddAsync(credential);
        await _context.SaveChangesAsync();

        var result = await _repository.GetVerifiedAsync(1, "PersonalTrainer", NowUtc, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetVerifiedAsync_VerifiedButExpired_ReturnsNull()
    {
        var credential = BuildCredential(expiresAt: NowUtc.AddDays(-1));
        await _context.AddAsync(credential);
        await _context.SaveChangesAsync();

        var result = await _repository.GetVerifiedAsync(1, "PersonalTrainer", NowUtc, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetVerifiedAsync_DifferentProfessionType_ReturnsNull()
    {
        var credential = BuildCredential(professionType: "Nutritionist", expiresAt: NowUtc.AddDays(30));
        await _context.AddAsync(credential);
        await _context.SaveChangesAsync();

        var result = await _repository.GetVerifiedAsync(1, "PersonalTrainer", NowUtc, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetVerifiedAsync_DifferentUser_ReturnsNull()
    {
        var credential = BuildCredential(userId: 2, expiresAt: NowUtc.AddDays(30));
        await _context.AddAsync(credential);
        await _context.SaveChangesAsync();

        var result = await _repository.GetVerifiedAsync(1, "PersonalTrainer", NowUtc, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetVerifiedAsync_NoCredentialAtAll_ReturnsNull()
    {
        var result = await _repository.GetVerifiedAsync(1, "PersonalTrainer", NowUtc, CancellationToken.None);

        Assert.Null(result);
    }
}
