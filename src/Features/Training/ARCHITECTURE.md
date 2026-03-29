# Training Domain - Architecture & Implementation

## Domain Scope

The `Training` domain manages catalog, planning, templating, and workout execution.

Responsibilities:
- Maintain exercise catalog (CRUD + suggestion) in SQL Server.
- Maintain equipment catalog (CRUD) in SQL Server.
- Create workout plans before execution (trainer/self flows).
- Create reusable workout templates and assign them to users as plans.
- Execute plans through session lifecycle (`start` -> `state updates` -> `finish`).
- Support extra sets (`isExtra`) beyond planned prescription during execution.
- Compute dashboard metrics (weekly volume, streak, completion rate, PRs, weekly progression).

## Persistence Model

### SQL Server (`TrainingDbContext`)
- `Exercises`
- `ExerciseSteps`
- `ExerciseMuscleProfiles`
- `Equipments`
- `ExerciseEquipments`

### MongoDB
- `workout_plans` (`WorkoutPlanDocument`)
- `workout_templates` (`WorkoutTemplateDocument`)
- `workout_sessions` (`WorkoutSessionDocument`)

## Endpoints

### Exercises
- `GET /api/training/exercises`
- `GET /api/training/exercises/{exerciseId}`
- `POST /api/training/exercises`
- `PUT /api/training/exercises/{exerciseId}`
- `DELETE /api/training/exercises/{exerciseId}`
- `POST /api/training/exercises/suggest`

### Equipments
- `GET /api/training/equipments`
- `GET /api/training/equipments/{equipmentId}`
- `POST /api/training/equipments`
- `PUT /api/training/equipments/{equipmentId}`
- `DELETE /api/training/equipments/{equipmentId}`

### Workout Plans
- `POST /api/training/workout-plans`
- `POST /api/training/workout-plans/{planId}/copy`
- `GET /api/training/workout-plans/{planId}`
- `GET /api/training/workout-plans/user/{targetUserId}`

### Workout Templates
- `POST /api/training/workout-templates`
- `POST /api/training/workout-templates/{templateId}/copy`
- `POST /api/training/workout-templates/{templateId}/assign/{targetUserId}`
- `GET /api/training/workout-templates`
- `GET /api/training/workout-templates/{templateId}`

### Workout Executions
- `POST /api/training/workouts/start`
- `PUT /api/training/workouts/{sessionId}/state`
- `POST /api/training/workouts/{sessionId}/finish`
- `GET /api/training/workouts/{sessionId}`
- `GET /api/training/workouts/user/{targetUserId}`

### Dashboard
- `GET /api/training/dashboard/me?sessionsTargetPerWeek=4`

## Authorization Rules

- Plan/template routes have dedicated scopes for create/read/copy/assign actions.
- Execution routes require lifecycle scopes (`start`, `update`, `finish`).
- Trainer/self target validation uses `ITrainingAccessPolicy`.

## Flow

1. Request passes authorization middleware and scope filter.
2. Controller delegates to command/query handler.
3. Handler validates input with FluentValidation.
4. Access policy validates actor-target permission for plan assignment/execution.
5. Catalog read/write goes to SQL Server repositories.
6. Plan/template/session read/write goes to MongoDB repositories.
7. Execution state updates persist `LastSavedAtUtc` + extra sets (`isExtra`).
8. Finish computes PRs and stores them in session document.

## ASCII Diagram

```text
┌──────────────────────────────┐
│          API Client          │
└──────────────┬───────────────┘
               │
               ▼
┌───────────────────────────────────────────────────────────────┐
│ Controllers (Catalog / Plans / Templates / Executions)       │
└──────────────┬────────────────────────────────────────────────┘
               │ CQRS + Result<T>
               ▼
┌──────────────────────────────┐      ┌─────────────────────────┐
│ Training Handlers + Policy   │─────▶│ GymManagementDbContext  │
│ (Validation + Authorization) │      │ (relationship checks)   │
└──────────────┬───────────────┘      └─────────────────────────┘
               │
      ┌────────┴────────┐
      ▼                 ▼
┌───────────────┐   ┌──────────────────────────────────────────┐
│ SQL Server    │   │ MongoDB                                  │
│ Exercises/... │   │ workout_plans / workout_templates /      │
└───────────────┘   │ workout_sessions                         │
                    └──────────────────────────────────────────┘
```
