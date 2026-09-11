# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/prd.md`
2. `tasks/prd-provider-opencode/techspec.md`
3. This file

---

# T03 — OpenCode usage provider adapter and snapshot mapping

## Outcome

Implements `OpenCodeUsageProvider : IUsageProvider` (`ProviderId = "opencode"`), orchestrating credential discovery and API client calls to produce authoritative `Snapshot` instances with three `LimitWindow` items, rate-limit backoff enforcement via `RateLimitPolicy`, and graceful error degradation.

## Dependencies and boundaries

- Depends on: T01, T02
- Unblocks: T04
- In scope:
  - `OpenCodeUsageProvider` class implementing `IUsageProvider` and `IDisposable`.
  - Maps 3 windows: 5-Hour Rolling (`TimeSpan.FromHours(5)`), Weekly (`TimeSpan.FromDays(7)`), Monthly (`TimeSpan.FromDays(30)`).
  - Calculates `UsedFraction = Math.Clamp(percent / 100.0, 0.0, 1.0)`.
  - Sets `TotalUnits = 100` and `RemainingUnits = (long)Math.Max(0, Math.Round(100.0 - percent))`.
  - Transitions to `ProviderStatus.NeedsAuth` when no credentials found or HTTP 401.
  - Transitions to `ProviderStatus.AccessDenied` when HTTP 403 (`EntitlementError`).
  - Integrates with `RateLimitPolicy` on HTTP 429: sets `Snapshot.ActiveBlock` with `BlockedReason.RateLimitReached` and respects `Retry-After`.
  - Preserves last-known valid reading and reports `ProviderStatus.Stale` on network failures.
- Out of scope:
  - Process activity monitoring (handled in T04).
  - UI rendering / WPF controls.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01 | `prd.md#outcomes-and-metrics` | First-class `IUsageProvider` for OpenCode |
| OBJ-03 | `prd.md#outcomes-and-metrics` | Accurate 3-tier quota telemetry |
| OBJ-04 | `prd.md#outcomes-and-metrics` | Rate-limit block persistence & backoff |
| FR-04 | `prd.md#functional-requirements` | Multi-Window Parsing into `LimitWindow` |
| FR-05 | `prd.md#functional-requirements` | Rate-Limit & 429 Handling (`RateLimitPolicy`) |
| FR-07 | `prd.md#functional-requirements` | Status State Machine (`Ok`, `NeedsAuth`, `AccessDenied`, `RateLimitReached`, `Stale`) |
| NFR-02 | `prd.md#non-functional-requirements` | Zero Fake Data (`UsedFraction = percent / 100.0`) |
| CMP-05 | `techspec.md#components-and-flow` | `OpenCodeUsageProvider` |
| TC-04 | `techspec.md#test-approach` | Usage provider snapshot mapping unit tests |

## Context to recover on demand

- Existing reference: [CursorUsageProvider.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageProvider.cs)
- Contracts: [IUsageProvider.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Contracts/IUsageProvider.cs), [Snapshot.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/Snapshot.cs), [LimitWindow.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/LimitWindow.cs)

## Work

- [ ] T03.1 Create `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs` implementing `IUsageProvider`.
- [ ] T03.2 Implement `GetSnapshotAsync` with status resolution, 3-tier limit window mapping, and `RateLimitPolicy` deadline checks.
- [ ] T03.3 Create unit tests in `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeUsageProviderTests.cs` verifying snapshot generation, limit window properties, 429 block handling, unauthenticated transitions, and stale state on network failure.

## Acceptance criteria

- `ProviderId` returns `"opencode"`.
- Success snapshot returns `Status = ProviderStatus.Ok` and `Fidelity = Fidelity.Official`.
- Exactly 3 limit windows returned: `"5-Hour Rolling"`, `"Weekly"`, and `"Monthly"`.
- Window `UsedFraction` equals `Percent / 100.0` (clamped between 0.0 and 1.0).
- HTTP 429 triggers `ActiveBlock` and locks out polling until `BlockedUntilUtc`.
- Network failure with cached reading returns `Status = ProviderStatus.Stale`.

## Verification

- Unit: Test suite `OpenCodeUsageProviderTests` passes with 100% assertions.
- Integration: Verified against mock discovery and API client.
- E2E: Omitted by desktop .NET policy.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --filter-class "*OpenCodeUsageProviderTests*" --minimum-expected-tests 1`
- Environment dependency: None.

## Affected files

- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeUsageProviderTests.cs`

## Observability and recovery

- Operational signal: Structured logs for quota snapshots and rate-limit transitions.
- Recovery: Cache fallback guarantees continuous telemetry display during temporary outages.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
