# Stable execution context

Load in this order: [prd.md](prd.md), [techspec.md](techspec.md), then this file. Reuse unchanged sources already read. Consult [tasks.md](tasks.md) for authoritative dependencies/state.

# T01: Make refresh and terminal shutdown safe

## Outcome

UsageStore safely drains admitted work and retains honest provider state across failures, with focused regression tests.

## Dependencies and boundaries

- Depends on: None.
- Unblocks: T04.
- In scope: Engine admission, linked cancellation, terminal StopAsync, deferred resource disposal, stale snapshot provenance, and provider-local timeout isolation.
- Out of scope: App wiring, dialogs, provider redesign, durable storage (P01), new retry policies.
- Implementation authorization: planning does not authorize execution; see manifest state.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| PRD and TechSpec IDs | PRD requirements/stories/outcomes; TechSpec decisions/components/test approach | FR-02 through FR-04, NFR-04/NFR-05; DEC-03/DEC-04; CMP-09/CMP-10; TC-02/TC-04/TC-08/TC-10. |

## Context to recover on demand

- Applicable skills: sdd-execute-task for execution; repository-cli-efficiency before searches/diffs; dotnet-efficient-validation and MTP reference before .NET validation; no-workarounds for lifecycle/error fixes. Follow applicable UI skills when implementing visuals.
- Existing code: recover only the affected files below and their immediate callers/tests. Preserve unrelated worktree changes.
- Contracts: TechSpec Contracts and data, Interfaces/errors/recovery, and Test approach are authoritative. Keep Core free of WPF/OS dependencies; borrowed credentials are read-only.
- Validation: manifest V01/V02 defines environment and build prerequisites; no task changes runner or package versions.

## Work

- [x] T01.1 Implement the TechSpec engine lifetime contract: close admission atomically, cancel admitted work, drain timer/holders/waiters, and release resources only after completion. Preserve restartable Stop/Start behavior and IDisposable compatibility.
- [x] T01.2 Preserve existing manual-refresh serialization and rate checks. Distinguish caller/store cancellation from a provider-local timeout; continue to later providers on the latter.
- [x] T01.3 Retain previous windows, fidelity, fetched timestamp, and applicable block state in engine-generated stale snapshots. Preserve unknown fractions.
- [x] T01.4 Add deterministic gate-based tests for concurrent stop/dispose, waiting refreshes, timer/startup-equivalent calls, failure isolation, unchanged snapshots, and stale provenance. Inspect actual registered providers' cancellation propagation; record an identified violation without silently expanding adapter scope.

## Acceptance criteria

- No semaphore is disposed while a holder or waiter can still use it; terminal stop rejects new work and concurrent callers observe completion.
- A provider failure or local timeout does not stop other eligible providers; caller cancellation does.
- Existing rate-limit and schedule checks pass; no credential writes or fake measurements are introduced.

## Verification

- Unit: UsageStoreTests, UsageStoreLifecycleTests, covering the scenarios above.
- Integration: preserve real boundaries; P01 owns durable storage design/evidence. Fakes establish coordination only, not provider or filesystem semantics.
- E2E: omitted by desktop .NET policy, including local full-application automation.
- Manual: No window launch is needed for this engine delivery. T05 owns real process/focus acceptance; test doubles do not prove provider I/O cancellation.
- Environment dependency: Windows/.NET 10.0.400-compatible SDK and matching Release outputs; see V01/V02. No new external-service authority is inferred.
- Expected evidence: commands and exit codes, executed/failed/skipped counts when tests run, build/source revision, and named manual results. Listing/zero tests is not a pass.

After V01/V02 prerequisites:

```powershell
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreLifecycleTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

## Affected files

Modify `src/TokenHound.Infrastructure/Engine/UsageStore.cs` and `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs`. Create `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreLifecycleTests.cs`. If required by the 300-line limit, extract `src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs` as the lifecycle helper permitted by CMP-09.

## Observability and recovery

- Operational signal: Snapshot status and exceptions preserve failure/cancellation semantics.
- Recovery: revert only this delivery's changes using normal version control after assessing dependent tasks; never delete credentials or provider state. Invalidate evidence only for affected source/build changes.
- Re-entry: inspect current task state and existing files before creating anything; preserve IDs, handoffs, and completed work. Report collisions rather than overwrite them.

## Handoff

> Updated by sdd-execute-task during implementation.

- Produced result: UsageStore safely drains admitted work, implements terminal StopAsync with admission closing and deferred resource disposal, preserves restartable timer Stop/Start and IDisposable compatibility, isolates provider-local timeouts from aborting subsequent providers, and retains sample provenance (fidelity, timestamp, limit windows with null fractions, active block) in engine-generated stale snapshots. Extracted UsageStoreLifetime helper to maintain files <= 300 lines. All registered providers verified for cancellation propagation.
- Changed files:
  - `src/TokenHound.Infrastructure/Engine/UsageStore.cs`
  - `src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs`
  - `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreLifecycleTests.cs`
  - `tasks/prd-main-window-context-menu/task_01.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal` (exit 0)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreTests*"` (exit 0, 11 passed, 0 failed, 529ms)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreLifecycleTests*"` (exit 0, 8 passed, 0 failed, 531ms)
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` (exit 0, 252 passed, 0 failed, 1s 621ms)
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal` (exit 0)
- Validated state: All 19 UsageStore unit tests pass deterministically. File length invariant satisfied across all modified/created files (all <= 300 lines). Full Infrastructure test suite passes (252 tests).
- Open items: P01 / GAP-02 remains open for durable rate-limit persistence (TC-09).

### ADR candidates

None - direct TechSpec implementation (DEC-03, DEC-04, CMP-09).

