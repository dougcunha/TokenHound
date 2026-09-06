# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-cursor/prd.md`
2. `tasks/prd-provider-cursor/techspec.md`
3. This file

---

# T05 — Cursor Activity Monitor

## Outcome

Implements `CursorActivityMonitor` fulfilling `IActivityMonitor` (`ProviderId => "cursor"`), verifying active Composer runs in SQLite alongside Windows `Cursor.exe` process liveness.

## Work

- [x] T05.1 Implement `CursorActivityMonitor.cs` under `TokenHound.Infrastructure/Providers/Cursor/` implementing `IActivityMonitor`.
- [x] T05.2 Coordinate `CursorComposerReader` and `ProcessLiveness`.
- [x] T05.3 Return `AgentSessionState.Busy` if `unfinishedRunAt` is active, `Cursor.exe` is alive, and checkpoint is within 15 minutes.
- [x] T05.4 Return `AgentSessionState.Idle` otherwise (or null if Cursor is not running).
- [x] T05.5 Implement `CursorActivityMonitorTests.cs` verifying busy, idle, and waiting states.

## Acceptance criteria

- `ProviderId` is `"cursor"`.
- Classifies as `Busy` only when process is genuinely alive and run is active.
- Correctly identifies dead runs if process has exited.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorActivityMonitorTests*"`

## Handoff

- Produced result: Implemented `CursorActivityMonitor` fulfilling `IActivityMonitor` with `ProviderId => "cursor"`, evaluating Cursor process liveness via process enumeration/delegates and Composer run states with a 15-minute staleness threshold, supporting busy, waiting, and idle states.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorActivityMonitor.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorActivityMonitorTests.cs`
- Checks: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorActivityMonitorTests*"` (6 tests passed, 0 warnings, exit code 0).
- Validated state: 6 tests passed covering process not running, idle with no headers, active unfinished run, stale run, waiting approval, and cancellation.
- Open items: None.

### ADR candidates

None.
