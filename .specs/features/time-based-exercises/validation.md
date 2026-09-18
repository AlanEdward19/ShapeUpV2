# Time-Based Exercises Validation

**Date**: 2026-09-18
**Spec**: `.specs/features/time-based-exercises/spec.md`
**Diff range**: `848a610..HEAD` (as instructed); the feature's own commits are the narrower, precisely-bounded range `5c457c0..7f886ab` (23 commits: `feat(training): add ExerciseType enum for catalog classification` through `fix(training): avoid RestSeconds null-unwrap crash for time-based sets in execution state update`), matching tasks.md's T1-T22. Commits between `848a610` and `5c457c0` belong to three other, already-closed features (`exercise-variations`, `workout-execution-validation`, `workout-schedule-dashboard`/`xp-feedback-loop` doc closures) and are out of scope for this verification.
**Verifier**: independent sub-agent (author ≠ verifier), fresh context, no prior knowledge of implementation batches.
**Scope**: Backend-only pass per tasks.md's Scope note. TBE-02's editor UI, TBE-03's client-side gate, and TBE-05 (frontend rendering) are explicitly out of scope and are not counted as gaps.

---

## Task Completion

All 22 tasks (T1-T22) in tasks.md are checked off `[x]`. Verified by reading the actual diff/code, not by trusting the checkboxes:

| Task | Status | Notes |
|---|---|---|
| T1-T2 | ✅ Done | `ExerciseType` enum + `Exercise.ExerciseType` column confirmed in `src/Features/Training/Shared/Enums/ExerciseType.cs`, `src/Features/Training/Shared/Entities/Exercise.cs:12` |
| T3 | ✅ Done | Catalog CRUD threading confirmed + tested |
| T4-T7 | ✅ Done | VO fields + null-safe `Volume` + response surfacing confirmed |
| T8-T13 | ✅ Done | Passthrough across plan/template/session flows confirmed, including two documented `SPEC_DEVIATION` data-loss fixes (`FinishWorkoutExecutionHandler.cs:58-64`, `UpdateWorkoutExecutionStateHandler.cs:66-72`) which are real and correctly reasoned |
| T14-T19 | ✅ Done | Validator relaxation + TimeBased gates (plan/template create+update, execution state) confirmed with matching tests |
| T20-T22 | ✅ Done | `best_pace` PR logic in both `EvaluatePrs` copies, plus the `AntiCheatClassifier` nullable-safety fix, confirmed |

---

## Spec-Anchored Acceptance Criteria

Only backend-scope ACs are evaluated. `[Frontend]`-tagged ACs (TBE-03 AC1-AC3, TBE-05 all, TBE-04 AC1) and TBE-02 AC1 (editor UI) are out of scope per tasks.md's Scope note and are not scored.

| Criterion (WHEN X THEN Y) | Spec-defined outcome | `file:line` + assertion | Result |
|---|---|---|---|
| TBE-01 AC1: mark `ExerciseType` on create/edit | Persisted value returned on create | `tests/UnitTests/Domains/Training/Exercises/CreateExerciseHandlerTests.cs:28` — `Assert.Equal(ExerciseType.TimeBased, result.Value!.ExerciseType)` | ✅ PASS |
| TBE-01 AC2: pre-existing exercise defaults to `WeightBased` | Omitted type → `WeightBased` | `CreateExerciseHandlerTests.cs:44` — `Assert.Equal(ExerciseType.WeightBased, result.Value!.ExerciseType)` | ✅ PASS |
| TBE-01 AC3: persists and reflects on update / snapshot | Update changes persisted type; reflected in `GetById`/`MapResponse` | `tests/UnitTests/Domains/Training/Exercises/UpdateExerciseHandlerTests.cs:26-27` — `Assert.Equal(ExerciseType.TimeBased, result.Value!.ExerciseType)` + `Assert.Equal(ExerciseType.TimeBased, existing.ExerciseType)`; snapshot flatten: `tests/UnitTests/Domains/Training/Workouts/StartWorkoutExecutionHandlerTests.cs:255,260` — `Assert.Equal(ExerciseType.TimeBased, running.ExerciseType)` / `Assert.Equal(ExerciseType.WeightBased, benchPress.ExerciseType)` | ✅ PASS |
| TBE-02 AC2 [Backend half]: reject plan save with `TimeBased` set missing duration, message names the field/exercise | 400 + message naming exercise id | `tests/UnitTests/Domains/Training/WorkoutPlans/CreateWorkoutPlanHandlerTests.cs:521-524` — `Assert.Equal(400, result.Error!.StatusCode)` + `Assert.Contains("'1'", result.Error.Message, ...)`; mirrored in `UpdateWorkoutPlanHandlerTests.cs:269+`, `CreateWorkoutTemplateHandlerTests.cs:156+`, `UpdateWorkoutTemplateHandlerTests.cs:162+` | ✅ PASS |
| TBE-02 AC2 (positive path): duration filled, no distance → saves normally | Success, `DurationSeconds` persisted, `DistanceMeters` null | `CreateWorkoutPlanHandlerTests.cs:552-554` — `Assert.Equal(120, capturedPlan!.Blocks[0].Exercises[0].Sets[0].DurationSeconds)` + `Assert.Null(...DistanceMeters)` | ✅ PASS |
| TBE-02 AC3: `Technique` restricted to `Straight` for `TimeBased` | Non-`Straight` technique on `TimeBased` set → rejected | `CreateWorkoutPlanHandlerTests.cs:574-578` — `Assert.True(result.IsFailure)` + `Assert.Equal(400, result.Error!.StatusCode)`; error message: `TrainingErrors.TechniqueNotAllowedForTimeBasedExercise` (`src/Features/Training/Shared/Errors/TrainingErrors.cs:46-47`) | ✅ PASS |
| TBE-02 AC4: `TimeBased` inside heterogeneous block (Superset/Amrap/Emom) accepted without restriction | No new restriction added; existing block-type validators unchanged in this diff | No direct new test found combining `TimeBased` exercise + `BlockType.Superset`/`Amrap`/`Emom` in the same block | ⚠️ Spec-precision/coverage gap (see below) |
| TBE-03 AC4 [Backend]: `UpdateWorkoutExecutionStateCommand` with `TimeBased` set, `DurationSeconds` null/0/negative → 400 | 400 + message naming exercise | `tests/UnitTests/Domains/Training/Workouts/UpdateWorkoutExecutionStateHandlerTests.cs:269-271` — `Assert.Equal(400, ...)` + `Assert.Contains("'1'", ...)`; zero/negative theory at `:280-312` | ✅ PASS |
| TBE-03 AC5 [Backend]: `WeightBased` set unaffected | Existing WEV-02 gate behavior identical | `UpdateWorkoutExecutionStateHandlerTests.cs:360+` — `HandleAsync_WhenWeightBasedExerciseSetHasNoDuration_PersistsSuccessfullyUnaffectedByTimeBasedGate` (regression, unaffected) | ✅ PASS |
| TBE-03 AC6: duration cleared post-completion → auto-uncomplete | No per-set "completed" flag exists in backend model; T19 confirmed by code inspection this concept is `[Frontend]`-only (`set.completed` in `TrainingPlansClient.jsx`) | No backend code/state to assert against — verified there genuinely is no per-set completed flag in `WorkoutSessionDocument`/`ExecutedSetDocumentValueObject` | ⚠️ Out of backend scope (consistent with Scope note, not a gap) |
| TBE-04 AC2: `RequireRpe=true` blocks `TimeBased` set completion without RPE | Same mechanism as WEV-07, applies uniformly regardless of `ExerciseType` | Code inspection confirms the `RequireRpe` gate (`UpdateWorkoutExecutionStateHandler.cs:49-51`, `FinishWorkoutExecutionHandler.cs:47-49`) never branches on `ExerciseType` — but no existing test combines `ExerciseType.TimeBased` with `RequireRpe = true` to prove it directly | ⚠️ Spec-precision/coverage gap (see below) — low risk since code path is shared, not new |
| TBE-06 AC1: `TimeBased` set with duration+distance → compute pace, compare to history | `pace = DurationSeconds / DistanceMeters`; new best → PR | `tests/UnitTests/Domains/Training/Workouts/FinishWorkoutExecutionHandlerTests.cs:359-364` — `Assert.Equal(0.3m, pacePrs[0].Value)` (300/1000=0.3, matches spec formula exactly) + `Assert.Equal(5, pacePrs[0].ExerciseId)`; mirrored in `WorkoutHandlerTests.cs:162-164` for `CompleteWorkoutSessionHandler` | ✅ PASS |
| TBE-06 AC2: better pace → `"best_pace"` PR persisted | `Type == "best_pace"` | Same lines as above — `pacePrs = capturedPrs!.Where(pr => pr.Type == "best_pace")`, `Assert.Single(pacePrs)` | ✅ PASS |
| TBE-06 AC3: `TimeBased` set without distance → no PR attempted | Empty PR list, not an error | `FinishWorkoutExecutionHandlerTests.cs:407-409` — `Assert.True(result.IsSuccess)` + `Assert.Empty(capturedPrs!)` | ✅ PASS |
| TBE-06 AC4: `WeightBased` set → only `max_volume`-family PRs, never `best_pace` | No crossover | `FinishWorkoutExecutionHandlerTests.cs:463-465` — `Assert.Contains(..., pr.ExerciseId == 5 && pr.Type == "best_pace")` + `Assert.DoesNotContain(..., pr.ExerciseId == 1 && pr.Type == "best_pace")`; mirrored `WorkoutHandlerTests.cs:246-248` | ✅ PASS |
| Domain logic: `ExecutedSetDocumentValueObject.Volume` null-safety | `0m` when Load null, Repetitions null, or both; product when both present | `tests/UnitTests/Domains/Training/Workouts/ExecutedSetDocumentValueObjectTests.cs:12,20,28,36` — all 4 branches asserted exactly | ✅ PASS |
| `AntiCheatClassifier.FlattenPairs` nullable-safety | No throw on `TimeBased`-only session (Load/Reps null) | `tests/UnitTests/Domains/Gamification/AntiCheatClassifierDuplicationTests.cs:129` — `ClassifyAsync_WhenSessionHasOnlyTimeBasedSetsWithNullLoadAndRepetitions_DoesNotThrow` | ✅ PASS |
| Response surfacing (TBE-05 backend half): API returns `DurationSeconds`/`DistanceMeters` for `TimeBased` sets | Fields present in `WorkoutSessionResponse` | `tests/UnitTests/Domains/Training/Workouts/WorkoutSessionResponseMapperTests.cs:43-44` — `Assert.Equal(1800, mappedSet.DurationSeconds)` + `Assert.Equal(5000m, mappedSet.DistanceMeters)` | ✅ PASS |

**Status**: ❌ Gaps present (2 spec-precision/coverage gaps below the "PASS" bar; both low-severity, neither indicates broken behavior by code inspection) — see Ranked Gaps.

---

## Ranked Gaps

1. **TBE-04 AC2 — no direct test proves `RequireRpe` gate applies to a `TimeBased` exercise.** `UpdateWorkoutExecutionStateHandler.cs:49-51` and `FinishWorkoutExecutionHandler.cs:47-49` implement the RPE-required check without any `ExerciseType` branch (confirmed by reading both files in full — the check runs identically for both types), so the mechanism is almost certainly correct by construction. But per evidence-or-zero, no existing test constructs a `TimeBased`-shaped exercise (`ExerciseType.TimeBased`, no Load/Repetitions) with `RequireRpe = true` and asserts the 400/RPE-required outcome. All existing `RequireRpe` tests use `WeightBased`-shaped fixtures (e.g. "Bench Press"). Fix: add one test per handler using a `TimeBased` exercise fixture with `RequireRpe = true` and a set lacking `Intensity`, asserting `TrainingErrors.RpeRequiredForExercise`.
2. **TBE-02 AC4 — no direct test proves a `TimeBased` exercise inside a `Superset`/`Amrap`/`Emom` block (mixed with `WeightBased` exercises) is accepted without restriction.** This AC is satisfied by *absence* of a restriction (no code changed to add one), which is harder to prove than a positive assertion. Existing block-type tests (Superset/Amrap/Emom minimums) don't combine block type with `ExerciseType.TimeBased`. Fix: add one test creating a plan with a `Superset`/`Amrap` block containing one `WeightBased` and one `TimeBased` exercise, asserting success and that both sets' fields round-trip correctly.

Both gaps are test-coverage gaps, not implementation defects — code inspection in both cases shows the relevant logic is type-agnostic/unrestricted as required. Neither blocks the backend-only pass's core guarantees (gate correctness, PR correctness, data passthrough), which are fully evidenced. Given the low severity and that these affect only defense-in-depth test coverage (not the primary MVP gates TBE-01/02/03/06), I'm not blocking the PASS verdict on them, but flagging both as fix tasks for a future iteration.

---

## Discrimination Sensor

All mutations applied and reverted in the real working tree (no worktree/stash used — direct edit + `dotnet test` + manual revert, confirmed clean via `git status`/`git diff` after each cycle and at the end).

| # | File:line | Description | Killed? |
|---|---|---|---|
| 1 | `src/Features/Training/WorkoutPlans/CreateWorkoutPlan/CreateWorkoutPlanHandler.cs:46` | Flipped `mapped.ExerciseType == ExerciseType.TimeBased` → `!=` | ✅ Killed — 15/26 tests failed in `CreateWorkoutPlanHandlerTests` (both the new TimeBased-gate tests and, notably, the WeightBased-path tests, since the gate now wrongly engages for WeightBased exercises) |
| 2 | `src/Features/Training/Workouts/FinishWorkoutExecution/FinishWorkoutExecutionHandler.cs:165,171` | Removed the `&& s.DistanceMeters is > 0` guard from both `pacedCurrentSets`/`pacedHistorySets` filters in `EvaluatePrs` | ✅ Killed — `HandleAsync_WhenTimeBasedSetHasNoDistance_AddsNoPrForThatSet` failed with `System.InvalidOperationException: Nullable object must have a value` (force-unwrap of null `DistanceMeters`) |
| 3 | `src/Features/Training/Shared/Documents/ValueObjects/ExecutedSetDocumentValueObject.cs:23-25` | Changed null-safe `Volume` to `(Load ?? 0m) * (Repetitions ?? 1)` (wrong default, no longer guards both-null) | ✅ Killed — `Volume_WhenRepetitionsIsNull_ReturnsZero` failed (expected 0, got 100) |

**Sensor depth**: lightweight (3 targeted mutations, default tier)
**Result**: 3/3 killed — ✅ PASS

Working tree confirmed clean after all three cycles: `git status --short` empty, `git diff --stat` empty, full suite re-run green (458/458) after final revert.

---

## Code Quality

| Principle | Status |
|---|---|
| No features beyond what was asked | ✅ — scope matches tasks.md's 22 tasks exactly |
| No abstractions for single-use code | ✅ |
| No unnecessary "flexibility" added | ✅ |
| Only touched files required for task | ✅ — diff is confined to Training/Gamification handlers, VOs, validators, migrations, and their tests |
| Didn't "improve" unrelated code | ✅ — two `SPEC_DEVIATION` comments document narrowly-scoped, necessary data-loss fixes discovered mid-implementation, not speculative improvements |
| Matches existing patterns/style | ✅ — reuses `RestSeconds`/`RequireRpe` nullable/flatten precedents throughout |
| Would senior engineer approve? | ✅ |
| Tests map to acceptance criteria and are non-shallow | ✅ — spot-checked TBE-06 (pace math verified to exact decimal value, not just "PR exists") |
| Spec-anchored outcome check | ⚠️ — 2 gaps noted above; all other ACs match spec-defined outcomes precisely |
| Per-layer Coverage Expectation met | ✅ — matches tasks.md's own Test Coverage Matrix |
| Every test in scope maps to a spec AC/Done-when criterion | ✅ — no unclaimed/orphan tests found in the diff |
| Documented project quality/testing guidelines followed | `src/AGENTS.md` "Unit Testing" section (mock repositories/validators, test handlers in isolation) — followed throughout |

---

## Edge Cases (from spec.md)

- [x] Reclassification of exercise type after use in existing plans — snapshot-at-start principle preserved (reuses `RequireRpe` mechanism; no new code path, verified by design consistency)
- [x] `TimeBased` set marked `IsExtra` — no special-casing found or needed; gate applies uniformly (confirmed by code inspection — no `IsExtra` branch anywhere near the TimeBased gates)
- [ ] Invalid duration format (non-numeric mm:ss input) → treated as empty for gate purposes — **[Frontend], out of scope**, not evaluated
- [x] `DistanceMeters` negative → rejected — `WorkoutExerciseDtoValidatorTests.cs:139-146` (`Validate_WhenDistanceMetersIsNegative_IsInvalid`)
- [x] `TimeBased` exercise inside `Amrap`/`Emom` — no automatic duration-vs-block-timer reconciliation attempted (confirmed: no such logic exists anywhere in the diff)

---

## Gate Check

- **Gate command**: `dotnet build src/ShapeUp.csproj` then `dotnet test tests/UnitTests/UnitTests.csproj` (IntegrationTests skipped per instructions — pre-existing FK-cascade migration bug in `AddExerciseEquivalents`, unrelated to this feature)
- **Build result**: 0 errors, 2 pre-existing NU1903 vulnerability warnings (unrelated to this feature)
- **Test result**: **458 passed, 0 failed, 0 skipped**
- **Failures**: none
- **Skipped tests**: none

---

## Fix Plans

### Fix 1: Add RequireRpe + TimeBased combination test (TBE-04 AC2)
- **Root cause**: test coverage gap, not an implementation defect — code is type-agnostic by inspection
- **Fix task**: Add `HandleAsync_WhenTimeBasedExerciseRequiresRpeAndIntensityMissing_ReturnsValidationError` to `UpdateWorkoutExecutionStateHandlerTests.cs` and an equivalent to `FinishWorkoutExecutionHandlerTests.cs`, using an `ExerciseType.TimeBased` fixture with `RequireRpe = true`
- **Priority**: Minor

### Fix 2: Add mixed-type block acceptance test (TBE-02 AC4)
- **Root cause**: test coverage gap for a "no restriction added" AC
- **Fix task**: Add a `CreateWorkoutPlanHandlerTests` case with a `Superset` or `Amrap` block containing one `WeightBased` and one `TimeBased` exercise, asserting success and correct field round-trip for both
- **Priority**: Minor

---

## Requirement Traceability Update

| Requirement | Previous Status | New Status |
|---|---|---|
| TBE-01 | Pending | ✅ Verified (backend) |
| TBE-02 | Pending | ✅ Verified (backend half) — 1 minor coverage gap (AC4) |
| TBE-03 | Pending | ✅ Verified (backend half) — AC6 confirmed N/A to backend (no per-set completed flag exists) |
| TBE-04 | Pending | ✅ Verified (backend half) — 1 minor coverage gap (AC2 direct test) |
| TBE-05 | Pending | Out of scope this pass (frontend) — backend half (response surfacing) ✅ Verified |
| TBE-06 | Pending | ✅ Verified (backend) |

---

## Summary

**Overall**: ⚠️ Issues (2 minor test-coverage gaps; no implementation defects found; both gate and sensor are clean)

**Spec-anchored check**: 16/18 backend-scope AC-level rows matched spec outcome exactly with precise `file:line` evidence; 2 flagged as coverage gaps (not code defects — both verified correct by direct code inspection, just lacking a dedicated test)
**Sensor**: 3/3 mutations killed
**Gate**: 458 passed, 0 failed (UnitTests); build clean (0 errors)

**What works**: `ExerciseType` catalog classification, nullable-safe set VOs (`Volume`), full passthrough across plan/template/session authoring and execution flows, backend completion gates for both plan-save and execution-state-update paths (mirrored across plan/template create/update), `best_pace` PR calculation with exact-value verification, and defensive nullable-safety fixes in `AntiCheatClassifier`/`UpdateWorkoutExecutionStateHandler` — all evidenced with precise `file:line` assertions matching the spec's exact defined outcomes (e.g. pace = 300/1000 = 0.3 verified to the decimal).

**Issues found**: Two test-coverage gaps (TBE-04 AC2 RPE+TimeBased combination; TBE-02 AC4 TimeBased-in-heterogeneous-block). Both are code-inspection-confirmed non-issues in terms of actual behavior, but lack the direct test evidence this Verifier's evidence-or-zero rule requires for a clean PASS.

**Next steps**: Add the two fix-task tests above (low effort, ~30 min); no code changes needed since the underlying logic is already correct.
