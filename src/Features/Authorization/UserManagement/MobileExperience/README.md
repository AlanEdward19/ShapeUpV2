## Mobile experience
+
+`POST /api/users/me/mobile-experience` accepts only android/ios, resolves the
+actor from Firebase context and records `Users.FirstMobileSeenAtUtc` using
+one conditional SQL UPDATE. Concurrent/repeated calls cannot both claim first
+use. Existing accounts are backfilled only by migration AddMobileExperience.
+Missing/inactive users return 403; invalid platform 400; absent session 401.
+The per-account policy permits 20 requests/minute, then 429. This metadata grants
+no capability. Migration source is delivered without applying it to databases.
+
## Verification
+
+MobileExperienceTests covers validation, repeats, inactive accounts and migration.
+MobileExperienceEndpointTests covers HTTP statuses and a real concurrent SQL UPDATE
+using a disposable test database. No development database is changed by these tests.
+Apply the migration with the normal deployment process before enabling the API.
+The mobile client tolerates an unavailable endpoint and does not block login.
+