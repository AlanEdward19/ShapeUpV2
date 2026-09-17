# Lessons (project-local)

> No-script fallback: `scripts/lessons.py` is not present in this repo. Entries below are grounded in `.specs/features/exercise-variations/validation.md` only.

## Candidates

| ID | Feature | Signal | Source | Scope | Text |
| -- | ------- | ------ | ------ | ----- | ---- |
| L-001 | exercise-variations | ac_gap | EXVAR-02 | training/equivalents | When symmetry or other invariants live in a repository, assert them with a real (in-memory/integration) repo test — mocking the interface cannot prove bidirectional persistence. |
| L-002 | exercise-variations | surviving_mutant | ExerciseEquivalentRepository.cs:12-16 (mutant 3) | training/equivalents | Discrimination coverage must hit the code that owns the behavior; a surviving mutant on a mocked dependency means add a test at that dependency’s layer. |

## Confirmed

_(none yet — promote after recurrence across ≥2 distinct features)_
