# Event Bus (Outbox + Pluggable Broker) — Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user — do not proceed without it.**

---

**Design**: `ShapeUpApi/.specs/features/event-bus/design.md`
**Status**: Draft

---

## Test Coverage Matrix

> Generated from codebase (`ShapeUpApi/src/AGENTS.md` — guideline found, no numeric coverage threshold; existing xUnit unit/integration patterns in `ShapeUpApi/tests/`) and spec. Confirm before Execute.

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
|---|---|---|---|---|
| Spike/compat proof (packages + minimal round-trip) | integration (real infra: RabbitMQ + Mongo replica set) | Proves the two flagged uncertainties in design.md Risks — this IS the test, not optional | `ShapeUpApi/tests/IntegrationTests/Domains/Messaging/**` | `dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` (after `docker compose up -d mongo rabbitmq`) |
| Docker-compose / infra config (RabbitMQ service, Mongo replica set) | none | Build/start gate only — correctness proven transitively by every integration test below actually connecting | `docker-compose.yml` | `docker compose up -d mongo rabbitmq` succeeds, containers healthy |
| Bus registration (`MessagingExtensions.cs`) | none | Config/wiring — correctness proven transitively by T1/T8 actually publishing/consuming through it | `ShapeUpApi/src/Configurations/MessagingExtensions.cs` | `dotnet build ShapeUpApi/src/ShapeUp.csproj` |
| Event contract (`WorkoutFinished` record) | none | Plain DTO, no branching logic | `ShapeUpApi/src/Features/Training/Shared/Events/**` | `dotnet build ShapeUpApi/src/ShapeUp.csproj` |
| `FinishWorkoutExecutionHandler` extension (publish call) | unit | 1:1 to spec AC (correct event payload on success path); existing handler tests are the floor (`FinishWorkoutExecutionHandlerTests.cs`) | `ShapeUpApi/tests/UnitTests/Domains/Training/Workouts/**` | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| `WorkoutFinishedConsumer` (example) | unit | Consumer processes the event and logs — every listed edge case (already-processed dedupe is MassTransit's own concern, tested at integration level below) | `ShapeUpApi/tests/UnitTests/Domains/Training/Workouts/**` | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| End-to-end pipe (publish→broker→consume, retry, dead-letter, restart-resilience, broker-down resilience, duplicate delivery) | integration | Every P1 acceptance criterion in spec.md gets a dedicated scenario — happy path + every listed edge case | `ShapeUpApi/tests/IntegrationTests/Domains/Messaging/**` | `dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` (after `docker compose up -d mongo rabbitmq`) |

## Parallelism Assessment

> Generated from codebase — confirm before Execute.

| Test Type | Parallel-Safe? | Isolation Model | Evidence |
|---|---|---|---|
| unit (xUnit + Moq) | Yes | Per-`[Fact]` mocked dependencies, no shared fixture — same pattern as `CreateWorkoutPlanHandlerTests.cs` | Existing unit test suite already runs this way |
| integration — real infra (RabbitMQ + Mongo replica set) | No | Shared broker/queues and shared Mongo replica set across the whole run — same class of constraint as `[Collection("SQL Server Write Operations")]` | `WorkoutPlansEndpointsIntegrationTests.cs:16` (existing analogous pattern); no evidence this feature's tests can safely run concurrently against one shared RabbitMQ/Mongo instance |
| integration — MassTransit in-memory test harness | No (conservative default) | Harness isolation per test class not independently verified in this session — per the skill's own rule, undetermined parallel-safety defaults to sequential | None found; verify in T1's spike and revisit if confirmed safe |

## Gate Check Commands

> Generated from codebase — confirm before Execute.

| Gate Level | When to Use | Command |
|---|---|---|
| Quick | After tasks with unit tests only | `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` |
| Full | After tasks with integration tests | `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` |
| Build | Config/entity-only tasks | `dotnet build ShapeUpApi/src/ShapeUp.csproj` |

---

## Execution Plan

### Phase 1: Spike + Infra (Sequential — everything else depends on this surviving)

```
T1 ──→ T2 ──→ T3
```

### Phase 2: Bus Registration (Sequential, depends on Phase 1)

```
T3 ──→ T4
```

### Phase 3: Event Contract, Publisher, Consumer (Parallel OK after Phase 2)

```
T4 ──┬→ T5 ──┐
     ├→ T6 [P]│ (T6 needs T5's contract, so T5→T6, not parallel with T5)
     └→ T7 [P]│ (T7 needs T5's contract too, so T5→T7)
```

### Phase 4: End-to-End Verification (Sequential — same shared infra, one scenario at a time)

```
T5,T6,T7 ──→ T8 ──→ T9 ──→ T10 ──→ T11 ──→ T12
```

### Phase 5: Final Gate (Sequential)

```
T12 ──→ T13
```

---

## Task Breakdown

### T1: SPIKE — verify MassTransit compat + Mongo-outbox atomicity (timeboxed)

**What**: Add `MassTransit`, `MassTransit.RabbitMQ`, `MassTransit.MongoDb` NuGet packages. Build the smallest possible publish→consume round trip against real RabbitMQ + a Mongo replica set (local, ad-hoc — not the final `docker-compose.yml` yet) to confirm: (a) the packages build and run cleanly on `net10.0` (preview SDK), (b) `AddMongoDbOutbox` genuinely gives atomicity between a domain document write and the outbox entry — write a document via a plain `IMongoCollection` call AND publish an event inside one outbox-managed scope, force a failure after the document write but before commit, confirm neither persists
**Where**: Throwaway spike project/test — result gets folded into `Configurations/MessagingExtensions.cs` (T4) once confirmed; do not build production code before this passes
**Depends on**: None
**Reuses**: N/A (first messaging code in the repo)
**Requirement**: EVTB-01, EVTB-02 (feasibility proof — this is the "T1" design.md's Risks & Concerns table refers to)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [x] Packages added, build succeeds on `net10.0`
- [x] Round trip proves atomicity (forced-failure scenario: neither document nor event exists after rollback)
- [x] **If either check fails**: STOP, do not proceed to T2 — report the concrete failure to the user and revisit the approach (per design.md Risks & Concerns — this is a real go/no-go gate, not a formality)
- [x] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Messaging`

**Tests**: integration (real infra)
**Gate**: full

**Commit**: `spike(messaging): verify MassTransit + Mongo outbox atomicity on net10.0`

---

### T2: `docker-compose.yml` — add RabbitMQ, reconfigure Mongo as single-node replica set

**What**: Add a `rabbitmq:3-management` service (matches the project's self-hosted-no-SaaS convention already used for Seq). Reconfigure the existing `mongo` service to start with `--replSet rs0` and run the one-time `rs.initiate()` (via a compose healthcheck/init script or documented manual step)
**Where**: `docker-compose.yml`
**Depends on**: T1 (spike must pass before committing to this infra shape)
**Reuses**: existing `seq` service block as the self-hosted pattern template
**Requirement**: EVTB-02 (AD-010 in STATE.md)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [x] `docker compose up -d mongo rabbitmq` starts both services healthy
- [x] `mongosh --eval "rs.status()"` (or driver-level check) confirms the replica set is initiated and primary
- [x] RabbitMQ management UI reachable on its mapped port, bound to the dev network only (same caution as Seq's existing comment)
- [x] Gate check passes: build (no app code changed yet, this is infra-only)

**Tests**: none
**Gate**: build

**Commit**: `chore(infra): add RabbitMQ service, reconfigure Mongo as single-node replica set`

---

### T3: Re-run full existing test suite against the new Mongo config

**What**: Run the complete existing unit + integration suite (236 unit + 218 integration, pre-this-feature baseline) against the reconfigured (replica-set) Mongo, before any new messaging code exists, to catch any regression the infra change itself introduces
**Where**: N/A (verification task, no new files)
**Depends on**: T2
**Reuses**: N/A
**Requirement**: N/A — regression gate for AD-010, not a WOED/EVTB requirement itself

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` — 236/236 (same count as before this feature)
- [ ] `dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` — 218/218 (same count as before this feature)
- [ ] **If any existing test breaks**: STOP, fix before proceeding to Phase 2 — a regression here means the replica-set change itself is unsafe, not something to paper over

**Tests**: none (aggregation/regression gate)
**Gate**: full

**Commit**: `chore(infra): confirm no regression after Mongo replica-set migration (236/236, 218/218)`

---

### T4: `MessagingExtensions.cs` — register MassTransit (RabbitMQ transport + Mongo outbox)

**What**: New `IServiceCollection.AddMessaging(IConfiguration)` extension method: `AddMassTransit` with `UsingRabbitMq` transport config (host/credentials from configuration, following the existing `ConnectionStrings`/`Mongo__Training__...` env-var convention) and `AddMongoDbOutbox` pointed at Training's existing `IMongoDatabase`. Called once from `Program.cs`
**Where**: `ShapeUpApi/src/Configurations/MessagingExtensions.cs` (new), `Program.cs` (edited, one line added)
**Depends on**: T3
**Reuses**: existing `DependencyInjectionExtensions.cs` pattern (one `Add*` method per concern), existing Training Mongo DI registration
**Requirement**: EVTB-04, EVTB-09

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `AddMessaging` registers RabbitMQ transport + Mongo outbox, callable from `Program.cs`
- [ ] App starts successfully with the new registration (no DI resolution errors)
- [ ] Gate check passes: `dotnet build ShapeUpApi/src/ShapeUp.csproj`

**Tests**: none
**Gate**: build

**Commit**: `feat(messaging): register MassTransit with RabbitMQ transport and Mongo outbox`

---

### T5: `WorkoutFinished` event contract

**What**: New plain record `WorkoutFinished(string SessionId, int TargetUserId, int ExecutedByUserId, DateTime EndedAtUtc)`
**Where**: `ShapeUpApi/src/Features/Training/Shared/Events/WorkoutFinished.cs` (new)
**Depends on**: T4
**Reuses**: N/A
**Requirement**: EVTB-10

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Record defined, compiles
- [ ] Gate check passes: `dotnet build ShapeUpApi/src/ShapeUp.csproj`

**Tests**: none
**Gate**: build

**Commit**: `feat(training): add WorkoutFinished event contract`

---

### T6: Extend `FinishWorkoutExecutionHandler` to publish `WorkoutFinished`

**What**: Inject `IPublishEndpoint` into `FinishWorkoutExecutionHandler`; after the existing successful completion write, call `await publishEndpoint.Publish(new WorkoutFinished(...))` with the correct field mapping from the session being completed
**Where**: `ShapeUpApi/src/Features/Training/Workouts/FinishWorkoutExecution/FinishWorkoutExecutionHandler.cs` (edited)
**Depends on**: T5
**Reuses**: existing handler structure/tests (`FinishWorkoutExecutionHandlerTests.cs` is the floor)
**Requirement**: EVTB-01, EVTB-10

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Handler publishes `WorkoutFinished` with correct `SessionId`/`TargetUserId`/`ExecutedByUserId`/`EndedAtUtc` on the success path
- [ ] Existing `FinishWorkoutExecutionHandlerTests.cs` cases still pass with `IPublishEndpoint` mocked, PLUS a new case asserting `Publish` was called with the exact expected payload
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing count + 1 new case, all pass

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): publish WorkoutFinished when a workout session completes`

---

### T7: `WorkoutFinishedConsumer` (example consumer)

**What**: New `class WorkoutFinishedConsumer : IConsumer<WorkoutFinished>` — `Consume` logs the event via `ILogger` (structured log: SessionId, TargetUserId). This is a throwaway proof-of-concept; Gamification's real consumer replaces it later (per spec Out of Scope)
**Where**: `ShapeUpApi/src/Features/Training/Workouts/FinishWorkoutExecution/WorkoutFinishedConsumer.cs` (new)
**Depends on**: T5
**Reuses**: existing `ILogger`/OpenTelemetry pipeline
**Requirement**: EVTB-10

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Consumer logs the event's key fields when `Consume` is called
- [ ] Unit test asserts the log call happens with the expected event data (mock `ILogger`, or `ConsumeContext<WorkoutFinished>`)
- [ ] Gate check passes: `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj`
- [ ] Test count: existing count + 1 new case

**Tests**: unit
**Gate**: quick

**Commit**: `feat(training): add WorkoutFinishedConsumer example consumer`

---

### T8: Integration test — real end-to-end round trip (happy path + atomicity)

**What**: Call the real `/finish` workout endpoint (via `IntegrationWebApplicationFactory`, same pattern as existing Training integration tests) against the real RabbitMQ + Mongo replica set (from T2). Assert the consumer (T7) receives and processes the event. Additionally: force a failure inside the transaction (test-only fault injection) to prove the AC2 rollback guarantee (neither session-completion write nor event survive)
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Messaging/WorkoutFinishedEndToEndTests.cs` (new)
**Depends on**: T6, T7
**Reuses**: `IntegrationWebApplicationFactory`, `SqlServerFixture`-equivalent pattern for Mongo/RabbitMQ setup (new fixture, same spirit)
**Requirement**: EVTB-01, EVTB-02, EVTB-10

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Finishing a real workout session results in the consumer observably processing `WorkoutFinished` (e.g., a test-visible side effect or captured log)
- [ ] Forced-failure scenario proves rollback: neither the session completion nor the event exist after
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Messaging`
- [ ] Test count: 2 new cases (happy path, forced-failure rollback), both pass

**Tests**: integration (real infra)
**Gate**: full

**Commit**: `test(messaging): cover WorkoutFinished end-to-end round trip and rollback atomicity`

---

### T9: Integration test — consumer retry + dead-letter

**What**: A test-only consumer variant that throws N times then succeeds (or always throws, to force exhaustion) — confirm MassTransit's built-in retry middleware re-attempts with backoff, and that exhausting retries moves the message to the `_error` (dead-letter) queue, observable via the pipeline (not silently dropped)
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Messaging/WorkoutFinishedRetryDeadLetterTests.cs` (new)
**Depends on**: T8
**Reuses**: same fixture as T8
**Requirement**: EVTB-08, EVTB-11

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] A consumer that always throws results in the message landing in the dead-letter queue after the configured retry limit, not lost
- [ ] The dead-letter event is observable (log/trace via existing OpenTelemetry pipeline)
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Messaging`
- [ ] Test count: existing + 1 new case

**Tests**: integration
**Gate**: full

**Commit**: `test(messaging): cover consumer retry exhaustion and dead-letter routing`

---

### T10: Integration test — restart resilience (pending outbox survives bus restart)

**What**: Write a document + outbox entry (bypass the normal publish call, insert directly), stop the bus/relay, restart it, confirm the pending event is still delivered — proves EVTB-06 without requiring an actual OS-process kill
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Messaging/WorkoutFinishedRestartResilienceTests.cs` (new)
**Depends on**: T8
**Reuses**: same fixture as T8

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] A pending outbox entry created before the bus (re)starts is delivered once it starts
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Messaging`
- [ ] Test count: existing + 1 new case

**Tests**: integration
**Gate**: full

**Commit**: `test(messaging): cover pending-event delivery across a bus restart`

---

### T11: Integration test — duplicate delivery is idempotent

**What**: Publish the same event (same `EventId`/message identity) twice manually, confirm the consumer's effect happens once (relies on MassTransit's own dedup window, or the consumer's own idempotency check if outside that window — whichever the design actually lands on, verified here)
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Messaging/WorkoutFinishedIdempotencyTests.cs` (new)
**Depends on**: T8
**Reuses**: same fixture as T8
**Requirement**: EVTB-07

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] Redelivering the identical event a second time does not duplicate the consumer's effect
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Messaging`
- [ ] Test count: existing + 1 new case

**Tests**: integration
**Gate**: full

**Commit**: `test(messaging): cover idempotent handling of a duplicate event delivery`

---

### T12: Integration test — broker-down resilience

**What**: Stop the RabbitMQ container mid-test (or block connectivity), publish an event (goes to outbox), confirm it stays `Pending` without crashing the app, then restore RabbitMQ and confirm delivery resumes
**Where**: `ShapeUpApi/tests/IntegrationTests/Domains/Messaging/WorkoutFinishedBrokerDownResilienceTests.cs` (new)
**Depends on**: T8
**Reuses**: same fixture as T8
**Requirement**: EVTB-05

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] With RabbitMQ down, publishing doesn't crash the app and the event remains pending
- [ ] With RabbitMQ restored, the pending event is delivered without manual intervention
- [ ] Gate check passes: `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Messaging`
- [ ] Test count: existing + 1 new case

**Tests**: integration
**Gate**: full

**Commit**: `test(messaging): cover broker-down resilience and recovery`

---

### T13: Full-stack gate — final smoke

**What**: Run the complete gate across the backend repo and confirm every EVTB requirement is covered
**Where**: N/A (verification task)
**Depends on**: T1–T12 (all)
**Reuses**: N/A
**Requirement**: All EVTB-01..11 (final confirmation)

**Tools**:
- MCP: NONE
- Skill: NONE

**Done when**:
- [ ] `dotnet build ShapeUpApi/src/ShapeUp.csproj` passes
- [ ] `dotnet test ShapeUpApi/tests/UnitTests/UnitTests.csproj` passes, full count reported
- [ ] `docker compose up -d mongo rabbitmq && dotnet test ShapeUpApi/tests/IntegrationTests/IntegrationTests.csproj` passes, full count reported
- [ ] Every EVTB-NN requirement in spec.md's traceability table marked Verified

**Tests**: none (aggregation gate)
**Gate**: full (with infra up)

**Commit**: `chore(messaging): final gate for event-bus feature (EVTB-01..11)`

---

## Parallel Execution Map

```
Phase 1 (Sequential - go/no-go gate):
  T1 ──→ T2 ──→ T3

Phase 2 (Sequential):
  T3 ──→ T4

Phase 3 (Mostly sequential - T6/T7 both need T5's contract first):
  T4 → T5 → T6
            └→ T7  (T6 and T7 are order-free relative to each other once T5 exists, but
                     both touch the same feature folder in the same phase - executed in
                     sequence for a cleaner diff, not because of a hard technical dependency)

Phase 4 (Sequential - shared real infra, one scenario at a time):
  T8 → T9 → T10 → T11 → T12

Phase 5 (Sequential):
  T12 → T13
```

**Parallelism constraint:** All integration tasks in this feature share real RabbitMQ + Mongo infra (Parallelism Assessment: not parallel-safe) — none are marked `[P]`. T6/T7 are unit-tested and technically order-free once T5 exists, but are sequenced for review clarity given they're both small.

**How phase-based execution works**: 5 phases > 3 → per the skill's Sub-Agent Delegation rule, offer one worker per phase (sequential) before starting Execute.

---

## Task Granularity Check

| Task | Scope | Status |
|---|---|---|
| T1: Spike | 1 concept (feasibility proof), throwaway code | ✅ Granular |
| T2: docker-compose infra | 1 file | ✅ Granular |
| T3: Regression run | 0 new files (verification) | ✅ Granular (explicit aggregation task) |
| T4: MessagingExtensions.cs | 2 files (1 new + 1-line Program.cs edit), 1 concept | ✅ Granular (cohesive) |
| T5: WorkoutFinished contract | 1 file | ✅ Granular |
| T6: Handler extension | 1 file (+ its test file) | ✅ Granular |
| T7: Example consumer | 1 file (+ its test file) | ✅ Granular |
| T8-T12: Integration scenarios | 1 file each | ✅ Granular |
| T13: Final gate | 0 files (verification) | ✅ Granular |

---

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
|---|---|---|---|
| T1 | None | None | ✅ Match |
| T2 | T1 | T1→T2 | ✅ Match |
| T3 | T2 | T2→T3 | ✅ Match |
| T4 | T3 | T3→T4 | ✅ Match |
| T5 | T4 | T4→T5 | ✅ Match |
| T6 | T5 | T5→T6 | ✅ Match |
| T7 | T5 | T5→T7 | ✅ Match |
| T8 | T6, T7 | T6,T7→T8 | ✅ Match |
| T9 | T8 | T8→T9 | ✅ Match |
| T10 | T8 | T8→T9→T10 (sequential chain, T10 depends transitively via T9 in the diagram's linear phase 4) | ✅ Match (T10's real dependency is T8; running after T9 in Phase 4's sequence is scheduling, not a hard dependency — noted here to avoid ambiguity) |
| T11 | T8 | same note as T10 | ✅ Match |
| T12 | T8 | same note as T10 | ✅ Match |
| T13 | T1-T12 | T12→T13 | ✅ Match |

---

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
|---|---|---|---|---|
| T1 | Spike/compat proof | integration | integration | ✅ OK |
| T2 | Infra config | none | none | ✅ OK |
| T3 | Aggregation | none | none | ✅ OK |
| T4 | Bus registration | none | none | ✅ OK |
| T5 | Event contract (DTO) | none | none | ✅ OK |
| T6 | Handler (domain logic) | unit | unit | ✅ OK |
| T7 | Consumer | unit | unit | ✅ OK |
| T8-T12 | End-to-end pipe scenarios | integration | integration | ✅ OK |
| T13 | Aggregation | none | none | ✅ OK |

All ✅ — no restructuring needed.

---

## MCPs and Skills — confirm before Execute

`Tools` is `NONE`/`NONE` for every task above (no project MCP or skill beyond `tlc-spec-driven` itself found configured for this repo).

- ✅ Tool preference for T1-T2 infra work (`docker compose`/`mongosh`): **terminal direto** (Bash) — confirmed.
- ⏳ 5 phases > 3 → sub-agent per phase vs inline: **still open** — user will say when it's time to execute.

**Status**: waiting on user's go-ahead to start Execute (T1).
</content>
