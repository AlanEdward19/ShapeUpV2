# Nutrition Domain - Architecture & Implementation

## Domain Scope

The `Nutrition` domain covers food catalog management, personal food overrides, moderation,
nutrition profiles, daily diary logging, meal plans, weight tracking, and end-of-day goal
evaluation that feeds gamification streaks.

Core responsibilities:
- Maintain a public food catalog (`Food`) and personal overrides (`FoodOverride`) in MongoDB.
- Queue and decide food moderation requests when a user edits a public food.
- Store high-frequency diary writes and nutrition profiles in SQL Server (`NutritionDbContext`).
- Compute TDEE-based or manual macro goals during onboarding.
- Support meal-plan creation/activation and diary item substitution.
- Evaluate whether daily macro goals were met at UTC day cutoff via a MassTransit recurring job.
- Publish `NutritionGoalMet` (no outbox — AD-012) for downstream gamification consumers.
- Host migrated `WeightTracking` endpoints under `/api/nutrition/weight/*`.

> Persistence splits by change frequency inside one domain (AD-013): low-write catalog and
> meal-plan documents live in Mongo; high-write diary/profile/evaluation data lives in SQL.

## Domain Structure

```text
Features/Nutrition/
├── Shared/
│   ├── Documents/          # Mongo (Food, FoodOverride, FoodModerationRequest, MealPlan, Weight*)
│   ├── Entities/           # SQL (NutritionProfile, DiaryDay/Entry, NutritionGoalEvaluation, Weight*)
│   ├── Abstractions/       # IFoodRepository, IMealPlanRepository, ...
│   ├── Errors/
│   └── ValueObjects/       # MacroValueObject
├── Infrastructure/
│   ├── Data/               # NutritionDbContext (SQL Server)
│   └── Mongo/              # Mongo repositories
├── Foods/                  # Create, search, barcode, override, active version, delete
├── Moderation/             # Pending queue + approve/reject (email via Notifications)
├── Profile/                # Onboarding, manual goal, get profile
├── Diary/                  # Add/remove entries, get day, suggest/substitute
├── MealPlans/              # Create + activate
├── WeightTracking/         # Target + daily registers (migrated from Training)
├── GoalEvaluation/         # Recurring job consumer + NutritionGoalMet publisher
├── NutritionModule.cs
└── ARCHITECTURE.md
```

## Database Structure

### SQL Server (`NutritionDbContext`)

| Table | Purpose |
|---|---|
| `NutritionProfiles` | Anthropometrics + owned `ActiveGoal` macros (PK: `UserId`) |
| `NutritionWeightTargets` | Current weight target per user |
| `NutritionWeightRegisters` | Daily weight log (unique per user+date) |
| `NutritionDiaryDays` | One row per user per calendar date |
| `NutritionDiaryEntries` | Diary items (client-generated id, meal slot, food ref, macros) |
| `NutritionGoalEvaluations` | Idempotent audit that a day was evaluated |

`DiaryEntry.FoodId` references Mongo `Food`/`FoodOverride` documents without FK (cross-store
convention, same spirit as `WorkoutEvaluation.SessionId` in Gamification).

### MongoDB (`Mongo__Nutrition__ConnectionString`)

| Collection | Purpose |
|---|---|
| `foods` | Public catalog (sparse unique barcode index) |
| `food_overrides` | Personal food versions pending or active |
| `food_moderation_requests` | Admin moderation queue |
| `meal_plans` | Fixed meal-plan documents with nested items |
| `weight_targets` / `weight_registers` | Legacy Mongo weight data (repository layer) |

Mongo collections in this domain do not publish domain events.

## Endpoints

### Foods (`/api/nutrition/foods`)
- `POST` — create public food (authenticated user).
- `GET` — keyset search (`cursor`, `pageSize`, `q`).
- `GET barcode/{barcode}` — lookup by barcode (resolves override when active).
- `POST {foodId}/override` — create personal override (queues moderation).
- `PUT {foodId}/active-version` — switch active public vs override version.
- `DELETE {foodId}` — soft-delete public food (`capability:platform.nutrition_foods.moderate`).

### Food moderation (`/api/nutrition/food-moderation`)
- `GET pending` — keyset pending queue (`capability:platform.nutrition_foods.moderate`).
- `POST {requestId}/decide` — approve (unify public food) or reject (keep override, email user).

### Profile (`/api/nutrition/profile`)
- `GET` — current nutrition profile + active goal.
- `POST onboarding` — TDEE-based goal from anthropometrics.
- `PUT goal` — manual macro goal override.

### Diary (`/api/nutrition/diary`)
- `POST entries` — idempotent upsert by client entry id.
- `DELETE entries/{entryId}` — remove entry.
- `GET` — get diary day by `date` query param.
- `GET substitutes` — macro-similar food suggestions.
- `PUT entries/{entryId}/substitute` — replace diary item food.

### Meal plans (`/api/nutrition/meal-plans`)
- `POST` — create meal plan document.
- `POST {mealPlanId}/activate` — mark plan active for user.

### Weight (`/api/nutrition/weight`)
- `PUT target` — upsert weight target.
- `POST registers` — upsert daily weight register.
- `GET registers` — list registers (keyset pagination).

## End-to-End Flows

### Diary write (high-frequency SQL path)
1. Client sends authenticated request with client-generated entry id.
2. Handler loads/creates `DiaryDay` for the user's date.
3. Food macros are resolved from Mongo (`Food`/`FoodOverride` via `FoodVersionResolver`).
4. Entry is upserted idempotently; day totals derive from stored `ComputedMacros`.
5. `Result<T>` mapped to HTTP by controller.

### Food override + moderation (Mongo path)
1. User edits a public food → `FoodOverride` persisted + `FoodModerationRequest` queued.
2. Admin lists pending requests and approves or rejects.
3. Approve unifies public `Food` and removes override; reject keeps override and sends
   template email via `Notifications` (guarded by `notifications.email-enabled` feature flag).

### Goal evaluation (time-cutoff event — AD-012)
1. MassTransit recurring job (`EvaluateNutritionGoals`) runs at UTC day boundary.
2. Consumer loads unevaluated `DiaryDay` rows for the prior date from SQL.
3. Compares summed macros against `NutritionProfile.ActiveGoal` tolerance.
4. On success, publishes `NutritionGoalMet` via `IPublishEndpoint` (no outbox).
5. Marks day evaluated + inserts `NutritionGoalEvaluation` for idempotency.
6. `GamificationNutritionGoalMetConsumer` credits nutrition streak columns.

## Dependency Injection View

```text
NutritionModule
├── NutritionDbContext (SQL Server)
├── IMongoClient (shared singleton, Nutrition or Training connection string)
├── Mongo repositories (Food, Override, Moderation, MealPlan, WeightTracking)
├── CQRS handlers + FluentValidation per slice
└── GoalEvaluation job consumer (MassTransit IJobConsumer, RabbitMQ in production)

Downstream consumers (other domains)
└── Gamification: GamificationNutritionGoalMetConsumer on NutritionGoalMet
```

## Operational Notes

- Diary entry ids are client-generated (Mongo ObjectId-shaped) for offline idempotent retries.
- Search and moderation list endpoints use keyset pagination only.
- `Messaging:EnableNutritionGoalJob` defaults false on InMemory test hosts — do not re-enable
  job saga there (suite stability).
- Weight tracking was migrated from `Features/Training`; routes now live under nutrition.

## Single Source of Truth

Canonical reference for Nutrition domain architecture. Requirements and design:
`.specs/features/nutrition/{spec.md,design.md}`. Project decisions: `AD-012`, `AD-013` in
`.specs/STATE.md`.

## ASCII Diagram

```text
┌─────────────────────────────────────────────────────────────────┐
│                    CLIENT (Web / Mobile)                          │
│   Foods · Diary · Profile · Meal plans · Weight · Moderation    │
└──────────────────────────┬──────────────────────────────────────┘
                           │ Firebase bearer token
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ASP.NET CORE API                             │
├─────────────────────────────────────────────────────────────────┤
│ AuthorizationMiddleware → UserContext                             │
│ [Authorize(Policy="capability:...")] on admin/moderation routes │
└──────────────────────────┬──────────────────────────────────────┘
                           │
          ┌────────────────┴────────────────┐
          ▼                                 ▼
┌──────────────────────┐          ┌──────────────────────┐
│ Nutrition controllers │          │ Recurring job        │
│ Foods · Diary ·       │          │ NutritionGoalEval  │
│ Profile · MealPlans · │          │ Consumer (UTC cutoff)│
│ Weight · Moderation   │          └──────────┬───────────┘
└──────────┬───────────┘                     │
           │                                 │ IPublishEndpoint
           ▼                                 │ (no outbox, AD-012)
┌──────────────────────┐                     ▼
│ CQRS handlers +      │          ┌──────────────────────┐
│ FluentValidation +   │          │ RabbitMQ / MassTransit│
│ Result pattern       │          │ NutritionGoalMet      │
└──────────┬───────────┘          └──────────┬───────────┘
           │                                 │
     ┌─────┴─────┐                           ▼
     ▼           ▼                 ┌──────────────────────┐
┌─────────┐ ┌─────────┐            │ Gamification consumer │
│ MongoDB │ │ SQL     │            │ (nutrition streak)    │
│ Food    │ │ Diary   │            └──────────────────────┘
│ Override│ │ Profile │
│ MealPlan│ │ Eval    │
│ Moderate│ │ Weight  │
└─────────┘ └─────────┘
```
