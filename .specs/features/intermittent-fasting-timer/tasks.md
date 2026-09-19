# Intermittent Fasting Timer (API) Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user - do not proceed without it.**

---

**Design**: `.specs/features/intermittent-fasting-timer/design.md`
**Status**: In Progress

**Progress log**: T1 committed — FastingClockCalculator + unit tests (2026-09-19).
**Scope**: Backend only (`ShapeUpV2`). `IFTA-01`–`08`. Web `IFTW-*` is a later repo.

---

## Test Coverage Matrix

> Generated from codebase, project guidelines, and spec. Guidelines found: `src/AGENTS.md` (CQRS, FluentValidation, Result, CancellationToken, keyset pagination, ARCHITECTURE.md). Samples: `CreateMealPlanHandlerTests.cs`, `CompleteOnboardingCommandValidatorTests.cs`, `NutritionProfileEndpointsIntegrationTests.cs`.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
| --- | --- | --- | --- | --- |
| `FastingClockCalculator` | unit | IFTA-01 AC2/3/5; DST next-valid; override vs agenda; Idle | `tests/UnitTests/Domains/Nutrition/Fasting/*` | `dotnet test tests/UnitTests/UnitTests.csproj` |
| `FastingFeatureGuard` | unit | IFTA-05: disabled → code `nutrition.fasting.disabled` 404; enabled/missing-key → pass | same | same |
| Handlers + validators | unit | 1:1 IFTA ACs for that handler; 400 field name; 409 duplicate Start; 403 P2; lazy complete | same | same |
| EF entities + migrations | none | schema — build gate | `Nutrition/Infrastructure/Data/` | `dotnet build src/ShapeUp.slnx` |
| `FastingController` + DI | none | thin; covered by integration | `FastingController.cs`, `NutritionModule.cs` | build |
| HTTP endpoints | integration | every fasting route: happy + listed errors (401, 400, 409, 404 flag, 403 P2) | `tests/IntegrationTests/Domains/Nutrition/Endpoints/` | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Fasting` |

## Gate Check Commands

> Generated from codebase.

| Gate Level | When to Use | Command |
| --- | --- | --- |
| Quick | After unit-tested tasks | `dotnet test tests/UnitTests/UnitTests.csproj --filter FullyQualifiedName~Nutrition.Fasting` |
| Full | After integration task | `dotnet test tests/UnitTests/UnitTests.csproj --filter FullyQualifiedName~Nutrition.Fasting && dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Fasting` |
| Build | Entity/migration/controller | `dotnet build src/ShapeUp.slnx` |

If the filtered unit suite is empty before T1 tests exist, run `dotnet test tests/UnitTests/UnitTests.csproj --filter FullyQualifiedName~Nutrition.Fasting` anyway (0 tests is fail — add tests first). After T1, expect a growing count; never delete existing Nutrition tests.

---

## Execution Plan

Phases run sequentially. Tasks within a phase run in order.

### Phase 1: Foundation

```
T1 → T2 → T3 → T4
```

### Phase 2: Handlers

```
T5 → T6 → T7 → T8 → T9
```

### Phase 3: HTTP

```
T10 → T11
```

---

## Task Breakdown

### Phase 1: Foundation

### T1: FastingClockCalculator + protocol hours

**What**: Pure calculator: presets `14:10`/`16:8`/`18:6`/`20:4`; custom 12–23; agenda windows from eating start + IANA; override Fasting/Eating remaining; Idle when no agenda.
**Where**: `src/Features/Nutrition/Fasting/Shared/FastingClockCalculator.cs`
**Depends on**: None
**Reuses**: `TimeZoneInfo`
**Requirement**: IFTA-01, IFTA-02
**Tools**: Skill `tlc-spec-driven` Execute; MCP none
**Done when**:
- [x] `16:8` + 720 minutes + `America/Sao_Paulo` at 11:00 local → Fasting, boundary 12:00 local as UTC
- [x] same at 12:00 local → Eating, boundary 20:00 local as UTC
- [x] eating start 0 + `16:8` → eat 00:00–08:00
- [x] DST skip/repeat uses next valid local time (tested)
- [x] Gate quick passes
**Tests**: unit
**Gate**: quick
**Commit**: `feat(nutrition): add fasting clock calculator`

---

### T2: SQL entities and NutritionDbContext mapping

**What**: `FastingAgenda` (nullable agenda fields + recommendation) and `FastingOverride`; configure tables, unique filtered index one active override per user.
**Where**: `src/Features/Nutrition/Shared/Entities/FastingAgenda.cs`
**Depends on**: T1
**Reuses**: `NutritionDbContext` mapping style
**Requirement**: IFTA-01, IFTA-03
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] Entities mapped; project builds
**Tests**: none
**Gate**: build
**Commit**: `feat(nutrition): add fasting agenda and override entities`

---

### T3: Nutrition EF migration for fasting tables

**What**: EF migration creating `NutritionFastingAgendas` and `NutritionFastingOverrides` with the filtered unique index.
**Where**: `src/Features/Nutrition/Infrastructure/Data/Migrations/`
**Depends on**: T2
**Reuses**: existing Nutrition InitialCreate style
**Requirement**: IFTA-01
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] Migration + snapshot updated; `dotnet build src/ShapeUp.slnx` passes
**Tests**: none
**Gate**: build
**Commit**: `feat(nutrition): migrate fasting agenda and override tables`

---

### T4: Seed `nutrition.intermittent-fasting` flag + guard

**What**: PlatformFeatureFlags migration HasData key enabled=true. `IUtcClock` + `FastingFeatureGuard` returning `nutrition.fasting.disabled` 404 when off.
**Where**: `src/Features/Nutrition/Fasting/Shared/FastingFeatureGuard.cs`
**Depends on**: T3
**Reuses**: `IFeatureFlagReader`, flag InitialCreate InsertData
**Requirement**: IFTA-05
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] Guard unit: disabled → code `nutrition.fasting.disabled` status 404
- [ ] Guard unit: enabled → success
- [ ] Seed row enabled=true
- [ ] Gate quick passes
**Tests**: unit
**Gate**: quick
**Commit**: `feat(nutrition): seed fasting feature flag and guard`

---

### Phase 2: Handlers

### T5: PutFastingAgenda handler

**What**: PUT agenda: presets or custom 12–23; 30-min grid; valid IANA; upsert owner row; 400 names field.
**Where**: `src/Features/Nutrition/Fasting/PutAgenda/PutFastingAgendaHandler.cs`
**Depends on**: T4
**Reuses**: `SetManualGoalHandler` DbContext upsert
**Requirement**: IFTA-01, IFTA-07
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] PUT 16:8 / 720 / valid TZ persists FastHours 16 EatHours 8
- [ ] invalid protocol/minutes/TZ/fastHours 8 → 400 naming field
- [ ] custom 15 → eat 9
- [ ] PUT during active override does not change override timestamps (test with in-memory/fake db or later T6 fixture — if DbContext needed, use EF InMemory or SQLite if already in unit tests; otherwise NutritionDbContext with UseInMemoryDatabase)
- [ ] Gate quick passes
**Tests**: unit
**Gate**: quick
**Commit**: `feat(nutrition): persist intermittent fasting agenda`

---

### T6: GetFastingClock handler

**What**: GET snapshot: Idle empty; agenda-derived clock; override source; lazy-complete past `eatEndsAt`.
**Where**: `src/Features/Nutrition/Fasting/GetClock/GetFastingClockHandler.cs`
**Depends on**: T5
**Reuses**: `FastingClockCalculator`, `IUtcClock`
**Requirement**: IFTA-02
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] no agenda no override → Idle nulls
- [ ] agenda 16:8 mocked 11:00 → Fasting source Agenda
- [ ] active override → source Override, UTC timestamps
- [ ] eatEndsAt past → override Completed persisted, clock from agenda
- [ ] Gate quick passes
**Tests**: unit
**Gate**: quick
**Commit**: `feat(nutrition): get fasting clock snapshot`

---

### T7: Start, end-early, and cancel override handlers

**What**: Start requires agenda; 409 if active Fasting/Eating; end-early Fasting→Eating eatEndsAt=now+eatHours; cancel → Cancelled; 409 if no active / end-early while Eating.
**Where**: `src/Features/Nutrition/Fasting/StartOverride/StartFastingOverrideHandler.cs`
**Depends on**: T6
**Reuses**: `IUtcClock`
**Requirement**: IFTA-03, IFTA-04
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] Start 16:8 → Fasting, fastEndsAt = now+16h (not agenda 20:00)
- [ ] Start without agenda → 400
- [ ] second Start → 409 unchanged
- [ ] end-early / cancel ACs from spec
- [ ] Gate quick passes
**Tests**: unit
**Gate**: quick
**Commit**: `feat(nutrition): start end and cancel fasting override`

---

### T8: SetFastingRecommendation handler

**What**: PUT recommendation for clientUserId; Training relationship required; 403 otherwise; does not create override; client GET shows recommendation.
**Where**: `src/Features/Nutrition/Fasting/SetRecommendation/SetFastingRecommendationHandler.cs`
**Depends on**: T7
**Reuses**: `IProfessionalClientRelationshipRepository.GetActiveAsync(..., "Training", ...)`
**Requirement**: IFTA-06
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] with relationship stores recommendation protocol, no override
- [ ] without relationship 403
- [ ] client agenda PUT still allowed to differ (covered by T5 + GET)
- [ ] Gate quick passes
**Tests**: unit
**Gate**: quick
**Commit**: `feat(nutrition): save professional fasting recommendation`

---

### T9: GetFastingHistory handler

**What**: Keyset list of Completed/Cancelled, newest first, pageSize default/max 14, empty list 200, duration fasted.
**Where**: `src/Features/Nutrition/Fasting/GetHistory/GetFastingHistoryHandler.cs`
**Depends on**: T8
**Reuses**: diary/weight keyset cursor style if present; else opaque base64 of CompletedAtUtc+Id
**Requirement**: IFTA-08
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] empty → items []
- [ ] returns at most 14
- [ ] Gate quick passes
**Tests**: unit
**Gate**: quick
**Commit**: `feat(nutrition): list fasting override history`

---

### Phase 3: HTTP

### T10: FastingController, DI, ARCHITECTURE

**What**: Controller routes per design; guard on every action; register in `NutritionModule`; document endpoints in `ARCHITECTURE.md`.
**Where**: `src/Features/Nutrition/Fasting/FastingController.cs`
**Depends on**: T9
**Reuses**: `NutritionProfileController`, `ToActionResult`
**Requirement**: IFTA-01, IFTA-02, IFTA-03, IFTA-04, IFTA-05, IFTA-06, IFTA-08
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] All P1+P2+P3 routes exist; Start 201; module registers handlers/validators/clock/guard
- [ ] ARCHITECTURE.md lists Fasting tables + endpoints
- [ ] Gate build passes
**Tests**: none
**Gate**: build
**Commit**: `feat(nutrition): expose fasting HTTP endpoints`

---

### T11: Fasting endpoint integration tests

**What**: Integration tests for IFTA happy paths and 401/400/409/404-flag/403.
**Where**: `tests/IntegrationTests/Domains/Nutrition/Endpoints/FastingEndpointsIntegrationTests.cs`
**Depends on**: T10
**Reuses**: `NutritionProfileEndpointsIntegrationTests` auth seed
**Requirement**: IFTA-01, IFTA-02, IFTA-03, IFTA-04, IFTA-05, IFTA-06
**Tools**: Skill `tlc-spec-driven` Execute
**Done when**:
- [ ] PUT+GET clock, Start+GET Override, cancel back to Agenda, second Start 409, unauthenticated 401, flag off 404 `nutrition.fasting.disabled`, P2 403
- [ ] If Testcontainers cannot start, stop and report blocker — do not delete tests
- [ ] Gate full passes when host is up
**Tests**: integration
**Gate**: full
**Commit**: `test(nutrition): cover fasting HTTP endpoints`

---

## Phase Execution Map

```
Phase 1 → Phase 2 → Phase 3

Phase 1:  T1 → T2 → T3 → T4
Phase 2:  T5 → T6 → T7 → T8 → T9
Phase 3:  T10 → T11
```

Batches (~7 tasks, whole phases): **Batch 1 = Phase 1 (T1–T4)**. **Batch 2 = Phase 2+3 (T5–T11)**. Sequential. Verifier after T11 (orchestrator, not implementer).

---

## Task Granularity Check

| Task | Scope | Status |
| --- | --- | --- |
| T1 calculator | 1 component | ✅ |
| T2 entities | cohesive mapping | ✅ |
| T3 migration | 1 migration | ✅ |
| T4 flag+guard | cohesive IFTA-05 | ✅ |
| T5 PutAgenda | 1 command | ✅ |
| T6 GET clock | 1 query | ✅ |
| T7 override writes | 3 commands same aggregate | ⚠️ cohesive |
| T8 recommendation | 1 command | ✅ |
| T9 history | 1 query | ✅ |
| T10 controller+DI | wiring | ✅ |
| T11 integration | 1 test class | ✅ |

---

## Diagram-Definition Cross-Check

| Task | Depends On (body) | Diagram Shows | Status |
| --- | --- | --- | --- |
| T1 | None | start | ✅ |
| T2 | T1 | T1 → T2 | ✅ |
| T3 | T2 | T2 → T3 | ✅ |
| T4 | T3 | T3 → T4 | ✅ |
| T5 | T4 | T4 → T5 (phase 2 after 1) | ✅ |
| T6 | T5 | T5 → T6 | ✅ |
| T7 | T6 | T6 → T7 | ✅ |
| T8 | T7 | T7 → T8 | ✅ |
| T9 | T8 | T8 → T9 | ✅ |
| T10 | T9 | T9 → T10 | ✅ |
| T11 | T10 | T10 → T11 | ✅ |

---

## Test Co-location Validation

| Task | Code Layer | Matrix Requires | Task Says | Status |
| --- | --- | --- | --- | --- |
| T1 | calculator | unit | unit | ✅ |
| T2 | entity | none | none | ✅ |
| T3 | migration | none | none | ✅ |
| T4 | guard | unit | unit | ✅ |
| T5 | handler | unit | unit | ✅ |
| T6 | handler | unit | unit | ✅ |
| T7 | handler | unit | unit | ✅ |
| T8 | handler | unit | unit | ✅ |
| T9 | handler | unit | unit | ✅ |
| T10 | controller | none | none | ✅ |
| T11 | HTTP | integration | integration | ✅ |
