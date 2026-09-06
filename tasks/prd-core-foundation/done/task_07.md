# Stable execution context

Load in this exact order:

1. `tasks/prd-core-foundation/prd.md`
2. `tasks/prd-core-foundation/techspec.md`
3. This file

---

# T07 — Infrastructure ProcessLiveness

## Outcome

Implements `ProcessLiveness` in `TokenHound.Infrastructure/System/` to check whether an assistant process is running, comparing both OS PID existence and `StartTimeUtc` to prevent misattribution caused by aggressive Windows PID recycling.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: PRD 02 (Claude Code session liveness)
- In scope: Process query logic, timestamp comparison, and unit tests using the current process and simulated recycled PID timestamps.
- Out of scope: WMI process discovery or network socket inspection.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-10 | `prd.md#functional-requirements` | ProcessLiveness PID + start time recycling |
| DEC-08 | `techspec.md#technical-decisions` | PID presence + StartTimeUtc verification |
| CMP-07 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/System/ProcessLiveness.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Spec: `docs/specs/01-READING-STRATEGY-RESILIENCE.md`, `docs/specs/03-PROVIDER-CLAUDE-CODE.md`

## Work

- [ ] T07.1 Implement `ProcessLiveness.cs` under `TokenHound.Infrastructure/System/` with `IsProcessAlive(int pid, DateTimeOffset? expectedStartTimeUtc)`.
- [ ] T07.2 Catch `ArgumentException` and `InvalidOperationException` when querying terminated processes and return false.
- [ ] T07.3 Implement tolerance threshold (e.g. 1 second) for start time comparison across system clock resolutions.
- [ ] T07.4 Implement `ProcessLivenessTests.cs` testing current process PID (true), non-existent PID (false), and mismatched start time (false).

## Acceptance criteria

- Returns `true` when PID exists and start timestamp matches within tolerance.
- Returns `false` when PID does not exist or start timestamp indicates a recycled PID.
- Never throws unhandled exceptions on exited or inaccessible processes.

## Verification

- Unit: Current process PID returns true; mismatched timestamp on current PID returns false.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*ProcessLivenessTests*"`
- Expected evidence: MTP test execution validates PID recycling detection.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/System/ProcessLiveness.cs`
  - `tests/TokenHound.Infrastructure.Tests/System/ProcessLivenessTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Adjust timestamp tolerance if platform timer precision differs.

## Handoff

- Produced result: Implemented `ProcessLiveness` in `TokenHound.Infrastructure.System` to check OS process liveness and match start timestamps against expected UTC values within a tolerance (default 1.0s), preventing misattribution caused by aggressive Windows PID recycling. Handled non-existent, terminated, and inaccessible processes safely by catching `ArgumentException`, `InvalidOperationException`, and `Win32Exception` without throwing unhandled exceptions. Provided helper `GetProcessStartTimeUtc(int pid)` returning UTC `DateTimeOffset?`. Implemented comprehensive unit tests in `ProcessLivenessTests` covering active current PID matching, mismatched start time, non-existent PID, zero/negative PID, null start time (existence-only check), custom tolerance boundaries, and invalid PID handling in `GetProcessStartTimeUtc`.
- Changed files:
  - `src/TokenHound.Infrastructure/System/ProcessLiveness.cs`
  - `tests/TokenHound.Infrastructure.Tests/System/ProcessLivenessTests.cs`
  - `tasks/prd-core-foundation/task_07.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (exit code: 0, 0 errors)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProcessLivenessTests*"` (exit code: 0, 13 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (exit code: 0, 52 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (exit code: 0, 34 passed, 0 failed, 0 skipped)
- Validated state: All acceptance criteria met; returns true for valid PID and matching start time within tolerance; returns false for non-existent PID, negative/zero PID, or mismatched start time exceeding tolerance; never throws on terminated or inaccessible processes; C# architecture and style invariants in `AGENTS.md` strictly verified (sealed static class, file-scoped namespaces, alphabetized usings, XML doc comments on all public members, files <= 300 lines, methods <= 30 lines, nesting <= 3 levels, blank line formatting).
- Open items: None.

### ADR candidates

None.
