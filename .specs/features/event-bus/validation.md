# Event Bus Validation

**Date**: 2026-09-09
**Spec**: `.specs/features/event-bus/spec.md`
**Diff range**: `121d3f0..8d24796`
**Verifier**: independent sub-agent (author ≠ verifier, iteration 2/3)
**Fix commits verified**: `cb49bbf`, `c8067c7`, `00783aa`, `bd1a531`, `8d24796` (after `c0e0cbd`)

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

## Re-check: Previous Ranked Gaps

| # | Gap (iteration 1) | Re-check result | Evidence |
| - | ----------------- | --------------- | -------- |
| 1 | EVTB-04 AC2 — outbox cleared/Published after ack | ✅ **CLOSED** | `OutboxRelayAssertions.cs:26-29` — `Assert.True(pendingCount == 0)` for WorkoutFinished in `outbox.messages`; `WorkoutFinishedEndToEndTests.cs:65-67` — `AssertWorkoutFinishedOutboxRelayedAsync`; `WorkoutFinishedRestartResilienceTests.cs:51-53` — `AssertOutboxMessageRelayedAsync` |
| 2 | EVTB-09 AC4 — in-memory transport publish/consume | ✅ **CLOSED** | `WorkoutFinishedInMemoryTransportTests.cs:29-54` — `FinishWorkout_WithInMemoryTransport_PublishesAndConsumesWorkoutFinished`; `Assert.Equal(HttpStatusCode.OK)`; `ContainsExpectedPayload(sessionId, owner.UserId, owner.UserId, endedAtUtc)` |
| 3 | EVTB-10 AC1 — E2E all four WorkoutFinished fields | ✅ **CLOSED** | `WorkoutFinishedEndToEndTests.cs:59-63` — `ContainsExpectedPayload` (SessionId via capture key + TargetUserId + ExecutedByUserId + EndedAtUtc); `WorkoutFinishedConsumerLogCapture.cs:34-45` — field equality + `TotalSeconds < 1` on `EndedAtUtc` |
| 4 | EVTB-11 — MessagingReceiveFaultLogger + retry log | ✅ **CLOSED** | `MessagingReceiveFaultLogger.cs:18-23` — `LogError` with `MessageId`, `ConsumerType`, `Reason={exception.Message}`; `WorkoutFinishedRetryDeadLetterTests.cs:56-60` — `MessagingFaultLogCapture.ContainsDeadLetterEvidence(transportMessageId, "Intentional consumer failure...")` |
| 5 | Full integration 0 failed (7 skip OK) | ❌ **OPEN** | Run 1: **218** passed, **2** failed, **7** skipped / 227; Run 2: **217** passed, **3** failed, **7** skipped / 227. All failures: `NullReferenceException` at `BusDepotAgentSupervisor.PreStop` during `WebApplicationFactory.Dispose()` teardown (not assertion failures). Isolated re-run of failed tests: **6/6 passed**. |

---

## Spec-Anchored Acceptance Criteria

### P1: Publisher grava evento atomicamente com sua escrita de domínio

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: handler + `Publish` in same transaction → outbox row committed atomically with aggregate | Neither aggregate nor outbox without the other on success; both persist on success | `MassTransitMongoOutboxSpikeTests.cs:44-50`; `WorkoutFinishedEndToEndTests.cs:55-75` — `Assert.Equal(HttpStatusCode.OK)`; `Assert.True(session.IsCompleted)`; `ContainsExpectedPayload`; `AssertWorkoutFinishedOutboxRelayedAsync` | ✅ PASS |
| AC2: transaction failure rolls back aggregate AND event | Neither exists after rollback | `MassTransitMongoOutboxSpikeTests.cs:82-98`; `WorkoutFinishedEndToEndTests.cs:96-113` — `Assert.False(session.IsCompleted)`; `Assert.Equal(0, outboxCount)`; `Assert.False(ContainsSessionId)` | ✅ PASS |
| AC3: Mongo publisher uses real session/transaction | `IClientSessionHandle` transaction covers aggregate + outbox | `IWorkoutOutboxTransaction.cs:15-21`; `FinishWorkoutExecutionHandler.cs:72-87` | ✅ PASS |
| AC4: SQL publisher uses DbContext transaction | EF `SaveChangesAsync` covers aggregate + outbox | — | ✅ N/A — `spec.md:158` |

**EVTB-01**: ✅ PASS  
**EVTB-02**: ✅ PASS  
**EVTB-03**: ✅ N/A (documented)

---

### P1: Relay entrega eventos pendentes ao broker de forma confiável

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: `Pending` outbox row published within configurable polling interval | Event reaches broker/consumer after outbox write | `WorkoutFinishedRestartResilienceTests.cs:48-49`; `WorkoutFinishedBrokerDownResilienceTests.cs:63-67`; `WorkoutFinishedEndToEndTests.cs:57` | ✅ PASS (delivery proven; polling interval not timed) |
| AC2: broker ack → row marked `Published`, never re-relayed | Outbox terminal state after successful delivery | `OutboxRelayAssertions.cs:26-29` — `Assert.True(pendingCount == 0)` WorkoutFinished in `outbox.messages`; `WorkoutFinishedEndToEndTests.cs:65-67`; `WorkoutFinishedRestartResilienceTests.cs:51-53` — `Assert.Equal(0, remaining)` all outbox messages | ✅ PASS — MassTransit removes delivered rows from `outbox.messages` (equivalent to Published/no-relay) |
| AC3: broker unavailable → row stays `Pending`, retried later | Pending count ≥ 1 while broker down; delivery after recovery | `WorkoutFinishedBrokerDownResilienceTests.cs:58-61` | ✅ PASS |
| AC4: process restart resumes pending outbox lines | Pending entry delivered after bus restart | `WorkoutFinishedRestartResilienceTests.cs:32-53` | ✅ PASS |

**EVTB-04**: ✅ PASS (AC2 closed by `cb49bbf`)  
**EVTB-05**: ✅ PASS  
**EVTB-06**: ✅ PASS

---

### P1: Consumidor processa evento de forma idempotente

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: duplicate `EventId` already processed → no-op, ack | Consumer effect count = 1 after duplicate delivery | `WorkoutFinishedIdempotencyTests.cs:38-60` — `Assert.Equal(1, ProcessedCount)` | ✅ PASS |
| AC2: new `EventId` → process and register before ack | First delivery processed once | `WorkoutFinishedIdempotencyTests.cs:60`; `WorkoutFinishedEndToEndTests.cs:57-63` | ✅ PASS |
| AC3: consumer exception → not marked processed; retry with exponential backoff to limit | Multiple retry attempts before exhaustion | `WorkoutFinishedRetryDeadLetterTests.cs:53` — `Assert.True(Attempted.Count >= 3)` | ⚠️ Spec-precision gap — retries proven; backoff curve/timing and processed-state not asserted |
| AC4: retry exhausted → dead-letter, never silent discard | Message in `_error` queue after exhaustion | `WorkoutFinishedRetryDeadLetterTests.cs:49-54` — `errorCount >= 1` | ✅ PASS |

**EVTB-07**: ✅ PASS  
**EVTB-08**: ⚠️ PARTIAL — AC4 covered; AC3 backoff/processed-state imprecise (per instructions: do not FAIL solely for this)

---

### P1: Broker plugável por interface

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: publish via `IPublishEndpoint` only | Domain depends on MassTransit abstraction | `FinishWorkoutExecutionHandler.cs:17`; `FinishWorkoutExecutionHandlerTests.cs:141-146` | ✅ PASS |
| AC2: consume via abstract consumer interface | `IConsumer<T>` in domain | `WorkoutFinishedConsumer.cs:6` | ✅ PASS |
| AC3: RabbitMQ adapter registered → publish/consume without domain change | E2E delivery via RabbitMQ | `WorkoutFinishedEndToEndTests.cs:37-63` | ✅ PASS |
| AC4: in-memory/fake adapter swap in DI → domain behaves same | Finish/publish/consume on `Messaging:Transport=InMemory` | `IntegrationWebApplicationFactory.cs:43`; `WorkoutFinishedInMemoryTransportTests.cs:29-54` — full finish→consume with `ContainsExpectedPayload` | ✅ PASS (closed by `00783aa`) |

**EVTB-09**: ✅ PASS

---

### P1: Prova ponta a ponta — `Training` publica `WorkoutFinished`

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: success → `WorkoutFinished` with `SessionId`, `TargetUserId`, `ExecutedByUserId`, `EndedAtUtc` in same Mongo transaction | Exact field values on published event | `FinishWorkoutExecutionHandlerTests.cs:141-146` — `It.Is<WorkoutFinished>(e => e == new WorkoutFinished("session-3", 42, 17, endedAtUtc))`; `WorkoutFinishedEndToEndTests.cs:59-63` — `ContainsExpectedPayload`; `WorkoutFinishedInMemoryTransportTests.cs:50-54` — same | ✅ PASS (closed by `c8067c7`) |
| AC2: example consumer processes `WorkoutFinished` on arrival | Consumer side-effect (log) | `WorkoutFinishedConsumerTests.cs:24-32`; `WorkoutFinishedEndToEndTests.cs:57-63` | ✅ PASS |

**EVTB-10**: ✅ PASS

---

### P2: Dead-letter observável

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| AC1: dead-letter → log `EventId`, reason, stack trace/erro via observability pipeline | Structured log with identifiers | `MessagingReceiveFaultLogger.cs:18-23` — `MessageId`, `Reason`; `WorkoutFinishedRetryDeadLetterTests.cs:58-60` — `ContainsDeadLetterEvidence(transportMessageId, reasonFragment)` + fault type | ✅ PASS — ⚠️ stack trace not explicitly asserted in capture (`MessagingFaultLogCapture.cs:47-49` records type+message only) |

**EVTB-11**: ✅ PASS (closed by `bd1a531`; minor spec-precision on stack trace)

---

**Status**: ❌ Gate failure remains (MassTransit dispose flake under full suite)

---

## Discrimination Sensor

Scratch state: detached worktree `ShapeUpApi-mutant-v2` at `8d24796`; all mutations restored via `git checkout --`.

| Mutation | File:line | Description | Killed? |
| -------- | --------- | ----------- | ------- |
| M1 | `FinishWorkoutExecutionHandler.cs:82-84` | Skip `Publish` (`Task.CompletedTask`) | ✅ Killed — `HandleAsync_WhenCompletionSucceeds_PublishesWorkoutFinishedWithExpectedPayload` |
| M2 | `IWorkoutOutboxTransaction.cs:25` | `AbortTransaction` → `CommitTransaction` on fault | ✅ Killed — `FinishWorkout_WhenFaultInjectedBeforeCommit_RollsBackCompletionAndOutbox` |
| M3 | `FinishWorkoutExecutionHandler.cs:83` | Swap `TargetUserId`/`ExecutedByUserId` in payload | ✅ Killed — unit payload equality |

**Sensor depth**: P0-targeted (3 behavior mutations per iteration-2 instructions)  
**Result**: 3/3 killed — ✅ PASS

---

## Interactive UAT Results

Skipped — backend infrastructure feature.

---

## Code Quality

| Principle | Status |
| --------- | ------ |
| Minimum code | ✅ |
| Surgical changes | ✅ |
| No scope creep | ✅ |
| Matches patterns | ✅ |
| Spec-anchored outcome check | ✅ — 4 prior gaps closed; 2 spec-precision gaps (EVTB-08 AC3, EVTB-11 stack trace) |
| Per-layer Coverage Expectation met | ✅ |
| Every test maps to a spec requirement | ✅ |
| Documented guidelines followed | `tasks.md` Test Coverage Matrix; `src/AGENTS.md` |

---

## Edge Cases

- [x] Duplicate relay instances — delegated to MassTransit outbox
- [ ] Payload size validation at boundary — NOT tested
- [x] Consumer offline → message retained in queue
- [x] Mongo unavailable → transaction fails cleanly

---

## Gate Check

| Gate | Command | Result |
| ---- | ------- | ------ |
| Build | `dotnet build src/ShapeUp.csproj` | ✅ 0 errors (3 warnings) |
| Unit | `dotnet test tests/UnitTests/UnitTests.csproj` | ✅ **238** passed, 0 failed, 0 skipped |
| Messaging integration | `dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter FullyQualifiedName~Messaging` | ✅ **9** passed, 0 failed, 0 skipped |
| Full integration (run 1) | `dotnet test tests/IntegrationTests/IntegrationTests.csproj` | ❌ **218** passed, **2** failed, **7** skipped / 227 |
| Full integration (run 2) | same | ❌ **217** passed, **3** failed, **7** skipped / 227 |

**Failed tests (varies by run — teardown only, tests pass in isolation)**:

- `PlatformAdminCapabilityIntegrationTests.GetAll_NonAdminUser_StillAllowed` — NRE `BusDepotAgentSupervisor.PreStop` in `Dispose()` (`PlatformAdminCapabilityIntegrationTests.cs:30`)
- `TrainingEndpointsIntegrationTests.EquipmentEndpoints_*` — same NRE in `Dispose()` (`TrainingEndpointsIntegrationTests.cs:24`)
- `WorkoutsEndpointsIntegrationTests.GetById_NonOwnerAccessing_ReturnsForbidden` — same (`WorkoutsEndpointsIntegrationTests.cs:24`)
- `WorkoutPlansEndpointsIntegrationTests.Create_SupersetWithTwoExercises_*` — same (`WorkoutPlansEndpointsIntegrationTests.cs:38`)

**Root cause (iteration 2)**: `8d24796` swallows NRE only in `IntegrationWebApplicationFactory.DisposeAsync()` (`IntegrationWebApplicationFactory.cs:91-100`); ~10 test classes call sync `_factory.Dispose()` which bypasses the override and still surfaces the MassTransit InMemory+outbox `PreStop` NRE under full-suite load.

**Skipped (7)**: Authorization middleware/user GET (2), GymManagement endpoints (5) — pre-existing.

**Test count delta (feature)**: Unit 238 (+2). Integration Messaging 9 (+1 in-memory). Full suite 227 total (+1 vs iteration 1).

---

## Fix Plans

### Fix 1: Complete MassTransit dispose stability (gate blocker)

- **Root cause**: NRE swallow only on `DisposeAsync()` path; sync `Dispose()` used by multiple SQL Server Write Operations collection tests.
- **Fix task**: Override `Dispose(bool)` or migrate all `IntegrationWebApplicationFactory` consumers to `await DisposeAsync()`; alternatively centralize factory teardown in a collection fixture.
- **Priority**: Blocker (full integration gate)

---

## Requirement Traceability Update

| Requirement | Iteration 1 | Iteration 2 (Verifier) |
| ----------- | ----------- | ---------------------- |
| EVTB-01 | ✅ Verified | ✅ Verified |
| EVTB-02 | ✅ Verified | ✅ Verified |
| EVTB-03 | ✅ N/A | ✅ N/A |
| EVTB-04 | ⚠️ AC2 gap | ✅ Verified |
| EVTB-05 | ✅ Verified | ✅ Verified |
| EVTB-06 | ✅ Verified | ✅ Verified |
| EVTB-07 | ✅ Verified | ✅ Verified |
| EVTB-08 | ⚠️ Partial | ⚠️ Partial (backoff imprecise) |
| EVTB-09 | ⚠️ AC4 gap | ✅ Verified |
| EVTB-10 | ⚠️ E2E fields | ✅ Verified |
| EVTB-11 | ❌ Gap | ✅ Verified |

---

## Notes (Lessons — iteration 2 signal)

6. **gate_fail**: `DisposeAsync` NRE swallow does not protect tests calling sync `WebApplicationFactory.Dispose()` — override both teardown paths or standardize on `DisposeAsync` across integration fixtures.

---

## Summary

**Overall**: ❌ Not Ready (gate)

**Spec-anchored check**: 4/4 prior hard gaps closed; 2 spec-precision gaps remain (EVTB-08 AC3 backoff, EVTB-11 stack trace) — neither blocks per instructions  
**Sensor**: 3/3 mutations killed  
**Gate**: Build ✅; Unit 238/238 ✅; Messaging 9/9 ✅; Full integration ❌ (2–3 flaky dispose failures / 227)

**What works**: All iteration-1 AC evidence gaps addressed — outbox relay clearance, in-memory transport E2E, full WorkoutFinished payload assertions, dead-letter fault logging.

**Next steps**: Fix sync `Dispose()` teardown path (iteration 3/3); re-run Verifier.
