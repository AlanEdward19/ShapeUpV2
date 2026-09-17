# Agenda Semanal do Treino no Dashboard — Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Design**: `.specs/features/workout-schedule-dashboard/design.md`
**Status**: Verified PASS (backend scope) — see `validation.md`

## Progress Log

- ✅ T1 `d9c2386` — AssignedWeekdays on WorkoutPlanDocument
- ✅ T2 `3029284` — Response + Clone + ToPlanResponse mapping
- ✅ T3 `17ec69f` — Create persists/returns AssignedWeekdays (+ dedupe, invalid enum)
- ✅ T4 `c5d5443` — Update persists/clears/dedupes AssignedWeekdays
- ✅ T5 `efc6250` — Assign template yields empty AssignedWeekdays
- ✅ T6 `baef142` — Dashboard echoes dynamic sessionsTargetPerWeek (WSD-07)

Quick gate after T6: **393 passed**, 0 failed, 0 skipped.

---

## Test Coverage Matrix

> Generated from codebase (`src/AGENTS.md`, samples in `tests/UnitTests/Domains/Training/`), spec, and design. No numeric coverage threshold in `AGENTS.md` — strong default applied. Guidelines found: `src/AGENTS.md` (FluentValidation, Result pattern, testability).

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Document field `AssignedWeekdays` on `WorkoutPlanDocument` | none | Additive field with safe default `[]` — build gate only | `src/Features/Training/Shared/Documents/WorkoutPlanDocument.cs` | `dotnet build src/ShapeUp.slnx` |
| Response/mappings (`WorkoutPlanResponse`, `ToResponse`, `Clone`, `ToPlanResponse`) | none | Mechanical field plumbing — build gate only; Create/Update handler tests assert round-trip | `WorkoutPlans/Shared/**`, `WorkoutTemplates/Shared/WorkoutTemplateMappings.cs` | `dotnet build src/ShapeUp.slnx` |
| Handlers Create/Update WorkoutPlan (+ validators) | unit | 1:1 to WSD-01 ACs: empty persist, multi-day persist, dedupe, omit→empty; existing behavior not regressed | `tests/UnitTests/Domains/Training/WorkoutPlans/*HandlerTests.cs` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| AssignWorkoutTemplateHandler | unit | New plan has `AssignedWeekdays == []` | `tests/UnitTests/Domains/Training/WorkoutTemplates/*` or extend existing | `dotnet test tests/UnitTests/UnitTests.csproj` |
| GetTrainingDashboardHandler (WSD-07) | unit | Accepts dynamic `N>0` and echoes as `SessionsTargetPerWeek`; rejects `N<=0` (already covered — add one AC-anchored case for dynamic N≠5) | `tests/UnitTests/Domains/Training/Dashboard/TrainingDashboardHandlerTests.cs` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Integration/Endpoints | none (unaffected) | No new endpoints; contract additive on existing plan responses | — | — |

## Parallelism Assessment

> Unit tests mock repositories — parallel-safe.

| Test Type | Parallel-Safe? | Isolation Model | Evidence |
|---|---|---|---|
| unit | Yes | Moq per test, no shared DB | `CreateWorkoutPlanHandlerTests.cs`, `TrainingDashboardHandlerTests.cs` |

## Gate Check Commands

| Gate Level | When to Use | Command |
|---|---|---|
| Quick | After every handler/test task | `dotnet test tests/UnitTests/UnitTests.csproj` |
| Full | Not required (no integration changes) | `dotnet test src/ShapeUp.slnx` |
| Build | After document/mapping-only tasks | `dotnet build src/ShapeUp.slnx` |

---

## Execution Plan

### Phase 1: Data + Response Plumbing

```
T1 → T2
```

### Phase 2: Authoring Persistence

```
T3 → T4
```

### Phase 3: Assign default + Dashboard contract

```
T5 → T6
```

---

## Task Breakdown

### T1: Add `AssignedWeekdays` to `WorkoutPlanDocument`

**What**: Add `List<DayOfWeek> AssignedWeekdays { get; set; } = [];` with BSON string enum representation (same pattern as `Difficulty`).
**Where**: `src/Features/Training/Shared/Documents/WorkoutPlanDocument.cs`
**Depends on**: None
**Reuses**: `[BsonRepresentation(BsonType.String)]` on `Difficulty`
**Requirement**: WSD-01

**Tools**: MCP: NONE · Skill: `tlc-spec-driven`

**Done when**:
- [ ] Property exists, defaults to empty list
- [ ] Build gate passes

**Tests**: none
**Gate**: build

**Commit**: `feat(training): add AssignedWeekdays to WorkoutPlanDocument`

---

### T2: Expose `AssignedWeekdays` on response + mappings

**What**: Add `DayOfWeek[] AssignedWeekdays` to `WorkoutPlanResponse`; map in `ToResponse`, `Clone`, and `ToPlanResponse`.
**Where**: `WorkoutPlanResponse.cs`, `WorkoutPlanMappings.cs`, `WorkoutTemplateMappings.cs`
**Depends on**: T1
**Reuses**: existing mapping style for `Blocks`/`Difficulty`
**Requirement**: WSD-01

**Tools**: MCP: NONE · Skill: `tlc-spec-driven`

**Done when**:
- [ ] Response includes `AssignedWeekdays`
- [ ] Clone copies the list (not shared reference)
- [ ] `ToPlanResponse` includes the field (Assign/Copy paths)
- [ ] Build gate passes

**Tests**: none
**Gate**: build

**Commit**: `feat(training): map AssignedWeekdays on plan response and clone`

---

### T3: Create WorkoutPlan persists/returns `AssignedWeekdays`

**What**: Extend `CreateWorkoutPlanCommand` (+ validator + handler) to accept optional weekdays, dedupe before persist, default empty when omitted.
**Where**: `CreateWorkoutPlanCommand.cs`, `CreateWorkoutPlanCommandValidator.cs`, `CreateWorkoutPlanHandler.cs`, `CreateWorkoutPlanHandlerTests.cs`
**Depends on**: T2
**Reuses**: existing create handler + WEV-style tests at bottom of test file
**Requirement**: WSD-01

**Tools**: MCP: NONE · Skill: `tlc-spec-driven`

**Done when**:
- [ ] Omit / null / empty → persisted `[]` and returned `[]`
- [ ] Monday+Thursday persisted and returned
- [ ] Duplicate Monday in payload → single Monday persisted (no 400)
- [ ] Invalid enum → 400
- [ ] Quick gate passes; new tests green; no silent deletions

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): persist AssignedWeekdays on create workout plan`

---

### T4: Update WorkoutPlan persists/returns `AssignedWeekdays`

**What**: Same field on update path — can set, clear to empty, dedupe.
**Where**: `UpdateWorkoutPlanCommand.cs`, `UpdateWorkoutPlanCommandValidator.cs`, `UpdateWorkoutPlanHandler.cs`, `UpdateWorkoutPlanHandlerTests.cs`
**Depends on**: T3
**Reuses**: update handler + existing RequireRpe update tests pattern
**Requirement**: WSD-01

**Tools**: MCP: NONE · Skill: `tlc-spec-driven`

**Done when**:
- [ ] Update with days persists/returns them
- [ ] Update with empty clears previous days
- [ ] Dedupe on update
- [ ] Quick gate passes

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): persist AssignedWeekdays on update workout plan`

---

### T5: Assign template yields plan with empty `AssignedWeekdays`

**What**: Confirm `AssignWorkoutTemplateHandler` produces `AssignedWeekdays == []` (default from document); add unit assertion if Assign tests exist, else add focused test.
**Where**: `AssignWorkoutTemplateHandler.cs` (only if missing default), Assign handler tests
**Depends on**: T2
**Reuses**: document default `[]`
**Requirement**: WSD-01 (edge: assign path)

**Tools**: MCP: NONE · Skill: `tlc-spec-driven`

**Done when**:
- [ ] Assigned plan response has empty `AssignedWeekdays`
- [ ] Quick gate passes

**Tests**: unit
**Gate**: quick

**Commit**: `test(training): assert AssignWorkoutTemplate clears AssignedWeekdays`

---

### T6: WSD-07 — dashboard accepts dynamic `sessionsTargetPerWeek`

**What**: Add unit test proving handler echoes a dynamic N (e.g. 2 or 3) as `SessionsTargetPerWeek` and computes completionRate from it — no production code change expected.
**Where**: `TrainingDashboardHandlerTests.cs`
**Depends on**: None (can run after Phase 2; listed in Phase 3 for clarity)
**Reuses**: existing dashboard handler tests
**Requirement**: WSD-07

**Tools**: MCP: NONE · Skill: `tlc-spec-driven`

**Done when**:
- [ ] Test with N=2 (or other ≠5) asserts response target == N
- [ ] Existing N<=0 rejection still green
- [ ] Quick gate passes

**Tests**: unit
**Gate**: quick

**Commit**: `test(training): lock dynamic sessionsTargetPerWeek on dashboard`

---

## Parallel Execution Map

```
Phase 1 (Sequential):
  T1 ──→ T2

Phase 2 (Sequential):
  T3 ──→ T4

Phase 3 (Sequential within phase; T5 depends T2, T6 independent of T5):
  T5 ──→ T6
```

Note: T6 `Depends on: None` but placed after T5 in Phase 3 for a single sequential worker path. Diagram vs body: T6 has no arrow from T5 required — executing T5 then T6 is ordering convenience only.

## Diagram-Definition Cross-Check

| Task | Depends On (body) | Diagram Shows | Status |
|---|---|---|---|
| T1 | None | start | ✅ |
| T2 | T1 | T1→T2 | ✅ |
| T3 | T2 | Phase2 after Phase1 | ✅ |
| T4 | T3 | T3→T4 | ✅ |
| T5 | T2 | Phase3 after Phase1 (via T2) | ✅ |
| T6 | None | after T5 (order only) | ✅ Match (no false dep) |

## Test Co-location Validation

| Task | Code Layer | Matrix Requires | Task Says | Status |
|---|---|---|---|---|
| T1 | Document field | none | none | ✅ |
| T2 | Response/mappings | none | none | ✅ |
| T3 | Create handler | unit | unit | ✅ |
| T4 | Update handler | unit | unit | ✅ |
| T5 | Assign handler | unit | unit | ✅ |
| T6 | Dashboard handler | unit | unit | ✅ |

## Granularity Check

| Task | Scope | Status |
|---|---|---|
| T1 Document field | 1 property | ✅ |
| T2 Response+3 mappers | cohesive plumbing same field | ✅ |
| T3 Create path | 1 write path + tests | ✅ |
| T4 Update path | 1 write path + tests | ✅ |
| T5 Assign assert | 1 path | ✅ |
| T6 Dashboard test | 1 AC lock | ✅ |
