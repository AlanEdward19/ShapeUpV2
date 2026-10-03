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


## Email templates
O HTML-fonte dos templates do Resend fica em `Templates/` (paleta Warm Oxide, igual ao app mobile/web).

| Alias | Arquivo | Variáveis |
| --- | --- | --- |
| `shapeup-invitation` | `Templates/shapeup-invitation.html` | `register_url`, `trainer_name`, `plan_name` (fallback "Acompanhamento personalizado"), `expires_in` (fallback "24 horas") |

Ao alterar o arquivo, atualize e publique o template correspondente no Resend.
