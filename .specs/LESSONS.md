# LESSONS - auto-maintained by scripts/lessons.py

> Machine-owned. Do NOT hand-edit. Changes are overwritten on the next `lessons.py` write.
> Canonical state lives in `.specs/lessons.json`. Edit lessons only via the script.
> promote_threshold=2 distinct features · window_days=45 · quarantine_threshold=2

## Confirmed (load these at Specify/Design)

Corroborated across multiple features. Safe to apply as guidance.

_none_

## Candidates (under observation - do NOT load as guidance yet)

Seen once or not yet corroborated. Tracked, not trusted.

### L-001 - When a write stores a nested DTO that GET later exposes, assert the GET payload field not only the write-side column.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `nutrition-fasting` · harmful: 0
- features: intermittent-fasting-timer
- evidence: IFTA-06 AC2 (nutrition-fasting)
- last seen: 2026-09-19T16:55:55Z

### L-002 - When two fields on one row must stay independent, seed both then assert the untouched field after the write.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `nutrition-fasting` · harmful: 0
- features: intermittent-fasting-timer
- evidence: IFTA-06 AC4 (nutrition-fasting)
- last seen: 2026-09-19T16:55:55Z

### L-003 - A disable/404 guard is not enough: assert stored rows still exist after the flag is off.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `feature-flags` · harmful: 0
- features: intermittent-fasting-timer
- evidence: IFTA-05 AC2 (feature-flags)
- last seen: 2026-09-19T16:55:55Z

### L-004 - When the spec requires naming the invalid field, assert the error property or message not only the HTTP status.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `nutrition-fasting` · harmful: 0
- features: intermittent-fasting-timer
- evidence: IFTA-03 AC2 (nutrition-fasting)
- last seen: 2026-09-19T16:55:55Z

### L-005 - For list payloads, assert each required item field (protocol, outcome, duration) not only page size.
- signal: `ac_gap` · recurrence: 1 feature(s) · scope: `nutrition-fasting` · harmful: 0
- features: intermittent-fasting-timer
- evidence: IFTA-08 AC1 (nutrition-fasting)
- last seen: 2026-09-19T16:55:55Z

## Quarantined (failed when applied - ignore)

A confirmed lesson that recurred alongside failure. Kept for the maintainer to review.

_none_
