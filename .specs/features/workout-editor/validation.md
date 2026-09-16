# Workout Editor — Independent Verification Report

**Verifier:** TLC spec-driven, fresh pass (author ≠ verifier)  
**Verified at:** 2026-09-15 ~19:30 local  
**Api HEAD:** `280cd31` (ShapeScore unit gate fix)  
**Web HEAD (read-only):** `95fb58c` (PlanEditor `stitch=false` default)  
**Prior FAIL gaps addressed:** full `UnitTests` 347/347; native `BlockCard`/`SetRow` default path; i18n keys re-counted  
**Stitch:** `ShapeUp-Web/src/stitch/Builder.jsx` not moved (opt-in via `stitch=true` only)

## Verdict: **PASS**

All mandatory gates green. P1 acceptance criteria satisfied on API + **native default** editor path (`PlanEditor` `stitch=false`). Discrimination sensor 5/5 killed. **Zero ranked blocking gaps.** Manual Firebase browser UAT remains **deferred** (env); does not fail this pass.

---

## 1. Tasks T1–T22

All tasks **T1–T22** (incl. **T7b**, **T14b**) marked **✅ Complete** in `tasks.md` (Phase 5: 22/22, T22 `905575e`).

**Verifier:** T22 “done when” full unit suite — **satisfied** (`347/347`).

---

## 2. Mandatory gates (this run)

| Gate | Command | Result |
|------|---------|--------|
| Build | `dotnet build src/ShapeUp.csproj` | **PASS** — 0 errors |
| Unit (full) | `dotnet test tests/UnitTests/UnitTests.csproj` | **PASS** — **347/347** |
| Unit (WOED) | `--filter "FullyQualifiedName~WorkoutPlan\|FullyQualifiedName~WorkoutTemplate"` | **PASS** — 35/35 |
| Integration (full) | `dotnet test tests/IntegrationTests/IntegrationTests.csproj` | **PASS** — 281 passed, 0 failed, 7 skipped (~11m 26s) |
| Integration (WOED) | `--filter "FullyQualifiedName~WorkoutPlans\|FullyQualifiedName~WorkoutTemplates"` | **PASS** — 38/38 (~1m 42s) |
| Web lint | `npm --prefix ShapeUp-Web run lint` | **PASS** — 0 errors, 2 pre-existing warnings |
| Web build | `npm --prefix ShapeUp-Web run build` | **PASS** |

---

## 3. P1 acceptance criteria (spec-anchored)

**Default UI path:** `ClientDetail.jsx:108` `stitch = false`; `:265` Stitch only when `stitch` true; `:305-312` renders `BlockCard` list. Call sites omit `stitch` → native path: `TrainingPlansIndependent.jsx:993`, `TrainingPlansProfessional.jsx:264-268`, `ClientDetail.jsx:1342` (PlanEditor usage).

### Superset

| AC | Outcome | Evidence | Status |
|----|---------|----------|--------|
| 1 | Persist Superset + order | `CreateWorkoutPlanHandlerTests.cs:171-192`; `WorkoutPlansEndpointsIntegrationTests.cs:215-235` | **PASS** |
| 2 | Reject &lt;2 ex, explicit msg | `CreateWorkoutPlanCommandValidator.cs:23-26`; unit `:154-166`; integration `:238-250` | **PASS** |
| 3 | Reopen: grouped vs Straight | `BlockCard.jsx:32` (`data-block-type={block.type}`, `su-block-card`); exercises in block container `:78-88` | **PASS** (native path) |
| 4 | Reject RestSeconds in Superset | Validator `:40-42`; unit `:305-320`; integration `:330-346` | **PASS** |

### AMRAP

| AC | Outcome | Evidence | Status |
|----|---------|----------|--------|
| 1 | Persist Amrap + TimeCap | `CreateWorkoutPlanHandlerTests.cs:211-227`; integration `:268-287` | **PASS** |
| 2 | Reject bad/missing TimeCap | Validator `:27-31`; unit `:196-208`; integration `:253-265` | **PASS** |
| 3 | Blank Reps OK | Unit `:220-228`; integration `:277-287` | **PASS** |
| 4 | Reject RestSeconds | Same RestSeconds cross-block rule | **PASS** |

### EMOM

| AC | Outcome | Evidence | Status |
|----|---------|----------|--------|
| 1 | Persist Emom + fields | `CreateWorkoutPlanHandlerTests.cs:279-298`; integration `:306-327` | **PASS** |
| 2 | Reject bad/missing interval/rounds | Validator `:32-39`; unit `:232-276`; integration `:291-303` | **PASS** |
| 3 | Rotation order in UI | API order: unit `:290-297`, integration `:325-326`; UI: `BlockCard.jsx:79-87` maps `block.exercises` in array order; `ExerciseRow.jsx:25` shows `index + 1` | **PASS** (native path) |
| 4 | Reject RestSeconds | RestSeconds rule | **PASS** |

### Intensity RPE \| RIR

| AC | Outcome | Evidence | Status |
|----|---------|----------|--------|
| 1 | RPE only on set | Payload: `ClientDetail.jsx:62-63` single `intensity` object | **PASS** |
| 2 | RIR persisted | `CreateWorkoutPlanHandlerTests.cs:342-360` | **PASS** |
| 3 | Toggle clears other | `SetRow.jsx:11-15` | **PASS** (native path) |
| 4 | Null intensity OK | Validator `:59`; unit `:322-338`; integration `:349-363` | **PASS** |

### Migration P1

Removed in `spec.md` — **N/A**.

### Success criteria / UAT

| Item | Evidence | Status |
|------|----------|--------|
| Mixed blocks round-trip | `WorkoutPlansEndpointsIntegrationTests.cs:371` `Create_MixedBlockTypes_RoundTripsWithoutDataLoss` | **PASS** (API) |
| No dual RPE+RIR | Single `Intensity` DTO + `SetRow` exclusive toggle | **PASS** |
| i18n parity | 11 WOED keys × 3 locales — 33 occurrences in `LanguageContext.jsx` (`pro.builder.block.*`, `pro.builder.intensity.toggle`) | **PASS** |
| Browser UAT | Firebase env blocked per brief | **DEFERRED** (non-blocking) |

---

## 4. Discrimination sensor (≥5 mutants, restored)

Target: `CreateWorkoutPlanCommandValidator.cs`  
Kill signal: `dotnet test tests/UnitTests/UnitTests.csproj --filter FullyQualifiedName~CreateWorkoutPlanHandlerTests`

| ID | Mutation | Result |
|----|----------|--------|
| M2 | Superset `>= 2` → `>= 0` | **KILLED** (1 failed) |
| M4 | Drop Emom `IntervalSeconds` `.NotNull()` | **KILLED** (1 failed) |
| M5 | Intensity `InclusiveBetween(1,10)` → `(0,10)` | **KILLED** (1 failed) |
| M6 | Drop Amrap `TimeCapSeconds` `.NotNull()` | **KILLED** (1 failed) |
| M7 | RestSeconds cross-block `.Must` → always `true` | **KILLED** (1 failed) |

**Score: 5/5 KILLED.** Validator restored (`git diff` clean on validator).

---

## 5. AD-007 spot-check

| Rule | Evidence | Status |
|------|----------|--------|
| Block only on planning | `StartWorkoutExecutionHandler.cs:52-53` flattens `plan.Blocks` → flat `Exercises` on execution document | **PASS** |
| Intensity exclusive Rpe\|Rir | `CreateWorkoutPlanCommandValidator.cs:59`; planning/execution Intensity VOs | **PASS** |

---

## 6. Ranked gaps (blocking)

*None.*

### Non-blocking notes

- **Manual browser success criteria** — not run (Firebase); covered at API layer by mixed-block integration test.
- **Stitch Builder** — `stitch=true` still flattens blocks (`Builder.jsx`); intentionally opt-in until stitch-migration T19; not required for WOED UI ACs on default path.

---

## 7. Commands executed (this pass)

```text
git log -3 --oneline  # Api + Web
dotnet build src/ShapeUp.csproj
dotnet test tests/UnitTests/UnitTests.csproj
dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~WorkoutPlan|FullyQualifiedName~WorkoutTemplate"
dotnet test tests/IntegrationTests/IntegrationTests.csproj
dotnet test tests/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~WorkoutPlans|FullyQualifiedName~WorkoutTemplates"
npm --prefix ShapeUp-Web run lint
npm --prefix ShapeUp-Web run build
# Validator scratch mutants M2,M4,M5,M6,M7 → restored
```

*End of report.*
