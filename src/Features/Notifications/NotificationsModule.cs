namespace ShapeUp.Features.Notifications;

using FluentValidation;
using Inbox;
using Infrastructure.Resend;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Resend;
using SendEmailHtml;
using SendEmailTemplate;
using ShapeUp.Shared.Privacy;
using Shared.Abstractions;
using Shared.Options;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions();
        services.AddOptions<ResendEmailOptions>()
            .Bind(configuration.GetSection(ResendEmailOptions.SectionName));

        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = configuration[$"{ResendEmailOptions.SectionName}:ApiToken"] ?? string.Empty;
            options.ThrowExceptions = true;
        });

        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailNotificationSender, ResendEmailNotificationSender>();

        services.AddScoped<SendEmailHtmlHandler>();
        services.AddScoped<IValidator<SendEmailHtmlCommand>, SendEmailHtmlValidator>();
        services.AddScoped<SendEmailTemplateHandler>();
        services.AddScoped<IValidator<SendEmailTemplateCommand>, SendEmailTemplateValidator>();

        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<InboxDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<ListNoticesHandler>();
        services.AddScoped<MarkNoticeReadHandler>();
        services.AddScoped<SendNoticeHandler>();
        services.AddScoped<IValidator<SendNoticeCommand>, SendNoticeValidator>();
        services.AddScoped<IUserDataEraser, InboxEraser>();

        return services;
    }
}

