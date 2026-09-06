# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-codex/prd.md`
2. `tasks/prd-provider-codex/techspec.md`
3. This file

---

# T05 — Codex Activity Monitor

## Outcome

Implements `CodexActivityMonitor` fulfilling `IActivityMonitor` (`ProviderId => "codex"`), detecting Codex agent activity by checking whether active rollout files or the desktop SQLite database were modified within an 8-second window.

## Work

- [x] T05.1 Implement `CodexActivityMonitor.cs` under `TokenHound.Infrastructure/Providers/Codex/` implementing `IActivityMonitor`.
- [x] T05.2 Implement file write time checking against configurable 8-second threshold (`TimeSpan.FromSeconds(8)`).
- [x] T05.3 Allow injecting base path / time provider for deterministic testing.
- [x] T05.4 Return `AgentSession` with `AgentSessionState.Busy` if file written <= 8s ago; otherwise `Idle` (or null if no files found).
- [x] T05.5 Implement `CodexActivityMonitorTests.cs` verifying busy vs idle transitions and missing file safety.

## Acceptance criteria

- `ProviderId` is `"codex"`.
- Reports `Busy` when rollout file modified within last 8 seconds.
- Reports `Idle` when rollout file older than 8 seconds.
- Handles missing directories safely without throwing.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexActivityMonitorTests*"`

## Handoff

- Produced result: Implemented Codex activity detection from active rollout files indexed by `state_5.sqlite`, with a filesystem fallback and desktop `codex-dev.db` write checks. Recent writes at or below the configurable eight-second threshold return `Busy`; older discovered files return `Idle`; no discovered files return null.
- Changed files: `src/TokenHound.Infrastructure/Providers/Codex/CodexActivityMonitor.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexActivityMonitorTests.cs`; this task handoff.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexActivityMonitorTests*"`; exact Codex class filters for T01-T04.
- Validated state: Build passed with 0 errors; focused activity tests passed 5 tests; the four prior Codex test classes passed 20 tests combined. Build reported the pre-existing NU1903 SQLitePCLRaw vulnerability warning. A broader `*Codex*Tests*` filter and unfiltered listing matched zero tests under this MTP runner, so exact class filters were used and each enforced the minimum expected count.
- Open items: None for T05.

### ADR candidates

None.
