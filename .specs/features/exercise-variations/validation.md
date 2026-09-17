# exercise-variations Validation

**Date**: 2026-09-17
**Spec**: `.specs/features/exercise-variations/spec.md`
**Diff range**: `50c3306..1b82672` (HEAD)
**Verifier**: independent sub-agent (author ≠ verifier) — **re-check after fix iteration 1**
**Scope**: Backend half only — EXVAR-01/02/03/09 (API), EXVAR-06/07 (swap). EXVAR-04/05/08 and UI halves out of scope (handoff Web).
**Prior verdict**: FAIL ❌ at `50c3306..eb96d30` (EXVAR-02 no evidence; OR-side mutant survived)
**Fix under review**: `1b82672` — `ExerciseEquivalentRepositoryTests.cs` (InMemory EF)

---

## Task Completion

| Task | Status | Notes |
| ---- | ------ | ----- |
| T1 ExerciseEquivalent entity + EF | ✅ Done | `02c2657` |
| T2 Migration | ✅ Done | `2bb9844` |
| T3 Repository + DI | ✅ Done | `904def4` + discriminating tests in `1b82672` |
| T4 SetExerciseEquivalent + tests | ✅ Done | `2d1b1a8` |
| T5 RemoveExerciseEquivalent + tests | ✅ Done | `200710a` |
| T6 GetExerciseEquivalents + tests | ✅ Done | `f506bf2` |
| T7 HTTP equivalents endpoints | ✅ Done | `9cec2b4` |
| T8 SwapExerciseInSession + tests | ✅ Done | `45f8df6` |
| T9 swap-exercise HTTP endpoint | ✅ Done | `eb96d30` |
| Fix: EXVAR-02 repo symmetry tests | ✅ Done | `1b82672` |

---

## Spec-Anchored Acceptance Criteria

In-scope backend ACs only. UI/picker/offline-queue ACs omitted (not scored).

### EXVAR-01 — Persist equivalents (API)

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
| ------------------------- | -------------------- | ----------------------- | ------ |
| WHEN valid pair selected THEN persist relation on catalog | `SetEquivalentAsync` invoked; success | `SetExerciseEquivalentHandlerTests.cs:48-50` — `Assert.True(result.IsSuccess)` + `Verify(...SetEquivalentAsync(1, 2...), Times.Once)` | ✅ PASS |
| WHEN self-equivalent THEN reject | HTTP/domain 400; no persist | `SetExerciseEquivalentHandlerTests.cs:17-21` — `Assert.Equal(400, ...)` + `Verify(...Never)` | ✅ PASS |
| WHEN exercise missing THEN not found | 404 | `SetExerciseEquivalentHandlerTests.cs:37-38` — `Assert.Equal(404, ...)` | ✅ PASS |
| WHEN GET equivalents THEN mapped list (name/id) | `ExerciseResponse[]` with peer id/name | `GetExerciseEquivalentsHandlerTests.cs:72-75` — `Assert.Single` / `Assert.Equal(2, ...Id)` / `Assert.Equal("Push Up", ...Name)` | ✅ PASS |
| WHEN no equivalents THEN empty list | empty array success | `GetExerciseEquivalentsHandlerTests.cs:41-42` — `Assert.Empty(result.Value!)` | ✅ PASS |

### EXVAR-02 — Symmetry

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
| ------------------------- | -------------------- | ----------------------- | ------ |
| WHEN A↔B persisted THEN A lists B AND B lists A | Bidirectional read from one canonical row | `ExerciseEquivalentRepositoryTests.cs:19-25` — after `SetEquivalentAsync(1,2)`: `Assert.Equal(2, fromOne[0].Id)` **and** `Assert.Equal(1, fromTwo[0].Id)` | ✅ PASS |
| WHEN pair written with reversed ids THEN still one canonical row + both sides readable | `ExerciseId < EquivalentExerciseId`; Get either side returns peer | `ExerciseEquivalentRepositoryTests.cs:37-44` — `Assert.Equal(3, row.ExerciseId)` / `Assert.Equal(5, row.EquivalentExerciseId)` + both Get asserts | ✅ PASS |

### EXVAR-03 — Non-blocking muscle-group warning

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
| ------------------------- | -------------------- | ----------------------- | ------ |
| WHEN no shared muscle group THEN warning true, still success | `MuscleGroupOverlapWarning == true`, IsSuccess | `SetExerciseEquivalentHandlerTests.cs:60-61` — `Assert.True(result.IsSuccess)` + `Assert.True(...MuscleGroupOverlapWarning)` | ✅ PASS |
| WHEN shared muscle THEN warning false | `MuscleGroupOverlapWarning == false` | `SetExerciseEquivalentHandlerTests.cs:48-49` — `Assert.False(...MuscleGroupOverlapWarning)` | ✅ PASS |

### EXVAR-09 — Remove equivalence

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
| ------------------------- | -------------------- | ----------------------- | ------ |
| WHEN remove THEN relation removed both senses | no peers from either Get; table empty | `ExerciseEquivalentRepositoryTests.cs:70-72` — `Assert.Empty(Get(1))` + `Assert.Empty(Get(2))` + `Assert.Empty(db.ExerciseEquivalents)` | ✅ PASS |
| WHEN remove when absent THEN idempotent success | two calls both success | `RemoveExerciseEquivalentHandlerTests.cs:41-43` — `Assert.True(first/second.IsSuccess)` + `Times.Exactly(2)` | ✅ PASS |

### EXVAR-06 — Swap in active session (API; UI button out of scope)

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
| ------------------------- | -------------------- | ----------------------- | ------ |
| WHEN swap to registered equivalent THEN append new exercise in session | session exercises count 2; new id present | `SwapExerciseInSessionHandlerTests.cs:54-58` — `Assert.Equal(2, persisted!.Count)` + `Assert.Equal(2, persisted[1].ExerciseId)` | ✅ PASS |
| WHEN new exercise already in session THEN reject | 400; no update | `SwapExerciseInSessionHandlerTests.cs:118-119` — `Assert.Equal(400, ...)` | ✅ PASS |
| WHEN not equivalent THEN reject | 400; never UpdateState | `SwapExerciseInSessionHandlerTests.cs:86-90` — `Assert.Equal(400, ...)` + `Verify(...Never)` | ✅ PASS |
| WHEN session completed THEN reject | conflict 409 | `SwapExerciseInSessionHandlerTests.cs:142-143` — `Assert.Equal(409, ...)` | ✅ PASS |
| WHEN swap THEN session-only (plan untouched) | only `UpdateStateAsync` on session exercises | `SwapExerciseInSessionHandler.cs:82` + test callback on `UpdateStateAsync` only | ✅ PASS |

### EXVAR-07 — Retain completed sets on swap

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
| ------------------------- | -------------------- | ----------------------- | ------ |
| WHEN swap with retained sets THEN original keeps those sets; substitute starts empty | original Sets.Count == retained; new Sets empty; RequireRpe false | `SwapExerciseInSessionHandlerTests.cs:55-59` — `Assert.Equal(2, persisted[0].Sets.Count)` + `Assert.Empty(persisted[1].Sets)` + `Assert.False(...RequireRpe)` | ✅ PASS |

**Out of scope (not scored)**: EXVAR-04, EXVAR-05, EXVAR-08; Story UI ACs (picker, drawer, offline enqueueMutation).

**Status**: ✅ All in-scope ACs covered

---

## Discrimination Sensor

Focused re-check of prior surviving mutant + Canonicalize (EXVAR-02). Scratch mutate → filter repo tests → restore via `git checkout`. Production tree clean after.

| Mutation | File:line | Description | Killed? |
| -------- | --------- | ----------- | ------- |
| 1 | `ExerciseEquivalentRepository.cs:14` | Broke bidirectional read (`ExerciseId == id` only; dropped `\|\| EquivalentExerciseId`) | ✅ Killed — `SetThenGet_FromEitherSide_ReturnsPeer_EXVAR02` + `Set_WhenOrderReversed_...` failed (empty Get on other side) |
| 2 | `ExerciseEquivalentRepository.cs:60` | Broke `Canonicalize` to always `(a, b)` (no sort) | ✅ Killed — canonicalize row assert, `Set_WhenDuplicate_IsIdempotentSingleRow` (2 rows), remove both-sides empty |

Prior sensor mutants (muscle warning flip, swap clears retained sets) remain killed from iteration 0; not re-run.

**Sensor depth**: lightweight (2 EXVAR-02-targeted mutations this iteration)
**Result**: 2/2 killed — PASS ✅
**Mutations discarded**: file restored via `git checkout`; confirmed no `git diff` on repository source.

---

## Interactive UAT Results

Skipped — backend-only validation; no user-facing surface in this repo half.

---

## Code Quality

| Principle | Status |
| --------- | ------ |
| Minimum code | ✅ |
| Surgical changes | ✅ |
| No scope creep | ✅ |
| Matches patterns | ✅ InMemory EF repo tests match existing unit-test project layout |
| Spec-anchored outcome check | ✅ EXVAR-02 both-sides asserts present |
| Per-layer Coverage Expectation met | ✅ repo layer owns symmetry/canonicalize/idempotent set; handlers still mock for orchestration |
| Every test maps to a spec requirement | ✅ |
| Documented guidelines followed: `src/AGENTS.md` | ✅ |

---

## Edge Cases (backend-relevant)

- [x] Idempotent duplicate Set (mark pair 2x / reversed order): `ExerciseEquivalentRepositoryTests.cs:48-57` — `Assert.Single(db.ExerciseEquivalents)`
- [x] Remove idempotent: covered by Remove handler tests
- [x] Remove clears both senses: `ExerciseEquivalentRepositoryTests.cs:70-72`
- [x] Capability on POST/DELETE: `ExercisesController.cs` `[Authorize(Policy = "capability:platform.exercises.manage")]` — wiring only
- [ ] Deleted exercise omitted from GET list: join filters in repo; **still no dedicated test** (non-blocker residual; not EXVAR-02)
- [x] Swap rejects non-equivalent / duplicate-in-session / completed session: covered

---

## Gate Check

- **Gate command**: `dotnet test tests/UnitTests/UnitTests.csproj`
- **Result**: 410 passed, 0 failed, 0 skipped
- **Test count before feature**: ~393
- **Test count after prior FAIL**: 406
- **Test count after fix**: 410
- **Delta**: +4 (`ExerciseEquivalentRepositoryTests`)
- **Skipped tests**: none
- **Failures**: none

---

## Fix Plans

None for in-scope EXVAR-02 blocker. Optional residual: add assertion that soft/hard-deleted peer is omitted from Get (edge case).

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status |
| ----------- | --------------- | ---------- |
| EXVAR-01 | ✅ Verified (API) | ✅ Verified |
| EXVAR-02 | ❌ Needs Fix | ✅ Verified (repo both-sides + sensor kill) |
| EXVAR-03 | ✅ Verified | ✅ Verified |
| EXVAR-04 | ⏭️ Out of scope (Web) | ⏭️ Out of scope (Web) |
| EXVAR-05 | ⏭️ Out of scope (Web) | ⏭️ Out of scope (Web) |
| EXVAR-06 | ✅ Verified (swap API) | ✅ Verified |
| EXVAR-07 | ✅ Verified | ✅ Verified |
| EXVAR-08 | ⏭️ Out of scope (Web) | ⏭️ Out of scope (Web) |
| EXVAR-09 | ✅ Verified | ✅ Verified (remove both-sides now asserted at repo) |

---

## Summary

**Overall**: ✅ Ready (backend half)

**Spec-anchored check**: 6/6 in-scope requirement groups matched; 0 EXVAR-02 gaps
**Sensor**: 2/2 EXVAR-02 mutations killed (prior OR-side survivor now dead)
**Gate**: 410 passed

**What works**: Prior handler/swap coverage plus InMemory repo tests proving Set→Get both sides, canonicalize, duplicate-set idempotency, remove clears both sides.

**Issues found**: none blocking. Residual optional: deleted-peer omission test.

**Next steps**: none for EXVAR-02; Web half owns EXVAR-04/05/08.
