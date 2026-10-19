# Initial nutrition goals

POST /api/nutrition/profile/onboarding keeps the existing anthropometric contract.
It calculates with TdeeCalculator using the latest registered weight, persists the
profile, macro goal and suggested water goal, and returns the resulting profile.

Optional fields added for mobile contextual setup:
- onlyIfMissing: false by default; true returns 409 when a goal already exists.
- expectedUserId: absent by default; a mismatch with the actor returns 403 before writes.

The client sends these options when queued initial setup is confirmed. A replay
cannot replace a manually set or professional goal or target a different account.
SQL Server initial creation uses a serializable transaction and UPDLOCK/HOLDLOCK
for the account row/range; unrelated existing clients retain their prior behavior.
No second calculator or new nutrition endpoint is introduced.
