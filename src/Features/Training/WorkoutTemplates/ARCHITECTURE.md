# Workout Templates - Architecture

## Scope

`WorkoutTemplates` stores reusable workout blueprints created by trainers (or advanced users) without binding to one specific client execution.

Responsibilities:
- Create and list personal templates.
- Copy templates for quick iteration.
- Assign template to a target user by creating a new workout plan snapshot.

## Persistence

MongoDB collection: `workout_templates`
- `Id`
- `CreatedByUserId`
- `Name`, `Notes`
- `CreatedAtUtc`, `UpdatedAtUtc`
- Planned exercises/sets snapshot

Template assignment writes into `workout_plans` as a new plan instance.

## Endpoints

- `POST /api/training/workout-templates`
- `POST /api/training/workout-templates/{templateId}/copy`
- `POST /api/training/workout-templates/{templateId}/assign/{targetUserId}`
- `GET /api/training/workout-templates`
- `GET /api/training/workout-templates/{templateId}`

## Flow

1. Controller enforces template scope.
2. Handler validates payload with FluentValidation.
3. Copy/read is restricted to template owner.
4. Assign checks actor-target permission via `ITrainingAccessPolicy`.
5. Repository persists template and generated plan snapshots in MongoDB.

```text
Client
  |
  v
WorkoutTemplatesController
  |
  v
CQRS Handlers + Validation + AccessPolicy
  |
  +--> MongoWorkoutTemplateRepository (workout_templates)
  |
  +--> MongoWorkoutPlanRepository (assignment output)
```

