# Workout Execution Validation — Validation

**Date**: 2026-09-16
**Spec**: `.specs/features/workout-execution-validation/spec.md`
**Diff range**: `ShapeUpApi` `24cfd1a..9a3ced8` (13 commits, T1–T13)
**Verifier**: independent sub-agent (author ≠ verifier)
**Scope**: Backend only. Covers **[Backend]**-tagged ACs for WEV-02, WEV-05, WEV-06, WEV-07, WEV-08. WEV-01/03/04 and all **[Frontend]** halves are out of scope (handoff to `ShapeUp-Web`, not reviewed here).

---

## Task Completion

| Task | Status | Notes |
| ---- | ------ | ----- |
| T1 `f62394d` | ✅ Done | `RequireRpe` on `BlockExerciseDocumentValueObject` — confirmed in tree |
| T2 `e070abb` | ✅ Done | `RequireRpe` on `ExecutedExerciseDocumentValueObject` — confirmed in tree |
| T3 `4fb0059` | ✅ Done | `RequireRpe` positional param on `WorkoutExerciseDto` — confirmed in tree |
| T4 `490f1aa` | ✅ Done | Create WorkoutPlan persists/returns/clones `RequireRpe` |
| T5 `e42c896` | ✅ Done | Update WorkoutPlan persists/returns `RequireRpe`, flips both directions |
| T6 `a525906` | ✅ Done | Create WorkoutTemplate persists/returns/copies `RequireRpe` |
| T7 `50a1707` | ✅ Done | Update WorkoutTemplate persists/returns `RequireRpe`, flips both directions |
| T8 `577e617` | ✅ Done | `StartWorkoutExecutionHandler` flattens `RequireRpe` into session snapshot |
| T9 `ac78bec` | ✅ Done | `WorkoutExerciseDtoValidator` extracted, RPE optional by default |
| T10 `5314f69` | ✅ Done | `UpdateWorkoutExecutionStateCommandValidator` delegates, no unconditional `Intensity.NotNull()` |
| T11 `91eae7f` | ✅ Done | `TrainingErrors.RpeRequiredForExercise` added |
| T12 `01f52bf` | ✅ Done | RequireRpe gate + null-safe Intensity mapping on Update handler |
| T13 `9a3ced8` | ✅ Done | RequireRpe gate + null-safe mapping + validator wiring on Finish |

Commit hashes in `tasks.md`'s Progress Log verified against `git log --oneline 24cfd1a..9a3ced8` — all 13 match exactly, in the stated order.

---

## Spec-Anchored Acceptance Criteria

### WEV-02: Reforço server-side do gate de conclusão (P1)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | --------------------- | ----------------------- | ------ |
| AC1: `Repetitions` null/`<=0` ou `Load` negativo → 400 | Rejects with 400 | `WorkoutExerciseDtoValidatorTests.cs:28-35` — `Assert.False(result.IsValid)` (Repetitions null); `:37-47` — `[InlineData(0)][InlineData(-1)]` `Assert.False(result.IsValid)`; `:49-57` — `Assert.False(result.IsValid)` (Load=-1); mirrored at `UpdateWorkoutExecutionStateCommandValidatorTests.cs:28-36,38-46`; **NEW for Finish** at `FinishWorkoutExecutionHandlerTests.cs:275-295` — `[InlineData(null,30)][InlineData(10,-5)]` `Assert.Equal(400, result.Error!.StatusCode)` + `sessionRepository.Verify(x => x.GetByIdAsync(...), Times.Never)` | ✅ PASS |
| AC2: `Load==0` & `Repetitions>=1` → aceita | Valid, accepted | `WorkoutExerciseDtoValidatorTests.cs:59-67` — `Assert.True(result.IsValid)` | ✅ PASS |
| AC3: validação falha → 400 sem persistir | 400, zero session-write side effects | Finish: `FinishWorkoutExecutionHandlerTests.cs:275-295` — `Times.Never` on `GetByIdAsync` (session never even fetched). Update: `UpdateWorkoutExecutionStateHandlerTests.cs:169-189` (`HandleAsync_WhenSetHasInvalidFormat_ReturnsValidationErrorWithoutPersisting`, added post-Verifier at commit `87f8408`) — `Assert.Equal(400, ...)` + `Times.Never` on both `sessionRepository.GetByIdAsync` and `.UpdateStateAsync` for a generic invalid-format input (`Repetitions=null`, `Load=-5`). | ✅ PASS |

### WEV-05: Campo `RequireRpe` por exercício — backend half (P1)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | --------------------- | ----------------------- | ------ |
| AC1: `BlockExerciseDocumentValueObject` ganha bool `RequireRpe`, default `false` | Field exists, defaults false | `BlockExerciseDocumentValueObject.cs:8` — `public bool RequireRpe { get; set; } = false;` (build-gate only per Test Coverage Matrix — additive field, no branching) | ✅ PASS |
| AC2: plano/template salvo com `true` persiste e reflete no snapshot do `Start` | Persisted + returned + carried into session | `CreateWorkoutPlanHandlerTests.cs:381-397` — `Assert.True(capturedPlan!.Blocks[0].Exercises[0].RequireRpe)` + `Assert.True(result.Value!.Blocks[0].Exercises[0].RequireRpe)`; `UpdateWorkoutPlanHandlerTests.cs:141-156` (flip false→true) and `:160-186` (flip true→false); `CreateWorkoutTemplateHandlerTests.cs:118-132`; `UpdateWorkoutTemplateHandlerTests.cs:111-126,130-156`; snapshot flatten at `StartWorkoutExecutionHandlerTests.cs:96-162` — `Assert.True(benchPress.RequireRpe); Assert.False(squat.RequireRpe)` (mixed-flag plan, proves per-exercise fidelity, not just a blanket copy) | ✅ PASS |

### WEV-06: Toggle em massa — backend half (P1)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | --------------------- | ----------------------- | ------ |
| AC2: plano salvo após ação em massa persiste `RequireRpe=true` para todos os exercícios afetados, no mesmo fluxo de salvar já existente (sem endpoint novo) | Persisted via existing save endpoint, no new route | No endpoint files touched (`git diff --stat` shows zero `Program.cs`/routing changes) — bulk is a frontend-only state operation; backend correctness reduces to "the standard per-exercise `RequireRpe` mapping is correct for every exercise in the payload," proven exercise-by-exercise by the same T4/T5/T6/T7 evidence above. No dedicated multi-exercise-in-one-request test exists to directly exercise "N exercises, all `true`," but no bulk-specific backend code path exists either (per design.md, this is intentionally not a backend concept) | ✅ PASS (reasoned from absence of a bulk-specific code path + per-exercise evidence) |

### WEV-07: Execução bloqueia conclusão sem RPE quando exigido — backend half (P1)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | --------------------- | ----------------------- | ------ |
| AC3: `Update`/`Finish` com exercício `RequireRpe=true` e `Intensity==null` → 400 | 400, exercise identified, zero persistence | `UpdateWorkoutExecutionStateHandlerTests.cs:87-123` — `Assert.Equal(400, result.Error!.StatusCode)` + `sessionRepository.Verify(x => x.UpdateStateAsync(...), Times.Never)`; `FinishWorkoutExecutionHandlerTests.cs:149-184` — same 400 assertion + `Times.Never` on both `UpdateStateAsync` AND `publishEndpoint.Publish` (stronger — proves no `WorkoutFinished` event either) | ✅ PASS |
| AC4: exercício sem `RequireRpe` (default) → aceita `Intensity==null` | 200, persisted, bug fixed | `UpdateWorkoutExecutionStateHandlerTests.cs:169-210` — `Assert.True(result.IsSuccess)` + `Assert.False(capturedExercises![0].RequireRpe)` + `Assert.Null(capturedExercises[0].Sets[0].Intensity)`; `FinishWorkoutExecutionHandlerTests.cs:231-273` — same pattern | ✅ PASS |

### WEV-08: Validação de faixa 1–10 do RPE — backend half (P2)

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | --------------------- | ----------------------- | ------ |
| AC3: `Intensity.Value` fora de `1..10` → 400 | Rejected | `WorkoutExerciseDtoValidatorTests.cs:79-89` — `[InlineData(0)][InlineData(11)]` `Assert.False(result.IsValid)`; `UpdateWorkoutExecutionStateCommandValidatorTests.cs:48-56` — `Assert.False(result.IsValid)` for value 11 | ✅ PASS |
| AC4: `failure` trava RPE=10 → aceito normalmente | Accepted (10 is in range, no special-case code needed) | `WorkoutExerciseDtoValidatorTests.cs:91-101` — `[InlineData(1)][InlineData(10)]` `Assert.True(result.IsValid)`; no dedicated `failure`/`set.failure` backend field exists (correctly — that's a client-only concept per spec Assumptions; backend only needs to accept RPE=10, which it does) | ✅ PASS |

**Status**: 10/10 backend ACs fully covered with precise `file:line` evidence matching the spec-defined outcome. The one spec-precision gap originally flagged (WEV-02 AC3, Update-side zero-persistence assertion for the generic format-failure path) was closed post-verification at commit `87f8408`.

---

## Discrimination Sensor

| # | File:line | Description | Killed? |
| - | --------- | ------------ | ------- |
| 1 | `UpdateWorkoutExecutionStateHandler.cs:49` | Flipped RequireRpe gate condition `requireRpe && ...` → `!requireRpe && ...` | ✅ Killed — 2/5 `UpdateWorkoutExecutionStateHandlerTests` failed (`...RequiresRpeAndIntensityMissing...`, `...DoesNotRequireRpeAndIntensityMissing...`) |
| 2 | `WorkoutExerciseDtoValidator.cs:15` | Weakened Repetitions rule `GreaterThan(0)` → `GreaterThan(-1)` | ✅ Killed — 1/14 `WorkoutExerciseDtoValidatorTests` failed (`Validate_WhenRepetitionsIsZeroOrNegative_IsInvalid(repetitions: 0)`) |
| 3 | `FinishWorkoutExecutionCommandValidator.cs:14` | Commented out `RuleForEach(x => x.Exercises).SetValidator(new WorkoutExerciseDtoValidator())...` | ✅ Killed — 2/8 `FinishWorkoutExecutionHandlerTests` failed (both cases of `HandleAsync_WhenExercisesSetHasInvalidFormat_ReturnsValidationError`, expected 400 got 404) |

**Sensor depth**: lightweight (3 targeted faults, one per highest-risk new behavior: the RPE gate, the format-validation threshold, and the Finish defense-in-depth wiring)
**Result**: 3/3 killed — PASS ✅

All mutations applied directly to the real tree and reverted via `git checkout -- <file>` immediately after each run; `git status --short` confirmed clean (only the pre-existing untracked `design.md`/`tasks.md` docs remained) after each revert and after the final full-suite re-run (382/382 passed).

---

## Edge Cases (spec.md)

| Edge case | Result |
| --------- | ------ |
| Set extra (`set.isExtra`) — gate applies same as any other set | ✅ Confirmed structurally — the RequireRpe gate (`exerciseInput.Sets.Any(s => s.Intensity is null)`) and `WorkoutExerciseDtoValidator` iterate all sets uniformly with no `IsExtra` branch; `UpdateWorkoutExecutionStateHandlerTests.cs:44-85` exercises an extra set (`IsExtra: true`) through the normal persist path with no special casing |
| `failure=true` traps RPE=10 → accepted, not blocked | ✅ `WorkoutExerciseDtoValidatorTests.cs:91-101` — 10 is within the inclusive range, no special-case code required or present |
| Session started BEFORE `RequireRpe` set on plan → uses frozen snapshot, unaffected by later plan edits | ✅ Provable by code inspection (per task instructions, this is an emergent property, not test-driven): `StartWorkoutExecutionHandler.cs:58` copies `RequireRpe` from the plan into the session snapshot only once, at `Start`; `UpdateWorkoutExecutionStateHandler.cs:48` and `FinishWorkoutExecutionHandler.cs:46` read `RequireRpe` exclusively from `session.Exercises` (the persisted session document), never from a freshly-fetched plan/catalog — there is no code path in either handler that re-reads `BlockExerciseDocumentValueObject.RequireRpe` from the plan after `Start`. A later plan edit cannot reach an in-progress session's gate decision. |
| Bulk toggle overwrites ALL to `true` (not a mixed-state-preserving toggle) | N/A backend — no bulk-specific backend code path exists (see WEV-06 above); the "overwrite all" semantics live entirely in the frontend's local-state construction of the payload before the existing save call |
| `plan.phase`/`plan.difficulty` fallback for unknown values | Out of scope — [Frontend] only (WEV-04) |
| Draft preserved when navigating away mid-set | Out of scope — [Frontend] only (WEV-01) |

---

## Code Quality

| Principle | Status | Notes |
| --------- | ------ | ----- |
| Minimum code / surgical changes | ✅ | 26 files touched, all additive (+771/-24); no unrelated refactors |
| No scope creep | ✅ | `git diff --stat 24cfd1a..9a3ced8` file list matches T1–T13's "Where" fields exactly — every touched file traces to a task |
| AD-005 followed (gate is handler-level, not validator-level) | ✅ | `WorkoutExerciseDtoValidator.cs` and both command validators (`UpdateWorkoutExecutionStateCommandValidator.cs`, `FinishWorkoutExecutionCommandValidator.cs`) contain **zero** references to `RequireRpe` — confirmed by direct read of all three files. The gate exists only in `UpdateWorkoutExecutionStateHandler.cs:48-50` and `FinishWorkoutExecutionHandler.cs:46-48`, both reading `session.Exercises` (post-fetch), matching the documented pattern |
| CQRS / Result pattern / constructor injection / file-scoped namespaces | ✅ | Consistent with existing handlers (`Result<T>.Failure/.Success`, constructor-injected repositories/validators, `namespace X;` style) throughout all touched files |
| Null-safe Intensity mapping (Risks & Concerns mitigation) | ✅ | Both `UpdateWorkoutExecutionStateHandler.cs:69` and `FinishWorkoutExecutionHandler.cs:64` use `s.Intensity is null ? null : new IntensityDocumentValueObject {...}` — the force-unwrap risk flagged in design.md was correctly fixed, matching the pre-existing `StartWorkoutExecutionHandler.cs:67` pattern |
| Defensive Copy fixes (data-loss prevention) | ✅ | `WorkoutPlanMappings.cs:38` (`Clone`) and `CopyWorkoutTemplateHandler.cs:54` both carry `RequireRpe` — confirmed by direct read; no dedicated test per tasks.md's explicit (and reasonable) scope note |
| Per-layer Coverage Expectation met (tasks.md matrix) | ✅ | Validators/handlers unit-tested per every listed edge case; VO/DTO-only changes correctly left test-free (build-gate only, matches matrix) |
| Every test maps to a spec AC / Done-when criterion | ✅ | No unclaimed tests found — all new/extended test methods trace to WEV-02/05/06/07/08 or a T-task's Done-when list |
| Documented guidelines followed | ✅ | `src/AGENTS.md` conventions (CQRS, Result, FluentValidation, file-scoped namespaces) — no deviations found |

---

## Gate Check

- **Gate command**: `dotnet test tests/UnitTests/UnitTests.csproj`
- **Result at Verifier PASS**: **382 passed**, 0 failed, 0 skipped
- **Result after closing the spec-precision gap** (`87f8408`): **383 passed**, 0 failed, 0 skipped
- **Baseline per tasks.md Progress Log**: 355 after Batch 1 (T1–T7), 382 after Batch 2 (T8–T13) — **confirmed matching**, no discrepancy
- **Test delta**: +27 net across T8–T13, +1 post-verification hardening
- **Skipped tests**: none
- **Failures**: none

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status |
| ----------- | ---------------- | ---------- |
| WEV-02 | Pending | ✅ Verified (backend-only requirement — fully covered) |
| WEV-05 | Pending | Backend done / Frontend pending |
| WEV-06 | Pending | Backend done / Frontend pending |
| WEV-07 | Pending | Backend done / Frontend pending |
| WEV-08 | Pending | Backend done / Frontend pending |
| WEV-01, WEV-03, WEV-04 | Pending | Untouched — 100% [Frontend], out of scope for this repo/review |

---

## Summary

**Overall**: ✅ Ready (backend scope)

**Spec-anchored check**: 10/10 backend ACs matched spec-defined outcome with direct `file:line` evidence (0 open spec-precision gaps after the post-verification hardening commit)
**Sensor**: 3/3 mutations killed (RPE gate condition, Repetitions threshold, Finish validator wiring) — all targeting the load-bearing new behaviors
**Gate**: 383/383 passed, 0 failed, 0 skipped (382 at Verifier PASS + 1 hardening test)

**What works**: The core bug fix (RPE optional by default) is correctly implemented and regression-tested; the `RequireRpe` gate correctly reads from the frozen session snapshot (never re-derived from the catalog/plan) in both `Update` and `Finish`, with zero persistence/publish side effects before rejection; `RequireRpe` threads end-to-end from authoring (Plans + Templates, create + update) through the `Start` snapshot without resetting on repeated `Update`/`Finish` calls (proven by explicit "carries forward" assertions in both handler test suites); the previously-identified Finish defense-in-depth gap is closed and proven to have teeth by the discrimination sensor; Repetitions/Load regression rules are intact; AD-005 (handler-level gate, not validator-level) was followed with no leakage of `RequireRpe` into any validator.

**Issues found**: none open. The one issue found during verification (WEV-02 AC3, Update-handler zero-persistence assertion missing for the generic format-failure path) was fixed immediately (`87f8408`, `HandleAsync_WhenSetHasInvalidFormat_ReturnsValidationErrorWithoutPersisting`) and re-run green.

**Next steps**: None for backend — feature is verification-complete for all in-scope backend ACs (WEV-02, WEV-05, WEV-06, WEV-07, WEV-08 backend halves). Frontend halves (WEV-01, WEV-03, WEV-04, and the [Frontend] halves of WEV-05/06/07/08) are handed off to `ShapeUp-Web` per design.md's Cross-repo handoff section.

**Lessons distillation note**: no `scripts/lessons.py`/`.specs/lessons.json` infrastructure exists in this repo (confirmed absent) — the self-improving lessons layer described in this skill's `lessons.md` was not applied. The one signal that would have qualified (the now-closed spec-precision gap) is resolved within this same validation cycle, so nothing was lost by skipping it here; if this project adopts the lessons layer later, the `spec_precision_gap` signal type from this report is available for backfill.
