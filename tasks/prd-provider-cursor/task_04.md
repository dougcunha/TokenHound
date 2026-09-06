# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-cursor/prd.md`
2. `tasks/prd-provider-cursor/techspec.md`
3. This file

---

# T04 — Cursor Usage Provider Adapter

## Outcome

Implements `CursorUsageProvider` fulfilling `IUsageProvider` (`ProviderId => "cursor"`), coordinating session discovery, API telemetry fetching, and domain quota mapping with Zero Fake Data.

## Work

- [x] T04.1 Implement `CursorUsageProvider.cs` under `TokenHound.Infrastructure/Providers/Cursor/` implementing `IUsageProvider`.
- [x] T04.2 Query auth via `CursorSessionDiscovery`; return `ProviderStatus.NeedsAuth` if credentials missing.
- [x] T04.3 Query API via `CursorApiClient` and map `individualUsage.plan.totalPercentUsed` to primary window.
- [x] T04.4 Map secondary API window if `apiPercentUsed > 0` and optional OnDemand window.
- [x] T04.5 Implement `CursorUsageProviderTests.cs` covering normal usage, free account bonus handling, and NeedsAuth.

## Acceptance criteria

- `ProviderId` is `"cursor"`.
- Maps `totalPercentUsed` / 100.0 without calculating `used / limit` on bonus pools.
- Returns NeedsAuth when `state.vscdb` is missing or unauthenticated.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorUsageProviderTests*"`

## Handoff

- Produced result: Implemented `CursorUsageProvider` fulfilling `IUsageProvider` with `ProviderId => "cursor"`, mapping `totalPercentUsed` directly without flawed bonus pool division, supporting optional API credits and OnDemand windows, and handling missing auth / stale API states.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageResponse.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorApiClient.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageProvider.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorUsageProviderTests.cs`
- Checks: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorUsageProviderTests*"` (4 tests passed, 0 warnings, exit code 0).
- Validated state: 4 tests passed covering unauthenticated, normal allowance, free tier bonus pools, and API error stale handling.
- Open items: None.

### ADR candidates

None.
