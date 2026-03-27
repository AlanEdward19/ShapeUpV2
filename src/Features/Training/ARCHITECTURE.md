# Training Domain - Architecture & Implementation

## Domain Scope

The `Training` domain manages the training catalog and workout execution lifecycle.

Responsibilities:
- Maintain exercise catalog (CRUD + suggestion) in SQL Server.
- Maintain equipment catalog (CRUD) in SQL Server.
- Record workout executions (sessions, exercises, sets, RPE, rest, duration) in MongoDB.
- Enforce role/scope-based creation rules for workout sessions.
- Compute dashboard metrics (weekly volume, streak, completion rate, PRs, weekly progression).

## Persistence Model

### SQL Server (`TrainingDbContext`)
- `Exercises`
- `ExerciseSteps`
- `ExerciseMuscleProfiles`
- `Equipments`
- `ExerciseEquipments`

### MongoDB (`workout_sessions` collection)
- `WorkoutSessionDocument`
  - session metadata
  - executed exercises + sets
  - generated PR events

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

### Workouts
- `POST /api/training/workouts`
- `POST /api/training/workouts/{sessionId}/complete`
- `GET /api/training/workouts/{sessionId}`
- `GET /api/training/workouts/user/{targetUserId}`

### Dashboard
- `GET /api/training/dashboard/me?sessionsTargetPerWeek=4`

## Authorization Rules

Workout creation uses composed permissions:
- Client can create for self with `training:workouts:create:self`.
- Trainer can create for linked trainer-client relation with `training:workouts:create:trainer`.
- Gym trainer staff can create for clients from same gym with `training:workouts:create:gym_staff`.
- Multi-role users accumulate all allowed paths.

## Flow

1. Request passes `AuthorizationMiddleware` and scope filter.
2. Controller delegates to command/query handler.
3. Handler validates input with FluentValidation.
4. For workout creation, `ITrainingAccessPolicy` validates actor-target permission.
5. Catalog read/write goes to SQL Server repositories.
6. Workout execution read/write goes to MongoDB repository.
7. Completion computes PRs and stores them in session document.
8. Dashboard reads weekly ranges and computes metrics in backend.

## ASCII Diagram

```text
┌──────────────────────────────┐
│          API Client          │
└──────────────┬───────────────┘
               │
               ▼
┌───────────────────────────────────────────────────────────────┐
│ Controllers (Exercises / Equipments / Workouts / Dashboard)  │
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
┌───────────────┐   ┌──────────────────┐
│ SQL Server    │   │ MongoDB          │
│ Exercises/... │   │ workout_sessions │
└───────────────┘   └──────────────────┘
```

