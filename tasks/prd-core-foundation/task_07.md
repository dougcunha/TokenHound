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

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
