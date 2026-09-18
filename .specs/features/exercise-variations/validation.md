# exercise-variations T10 Validation — PASS ✅

**Date**: 2026-09-18
**Spec**: `.specs/features/exercise-variations/spec.md`
**Diff surface**: commits `569208f` and `940650d`, inspected separately; intermediate Docker commit `22bf8ec` excluded
**Verifier**: independent sub-agent (author ≠ verifier)
**Scope**: T10 only, specifically `spec.md:133`: preserve the orphan `ExerciseEquivalents` row after both exercises are deleted, and omit missing peers from reads.

**Overall**: ✅ Ready. The implementation has no EF/database foreign keys or cascades for either endpoint. The strengthened test deletes both exercises, proves the orphan row remains, and proves the read omits the absent peer. Both directed cascade mutants are killed.

---

## Task Completion

| Task | Status | Notes |
| ---- | ------ | ----- |
| T10 Preserve orphan equivalence rows without SQL Server cascade paths | ✅ Verified | Unit gate and build pass; evidence-or-zero passes; sensor kills 2/2 mutants |

---

## Change Evidence

| Expected change | Evidence | Result |
| --------------- | -------- | ------ |
| Entity exposes IDs only, with no relationship navigations | `src/Features/Training/Shared/Entities/ExerciseEquivalent.cs:3` — entity contains only `ExerciseId`, `EquivalentExerciseId`, and `CreatedAtUtc` through line 7 | ✅ |
| EF model preserves composite key and lookup index without relationships | `src/Features/Training/Infrastructure/Data/TrainingDbContext.cs:76` — configuration through line 80 contains only `HasKey(...)` and `HasIndex(...)` | ✅ |
| Migration creates the table without database FKs or cascades | `src/Features/Training/Infrastructure/Data/Migrations/20260917113659_AddExerciseEquivalents.cs:22` — constraints contain only `table.PrimaryKey(...)` through line 25 | ✅ |
| Snapshot preserves the FK-free model | `src/Features/Training/Infrastructure/Data/Migrations/TrainingDbContextModelSnapshot.cs:119` — entity metadata through line 135 contains properties, key, index, and table only | ✅ |
| Read resolves only peers still present in the exercise catalog | `src/Features/Training/Infrastructure/Repositories/ExerciseEquivalentRepository.cs:21` — query filters `Exercises` with `otherIds.Contains(x.Id)` through line 27 | ✅ |

---

## Spec-Anchored Acceptance Criteria

| Criterion | Spec-defined outcome | `file:line` + assertion | Result |
| --------- | -------------------- | ----------------------- | ------ |
| WHEN both equivalent exercises are deleted THEN the relation remains orphaned with no automatic cleanup (`spec.md:133`) | No FK/cascade exists; both catalog rows are deleted; the equivalence row remains | `tests/UnitTests/Domains/Training/Exercises/ExerciseEquivalentRepositoryTests.cs:81` — `Assert.Empty(...GetForeignKeys())`; `:84` — `RemoveRange(await db.Exercises.ToListAsync())`; `:87` — `Assert.Single(db.ExerciseEquivalents)` | ✅ PASS |
| WHEN a relation points to a missing peer THEN reads omit that peer | The result for the deleted exercise is empty, not an error or unavailable placeholder | `tests/UnitTests/Domains/Training/Exercises/ExerciseEquivalentRepositoryTests.cs:88` — `Assert.Empty(await sut.GetEquivalentsAsync(1, CancellationToken.None))` | ✅ PASS |

**Status**: ✅ 2/2 scoped outcomes match the spec; 0 coverage gaps; 0 spec-precision gaps.

### Evidence-or-zero conclusion

The test explicitly asserts all three required states: zero model FKs, deletion of both catalog exercises, and survival of exactly one orphan relation row. Its read assertion independently verifies that an absent peer is omitted.

---

## Discrimination Sensor

Each mutation ran in a separate temporary worktree detached at `940650d`. The real working tree was never mutated.

| Mutation | Injection point | Description | Killed? |
| -------- | --------------- | ----------- | ------- |
| 1 | `src/Features/Training/Infrastructure/Data/TrainingDbContext.cs:79` | Restored `HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade)` | ✅ Killed: targeted test failed at `ExerciseEquivalentRepositoryTests.cs:81`; model exposed required cascade FK on `ExerciseId` |
| 2 | `src/Features/Training/Infrastructure/Data/TrainingDbContext.cs:79` | Restored `HasForeignKey(x => x.EquivalentExerciseId).OnDelete(DeleteBehavior.Cascade)` | ✅ Killed: targeted test failed at `ExerciseEquivalentRepositoryTests.cs:81`; model exposed required cascade FK on `EquivalentExerciseId` |

- **Command**: `dotnet test tests/UnitTests/UnitTests.csproj --filter FullyQualifiedName~DeleteExercise_LeavesOrphanRowAndGetOmitsMissingPeer_EXVAR01`
- **Sensor depth**: lightweight, 2 directed behavior/model mutations covering both relationship endpoints
- **Result**: 2/2 killed — PASS ✅
- **Real-tree status before**: ` M .specs/features/exercise-variations/validation.md`
- **Real-tree status after cleanup**: ` M .specs/features/exercise-variations/validation.md`
- **Isolation**: byte-for-byte identical porcelain output before and after sensor cleanup

---

## Gate Check

- **Unit command**: `dotnet test tests/UnitTests/UnitTests.csproj --no-restore`
- **Unit result**: 459 passed, 0 failed, 0 skipped
- **Build command**: `dotnet build src/ShapeUp.slnx --no-restore`
- **Build result**: succeeded, 0 errors, 15 warnings
- **Warnings**: known dependency vulnerabilities and pre-existing compiler warnings; none is introduced by T10
- **Test count before T10**: 458, from the prior feature gate baseline
- **Test count after T10**: 459
- **Delta**: +1 test
- **SQL Server migration apply**: not rerun in this re-verification; the requested gates were unit + build. Static migration/model/snapshot evidence is consistent.

---

## Code Quality

| Principle | Status |
| --------- | ------ |
| Minimum code | ✅ Removes only relationships/cascades while retaining key and lookup index |
| Surgical changes | ✅ `569208f` changes the T10 persistence surface; `940650d` only strengthens its test |
| No scope creep | ✅ Docker commit `22bf8ec` excluded from review |
| Matches repository patterns | ✅ EF Core configuration and xUnit/InMemory test match existing code |
| Spec-anchored outcome check | ✅ Assertions target the exact orphan-row outcome from `spec.md:133` |
| Per-layer coverage expectation | ✅ Model shape plus repository behavior covered for the scoped edge case |
| Every scoped test maps to a spec criterion | ✅ Repository test maps directly to EXVAR-01 edge case |
| Documented guidelines followed: `src/AGENTS.md` | ✅ |

---

## Requirement Traceability

| Requirement | Previous T10 status | New T10 status |
| ----------- | ------------------- | -------------- |
| EXVAR-01 edge case: both endpoints deleted, orphan preserved, missing peer omitted | ❌ Needs Fix | ✅ Verified |

Other exercise-variations requirements were not re-evaluated in this scoped pass.

---

## Summary

**Overall**: ✅ Ready

**Spec-anchored check**: 2/2 scoped outcomes matched; 0 gaps
**Gate**: 459 passed, 0 failed, 0 skipped; build succeeded with 0 errors
**Sensor**: 2 mutations injected, 2 killed, 0 survived

**What changed since the prior FAIL**: commit `940650d` added an explicit zero-FK assertion and deletes both exercises. The former surviving `ExerciseId` cascade mutant now fails, as does the corresponding `EquivalentExerciseId` mutant.

**Lessons**: clean PASS; no new lesson recorded.
