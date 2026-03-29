# Workout Plans - Architecture

## Scope

`WorkoutPlans` stores planned workouts before execution. A plan contains target user, metadata, and full exercise/set prescription.

Responsibilities:
- Create a workout plan for self or a permitted client.
- Read plan details and list plans by user with keyset pagination.
- Copy an existing plan into a new editable plan.

## Persistence

MongoDB collection: `workout_plans`
- `Id`
- `TargetUserId`
- `CreatedByUserId`
- `TrainerUserId`
- `Name`, `Notes`
- `CreatedAtUtc`, `UpdatedAtUtc`
- Planned exercises/sets snapshot

## Endpoints

- `POST /api/training/workout-plans`
- `POST /api/training/workout-plans/{planId}/copy`
- `GET /api/training/workout-plans/{planId}`
- `GET /api/training/workout-plans/user/{targetUserId}?cursor&pageSize`

## Flow

1. Controller validates scope and forwards command/query to handler.
2. Handler validates input with FluentValidation.
3. Access policy checks actor-target permission for creation/copy.
4. Repository writes/reads plan document in MongoDB.
5. Handler maps document to response DTO.

```text
Client
  |
  v
WorkoutPlansController
  |
  v
CQRS Handlers + FluentValidation + AccessPolicy
  |
  v
MongoWorkoutPlanRepository (workout_plans)
```

