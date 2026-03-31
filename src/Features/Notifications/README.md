# Notifications Domain

The `Notifications` domain is responsible for outbound email delivery.

## Available capabilities
- Send email with raw HTML payload.
- Send email using a published Resend `templateId` plus variables.

## Configuration
Configure the provider using `Notifications:Resend`:

- `ApiToken`
- `FromEmail`
- `FromName`
- `ReplyTo` (optional)

## Endpoints
- `POST /api/notifications/emails/send-html`
- `POST /api/notifications/emails/send-template`

Both endpoints require an authenticated user context and the corresponding authorization scopes.

