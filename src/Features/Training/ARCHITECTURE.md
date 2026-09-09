# Training Domain - Architecture & Implementation

## Domain Scope

The `Training` domain manages catalog, planning, templating, and workout execution.

Responsibilities:
- Maintain exercise catalog (CRUD + suggestion) in SQL Server.
- Maintain equipment catalog (CRUD) in SQL Server.
- Create workout plans before execution (trainer/self flows).
- Create reusable workout templates and assign them to users as plans.
- Track user target weight and daily weight registers.
- Start workout sessions from existing workout plans and execute them through the lifecycle (`start` -> `state updates` -> `finish`).
- Allow cancellation of running workout sessions and query current active execution for the logged-in user.
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
- `weight_targets` (`WeightTargetDocument`)
- `weight_registers` (`WeightRegisterDocument`)

## Planning Block Model (workout-editor)

`WorkoutPlanDocument`/`WorkoutTemplateDocument` structure their prescription as `Blocks: List<BlockDocumentValueObject>` — the structural unit a professional builds a plan/template out of, not a flat list of exercises.

- **`BlockType`**: `Straight` (one exercise, sets performed in sequence — the default/simple case), `Superset` (2+ exercises performed back-to-back with no rest between them), `Amrap` (fixed time-cap, as-many-rounds/reps-as-possible), `Emom` (fixed interval + total rounds, one or more exercises rotated per round).
- Each `Block` contains 1+ `BlockExerciseDocumentValueObject`, each with its own list of `Set` (`PlannedSetDocumentValueObject`).
- **Validation** (`CreateWorkoutPlanCommandValidator`/`UpdateWorkoutPlanCommandValidator` and the Template equivalents): `Superset` requires ≥2 exercises; `Amrap` requires `TimeCapSeconds > 0`; `Emom` requires `IntervalSeconds > 0` and `TotalRounds > 0`; `RestSeconds` on any set is rejected unless the parent block is `Straight` (rest between grouped exercises/rounds is governed by the block's own timing fields, not per-set).
- **Intensity is exclusive**: `Set.Intensity` is a single nullable object `{ Type: Rpe|Rir, Value }` (never two separate fields) — a set can be scored in RPE, in RIR, or left unscored, never both. `Set.Repetitions`/`Set.RestSeconds` are nullable too (an AMRAP set may have no fixed rep target; a non-Straight block's sets have no per-set rest).
- **Execution stays flat** (`WorkoutSessionDocument.Exercises`, `ExecutedExerciseDocumentValueObject`/`ExecutedSetDocumentValueObject`) — no `Block` wrapper there. `StartWorkoutExecutionHandler` flattens a plan's blocks (`plan.Blocks.SelectMany(b => b.Exercises)`) when seeding a session. This is a deliberate architectural decision (see `.specs/STATE.md` AD-007): Block is how a professional *authors* a plan; what was actually *executed* is a separate concern, decided independently by whatever feature models workout execution in depth. `ExecutedSetDocumentValueObject`/`ExecutedSetValueObject` do reuse the same `Intensity` shape (Rpe/Rir), since intensity scoring applies uniformly to planning and execution.
- Same shape and rules apply symmetrically to `WorkoutTemplateDocument` — `AssignWorkoutTemplateHandler` copies a template's `Blocks` into a new plan unchanged, and `CopyWorkoutPlanHandler`/`CopyWorkoutTemplateHandler` clone `Blocks` the same way.

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
- `POST /api/training/workouts/{sessionId}/cancel`
- `GET /api/training/workouts/{sessionId}`
- `GET /api/training/workouts/user/{targetUserId}`
- `GET /api/training/workouts/me/active`

### Dashboard
- `GET /api/training/dashboard/me?sessionsTargetPerWeek=4`

### Weight Tracking
- `PUT /api/training/weight/target`
- `POST /api/training/weight/registers`
- `GET /api/training/weight/registers?startDateUtc=...&endDateUtc=...`

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
7. Workout sessions are created only when an existing workout plan is started.
8. Execution state updates persist `LastSavedAtUtc` + extra sets (`isExtra`).
9. Finish computes PRs and stores them in session document.
10. Cancel marks the session as cancelled (`IsCancelled`) and closes the execution timestamp window.
11. Active-session query returns whether the logged user still has an open session in progress.
12. Weight register writes are upserted per user/day (`UserId` + `Day` in `yyyy-MM-dd`), so a second submit on the same day updates the existing entry.

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
│ Exercises/... │   │ workout_plans / workout_templates:       │
└───────────────┘   │   Blocks[Straight|Superset|Amrap|Emom]   │
                    │     -> BlockExercise[] -> Set[]           │
                    │ workout_sessions: Exercises[] -> Set[]    │
                    │   (flat, no Block - AD-007)               │
                    └──────────────────────────────────────────┘
```
