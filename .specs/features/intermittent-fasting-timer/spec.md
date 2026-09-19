# Intermittent Fasting Timer (API) — Specification

> **Canonical backend.** Product rules for agenda, override, validation, auth, and the clock snapshot live here (`ShapeUpV2`, aka ShapeUpApi). Sister spec: `ShapeUp-Web/.specs/features/intermittent-fasting-timer/spec.md` (UI, countdown tick, disclaimer, diary banner). Nutrition already exists (`/api/nutrition/*`, self via `HttpContext.GetUserId()`). There is no `nutritionist` role — P2 uses `professional` + existing professional–client relationship. Order: **API first**, then Web consumes `GET /api/nutrition/fasting`.

## Problem Statement

Athletes have no persisted fasting agenda or off-schedule session. The Web timer cannot be localStorage-only: reload must restore the same window. Foodvisor-style fixed daily times fail when the athlete starts a fast now. The API must store one daily agenda, at most one active override, and return a **clock snapshot** so the client does not reimplement DST or lazy expiry.

## Goals

- [ ] Authenticated owner can PUT an agenda (preset protocol + eating-window start + IANA timezone) and GET a clock snapshot derived from that agenda
- [ ] Authenticated owner can Start an override; End-early and Cancel apply only to that override; GET then returns agenda-derived clock again
- [ ] Duplicate Start while an override is Fasting or Eating is rejected (409)
- [ ] Flag `nutrition.intermittent-fasting` off → fasting routes 404 with a stable error code (data not deleted)
- [ ] Professional can PUT a protocol recommendation on a linked client (P2)

## Out of Scope

| Feature | Reason |
| --- | --- |
| HTTP countdown / WebSocket ticks | Client ticks from `boundaryAt`. |
| Disclaimer copy, Jejum tab, browser notifications | Web spec. |
| Blocking diary writes during a fast | P2 Web banner only; diary endpoints unchanged. |
| Medical eligibility | Not an API concern. |
| Multi-day / >24h protocols | Daily IF; hours sum to 24. |
| Weekday vs weekend agendas | One agenda per user. |
| Push/email at boundaries | No new notification channel. |
| New role or gym-admin fasting dashboard | Existing auth only. |
| Gamification XP for completing a fast | Out of this feature. |
| ShapeUp-Web UI | Sister spec. |

---

## Assumptions & Open Questions

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --- | --- | --- | --- |
| Product audience / clock model | Same as Web discuss 2026-09-19: athlete P1; agenda auto + Start override current cycle only; professional recommendation P2. | User confirmed on Web spec; API is the store for that model. | y |
| Module | New vertical slice `Features/Nutrition/Fasting` under existing Nutrition, routes `/api/nutrition/fasting*`. | Nutrition is the bounded context for eating behavior (AD-012/013). | y |
| Persistence | Agenda = current-state document/row per user (replace on PUT). Override = at most one **active** document (`Fasting` or `Eating`). Completed/cancelled overrides kept for P3 history. | Avoid 365 agenda-day rows. | y |
| Clock snapshot | `GET /api/nutrition/fasting` computes `clock.status` (`Idle` \| `Fasting` \| `Eating`) and `clock.boundaryAt` (UTC) from agenda + `now` when no active override; if override `eatEndsAt` ≤ now, treat override as completed (lazy) then derive from agenda. | Web must not own DST. | y |
| Agenda fields | `protocol` one of `14:10`, `16:8`, `18:6`, `20:4` in P1; `eatingStartMinutes` 0–1410 inclusive, multiple of 30; `timeZone` IANA string from client. Fast hours / eat hours derived (`16:8` → 16 and 8). | Matches Web agenda input. | y |
| Override on Start | Requires saved agenda (protocol known). `startedAt` = server UTC now; `fastEndsAt` = startedAt + fast hours; then Eating until `eatEndsAt` = fastEndsAt + eat hours. End-early: status Eating, `eatEndsAt` = now + eat hours. Cancel: status Cancelled, no longer active. | User confirmed override semantics. | y |
| Agenda PUT during override | Allowed; does not mutate override timestamps. | Confirmed on Web spec. | y |
| Auth P1 | Same as diary/profile: caller `userId` is the owner. No clientId on P1 routes. | Nutrition self-access. | y |
| Auth P2 | PUT recommendation for `clientUserId` allowed only if the caller has an active professional–client relationship to that user (same source Training already uses). Athletes cannot write another user's agenda/override. | No new capability string required for P1; P2 reuses relationship. | y |
| Feature flag | Key `nutrition.intermittent-fasting` via existing `IFeatureFlagReader`. Seed **enabled = true**. WHEN disabled, every fasting endpoint SHALL return 404 with error code `nutrition.fasting.disabled` and SHALL NOT delete stored agenda/override. | Reuse PlatformFeatureFlags. | n |
| Input validation | Invalid protocol, eating start not on 30-min grid, unknown IANA zone → 400 naming the field. Custom hours 12–23 integer P2 only; P1 rejects unknown protocol. | Bounds dimension. | y |
| Failure | Handler failures use existing Result → ActionResult mapping. No partial override+agenda write in one command (separate endpoints). | Nutrition pattern. | y |
| Idempotency | Start has no client id in P1. Second Start while active → 409, existing override unchanged. PUT agenda is replace (last write wins). End-early/Cancel on missing active override → 409. | Duplicate Start dimension. | y |
| Concurrency | Last successful write wins. No optimistic concurrency token in P1. | Simple IF data. | y |
| Data lifecycle | Agenda replace-only. Active override lazy-completes on GET/Start. P3 lists last 14 completed/cancelled, retain 90 days (purge job or filter-by-date; Design chooses). Account deletion removes fasting data with the user (same as other nutrition data). | History not P1. | y |
| Observability | N/A — no new metric required. Existing request logging is enough. | Dimension N/A. | y |
| External deps | None. IANA TZ from BCL/`TimeZoneInfo`. | No Foodvisor. | y |
| Rate limits | N/A — authenticated nutrition volume, no public clock. | Dimension N/A. | y |
| Diary | AddDiaryEntry SHALL NOT read or write fasting state. | Web P2 warning is client-side from GET clock. | y |
| HTTP shapes | JSON camelCase like other nutrition DTOs. `clock.source` = `Agenda` \| `Override`. `Idle` only when no agenda and no active override (or flag on but empty). | Client mapping. | y |
| Remaining dimensions | Covered. | Complex sweep done. | y |

**Open questions:** none — all resolved or logged above.

---

## User Stories

### P1: Persist daily agenda ⭐ MVP

**User Story**: As an athlete, I want my protocol and eating-window start stored so every device sees the same rhythm.

**Why P1**: Without PUT, the Web clock cannot persist.

**Acceptance Criteria**:

1. WHEN the owner PUTs `/api/nutrition/fasting/agenda` with `protocol` `16:8`, `eatingStartMinutes` `720`, and a valid IANA `timeZone` THEN the system SHALL store those values for that user and SHALL return 200 with the saved agenda
2. WHEN GET clock runs after that PUT with no override and local time 11:00 in `timeZone` THEN `clock.status` SHALL be `Fasting` and `clock.boundaryAt` SHALL be today's 12:00 in that zone as UTC
3. WHEN GET clock runs with local time 12:00 THEN `clock.status` SHALL be `Eating` and `clock.boundaryAt` SHALL be today's 20:00 in that zone as UTC
4. IF `protocol` is not a P1 preset OR `eatingStartMinutes` is not a multiple of 30 in `0..1410` OR `timeZone` is not a valid IANA id THEN the system SHALL return 400 and SHALL name the invalid field
5. WHEN eating start is `0` (`00:00`) with `16:8` THEN derived eating SHALL be 00:00–08:00 in `timeZone`

**Independent Test**: PUT 16:8 / 720 / `America/Sao_Paulo`; GET at mocked 11:00 BRT → Fasting until 12:00 BRT.

---

### P1: Clock snapshot GET ⭐ MVP

**User Story**: As the Web client, I want one GET that tells me Fasting vs Eating and when the boundary is, including after sleep.

**Why P1**: Single source of truth; lazy expiry of overrides.

**Acceptance Criteria**:

1. WHEN the owner has no agenda and no active override THEN GET `/api/nutrition/fasting` SHALL return 200 with `agenda` null, `override` null, and `clock.status` `Idle`
2. WHEN an override has `eatEndsAt` in the past THEN GET SHALL treat it as completed (not active) and SHALL return agenda-derived `clock` if an agenda exists
3. WHEN an active override exists THEN GET `clock.status` and `clock.boundaryAt` SHALL come from the override (`fastEndsAt` if Fasting, `eatEndsAt` if Eating) and `clock.source` SHALL be `Override`
4. The system SHALL return override timestamps as UTC instants
5. IF the caller is not authenticated THEN the system SHALL return 401

**Independent Test**: Start override; GET; `source` Override. Advance clock past `eatEndsAt`; GET; `source` Agenda.

---

### P1: Start override ⭐ MVP

**User Story**: As an athlete, I want Start now so today's agenda times do not trap me.

**Why P1**: Confirmed clock model.

**Acceptance Criteria**:

1. WHEN the owner POSTs `/api/nutrition/fasting/override/start` with a saved `16:8` agenda THEN the system SHALL create an active override with `status` `Fasting`, `fastEndsAt` = server now plus 16 hours, and SHALL return 201 with that override
2. IF Start is called with no saved agenda THEN the system SHALL return 400 naming agenda/protocol as missing
3. IF Start is called while an override is `Fasting` or `Eating` THEN the system SHALL return 409 and SHALL leave the existing override unchanged
4. WHEN the owner PUTs a new agenda during an active override THEN the system SHALL save the agenda and SHALL not change override `fastEndsAt` / `eatEndsAt`

**Independent Test**: Agenda eat 12:00; Start at 19:00 → fastEndsAt ≈ now+16h, not 20:00.

---

### P1: End-early and cancel ⭐ MVP

**User Story**: As an athlete, I want to end the fasting leg early or abort the override without deleting my agenda.

**Why P1**: Completes the override state machine.

**Acceptance Criteria**:

1. WHEN the owner POSTs end-early while override `status` is `Fasting` THEN the system SHALL set `status` `Eating` and `eatEndsAt` = server now plus protocol eat hours
2. WHEN the owner POSTs cancel while an override is `Fasting` or `Eating` THEN the system SHALL mark the override `Cancelled` (not active) and SHALL leave the agenda intact
3. IF end-early or cancel is called with no active override THEN the system SHALL return 409
4. IF end-early is called while override `status` is already `Eating` THEN the system SHALL return 409 and SHALL not change `eatEndsAt`

**Independent Test**: Start → cancel → GET clock from agenda. Start → end-early → status Eating.

---

### P1: Feature flag ⭐ MVP

**User Story**: As a platform admin, I want to hide fasting without wiping data.

**Why P1**: Flags already exist; Web needs a hard off switch.

**Acceptance Criteria**:

1. WHERE `nutrition.intermittent-fasting` is disabled, WHEN any `/api/nutrition/fasting*` endpoint is called THEN the system SHALL return 404 with error code `nutrition.fasting.disabled`
2. WHERE the flag is disabled THEN stored agenda and overrides SHALL remain
3. The system SHALL seed `nutrition.intermittent-fasting` as enabled so local/dev works without a manual toggle

**Independent Test**: Toggle off → GET fasting 404; toggle on → GET 200 with prior agenda.

---

### P2: Professional recommendation

**User Story**: As a professional, I want to store a protocol recommendation on a client.

**Why P2**: Coaching wedge; not required for the timer demo.

**Acceptance Criteria**:

1. WHEN a professional with an active relationship PUTs a P1 protocol on that client THEN the system SHALL store it as `recommendation` on the client and SHALL NOT create an override
2. WHEN the client GETs clock THEN `recommendation.protocol` SHALL be present when set
3. IF the caller has no relationship to the client THEN the system SHALL return 403
4. IF the client PUTs their own agenda THEN the system SHALL persist that agenda even when it differs from `recommendation`

**Independent Test**: Pro sets 18:6; client GET shows recommendation 18:6; client PUT 16:8 agenda succeeds.

---

### P2: Custom protocol hours

**User Story**: As an athlete, I want fast hours 12–23 stored as a protocol.

**Why P2**: Presets cover P1.

**Acceptance Criteria**:

1. WHEN the owner PUTs agenda with custom `fastHours` 12–23 inclusive (integer) and omits a named preset THEN the system SHALL set eat hours to `24 - fastHours` and SHALL save
2. IF `fastHours` is outside 12–23 or not an integer THEN the system SHALL return 400 naming `fastHours`

**Independent Test**: PUT fastHours 15 → eat 9. PUT 8 → 400.

---

### P3: Override history

**User Story**: As an athlete, I want recent completed or cancelled overrides.

**Why P3**: Clock works without history.

**Acceptance Criteria**:

1. WHEN the owner GETs `/api/nutrition/fasting/history` THEN the system SHALL return up to 14 most recent completed or cancelled overrides (newest first) with start instant, protocol, outcome, and fasting duration
2. IF there are none THEN the system SHALL return 200 with an empty list

---

## Edge Cases

- IF DST skips the saved local eating start THEN `boundaryAt` SHALL use the zone's next valid local time
- IF GET and Start race THEN at most one active override remains; the 409 rule holds for the second Start
- IF flag turns off mid-override THEN 404 on routes; data remains for when the flag turns on
- WHEN remaining time is computed, `boundaryAt` SHALL still be an absolute UTC instant (client formats `hh:mm:ss`)

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| -------------- | ----- | ----- | ------ |
| IFTA-01 | P1: Persist daily agenda | Tasks | Implementing |
| IFTA-02 | P1: Clock snapshot GET | Tasks | Implementing |
| IFTA-03 | P1: Start override | Tasks | Pending |
| IFTA-04 | P1: End-early and cancel | Tasks | Pending |
| IFTA-05 | P1: Feature flag | Tasks | Implementing |
| IFTA-06 | P2: Professional recommendation | Tasks | Pending |
| IFTA-07 | P2: Custom protocol hours | Tasks | Pending |
| IFTA-08 | P3: Override history | Tasks | Pending |

**ID format:** `IFTA-NN` (API). Web uses `IFTW-NN`.

**Coverage:** 8 total, 0 mapped to tasks, 8 unmapped

---

## Success Criteria

- [ ] PUT agenda + GET clock matches Fasting/Eating for a mocked local time without an override
- [ ] Start → GET `source` Override → cancel → GET `source` Agenda
- [ ] Second Start → 409
- [ ] Flag off → 404 `nutrition.fasting.disabled`; data still there after flag on
- [ ] Unauthenticated → 401
