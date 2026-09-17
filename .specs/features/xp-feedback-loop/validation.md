# XP Feedback Loop — Validation

**Date**: 2026-09-16
**Spec**: `.specs/features/xp-feedback-loop/spec.md`
**Diff range**: `ShapeUpApi` `93d99f0..6be1403` (1 commit, `6be1403`)
**Verifier**: independent sub-agent (author ≠ verifier)
**Scope**: Backend only. This spec is ~100% `ShapeUp-Web` (frontend) work — a "+120 XP" celebration popup and a dashboard progress-bar rendering fix. The ONLY backend-relevant surface is XPF-04 (`GET /api/gamification/me` `totalXp`/`level` consistency) and the backend half of XPF-06 (root-cause investigation of the zeroed progress bar). XPF-01/02/03/05 and the frontend half of XPF-06 are 100% `ShapeUp-Web` and are not reviewed here.

---

## Task Completion

This feature has no `tasks.md` task list in the conventional sense — Design phase concluded that root-cause investigation found no backend defect, so Execute produced exactly one artifact: a new regression test locking in an invariant that already held. There is one "task" to verify:

| Task | Status | Notes |
| ---- | ------ | ----- |
| Add regression test proving `TotalXp`/`Level` consistency at the read seam (`6be1403`) | ✅ Done | `GetGamificationProfileHandlerTests.HandleAsync_WhenTotalXpIsNotOnLevelBoundary_ReturnsLevelConsistentWithTotalXp` added at end of `tests/UnitTests/Domains/Gamification/GetGamificationProfileHandlerTests.cs` (lines 102-125). Confirmed present in tree, confirmed it's the only change in the commit. |

No production code change was made, and design.md is explicit that none was warranted (see Root Cause Investigation section, reproduced and independently re-verified below).

---

## Root-Cause Investigation — Independently Re-Verified

**Claim under test**: there is no backend defect causing the zeroed progress bar; `TotalXp` and `Level` cannot diverge server-side; there is no cache/staleness window.

I did not trust design.md's citations — I opened every file myself.

1. **Write path is atomic (2 independent producers, both correct).**
   - `GamificationWorkoutFinishedConsumer.ApplyCredit` (`src/Features/Gamification/WorkoutFinished/GamificationWorkoutFinishedConsumer.cs:108` `profile.TotalXp += WorkoutXpReward;` and `:116` `profile.Level = newLevel;` where `newLevel = LevelCalculator.CalculateFromTotalXp(profile.TotalXp)` at `:115`) — both writes happen on the same tracked `GamificationProfile` entity, followed by exactly one `SaveChangesAsync(cancellationToken)` at `:92`. Design.md's line citations (`:108,115`) matched exactly on direct read.
   - **Second, independent write path found and checked** (not cited with correct line numbers in design.md, but substance verified): `GamificationNutritionGoalMetConsumer.cs:44` (`profile.TotalXp += NutritionXpReward;`) and `:51` (`profile.Level = LevelCalculator.CalculateFromTotalXp(profile.TotalXp);`), followed by one `SaveChangesAsync` at `:61`. Same atomic pattern. (Minor documentation nit: design.md cites this file at a path/line pair — `GamificationWorkoutFinishedConsumer.cs:38,51` — that doesn't match; the file is actually at `src/Features/Gamification/NutritionGoalMet/GamificationNutritionGoalMetConsumer.cs` and the real write lines are `44,51`, not `38,51` — `38` is the unrelated `Level = LevelCalculator.CalculateFromTotalXp(0)` in the new-profile branch. Cosmetic — the substantive claim, "same tracked entity, single `SaveChangesAsync`," holds for this file too.)
   - **Exhaustive search**: `grep -n '\.Level\s*=|\.TotalXp\s*(\+=|=)' src/` returns exactly these 4 lines across exactly these 2 files. No other write site to `GamificationProfile.Level` or `.TotalXp` exists anywhere in `src/`.

2. **Read path has zero caching.** `GetGamificationProfileHandler.HandleAsync` (`src/Features/Gamification/GetGamificationProfile/GetGamificationProfileHandler.cs:17-19`) does one `dbContext.Profiles.AsNoTracking().FirstOrDefaultAsync(...)`, then maps `TotalXp`/`Level` verbatim (`:52-53`, `profile.TotalXp` / `profile.Level`) into `GamificationProfileResponse`. Grepped `IMemoryCache|IDistributedCache|ResponseCache|OutputCache` across all of `src/` (not just Gamification, per the task's ask): the only hit is `src/AGENTS.md:385`, a documentation line recommending `IMemoryCache` as a general convention — not actual usage anywhere in code. Zero real cache in the read path.

3. **No server-side "XP in current level" field exists.** `GamificationProfile` entity (`src/Features/Gamification/Shared/Entities/GamificationProfile.cs`) has exactly the fields shown (`UserId, TotalXp, Level, CurrentStreak, NutritionCurrentStreak, LastNutritionGoalMetDate, LastActivityDateUtc, ShapeCoins, LastStreakMilestoneAwarded, LastEvaluationLeveledUp, LastEvaluationLevelFrom, LastEvaluationLevelTo, LastEvaluationStreakMilestoneHit, LastEvaluationStreakMilestoneValue, UpdatedAtUtc`) — no in-level-XP or percentage field. `GamificationProfileResponse` (same directory) mirrors this — no such field on the DTO either. `LevelCalculator.CalculateFromTotalXp(totalXp) => totalXp / 500 + 1` (`src/Features/Gamification/Shared/LevelCalculator.cs:7-8`) is the only XP→Level math on the backend; there is no `totalXp % 500` (in-level remainder) computed anywhere server-side. Confirms the "100% client-side math" claim.

4. **Counter-example search**: I looked for any path where `Level` could persist without `TotalXp` updating in the same operation, or vice versa. All 4 write sites found in step 1 update both fields together, unconditionally, before a single `SaveChangesAsync`, with no early return between the two writes and no conditional branch that writes one without the other. No admin/seed/migration write path touches these fields (`GamificationDbContextModelSnapshot.cs` and the two `*.Designer.cs` migration files matched the earlier `GamificationProfile` grep only as EF Core scaffolding metadata, not runtime writes). **No divergence path found.**

**Verdict: the "no backend bug" claim is independently confirmed.** The root-cause conclusion (bug is 100% frontend, in `ShapeUp-Web`'s `GamificationProgressCard.jsx`) is sound given everything reachable from this repo.

---

## Spec-Anchored Acceptance Criteria

### XPF-04: Barra de progresso — consistência `totalXp`/`level` na API [Backend] (P1)

| Criterion | Spec-defined outcome | `file:line` + evidence | Result |
| --------- | --------------------- | ----------------------- | ------ |
| AC1: `GET /api/gamification/me` SHALL retornar `totalXp` atualizado de forma consistente com `level` na MESMA resposta | Both fields read from the same row, mapped verbatim, no divergence possible | `GetGamificationProfileHandler.cs:17-19` (single `AsNoTracking` read) + `:52-53` (verbatim mapping); write-side invariant enforced at `GamificationWorkoutFinishedConsumer.cs:108,115-116` and `GamificationNutritionGoalMetConsumer.cs:44,51`, both single-`SaveChangesAsync`; **regression-locked** by new test `GetGamificationProfileHandlerTests.cs:102-125`, which asserts `Assert.Equal(LevelCalculator.CalculateFromTotalXp(result.Value.TotalXp), result.Value.Level)` — deriving the expected `Level` from the oracle function rather than a hardcoded duplicate, for a non-level-boundary `TotalXp=750` (`750 % 500 = 250 ≠ 0`, confirmed by `Assert.NotEqual(0, ...)` in the same test, so the scenario is a genuine non-trivial case, not a boundary value that would pass by accident) | ✅ PASS |

### XPF-06: Barra de progresso — investigação de causa raiz + teste de regressão [Backend half] (P1)

| Criterion | Spec-defined outcome | `file:line` + evidence | Result |
| --------- | --------------------- | ----------------------- | ------ |
| AC5 (backend half): root-cause investigation completed BEFORE proposing a fix; regression test added reproducing/locking the invariant | Documented investigation ruling backend in/out; a test that would fail under the described symptom | design.md §"Root Cause Investigation" (lines 9-26) documents the investigation; independently re-verified above (Root-Cause Investigation section) — confirmed no backend defect exists, confirmed via exhaustive grep of all 4 write sites and the read path. New test (`GetGamificationProfileHandlerTests.cs:102-125`) added as the regression lock, proven to have teeth by the Discrimination Sensor below | ✅ PASS (backend half only — frontend half requires a `ShapeUp-Web`-side reproduction test per spec, out of reach from this repo) |

**Out of scope / not reviewed** (100% `ShapeUp-Web`, no backend code exists for these): XPF-01 (popup pending/resolution state), XPF-02 (popup timeout/neutral state), XPF-03 (popup image placeholder slot), XPF-05 (progress-bar rendering fix in `GamificationProgressCard.jsx`), and the frontend half of XPF-06 (frontend regression test reproducing the broken render).

---

## Discrimination Sensor

| # | File:line | Description | Killed? |
| - | --------- | ------------ | ------- |
| 1 | `GetGamificationProfileHandler.cs:53` | Hardcoded response mapping `Level: profile.Level` → `Level: 1` | ✅ Killed — 2/3 `GetGamificationProfileHandlerTests` failed: the new test (`HandleAsync_WhenTotalXpIsNotOnLevelBoundary_ReturnsLevelConsistentWithTotalXp`, expected 2 got 1) AND the pre-existing test (`HandleAsync_WhenProfileExists_ReturnsStoredValuesAndCalculatedShapeScore`, expected 2 got 1) |

**Sensor depth**: lightweight (1 targeted fault on the exact seam the new test claims to guard — the response-mapping line for `Level`)
**Result**: 1/1 killed — PASS ✅. The new test does not merely duplicate the pre-existing test's coverage: it derives the expected value from `LevelCalculator.CalculateFromTotalXp` (the oracle) rather than repeating the hardcoded `2`, so it would also catch a regression where `Level` is mapped from any other wrong-but-coincidentally-plausible source, not just a literal `1`.

Mutation applied directly to the real tree (`src/Features/Gamification/GetGamificationProfile/GetGamificationProfileHandler.cs`) and reverted via `git checkout -- <file>` immediately after the run; `git status` confirmed clean (only the pre-existing untracked `design.md`) after revert, and the full suite was re-run green (384/384) after reverting.

---

## Edge Cases (spec.md)

| Edge case | Result |
| --------- | ------ |
| Cache-invalidation-only fix if root cause were frontend cache (spec line 100: "se a causa raiz for cache do frontend... a correção NÃO exige mudança de contrato de API") | ✅ Consistent with findings — root cause is frontend (a `GamificationProgressCard.jsx`-side stale-render/field-mismatch issue per design.md's three hypotheses), and no API contract change was made or is needed; `GamificationProfileResponse` shape is untouched by this commit |
| Non-level-boundary `TotalXp` (the case where the symptom would actually manifest, per spec) | ✅ Test seeds `TotalXp=750` (`750 % 500 = 250`), not a multiple of 500 — the case where a naive "in-level XP" computation could plausibly diverge from `Level` if it existed server-side; confirms the test isn't accidentally testing a trivial/boundary case |
| Two consumers independently write `TotalXp`/`Level` (`WorkoutFinished` and `NutritionGoalMet`) | ✅ Both checked directly; both atomic, both correct — this wasn't called out with correct line numbers in design.md but the substance is sound (see Root-Cause Investigation §1) |
| Popup pending/timeout/offline-sync edge cases (spec lines 96-99) | Out of scope — 100% [Frontend], not reviewed |

---

## Code Quality

| Principle | Status | Notes |
| --------- | ------ | ----- |
| Minimum code / surgical change | ✅ | `git diff --stat 93d99f0..6be1403` touches exactly 1 file, +25/-0 lines — a single new test method, nothing else |
| No scope creep | ✅ | No production code changed; matches design.md's explicit "no backend code fix is required or appropriate" conclusion |
| No speculative fix for an unconfirmed bug | ✅ | Design.md's Tech Decisions table explicitly rejects making a speculative backend change with no identified defect — verified this discipline was followed (zero production diffs) |
| Test uses the oracle function, not a hardcoded duplicate | ✅ | `Assert.Equal(LevelCalculator.CalculateFromTotalXp(result.Value.TotalXp), result.Value.Level)` — confirmed by direct read of the test and of `LevelCalculator.cs` (formula: `totalXp / 500 + 1`); `CalculateFromTotalXp(750) = 2`, matching the test's seeded `Level: 2`, confirming the test's own fixture is internally consistent (not a test bug) |
| Test follows existing suite's InMemory-EF-Core pattern | ✅ | Reuses `_dbContext`, `_workoutSessionRepository` mock, and `CreateHandler()` helper exactly as the two pre-existing tests in the same file do |

---

## Gate Check

- **Gate command**: `dotnet test tests/UnitTests/UnitTests.csproj`
- **Result before this commit** (`93d99f0`, implied by baseline): 383 passed
- **Result after this commit** (`6be1403`, independently re-run): **384 passed**, 0 failed, 0 skipped — confirms the claimed +1 net test delta
- **Result during sensor mutation**: 1 passed, 2 failed (of the 3 `GetGamificationProfileHandlerTests`) — as expected, mutation killed
- **Result after sensor revert**: **384 passed**, 0 failed, 0 skipped — clean

---

## Requirement Traceability Update

Applied to `.specs/features/xp-feedback-loop/spec.md`'s Requirement Traceability table:

| Requirement | Previous Status | New Status |
| ----------- | ---------------- | ---------- |
| XPF-04 | Pending | ✅ Verified (backend-only requirement, fully covered) |
| XPF-06 | Pending | Backend done / Frontend pending (root-cause investigation complete and independently confirmed; frontend fix + frontend regression test remain `ShapeUp-Web` work) |
| XPF-01, XPF-02, XPF-03, XPF-05 | Pending | Unchanged — Pending (100% [Frontend], not reviewed in this repo) |

---

## Summary

**Overall**: ✅ Ready (backend scope)

**Root-cause claim**: Independently confirmed. Both `GamificationProfile.TotalXp` and `.Level` writes (across the 2 consumers that touch them — `GamificationWorkoutFinishedConsumer` and `GamificationNutritionGoalMetConsumer`) are atomic (same tracked entity, single `SaveChangesAsync`), the read path (`GetGamificationProfileHandler`) has zero caching and maps both fields verbatim from one row, no server-side "in-level XP" field exists anywhere, and an exhaustive grep found no other write site to either field in `src/`. No divergence path exists. The zeroed progress bar's root cause is frontend-only, as design.md concludes.

**Spec-anchored check**: 2/2 in-scope backend ACs matched (XPF-04 fully, XPF-06 backend half fully) with direct `file:line` evidence, independently re-derived rather than trusted from design.md's citations. One cosmetic citation error found in design.md (wrong file path/line numbers for the `GamificationNutritionGoalMetConsumer.cs` evidence) — does not affect the substantive claim, noted for hygiene only, not a blocking gap.

**Sensor**: 1/1 mutation killed. The new test has real teeth — it uses `LevelCalculator.CalculateFromTotalXp` as an oracle rather than a hardcoded duplicate, so it guards against more than just the one literal-`1` mutation tried.

**Gate**: 384 passed, 0 failed, 0 skipped (post-commit), matching the claimed baseline delta (383→384) exactly.

**What works**: The backend contract for XPF-04 already satisfied the spec before this commit; the commit's sole contribution — one regression test — correctly locks in that invariant at the exact seam (`GetGamificationProfileHandler`'s response mapping) most likely to break it in a future change, using the calculator as an oracle so it's resilient to more than the one mutation tested.

**Issues found**: none blocking. One documentation-only nit in the working-tree `design.md` (incorrect file path/line citation for `GamificationNutritionGoalMetConsumer.cs`'s evidence — cites `GamificationWorkoutFinishedConsumer.cs:38,51` where it should read `GamificationNutritionGoalMetConsumer.cs:44,51`). Does not affect the correctness of the underlying claim, which was independently re-verified against the real file.

**Next steps**: None for backend — XPF-04 and the backend half of XPF-06 are verification-complete. XPF-01/02/03/05 and the frontend half of XPF-06 are `ShapeUp-Web` work, out of reach from this repo, per design.md's Cross-repo handoff section.

**Lessons distillation note**: no `scripts/lessons.py`/`.specs/lessons.json` infrastructure exists in this repo (confirmed absent, consistent with the prior feature's validation report). One signal worth carrying forward informally: design.md's file:line citations should be re-verified by direct read even when the surrounding argument is sound — one of four citations in this feature's investigation pointed at the wrong file/lines despite the underlying claim being correct.
