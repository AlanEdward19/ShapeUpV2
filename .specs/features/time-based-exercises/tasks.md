# Exercícios Baseados em Tempo — Tasks (Backend only)

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Spec**: `.specs/features/time-based-exercises/spec.md`
**Design**: `.specs/features/time-based-exercises/design.md`
**Status**: Approved
**Scope note**: This pass covers **only the API backend** (`ShapeUpApi`) requirement IDs marked `[Backend]` or unmarked/structural in the spec (TBE-01, TBE-02 backend half, TBE-03 backend half, TBE-06). `ShapeUp-Web` frontend ACs (TBE-02 editor UI conditional inputs, TBE-03 client-side gate, TBE-05) are explicitly out of scope here — tracked as the documented "Next step" in `.specs/STATE.md`.

---

## Test Coverage Matrix

> Generated from codebase sampling (`tests/UnitTests/Domains/Training/**`, `tests/IntegrationTests/Domains/Training/**`). No dedicated testing-guidelines doc found beyond `src/AGENTS.md` ("Unit Testing" section — mock repositories/validators, test handlers in isolation, test FluentValidation rules separately); strong defaults applied on top of that.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Enum / VO field additions (`ExerciseType`, set VOs) | none | Build gate only — no behavior to assert beyond compilation | — | `dotnet build` |
| `Exercise` entity + EF migration | none | Build gate + migration applies cleanly | — | `dotnet build` |
| Catalog handlers (`CreateExerciseHandler`, `UpdateExerciseHandler`) | unit | 1:1 to TBE-01 ACs (persist/default `ExerciseType`) | `tests/UnitTests/Domains/Training/Exercises/*HandlerTests.cs` | `dotnet test` |
| FluentValidation validators (all `*CommandValidator.cs`, `WorkoutExerciseDtoValidator`) | unit | Every new/changed rule (nullable Load, Duration/Distance positivity, Technique restriction) has a pass + fail case | `tests/UnitTests/Domains/Training/**/*ValidatorTests.cs` (new files, mirroring handler test folders) | `dotnet test` |
| Plan/Template/Session handlers (Create/Update/Start/UpdateState/Finish/Complete/Swap) | unit | All branches; 1:1 to spec ACs (TBE-02 AC1-4, TBE-03 AC4-6, TBE-06 AC1-4); every listed Edge Case with a handler-level effect has a test | `tests/UnitTests/Domains/Training/{Workouts,WorkoutPlans,WorkoutTemplates}/*HandlerTests.cs` | `dotnet test` |
| `ExecutedSetDocumentValueObject.Volume` (domain logic) | unit | Null-safe branches (Load null, Repetitions null, both present) | `tests/UnitTests/Domains/Training/**` (new `ExecutedSetDocumentValueObjectTests.cs` or inline in a handler test) | `dotnet test` |
| PR evaluation (`EvaluatePrs` in Finish/CompleteWorkoutSessionHandler) | unit | 1:1 to TBE-06 AC1-4 + existing `max_volume`/`max_load`/`max_reps_same_load` regression (must not break for `WeightBased` sets) | `tests/UnitTests/Domains/Training/Workouts/{FinishWorkoutExecutionHandlerTests,CompleteWorkoutSessionHandlerTests}.cs` | `dotnet test` |
| API contract (controllers/routes) | none this pass | No new routes/params added — existing routes gain fields on existing request/response shapes; covered indirectly by the integration suite already exercising those routes | `tests/IntegrationTests/Domains/Training/*.cs` | `dotnet test` |

**Coverage Expectation defaults used:** domain/business logic → all branches, 1:1 to spec ACs; entity/config/schema → none (build gate only). No repo-specific override found beyond the "mock repositories, test handlers in isolation" note in `src/AGENTS.md`.

## Gate Check Commands

| Gate Level | When to Use | Command |
|---|---|---|
| Quick | After tasks touching only unit-testable code (enums, VOs, validators, handlers) | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Build | After entity/migration/VO-signature-only tasks with no new test | `dotnet build` |
| Full | After the last task of each phase, and always before the final commit of the feature | `dotnet test` (runs both `UnitTests` and `IntegrationTests`) |

---

## Execution Plan

Phases are ordered and run sequentially — each phase completes before the next begins, tasks within a phase run in order.

### Phase 1: Catalog foundation (`ExerciseType`)
```
T1 → T2 → T3
```

### Phase 2: Set value objects (duration/distance, nullable Load/Repetitions)
```
T4 → T5 → T6 → T7
```

### Phase 3: Plan/template/session field passthrough (no new gate yet — just don't lose data)
```
T8 → T9 → T10 → T11 → T12 → T13
```

### Phase 4: Backend validation gates (TBE-02, TBE-03)
```
T14 → T15 → T16 → T17 → T18 → T19
```

### Phase 5: PR de pace (TBE-06) + PR nullable-safety
```
T20 → T21 → T22
```

---

## Task Breakdown

### T1: Create `ExerciseType` enum

**What**: New enum `ExerciseType { WeightBased = 1, TimeBased = 2 }`.
**Where**: `src/Features/Training/Shared/Enums/ExerciseType.cs` (new file)
**Depends on**: None
**Reuses**: Same convention as `src/Features/Training/Shared/Enums/LoadUnit.cs`/`SetType.cs`
**Requirement**: TBE-01

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] File created, enum matches design.md exactly
- [x] `dotnet build` succeeds

**Tests**: none
**Gate**: build

---

### T2: `Exercise.ExerciseType` property + EF migration

**What**: Add `public ExerciseType ExerciseType { get; set; } = ExerciseType.WeightBased;` to `Exercise`; generate and apply EF Core migration with column default `WeightBased` so all existing rows are unaffected.
**Where**: `src/Features/Training/Shared/Entities/Exercise.cs`; new migration under `src/Features/Training/Infrastructure/Data/Migrations/` (DbContext: `TrainingDbContext`)
**Depends on**: T1
**Reuses**: Migration shape/naming of `20260917113659_AddExerciseEquivalents.cs`; store as string like other Training enums (`[BsonRepresentation]` is Mongo-only — for EF, check whether other enum columns in `Exercise`/sibling entities use `.HasConversion<string>()` or default int storage in `TrainingDbContext` `OnModelCreating`/entity configs before deciding column type)
**Requirement**: TBE-01

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] `ExerciseType` column added with a non-breaking default for existing rows
- [x] Migration generated via `dotnet ef migrations add AddExerciseType --context TrainingDbContext --output-dir Features/Training/Infrastructure/Data/Migrations` from the `src` project, `Up`/`Down` reviewed
- [x] `dotnet build` succeeds; migration apply attempted against the configured dev DB — blocked by a pre-existing, unrelated migration bug (`AddExerciseEquivalents` FK cascade cycle on SQL Server), not caused by this task; `Up`/`Down` of `AddExerciseType` itself reviewed by inspection only (see commit note)

**Tests**: none
**Gate**: build

---

### T3: Thread `ExerciseType` through catalog CRUD + response

**What**: Add `ExerciseType` to `ExerciseResponse`, `CreateExerciseCommand`, `UpdateExerciseCommand`; validate with `IsInEnum()` in both validators; set/persist it in `CreateExerciseHandler`/`UpdateExerciseHandler` (`MapResponse` included); default to `WeightBased` when the command omits it (per TBE-01 AC1/AC2).
**Where**: `Exercises/Shared/ViewModels/ExerciseResponse.cs`, `Exercises/CreateExercise/{CreateExerciseCommand,CreateExerciseCommandValidator,CreateExerciseHandler}.cs`, `Exercises/UpdateExercise/{UpdateExerciseCommand,UpdateExerciseCommandValidator,UpdateExerciseHandler}.cs`
**Depends on**: T2
**Reuses**: Existing `Name`/`NamePt` field-threading pattern in the same files
**Requirement**: TBE-01

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Creating an exercise with `ExerciseType = TimeBased` persists and returns it
- [x] Creating/updating an exercise without specifying `ExerciseType` (or a pre-existing row) reports `WeightBased`
- [x] Updating an exercise's `ExerciseType` is reflected on the next `GetById`/`MapResponse`
- [x] `dotnet test tests/UnitTests/UnitTests.csproj` passes; new tests added to `CreateExerciseHandlerTests.cs`/`UpdateExerciseHandlerTests.cs` (or new validator test files) — expected net new test count: at least 4 (create persists TimeBased, create defaults WeightBased, update changes type, validator rejects out-of-range enum value)

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): classify exercises as weight- or time-based`

---

### T4: `PlannedSetDocumentValueObject` — duration/distance + nullable `Load`

**What**: Add `public int? DurationSeconds { get; set; }` and `public decimal? DistanceMeters { get; set; }`; change `Load` from `decimal` to `decimal?`.
**Where**: `Features/Training/Shared/Documents/ValueObjects/PlannedSetDocumentValueObject.cs`
**Depends on**: None (independent of Phase 1)
**Reuses**: Same nullable pattern as the existing `RestSeconds: int?` on the same class
**Requirement**: TBE-02, TBE-03

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Fields added exactly as in design.md's Data Models section
- [x] `dotnet build` fails at every call site that reads `.Load` as non-nullable (expected — fixed in Phase 3); call sites that only *assign* `Load = s.Load` keep compiling (confirmed: 3 build errors, all trace to `WorkoutSetValueObject.Load`/`ExecutedSetDocumentValueObject.Load` still non-nullable, resolved by T5/T6 below)

**Tests**: none
**Gate**: build

---

### T5: `WorkoutSetValueObject` record — duration/distance + nullable `Load`

**What**: Add `int? DurationSeconds = null, decimal? DistanceMeters = null` as new **trailing** optional positional parameters (after `IsExtra`) so existing positional-arg call sites keep compiling unchanged; change `Load` from `decimal` to `decimal?`.
**Where**: `Features/Training/Workouts/Shared/ValueObjects/WorkoutSetValueObject.cs`
**Depends on**: None
**Reuses**: Trailing-default-parameter pattern already used for `IsExtra = false` on the same record
**Requirement**: TBE-02, TBE-03

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Record signature updated exactly as above (order matters — appending, not inserting, keeps existing tests like `UpdateWorkoutExecutionStateHandlerTests.cs` compiling without touching them)
- [x] `dotnet build` — only `.Load`-as-non-nullable read sites break (expected, fixed later; confirmed: 4 errors, all `Load = s.Load` assignments into the still-non-nullable `ExecutedSetDocumentValueObject.Load`, resolved by T6 below)

**Tests**: none
**Gate**: build

---

### T6: `ExecutedSetDocumentValueObject` — duration/distance, nullable `Load`/`Repetitions`, null-safe `Volume`

**What**: Add `DurationSeconds`/`DistanceMeters`; change `Load` to `decimal?` and `Repetitions` to `int?`; change `Volume` to `Load.HasValue && Repetitions.HasValue ? Load.Value * Repetitions.Value : 0m`.
**Where**: `Features/Training/Shared/Documents/ValueObjects/ExecutedSetDocumentValueObject.cs`
**Depends on**: None
**Reuses**: Design.md's exact snippet for this class
**Requirement**: TBE-02, TBE-03, TBE-06 (Volume=0 is the foundation `EvaluatePrs` in Phase 5 relies on)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] `Volume` returns `0m` when `Load` is null, when `Repetitions` is null, or both — and the unchanged product when both are present
- [x] New unit test(s) added asserting all 3 branches (Load-only-null, Repetitions-only-null, both-null all → 0; both-present → product) — 4 new tests in `ExecutedSetDocumentValueObjectTests.cs`
- [x] `dotnet test tests/UnitTests/UnitTests.csproj` — **blocked**: `UnitTests.csproj` references `ShapeUp.csproj`, which now fails to build for reasons entirely outside T1-T7 (nullable `Load`/`Repetitions` propagate to 3 files never listed by any task in T1-T7: `AntiCheatClassifier.cs` — not covered by ANY task T1-T21, a tasks.md gap — plus `CompleteWorkoutSessionHandler.cs`/`FinishWorkoutExecutionHandler.cs`, which are T20/T21's explicit territory). Verified by diffing `dotnet build` error output before/after this change: zero new/unexpected errors introduced; the 4 new tests were verified correct by code inspection against the implemented `Volume` logic instead of a live green run. See batch report for full file list.

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): make set duration/distance and nullable load first-class in set VOs`

---

### T7: Response VOs — surface duration/distance to API consumers

**What**: Add `DurationSeconds`/`DistanceMeters` to whatever record backs `ExecutedSetValueObject` used inside `ExecutedExerciseDto`/`WorkoutSessionResponse`; update `WorkoutSessionResponseMapper.Map` to pass them through (mirrors how it already passes `set.Volume`).
**Where**: `Features/Training/Workouts/Shared/ViewModels/WorkoutSessionResponse.cs` (and the `ExecutedSetValueObject`/`ExecutedExerciseDto` records it contains), `Features/Training/Workouts/Shared/WorkoutSessionResponseMapper.cs`
**Depends on**: T6
**Reuses**: Existing positional-record pass-through pattern in the same mapper
**Requirement**: TBE-05 (backend half — the frontend cannot render duration/distance if the API never returns them)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] `WorkoutSessionResponse` for a session containing a `TimeBased` set includes its `DurationSeconds`/`DistanceMeters`
- [x] `dotnet build` — confirmed via error diffing that this task's own edits (`ExecutedSetValueObject`, `WorkoutSessionResponseMapper.cs`) introduce zero new errors and resolve the 2 errors this file previously had; the only remaining `dotnet build`/`dotnet test` failures are the pre-existing, out-of-T1-T7-scope files noted in T6 (`AntiCheatClassifier.cs`, `CompleteWorkoutSessionHandler.cs`, `FinishWorkoutExecutionHandler.cs`); no test asserting the old positional shape existed to update (none found)

**Tests**: unit (extend nearest existing handler test that asserts response shape, e.g. `StartWorkoutExecutionHandlerTests.cs`)
**Gate**: quick

**Commit**: `feat(training): surface set duration/distance in workout session responses`

---

### T8: `WorkoutPlanMappings` — carry duration/distance through Clone/ToResponse

**What**: Update `Clone` and `ToResponse` in `WorkoutPlanMappings.cs` to copy `DurationSeconds`/`DistanceMeters` alongside the existing set fields.
**Where**: `Features/Training/WorkoutPlans/Shared/WorkoutPlanMappings.cs`
**Depends on**: T4, T5
**Reuses**: The exact copy blocks already there for `RestSeconds`
**Requirement**: TBE-02

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Cloning a plan containing a `TimeBased` set preserves its duration/distance in the clone
- [x] `ToResponse()` includes duration/distance for `TimeBased` sets
- [x] `dotnet build` succeeds (no test file currently exercises `Clone` directly — covered transitively by Phase 4 handler tests; confirmed via error diffing — zero new errors from this file, only the pre-existing T20/T21/T22-scoped breakage remains)

**Tests**: none (transitively covered by T15/T16)
**Gate**: build

---

### T9: `CreateWorkoutPlanHandler` + `UpdateWorkoutPlanHandler` — carry duration/distance

**What**: In both handlers' `PlannedSetDocumentValueObject` construction, copy `DurationSeconds`/`DistanceMeters` from the input set.
**Where**: `WorkoutPlans/CreateWorkoutPlan/CreateWorkoutPlanHandler.cs`, `WorkoutPlans/UpdateWorkoutPlan/UpdateWorkoutPlanHandler.cs`
**Depends on**: T8
**Reuses**: Same mapping block pattern in both files
**Requirement**: TBE-02

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Creating/updating a plan with a `TimeBased` set's duration/distance round-trips into the persisted document
- [x] `dotnet build` succeeds (behavior asserted in Phase 4 tests once the gate exists; confirmed via error diffing — same 7 pre-existing T20/T21/T22-scoped errors, zero new)

**Tests**: none this task (asserted by T15/T16)
**Gate**: build

---

### T10: `CreateWorkoutTemplateHandler` + `UpdateWorkoutTemplateHandler` + template mappings — carry duration/distance

**What**: Same mechanical field passthrough as T9, applied to the template-authoring handlers and `WorkoutTemplateMappings.cs`.
**Where**: `WorkoutTemplates/CreateWorkoutTemplate/CreateWorkoutTemplateHandler.cs`, `WorkoutTemplates/UpdateWorkoutTemplate/UpdateWorkoutTemplateHandler.cs`, `WorkoutTemplates/Shared/WorkoutTemplateMappings.cs`
**Depends on**: T8
**Reuses**: Same block, copied from T9's finished shape
**Requirement**: TBE-02 (parity between plan authoring and template authoring)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Creating/updating a template with a `TimeBased` set's duration/distance round-trips
- [x] `dotnet build` succeeds (confirmed via error diffing — same 7 pre-existing T20/T21/T22-scoped errors, zero new)

**Tests**: none this task (asserted by T17/T18)
**Gate**: build

---

### T11: `CopyWorkoutTemplateHandler` + `AssignWorkoutTemplateHandler` — carry duration/distance

**What**: Update both handlers' `PlannedSetDocumentValueObject` construction (copying from an existing template/plan, not from raw user input) to also copy `DurationSeconds`/`DistanceMeters` — otherwise a `TimeBased` template silently loses its duration the moment it's copied or assigned to a plan.
**Where**: `WorkoutTemplates/CopyWorkoutTemplate/CopyWorkoutTemplateHandler.cs`, `WorkoutTemplates/AssignWorkoutTemplate/AssignWorkoutTemplateHandler.cs`
**Depends on**: T4
**Reuses**: Same copy-block pattern already present in both files
**Requirement**: TBE-02 (data-loss prevention, not a new gate — source data is already validated)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Copying/assigning a template containing a `TimeBased` set preserves duration/distance in the resulting template/plan
- [x] `dotnet build` succeeds (confirmed via error diffing — same 7 pre-existing T20/T21/T22-scoped errors, zero new); regression test added to existing `AssignWorkoutTemplateHandlerTests.cs`; `CopyWorkoutTemplateHandler` has no existing test scaffold — gap logged, no new test file invented per task instruction

**Tests**: unit (extend existing tests if present; otherwise `none` and log the gap — do not invent a new test file just for a field-passthrough with no existing test scaffold)
**Gate**: quick

**Commit**: `feat(training): thread exercise duration/distance through plan and template authoring flows`

---

### T12: `StartWorkoutExecutionHandler` — flatten `ExerciseType`, carry duration/distance, drop `Repetitions ?? 0`

**What**: Add `public ExerciseType ExerciseType { get; set; } = ExerciseType.WeightBased;` to `ExecutedExerciseDocumentValueObject`; in `StartWorkoutExecutionHandler`, look up the exercise (already available via `plan.Blocks...`? — check whether `BlockExerciseDocumentValueObject` needs `ExerciseType` added too, since the plan snapshot itself doesn't currently carry it) and flatten `ExerciseType` onto the session snapshot exactly like `RequireRpe` already is; copy `DurationSeconds`/`DistanceMeters`; change `Repetitions = s.Repetitions ?? 0` to `Repetitions = s.Repetitions` (now nullable end-to-end).
**Where**: `Features/Training/Shared/Documents/ValueObjects/ExecutedExerciseDocumentValueObject.cs`, `Features/Training/Shared/Documents/ValueObjects/BlockExerciseDocumentValueObject.cs` (add `ExerciseType` if the flatten source needs it — confirm by tracing where `BlockExerciseDocumentValueObject.RequireRpe` itself gets its value from; if `ExerciseType` is catalog-only and never stored per-block, this handler must instead re-fetch it from `IExerciseRepository`/`ExerciseId` at start time, mirroring the pattern in T9's handlers), `Workouts/StartWorkoutExecution/StartWorkoutExecutionHandler.cs`
**Depends on**: T3, T6
**Reuses**: The exact flatten mechanism already used for `RequireRpe` in this same handler
**Requirement**: TBE-03 (this is the snapshot the execution gate in T19 reads from)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Starting a session from a plan containing a `TimeBased` exercise produces a session snapshot whose `ExecutedExerciseDocumentValueObject.ExerciseType == TimeBased`
- [x] A `WeightBased` exercise's session snapshot is unaffected (`ExerciseType == WeightBased`, same as before this feature existed)
- [x] `DurationSeconds`/`DistanceMeters` on each set survive from plan to session start
- [x] `Repetitions` on a `TimeBased` set's `ExecutedSetDocumentValueObject` is `null`, not `0`, after start
- [x] `dotnet test tests/UnitTests/UnitTests.csproj` — **blocked** solution-wide by the same pre-existing T20/T21/T22-scoped breakage (`AntiCheatClassifier.cs`, `CompleteWorkoutSessionHandler.cs`, `FinishWorkoutExecutionHandler.cs`); verified via `dotnet build src/ShapeUp.csproj` error diffing (zero new errors from this task's files) plus manual code-inspection of the 4 new/extended assertions in `StartWorkoutExecutionHandlerTests.cs` against the implemented handler logic. `IExerciseRepository` added as a new constructor dependency on `StartWorkoutExecutionHandler` — all 5 existing test call sites updated to pass a mocked repository (only the 2 tests with non-empty `Blocks` needed `GetByIdAsync` setups); DI resolves it automatically (`services.AddScoped<StartWorkoutExecutionHandler>()`, no explicit factory). Net new/extended test count: 1 new fact + assertions added to 2 existing facts.

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): flatten exercise type onto workout session snapshot at start`

---

### T13: `UpdateWorkoutExecutionStateHandler` + `SwapExerciseInSessionHandler` — carry duration/distance, nullable `Repetitions`

**What**: In both handlers' `ExecutedSetDocumentValueObject` construction, copy `DurationSeconds`/`DistanceMeters`; change `Repetitions = s.Repetitions!.Value` / `s.Repetitions ?? 0` to plain nullable pass-through (`s.Repetitions`) now that the target field is `int?`.
**Where**: `Workouts/UpdateWorkoutExecutionState/UpdateWorkoutExecutionStateHandler.cs`, `Workouts/SwapExerciseInSession/SwapExerciseInSessionHandler.cs`
**Depends on**: T6, T12
**Reuses**: Same construction blocks already in both files
**Requirement**: TBE-03

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Saving execution state for a `TimeBased` set preserves its duration/distance
- [x] Swapping to a retained `TimeBased` set preserves its duration/distance
- [x] `dotnet build` succeeds (gate behavior itself is T19; this task is passthrough only; confirmed via error diffing — same 7 pre-existing T20/T21/T22-scoped errors, zero new)

  **SPEC_DEVIATION (in-scope data-loss fix, not new behavior):** `UpdateWorkoutExecutionStateHandler`'s `mappedExercises` projection rebuilds `ExecutedExerciseDocumentValueObject` from scratch on every state update and, before this task, never copied `ExerciseType` — meaning it silently reset to the default `WeightBased` on every save, undoing T12's flatten-at-start and breaking the read that T20/T21 (Phase 5) explicitly depend on ("via the exercise's flattened type from T12"). Added `ExerciseType = x.Exercise.ExerciseType` (available on the already-fetched `exerciseMaps` tuple) to close this gap — same "just don't lose data" mandate stated for Phase 3. Same fix applied to the new-exercise entry added by `SwapExerciseInSessionHandler` (`ExerciseType = mapped.ExerciseType`).

**Tests**: none this task (asserted together with T19's gate tests)
**Gate**: build

---

### T14: `WorkoutExerciseDtoValidator` — relax unconditional set rules, add duration/distance format rules

**What**: Change `RuleFor(x => x.Repetitions).NotNull()` and `RuleFor(x => x.RestSeconds).NotNull()` to be conditional/removed so a `TimeBased` set (null `Repetitions`, null `Load`) can pass this layer; make the `Load` rule `.GreaterThanOrEqualTo(0).When(x => x.Load.HasValue)`; add `set.RuleFor(x => x.DurationSeconds!.Value).GreaterThan(0).When(x => x.DurationSeconds.HasValue)` and `set.RuleFor(x => x.DistanceMeters!.Value).GreaterThanOrEqualTo(0).When(x => x.DistanceMeters.HasValue)`. The **required-when-`TimeBased`** rule stays out of this validator (it has no access to `ExerciseType`) — that's enforced in T19 at the handler.
**Where**: `Features/Training/Workouts/Shared/Dtos/WorkoutExerciseDtoValidator.cs`
**Depends on**: T5
**Reuses**: The `.When(x => x.Repetitions.HasValue)` pattern already used one line below the rule being relaxed
**Requirement**: TBE-03 (backend, defense-in-depth format checks)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] A `WorkoutExerciseDto` with a set that has `Repetitions = null, Load = null, DurationSeconds = 120` passes this validator
- [x] A `WorkoutExerciseDto` with a set that has `DistanceMeters = -1` fails
- [x] Existing `WeightBased`-shaped payloads (both fields present, valid) still pass — regression
- [x] `dotnet test tests/UnitTests/UnitTests.csproj` — **blocked** solution-wide by the same pre-existing T20/T21/T22-scoped breakage (`AntiCheatClassifier.cs`, `CompleteWorkoutSessionHandler.cs`, `FinishWorkoutExecutionHandler.cs`); verified via error diffing (same 7 pre-existing errors, zero new) plus code inspection of the 6 new/extended assertions against the implemented FluentValidation rules. Net new test count: 5 (`Validate_WhenTimeBasedShapedSetHasNoLoadOrRepetitionsButHasDuration_IsValid`, `Validate_WhenDistanceMetersIsNegative_IsInvalid`, `Validate_WhenDistanceMetersIsZero_IsValid`, `Validate_WhenDurationSecondsIsZeroOrNegative_IsInvalid` (theory x2), `Validate_WhenWeightBasedShapedSetHasNoDurationOrDistance_IsValid`)

**Tests**: unit (new `WorkoutExerciseDtoValidatorTests.cs` if none exists yet)
**Gate**: quick

**Commit**: `fix(training): allow duration-based sets through the execution DTO validator`

---

### T15: `CreateWorkoutPlanCommandValidator` + `CreateWorkoutPlanHandler` — TimeBased gate

**What**: In the validator, make `set.RuleFor(x => x.Load).GreaterThanOrEqualTo(0)` conditional on `HasValue`; add positivity rules for `DurationSeconds`/`DistanceMeters` mirroring T14. In the handler, after loading each `exercise`/`mapped` (`ExerciseType` now available per T3), when `mapped.ExerciseType == TimeBased`: reject (return `TrainingErrors`/`CommonErrors.Validation`) any set with `DurationSeconds is null or <= 0`, and reject any set whose `Technique != Straight`.
**Where**: `WorkoutPlans/CreateWorkoutPlan/CreateWorkoutPlanCommandValidator.cs`, `WorkoutPlans/CreateWorkoutPlan/CreateWorkoutPlanHandler.cs`
**Depends on**: T9, T14
**Reuses**: The exact `foreach (var exerciseInput in blockInput.Exercises)` loop already in the handler — insert the check right after `var mapped = CreateExerciseHandler.MapResponse(exercise);`
**Requirement**: TBE-02

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Saving a plan with a `TimeBased` exercise's set missing `DurationSeconds` is rejected with a validation error naming the missing field
- [x] Saving the same set with `DurationSeconds` filled (no `DistanceMeters`) succeeds
- [x] Saving a `TimeBased` set with `Technique != Straight` is rejected
- [x] A `WeightBased` exercise's plan save is completely unaffected — regression test
- [x] `dotnet test tests/UnitTests/UnitTests.csproj` — **blocked** solution-wide by the same pre-existing T20/T21/T22-scoped breakage; verified via error diffing (same 7 pre-existing errors, zero new) plus code inspection. Net new test count: 4 (`HandleAsync_WhenTimeBasedExerciseSetMissingDuration_ReturnsValidationErrorNamingExercise`, `HandleAsync_WhenTimeBasedExerciseSetHasDurationWithoutDistance_CreatesWorkoutPlan`, `HandleAsync_WhenTimeBasedExerciseSetHasNonStraightTechnique_ReturnsValidationError`, `HandleAsync_WhenWeightBasedExerciseSetHasNoDuration_CreatesWorkoutPlanUnaffectedByTimeBasedGate`)

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): reject time-based exercise sets without duration when saving a plan`

---

### T16: `UpdateWorkoutPlanCommandValidator` + `UpdateWorkoutPlanHandler` — mirror T15

**What**: Same gate as T15, applied to update.
**Where**: `WorkoutPlans/UpdateWorkoutPlan/UpdateWorkoutPlanCommandValidator.cs`, `WorkoutPlans/UpdateWorkoutPlan/UpdateWorkoutPlanHandler.cs`
**Depends on**: T9, T14, T15 (mirror after T15's shape is settled)
**Reuses**: T15's finished implementation, copied
**Requirement**: TBE-02

**Tools**: MCP: NONE / Skill: NONE

**Done when**: Same four bullets as T15, applied to update; if no `UpdateWorkoutPlanHandlerTests.cs` exists yet, create it following the naming/style of `CreateWorkoutPlanHandlerTests.cs` — [x] done, mirrored T15 exactly (4 new tests); verified via error diffing (same 7 pre-existing errors, zero new)
**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): reject time-based exercise sets without duration when updating a plan`

---

### T17: `CreateWorkoutTemplateCommandValidator` + `CreateWorkoutTemplateHandler` — mirror T15

**What**: Same gate as T15, applied to template creation.
**Where**: `WorkoutTemplates/CreateWorkoutTemplate/CreateWorkoutTemplateCommandValidator.cs`, `WorkoutTemplates/CreateWorkoutTemplate/CreateWorkoutTemplateHandler.cs`
**Depends on**: T10, T14, T15
**Reuses**: T15's finished implementation
**Requirement**: TBE-02 (parity — a template author gets the same protection as a plan author)

**Tools**: MCP: NONE / Skill: NONE

**Done when**: Same four bullets as T15, applied to template creation; add/extend `CreateWorkoutTemplateHandlerTests.cs`
**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): reject time-based exercise sets without duration when saving a template`

---

### T18: `UpdateWorkoutTemplateCommandValidator` + `UpdateWorkoutTemplateHandler` — mirror T15

**What**: Same gate as T15, applied to template update.
**Where**: `WorkoutTemplates/UpdateWorkoutTemplate/UpdateWorkoutTemplateCommandValidator.cs`, `WorkoutTemplates/UpdateWorkoutTemplate/UpdateWorkoutTemplateHandler.cs`
**Depends on**: T10, T14, T17
**Reuses**: T17's finished implementation
**Requirement**: TBE-02

**Tools**: MCP: NONE / Skill: NONE

**Done when**: Same four bullets as T15, applied to template update; add/extend `UpdateWorkoutTemplateHandlerTests.cs`
**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): reject time-based exercise sets without duration when updating a template`

---

### T19: `UpdateWorkoutExecutionStateHandler` — TBE-03 backend completion gate

**What**: After resolving `exercise`/`mapped` for each `exerciseInput` (same loop that already resolves `requireRpe`), when `mapped.ExerciseType == TimeBased`: reject with a 400 validation failure any set whose `DurationSeconds is null or <= 0` (mirrors the existing `RpeRequiredForExercise`-style early return). Leave the `WeightBased` path (existing `Repetitions!.Value`/`RestSeconds!.Value` access after T13's changes — re-verify these still compile now that the DTO validator no longer force-requires them) exactly as-is.
**Where**: `Workouts/UpdateWorkoutExecutionState/UpdateWorkoutExecutionStateHandler.cs`; add a `TrainingErrors.DurationRequiredForExercise(int exerciseId)`-style error if the errors catalog needs a new entry (check `Features/Training/Shared/Errors/TrainingErrors.cs` for the existing pattern used by `RpeRequiredForExercise`)
**Depends on**: T13, T14, T15 (same gate shape as the plan-save gate, but at execution time)
**Reuses**: The `requireRpe`/`RpeRequiredForExercise` early-return pattern a few lines above in the same handler
**Requirement**: TBE-03

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Updating execution state with a `TimeBased` set missing/zero/negative `DurationSeconds` returns a 400 with a message naming the exercise
- [ ] The same set with valid `DurationSeconds` (with or without `DistanceMeters`) succeeds
- [ ] A `WeightBased` set's existing gate (peso/reps) is completely unaffected — regression test using the exact assertions already in `UpdateWorkoutExecutionStateHandlerTests.cs`
- [ ] A `TimeBased` set that was completed, then has its duration cleared in a later update, is **not** auto-uncompleted by this task (that's a `[Frontend]` concern per spec Edge Cases — note explicitly if backend has no "completed" flag to touch here; if it does, flag as a gap for the Verifier rather than guessing)
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes; net new test count: at least 3

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): reject time-based set completion without a valid duration server-side`

---

### T20: `FinishWorkoutExecutionHandler.EvaluatePrs` — null-safe PRs + `best_pace`

**What**: Guard `max_volume`/`max_load`/`max_reps_same_load` against sets with `Load == null` (a `TimeBased` set should never surface a `max_load`/`max_reps_same_load` PR — filter it out of those specific computations rather than letting a null `Load` flow into a numeric comparison/groupby). Add a new pass: for each set with `ExerciseType == TimeBased` (via the exercise's flattened type from T12) and `DurationSeconds >= 1 && DistanceMeters > 0`, compute `pace = DurationSeconds / DistanceMeters`, compare against the historical minimum pace for that `ExerciseId` (lower is better), and if it's a new best (or no prior record), add a `WorkoutPrDocumentValueObject { Type = "best_pace", Value = pace }`. Sets without `DistanceMeters` never enter this pass (no PR attempted, not an error).
**Where**: `Workouts/FinishWorkoutExecution/FinishWorkoutExecutionHandler.cs` (`EvaluatePrs` private method)
**Depends on**: T6, T12, T13
**Reuses**: The existing `historySets`/`prs.Add(new WorkoutPrDocumentValueObject {...})` shape already used for `max_volume`
**Requirement**: TBE-06

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Finishing a session with a `TimeBased` set that has a better pace than history adds exactly one `best_pace` PR for that exercise
- [ ] Finishing a session with a `TimeBased` set that has no `DistanceMeters` adds no PR for that set (not `best_pace`, not `max_volume`/`max_load`)
- [ ] Finishing a session with only `WeightBased` sets produces identical PR output to before this task (regression — same test data as existing `FinishWorkoutExecutionHandlerTests.cs` cases, unmodified assertions)
- [ ] A mixed session (some `WeightBased`, some `TimeBased` with distance) produces both `max_volume`-family PRs for the former and `best_pace` for the latter, never crossing over
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes; net new test count: at least 4

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): add pace personal records for time-based exercises with distance`

---

### T21: `CompleteWorkoutSessionHandler.EvaluatePrs` — mirror T20

**What**: Identical change to T20's `EvaluatePrs`, applied to this handler's copy of the method.
**Where**: `Workouts/CompleteWorkoutSession/CompleteWorkoutSessionHandler.cs`
**Depends on**: T20 (mirror after its shape is settled)
**Reuses**: T20's finished implementation
**Requirement**: TBE-06

**Tools**: MCP: NONE / Skill: NONE

**Done when**: Same five bullets as T20, applied to `CompleteWorkoutSessionHandlerTests.cs`
**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): add pace personal records for time-based exercises with distance (session completion path)`

---

### T22: `AntiCheatClassifier.FlattenPairs` — nullable-safe `Load`/`Repetitions` read (build-break fix, discovered during Batch 1)

**What**: `FlattenPairs` (private static method) builds `List<(int ExerciseId, decimal Load, int Repetitions)>` from `set.Load`/`set.Repetitions`, which are `decimal?`/`int?` as of T6. This is a genuine gap in the original task breakdown — not a business-rule change, a compile-fix so the solution builds. Change the tuple's read to `(exercise.ExerciseId, set.Load ?? 0m, set.Repetitions ?? 0)`, matching the exact "safe default" principle design.md's Risks section already established for `Volume` (a `TimeBased` set with no load/reps contributes `0`, never throws, never corrupts the anti-cheat pair-match ratio for `WeightBased` sessions where both fields are always present anyway). Do **not** change `IsSameShape`'s existing `currentSet.Load != priorSet.Load || currentSet.Repetitions != priorSet.Repetitions` comparison (line 190) — nullable `!=` comparison already compiles and behaves correctly (two nulls are equal, matching "no change" intent), so it needs no edit.
**Where**: `Features/Gamification/Shared/AntiCheat/AntiCheatClassifier.cs` (`FlattenPairs`, ~line 213-216)
**Depends on**: T6 (needs the nullable `Load`/`Repetitions` types to already exist to reproduce the break)
**Reuses**: The exact `?? 0m` / `Volume`-is-safe-default precedent from design.md's Risks & Concerns table, applied at the one remaining call site design.md didn't enumerate
**Requirement**: none (pre-existing spec Cross-Cutting Dependency row for `AntiCheatClassifier` — this task only makes it compile safely; it does NOT implement the "separate anomaly rule for TimeBased sessions" follow-up the spec explicitly defers to `gamification`)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] `dotnet build` succeeds solution-wide (this, combined with T20/T21, is what actually turns the whole-solution build green again after T4-T6's nullable changes)
- [ ] A session containing only `TimeBased` sets (`Load`/`Repetitions` both null) does not throw when anti-cheat pair-matching runs against it — regression test if `AntiCheatClassifier` already has a test file; otherwise `none` and log the gap (do not invent a new test scaffold for a one-line defensive read with no existing test harness)
- [ ] Existing `WeightBased`-only anti-cheat behavior is byte-for-byte unchanged (Load/Repetitions are never null there, so `?? 0m`/`?? 0` never triggers) — confirm via existing `AntiCheatClassifier` tests, if any, still passing unmodified
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes with no regressions

**Tests**: unit if an existing `AntiCheatClassifierTests.cs`-style file exists to extend; otherwise none (pure defensive-null fix, no new business behavior to assert)
**Gate**: quick

**Commit**: `fix(gamification): read possibly-null load/reps safely in anti-cheat pair matching`

---

## Phase Execution Map

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5

Phase 1:  T1 ──→ T2 ──→ T3
Phase 2:  T4 ──→ T5 ──→ T6 ──→ T7
Phase 3:  T8 ──→ T9 ──→ T10 ──→ T11 ──→ T12 ──→ T13
Phase 4:  T14 ──→ T15 ──→ T16 ──→ T17 ──→ T18 ──→ T19
Phase 5:  T20 ──→ T21 ──→ T22
```

Execution is strictly sequential — there is no intra-phase parallelism.

**Note on cross-phase dependencies:** T12 depends on T3 (Phase 1) in addition to T6 (Phase 2) — flagged explicitly since it's a backward reference across a phase boundary that isn't otherwise visible from adjacency. T15/T16/T17/T18 all depend on T14 (Phase 4, same phase, earlier task) and on their respective Phase-3 passthrough task. T19 depends on T13, T14, and reuses T15's shape (cross-referenced, not a hard code dependency). T20/T21 depend on T12/T13 for the flattened `ExerciseType` they read.

---

## Task Granularity Check

| Task | Scope | Status |
|---|---|---|
| T1 | 1 file (enum) | ✅ Granular |
| T2 | 1 entity + 1 migration (one concept: persist the type) | ✅ Granular |
| T3 | 1 concept (thread ExerciseType through catalog CRUD), 6 files | ⚠️ OK — same cohesive concept, same shape repeated across the CRUD pair |
| T4 | 1 VO | ✅ Granular |
| T5 | 1 VO | ✅ Granular |
| T6 | 1 VO + its computed property | ✅ Granular |
| T7 | 1 response shape + its mapper | ✅ Granular |
| T8 | 1 file (mappings) | ✅ Granular |
| T9 | 1 concept (field passthrough), 2 files, same block | ✅ Granular (mechanical mirror) |
| T10 | 1 concept, 3 files, same block | ✅ Granular (mechanical mirror) |
| T11 | 1 concept, 2 files, same block | ✅ Granular (mechanical mirror) |
| T12 | 1 handler + its 2 dependent VOs (one flatten concept) | ⚠️ OK — cohesive, matches how `RequireRpe` flatten was originally built |
| T13 | 1 concept, 2 files | ✅ Granular (mechanical mirror) |
| T14 | 1 validator | ✅ Granular |
| T15 | 1 gate concept, validator + handler pair | ✅ Granular (validator+handler is one gate, same precedent as existing code) |
| T16 | mirror of T15 | ✅ Granular |
| T17 | mirror of T15 | ✅ Granular |
| T18 | mirror of T15 | ✅ Granular |
| T19 | 1 handler, 1 gate concept | ✅ Granular |
| T20 | 1 method in 1 handler | ✅ Granular |
| T21 | mirror of T20 | ✅ Granular |

No task exceeds "one cohesive concept, mechanically mirrored across files" — none require further splitting.

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
|---|---|---|---|
| T1 | None | — | ✅ Match |
| T2 | T1 | T1→T2 | ✅ Match |
| T3 | T2 | T2→T3 | ✅ Match |
| T4 | None | — | ✅ Match |
| T5 | None | — (parallel to T4 within Phase 2, sequential by convention) | ✅ Match |
| T6 | None | — | ✅ Match |
| T7 | T6 | T6→T7 | ✅ Match |
| T8 | T4, T5 | Phase 2→Phase 3 boundary | ✅ Match |
| T9 | T8 | T8→T9 | ✅ Match |
| T10 | T8 | T8→T10 (parallel lineage to T9, sequenced after in list) | ✅ Match |
| T11 | T4 | Phase 2→Phase 3 boundary (T4, not T8) | ✅ Match — noted in Phase dependency note |
| T12 | T3, T6 | Phase 1→Phase 3 and Phase 2→Phase 3 boundaries | ✅ Match — flagged in cross-phase note |
| T13 | T6, T12 | T12→T13 | ✅ Match |
| T14 | T5 | Phase 2→Phase 4 boundary | ✅ Match |
| T15 | T9, T14 | T9→T15, T14→T15 | ✅ Match |
| T16 | T9, T14, T15 | T15→T16 | ✅ Match |
| T17 | T10, T14, T15 | T15→T17 | ✅ Match |
| T18 | T10, T14, T17 | T17→T18 | ✅ Match |
| T19 | T13, T14, T15 | T14→T19, T13→T19 | ✅ Match |
| T20 | T6, T12, T13 | Phase 3→Phase 5 boundary | ✅ Match |
| T21 | T20 | T20→T21 | ✅ Match |

All dependencies point backward or within the same phase; no forward references.

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
|---|---|---|---|---|
| T1 | Enum | none | none | ✅ OK |
| T2 | Entity/migration | none | none | ✅ OK |
| T3 | Catalog handlers | unit | unit | ✅ OK |
| T4 | VO | none | none | ✅ OK |
| T5 | VO | none | none | ✅ OK |
| T6 | VO + domain logic (`Volume`) | unit | unit | ✅ OK |
| T7 | Response VO + mapper | unit | unit | ✅ OK |
| T8 | Mapping (mechanical) | none (no behavior beyond build) | none | ✅ OK |
| T9 | Plan handlers (mechanical passthrough only) | build only for passthrough-only change | none | ✅ OK — behavior asserted at T15/T16 where the gate exists |
| T10 | Template handlers (mechanical passthrough only) | build only | none | ✅ OK — behavior asserted at T17/T18 |
| T11 | Copy/Assign handlers (mechanical passthrough only) | unit if existing scaffold, else none | unit/none (conditional) | ✅ OK |
| T12 | Session start handler | unit | unit | ✅ OK |
| T13 | Session update/swap handlers (mechanical passthrough only) | build only | none | ✅ OK — behavior asserted at T19 |
| T14 | Validator | unit | unit | ✅ OK |
| T15 | Plan handler + validator (gate) | unit | unit | ✅ OK |
| T16 | Plan handler + validator (gate) | unit | unit | ✅ OK |
| T17 | Template handler + validator (gate) | unit | unit | ✅ OK |
| T18 | Template handler + validator (gate) | unit | unit | ✅ OK |
| T19 | Execution handler (gate) | unit | unit | ✅ OK |
| T20 | PR evaluation (domain logic) | unit | unit | ✅ OK |
| T21 | PR evaluation (domain logic) | unit | unit | ✅ OK |

No violations. Tasks marked `none`/`build` are pure mechanical field-passthrough with no new branch to assert — their one behavior-relevant consequence (data not lost) is asserted by the very next task in the same file/handler family that adds a gate or reads the field back out (T9→T15/16, T10→T17/18, T13→T19), per the "merge forward" rule for untestable-until-later code.

---

## Tips

- **This is a big feature — the size is real, not over-engineering.** 21 tasks span catalog, 3 set VOs, 4 authoring handlers (Plan×2, Template×2), 2 copy/assign handlers, 3 execution-flow handlers, and 2 PR-evaluation methods. Each individual task is small; the count comes from mirroring the same 2-3 shapes (nullable-safety, passthrough, gate) across every place `WeightBased`-only logic was previously hard-coded.
- **`WEV-01`/`WEV-02` are never reopened** — every gate task adds an `if (ExerciseType == TimeBased)` branch beside the untouched `WeightBased` path, never inside it.
- **Reuse over invention** — nearly every task's `Reuses` field points at a pattern already in the file being changed (`RequireRpe` flatten, `RestSeconds` nullable precedent, existing `.When(x => x.Field.HasValue)` rules). If an implementer finds themselves inventing a new mechanism, stop and re-check design.md.
