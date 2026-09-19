# Intermittent Fasting Timer (API) Context

**Gathered:** 2026-09-19
**Spec:** `.specs/features/intermittent-fasting-timer/spec.md`
**Sister:** `ShapeUp-Web/.specs/features/intermittent-fasting-timer/`
**Status:** Ready for design after spec confirmation

---

## Feature Boundary

Nutrition Fasting vertical slice: persist one daily agenda, at most one active override, GET clock snapshot (derived or override, lazy complete). Feature flag `nutrition.intermittent-fasting`. P2 recommendation via professional–client relationship. No UI, no diary mutation, no ticks.

---

## Implementation Decisions

### Audience and clock

- P1 owner self-serve. Agenda drives Fasting/Eating. Start override replaces the current cycle only.
- Professional recommendation is P2, not an override on the coach.

### Contract

- `GET /api/nutrition/fasting` is the snapshot Web binds to (`clock.status`, `clock.boundaryAt`, `clock.source`).
- PUT agenda, POST start / end-early / cancel.
- Flag off → 404 `nutrition.fasting.disabled`.

### Agent's Discretion

- SQL vs Mongo inside Nutrition (nutrition spec already deferred store per sub-domain to Design).
- Whether lazy-complete on GET writes a Completed row immediately or only filters active.
- Production default of the flag (seed enabled for local).

### Declined / Undiscussed

- Exact JSON property names beyond camelCase nutrition convention.
- History purge job vs filter-by-90-days.

---

## Specific References

- Web discuss 2026-09-19: both agenda and Start; override only current cycle.
- Existing: `NutritionProfileController` / `DiaryController` self `GetUserId()`; `IFeatureFlagReader`; professional–client relationship used by Training access.

---

## Deferred Ideas

- Same as Web: weekend templates, auto-end on diary, push, XP for fasting.
