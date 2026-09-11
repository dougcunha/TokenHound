# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/prd.md`
2. `tasks/prd-provider-opencode/techspec.md`
3. This file

---

# T04 — Process activity monitor and configuration registration

## Outcome

Implements `OpenCodeActivityMonitor : IActivityMonitor` to track running `opencode` (CLI) and `OpenCode` (Desktop) processes, registers the provider in `appsettings.json` defaults, and provides end-to-end wiring for TokenHound's orchestration engine.

## Dependencies and boundaries

- Depends on: T03
- Unblocks: —
- In scope:
  - `OpenCodeActivityMonitor` class implementing `IActivityMonitor` (`ProviderId = "opencode"`).
  - Inspects process table using `ProcessDiscovery` / `ProcessLiveness`.
  - Returns `AgentSession` with `AgentSessionState.Busy` or `AgentSessionState.Idle` based on process presence.
  - Adds default entry for OpenCode in `src/TokenHound.App/appsettings.json`.
- Out of scope:
  - Intercepting IPC or modifying OpenCode processes.
  - WPF presentation layer controls.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-05 | `prd.md#outcomes-and-metrics` | Real-time agent process liveness |
| FR-06 | `prd.md#functional-requirements` | Process Liveness Monitoring (`opencode` / `OpenCode`) |
| FR-08 | `prd.md#functional-requirements` | Configuration & Registration |
| CMP-06 | `techspec.md#components-and-flow` | `OpenCodeActivityMonitor` |
| CMP-07 | `techspec.md#components-and-flow` | `appsettings.json` registration |
| TC-05 | `techspec.md#test-approach` | Activity monitor liveness unit tests |

## Context to recover on demand

- Existing reference: [CursorActivityMonitor.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Providers/Cursor/CursorActivityMonitor.cs)
- Contracts: [IActivityMonitor.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Contracts/IActivityMonitor.cs), [AgentSession.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/AgentSession.cs)

## Work

- [ ] T04.1 Create `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeActivityMonitor.cs` implementing `IActivityMonitor`.
- [ ] T04.2 Create unit tests in `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeActivityMonitorTests.cs` validating liveness detection when processes are present vs absent.
- [ ] T04.3 Update `src/TokenHound.App/appsettings.json` to include `"OpenCode": { "Enabled": true }` in the default provider settings.
- [ ] T04.4 Verify the entire test suite passes across `TokenHound.Infrastructure.Tests`.

## Acceptance criteria

- `ProviderId` returns `"opencode"`.
- `CheckLivenessAsync` returns `null` when neither `opencode` nor `OpenCode` process is running.
- `CheckLivenessAsync` returns valid `AgentSession` with PID and process name when active.
- `appsettings.json` contains OpenCode entry without invalidating existing configuration schema.
- All test suites in `TokenHound.Infrastructure.Tests` pass.

## Verification

- Unit: Test suite `OpenCodeActivityMonitorTests` passes with 100% assertions.
- Integration: Full solution test suite passes with `--minimum-expected-tests 1`.
- E2E: Omitted by desktop .NET policy.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: None.

## Affected files

- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeActivityMonitor.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeActivityMonitorTests.cs`
- Modify: `src/TokenHound.App/appsettings.json`

## Observability and recovery

- Operational signal: Debug logs when process liveness status changes.
- Recovery: Process disappearance immediately reverts status to idle/null.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
