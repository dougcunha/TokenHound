# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T05 — Central UsageStore Engine Coordinator

## Outcome

Implements `UsageStore` in `TokenHound.Infrastructure/Engine/` to manage the central polling schedule (60s active vs 300s idle), coordinate registered `IUsageProvider` and `IActivityMonitor` instances, and raise snapshot update events.

## Dependencies and boundaries

- Depends on: T03, T04
- Unblocks: T08
- In scope: Implementation of `UsageStore`, thread-safe snapshot storage, timer-based polling dispatch, manual refresh support, and unit tests.
- Out of scope: Disk persistence across restarts or UI bindings.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-05 | `prd.md#functional-requirements` | Central UsageStore polling coordination |
| DEC-05 | `techspec.md#technical-decisions` | UsageStore coordinating providers |
| CMP-05 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Engine/UsageStore.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Policies: `src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs`, `RateLimitPolicy.cs`
- Contracts: `src/TokenHound.Core/Contracts/IUsageProvider.cs`

## Work

- [ ] T05.1 Implement `UsageStore.cs` under `TokenHound.Infrastructure/Engine/`.
- [ ] T05.2 Support registering multiple `IUsageProvider` and `IActivityMonitor` instances.
- [ ] T05.3 Implement periodic tick evaluating `RefreshSchedulePolicy.ShouldRefresh`.
- [ ] T05.4 Expose `event EventHandler<Snapshot>? SnapshotUpdated` and `IReadOnlyDictionary<string, Snapshot> CurrentSnapshots`.
- [ ] T05.5 Expose `RefreshNowAsync` for manual immediate refresh.
- [ ] T05.6 Implement `UsageStoreTests.cs` verifying snapshot caching and event propagation.

## Acceptance criteria

- Thread-safe coordination across concurrent callers.
- Honors `RateLimitPolicy.CanDispatch` before polling providers.
- Fires `SnapshotUpdated` event whenever a new snapshot is fetched.

## Verification

- Unit: Register mock provider, call `RefreshNowAsync`, and verify event received with updated snapshot.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*UsageStoreTests*"`
- Expected evidence: MTP test suite passes.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/Engine/UsageStore.cs`
  - `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert engine changes if polling timing logic diverges.

## Handoff

- Produced result: Implemented central coordinator `UsageStore` in `TokenHound.Infrastructure.Engine` implementing `IDisposable`. Supports registering multiple `IUsageProvider` and `IActivityMonitor` instances, thread-safe snapshot storage with `IReadOnlyDictionary<string, Snapshot> CurrentSnapshots`, `SnapshotUpdated` event broadcast, periodic cadence evaluation via `RefreshSchedulePolicy.ShouldRefresh(isAnyBusy, timeSinceLastAttempt, idleInterval)`, rate-limit checking before polling via `RateLimitPolicy.CanDispatch`, manual immediate refresh with `RefreshNowAsync(cancellationToken)`, and clean periodic timer management (`Start`, `Stop`, `Dispose`). Implemented 9 unit tests in `UsageStoreTests` using `NSubstitute` verifying registration, manual refresh caching and event dispatch, skipping providers with future rate limit deadlines, polling when deadlines expire, busy monitor active refresh triggering, idle monitor cadence throttling, clean timer shutdown upon disposal, graceful handling of provider exceptions, and cancellation token propagation.
- Changed files:
  - `src/TokenHound.Infrastructure/Engine/UsageStore.cs`
  - `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs`
  - `tasks/prd-tracer-bullet-claude/task_05.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (Passed: 0 errors, 4 warnings)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*UsageStoreTests*"` (Passed: 9 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 129 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 34 passed, 0 failed, 0 skipped)
- Validated state: Full test suite passing across Core and Infrastructure (163 tests passing). Adheres strictly to AGENTS.md (sealed classes, file-scoped namespaces, <= 300 lines per file, <= 30 lines per method, <= 3 nesting levels, `.ConfigureAwait(false)`, CancellationToken propagation, thread safety via `ConcurrentDictionary` and `SemaphoreSlim`).
- Open items: None. Ready for downstream integration in T08 (`NotchViewModel`).

### ADR candidates

None. Implementation conforms strictly to TechSpec DEC-05 and CMP-05.
