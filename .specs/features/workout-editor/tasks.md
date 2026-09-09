# Editor de Treino — Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Design**: `ShapeUpApi/.specs/features/workout-editor/design.md`
**Status**: Draft

---

## Test Coverage Matrix

> Generated from codebase (`ShapeUpApi/src/AGENTS.md` — guideline found, no numeric coverage threshold; existing test samples in `ShapeUpApi/tests/`) and spec. Confirm before Execute.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Enums / Value Objects / DTOs (Block, Intensity, updated Set) | none | Build gate only — no branching logic | `ShapeUpApi/src/Features/Training/Shared/{Enums,Documents}/**`, `Workouts/Shared/{Dtos,ValueObjects}/**` | `dotnet build ShapeUpApi/src/ShapeUp.csproj` |
| Command Validators (Superset/Amrap/Emom rules, Intensity optional) | unit | All branches; 1:1 to WOED-01..08 ACs; every listed edge case has a test | `ShapeUpApi/tests/UnitTests/Domains/Training/**/*HandlerTests.cs` (validator exercised via handler — existing pattern, e.g. `CreateWorkoutPlanHandlerTests.cs`) | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| Command Handlers (Create/Update Plan & Template, Execution Intensity rename) | unit | All branches; 1:1 to ACs | `ShapeUpApi/tests/UnitTests/Domains/Training/**` | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| Mappings (`WorkoutPlanMappings`/`WorkoutTemplateMappings` Clone/ToResponse) | unit | Key mapping paths (Block→BlockExercise→Set round-trip, used by Copy) | `ShapeUpApi/tests/UnitTests/Domains/Training/**` | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| Controllers/Endpoints (WorkoutPlans, WorkoutTemplates) | integration | All routes in scope: happy path + every listed edge case + error paths | `ShapeUpApi/tests/IntegrationTests/Domains/Training/Endpoints/*.cs` | `dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` |
| Frontend (`BlockCard`/`ExerciseRow`/`SetRow`/`PlanEditor`/`useTrainingApi`) | none | Build/lint gate only — no test framework exists in `ShapeUp-Web` today (pre-existing gap, tracked in root `GAPS.md`, out of scope for this feature) | `ShapeUp-Web/src/**` | `npm --prefix ShapeUp-Web run lint && npm --prefix ShapeUp-Web run build` |

## Parallelism Assessment

> Generated from codebase — confirm before Execute.

| Test Type | Parallel-Safe? | Isolation Model | Evidence |
|---|---|---|---|
| unit (xUnit + Moq) | Yes | Per-`[Fact]` mocked repositories/validators, no shared fixture or static state | `CreateWorkoutPlanHandlerTests.cs:15-20` — new `Mock<>` instances per test |
| integration (xUnit + `WebApplicationFactory` + SQL Server) | No | `[Collection("SQL Server Write Operations")]` groups tests onto a shared DB fixture — xUnit runs same-collection tests sequentially | `WorkoutPlansEndpointsIntegrationTests.cs:16` |
| frontend build/lint | Yes | Stateless CLI invocations, no shared runtime state | N/A |

## Gate Check Commands

> Generated from codebase — confirm before Execute.

| Gate Level | When to Use | Command |
|---|---|---|
| Quick | After tasks with unit tests only | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| Full | After tasks with integration tests | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` |
| Build | After phase completion, entity-only tasks, or frontend tasks | `dotnet build ShapeUpApi/src/ShapeUp.csproj && npm --prefix ShapeUp-Web run lint && npm --prefix ShapeUp-Web run build` |

---

## Phase 3 status: ✅ Closed (2026-09-08)

T14, T14b, T15 all complete. `dotnet build` (whole solution) clean. **One more gap found**, same class as T7b: running the *full* integration suite (not just the two files T14/T14b targeted) surfaced 17 failures in 3 untouched files — `WorkoutPlanningScopeEndpointsTests.cs`, `WorkoutsEndpointsIntegrationTests.cs`, `TrainingEndpointsIntegrationTests.cs` — all still building workout-plan payloads with the old flat `exercises`/`rpe` shape. Fixed mechanically (wrap in a Straight block, rename `rpe`→`intensity`); `WorkoutsEndpointsIntegrationTests`' execution-state PUT body correctly stayed flat (AD-007), only its field renamed. Commit `0d3000a`. Full integration suite re-run confirmed: **217 total, 210 passed, 7 skipped (pre-existing, unrelated), 0 failed.**

## Phase 2 status: ✅ Closed (2026-09-08)

T7-T13 (+ T7b) all complete. `dotnet build` (whole solution) and `dotnet test` (UnitTests, 236/236) both green. Two real correctness bugs found and fixed along the way (not scope creep — both blocked the gate):
1. **FluentValidation gotcha**: `GreaterThan(0)` alone treats a `null` value as valid (only `NotNull()` fails on null). `TimeCapSeconds`/`IntervalSeconds`/`TotalRounds` validators in all 4 command validators (T7-T10) were missing `.NotNull()`, so an omitted Amrap/Emom field silently passed. Fixed (`0266108`, folded into T9/T10 from the start).
2. **Test mock bug**: `NewSut`'s exercise-repository mock always returned `Exercise{Id=1}` regardless of the requested id, hiding a possible ordering bug in multi-exercise blocks. Fixed to echo the requested id (`96b71f8`).

**Integration tests are currently RED** (`WorkoutPlansEndpointsIntegrationTests`: 10 of 13 failing) — expected and tracked as **T14's job**, not a new gap: those tests still POST the old flat `exercises`/`rpe` JSON shape, which the new `Blocks`-required validator correctly rejects with `400`. Do not treat this as a regression to fix ad-hoc; T14/T14b rewrite these payloads as part of Phase 3.

---

## Execution Plan

### Phase 1: Backend Data Foundation (Sequential, with 2 internal [P] pairs)

```
T1 ─┬→ T3 ─┬→ T4 ─┐
T2 ─┘      └→ T5 ─┴→ T6
```

### Phase 2: Backend Commands / Validators / Handlers / Mappings (Parallel OK after Phase 1)

```
T6 ──┬→ T7  [P]
     ├→ T8  [P]
     ├→ T9  [P]
     ├→ T10 [P]
     ├→ T11 [P]
     └→ T12 [P]
T5 ──→ T13 [P]
T4 ──→ T7b [P]
```

### Phase 3: Backend Integration + Docs (Sequential after Phase 2)

```
T7, T8 ──→ T14 [P]
T9, T10 ─→ T14b [P]
T7..T13, T7b ─→ T15
```

### Phase 4: Frontend (mostly sequential — components build on each other)

```
T15 ──→ T16 ──→ T17 ──→ T18 ──→ T19 ──→ T20 ──→ T21
```

### Phase 5: Final Gate (Sequential)

```
T21 ──→ T22
```

---

## Task Breakdown

### T1: Create `BlockType` enum ✅ Complete (ba98c2c)

**What**: New enum `BlockType { Straight, Superset, Amrap, Emom }`
**Where**: `ShapeUpApi/src/Features/Training/Shared/Enums/BlockType.cs`
**Depends on**: None
**Reuses**: Same file pattern as `Shared/Enums/SetType.cs`, `Technique.cs`
**Requirement**: WOED-01, WOED-04, WOED-06

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Enum defined with 4 values, file-scoped namespace matches directory
- [ ] `dotnet build ShapeUpApi/src/ShapeUp.csproj` passes

**Tests**: none
**Gate**: build

---

### T2: Create `IntensityType` enum + `IntensityDocumentValueObject` [P] ✅ Complete (5e3bc1b)

**What**: New enum `IntensityType { Rpe, Rir }` and new document value object `IntensityDocumentValueObject { IntensityType Type; int Value }`
**Where**: `Shared/Enums/IntensityType.cs`, `Shared/Documents/ValueObjects/IntensityDocumentValueObject.cs`
**Depends on**: None
**Reuses**: `PlannedSetDocumentValueObject.cs` pattern (`[BsonRepresentation(BsonType.String)]` on the enum property)
**Requirement**: WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Both types defined, Mongo-serializable (enum as string)
- [ ] `dotnet build` passes

**Tests**: none
**Gate**: build

---

### T3: Update `PlannedSetDocumentValueObject` — Intensity, nullable Repetitions/RestSeconds ✅ Complete (feba436)

**What**: Replace `Rpe: int` with `Intensity: IntensityDocumentValueObject?`; make `Repetitions` and `RestSeconds` nullable
**Where**: `Shared/Documents/ValueObjects/PlannedSetDocumentValueObject.cs`
**Depends on**: T2
**Reuses**: existing class shape, only 3 properties change
**Requirement**: WOED-04 (AMRAP sets without fixed reps), WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `Rpe` removed, `Intensity` nullable property added
- [ ] `Repetitions: int?`, `RestSeconds: int?`
- [ ] `dotnet build` passes (compile errors elsewhere expected until Phase 2 — acceptable at this checkpoint per task boundary)

**Tests**: none
**Gate**: build

---

### T4: Create `BlockDocumentValueObject`, rename `PlannedExerciseDocumentValueObject`→`BlockExerciseDocumentValueObject`, update plan/template documents ✅ Complete (feba436 rename + 80f2c6f rest)

> **Gap found during T3/T4 gate check (2026-09-08):** whole-project `dotnet build` surfaced 3 call sites to `WorkoutPlanDocument.Exercises`/`WorkoutTemplateDocument.Exercises` not accounted for by any task in this file — none are in `Features/Training`, so they weren't caught by the Phase 2 file scan:
> - `Features/GymManagement/Shared/TrainerClientAdherenceCalculator.cs` (4 sites: lines ~49, 94, 125, 143)
> - `Features/GymManagement/TrainerClients/GetTrainerClients/GetTrainerClientsHandler.cs` (1 site: line ~98)
> - `Features/Training/Workouts/StartWorkoutExecution/StartWorkoutExecutionHandler.cs` (1 site: line ~52) — **design.md was wrong here**: it assumed Execution only touched `WorkoutExerciseDto`/`WorkoutSetValueObject` (confirmed via grep), but `StartWorkoutExecutionHandler` also reads `WorkoutPlanDocument.Exercises` directly (to seed the execution from the plan) — a path the earlier grep didn't check.
>
> None of these are covered by T7-T15. Added as **T7b** below (new task) before Phase 2 is considered complete. `design.md` Risks & Concerns updated to reflect this.

**What**: New `BlockDocumentValueObject { BlockType Type; List<BlockExerciseDocumentValueObject> Exercises; int? TimeCapSeconds; int? IntervalSeconds; int? TotalRounds; int? RestAfterSeconds }`. Rename `PlannedExerciseDocumentValueObject`→`BlockExerciseDocumentValueObject` (same fields). Update `WorkoutPlanDocument.Exercises`→`Blocks: List<BlockDocumentValueObject>` and same in `WorkoutTemplateDocument`
**Where**: `Shared/Documents/ValueObjects/BlockDocumentValueObject.cs` (new), `.../BlockExerciseDocumentValueObject.cs` (renamed from `PlannedExerciseDocumentValueObject.cs`), `Shared/Documents/WorkoutPlanDocument.cs`, `Shared/Documents/WorkoutTemplateDocument.cs`
**Depends on**: T1, T3
**Reuses**: `WorkoutPlanDocument.cs` existing Mongo attributes pattern
**Requirement**: WOED-01, WOED-04, WOED-06

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Both documents expose `Blocks` instead of `Exercises`
- [ ] `dotnet build` passes (Shared/Documents layer compiles standalone)

**Tests**: none
**Gate**: build

---

### T5: Create `IntensityDto`, update `WorkoutSetValueObject` [P] ✅ Complete (922a4b5)

**What**: New `IntensityDto(IntensityType Type, int Value)`. Update `WorkoutSetValueObject`: `Rpe: int`→`Intensity: IntensityDto?`, `Repetitions`/`RestSeconds` nullable — mirrors T2/T3 at the DTO (API contract) layer, shared by planning AND execution
**Where**: `Workouts/Shared/Dtos/IntensityDto.cs` (new), `Workouts/Shared/ValueObjects/WorkoutSetValueObject.cs`
**Depends on**: T2
**Reuses**: same shape change as T3, different layer
**Requirement**: WOED-04, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `IntensityDto` defined
- [ ] `WorkoutSetValueObject` fields updated
- [ ] `dotnet build` passes at this layer (downstream call sites fixed in Phase 2)

**Tests**: none
**Gate**: build

---

### T6: Create `BlockDto` ✅ Complete (cc9d149)

**What**: New `BlockDto(BlockType Type, WorkoutExerciseDto[] Exercises, int? TimeCapSeconds, int? IntervalSeconds, int? TotalRounds, int? RestAfterSeconds)` — `WorkoutExerciseDto` reused unchanged
**Where**: `Workouts/Shared/Dtos/BlockDto.cs`
**Depends on**: T1, T5
**Reuses**: existing `WorkoutExerciseDto`
**Requirement**: WOED-01, WOED-04, WOED-06

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `BlockDto` defined and compiles
- [ ] `dotnet build` passes

**Tests**: none
**Gate**: build

---

### T7: WorkoutPlans — Create: Block-shaped command, validator, handler [P] ✅ Complete (30a6385)

**What**: `CreateWorkoutPlanCommand.Exercises: WorkoutExerciseDto[]` → `Blocks: BlockDto[]`. `CreateWorkoutPlanCommandValidator`: add Superset ≥2-exercises rule, Amrap `TimeCapSeconds>0` rule, Emom `IntervalSeconds>0`+`TotalRounds>0` rules, cross-level rule rejecting `RestSeconds` when block `Type != Straight`, and change `Intensity` validation from mandatory `InclusiveBetween(1,10)` to `.When(x => x.Intensity != null)`. `CreateWorkoutPlanHandler`: map `BlockDto[]`→`BlockDocumentValueObject` list
**Where**: `WorkoutPlans/CreateWorkoutPlan/{CreateWorkoutPlanCommand,CreateWorkoutPlanCommandValidator,CreateWorkoutPlanHandler}.cs`
**Depends on**: T4, T6
**Reuses**: existing `RuleForEach().ChildRules()` nesting pattern, +1 level
**Requirement**: WOED-01, WOED-02, WOED-03, WOED-04, WOED-05, WOED-06, WOED-07, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Command/validator/handler updated, compiles
- [ ] `CreateWorkoutPlanHandlerTests.cs` updated to Block-shaped commands; existing forbidden-actor test still passes
- [ ] New validator test cases added (one per AC): Superset with 1 exercise → invalid; Amrap without `TimeCapSeconds` → invalid; Emom without `IntervalSeconds`/`TotalRounds` → invalid; `RestSeconds` set on non-Straight block → invalid; `Intensity: null` → valid (previously would've failed); valid Superset/Amrap/Emom payloads → valid
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing count + new validator cases, all pass (no silent deletions)

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): support Superset/Amrap/Emom blocks and optional RPE/RIR in CreateWorkoutPlan`

---

### T7b: Fix out-of-domain call sites reading `WorkoutPlanDocument`/`WorkoutTemplateDocument.Exercises` ✅ Complete (159104e — `StartWorkoutExecutionHandler.cs` flatten done, its remaining Rpe/Repetitions errors belong to T13)

**What**: Mechanical fix for the 3 call sites outside `Features/Training` (found during T3/T4 gate check, not caught by the original file scan) that read the old `.Exercises` shape directly. `TrainerClientAdherenceCalculator.cs` and `GetTrainerClientsHandler.cs` compute read-only stats (set/exercise counts, adherence) from plan data — walk `Blocks→Exercises→Sets` instead of `Exercises→Sets`, same aggregate result for `Straight`-only historical data. `StartWorkoutExecutionHandler.cs` seeds a `WorkoutExecutionDocument` from a `WorkoutPlanDocument`'s exercises — flatten `Blocks.SelectMany(b => b.Exercises)` to preserve today's flat seeding behavior (per AD-007, Execution stays flat; this is the flattening point)
**Where**: `Features/GymManagement/Shared/TrainerClientAdherenceCalculator.cs`, `Features/GymManagement/TrainerClients/GetTrainerClients/GetTrainerClientsHandler.cs`, `Features/Training/Workouts/StartWorkoutExecution/StartWorkoutExecutionHandler.cs`
**Depends on**: T4
**Reuses**: same `Blocks.SelectMany(b => b.Exercises)` flattening pattern needed in T13/T14
**Requirement**: N/A — compile-correctness fix for a gap in the original task breakdown, not a spec requirement

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] All 3 files compile against `Blocks` instead of `Exercises`
- [ ] Existing unit tests for `TrainerClientAdherenceCalculator`/`GetTrainerClientsHandler`/`StartWorkoutExecutionHandler` (if any) still pass, no assertions weakened
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: same as before this task (mechanical fix, no new/removed tests) unless existing tests already assumed a flat shape incompatible with Blocks, in which case note the fix in the task summary

**Tests**: unit
**Gate**: quick

**Commit**: `fix(training): flatten Blocks->Exercises at adherence/trainer-clients/start-execution call sites (gap found in T3/T4 gate check)`

---

### T8: WorkoutPlans — Update: Block-shaped command, validator, handler [P] ✅ Complete (9be0293)

**What**: Same change as T7 applied to `UpdateWorkoutPlanCommand`/`UpdateWorkoutPlanCommandValidator`/`UpdateWorkoutPlanHandler`
**Where**: `WorkoutPlans/UpdateWorkoutPlan/*.cs`
**Depends on**: T4, T6
**Reuses**: T7's validator rules (same rule set, different command type)
**Requirement**: WOED-01, WOED-02, WOED-03, WOED-04, WOED-05, WOED-06, WOED-07, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Command/validator/handler updated, compiles
- [ ] Existing `UpdateWorkoutPlanHandler` tests updated + same validator-case set as T7 added
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing count + new validator cases, all pass

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): support Superset/Amrap/Emom blocks and optional RPE/RIR in UpdateWorkoutPlan`

---

### T9: WorkoutTemplates — Create: Block-shaped command, validator, handler [P] ✅ Complete (089f1c6)

**What**: Same change as T7 applied to `CreateWorkoutTemplateCommand`/Validator/Handler
**Where**: `WorkoutTemplates/CreateWorkoutTemplate/*.cs`
**Depends on**: T4, T6
**Reuses**: T7's validator rules
**Requirement**: WOED-01, WOED-02, WOED-03, WOED-04, WOED-05, WOED-06, WOED-07, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Command/validator/handler updated, compiles
- [ ] Same validator-case set as T7 added for the Template variant
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing count + new cases, all pass

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): support Superset/Amrap/Emom blocks and optional RPE/RIR in CreateWorkoutTemplate`

---

### T10: WorkoutTemplates — Update: Block-shaped command, validator, handler [P] ✅ Complete (310754c)

**What**: Same change as T7 applied to `UpdateWorkoutTemplateCommand`/Validator/Handler
**Where**: `WorkoutTemplates/UpdateWorkoutTemplate/*.cs`
**Depends on**: T4, T6
**Reuses**: T7's validator rules
**Requirement**: WOED-01, WOED-02, WOED-03, WOED-04, WOED-05, WOED-06, WOED-07, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Command/validator/handler updated, compiles
- [ ] Same validator-case set as T7 added for the Template Update variant
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing count + new cases, all pass

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): support Superset/Amrap/Emom blocks and optional RPE/RIR in UpdateWorkoutTemplate`

---

### T11: `WorkoutPlanMappings` — Block-aware Clone/ToResponse [P] ✅ Complete (e2da0f0 — includes `WorkoutPlanResponse` ViewModel + the `AssignWorkoutTemplateHandler` gap, see note below T12)

**What**: Rewrite `Clone` and `ToResponse` to walk `Blocks→BlockExercises→Sets` instead of `Exercises→Sets`
**Where**: `WorkoutPlans/Shared/WorkoutPlanMappings.cs`
**Depends on**: T4, T6
**Reuses**: existing `Select().ToList()`/`ToArray()` pattern
**Requirement**: WOED-01, WOED-04, WOED-06 (Copy must preserve block shape)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `Clone`/`ToResponse` compile and walk the new Block structure
- [ ] Unit test added/extended (e.g. via `CopyWorkoutPlanHandler` tests) proving a Superset/Amrap/Emom block round-trips through Clone unchanged
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`

**Tests**: unit
**Gate**: quick

**Commit**: `refactor(training): map Block/BlockExercise/Set in WorkoutPlan clone and response`

---

### T12: `WorkoutTemplateMappings` — Block-aware equivalent [P] ✅ Complete (e2da0f0)

> **2 more gaps found while doing T11/T12 (2026-09-08):** `AssignWorkoutTemplateHandler.cs` (builds a `WorkoutPlanDocument` from a `WorkoutTemplateDocument` inline, no `Clone`-style extension method) and `CopyWorkoutTemplateHandler.cs` (builds its copy inline instead of via an extension method, unlike `CopyWorkoutPlanHandler`'s `.Clone()`) both walked `Exercises` directly and weren't covered by T9/T10/T11/T12's stated scope. Fixed in the same commit since they share the identical Block-walk shape. Also: `WorkoutPlanResponse`/`WorkoutTemplateResponse` view models needed their `Exercises: WorkoutExerciseDto[]` field renamed to `Blocks: BlockDto[]` (implied by "rewrite ToResponse" but not spelled out as its own line item).

**What**: Same change as T11 applied to `WorkoutTemplateMappings`
**Where**: `WorkoutTemplates/Shared/WorkoutTemplateMappings.cs`
**Depends on**: T4, T6
**Reuses**: T11's approach
**Requirement**: WOED-01, WOED-04, WOED-06

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `Clone`/`ToResponse` compile and walk the new Block structure
- [ ] Unit test added/extended proving round-trip via `CopyWorkoutTemplateHandler`
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`

**Tests**: unit
**Gate**: quick

**Commit**: `refactor(training): map Block/BlockExercise/Set in WorkoutTemplate clone and response`

---

### T13: Execution — mechanical Intensity rename (no Block) [P] ✅ Complete (7704e08)

> **Scope note:** also covered `ExecutedSetDocumentValueObject`/`ExecutedSetValueObject` (the execution-side Set types, distinct from `PlannedSet`/`WorkoutSetValueObject`) — their `Rpe:int` also renamed to `Intensity`, per AD-007 ("Intensidade... aplicado tanto em planejamento quanto em execução"). `Repetitions`/`RestSeconds` on these two types stayed **non-nullable** (deliberate divergence, not mechanical): an executed set always has a known actual rep count/rest, unlike a *planned* AMRAP set which may have no fixed target. `FinishWorkoutExecutionCommand` has no per-set validation today (confirmed before touching it) — preserved that laxity via `?? 0` fallback instead of adding new validation. `UpdateWorkoutExecutionStateCommandValidator` DID validate Reps/Rpe/RestSeconds as mandatory before — added explicit `.NotNull()` to preserve that exact behavior now that the shared `WorkoutSetValueObject` type made those fields nullable (for planning's AMRAP use case, unrelated to Execution). Also fixed 5 existing test files (compile-only, no assertions changed) and closed `StartWorkoutExecutionHandler.cs`'s remaining Rpe/Repetitions errors (T7b had only fixed its `Blocks` flatten).

**What**: Fix compile/behavior at every call site still using the old `Rpe: int` shape, now that `WorkoutSetValueObject` (T5) exposes `Intensity`. No Block wrapper introduced here (AD-007) — purely mechanical rename plus nullable `Repetitions`/`RestSeconds` handling
**Where**: `Workouts/FinishWorkoutExecution/{FinishWorkoutExecutionCommand,FinishWorkoutExecutionHandler}.cs`, `Workouts/UpdateWorkoutExecutionState/{UpdateWorkoutExecutionStateCommand,UpdateWorkoutExecutionStateHandler}.cs`
**Depends on**: T5
**Reuses**: existing handler logic, only the field access changes
**Requirement**: N/A — out-of-scope Execução feature, compile-correctness only (see design.md Risks)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Both commands/handlers compile against the new `WorkoutSetValueObject` shape
- [ ] `FinishWorkoutExecutionHandlerTests.cs`, `UpdateWorkoutExecutionStateHandlerTests.cs`, `WorkoutHandlerTests.cs`, `StartWorkoutExecutionHandlerTests.cs` updated to construct `Intensity` instead of `Rpe` where needed, all still pass
- [ ] No new behavior introduced (assert this in PR description, not just tests)
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: same as before this task (mechanical fix, no new/removed tests)

**Tests**: unit
**Gate**: quick

**Commit**: `refactor(training): rename Rpe to Intensity in workout execution DTOs (mechanical, no behavior change)`

---

### T14: Integration tests — WorkoutPlans endpoints [P] ✅ Complete (323d025) — 21/21 passing

**What**: Extend `WorkoutPlansEndpointsIntegrationTests.cs` with cases: create plan with a Superset block (2 exercises) → `201` + persisted shape correct; create with Superset of 1 exercise → `400`; create with Amrap missing `TimeCapSeconds` → `400`; valid Amrap (with and without fixed `Repetitions`) → `201`; create with Emom missing `IntervalSeconds`/`TotalRounds` → `400`; valid Emom with 2-exercise rotation → `201`, order preserved; `RestSeconds` set on a Superset/Amrap/Emom set → `400`; `Intensity` omitted → `201`
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Training/Endpoints/WorkoutPlansEndpointsIntegrationTests.cs`
**Depends on**: T7, T8
**Reuses**: existing `CreatePlanAsync`/`SeedUserAsync`/`CreateExerciseAsync` helpers in the same file
**Requirement**: WOED-01, WOED-02, WOED-03, WOED-04, WOED-05, WOED-06, WOED-07, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] All 8 scenarios above present as test cases, each asserting status code + response shape
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: existing count + 8 new cases minimum, all pass

**Tests**: integration
**Gate**: full

**Commit**: `test(training): cover Superset/Amrap/Emom validation and persistence in WorkoutPlans endpoints`

---

### T14b: Integration tests — WorkoutTemplates endpoints [P] ✅ Complete (27835ba) — 16/16 passing

> **JSON gotcha found while implementing T14/T14b:** the API serializes enums as camelCase strings (`DependencyInjectionExtensions.cs` registers a global `JsonStringEnumConverter`), but `ReadFromJsonAsync<T>()` without explicit options can't parse those back into `int` payload fields (throws `JsonException`/`FormatException`). Fixed by giving response payload records the actual enum types (`BlockType`, `IntensityType`, etc.) and passing a shared `JsonSerializerOptions` (`JsonSerializerDefaults.Web` + the same converter) to every `ReadFromJsonAsync` call that reads one. Forgetting `JsonSerializerDefaults.Web` on that options object breaks case-insensitive property matching too (a second bug caught immediately by the existing non-Block tests going red).

**What**: Same 8 scenarios as T14, applied to `WorkoutTemplatesEndpointsIntegrationTests.cs`
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Training/Endpoints/WorkoutTemplatesEndpointsIntegrationTests.cs`
**Depends on**: T9, T10
**Reuses**: T14's scenario set, existing helpers in this file
**Requirement**: WOED-01, WOED-02, WOED-03, WOED-04, WOED-05, WOED-06, WOED-07, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Same 8 scenarios present for templates
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj`
- [ ] Test count: existing count + 8 new cases minimum, all pass

**Tests**: integration
**Gate**: full

**Commit**: `test(training): cover Superset/Amrap/Emom validation and persistence in WorkoutTemplates endpoints`

---

### T15: Update `Features/Training/ARCHITECTURE.md` (mandatory per AGENTS.md) ✅ Complete (cf0a60a)

**What**: Document the Block model (Straight/Superset/Amrap/Emom), the `Intensity` exclusivity change, updated endpoints (same routes, new payload shape), and refresh the end-of-file ASCII diagram
**Where**: `ShapeUpApi/src/Features/Training/ARCHITECTURE.md`
**Depends on**: T7, T8, T9, T10, T11, T12, T13, T7b
**Reuses**: existing file structure/diagram style (per AGENTS.md, same style as `Features/Authorization/`)
**Requirement**: N/A — process requirement (AGENTS.md "Domain Architecture Documentation (Mandatory)")

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] File updated: Block model documented, Intensity exclusivity documented, ASCII diagram reflects new shape
- [ ] `dotnet build ShapeUpApi/src/ShapeUp.csproj` passes (docs-only, sanity build)

**Tests**: none
**Gate**: build

**Commit**: `docs(training): document Block model and Intensity exclusivity in ARCHITECTURE.md`

---

### T16: Payload builders (`buildWorkoutPlanBody`/`buildTemplateBody`) — `exercises`→`blocks` ✅ Complete (`b43ccf6`, `c37e543`, `ed9b3c7`, `0afb737`, `929c0d7`)

> **Scope correction found before starting:** `useTrainingApi.js` is a pure passthrough (`JSON.stringify(command)`) — it never builds the request body, so this task as originally written doesn't apply to that file. The real body-builders are `buildWorkoutPlanBody` (duplicated in `ClientDetail.jsx` and `TrainingPlansIndependent.jsx`) and `buildTemplateBody` (`TrainingPlansProfessional.jsx`). Retargeted to those 3 functions + the normalization layer (`trainingEnums.js`, `trainingNormalization.js`) they both depend on.
>
> **Bigger gap found while tracing the change through:** once `normalizePlan`/`normalizeTemplate` stop producing a flat `.exercises` array, every other `plan.exercises`/`tmpl.exercises` read across the app breaks — not just the editor. Found and fixed 15 call sites across 4 files: starting a workout session (`plan.exercises.map(...)` → `flattenBlockExercises(plan.blocks).map(...)`, ×4 occurrences across `TrainingPlansClient.jsx`/`TrainingPlansIndependent.jsx`), exercise/set-count displays (`TrainingPlansClient.jsx`, `TrainingPlansProfessional.jsx`, `ClientDetail.jsx`), the `s.rpe` display string (→ `s.intensityType`/`s.intensityValue`), and `handleCopyPlan`'s deep-clone (`ClientDetail.jsx`). Session/history reads (`session.exercises`, `h.exercises`) were confirmed to be Execution documents, not Plans — correctly left untouched (AD-007: Execution stays flat). Added `flattenBlockExercises`/`countBlockSets` helpers in `trainingNormalization.js` to avoid duplicating the flatten logic at each call site.

**Original scope (still accurate for what changed):** payload key `exercises`→`blocks`, matching the new backend `BlockDto[]` shape, `Intensity` sent as `{type, value}` instead of flat `rpe`.

**Requirement**: WOED-01, WOED-04, WOED-06, WOED-08

**Done when**:
- [x] All plan/template create+update payloads send `blocks` in the shape `{ type, exercises, timeCapSeconds?, intervalSeconds?, totalRounds?, restAfterSeconds? }`
- [x] Every downstream read of a plan/template's exercise list updated or confirmed correctly unaffected (Execution)
- [x] `npm --prefix ShapeUp-Web run lint` passes (0 errors)
- [x] `npm --prefix ShapeUp-Web run build` passes

**Tests**: none
**Gate**: build

---

### T17: `SetRow` component (extracted, + Intensity toggle) ✅ Complete (`1cf1e57`)

**What**: Extract per-set row JSX (`ClientDetail.jsx:365-391`) into its own component. Add RPE/RIR toggle (exclusive — switching clears the other's value, per spec WOED-08 AC3) replacing the single `rpe` input. Disable/hide the `rest` input when `blockType !== 'straight'`
**Where**: `ShapeUp-Web/src/components/training/SetRow.jsx` (new)
**Depends on**: T16
**Reuses**: `SET_TYPES`/`TECHNIQUES` constants (`ClientDetail.jsx:130,134`), existing `Input`/`su-select` styling classes
**Requirement**: WOED-03, WOED-05, WOED-07, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Component renders type/technique/reps/load/intensity(toggle+value)/rest columns
- [ ] Toggling RPE↔RIR clears the previous value (never both populated)
- [ ] `rest` disabled when `blockType` prop is not `'straight'`
- [ ] `npm --prefix ShapeUp-Web run lint` passes

**Tests**: none
**Gate**: build

**Commit**: `refactor(web): extract SetRow component with exclusive RPE/RIR intensity control`

---

### T18: `ExerciseRow` component (extracted) ✅ Complete (`1cf1e57`)

**What**: Extract per-exercise JSX (`ClientDetail.jsx:322-351`, name/tags/notes + set list) into its own component, rendering a list of `SetRow`
**Where**: `ShapeUp-Web/src/components/training/ExerciseRow.jsx` (new)
**Depends on**: T17
**Reuses**: existing JSX/styles, `SetRow` from T17
**Requirement**: WOED-01 (superset needs 2+ of these inside one block)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Component renders exercise name/tags/notes + `SetRow` list, add/remove set works
- [ ] `npm --prefix ShapeUp-Web run lint` passes

**Tests**: none
**Gate**: build

**Commit**: `refactor(web): extract ExerciseRow component`

---

### T19: `BlockCard` component (new — type selector + Amrap/Emom fields) ✅ Complete (`1cf1e57`)

**What**: New component rendering block-type selector (Straight/Superset/Amrap/Emom) + type-specific fields (`TimeCapSeconds` input for Amrap; `IntervalSeconds`+`TotalRounds` inputs for Emom) + a list of `ExerciseRow`. Blocks the type switch when the target type's minimum data isn't satisfied (spec Edge Case — e.g. Straight→Superset with only 1 exercise)
**Where**: `ShapeUp-Web/src/components/training/BlockCard.jsx` (new)
**Depends on**: T18
**Reuses**: `su-exercise-builder-card` styles, `ExerciseRow` from T18
**Requirement**: WOED-01, WOED-02, WOED-04, WOED-05, WOED-06, WOED-07

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Type selector renders 4 options, switching type shows/hides the right fields
- [ ] Amrap/Emom fields required (client-side) before allowing save of that block
- [ ] Type-switch guard: blocked when incompatible with current exercise count (e.g. Superset needs ≥2)
- [ ] `npm --prefix ShapeUp-Web run lint` passes

**Tests**: none
**Gate**: build

**Commit**: `feat(web): add BlockCard component supporting Straight/Superset/Amrap/Emom`

---

### T20: `PlanEditor` — rewire to Block-based state ✅ Complete (`c37e543`, plus `ed9b3c7`/`929c0d7` for the other 2 pages that read a plan's exercises)

**What**: Replace `currentExercises`/exercise-stack rendering with `currentBlocks` state, rendered via a list of `BlockCard` (T19). Adding an exercise from the library creates a new `Straight` block containing it (default UX unchanged for the simple case). Summary sidebar (`avgRpe`, `intensityDist`) filters to `Intensity.Type === 'rpe'` only (RIR sets excluded from that average, not converted). Save/Assign payload built from `currentBlocks`
**Where**: `ShapeUp-Web/src/pages/Dashboard/ClientDetail.jsx:136-...` (edited, not new file)
**Depends on**: T16, T19
**Reuses**: plan-meta form, summary sidebar Card, `ExerciseLibraryModal` — all unchanged
**Requirement**: WOED-01, WOED-04, WOED-06, WOED-08

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `currentBlocks` replaces `currentExercises`; `BlockCard` list renders correctly
- [ ] Adding an exercise creates a `Straight` block (existing single-exercise UX preserved)
- [ ] `avgRpe`/`intensityDist` computations only consider `Intensity.Type === 'rpe'` sets
- [ ] Save/Assign call `onSave`/`onAssign` with `blocks` in place of `exercises`
- [ ] `npm --prefix ShapeUp-Web run lint` passes
- [ ] `npm --prefix ShapeUp-Web run build` passes

**Tests**: none
**Gate**: build

**Commit**: `feat(web): rewire PlanEditor to Block-based state (Straight/Superset/Amrap/Emom)`

---

### T21: i18n keys — block types, Amrap/Emom fields, RIR label ✅ Complete (`110aa0c`) — 13 keys × 3 locales, 887 EN / 887 ES / 889 PT-BR (2 pre-existing PT-BR extras, unrelated), 0 missing

**What**: Add EN/PT-BR/ES keys for: block type labels (Superset/AMRAP/EMOM — Straight already covered by existing "Straight" technique key, confirm no clash), Amrap `TimeCapSeconds` field label, Emom `IntervalSeconds`/`TotalRounds` field labels, RIR label (alongside existing RPE label), RPE/RIR toggle control label
**Where**: `LanguageContext` (EN/PT-BR/ES key files — exact path per existing Fase 1 pattern)
**Depends on**: T19, T20 (final key names used in JSX)
**Reuses**: existing `t()` mechanism, same key-naming convention as `pro.builder.*`
**Requirement**: WOED-01, WOED-04, WOED-06, WOED-08 (Definition of Done — i18n)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] All new keys present in EN, PT-BR, and ES with 100% parity (same key count in all 3 — same check style as Fase 1's 865/865 parity)
- [ ] `npm --prefix ShapeUp-Web run lint` passes

**Tests**: none
**Gate**: build

**Commit**: `feat(web): add i18n keys for block types, AMRAP/EMOM fields, and RIR label (EN/PT-BR/ES)`

---

## Phase 4 status: ✅ Closed (2026-09-08)

T16-T21 all complete. `npm run lint` (0 errors, 6 pre-existing unrelated warnings) and `npm run build` both green. Real scope correction: T16 was written assuming `useTrainingApi.js` builds request payloads — it doesn't (pure passthrough) — retargeted to the actual body-builders + normalization layer, and a broader gap surfaced from there (15 `.exercises` call sites across 4 files, see T16 note above).

**Browser verification blocked**: tried to smoke-test the new BlockCard/SetRow UI in the live app, but the dev server throws `Firebase: Error (auth/invalid-api-key)` on load — no `.env`/Firebase config available in this sandbox, unrelated to this feature. Confirmed via network tab that all new component files load `200 OK` with no missing-module errors; full click-through (login → editor → save) was not possible here. Flagging this as an environment gap for whoever runs this next, not a code gap.

---

### T22: Full-stack gate — final smoke

**What**: Run the complete gate across both repos and confirm the feature's Success Criteria manually (create a plan with Straight + Superset + Amrap + Emom blocks mixed, save, reopen, verify no data loss)
**Where**: N/A (verification task, no new files)
**Depends on**: T1–T21 (all)
**Reuses**: N/A
**Requirement**: All WOED-01..12 (final confirmation)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `dotnet build ShapeUpApi/src/ShapeUp.csproj` passes
- [ ] `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` passes, full count reported
- [ ] `dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` passes, full count reported
- [ ] `npm --prefix ShapeUp-Web run lint && npm --prefix ShapeUp-Web run build` passes
- [ ] Manual scenario from spec Success Criteria confirmed end-to-end (mixed-block plan round-trips)

**Tests**: none (aggregation gate)
**Gate**: build (full solution)

**Commit**: `chore(training): final gate for workout-editor Block model (WOED-01..12)`

---

## Parallel Execution Map

```
Phase 1 (mostly sequential, 2 [P] pairs):
  T1 [P] ─┬→ T3 ─┬→ T4 ─┐
  T2 [P] ─┘      └→ T5 [P] ─┴→ T6

Phase 2 (Parallel — 7 independent files/tasks):
  T6, T4 complete, then:
    ├── T7  [P]
    ├── T8  [P]
    ├── T9  [P]
    ├── T10 [P]
    ├── T11 [P]
    └── T12 [P]
  T5 complete, then:
    └── T13 [P]
  T4 complete, then:
    └── T7b [P]

Phase 3 (Parallel pair, then sequential doc task):
  T7, T8 complete → T14  [P]
  T9, T10 complete → T14b [P]
  T7..T13, T7b complete → T15

Phase 4 (Sequential — components build on each other):
  T15 → T16 → T17 → T18 → T19 → T20 → T21

Phase 5 (Sequential):
  T21 → T22
```

**Parallelism constraint:** A task marked `[P]` has no unfinished dependency, its required test type is parallel-safe per the Parallelism Assessment above (unit = yes; integration = no — but T14/T14b are `[P]` relative to *each other*, not concurrent within the same collection, since they hit different endpoint sets; they still both belong to the sequential integration gate), and shares no mutable state with sibling `[P]` tasks.

**How phase-based execution works**: 5 phases > 3 → per the skill's Sub-Agent Delegation rule, offer one worker per phase (sequential) before starting Execute.

---

## Task Granularity Check

| Task | Scope | Status |
|---|---|---|
| T1: Create BlockType enum | 1 file | ✅ Granular |
| T2: Create IntensityType + IntensityDocumentValueObject | 2 files, 1 concept | ✅ Granular (cohesive) |
| T3: Update PlannedSetDocumentValueObject | 1 file | ✅ Granular |
| T4: Create BlockDocumentValueObject + rename + update 2 documents | 4 files, 1 concept (Block shape at doc layer) | ✅ Granular (cohesive) |
| T5: Create IntensityDto + update WorkoutSetValueObject | 2 files, 1 concept | ✅ Granular (cohesive) |
| T6: Create BlockDto | 1 file | ✅ Granular |
| T7-T10: Command+Validator+Handler per Create/Update × Plan/Template | 3 files each, 1 concept (same command's full slice) | ✅ Granular (cohesive, matches AGENTS.md vertical-slice unit) |
| T7b: Fix 3 out-of-domain call sites (gap found in T3/T4 gate check) | 3 files, 1 concept (flatten Blocks→Exercises, mechanical) | ✅ Granular (cohesive) |
| T11-T12: Mappings rewrite | 1 file each | ✅ Granular |
| T13: Execution rename | 4 files, 1 concept (mechanical rename) | ✅ Granular (cohesive) |
| T14-T14b: Integration tests | 1 file each | ✅ Granular |
| T15: ARCHITECTURE.md update | 1 file | ✅ Granular |
| T16: useTrainingApi.js payload | 1 file | ✅ Granular |
| T17-T19: Frontend components | 1 file each | ✅ Granular |
| T20: PlanEditor rewire | 1 file (edited) | ✅ Granular |
| T21: i18n keys | N files (3 locale files), 1 concept | ✅ Granular (cohesive) |
| T22: Final gate | 0 files (verification) | ✅ Granular (explicit aggregation task, not hidden scope) |

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
|---|---|---|---|
| T1 | None | None | ✅ Match |
| T2 | None | None | ✅ Match |
| T3 | T2 | T2→T3 | ✅ Match |
| T4 | T1, T3 | T1→T4, T3→T4 | ✅ Match |
| T5 | T2 | T2→T5 | ✅ Match |
| T6 | T1, T5 | T4→T6 (via T1 arrow chain), T5→T6 | ✅ Match |
| T7 | T4, T6 | T6→T7 | ✅ Match |
| T8 | T4, T6 | T6→T8 | ✅ Match |
| T9 | T4, T6 | T6→T9 | ✅ Match |
| T10 | T4, T6 | T6→T10 | ✅ Match |
| T11 | T4, T6 | T6→T11 | ✅ Match |
| T12 | T4, T6 | T6→T12 | ✅ Match |
| T13 | T5 | T5→T13 | ✅ Match |
| T7b | T4 | T4→T7b | ✅ Match |
| T14 | T7, T8 | T7,T8→T14 | ✅ Match |
| T14b | T9, T10 | T9,T10→T14b | ✅ Match |
| T15 | T7-T13, T7b | T7..T13,T7b→T15 | ✅ Match |
| T16 | T15 | T15→T16 | ✅ Match |
| T17 | T16 | T16→T17 | ✅ Match |
| T18 | T17 | T17→T18 | ✅ Match |
| T19 | T18 | T18→T19 | ✅ Match |
| T20 | T16, T19 | T19→T20 (T16 already satisfied earlier in chain) | ✅ Match |
| T21 | T19, T20 | T20→T21 | ✅ Match |
| T22 | T1-T21 (all) | T21→T22 | ✅ Match |

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
|---|---|---|---|---|
| T1-T6 | Enums/VOs/DTOs | none | none | ✅ OK |
| T7-T10 | Command Validators + Handlers | unit | unit | ✅ OK |
| T7b | Command Handlers (out-of-domain, read-only) | unit | unit | ✅ OK |
| T11-T12 | Mappings | unit | unit | ✅ OK |
| T13 | Command Handlers (Execution) | unit | unit | ✅ OK |
| T14-T14b | Controllers/Endpoints | integration | integration | ✅ OK |
| T15 | Docs (no code layer) | none | none | ✅ OK |
| T16 | Frontend (hook) | none | none | ✅ OK |
| T17-T21 | Frontend (components/i18n) | none | none | ✅ OK |
| T22 | Aggregation (no code layer) | none | none | ✅ OK |

All ✅ — no restructuring needed.

---

## MCPs and Skills — confirm before Execute

For each task above, `Tools` is set to `NONE`/`NONE` (no project MCP or skill beyond `tlc-spec-driven` itself was found configured for this repo). Confirm before I start Execute:

- Should any task use a specific MCP (e.g. a `dotnet`/`nuget` MCP, a browser MCP for frontend verification) instead of plain Bash/PowerShell?
- Any skill (beyond `tlc-spec-driven` for the Execute flow itself) I should invoke per task — e.g. `run` to launch/screenshot `ShapeUp-Web` after T20/T21?
</content>
