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

- [ ] T05.1 Implement `CodexActivityMonitor.cs` under `TokenHound.Infrastructure/Providers/Codex/` implementing `IActivityMonitor`.
- [ ] T05.2 Implement file write time checking against configurable 8-second threshold (`TimeSpan.FromSeconds(8)`).
- [ ] T05.3 Allow injecting base path / time provider for deterministic testing.
- [ ] T05.4 Return `AgentSession` with `AgentSessionState.Busy` if file written <= 8s ago; otherwise `Idle` (or null if no files found).
- [ ] T05.5 Implement `CodexActivityMonitorTests.cs` verifying busy vs idle transitions and missing file safety.

## Acceptance criteria

- `ProviderId` is `"codex"`.
- Reports `Busy` when rollout file modified within last 8 seconds.
- Reports `Idle` when rollout file older than 8 seconds.
- Handles missing directories safely without throwing.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexActivityMonitorTests*"`

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

None.
