# Event Bus Validation

**Date**: 2026-09-09
**Spec**: `.specs/features/event-bus/spec.md`
**Diff range**: `121d3f0..20082b0`
**Verifier**: independent sub-agent (author ≠ verifier)

---

## Task Completion

| Task | Status  | Notes |
| ---- | ------- | ----- |
| T1   | ✅ Done | Spike + atomicity rollback |
| T2   | ✅ Done | docker-compose mongo replica set + RabbitMQ |
| T3   | ✅ Done | Regression gate |
| T4   | ✅ Done | MessagingExtensions |
| T5   | ✅ Done | WorkoutFinished contract |
| T6   | ✅ Done | Handler publish |
| T7   | ✅ Done | Example consumer |
| T8   | ✅ Done | E2E + rollback integration |
| T9   | ✅ Done | Retry + dead-letter integration |
| T10  | ✅ Done | Restart resilience integration |
| T11  | ✅ Done | Idempotency integration |
| T12  | ✅ Done | Broker-down resilience integration |
| T13  | ✅ Done | Final gate (author-reported) |

---

## Spec-Anchored Acceptance Criteria

### P1: Publisher grava evento atomicamente com sua escrita de domínio

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: handler + `Publish` in same transaction → outbox row committed atomically with aggregate | Neither aggregate nor outbox without the other on success; both persist on success | `MassTransitMongoOutboxSpikeTests.cs:44-50` — document insert + `publishEndpoint.Publish` inside `BeginTransaction`/`CommitTransaction`; `WorkoutFinishedEndToEndTests.cs:54-64` — `Assert.Equal(HttpStatusCode.OK)` + `Assert.True(session.IsCompleted)` + consumer log for `sessionId` | ✅ PASS |
| AC2: transaction failure rolls back aggregate AND event | Neither exists after rollback | `MassTransitMongoOutboxSpikeTests.cs:82-98` — `AbortTransaction`; `Assert.Null(document)`; `Assert.Equal(0, outboxCount)`; `WorkoutFinishedEndToEndTests.cs:85-101` — `Assert.False(session.IsCompleted)`; `Assert.Equal(0, outboxCount)`; `Assert.False(WorkoutFinishedConsumerLogCapture.ContainsSessionId(sessionId))` | ✅ PASS |
| AC3: Mongo publisher uses real session/transaction | `IClientSessionHandle` transaction covers aggregate + outbox | `IWorkoutOutboxTransaction.cs:15-21` — `StartSession`/`BeginTransaction`/`CommitTransaction`; `FinishWorkoutExecutionHandler.cs:72-87` — publish inside `outboxTransaction.ExecuteAsync`; spike + E2E rollback tests above | ✅ PASS |
| AC4: SQL publisher uses DbContext transaction | EF `SaveChangesAsync` covers aggregate + outbox | — | ✅ N/A — `spec.md:158` documents EVTB-03 N/A (no SQL publisher in scope); `design.md:95-125` documents EF pattern for future domains |

**EVTB-01**: ✅ PASS (AC1–2)  
**EVTB-02**: ✅ PASS (AC3)  
**EVTB-03**: ✅ N/A (documented, not fake-tested)

---

### P1: Relay entrega eventos pendentes ao broker de forma confiável

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: `Pending` outbox row published within configurable polling interval | Event reaches broker/consumer after outbox write | `WorkoutFinishedRestartResilienceTests.cs:32-49` — publish while bus stopped, restart, `Assert.Equal(payload, received.Payload)`; `WorkoutFinishedBrokerDownResilienceTests.cs:63-67` — recovery delivery; `WorkoutFinishedEndToEndTests.cs:56` — consumer log within 45s | ✅ PASS (delivery proven; polling interval not timed) |
| AC2: broker ack → row marked `Published`, never re-relayed | Outbox state transitions to `Published` after successful delivery | — | ❌ GAP — no assertion on outbox `Published` state or dedup of relay |
| AC3: broker unavailable → row stays `Pending`, retried later | Pending count ≥ 1 while broker down; delivery after recovery | `WorkoutFinishedBrokerDownResilienceTests.cs:58-61` — `Assert.True(pendingCount >= 1)`; `Assert.Empty(BrokerDownProbeConsumer.Received)`; post-recovery `Assert.Equal(payload, received.Payload)` | ✅ PASS |
| AC4: process restart resumes pending outbox lines | Pending entry delivered after bus restart | `WorkoutFinishedRestartResilienceTests.cs:32-49` — stop bus, commit outbox, restart, `Assert.Equal(payload, received.Payload)` | ✅ PASS |

**EVTB-04**: ⚠️ PARTIAL — AC1/3/4 covered; AC2 no evidence  
**EVTB-05**: ✅ PASS (AC3)  
**EVTB-06**: ✅ PASS (AC4)

---

### P1: Consumidor processa evento de forma idempotente

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: duplicate `EventId` already processed → no-op, ack | Consumer effect count = 1 after duplicate delivery | `WorkoutFinishedIdempotencyTests.cs:38-60` — publish same `messageId` twice; `Assert.Equal(1, IdempotencyProbeConsumer.ProcessedCount)` | ✅ PASS (probe consumer; not `WorkoutFinishedConsumer`) |
| AC2: new `EventId` → process and register before ack | First delivery processed once | `WorkoutFinishedIdempotencyTests.cs:60` — `ProcessedCount == 1`; `WorkoutFinishedEndToEndTests.cs:56` — consumer log observed | ✅ PASS |
| AC3: consumer exception → not marked processed; retry with exponential backoff to limit | Multiple retry attempts before exhaustion | `WorkoutFinishedRetryDeadLetterTests.cs:52` — `Assert.True(AlwaysFailingRetryProbeConsumer.Attempted.Count >= 3)` | ⚠️ Spec-precision gap — retries proven; backoff curve/timing not asserted; processed-state not asserted |
| AC4: retry exhausted → dead-letter, never silent discard | Message in `_error` queue after exhaustion | `WorkoutFinishedRetryDeadLetterTests.cs:49-53` — `errorCount >= 1` on `{queue}_error` | ✅ PASS |

**EVTB-07**: ✅ PASS (AC1–2)  
**EVTB-08**: ⚠️ PARTIAL — AC4 covered; AC3 backoff/processed-state imprecise

---

### P1: Broker plugável por interface

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: publish via `IPublishEndpoint` only, never concrete broker SDK in domain | Domain depends on MassTransit abstraction | `FinishWorkoutExecutionHandler.cs:17` — `IPublishEndpoint`; `FinishWorkoutExecutionHandlerTests.cs:141-146` — mock `IPublishEndpoint` | ✅ PASS |
| AC2: consume via abstract consumer interface | `IConsumer<T>` in domain, no RabbitMQ SDK | `WorkoutFinishedConsumer.cs:6` — `IConsumer<WorkoutFinished>`; no `RabbitMQ.Client` under `src/Features` | ✅ PASS |
| AC3: RabbitMQ adapter registered → publish/consume without domain change | E2E delivery via RabbitMQ | `WorkoutFinishedEndToEndTests.cs:37-64` — real RabbitMQ factory; consumer log captured | ✅ PASS |
| AC4: in-memory/fake adapter swap in DI → domain behaves same | Finish/publish/consume works with `Messaging:Transport=InMemory` without Training code change | `IntegrationWebApplicationFactory.cs:39` — `["Messaging:Transport"] = "InMemory"` (app boots); no test asserts `WorkoutFinished` publish/consume on in-memory transport | ❌ GAP — DI swap proven; domain messaging equivalence not asserted |

**EVTB-09**: ⚠️ PARTIAL — AC1–3 covered; AC4 no behavioral evidence

---

### P1: Prova ponta a ponta — `Training` publica `WorkoutFinished`

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: `FinishWorkoutExecutionHandler` success → `WorkoutFinished` with `SessionId`, `TargetUserId`, `ExecutedByUserId`, `EndedAtUtc` in same Mongo transaction | Exact field values on published event | `FinishWorkoutExecutionHandlerTests.cs:141-146` — `It.Is<WorkoutFinished>(e => e == new WorkoutFinished("session-3", 42, 17, endedAtUtc))`; `FinishWorkoutExecutionHandler.cs:82-84` — publish inside `outboxTransaction.ExecuteAsync` | ⚠️ PARTIAL — unit asserts all four fields; integration E2E asserts only `sessionId` in consumer log (`WorkoutFinishedEndToEndTests.cs:56`, `WorkoutFinishedConsumerLogCapture.cs:18-19`) |
| AC2: example consumer processes `WorkoutFinished` on arrival | Consumer side-effect (log) | `WorkoutFinishedConsumerTests.cs:24-32` — log verify with session/target/executed/ended; `WorkoutFinishedEndToEndTests.cs:56` — `WaitForConsumerLogAsync(sessionId)` | ✅ PASS |

**EVTB-10**: ⚠️ PARTIAL — E2E missing value assertions for `TargetUserId`/`ExecutedByUserId`/`EndedAtUtc`

---

### P2: Dead-letter observável

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: dead-letter → log `EventId`, reason, stack trace via observability pipeline | Structured log/trace with identifiers | `WorkoutFinishedRetryDeadLetterTests.cs:49-53` — `_error` queue count only | ❌ GAP — no log/trace assertion for `EventId`, reason, or stack |

**EVTB-11**: ❌ GAP

---

**Status**: ❌ Gaps present (4 criteria without spec-matching evidence; 3 spec-precision gaps)

---

## Discrimination Sensor

Scratch state: detached worktree at `20082b0` (`ShapeUpApi-mutant-verify`); all mutations restored via `git checkout --`.

| Mutation | File:line | Description | Killed? |
| -------- | --------- | ----------- | ------- |
| M1 | `FinishWorkoutExecutionHandler.cs:82-84` | Skip `Publish` (`Task.CompletedTask` instead) | ✅ Killed — unit `PublishesWorkoutFinishedWithExpectedPayload` + E2E `FinishWorkout_EndToEnd` |
| M2 | `IWorkoutOutboxTransaction.cs:25` | `AbortTransaction` → `CommitTransaction` on fault | ✅ Killed — `RollsBackCompletionAndOutbox` |
| M3 | `FinishWorkoutExecutionHandler.cs:83` | Swap `TargetUserId`/`ExecutedByUserId` in payload | ✅ Killed — unit payload equality |
| M4 | `WorkoutFinishedConsumer.cs:12-17` | Remove consumer log side-effect | ✅ Killed — unit `Consume_LogsWorkoutFinishedEventWithExpectedFields` + E2E |
| M5 | `MessagingRetryTestHost.cs:37` | Remove retry middleware (`Immediate(2)` commented out) | ✅ Killed — `RoutesMessageToErrorQueueAfterRetryExhaustion` |

**Sensor depth**: P0-full (≥5 manual behavior mutations — data integrity / outbox path)  
**Result**: 5/5 killed — ✅ PASS

---

## Interactive UAT Results

Skipped — backend infrastructure feature (per verifier instructions).

---

## Code Quality

| Principle | Status |
| --------- | ------ |
| Minimum code | ✅ |
| Surgical changes | ✅ |
| No scope creep | ✅ |
| Matches patterns | ✅ |
| Spec-anchored outcome check | ❌ — gaps above |
| Per-layer Coverage Expectation met | ⚠️ — integration relay `Published` state untested |
| Every test maps to a spec requirement | ✅ |
| Documented guidelines followed | `tasks.md` Test Coverage Matrix; `src/AGENTS.md` cited in tasks |

---

## Edge Cases

- [x] Duplicate relay instances / optimistic claim — delegated to MassTransit outbox (no explicit multi-relay test; acceptable for scope)
- [ ] Payload size validation at boundary — NOT tested (no evidence)
- [x] Consumer offline → message retained in queue — implied by broker-down + restart tests
- [x] Mongo unavailable → transaction fails cleanly — rollback tests cover failed commit path

---

## Gate Check

| Gate | Command | Result |
| ---- | ------- | ------ |
| Build | `dotnet build src/ShapeUp.csproj` | ✅ 0 errors (3 warnings) |
| Unit | `dotnet test tests/UnitTests/UnitTests.csproj` | ✅ **238** passed, 0 failed, 0 skipped |
| Messaging integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Messaging` (after `docker compose up -d mongo rabbitmq` in `src/`) | ✅ **8** passed, 0 failed, 0 skipped |
| Full integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj` | ❌ **218** passed, **1** failed, **7** skipped / 226 total |

**Failed test**: `TrainingEndpointsIntegrationTests.ExerciseEndpoints_ShouldCreateSuggestUpdateAndGet` — `NullReferenceException` in `MassTransitBus.StopAsync` during `WebApplicationFactory.Dispose` (InMemory transport teardown). Not in Messaging filter; indicates cross-suite MassTransit lifecycle flake.

**Skipped (7)**: Authorization middleware/user GET (2), GymManagement endpoints (5) — pre-existing skips.

**Test count delta (feature)**: Unit +2 (236→238: handler publish + consumer log). Integration Messaging +8 spike/E2E scenarios; full suite 218 pass vs author-reported 219 (1 new failure on dispose).

---

## Fix Plans

### Fix 1: Assert outbox `Published` state after successful relay (EVTB-04 AC2)

- **Root cause**: Integration tests prove delivery but never query outbox document state post-delivery.
- **Fix task**: After E2E happy path or restart probe delivery, query `outbox.messages` (or MassTransit state collection) and assert delivery state = published/delivered.
- **Priority**: Major

### Fix 2: In-memory transport domain proof (EVTB-09 AC4)

- **Root cause**: `IntegrationWebApplicationFactory` sets `Messaging:Transport=InMemory` but no test asserts `WorkoutFinished` consumer effect under that configuration.
- **Fix task**: Add integration test using in-memory transport + log capture (or harness) proving finish → consume without RabbitMQ.
- **Priority**: Major

### Fix 3: E2E `WorkoutFinished` field value assertions (EVTB-10 AC1 / payload rule)

- **Root cause**: E2E only checks `sessionId` substring in log.
- **Fix task**: Extend `WorkoutFinishedConsumerLogCapture` assertions to require `target user`, `executed by`, and `ended at` values matching the finished session.
- **Priority**: Major

### Fix 4: Dead-letter observability (EVTB-11)

- **Root cause**: Retry test stops at `_error` queue depth.
- **Fix task**: Capture `ILogger`/`Activity` output during retry exhaustion; assert `EventId`/exception message present.
- **Priority**: Minor (P2)

### Fix 5: MassTransit dispose stability in InMemory integration factory

- **Root cause**: `TrainingEndpointsIntegrationTests.DisposeAsync` NRE on bus stop.
- **Fix task**: Ensure hosted MassTransit stops cleanly in test factory teardown (or use collection fixture ordering).
- **Priority**: Major (full integration gate)

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status (Verifier) |
| ----------- | --------------- | --------------------- |
| EVTB-01 | Verified | ✅ Verified |
| EVTB-02 | Verified | ✅ Verified |
| EVTB-03 | N/A (documented) | ✅ N/A (documented) |
| EVTB-04 | Verified | ⚠️ Partial — AC2 gap |
| EVTB-05 | Verified | ✅ Verified |
| EVTB-06 | Verified | ✅ Verified |
| EVTB-07 | Verified | ✅ Verified |
| EVTB-08 | Verified | ⚠️ Partial — backoff imprecise |
| EVTB-09 | Verified | ⚠️ Partial — AC4 gap |
| EVTB-10 | Verified | ⚠️ Partial — E2E payload fields |
| EVTB-11 | Verified | ❌ Needs Fix |

---

## Notes (Lessons — no `scripts/lessons.py` in repo)

1. **ac_gap**: Assert outbox relay terminal state (`Published`/delivered), not only consumer receipt.
2. **ac_gap**: When spec requires pluggable transport, prove publish→consume on the alternate adapter, not only DI registration.
3. **ac_gap**: Integration E2E must assert all spec-mandated event payload fields on value, not a single identifier substring.
4. **ac_gap**: P2 dead-letter observability needs log/trace assertions, not queue depth alone.
5. **gate_fail**: MassTransit `WebApplicationFactory` teardown must be part of the full integration gate when messaging is registered globally.

---

## Summary

**Overall**: ❌ Not Ready

**Spec-anchored check**: 4 hard gaps + 3 spec-precision gaps across EVTB-04/08/09/10/11  
**Sensor**: 5/5 mutations killed  
**Gate**: Build ✅; Unit 238/238 ✅; Messaging 8/8 ✅; Full integration 218/226 ❌ (1 MassTransit dispose failure)

**What works**: Atomic Mongo outbox publish/rollback, RabbitMQ E2E `WorkoutFinished` pipe, retry→`_error`, broker-down/restart resilience, idempotent duplicate delivery, unit-level payload discrimination.

**Next steps**: Address ranked fix plans; re-run Verifier after fixes (bounded to 3 iterations).
