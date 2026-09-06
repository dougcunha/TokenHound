# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-antigravity/prd.md`
2. `tasks/prd-provider-antigravity/techspec.md`
3. This file

---

# T04 — Antigravity Usage Provider Adapter

## Outcome

Implements `AntigravityUsageProvider` fulfilling `IUsageProvider` (`ProviderId => "gemini"`), coordinating multi-tier fallback between the local Language Server (Fidelity.Official) and local transcript turn counting (Fidelity.Derived).

## Work

- [x] T04.1 Implement `AntigravityUsageProvider.cs` under `TokenHound.Infrastructure/Providers/Antigravity/` implementing `IUsageProvider`.
- [x] T04.2 Coordinate discovery and client query against local Language Server.
- [x] T04.3 Fall back to `AntigravityTranscriptReader` when Language Server is unreachable.
- [x] T04.4 Ensure Fidelity is `.Official` when Language Server succeeds, and `.Derived` when transcript reader is used.
- [x] T04.5 Enforce Zero Fake Data: never populate non-null `UsedFraction` on derived transcript snapshots.
- [x] T04.6 Implement `AntigravityUsageProviderTests.cs` verifying waterfall stages and snapshot mappings.

## Acceptance criteria

- `ProviderId` is `"gemini"`.
- Returns Official snapshot with inverted consumed fraction when Language Server is online.
- Returns Derived snapshot with prompt count and null fraction when falling back to transcripts.
- Returns NeedsAuth when neither source produces telemetry.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityUsageProviderTests*"`

## Handoff

- Produced result: `AntigravityUsageProvider` coordinating Language Server RPC (Official fidelity) and local transcript prompt aggregation (Derived fidelity).
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityUsageProviderTests.cs`
- Checks: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*AntigravityUsageProviderTests*"` (4 tests passed, exit code 0).
- Validated state: Validated Official snapshot mapping with inverted fraction, Derived fallback with null fraction (Zero Fake Data), and NeedsAuth.
- Open items: None.

### ADR candidates

None.
