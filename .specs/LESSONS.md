# Lessons (project-local)

> No-script fallback: `scripts/lessons.py` is not present in this repo (Python not available in this environment either). Entries below are grounded in `.specs/features/exercise-variations/validation.md` and `.specs/features/time-based-exercises/validation.md`.

## Candidates

| ID | Feature | Signal | Source | Scope | Text |
| -- | ------- | ------ | ------ | ----- | ---- |
| L-001 | exercise-variations | ac_gap | EXVAR-02 | training/equivalents | When symmetry or other invariants live in a repository, assert them with a real (in-memory/integration) repo test — mocking the interface cannot prove bidirectional persistence. |
| L-002 | exercise-variations | surviving_mutant | ExerciseEquivalentRepository.cs:12-16 (mutant 3) | training/equivalents | Discrimination coverage must hit the code that owns the behavior; a surviving mutant on a mocked dependency means add a test at that dependency’s layer. |
| L-003 | time-based-exercises | ac_gap | TBE-04 AC2 | training/workouts | When an AC asserts that existing type-agnostic logic (e.g. a gate or validator) must also cover a newly added variant, add a dedicated test combining the new variant with that logic — sharing an unbranched code path is not itself evidence the combination is asserted. |
| L-004 | time-based-exercises | ac_gap | TBE-02 AC4 | training/workouts | When an AC's requirement is "no new restriction applies to case X" (a negative/absence claim), write a positive test exercising X directly — the absence of new branching code is not itself proof nothing accidentally restricts it. |

## Confirmed

_(none yet — promote after recurrence across ≥2 distinct features)_
