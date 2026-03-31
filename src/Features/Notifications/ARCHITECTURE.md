# Notifications Domain - Architecture & Implementation

## Domain Scope

The `Notifications` domain is responsible for outbound notification delivery starting with email transport through Resend.

Core responsibilities:
- Validate outbound email commands.
- Send raw HTML emails through Resend.
- Send emails using a published Resend `templateId` and template variables.
- Encapsulate provider-specific failures behind the shared `Result` pattern.
- Expose authenticated API endpoints for application-triggered notification dispatch.

## Domain Structure

```text
Features/Notifications/
├── Shared/
│   ├── Abstractions/
│   │   └── IEmailNotificationSender.cs
│   ├── Errors/
│   │   └── NotificationErrors.cs
│   ├── Helpers/
│   │   └── NotificationTemplateVariablesConverter.cs
│   ├── Models/
│   │   ├── EmailDispatchReceipt.cs
│   │   ├── SendHtmlEmailRequest.cs
│   │   └── SendTemplateEmailRequest.cs
│   └── Options/
│       └── ResendEmailOptions.cs
├── Infrastructure/
│   └── Resend/
│       └── ResendEmailNotificationSender.cs
├── SendEmailHtml/
│   ├── SendEmailHtmlCommand.cs
│   ├── SendEmailHtmlResponse.cs
│   ├── SendEmailHtmlValidator.cs
│   └── SendEmailHtmlHandler.cs
├── SendEmailTemplate/
│   ├── SendEmailTemplateCommand.cs
│   ├── SendEmailTemplateResponse.cs
│   ├── SendEmailTemplateValidator.cs
│   └── SendEmailTemplateHandler.cs
├── NotificationsController.cs
├── NotificationsModule.cs
├── README.md
└── ARCHITECTURE.md
```

## Database Structure

This domain does not persist notification state yet.

### Current persistence model
- No local tables.
- Delivery is delegated directly to Resend.
- Runtime configuration is loaded from `Notifications:Resend` in application configuration.

## Endpoints

- `POST /api/notifications/emails/send-html`
  - Requires scope `notifications:emails:send_html`
  - Body contains `to`, `subject`, `html`
  - Sends an HTML email through Resend.

- `POST /api/notifications/emails/send-template`
  - Requires scope `notifications:emails:send_template`
  - Body contains `to`, `subject`, `templateId`, `variables`
  - Sends an email based on a published Resend template.

## End-to-End Flow

1. Client calls a notifications endpoint with a Firebase bearer token.
2. `AuthorizationMiddleware` resolves the authenticated user and effective scopes.
3. `RequireScopesAttribute` validates the required notification scope.
4. Controller delegates the payload to the corresponding command handler.
5. Handler runs `FluentValidation` asynchronously with the request `CancellationToken`.
6. Handler maps the command into a provider-agnostic request model.
7. `IEmailNotificationSender` uses Resend to submit the email.
8. Provider result is mapped to `Result<T>` and returned as HTTP `202 Accepted` on success.

## Provider Configuration

```text
Notifications:Resend
├── ApiToken    -> Resend API token
├── FromEmail   -> Verified sender address/domain
├── FromName    -> Optional display name
└── ReplyTo     -> Optional reply-to address
```

## Operational Notes

- Provider credentials should be supplied from user-secrets or environment variables in non-local environments.
- HTML emails and template emails share the same sender configuration.
- Template variables are accepted as JSON and converted to CLR primitives/objects before being passed to Resend.
- Provider validation or transport failures are returned using structured `Result` errors instead of bubbling raw exceptions to controllers.

## ASCII Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│                         CLIENT APPLICATION                      │
│                 (Backoffice / API consumer / job)               │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ Authenticated HTTP request
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ASP.NET CORE API                             │
├─────────────────────────────────────────────────────────────────┤
│ AuthorizationMiddleware                                         │
│ 1) Validate bearer token                                        │
│ 2) Resolve user context + scopes                                │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ RequireScopesAttribute                                          │
│ Validate notifications scope                                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │ authorized
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ NotificationsController                                         │
│  ├─ SendHtml                                                    │
│  └─ SendTemplate                                                │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Command Handler + FluentValidation + Result Pattern             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ IEmailNotificationSender -> ResendEmailNotificationSender       │
│ Build EmailMessage / Template payload                           │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                         RESEND API                              │
│            Verified sender domain + provider delivery           │
└─────────────────────────────────────────────────────────────────┘
```

