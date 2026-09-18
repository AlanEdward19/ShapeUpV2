# Variações/Equivalências de Exercício — Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Design**: `.specs/features/exercise-variations/design.md`
**Status**: Verified PASS (backend scope) — see `validation.md`
**Scope**: Backend only (`ShapeUpApi`). Covers EXVAR-01/02/03/09 (API), EXVAR-06/07 (swap endpoint with `retainedSetsForOriginal`). EXVAR-04/05/08 and all UI are handoff to `ShapeUp-Web`.

## Progress Log

- ✅ T1 `02c2657` — ExerciseEquivalent entity + EF mapping
- ✅ T2 `2bb9844` — Migration AddExerciseEquivalents
- ✅ T3 `904def4` — Repository + DI
- ✅ T4 `2d1b1a8` — SetExerciseEquivalent + tests
- ✅ T5 `200710a` — RemoveExerciseEquivalent + tests
- ✅ T6 `f506bf2` — GetExerciseEquivalents + tests
- ✅ T7 `9cec2b4` — HTTP endpoints on ExercisesController
- ✅ T8 `45f8df6` — SwapExerciseInSession + tests
- ✅ T9 `eb96d30` — swap-exercise HTTP endpoint
- ✅ Fix `1b82672` — repo symmetry/idempotency tests (Verifier EXVAR-02)
- ✅ T10 — align ExerciseEquivalents persistence with orphan-row spec

Quick gate closing: **410 passed**, 0 failed, 0 skipped.

---

## Test Coverage Matrix

> Guidelines found: `src/AGENTS.md` (FluentValidation, Result, testability). Strong default for handler ACs. Samples: `CreateWorkoutPlanHandlerTests`, `UpdateWorkoutExecutionStateHandlerTests`.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Entity `ExerciseEquivalent` + EF config + migration | none | Schema only — build gate | `Shared/Entities/`, `TrainingDbContext`, Migrations | `dotnet build src/ShapeUp.slnx` |
| `IExerciseEquivalentRepository` impl | none | Exercised via handlers; no standalone repo test infra today | `Infrastructure/Repositories/` | build |
| Set/Remove/Get equivalent handlers (+ validators) | unit | 1:1 EXVAR-01/02/03/09: set persists, self reject, missing 404, muscle warning bool, symmetry via get both sides, remove idempotent, duplicate set idempotent | `tests/UnitTests/Domains/Training/Exercises/*` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| SwapExerciseInSession handler (+ validator) | unit | EXVAR-06/07: append new, retain sets, reject non-equivalent, reject duplicate in session, reject completed/cancelled, 404 session | `tests/UnitTests/Domains/Training/Workouts/*` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Controllers | none | Thin wiring; handlers covered by unit (same convention as WEV) | `ExercisesController`, `WorkoutsController` | build |

## Parallelism Assessment

| Test Type | Parallel-Safe? | Isolation Model | Evidence |
|---|---|---|---|
| unit | Yes | Moq per test | existing Training handler tests |

## Gate Check Commands

| Gate Level | When to Use | Command |
|---|---|---|
| Quick | After handler tasks | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Full | Unit suite + build + migration apply | `dotnet test tests/UnitTests/UnitTests.csproj && dotnet build src/ShapeUp.slnx`; then apply pending Training migrations against SQL Server |
| Build | Entity/migration/repo/controller wiring | `dotnet build src/ShapeUp.slnx` |

---

## Execution Plan

### Phase 1: Data foundation

```
T1 → T2 → T3
```

### Phase 2: Equivalents catalog API

```
T4 → T7
T5 → T7
T6 → T7
```

### Phase 3: Session swap

```
T8 → T9
```

### Phase 4: Migration compatibility fix

```
T10
```

---

## Task Breakdown

### Phase 1: Data foundation

### T1: Add `ExerciseEquivalent` entity + EF mapping

**What**: Entity with composite key `(ExerciseId, EquivalentExerciseId)`, FKs cascade, `CreatedAtUtc`; `DbSet` + `OnModelCreating` (invariant `ExerciseId < EquivalentExerciseId` enforced in repo, not DB check).
**Where**: `Shared/Entities/ExerciseEquivalent.cs`, `TrainingDbContext.cs`
**Depends on**: None
**Requirement**: EXVAR-01, EXVAR-02
**Tests**: none
**Gate**: build
**Commit**: `feat(training): add ExerciseEquivalent entity and EF mapping`

---

### T2: EF migration for `ExerciseEquivalents`

**What**: Add migration creating table + FKs.
**Where**: `Infrastructure/Data/Migrations/`
**Depends on**: T1
**Requirement**: EXVAR-01
**Tests**: none
**Gate**: build
**Commit**: `feat(training): migrate ExerciseEquivalents table`

---

### T3: `IExerciseEquivalentRepository` + implementation + DI

**What**: `GetEquivalentsAsync`, `SetEquivalentAsync` (canonicalize min/max, idempotent), `RemoveEquivalentAsync` (idempotent); register in `TrainingModule`.
**Where**: `Shared/Abstractions/`, `Infrastructure/Repositories/`, `TrainingModule.cs`
**Depends on**: T2
**Requirement**: EXVAR-02, EXVAR-09
**Tests**: none
**Gate**: build
**Commit**: `feat(training): add ExerciseEquivalent repository`

---

### Phase 2: Equivalents catalog API

### T4: SetExerciseEquivalent handler + tests

**What**: Command/validator/handler — reject self, 404 missing, persist via repo, response includes `MuscleGroupOverlapWarning` (true when no shared `MuscleGroup`).
**Where**: `Exercises/SetExerciseEquivalent/`, tests
**Depends on**: T3
**Requirement**: EXVAR-01, EXVAR-03
**Tests**: unit
**Gate**: quick
**Commit**: `feat(training): set exercise equivalent with muscle-overlap warning`

---

### T5: RemoveExerciseEquivalent handler + tests

**What**: Remove relation both directions (one canonical row); idempotent success.
**Where**: `Exercises/RemoveExerciseEquivalent/`, tests
**Depends on**: T3
**Requirement**: EXVAR-09
**Tests**: unit
**Gate**: quick
**Commit**: `feat(training): remove exercise equivalent idempotently`

---

### T6: GetExerciseEquivalents handler + tests

**What**: List equivalents as `ExerciseResponse[]` via `MapResponse`; empty list ok; deleted/missing filtered by join.
**Where**: `Exercises/GetExerciseEquivalents/`, tests
**Depends on**: T3
**Requirement**: EXVAR-02 (symmetry read), EXVAR-01
**Tests**: unit
**Gate**: quick
**Commit**: `feat(training): get exercise equivalents list`

---

### T7: Wire equivalents routes on `ExercisesController` + DI handlers

**What**: `GET/POST/DELETE .../{id}/equivalents[/{otherId}]`; POST/DELETE require `capability:platform.exercises.manage`; register handlers/validators.
**Where**: `ExercisesController.cs`, `TrainingModule.cs`
**Depends on**: T4, T5, T6
**Requirement**: EXVAR-01, EXVAR-09
**Tests**: none
**Gate**: build
**Commit**: `feat(training): expose exercise equivalents HTTP endpoints`

---

### Phase 3: Session swap

### T8: SwapExerciseInSession handler + tests

**What**: Command with `RetainedSetsForOriginal`; validate equivalent; reject if new already in session / completed / cancelled; replace original sets; append new exercise (`RequireRpe=false`); add `TrainingErrors.ExerciseAlreadyInSession`.
**Where**: `Workouts/SwapExerciseInSession/`, `TrainingErrors.cs`, tests
**Depends on**: T3
**Requirement**: EXVAR-06, EXVAR-07
**Tests**: unit
**Gate**: quick
**Commit**: `feat(training): swap exercise in active workout session`

---

### T9: Wire `POST .../workouts/{sessionId}/swap-exercise` + DI

**What**: Route on `WorkoutsController`; register handler/validator.
**Where**: `WorkoutsController.cs`, `TrainingModule.cs`
**Depends on**: T8
**Requirement**: EXVAR-06
**Tests**: none
**Gate**: build (+ quick full unit suite)
**Commit**: `feat(training): expose swap-exercise HTTP endpoint`

---

### Phase 4: Migration compatibility fix

### T10: Preserve orphan equivalence rows without SQL Server cascade paths

**What**: Remove database FK constraints from `ExerciseEquivalents` while keeping the composite key and lookup index. This matches the spec edge case: hard-deleting exercises does not clean equivalence rows, and reads omit missing peers.
**Where**: `ExerciseEquivalent.cs`, `TrainingDbContext.cs`, Training migration metadata, `ExerciseEquivalentRepositoryTests.cs`, feature design/validation
**Depends on**: T2
**Requirement**: EXVAR-01 edge case (deleted equivalents remain orphaned and are omitted from reads)
**Tests**: unit + migration apply
**Gate**: full (unit suite + build + real SQL Server migration apply)
**Commit**: `fix(training): preserve orphan exercise equivalence rows`

---

## Diagram-Definition Cross-Check

| Task | Depends On | Diagram | Status |
|---|---|---|---|
| T1 | None | start | ✅ |
| T2 | T1 | T1→T2 | ✅ |
| T3 | T2 | T2→T3 | ✅ |
| T4 | T3 | Phase2 | ✅ |
| T5 | T3 | Phase2 | ✅ |
| T6 | T3 | Phase2 | ✅ |
| T7 | T4,T5,T6 | after T4–T6 | ✅ |
| T8 | T3 | Phase3 | ✅ |
| T9 | T8 | T8→T9 | ✅ |
| T10 | T2 | Phase4 | ✅ |

## Test Co-location Validation

| Task | Matrix | Task Says | Status |
|---|---|---|---|
| T1–T3, T7, T9 | none | none | ✅ |
| T4–T6, T8 | unit | unit | ✅ |
| T10 | unit + migration apply | unit + migration apply | ✅ |

## Granularity Check

All tasks ≤ one cohesive deliverable (entity / migration / repo / one handler path / controller wiring). ✅
