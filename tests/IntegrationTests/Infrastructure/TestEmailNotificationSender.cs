using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Features.PlatformFeatureFlags.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace IntegrationTests.Infrastructure;

public sealed class TestEmailNotificationSender(IServiceScopeFactory scopeFactory) : IEmailNotificationSender
{
    private const string EmailEnabledFeatureFlagKey = "notifications.email-enabled";
    private readonly ConcurrentQueue<SentEmailRecord> _messages = new();

    public async Task<Result<EmailDispatchReceipt>> SendHtmlAsync(SendHtmlEmailRequest request, CancellationToken cancellationToken)
    {
        if (!await IsEmailEnabledAsync(cancellationToken))
            return Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt(string.Empty));

        var messageId = Guid.NewGuid().ToString("N");
        _messages.Enqueue(new SentEmailRecord(messageId, request.To, request.Subject, request.Html, null, new Dictionary<string, object?>()));
        return Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt(messageId));
    }

    public async Task<Result<EmailDispatchReceipt>> SendTemplateAsync(SendTemplateEmailRequest request, CancellationToken cancellationToken)
    {
        if (!await IsEmailEnabledAsync(cancellationToken))
            return Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt(string.Empty));

        var messageId = Guid.NewGuid().ToString("N");
        _messages.Enqueue(new SentEmailRecord(messageId, request.To, request.Subject, null, request.TemplateId, new Dictionary<string, object?>(request.Variables)));
        return Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt(messageId));
    }

    public IReadOnlyList<SentEmailRecord> Snapshot() => _messages.ToArray();

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }

    private async Task<bool> IsEmailEnabledAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var featureFlagReader = scope.ServiceProvider.GetRequiredService<IFeatureFlagReader>();
        return await featureFlagReader.IsEnabledAsync(EmailEnabledFeatureFlagKey, cancellationToken);
    }
}

public sealed record SentEmailRecord(
    string ProviderMessageId,
    string To,
    string Subject,
    string? Html,
    string? TemplateId,
    IReadOnlyDictionary<string, object?> Variables);
