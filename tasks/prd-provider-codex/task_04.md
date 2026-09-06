# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-codex/prd.md`
2. `tasks/prd-provider-codex/techspec.md`
3. This file

---

# T04 — Codex Usage Provider Adapter

## Outcome

Implements `CodexUsageProvider` fulfilling `IUsageProvider` (`ProviderId => "codex"`), coordinating two-stage fallback (AppServer JSON-RPC -> Rollout Log Tail -> Auth Status), and mapping primary (5h) and secondary (weekly) quota windows to `Snapshot`.

## Work

- [ ] T04.1 Implement `CodexUsageProvider.cs` under `TokenHound.Infrastructure/Providers/Codex/` implementing `IUsageProvider`.
- [ ] T04.2 Coordinate Stage 1 (`CodexAppServerClient`), Stage 2 (`CodexRolloutLogReader`), and auth check (`CodexAuthDiscovery`).
- [ ] T04.3 Map quotas: `UsedFraction = usedPercent / 100.0`, `TotalUnits = 100.0`, `RemainingUnits = 100.0 - usedPercent`, converting Unix second epochs to `DateTimeOffset`.
- [ ] T04.4 Handle `rateLimitReachedType` creating `UsageBlock` and setting `ProviderStatus.RateLimited`.
- [ ] T04.5 Return `ProviderStatus.NeedsAuth` when no auth tokens exist with actionable error description.
- [ ] T04.6 Implement `CodexUsageProviderTests.cs` covering AppServer success, Rollout fallback, RateLimited status, and NeedsAuth.

## Acceptance criteria

- `ProviderId` is `"codex"`.
- Uses AppServer as primary source; falls back to Rollout Log Reader when AppServer returns null.
- Correctly constructs `LimitWindow` records for primary and secondary quotas.
- Zero Fake Data: never invents unverified limits.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexUsageProviderTests*"`

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

None.
