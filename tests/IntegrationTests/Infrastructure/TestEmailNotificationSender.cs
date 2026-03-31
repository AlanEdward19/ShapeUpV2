using System.Collections.Concurrent;
using ShapeUp.Features.Notifications.Shared.Abstractions;
using ShapeUp.Features.Notifications.Shared.Models;
using ShapeUp.Shared.Results;

namespace IntegrationTests.Infrastructure;

public sealed class TestEmailNotificationSender : IEmailNotificationSender
{
    private readonly ConcurrentQueue<SentEmailRecord> _messages = new();

    public Task<Result<EmailDispatchReceipt>> SendHtmlAsync(SendHtmlEmailRequest request, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString("N");
        _messages.Enqueue(new SentEmailRecord(messageId, request.To, request.Subject, request.Html, null, new Dictionary<string, object?>()));
        return Task.FromResult(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt(messageId)));
    }

    public Task<Result<EmailDispatchReceipt>> SendTemplateAsync(SendTemplateEmailRequest request, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString("N");
        _messages.Enqueue(new SentEmailRecord(messageId, request.To, request.Subject, null, request.TemplateId, new Dictionary<string, object?>(request.Variables)));
        return Task.FromResult(Result<EmailDispatchReceipt>.Success(new EmailDispatchReceipt(messageId)));
    }

    public IReadOnlyList<SentEmailRecord> Snapshot() => _messages.ToArray();

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }
}

public sealed record SentEmailRecord(
    string ProviderMessageId,
    string To,
    string Subject,
    string? Html,
    string? TemplateId,
    IReadOnlyDictionary<string, object?> Variables);

