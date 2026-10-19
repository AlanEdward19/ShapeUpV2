# Safety - Architecture

User-generated content rules for the App Store (guideline 1.2) and basic abuse handling.

- `POST /api/safety/reports` `{ targetType, reason, targetId?, targetUserId?, details? }` - any user. Reasons: Harassment,
  Inappropriate, Spam, Impersonation, Other. Cannot report yourself; 30 reports per day per user.
- `GET /api/safety/reports?status=Open|Resolved|Dismissed&cursor&pageSize` and `POST /api/safety/reports/{id}/resolve`
  `{ decision: Resolved|Dismissed, note? }` - platform admin (`capability:platform.safety.review`). Oldest first.
- `POST|DELETE /api/safety/blocks/{userId}`, `GET /api/safety/blocks`. Blocking ends the trainer and nutrition links between the two
  (both directions) and `IUserBlockLookup` makes invite acceptance refuse a blocked pair (403).
- `ObjectionableText` (`Shared/Safety`) filters nutritionist diary comments (add/edit); the mobile app applies the same list to chat.

Tables (`SafetyDbContext`): `UserBlocks` (unique blocker+blocked), `ContentReports`. Account deletion removes the user's blocks and
open reports and anonymizes the rest (`SafetyUserDataEraser`).
