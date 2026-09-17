# workout-schedule-dashboard Validation

**Date**: 2026-09-17
**Spec**: `.specs/features/workout-schedule-dashboard/spec.md`
**Diff range**: `97f95cc..baef142` (commits: d9c2386, 3029284, 17ec69f, c5d5443, efc6250, baef142)
**Verifier**: independent sub-agent (author ≠ verifier)
**Scope**: Backend half only — WSD-01, WSD-07. Frontend ACs WSD-02..WSD-06 are handoff to ShapeUp-Web (not scored).

---

## Task Completion

| Task | Status | Notes |
| ---- | ------ | ----- |
| T1 | ✅ Done | `d9c2386` — `AssignedWeekdays` on document |
| T2 | ✅ Done | `3029284` — response + Clone + ToPlanResponse |
| T3 | ✅ Done | `17ec69f` — Create persist/dedupe/invalid |
| T4 | ✅ Done | `c5d5443` — Update set/clear/dedupe |
| T5 | ✅ Done | `efc6250` — Assign → empty weekdays |
| T6 | ✅ Done | `baef142` — dashboard dynamic N lock |

---

## Spec-Anchored Acceptance Criteria

### Backend in scope

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
| ------------------------- | -------------------- | ----------------------- | ------ |
| WSD-01 AC1: WHEN author saves workout with no weekdays THEN persist `AssignedWeekdays` as empty list | persisted + returned list empty | `CreateWorkoutPlanHandlerTests.cs:438-439` — `Assert.Empty(capturedPlan!.AssignedWeekdays)`; `Assert.Empty(result.Value!.AssignedWeekdays)` | ✅ PASS |
| WSD-01 AC2: WHEN author saves with one or more weekdays THEN persist that list (multi-day OK) | `[Monday, Thursday]` persisted + returned | `CreateWorkoutPlanHandlerTests.cs:459-460` — `Assert.Equal([DayOfWeek.Monday, DayOfWeek.Thursday], …)` | ✅ PASS |
| WSD-01 AC2 (update path): WHEN update provides weekdays THEN persist/return them | `[Tuesday, Friday]` | `UpdateWorkoutPlanHandlerTests.cs:208-209` — `Assert.Equal([DayOfWeek.Tuesday, DayOfWeek.Friday], …)` | ✅ PASS |
| WSD-01 AC3: WHEN duplicate weekdays in payload THEN dedupe before persist, never reject | success + single occurrence; no 400 | `CreateWorkoutPlanHandlerTests.cs:479-481` — `Assert.True(result.IsSuccess)`; `Assert.Equal([Monday, Thursday], …)` after `[Monday, Monday, Thursday]` | ✅ PASS |
| WSD-01 AC3 (update): same dedupe on update | `[Monday]` from `[Monday, Monday]` | `UpdateWorkoutPlanHandlerTests.cs:261-263` — `Assert.True` + `Assert.Equal([DayOfWeek.Monday], …)` | ✅ PASS |
| WSD-01 (clear / empty is valid): WHEN update sends empty THEN persist empty | empty list | `UpdateWorkoutPlanHandlerTests.cs:241-242` — `Assert.Empty(…)` | ✅ PASS (supports story Independent Test / Frontend AC5 contract on API) |
| WSD-01 (assign path): Assign template → new plan has empty weekdays | `AssignedWeekdays == []` on doc + response | `AssignWorkoutTemplateHandlerTests.cs:73-74` — `Assert.Empty(capturedPlan!.AssignedWeekdays)`; `Assert.Empty(result.Value!.AssignedWeekdays)` | ✅ PASS |
| WSD-01 (invalid enum → 400): design/error table | status 400, no persist | `CreateWorkoutPlanHandlerTests.cs:495-497` — `Assert.Equal(400, …)`; `Verify(… Times.Never)` | ✅ PASS (design; not a numbered AC) |
| WSD-07 AC3: WHEN `GET …/dashboard/me?sessionsTargetPerWeek=N` with dynamic N THEN accept and use N as today | `SessionsTargetPerWeek == N` (N≠5); completion rate from N | `TrainingDashboardHandlerTests.cs:99-101` — `Assert.Equal(2, …SessionsTargetPerWeek)`; `Assert.Equal(50m, …SessionsCompletionRate)` with 1 completed / N=2 | ✅ PASS |
| WSD-07 (reject N≤0 still): existing contract | validation failure | `TrainingDashboardHandlerTests.cs:20-21` — `Assert.True(result.IsFailure)`; `Assert.Equal("validation_error", …)` | ✅ PASS |

**Status**: ✅ All in-scope Backend ACs covered (no spec-precision gaps on WSD-01 / WSD-07)

### Frontend handoff (OUT OF SCOPE — do not fail)

| ID | Criterion summary | Status |
| --- | ----------------- | ------ |
| WSD-02 | PlanEditor multi-select | Handoff → ShapeUp-Web |
| WSD-03 | Card "hoje" only when weekday matches | Handoff → ShapeUp-Web |
| WSD-04 | No fetch when no today plan | Handoff → ShapeUp-Web |
| WSD-05 | Denominator = distinct assigned weekdays | Handoff → ShapeUp-Web |
| WSD-06 | Fallback = plan count; empty → no N=0 call | Handoff → ShapeUp-Web |

---

## Discrimination Sensor

| Mutation | File:line | Description | Killed? |
| -------- | --------- | ----------- | ------- |
| 1 | `CreateWorkoutPlanHandler.cs:94` | Removed `.Distinct()` — duplicates persisted | ✅ Killed by `HandleAsync_WhenAssignedWeekdaysHasDuplicates_DedupesBeforePersist` (Expected `[Monday, Thursday]`, Actual `[Monday, Monday, Thursday]`) |
| 2 | `UpdateWorkoutPlanHandler.cs:83` | Skip assign when command weekdays empty (cannot clear) | ✅ Killed by `HandleAsync_WhenAssignedWeekdaysCleared_PersistsEmpty` (`Assert.Empty` saw `[Monday, Wednesday]`) |
| 3 | `GetTrainingDashboardHandler.cs:46` | Hardcoded `SessionsTargetPerWeek` to `5` | ✅ Killed by `GetTrainingDashboardHandler_WhenDynamicTargetPerWeek_…` (Expected `2`, Actual `5`) |

**Sensor depth**: lightweight (3 behavior-level mutations)
**Result**: 3/3 killed — PASS ✅
**Working tree**: all mutations reverted via `git checkout`; pre-existing `.specs` / `.vscode` changes restored via stash; `src/` and `tests/` clean vs HEAD.

---

## Interactive UAT Results

Not performed — backend-only / infrastructure; automated checks sufficient per validate.md.

---

## Code Quality

| Principle | Status |
| --------- | ------ |
| Minimum code | ✅ Additive field + Distinct mapping; WSD-07 test-only |
| Surgical changes | ✅ 14 files in diff; no unrelated churn |
| No scope creep | ✅ Templates not given weekdays; dashboard handler unchanged in prod |
| Matches patterns | ✅ FluentValidation `IsInEnum`, Result pattern, Moq handler tests |
| Spec-anchored outcome check | ✅ Assertions target empty / multi-day / dedupe / N=2 |
| Per-layer Coverage Expectation | ✅ Domain handlers 1:1 to WSD-01 ACs; WSD-07 locked |
| Every test maps to a spec/design Done-when | ✅ New tests tagged WSD-01 / WSD-07 |
| Documented guidelines followed | ✅ `src/AGENTS.md` (FluentValidation, Result, testability) |

**Observation (non-blocking)**: no dedicated unit test that `Clone` copies `AssignedWeekdays` by value (design risk); covered indirectly via mapping presence in T2 + Create/Update round-trip. Not an AC gap.

---

## Edge Cases

- [x] Assign path yields empty `AssignedWeekdays` (backend) — tested
- [x] Clear weekdays on update (API supports Frontend AC5) — tested
- [ ] Deleted plan / same-day multi-plan / local TZ / readAllPages — **Frontend** (handoff)
- [x] Invalid DayOfWeek → 400 — tested (design error table)

---

## Gate Check

- **Gate command**: `dotnet test tests/UnitTests/UnitTests.csproj` (Quick gate from tasks.md)
- **Result**: 393 passed, 0 failed, 0 skipped
- **Test count before feature** (`97f95cc`): ~384 executed (269 `[Fact]` markers; Theories inflate total — delta via new Facts)
- **Test count after feature** (`baef142` / current): 393 passed
- **Delta**: +9 new `[Fact]` tests (Create×4, Update×3, Assign×1, Dashboard×1)
- **Skipped tests**: none
- **Failures**: none
- **Test integrity**: count increased; no deletions observed in feature diff

---

## Fix Plans

None — PASS.

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status |
| ----------- | --------------- | ---------- |
| WSD-01 | In Design / Implementing | ✅ Verified (backend) |
| WSD-02 | In Design | Handoff — Frontend |
| WSD-03 | In Design | Handoff — Frontend |
| WSD-04 | In Design | Handoff — Frontend |
| WSD-05 | In Design | Handoff — Frontend |
| WSD-06 | In Design | Handoff — Frontend |
| WSD-07 | In Design / Implementing | ✅ Verified (backend) |

---

## Summary

**Overall**: ✅ Ready (backend half)

**Spec-anchored check**: 2/2 backend requirements (WSD-01 ACs + WSD-07) matched spec outcomes | 0 spec-precision gaps
**Sensor**: 3/3 mutations killed
**Gate**: 393 passed

**What works**: Optional `AssignedWeekdays` persist/dedupe on Create/Update; Assign defaults empty; dashboard still echoes dynamic `sessionsTargetPerWeek`.

**Issues found**: none in backend scope

**Next steps**: Handoff WSD-02..WSD-06 to ShapeUp-Web; update `spec.md` requirement statuses to Verified for WSD-01/WSD-07 when orchestrator promotes.
