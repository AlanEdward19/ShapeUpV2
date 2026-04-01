using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.AcceptTrainerClientInvite;
using ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;
using ShapeUp.Features.GymManagement.TrainerClients.Shared;
using Microsoft.Extensions.Options;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.GymManagement.TrainerClients;

public class TrainerClientInviteHandlerTests
{
    private readonly Mock<ITrainerClientInviteRepository> _inviteRepository = new();
    private readonly Mock<ITrainerClientRepository> _trainerClientRepository = new();
    private readonly Mock<ITrainerPlanRepository> _trainerPlanRepository = new();
    private readonly Mock<IGymClientRepository> _gymClientRepository = new();
    private readonly Mock<IUserPlatformRoleRepository> _roleRepository = new();
    private readonly Mock<IEmailNotificationSender> _emailNotificationSender = new();
    private readonly Mock<ITrainerClientInviteRegisterUrlBuilder> _registerUrlBuilder = new();
    private readonly Mock<ITrainerClientInvitePayloadCodec> _payloadCodec = new();

    [Fact]
    public async Task GenerateInvite_WithoutPlan_ShouldCreateInvitedToken()
    {
        _emailNotificationSender
            .Setup(sender => sender.SendTemplateAsync(It.IsAny<SendTemplateEmailRequest>(), default))
            .ReturnsAsync(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt("provider-message-id")));
        _registerUrlBuilder
            .Setup(builder => builder.BuildRegisterUrl(7, It.IsAny<string>()))
            .Returns("https://shapeup.app/register?payload=encoded");
        _inviteRepository
            .Setup(repo => repo.GetActiveByTrainerAndEmailAsync(7, "client@test.com", default))
            .ReturnsAsync((TrainerClientInvite?)null);
        _inviteRepository
            .Setup(repo => repo.AddAsync(It.IsAny<TrainerClientInvite>(), default))
            .Callback<TrainerClientInvite, CancellationToken>((invite, _) => invite.Id = 123)
            .Returns(Task.CompletedTask);

        var handler = new GenerateTrainerClientInviteHandler(
            _inviteRepository.Object,
            _trainerPlanRepository.Object,
            _emailNotificationSender.Object,
            _registerUrlBuilder.Object,
            Options.Create(new TrainerClientInviteEmailOptions
            {
                TemplateId = "46dbcd80-c134-407d-ad36-2fe01ed0ca89",
                Subject = "Convite para ingressar na ShapeUp"
            }),
            new GenerateTrainerClientInviteValidator());

        var command = new GenerateTrainerClientInviteCommand(null, null);
        command.SetClientEmail("client@test.com");
        command.SetTrainerName("Alan Oliveira");

        var result = await handler.HandleAsync(command, 7, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(123, result.Value!.InviteId);
        Assert.Equal("client@test.com", result.Value.ClientEmail);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.Equal("Invited", result.Value.Status);
        _emailNotificationSender.Verify(sender => sender.SendTemplateAsync(
            It.Is<SendTemplateEmailRequest>(request =>
                request.To == "client@test.com" &&
                request.TemplateId == "46dbcd80-c134-407d-ad36-2fe01ed0ca89" &&
                request.Variables.ContainsKey("register_url") &&
                request.Variables.ContainsKey("trainer_name")), default), Times.Once);
    }

    [Fact]
    public async Task AcceptInvite_ShouldCreateTrainerClient_WhenInviteIsValid()
    {
        var token = "valid-token";
        var payload = "encoded-payload";
        var tokenHash = ShapeUp.Features.GymManagement.Shared.Security.TrainerClientInviteTokenCodec.ComputeHash(token);
        var invite = new TrainerClientInvite
        {
            Id = 77,
            TrainerId = 9,
            InviteeEmail = "client@test.com",
            AccessTokenHash = tokenHash,
            TrainerPlanId = null,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(2),
            Status = TrainerClientInviteStatus.Invited
        };

        _payloadCodec
            .Setup(codec => codec.Decode(payload))
            .Returns(Result<TrainerClientInviteUrlPayload>.Success(new TrainerClientInviteUrlPayload(9, token)));
        _inviteRepository.Setup(repo => repo.GetByTokenHashAsync(tokenHash, default)).ReturnsAsync(invite);
        _trainerClientRepository.Setup(repo => repo.GetByClientIdAsync(300, default)).ReturnsAsync((TrainerClient?)null);
        _gymClientRepository.Setup(repo => repo.GetByUserIdAsync(300, default)).ReturnsAsync((GymClient?)null);
        _trainerClientRepository
            .Setup(repo => repo.AddAsync(It.IsAny<TrainerClient>(), default))
            .Callback<TrainerClient, CancellationToken>((client, _) => client.Id = 555)
            .Returns(Task.CompletedTask);
        _inviteRepository.Setup(repo => repo.UpdateAsync(invite, default)).Returns(Task.CompletedTask);
        _roleRepository.Setup(repo => repo.GetByUserIdAndRoleAsync(300, It.IsAny<PlatformRoleType>(), default)).ReturnsAsync((UserPlatformRole?)null);
        _roleRepository.Setup(repo => repo.AddAsync(It.IsAny<UserPlatformRole>(), default)).Returns(Task.CompletedTask);
        _roleRepository.Setup(repo => repo.DeleteAsync(It.IsAny<int>(), default)).Returns(Task.CompletedTask);

        var handler = new AcceptTrainerClientInviteHandler(
            _inviteRepository.Object,
            _trainerClientRepository.Object,
            _trainerPlanRepository.Object,
            _gymClientRepository.Object,
            _roleRepository.Object,
            _payloadCodec.Object,
            new AcceptTrainerClientInviteValidator());

        var result = await handler.HandleAsync(new AcceptTrainerClientInviteCommand(payload), 300, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(555, result.Value!.TrainerClientId);
        Assert.Equal(9, result.Value.TrainerId);
        Assert.Equal(300, result.Value.ClientId);
        Assert.Null(result.Value.TrainerPlanId);
        Assert.Equal(TrainerClientInviteStatus.Accepted, invite.Status);
        Assert.Equal(300, invite.AcceptedByUserId);
        Assert.NotNull(invite.AcceptedAtUtc);
        _payloadCodec.Verify(codec => codec.Decode(payload), Times.Once);
    }
}

