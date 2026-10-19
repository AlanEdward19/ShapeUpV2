# Remove council verification (issue #28)

**Decision**: CREF/CRN approval stays manual. No council or vendor offers a lookup API (CFN/CONFEF have none; regional
CREFs sit behind bot protection; Infosimples/DirectD do not cover CRN/CREF).

## Requirements (EARS)
- REQ-1: WHEN a CREF/CRN credential is submitted or resubmitted, THE system SHALL send it to `UnderReview` via `ManualReviewCredentialVerifier`.
- REQ-2: THE system SHALL NOT contain the council HTTP client, circuit breaker, name matcher, council verifiers, verification audit or periodic recheck.
- REQ-3: THE database SHALL drop `CredentialVerificationAudits` and `ProfessionalCredentials.LastCouncilCheckAt` through a reversible migration.
- REQ-4: WHEN a user's data is erased, THE system SHALL still delete their credentials.
- REQ-5: `ICredentialVerifier` SHALL remain as the extension point for a future official source.

## Out of scope
Link to the council lookup in the admin panel: ShapeUp-Web#11.
