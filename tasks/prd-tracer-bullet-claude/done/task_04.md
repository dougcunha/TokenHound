# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T04 — Claude Session Monitor (IActivityMonitor)

## Outcome

Implements `ClaudeSessionMonitor` in `TokenHound.Infrastructure/Providers/Claude/` implementing `IActivityMonitor`, scanning `%USERPROFILE%\.claude\sessions\*.json` and validating PID liveness with `ProcessLiveness`.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T05
- In scope: Implementation of `ClaudeSessionMonitor`, session JSON parsing, PID validation, and unit tests.
- Out of scope: Network requests or UI display.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-04 | `prd.md#functional-requirements` | Claude session monitor with PID liveness |
| DEC-04 | `techspec.md#technical-decisions` | ClaudeSessionMonitor using ProcessLiveness |
| CMP-04 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- System primitives: `src/TokenHound.Infrastructure/System/ProcessLiveness.cs`
- Contracts: `src/TokenHound.Core/Contracts/IActivityMonitor.cs`

## Work

- [ ] T04.1 Implement `ClaudeSessionMonitor.cs` implementing `IActivityMonitor`.
- [ ] T04.2 Locate and parse session JSON files under `%USERPROFILE%\.claude\sessions\`.
- [ ] T04.3 Validate PID existence and start time with `ProcessLiveness.IsProcessAlive`.
- [ ] T04.4 Return `AgentSession` reporting `Busy` or `Idle` state.
- [ ] T04.5 Implement `ClaudeSessionMonitorTests.cs` using temporary directories and mock sessions.

## Acceptance criteria

- `ProviderId` returns `"claude"`.
- Returns `AgentSession` with `State = Busy` when a session PID is currently running.
- Returns `null` or `State = Idle` when no active session process exists.

## Verification

- Unit: Test session detection with current process PID (Busy) and terminated PID (Idle).
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeSessionMonitorTests*"`
- Expected evidence: MTP test suite passes.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeSessionMonitorTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert session scanning logic if file schema changes.

## Handoff

- Produced result: Implemented `ClaudeSessionMonitor` implementing `IActivityMonitor` in `TokenHound.Infrastructure.Providers.Claude`. Scans `%USERPROFILE%\.claude\sessions\*.json` for session files using `SharedFileReader.ReadAllTextAsync`, parses JSON for PID and start time, validates process liveness via `ProcessLiveness.IsProcessAlive`, returns an `AgentSession` reporting `State = AgentSessionState.Busy` when alive, and returns `null` when no session or no active process exists. 18 MTP unit tests created in `ClaudeSessionMonitorTests` verifying live PID detection, dead PID handling, missing directory handling, malformed JSON skipping, start time tolerance and recycled PID checks, JSON property parsing, and cancellation token propagation.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeSessionMonitorTests.cs`
  - `tasks/prd-tracer-bullet-claude/task_04.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (Passed: 0 errors, 4 warnings)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeSessionMonitorTests*"` (Passed: 18 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 120 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 34 passed, 0 failed, 0 skipped)
- Validated state: .NET 10 (`net10.0`), all acceptance criteria satisfied, zero regressions across existing tests (120 Infrastructure + 34 Core = 154 tests passing). Adheres strictly to AGENTS.md (sealed classes, file-scoped namespaces, <= 300 lines per file, <= 30 lines per method, <= 3 nesting levels, `.ConfigureAwait(false)`, CancellationToken propagation).
- Open items: None. Ready for downstream integration in T05 (`UsageStore`).

### ADR candidates

None. Implementation conforms strictly to TechSpec DEC-04 and CMP-04.
