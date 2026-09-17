# Validação de Execução de Treino — Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Design**: `.specs/features/workout-execution-validation/design.md`
**Status**: Verified PASS (backend scope) — see `validation.md`
**Scope**: Backend only (`ShapeUpApi`). Covers WEV-02, WEV-05, WEV-06, WEV-07, WEV-08 **[Backend]** halves. WEV-01/03/04 and the **[Frontend]** halves of WEV-05/06/07/08 are out of scope (handoff to `ShapeUp-Web`, see design.md).

## Progress Log

- ✅ T1 `f62394d` — RequireRpe on BlockExerciseDocumentValueObject
- ✅ T2 `e070abb` — RequireRpe on ExecutedExerciseDocumentValueObject
- ✅ T3 `4fb0059` — RequireRpe on WorkoutExerciseDto
- ✅ T4 `490f1aa` — WorkoutPlan create persists/returns RequireRpe
- ✅ T5 `e42c896` — WorkoutPlan update persists/returns RequireRpe
- ✅ T6 `a525906` — WorkoutTemplate create persists/returns RequireRpe
- ✅ T7 `50a1707` — WorkoutTemplate update persists/returns RequireRpe
- ✅ T8 `577e617` — RequireRpe carried into execution snapshot on Start
- ✅ T9 `ac78bec` — WorkoutExerciseDtoValidator extracted, RPE optional by default
- ✅ T10 `5314f69` — UpdateWorkoutExecutionState validator no longer requires Intensity unconditionally
- ✅ T11 `91eae7f` — TrainingErrors.RpeRequiredForExercise added
- ✅ T12 `01f52bf` — RequireRpe gate enforced on UpdateWorkoutExecutionState
- ✅ T13 `9a3ced8` — RequireRpe gate + set-format validation enforced on FinishWorkoutExecution

Batch 1 gate: 355 passed. Batch 2 gate: 382 passed, 0 failed, 0 skipped (net +27 across T8-T13). Full suite green.

---

## Test Coverage Matrix

> Generated from codebase (`AGENTS.md`, existing test samples in `tests/UnitTests/Domains/Training/`), spec, and design. No explicit coverage-threshold guideline found in `AGENTS.md` — strong default applied. Guidelines found: `src/AGENTS.md` (validation/testability conventions, no numeric threshold).

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Domain VOs/DTOs — new `RequireRpe` field (`BlockExerciseDocumentValueObject`, `ExecutedExerciseDocumentValueObject`, `WorkoutExerciseDto`) | none | Additive field with safe default, no branching logic — build gate only | `src/Features/Training/Shared/Documents/ValueObjects/*.cs`, `src/Features/Training/Workouts/Shared/Dtos/WorkoutExerciseDto.cs` | `dotnet build src/ShapeUp.slnx` |
| Validators (`WorkoutExerciseDtoValidator` new, `UpdateWorkoutExecutionStateCommandValidator`, `FinishWorkoutExecutionCommandValidator`) | unit | All branches; 1:1 to WEV-02/WEV-07/WEV-08 ACs; every listed edge case (reps null/`<=0`, load negative, load=0 & reps>=1, intensity absent-by-default, intensity out of 1–10) | `tests/UnitTests/Domains/Training/Workouts/*ValidatorTests.cs` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Handlers (`CreateWorkoutPlanHandler`, `UpdateWorkoutPlanHandler`, `CreateWorkoutTemplateHandler`, `UpdateWorkoutTemplateHandler`, `StartWorkoutExecutionHandler`, `UpdateWorkoutExecutionStateHandler`, `FinishWorkoutExecutionHandler`) | unit | All branches; 1:1 to WEV-05/WEV-06/WEV-07 ACs; every listed edge case (`RequireRpe` persisted/read back, frozen at Start per AD-007, gate 400 vs 200, existing behavior not regressed) | `tests/UnitTests/Domains/Training/{WorkoutPlans,WorkoutTemplates,Workouts}/*HandlerTests.cs` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Defensive mapping fixes with zero pre-existing coverage (`WorkoutPlanMappings.Clone`, `CopyWorkoutTemplateHandler` inline clone) | none | Pre-existing Copy flows have no test infrastructure anywhere in the repo today (confirmed via search) — one-line fix to prevent `RequireRpe` silently resetting to `false` on copy; introducing new Copy test scaffolding is out of spec scope | `src/Features/Training/WorkoutPlans/Shared/WorkoutPlanMappings.cs`, `src/Features/Training/WorkoutTemplates/CopyWorkoutTemplate/CopyWorkoutTemplateHandler.cs` | `dotnet build src/ShapeUp.slnx` |
| Integration/Endpoints | none (unaffected) | No new endpoints added; existing HTTP-level auth/routing coverage (`WorkoutExecutionEndpointsTests.cs`, `WorkoutsEndpointsIntegrationTests.cs`) is untouched by this feature. The spec's "Independent Test" scenarios phrased in HTTP terms are equivalently exercised by handler-level `Result`/status-code assertions, matching this domain's existing convention (validators have no dedicated integration coverage — they're proven through handler tests) | `tests/IntegrationTests/Domains/Training/**/*.cs` | `dotnet test src/ShapeUp.slnx` |

## Gate Check Commands

> Generated from `tests/UnitTests/UnitTests.csproj`, `tests/IntegrationTests/IntegrationTests.csproj`, `src/ShapeUp.slnx` — confirm before Execute.

| Gate Level | When to Use | Command |
|---|---|---|
| Quick | After every task in this feature (all touched layers are unit-tested or build-gate-only) | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Full | Not required for this feature (no integration test changes) — available as a fallback if a task uncovers a real HTTP-level gap | `dotnet test src/ShapeUp.slnx` |
| Build | After VO/DTO-only tasks (T1–T3) and the defensive mapping fixes folded into T4/T6 | `dotnet build src/ShapeUp.slnx` |

---

## Execution Plan

Phases are ordered and run sequentially — each phase completes before the next begins, and tasks within a phase execute in order.

### Phase 1: Data Model Foundation

```
T1 → T2 → T3
```

### Phase 2: Authoring Persistence (Plans & Templates)

```
T4 → T5 → T6 → T7
```

### Phase 3: Execution Snapshot & Shared Validation Infra

```
T8 → T9 → T10 → T11
```

### Phase 4: Execution Gate (Update & Finish handlers)

```
T12 → T13
```

---

## Task Breakdown

### T1: Add `RequireRpe` to `BlockExerciseDocumentValueObject`

**What**: Add `public bool RequireRpe { get; set; } = false;` to the authoring-side exercise VO.
**Where**: `src/Features/Training/Shared/Documents/ValueObjects/BlockExerciseDocumentValueObject.cs`
**Depends on**: None
**Reuses**: existing class shape (`ExerciseId`, `ExerciseName`, `StrengthGainPercentage`, `Sets`)
**Requirement**: WEV-05

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `RequireRpe` property added with default `false`
- [ ] `dotnet build src/ShapeUp.slnx` succeeds with 0 errors

**Tests**: none
**Gate**: build

**Commit**: `feat(training): add RequireRpe field to BlockExerciseDocumentValueObject`

---

### T2: Add `RequireRpe` to `ExecutedExerciseDocumentValueObject`

**What**: Add `public bool RequireRpe { get; set; } = false;` to the execution-side exercise VO (snapshot).
**Where**: `src/Features/Training/Shared/Documents/ValueObjects/ExecutedExerciseDocumentValueObject.cs`
**Depends on**: None
**Reuses**: existing class shape
**Requirement**: WEV-05

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `RequireRpe` property added with default `false`
- [ ] `dotnet build src/ShapeUp.slnx` succeeds with 0 errors

**Tests**: none
**Gate**: build

**Commit**: `feat(training): add RequireRpe field to ExecutedExerciseDocumentValueObject`

---

### T3: Add `RequireRpe` to `WorkoutExerciseDto`

**What**: Append `bool RequireRpe = false` as the last positional parameter of the shared exercise DTO (used by both authoring and execution commands).
**Where**: `src/Features/Training/Workouts/Shared/Dtos/WorkoutExerciseDto.cs`
**Depends on**: None
**Reuses**: existing record — new parameter has a default, so it's backward compatible with every existing call site
**Requirement**: WEV-05

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `RequireRpe` positional parameter added with default `false`
- [ ] `dotnet build src/ShapeUp.slnx` succeeds with 0 errors (confirms no call site broke)

**Tests**: none
**Gate**: build

**Commit**: `feat(training): add RequireRpe field to WorkoutExerciseDto`

---

### T4: Wire `RequireRpe` through WorkoutPlan create + response mapping

**What**: `CreateWorkoutPlanHandler` maps `exerciseInput.RequireRpe` into the new `BlockExerciseDocumentValueObject.RequireRpe` when persisting; `WorkoutPlanMappings.ToResponse` carries `e.RequireRpe` into the response `WorkoutExerciseDto`; `WorkoutPlanMappings.Clone` (used by `CopyWorkoutPlanHandler`) also carries `e.RequireRpe` (defensive fix — without it, copying a plan would silently reset `RequireRpe` to `false`, a data-loss bug this feature would otherwise introduce).
**Where**: `src/Features/Training/WorkoutPlans/CreateWorkoutPlan/CreateWorkoutPlanHandler.cs`, `src/Features/Training/WorkoutPlans/Shared/WorkoutPlanMappings.cs`
**Depends on**: T1, T3
**Reuses**: existing exercise-mapping loop in the handler; existing `ToResponse`/`Clone` extension methods
**Requirement**: WEV-05, WEV-06

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Creating a plan with an exercise where `RequireRpe = true` persists that value on the stored `BlockExerciseDocumentValueObject`
- [ ] The response DTO returned from create reflects `RequireRpe` per exercise (via `ToResponse`)
- [ ] `WorkoutPlanMappings.Clone` copies `RequireRpe` (no dedicated test — see Test Coverage Matrix note on Copy flows)
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes, `CreateWorkoutPlanHandlerTests.cs` test count increases by at least 2 (RequireRpe true + false/default cases)

**Tests**: unit — extend `tests/UnitTests/Domains/Training/WorkoutPlans/CreateWorkoutPlanHandlerTests.cs`
**Gate**: quick

**Commit**: `feat(training): persist and return RequireRpe on WorkoutPlan create`

---

### T5: Wire `RequireRpe` through WorkoutPlan update

**What**: `UpdateWorkoutPlanHandler` maps `exerciseInput.RequireRpe` into `BlockExerciseDocumentValueObject.RequireRpe` the same way as T4's create path.
**Where**: `src/Features/Training/WorkoutPlans/UpdateWorkoutPlan/UpdateWorkoutPlanHandler.cs`
**Depends on**: T4
**Reuses**: `WorkoutPlanMappings.ToResponse` already fixed in T4
**Requirement**: WEV-05, WEV-06

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Updating a plan persists `RequireRpe` per exercise (including flipping `true` → `false` and vice versa on re-save)
- [ ] Response DTO reflects the updated value
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes, `UpdateWorkoutPlanHandlerTests.cs` test count increases by at least 2

**Tests**: unit — extend `tests/UnitTests/Domains/Training/WorkoutPlans/UpdateWorkoutPlanHandlerTests.cs`
**Gate**: quick

**Commit**: `feat(training): persist and return RequireRpe on WorkoutPlan update`

---

### T6: Wire `RequireRpe` through WorkoutTemplate create + response mapping + Copy defensive fix

**What**: Same as T4 but for templates: `CreateWorkoutTemplateHandler` maps `RequireRpe`; `WorkoutTemplateMappings.ToBlockDtos` (shared by `ToResponse`/`ToPlanResponse`) carries it into the response DTO; `CopyWorkoutTemplateHandler`'s inline clone (it does not reuse `WorkoutTemplateMappings`) also carries `RequireRpe` defensively.
**Where**: `src/Features/Training/WorkoutTemplates/CreateWorkoutTemplate/CreateWorkoutTemplateHandler.cs`, `src/Features/Training/WorkoutTemplates/Shared/WorkoutTemplateMappings.cs`, `src/Features/Training/WorkoutTemplates/CopyWorkoutTemplate/CopyWorkoutTemplateHandler.cs`
**Depends on**: T1, T3
**Reuses**: existing exercise-mapping loop, existing `ToBlockDtos` helper
**Requirement**: WEV-05, WEV-06

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Creating a template with `RequireRpe = true` on an exercise persists and returns it
- [ ] `CopyWorkoutTemplateHandler` copies `RequireRpe` (no dedicated test — same pre-existing gap noted in T4)
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes, `CreateWorkoutTemplateHandlerTests.cs` test count increases by at least 2

**Tests**: unit — extend `tests/UnitTests/Domains/Training/WorkoutTemplates/CreateWorkoutTemplateHandlerTests.cs`
**Gate**: quick

**Commit**: `feat(training): persist and return RequireRpe on WorkoutTemplate create`

---

### T7: Wire `RequireRpe` through WorkoutTemplate update

**What**: `UpdateWorkoutTemplateHandler` maps `exerciseInput.RequireRpe` the same way as T6's create path.
**Where**: `src/Features/Training/WorkoutTemplates/UpdateWorkoutTemplate/UpdateWorkoutTemplateHandler.cs`
**Depends on**: T6
**Reuses**: `WorkoutTemplateMappings.ToBlockDtos` already fixed in T6
**Requirement**: WEV-05, WEV-06

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Updating a template persists `RequireRpe` per exercise, including flips in both directions
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes, `UpdateWorkoutTemplateHandlerTests.cs` test count increases by at least 2

**Tests**: unit — extend `tests/UnitTests/Domains/Training/WorkoutTemplates/UpdateWorkoutTemplateHandlerTests.cs`
**Gate**: quick

**Commit**: `feat(training): persist and return RequireRpe on WorkoutTemplate update`

---

### T8: Flatten `RequireRpe` into the execution session snapshot at Start

**What**: `StartWorkoutExecutionHandler`'s flatten step copies `e.RequireRpe` from `BlockExerciseDocumentValueObject` into the new `ExecutedExerciseDocumentValueObject.RequireRpe` (frozen at session start, per AD-007 — later plan edits don't retroactively affect an in-progress session).
**Where**: `src/Features/Training/Workouts/StartWorkoutExecution/StartWorkoutExecutionHandler.cs`
**Depends on**: T1, T2
**Reuses**: existing `.SelectMany(b => b.Exercises).Select(e => new ExecutedExerciseDocumentValueObject {...})` flatten
**Requirement**: WEV-05 (AC2)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Starting a session from a plan with an exercise `RequireRpe = true` produces a session snapshot where that exercise's `ExecutedExerciseDocumentValueObject.RequireRpe == true`
- [ ] An exercise without the flag remains `false` in the snapshot
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes, `StartWorkoutExecutionHandlerTests.cs` test count increases by at least 1

**Tests**: unit — extend `tests/UnitTests/Domains/Training/Workouts/StartWorkoutExecutionHandlerTests.cs`
**Gate**: quick

**Commit**: `feat(training): carry RequireRpe into execution session snapshot on Start`

---

### T9: Create `WorkoutExerciseDtoValidator` (shared per-exercise/per-set format rules)

**What**: New reusable child validator extracting the existing per-set rules from `UpdateWorkoutExecutionStateCommandValidator` (`ExerciseId > 0`, `Sets.NotEmpty()`, `Repetitions` not-null + `> 0`, `Load >= 0`, `LoadUnit`/`SetType` in enum, `Intensity.Value` `InclusiveBetween(1,10)` when present, `RestSeconds` not-null + `>= 0`) — deliberately WITHOUT the unconditional `Intensity.NotNull()` rule (that becomes optional-by-default; the RequireRpe-conditional requirement is enforced later, at handler level, per AD-005 — see T12/T13).
**Where**: `src/Features/Training/Workouts/Shared/Dtos/WorkoutExerciseDtoValidator.cs` (new file)
**Depends on**: None
**Reuses**: rule bodies already written in `UpdateWorkoutExecutionStateCommandValidator` (moved, not reinvented)
**Requirement**: WEV-02, WEV-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `WorkoutExerciseDtoValidator : AbstractValidator<WorkoutExerciseDto>` created with the rules listed above
- [ ] Direct validator tests (no HTTP, no handler) cover: `Repetitions = null` → invalid; `Repetitions = 0`/negative → invalid; `Load` negative → invalid; `Load = 0` & `Repetitions = 1` → valid; `Intensity = null` → valid (no NotNull rule); `Intensity.Value = 0` and `= 11` → invalid; `Intensity.Value = 1` and `= 10` → valid; `ExerciseId <= 0` → invalid; `Sets` empty → invalid
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes, new `WorkoutExerciseDtoValidatorTests.cs` has at least 9 test cases

**Tests**: unit — new `tests/UnitTests/Domains/Training/Workouts/WorkoutExerciseDtoValidatorTests.cs`
**Gate**: quick

**Commit**: `feat(training): extract WorkoutExerciseDtoValidator with RPE optional by default`

---

### T10: Rewire `UpdateWorkoutExecutionStateCommandValidator` to delegate to `WorkoutExerciseDtoValidator`

**What**: Replace the inline `RuleForEach(x => x.Exercises).ChildRules(...)` block with `RuleForEach(x => x.Exercises).SetValidator(new WorkoutExerciseDtoValidator())`, removing the duplicated rules and — critically — the unconditional `Intensity.NotNull()` (the actual bug fix: RPE stops being mandatory for every set by default).
**Where**: `src/Features/Training/Workouts/UpdateWorkoutExecutionState/UpdateWorkoutExecutionStateCommandValidator.cs`
**Depends on**: T9
**Reuses**: `WorkoutExerciseDtoValidator` from T9
**Requirement**: WEV-02, WEV-07, WEV-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Validator delegates exercise/set rules to `WorkoutExerciseDtoValidator`
- [ ] A command with a set `Intensity = null` is now VALID at the command-validator level (bug fix — previously always rejected)
- [ ] A command with `Repetitions = null` or `Load < 0` is still rejected (regression check)
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes; new `UpdateWorkoutExecutionStateCommandValidatorTests.cs` covers both cases above directly against the command validator

**Tests**: unit — new `tests/UnitTests/Domains/Training/Workouts/UpdateWorkoutExecutionStateCommandValidatorTests.cs`
**Gate**: quick

**Commit**: `fix(training): stop requiring Intensity unconditionally in UpdateWorkoutExecutionState`

---

### T11: Add `TrainingErrors.RpeRequiredForExercise`

**What**: New static error factory `RpeRequiredForExercise(int exerciseId) => CommonErrors.Validation(...)` for the RequireRpe gate's 400 response.
**Where**: `src/Features/Training/Shared/Errors/TrainingErrors.cs`
**Depends on**: None
**Reuses**: existing `CommonErrors.Validation` pattern already used by every other error in this file
**Requirement**: WEV-07

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `RpeRequiredForExercise(int exerciseId)` added, message identifies the exercise
- [ ] `dotnet build src/ShapeUp.slnx` succeeds with 0 errors (exercised for real in T12/T13's handler tests)

**Tests**: none (trivial factory; behavior verified where it's consumed — T12, T13)
**Gate**: build

**Commit**: `feat(training): add RpeRequiredForExercise error`

---

### T12: `UpdateWorkoutExecutionStateHandler` — RequireRpe gate + null-safe Intensity mapping

**What**: Three coupled fixes in the same handler: (1) replace the force-unwrap `Intensity = new IntensityDocumentValueObject { Type = s.Intensity!.Type, ... }` with the null-safe pattern already used in `StartWorkoutExecutionHandler` (`s.Intensity is null ? null : new IntensityDocumentValueObject {...}`) — required now that `Intensity` can legitimately be null; (2) after fetching `session`, for each incoming exercise look up `session.Exercises.FirstOrDefault(x => x.ExerciseId == exerciseInput.ExerciseId)?.RequireRpe ?? false` and, if `true` and any of that exercise's incoming sets has `Intensity == null`, return `Result<WorkoutSessionResponse>.Failure(TrainingErrors.RpeRequiredForExercise(exerciseInput.ExerciseId))` before any persistence; (3) carry that looked-up `RequireRpe` value into the newly built `ExecutedExerciseDocumentValueObject` (otherwise the flag would be lost on the next save, since this handler rebuilds the exercise from the catalog, not from the existing snapshot).
**Where**: `src/Features/Training/Workouts/UpdateWorkoutExecutionState/UpdateWorkoutExecutionStateHandler.cs`
**Depends on**: T2, T10, T11
**Reuses**: null-safe `Intensity` mapping pattern from `StartWorkoutExecutionHandler.cs`; `session.Exercises.FirstOrDefault(...)` fallback pattern already used for `ExerciseName` in `FinishWorkoutExecutionHandler.cs:48`
**Requirement**: WEV-07

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Exercise with `RequireRpe = true` in the session snapshot and an incoming set with `Intensity = null` → 400 via `RpeRequiredForExercise`, no persistence call made
- [ ] Same exercise with `Intensity` present → 200, persisted
- [ ] Exercise with `RequireRpe = false` (default) and `Intensity = null` → 200, persisted (regression fix for the pre-existing bug)
- [ ] Persisted `ExecutedExerciseDocumentValueObject.RequireRpe` still reflects the session snapshot's value after the update (not reset to `false`)
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes, `UpdateWorkoutExecutionStateHandlerTests.cs` test count increases by at least 3

**Tests**: unit — extend `tests/UnitTests/Domains/Training/Workouts/UpdateWorkoutExecutionStateHandlerTests.cs`
**Gate**: quick

**Commit**: `feat(training): enforce RequireRpe gate on UpdateWorkoutExecutionState`

---

### T13: `FinishWorkoutExecutionHandler` — same gate/mapping fixes + wire `WorkoutExerciseDtoValidator` into Finish

**What**: Mirror T12's three fixes in `FinishWorkoutExecutionHandler` (null-safe `Intensity` mapping, RequireRpe gate before persistence, carry `RequireRpe` forward). Additionally — closing the defense-in-depth gap identified in design.md's Risks & Concerns — add `RuleForEach(x => x.Exercises).SetValidator(new WorkoutExerciseDtoValidator()).When(x => x.Exercises is not null)` to `FinishWorkoutExecutionCommandValidator`, which today validates none of `Exercises`' contents at all.
**Where**: `src/Features/Training/Workouts/FinishWorkoutExecution/FinishWorkoutExecutionHandler.cs`, `src/Features/Training/Workouts/FinishWorkoutExecution/FinishWorkoutExecutionCommandValidator.cs`
**Depends on**: T9, T11, T12
**Reuses**: same patterns as T12; `WorkoutExerciseDtoValidator` from T9
**Requirement**: WEV-02, WEV-07

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Same three gate/mapping behaviors as T12, verified against `FinishWorkoutExecutionHandler`'s own exercise-mapping block
- [ ] `FinishWorkoutExecutionCommand` with a set `Repetitions = null` or `Load < 0` in `Exercises` → 400 (NEW — previously accepted unconditionally, the gap this task closes)
- [ ] `FinishWorkoutExecutionCommand` with `Exercises = null` (the common case — most sessions finish without re-sending exercises) is unaffected — validator rule only applies `.When(x => x.Exercises is not null)`
- [ ] `dotnet test tests/UnitTests/UnitTests.csproj` passes, `FinishWorkoutExecutionHandlerTests.cs` test count increases by at least 4

**Tests**: unit — extend `tests/UnitTests/Domains/Training/Workouts/FinishWorkoutExecutionHandlerTests.cs`
**Gate**: quick

**Commit**: `feat(training): enforce RequireRpe gate and set-format validation on FinishWorkoutExecution`

---

## Phase Execution Map

```
Phase 1 → Phase 2 → Phase 3 → Phase 4

Phase 1:  T1 ──→ T2 ──→ T3
Phase 2:  T4 ──→ T5 ──→ T6 ──→ T7
Phase 3:  T8 ──→ T9 ──→ T10 ──→ T11
Phase 4:  T12 ──→ T13
```

Execution is strictly sequential — there is no intra-phase parallelism. A single agent (or batch worker) works one task at a time, in order.

**Packing into batches** (~7 tasks/worker, whole phases only): 13 tasks total → 2 batches.
- **Batch 1**: Phase 1 + Phase 2 (T1–T7, 7 tasks)
- **Batch 2**: Phase 3 + Phase 4 (T8–T13, 6 tasks)

Per the skill's sub-agent delegation rule (>~8 tasks total → offer sub-agents), this will be offered before Execute begins.

---

## Task Granularity Check

| Task | Scope | Status |
|---|---|---|
| T1: Add RequireRpe to BlockExerciseDocumentValueObject | 1 field, 1 file | ✅ Granular |
| T2: Add RequireRpe to ExecutedExerciseDocumentValueObject | 1 field, 1 file | ✅ Granular |
| T3: Add RequireRpe to WorkoutExerciseDto | 1 field, 1 file | ✅ Granular |
| T4: Wire RequireRpe through WorkoutPlan create + response mapping | 2 cohesive files (handler + its own mapping helper), 1 deliverable ("Plan create round-trips RequireRpe") | ✅ Granular (cohesive, see Tips: "2-3 related things in same file/deliverable = OK") |
| T5: Wire RequireRpe through WorkoutPlan update | 1 file | ✅ Granular |
| T6: Wire RequireRpe through WorkoutTemplate create + mapping + Copy fix | 3 cohesive files, 1 deliverable ("Template create/copy round-trips RequireRpe") | ✅ Granular (cohesive) |
| T7: Wire RequireRpe through WorkoutTemplate update | 1 file | ✅ Granular |
| T8: Flatten RequireRpe into execution snapshot at Start | 1 file, 1 mapping step | ✅ Granular |
| T9: Create WorkoutExerciseDtoValidator | 1 new file, 1 concept | ✅ Granular |
| T10: Rewire UpdateWorkoutExecutionStateCommandValidator | 1 file | ✅ Granular |
| T11: Add TrainingErrors.RpeRequiredForExercise | 1 method, 1 file | ✅ Granular |
| T12: UpdateWorkoutExecutionStateHandler gate + mapping fix | 1 file, 1 deliverable (3 coupled fixes that can't be split without breaking compilation/tests — see design.md Risks) | ✅ Granular (tight dependency chain, not splittable) |
| T13: FinishWorkoutExecutionHandler gate + mapping fix + validator wiring | 2 cohesive files, 1 deliverable (mirrors T12 + closes the Finish validation gap) | ✅ Granular (cohesive) |

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
|---|---|---|---|
| T1 | None | (start of Phase 1) | ✅ Match |
| T2 | None | T1 → T2 | ✅ Match (sequential slot, no real dependency) |
| T3 | None | T2 → T3 | ✅ Match (sequential slot, no real dependency) |
| T4 | T1, T3 | Phase 2 starts after Phase 1 | ✅ Match |
| T5 | T4 | T4 → T5 | ✅ Match |
| T6 | T1, T3 | T5 → T6 | ✅ Match (sequential slot; real dependency is on Phase 1, satisfied) |
| T7 | T6 | T6 → T7 | ✅ Match |
| T8 | T1, T2 | Phase 3 starts after Phase 2 | ✅ Match |
| T9 | None | T8 → T9 | ✅ Match (sequential slot, no real dependency) |
| T10 | T9 | T9 → T10 | ✅ Match |
| T11 | None | T10 → T11 | ✅ Match (sequential slot, no real dependency) |
| T12 | T2, T10, T11 | Phase 4 starts after Phase 3 | ✅ Match |
| T13 | T9, T11, T12 | T12 → T13 | ✅ Match |

No task depends on a task in a later phase — all dependencies point backward or within the same phase.

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
|---|---|---|---|---|
| T1 | Domain VO | none | none | ✅ OK |
| T2 | Domain VO | none | none | ✅ OK |
| T3 | Domain DTO | none | none | ✅ OK |
| T4 | Handler + Mapping | unit (handler) / none (mapping) | unit | ✅ OK |
| T5 | Handler | unit | unit | ✅ OK |
| T6 | Handler + Mapping | unit (handler) / none (mapping) | unit | ✅ OK |
| T7 | Handler | unit | unit | ✅ OK |
| T8 | Handler | unit | unit | ✅ OK |
| T9 | Validator (new) | unit | unit | ✅ OK |
| T10 | Validator | unit | unit | ✅ OK |
| T11 | Error factory | none | none | ✅ OK |
| T12 | Handler | unit | unit | ✅ OK |
| T13 | Handler + Validator | unit | unit | ✅ OK |

No violations — every unit-required layer has co-located test work in the same task; no task defers tests to a later task.

---

## Tools for Execution

Given the check above, no MCP or skill beyond `tlc-spec-driven` itself is needed for any task — this is pure C#/.NET code + xUnit tests in an already-established codebase pattern (no new library, no unfamiliar API, no external integration).
