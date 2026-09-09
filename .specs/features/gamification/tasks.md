# Gamification — Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Design**: `ShapeUpApi/.specs/features/gamification/design.md`
**Status**: Draft

---

## Test Coverage Matrix

> Generated from codebase (`ShapeUpApi/src/AGENTS.md` — guideline found, no numeric coverage threshold; existing xUnit unit/integration patterns) and spec. Confirm before Execute.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Enums / entities (`ActivityClassification`, `GamificationProfile`, `WorkoutEvaluation`) | none | Plain data shapes, no branching | `Features/Gamification/Shared/**` | `dotnet build ShapeUpApi/src/ShapeUp.csproj` |
| `GamificationDbContext` + migration | none | Config/schema — correctness proven transitively by every test below actually persisting through it | `Features/Gamification/Infrastructure/Data/**` | `dotnet build` + `dotnet ef database update` succeeds |
| `AntiCheatClassifier` (3 rules + aggregation) | unit | All branches; every threshold boundary in spec GAM-03 gets a dedicated test (not just one value per range) — this is exactly where a graded rule hides an off-by-one | `ShapeUpApi/tests/UnitTests/Domains/Gamification/**` | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| `StreakCalculator` / `LevelCalculator` | unit | 1:1 to spec GAM-06/GAM-07 ACs; every listed edge case (same-day, gap>1, brand-new user) | `ShapeUpApi/tests/UnitTests/Domains/Gamification/**` | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| `GamificationWorkoutFinishedConsumer` | unit | All branches: credit path (Verified/LikelyValid), withhold path (Suspicious/Invalid), idempotency short-circuit, streak-milestone bonus trigger | `ShapeUpApi/tests/UnitTests/Domains/Gamification/**` | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| `ShapeScoreCalculator`, `GetGamificationProfileHandler`, `GetRankingHandler` | unit | 1:1 to spec GAM-08/GAM-10 ACs, including the "no history yet" edge case | `ShapeUpApi/tests/UnitTests/Domains/Gamification/**` | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| End-to-end (real consumer through real bus, anti-cheat scenarios, ranking) | integration | Every P1/P2 acceptance criterion — happy path + every listed anti-cheat boundary + idempotency | `ShapeUpApi/tests/IntegrationTests/Domains/Gamification/**` | `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` (SQL Server via existing `SqlServerFixture`) |
| Frontend (`useGamificationApi`, `GamificationProgressCard`, `RankingList`, dashboard embedding) | none | `ShapeUp-Web` has no test framework today (pre-existing gap, tracked in root `GAPS.md`, out of scope for this feature — same conclusion `workout-editor` already reached) | `ShapeUp-Web/src/**` | `npm --prefix ShapeUp-Web run lint && npm --prefix ShapeUp-Web run build` |

## Parallelism Assessment

> Generated from codebase — confirm before Execute.

| Test Type | Parallel-Safe? | Isolation Model | Evidence |
|---|---|---|---|
| unit (xUnit + Moq) | Yes | Per-`[Fact]` mocked dependencies, no shared fixture | Existing unit suite pattern (`CreateWorkoutPlanHandlerTests.cs` etc.) |
| integration (real RabbitMQ + Mongo replica set + SQL Server) | No | Shared broker/queues/replica-set/SQL Server across the whole run | Same conclusion as `event-bus`'s own tasks.md; `[Collection("SQL Server Write Operations")]` already the established pattern for shared-SQL-Server tests |

## Gate Check Commands

> Generated from codebase — confirm before Execute.

| Gate Level | When to Use | Command |
|---|---|---|
| Quick | After tasks with unit tests only | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| Full | After tasks with integration tests | `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` |
| Build | Config/entity-only tasks | `dotnet build ShapeUpApi/src/ShapeUp.csproj` |

**Baseline before this feature** (per `event-bus`'s closing Handoff): unit **238/238**, integration **220 passed / 0 failed / 7 skipped**; frontend lint **0 errors** (6 pre-existing unrelated warnings, per `workout-editor`'s closing state).

---

## Execution Plan

### Phase 1: Foundation (Sequential)

```
T1 ──→ T2
```

### Phase 2: Anti-Cheat + Streak/Level (Parallel OK after Phase 1)

```
T2 ──┬→ T3 [P]
     ├→ T4 [P]
     └→ T5 [P]
```

### Phase 3: Consumer (Sequential, depends on Phase 2)

```
T3, T4, T5 ──→ T6
```

### Phase 4: Read Endpoints (Parallel OK after Phase 3)

```
T6 ──┬→ T7 ──→ T8 [P]
     │         └→ T9 [P]  (T8, T9 both depend on T7's ShapeScoreCalculator)
```

### Phase 5: End-to-End Verification (Sequential — shared real infra)

```
T8, T9 ──→ T10 ──→ T11 ──→ T12 ──→ T13
```

### Phase 6: Frontend (T16→T17 sequential; T18 order-free relative to that chain)

```
T8, T9 ──→ T15 ──┬→ T16 ──→ T17
                 └→ T18 [P]
```

### Phase 7: Final Gate (Sequential)

```
T13, T17, T18 ──→ T19
```

---

## Task Breakdown

### T1: `ActivityClassification` enum + `GamificationProfile`/`WorkoutEvaluation` entities

**What**: `enum ActivityClassification { Verified, LikelyValid, Suspicious, Invalid }`, `GamificationProfile` (UserId PK, TotalXp, Level, CurrentStreak, LastActivityDateUtc, ShapeCoins, LastStreakMilestoneAwarded, LastEvaluationLeveledUp/LevelFrom/LevelTo/StreakMilestoneHit/StreakMilestoneValue, UpdatedAtUtc), `WorkoutEvaluation` (SessionId PK, UserId, Classification, Reason, CreditGranted, EvaluatedAtUtc)
**Where**: `Features/Gamification/Shared/Enums/ActivityClassification.cs`, `Features/Gamification/Shared/Entities/{GamificationProfile,WorkoutEvaluation}.cs`
**Depends on**: None
**Reuses**: `GymManagementDbContext`'s entity style
**Requirement**: GAM-01..11 (foundation for all)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] All 3 types defined, compile
- [x] Gate check passes: `dotnet build ShapeUpApi/src/ShapeUp.csproj`

**Tests**: none
**Gate**: build

**Commit**: `feat(gamification): add ActivityClassification enum and GamificationProfile/WorkoutEvaluation entities`

---

### T2: `GamificationDbContext` + EF Core migration + DI registration

**What**: New `GamificationDbContext(DbContextOptions<GamificationDbContext>)` with `DbSet<GamificationProfile> Profiles`, `DbSet<WorkoutEvaluation> Evaluations`, Fluent config (PK, required fields, indexes — e.g. index on `WorkoutEvaluation.UserId` + `EvaluatedAtUtc` for the duplicate-lookup query). First EF Core migration. Registered in `Program.cs`/DI following the `GymManagementDbContext` pattern (same `ConnectionStrings:DefaultConnection`, same SQL Server)
**Where**: `Features/Gamification/Infrastructure/Data/GamificationDbContext.cs`, `Features/Gamification/Infrastructure/Migrations/*`
**Depends on**: T1
**Reuses**: `GymManagementDbContext.cs` as the template
**Requirement**: GAM-01..11 (storage foundation)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] `GamificationDbContext` compiles, migration generated and applies cleanly (`dotnet ef database update`)
- [x] DI registration wired in `Program.cs`
- [x] Gate check passes: `dotnet build ShapeUpApi/src/ShapeUp.csproj`

**Tests**: none
**Gate**: build

**Commit**: `feat(gamification): add GamificationDbContext, first migration, DI registration`

---

### T3: `AntiCheatClassifier` (3 rules + "worst wins" aggregation) [P]

**What**: `IAntiCheatClassifier`/`AntiCheatClassifier` implementing the 3 graded rules from spec GAM-03 (duration-ratio, duplication exact/partial, volume-vs-baseline) and combining via worst-wins aggregation
**Where**: `Features/Gamification/Shared/AntiCheat/{IAntiCheatClassifier,AntiCheatClassifier}.cs`
**Depends on**: T2
**Reuses**: `WorkoutSessionDocument` (Training, read-only)
**Requirement**: GAM-03

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Duration rule: dedicated test at every boundary (`r=0.29/0.3/0.31`, `0.49/0.5/0.51`, `0.79/0.8/0.81`) plus the degenerate empty-`Exercises` case (→ `Invalid`)
- [ ] Duplication rule: exact match → `Invalid`, ≥80% partial match → `Suspicious`, <80% → `Verified`, boundary at exactly 80%
- [ ] Volume rule: <3 prior sessions → `LikelyValid`; boundary tests at `3x`/`6x` the baseline average
- [ ] Aggregation: at least one test per pair of differing votes confirming "worst wins" (e.g. `Invalid`+`Verified`→`Invalid`; `Suspicious`+`LikelyValid`→`Suspicious`)
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing (238) + new cases, all pass

**Tests**: unit
**Gate**: quick

**Commit**: `feat(gamification): add AntiCheatClassifier with graded duration/duplication/volume rules`

---

### T4: `StreakCalculator` [P]

**What**: Pure function/class computing streak transition: increment (next day), no-op (same day), reset to 1 (gap>1 day), given `LastActivityDateUtc` + `CurrentStreak` + the new activity's date
**Where**: `Features/Gamification/Shared/StreakCalculator.cs`
**Depends on**: T2
**Reuses**: same day-based semantics as Training's existing `GetTrainingDashboardHandler.CalculateConsecutiveDays` (not called directly — reimplemented as an incremental/event-driven version per design, but same definition of "consecutive")
**Requirement**: GAM-06

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Increment, same-day no-op, and gap-reset cases all covered
- [ ] Brand-new user (no `LastActivityDateUtc`) starts at streak 1
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing + new cases

**Tests**: unit
**Gate**: quick

**Commit**: `feat(gamification): add StreakCalculator`

---

### T5: `LevelCalculator` [P]

**What**: `Level = floor(TotalXp / 500) + 1`
**Where**: `Features/Gamification/Shared/LevelCalculator.cs`
**Depends on**: T2
**Reuses**: nothing — new, trivial
**Requirement**: GAM-07

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Correct level at XP=0, at exactly a threshold (e.g. XP=500), and mid-level
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing + new cases

**Tests**: unit
**Gate**: quick

**Commit**: `feat(gamification): add LevelCalculator`

---

### T6: `GamificationWorkoutFinishedConsumer` (real consumer, replaces PoC)

**What**: `IConsumer<WorkoutFinished>` that: (1) short-circuits if `WorkoutEvaluation` already has this `SessionId`; (2) fetches the session (`IWorkoutSessionRepository.GetByIdAsync`) and recent sessions (`GetCompletedByUserInRangeAsync`); (3) classifies via `AntiCheatClassifier`; (4) if `Verified`/`LikelyValid`: credits 50 XP + 10 ShapeCoins, updates streak (`StreakCalculator`), recalculates level (`LevelCalculator`), applies the 7-day streak-milestone bonus (+50 coins, once per milestone) — else no credit; (5) always persists `WorkoutEvaluation` and the "what changed" snapshot on `GamificationProfile`. Registered in `MessagingExtensions.cs` (`bus.AddConsumer<GamificationWorkoutFinishedConsumer>()`), PoC `WorkoutFinishedConsumer` removed
**Where**: `Features/Gamification/WorkoutFinished/GamificationWorkoutFinishedConsumer.cs`, `Configurations/MessagingExtensions.cs` (edited)
**Depends on**: T3, T4, T5
**Reuses**: `IWorkoutSessionRepository`, `AntiCheatClassifier`, `StreakCalculator`, `LevelCalculator`
**Requirement**: GAM-01, GAM-02, GAM-06, GAM-07, GAM-09, GAM-11

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [x] Credit path: `Verified`/`LikelyValid` → XP+coins credited, streak/level updated, evaluation persisted with `CreditGranted=true`
- [x] Withhold path: `Suspicious`/`Invalid` → no credit, evaluation persisted with `CreditGranted=false`
- [x] Idempotency: second call with an already-evaluated `SessionId` is a no-op (no double credit)
- [x] Streak-milestone bonus fires exactly once per 7-multiple
- [x] "What changed" snapshot (`LastEvaluationLeveledUp`, etc.) reflects the actual transition when one occurs
- [x] PoC `WorkoutFinishedConsumer` removed from `MessagingExtensions.cs`
- [x] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [x] Test count: existing + new cases

**Tests**: unit
**Gate**: quick

**Commit**: `feat(gamification): add real WorkoutFinished consumer (classify, credit, streak/level, milestone bonus), remove PoC`

---

### T7: `ShapeScoreCalculator`

**What**: Computes the v1 ShapeScore (average of 4 sub-scores: Consistência, Evolução, Metas, Verificação) over a rolling 30-day window, per spec GAM-10 AC1
**Where**: `Features/Gamification/Shared/ShapeScoreCalculator.cs`
**Depends on**: T6
**Reuses**: `GamificationDbContext.Evaluations` (Verificação sub-score), `IWorkoutSessionRepository`/`WorkoutPrDocumentValueObject` (Evolução), Training's `SessionsTargetPerWeek` concept (Metas)
**Requirement**: GAM-10

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Each of the 4 sub-scores computed correctly in isolation, then averaged
- [ ] User with zero activity in the window → ShapeScore 0 (no error)
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing + new cases

**Tests**: unit
**Gate**: quick

**Commit**: `feat(gamification): add ShapeScoreCalculator (v1 formula)`

---

### T8: `GetGamificationProfileHandler` + `GamificationProfileController` (`GET /api/gamification/me`) [P]

**What**: CQRS query handler + controller returning the caller's own XP, level, streak, ShapeCoins, ShapeScore, and last-evaluation "what changed" signal — zeroed (not 404) when no `GamificationProfile` row exists yet
**Where**: `Features/Gamification/GetGamificationProfile/*`
**Depends on**: T7
**Reuses**: existing CQRS handler/controller/`ResultPattern` conventions
**Requirement**: GAM-08, GAM-11

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Existing profile returns real values; missing profile returns zeroed response, never an error
- [ ] Response includes the "what changed" fields from `GamificationProfile`
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing + new cases

**Tests**: unit
**Gate**: quick

**Commit**: `feat(gamification): add GET /api/gamification/me`

---

### T9: `GetRankingHandler` + `RankingController` (`GET /api/gamification/ranking`) [P]

**What**: Keyset-paginated (AGENTS.md mandatory — no offset/skip) global ranking by ShapeScore desc
**Where**: `Features/Gamification/GetRanking/*`
**Depends on**: T7
**Reuses**: existing keyset-pagination convention
**Requirement**: GAM-10

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Returns users ordered by ShapeScore desc, `items` + `nextCursor` shape (matches existing paginated-endpoint convention)
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing + new cases

**Tests**: unit
**Gate**: quick

**Commit**: `feat(gamification): add GET /api/gamification/ranking (keyset-paginated)`

---

### T10: Integration test — real end-to-end happy path

**What**: Finish a real, plausible workout via the real endpoint (real RabbitMQ + Mongo replica set + SQL Server), confirm the real consumer credits XP/coins, updates streak/level, and `GET /api/gamification/me` reflects it
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Gamification/GamificationEndToEndTests.cs` (new)
**Depends on**: T8, T9
**Reuses**: `IntegrationWebApplicationFactory`, `SqlServerFixture`, the messaging test infrastructure already built for `event-bus`
**Requirement**: GAM-01, GAM-06, GAM-07, GAM-08

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Finishing a plausible workout results in `GET /me` showing +50 XP, +10 coins, streak=1 (or incremented), correct level
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Gamification`
- [ ] Test count: existing (220) + 1 new case

**Tests**: integration
**Gate**: full

**Commit**: `test(gamification): cover end-to-end happy path (finish workout -> credit -> profile read)`

---

### T11: Integration test — anti-cheat boundary scenarios end-to-end

**What**: Through the real consumer (real infra), force each of the 3 rules' worst-case boundary (impossible-duration session, exact-duplicate session, 4x-volume-spike session) and confirm no credit + correct `WorkoutEvaluation` classification persisted
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Gamification/GamificationAntiCheatEndToEndTests.cs` (new)
**Depends on**: T10
**Reuses**: same fixture as T10
**Requirement**: GAM-03, GAM-04, GAM-05

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Each of the 3 forced scenarios ends with the user's XP/coins unchanged and the correct classification persisted
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Gamification`
- [ ] Test count: existing + 3 new cases

**Tests**: integration
**Gate**: full

**Commit**: `test(gamification): cover anti-cheat classification end-to-end (no credit on Suspicious/Invalid)`

---

### T12: Integration test — idempotency (redelivery doesn't double-credit)

**What**: Redeliver the same `WorkoutFinished` message (simulated via the bus's own redelivery, or a direct second `Consume` call against the same `SessionId`) and confirm XP/coins are credited exactly once
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Gamification/GamificationIdempotencyTests.cs` (new)
**Depends on**: T10
**Reuses**: same fixture as T10
**Requirement**: GAM-02

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Double-delivery of the same event results in exactly one credit
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Gamification`
- [ ] Test count: existing + 1 new case

**Tests**: integration
**Gate**: full

**Commit**: `test(gamification): cover idempotent crediting on event redelivery`

---

### T13: Integration test — ranking with multiple users

**What**: Seed 3+ users with different histories (different ShapeScores), call `GET /ranking`, confirm order and pagination
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Gamification/GamificationRankingTests.cs` (new)
**Depends on**: T10
**Reuses**: same fixture as T10
**Requirement**: GAM-10

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Users returned in ShapeScore-desc order; pagination (`nextCursor`) works across a page boundary
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Gamification`
- [ ] Test count: existing + 1 new case

**Tests**: integration
**Gate**: full

**Commit**: `test(gamification): cover global ranking ordering and pagination`

---

### T15: `useGamificationApi.js` hook (frontend)

**What**: New hook exposing `getGamificationProfile()` and `getRanking(cursor, pageSize)`, same shape/conventions as `useTrainingApi.js` (passthrough body, `useCallback` per method)
**Where**: `ShapeUp-Web/src/hooks/api/useGamificationApi.js` (new)
**Depends on**: T8, T9 (backend endpoints must exist)
**Reuses**: `useTrainingApi.js` structure verbatim
**Requirement**: GAM-12, GAM-13

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Both methods call the real endpoints, same pattern as every other API hook
- [ ] `npm --prefix ShapeUp-Web run lint` passes

**Tests**: none
**Gate**: build

**Commit**: `feat(web): add useGamificationApi hook (profile, ranking)`

---

### T16: `GamificationProgressCard` component

**What**: Presentational card showing XP/level-progress bar, streak, ShapeCoins, ShapeScore — zeroed state handled explicitly (not blank/broken) per spec GAM-12 AC3. Uses a `lucide-react` icon distinct from `Flame` (reserved for Training's existing streak card) to keep the two streak concepts visually distinct
**Where**: `ShapeUp-Web/src/components/gamification/GamificationProgressCard.jsx` (new)
**Depends on**: T15
**Reuses**: `Card`, existing `su-metric-card` CSS family, no new UI library
**Requirement**: GAM-12

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Renders all 5 values correctly given a populated profile
- [ ] Renders an explicit "no history yet" state given a zeroed profile (never a blank/broken card)
- [ ] `npm --prefix ShapeUp-Web run lint` passes

**Tests**: none
**Gate**: build

**Commit**: `feat(web): add GamificationProgressCard component`

---

### T17: Embed `GamificationProgressCard` into `DashboardClient.jsx`/`DashboardIndependent.jsx`

**What**: Fetch the profile via `useGamificationApi` in both dashboard pages and render the card alongside existing metric cards (does not touch/replace the existing Training-streak card — both coexist, per spec's documented Assumption that the two streaks can legitimately diverge)
**Where**: `ShapeUp-Web/src/pages/Dashboard/DashboardClient.jsx`, `ShapeUp-Web/src/pages/Dashboard/DashboardIndependent.jsx` (edited)
**Depends on**: T16
**Reuses**: existing dashboard data-fetching pattern (whatever hook-call convention each page already uses for its own metrics)
**Requirement**: GAM-12

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Both dashboards render the new card without disturbing existing cards/layout
- [ ] `npm --prefix ShapeUp-Web run lint` passes
- [ ] `npm --prefix ShapeUp-Web run build` passes

**Tests**: none
**Gate**: build

**Commit**: `feat(web): embed GamificationProgressCard in client/independent dashboards`

---

### T18: `RankingList` component + integration point

**What**: Paginated (keyset) global ranking list, own-user row highlighted if present on the current page
**Where**: `ShapeUp-Web/src/components/gamification/RankingList.jsx` (new), wired into wherever the product wants it surfaced (a dashboard section by default, per design.md — no dedicated route/nav-entry precedent to justify one now)
**Depends on**: T15
**Reuses**: `Card`, existing cursor-pagination consumption pattern already used elsewhere in the app
**Requirement**: GAM-13

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] Renders entries in ShapeScore-desc order, own-user row visually highlighted when present
- [ ] "Load more"/pagination works across a page boundary
- [ ] `npm --prefix ShapeUp-Web run lint` passes
- [ ] `npm --prefix ShapeUp-Web run build` passes

**Tests**: none
**Gate**: build

**Commit**: `feat(web): add RankingList component`

---

### T19: Full-stack gate — final smoke

**What**: Run the complete gate across both repos and confirm every GAM requirement is covered
**Where**: N/A (verification task)
**Depends on**: T1–T18 (all)
**Reuses**: N/A
**Requirement**: All GAM-01..13 (final confirmation)

**Tools**: MCP: NONE / Skill: NONE

**Done when**:
- [ ] `dotnet build ShapeUpApi/src/ShapeUp.csproj` passes
- [ ] `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` passes, full count reported
- [ ] `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` passes, full count reported
- [ ] `npm --prefix ShapeUp-Web run lint && npm --prefix ShapeUp-Web run build` passes
- [ ] Every GAM-NN requirement in spec.md's traceability table marked Verified

**Tests**: none (aggregation gate)
**Gate**: full

**Commit**: `chore(gamification): final gate for gamification feature (GAM-01..13)`

---

## Parallel Execution Map

```
Phase 1 (Sequential):
  T1 ──→ T2

Phase 2 (Parallel):
  T2 complete, then:
    ├── T3 [P]
    ├── T4 [P]
    └── T5 [P]

Phase 3 (Sequential):
  T3, T4, T5 complete, then:
    T6

Phase 4 (T7 sequential, T8/T9 parallel after):
  T6 → T7 → ┬── T8 [P]
            └── T9 [P]

Phase 5 (Sequential - shared real infra):
  T8, T9 complete, then:
    T10 → T11 → T12 → T13

Phase 6 (Frontend):
  T8, T9 complete, then:
    T15 ──┬── T16 ──→ T17
          └── T18 [P]

Phase 7 (Final Gate):
  T13, T17, T18 complete, then:
    T19
```

**Parallelism constraint:** T3/T4/T5 are unit-tested (parallel-safe) and only depend on T2 — order-free among themselves. T8/T9 both depend only on T7, order-free among themselves. All integration tasks (T10-T13) share real infra — sequential. T16/T18 both depend only on T15, order-free among themselves.

**How phase-based execution works**: 7 phases > 3 → per the skill's Sub-Agent Delegation rule, offer one worker per phase (sequential) before starting Execute.

---

## Task Granularity Check

| Task | Scope | Status |
|---|---|---|
| T1: Enum + 2 entities | 3 files, 1 concept (data shapes) | ✅ Granular (cohesive) |
| T2: DbContext + migration + DI | 2-3 files, 1 concept | ✅ Granular (cohesive) |
| T3: AntiCheatClassifier | 2 files (+ test file) | ✅ Granular |
| T4: StreakCalculator | 1 file (+ test file) | ✅ Granular |
| T5: LevelCalculator | 1 file (+ test file) | ✅ Granular |
| T6: Consumer + PoC removal | 2 files (+ test file) | ✅ Granular (cohesive) |
| T7: ShapeScoreCalculator | 1 file (+ test file) | ✅ Granular |
| T8: Profile endpoint | 2-3 files (handler+controller+response, 1 concept) | ✅ Granular (cohesive) |
| T9: Ranking endpoint | 2-3 files, 1 concept | ✅ Granular (cohesive) |
| T10-T13: Integration scenarios | 1 file each | ✅ Granular |
| T15: useGamificationApi hook | 1 file | ✅ Granular |
| T16: GamificationProgressCard | 1 file (+ CSS) | ✅ Granular |
| T17: Dashboard embedding | 2 files (DashboardClient/Independent) | ✅ Granular (cohesive) |
| T18: RankingList | 1 file (+ CSS) | ✅ Granular |
| T19: Final gate | 0 files (verification) | ✅ Granular |

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
|---|---|---|---|
| T1 | None | None | ✅ Match |
| T2 | T1 | T1→T2 | ✅ Match |
| T3 | T2 | T2→T3 | ✅ Match |
| T4 | T2 | T2→T4 | ✅ Match |
| T5 | T2 | T2→T5 | ✅ Match |
| T6 | T3, T4, T5 | T3,T4,T5→T6 | ✅ Match |
| T7 | T6 | T6→T7 | ✅ Match |
| T8 | T7 | T7→T8 | ✅ Match |
| T9 | T7 | T7→T9 | ✅ Match |
| T10 | T8, T9 | T8,T9→T10 | ✅ Match |
| T11 | T10 | T10→T11 | ✅ Match |
| T12 | T10 | T10(→T11)→T12 sequenced in Phase 5's linear chain; T12's real dependency is T10 only, not T11 — noted here to avoid ambiguity | ✅ Match (scheduling within the phase, not a hard dependency) |
| T13 | T10 | same note as T12 | ✅ Match |
| T15 | T8, T9 | WEB→API (via T8/T9 endpoints) | ✅ Match |
| T16 | T15 | hook→WIDGET | ✅ Match |
| T17 | T16 | WIDGET embedded in dashboard | ✅ Match |
| T18 | T15 | hook→WIDGET (ranking) | ✅ Match |
| T19 | T1-T18 | T13,T17,T18→T19 | ✅ Match |

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
|---|---|---|---|---|
| T1 | Enum/entities | none | none | ✅ OK |
| T2 | DbContext/migration | none | none | ✅ OK |
| T3 | AntiCheatClassifier | unit | unit | ✅ OK |
| T4 | StreakCalculator | unit | unit | ✅ OK |
| T5 | LevelCalculator | unit | unit | ✅ OK |
| T6 | Consumer | unit | unit | ✅ OK |
| T7 | ShapeScoreCalculator | unit | unit | ✅ OK |
| T8 | Query handler + controller | unit | unit | ✅ OK |
| T9 | Query handler + controller | unit | unit | ✅ OK |
| T10-T13 | End-to-end scenarios | integration | integration | ✅ OK |
| T15 | Frontend hook | none (build-gate only) | none | ✅ OK |
| T16 | Frontend component | none (build-gate only) | none | ✅ OK |
| T17 | Dashboard embedding | none (build-gate only) | none | ✅ OK |
| T18 | Frontend component | none (build-gate only) | none | ✅ OK |
| T19 | Aggregation | none | none | ✅ OK |

All ✅ — no restructuring needed.

---

## MCPs and Skills — confirm before Execute

`Tools` is `NONE`/`NONE` for every task above (no project MCP or skill beyond `tlc-spec-driven` itself found configured for this repo).

- Same as `event-bus`: terminal direto (Bash) for any `docker compose`/`dotnet ef` work — carrying that preference forward unless you say otherwise.
- 7 phases > 3 → sub-agent per phase vs inline: your call, same open question as before.

**Status**: waiting on your go-ahead to start Execute (T1).
</content>
