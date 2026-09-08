# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-02-cadence-and-retries/prd.md`
2. `tasks/prd-settings-02-cadence-and-retries/techspec.md`
3. This file

---

# T02 - Reconfigure polling cadence without interrupting refreshes

## Outcome

The running usage store accepts valid active and idle intervals, uses them for its next schedule decision, and replaces only future timer waits without canceling an in-flight provider refresh.

## Dependencies and boundaries

- Depends on: -
- Unblocks: T04
- In scope: `UsageStore.UpdateCadence`, lifecycle-safe periodic scheduling, cadence safety clamping, structured operational logging, and deterministic Infrastructure tests.
- Out of scope: persistence/UI, activity-monitor cadence, provider registration, snapshot cache reset, and failure-count or deadline clearing.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-05 | `prd.md#outcomes-and-metrics` | Apply the timer and idle cadence live |
| FR-09, FR-10 | `prd.md#functional-requirements` | No restart and no interruption of ongoing operations |
| NFR-02, NFR-05 | `prd.md#non-functional-requirements` | Safe floors, no UI blocking, no timer deadlock |
| DEC-04, CMP-05 | `techspec.md#technical-decisions`, `techspec.md#components-and-flow` | UsageStore cadence extension |
| TC-04 | `techspec.md#test-approach` | Live timer/idle update without disrupting work |

## Context to recover on demand

- Applicable skills: `no-workarounds`, `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `src/TokenHound.Infrastructure/Engine/UsageStore.cs` - idle cadence is currently readonly and timer ticks pass their cancellation token into refresh work.
- Existing code: `src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs` - `StartTimer` currently cancels the old token without awaiting its loop.
- Contract or integration: `techspec.md#usagestore-cadence-extension` and `techspec.md#errors-security-and-recovery`.

## Work

- [x] T02.1 Define one synchronization boundary for cadence reads/updates so `TickAsync` observes a complete active/idle pair and public updates reject stopping/disposed stores.
- [x] T02.2 Change timer replacement semantics to stop future waits while allowing an admitted refresh to complete; do not introduce blind delays, detached work, or broad exception swallowing.
- [x] T02.3 Add `UpdateCadence` with 30-second active clamping and `idle >= active` clamping, preserving snapshots, failure state, and registered providers; emit the specified structured cadence log at the Infrastructure boundary.
- [x] T02.4 Add deterministic Infrastructure tests that coordinate a running tick and verify the replacement schedule, changed idle threshold, clamp behavior, and non-interruption of in-flight work.

## Acceptance criteria

- Calling `UpdateCadence(60s, 120s)` on a running store changes future tick cadence and idle eligibility without recreating the store.
- Values below 30 seconds or an idle interval below active resolve safely; the update neither clears caches/deadlines nor cancels a refresh already admitted.
- Timer replacement and shutdown remain deadlock-free and observable through an Information log with typed active and idle arguments.

## Verification

- Unit: `UsageStoreCadenceTests` coordinates timer/tick completion without arbitrary delays and proves update, clamps, and lifecycle behavior.
- Integration: Exercise the real `UsageStore`/`UsageStoreLifetime` contract with controlled providers and time coordination; a timer fake alone is insufficient.
- E2E: Omitted by .NET desktop policy.
- Manual: MAN-02 later inspects real application logs after changing Active to 35 seconds; owner: implementer.
- Commands: `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreCadenceTests*"`
- Environment dependency: .NET SDK 10.0.400 and restored Infrastructure test output.
- Expected evidence: MTP reports the selected test count, all pass, and no uncaught lifecycle/cancellation diagnostic is hidden.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.cs`
- Modify: `src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreCadenceTests.cs`

## Observability and recovery

- Operational signal: `Information` log `Polling cadence updated: active {ActiveInterval}, idle {IdleInterval}`.
- Recovery: Apply factory default intervals through the same update API; no store recreation or cache clearing is permitted.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result:
  - Added `UsageStore.UpdateCadence(TimeSpan activeInterval, TimeSpan idleInterval)` with synchronized reads/writes via `_cadenceLock`, clamping active intervals `< 30s` to 30s and idle intervals `< safeActive` to `safeActive`, and rejecting disposed/stopping stores with `ObjectDisposedException`.
  - Added public read-only properties `ActiveInterval` and `IdleInterval` to `UsageStore` with XML documentation.
  - Repaired `UsageStoreLifetime.StartTimer` and timer replacement semantics: `StartTimer` now cancels future waits of the prior timer loop (`oldCts.Cancel()`) while admitting ticks using `_stoppingCts.Token` so in-flight refreshes are not canceled on replacement, chained running tasks via `Task.WhenAll` to eliminate detached unobserved work, and ensured safe CTS cleanup on loop completion.
  - Emitted structured Serilog `Information` log `Polling cadence updated: active {ActiveInterval}, idle {IdleInterval}` on cadence updates.
  - Created 11 deterministic unit/integration tests in `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreCadenceTests.cs` verifying clamping, rejection on stopping/disposed, dynamic idle threshold eligibility, running state preservation, and non-interruption of in-flight refreshes.
- Changed files:
  - `src/TokenHound.Infrastructure/Engine/UsageStore.cs` (modified, 289 lines)
  - `src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs` (modified, 297 lines)
  - `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreCadenceTests.cs` (created, 267 lines)
  - `tasks/prd-settings-02-cadence-and-retries/task_02.md` (updated checklist and handoff)
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo --verbosity:minimal` -> 0 errors, 0 warnings.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreCadenceTests*"` -> 11 passed, 0 failed, 0 skipped in 750ms.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` -> 514 passed, 0 failed, 0 skipped in 2s 356ms.
  - `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore -c Release -- --minimum-expected-tests 1` -> 89 passed, 0 failed, 0 skipped in 624ms.
- Validated state: Clean build on .NET SDK 10.0.400, Release configuration, all 514 infrastructure tests passing, zero warnings, all files <= 300 lines, methods <= 30 lines, nesting <= 3 levels.
- Open items:
  - Downstream task T04 will connect `CadenceSettingsViewModel` to `UsageStore.UpdateCadence`.

### ADR candidates

None - direct TechSpec DEC-04 implementation repairing the UsageStoreLifetime lifecycle contract for timer replacement.
