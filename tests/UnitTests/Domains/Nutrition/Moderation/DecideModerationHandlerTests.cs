using FluentValidation;
using Microsoft.Extensions.Options;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Notifications.SendEmailTemplate;
using ShapeUp.Features.Nutrition.Moderation.DecideModeration;
using ShapeUp.Features.Nutrition.Moderation.Shared.Options;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;
using ShapeUp.Features.Notifications.Shared.Abstractions;

namespace UnitTests.Domains.Nutrition.Moderation;

public class DecideModerationHandlerTests
{
    private readonly Mock<IFoodModerationRepository> _moderationRepository = new();
    private readonly Mock<IFoodRepository> _foodRepository = new();
    private readonly Mock<IFoodOverrideRepository> _overrideRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly DecideModerationHandler _handler;

    public DecideModerationHandlerTests()
    {
        var emailSender = new Mock<IEmailNotificationSender>();
        var sendEmailHandler = new SendEmailTemplateHandler(emailSender.Object, new SendEmailTemplateValidator());

        _handler = new DecideModerationHandler(
            _moderationRepository.Object,
            _foodRepository.Object,
            _overrideRepository.Object,
            _userRepository.Object,
            sendEmailHandler,
            Options.Create(new NutritionModerationEmailOptions()),
            new DecideModerationCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_WhenRequestAlreadyDecided_ReturnsConflict()
    {
        const string requestId = "request-1";
        _moderationRepository
            .Setup(x => x.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FoodModerationRequestDocument
            {
                Id = requestId,
                FoodId = "food-1",
                FoodOverrideId = "override-1",
                RequestedByUserId = 7,
                Status = "Approved",
                CreatedAtUtc = DateTime.UtcNow
            });

        var result = await _handler.HandleAsync(
            new DecideModerationCommand(requestId, "Approved"),
            99,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error!.Code);
        _moderationRepository.Verify(
            x => x.DecideAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDecideAsyncReturnsFalse_ReturnsConflict()
    {
        const string requestId = "request-2";
        SetupPendingRequest(requestId);

        _moderationRepository
            .Setup(x => x.DecideAsync(requestId, "Rejected", It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.HandleAsync(
            new DecideModerationCommand(requestId, "Rejected"),
            99,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error!.Code);
    }

    [Fact]
    public async Task HandleAsync_WhenApproved_UnifiesPublicFoodAndClearsOverride()
    {
        const string requestId = "request-3";
        SetupPendingRequest(requestId);

        _moderationRepository
            .Setup(x => x.DecideAsync(requestId, "Approved", It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(
            new DecideModerationCommand(requestId, "Approved"),
            99,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Approved", result.Value!.Status);

        _foodRepository.Verify(
            x => x.ApplyApprovedOverrideAsync(
                It.Is<FoodOverrideDocument>(o => o.Id == "override-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _overrideRepository.Verify(x => x.DeleteAsync("override-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRejected_KeepsOverrideAndDoesNotUpdatePublicFood()
    {
        const string requestId = "request-4";
        SetupPendingRequest(requestId);

        _moderationRepository
            .Setup(x => x.DecideAsync(requestId, "Rejected", It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.HandleAsync(
            new DecideModerationCommand(requestId, "Rejected"),
            99,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rejected", result.Value!.Status);

        _foodRepository.Verify(
            x => x.ApplyApprovedOverrideAsync(It.IsAny<FoodOverrideDocument>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _overrideRepository.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupPendingRequest(string requestId)
    {
        _moderationRepository
            .Setup(x => x.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FoodModerationRequestDocument
            {
                Id = requestId,
                FoodId = "food-1",
                FoodOverrideId = "override-1",
                RequestedByUserId = 7,
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            });

        _overrideRepository
            .Setup(x => x.GetByIdAsync("override-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FoodOverrideDocument
            {
                Id = "override-1",
                FoodId = "food-1",
                UserId = 7,
                MacrosPer100 = new MacroValueObject { Kcal = 120, ProteinG = 10, CarbG = 15, FatG = 4 },
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
    }
}
