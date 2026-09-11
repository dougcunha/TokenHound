# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/codereview_1/codereview.md`
2. This file

---

# T07 — Resolve OpenCode activity contract

## Outcome

The OpenCode activity contract is explicit and internally consistent: either the documented SQLite activity signal is implemented read-only, or the provider specification and TechSpec are revised before implementation to require process-only semantics.

## Classification

- Actionable: `codereview_1/CR-03`.
- Decision resolved: caller approved retaining the provider-spec `opencode.db` recent-activity contract.

## Dependencies and boundaries

- Depends on: T04
- Unblocks: —
- In scope: Resolve the provider-spec/TechSpec scope conflict, then implement and test the selected activity semantics without changing credential ownership or performing E2E desktop validation.
- Out of scope: Quota transport/rate-limit corrections in T05/T06, WPF visual redesign, process injection, database writes, and live OpenCode account calls.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_1/CR-03` | `codereview.md#findings` | The monitor reports every present process as `Busy` and does not implement the provider specification's recent SQLite activity rule. |
| OBJ-05 | `prd.md#outcomes-and-metrics` | Real-time agent process liveness. |
| FR-06 | `prd.md#functional-requirements` | Monitor `opencode` and `OpenCode` activity. |
| DEC-04 | `techspec.md#technical-decisions` | Process discovery and activity-monitor contract. |
| CMP-06 | `techspec.md#components-and-flow` | `OpenCodeActivityMonitor`. |
| TC-05 | `techspec.md#test-approach` | Activity monitor liveness tests. |
| Provider spec sections 4-5 | `docs/specs/12-PROVIDER-OPENCODE.md` | Read-only database activity and process liveness semantics. |

## Requirements

- First record the HIL decision: retain the documented `opencode.db` activity/token signal, or explicitly narrow the provider specification and TechSpec to process presence.
- If SQLite activity remains in scope, use `SafeSqliteReader` and read-only WAL/immutable fallback behavior; never write or lock the database.
- If SQLite activity remains in scope, report `Busy` only for activity within the documented 60-second threshold, `Idle` for a live but stale process, and `null` when no process is live.
- If process-only semantics are selected, update the source documents and acceptance criteria so they no longer promise database activity, then test the process-only contract.
- Preserve PID/start-time validation and cancellation behavior.

## Context to recover on demand

- TechSpec: `techspec.md#technical-decisions`, `techspec.md#components-and-flow`, `techspec.md#test-approach`
- Rules/skills: `AGENTS.md` SQLite WAL read-only rules; `dotnet-efficient-validation`; desktop E2E omission policy
- Code: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeActivityMonitor.cs` — current process-only Busy behavior
- Code: `src/TokenHound.Infrastructure/Storage/SafeSqliteReader.cs` — permitted read-only WAL access if retained
- Spec: `docs/specs/12-PROVIDER-OPENCODE.md:130-184` — local telemetry and activity requirements

## Work

- [x] T07.1 Obtain and record the scope decision in the authoritative SDD source before changing implementation.
- [x] T07.2 Implement the selected activity contract with read-only access and safe process handling, or revise the specification/TechSpec for an explicitly process-only contract.
- [x] T07.3 Add deterministic unit/integration tests for absent, recent, stale, and cancellation cases applicable to the selected contract.

## Acceptance criteria

- The authoritative source documents and implementation describe the same activity contract; no SQLite obligation remains silently unimplemented.
- A live process is not reported as `Busy` solely because it exists when the retained contract requires recent database activity.
- Any retained SQLite access uses `SafeSqliteReader` read-only behavior and does not modify OpenCode files.
- Required unit/integration tests pass; E2E remains omitted under desktop .NET policy.

## Verification

- Unit: Test process absence, process presence, recent activity, stale activity, invalid PID/start time, and cancellation for the selected contract.
- Integration: If SQLite remains in scope, use a temporary WAL database and verify read-only/fallback behavior; otherwise verify the revised process-only source contract and monitor tests.
- E2E: Omitted by desktop .NET policy.
- Manual: Windows HUD acceptance is not required by the TechSpec; if requested after the scope decision, it must be executed separately and recorded as manual evidence.
- Environment dependency: HIL decision resolved by the caller; SQLite validation used a Windows-compatible temporary SQLite test setup.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*OpenCodeActivityMonitorTests*" --minimum-expected-tests 1`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: Recorded scope decision, updated source/implementation alignment, deterministic activity tests, and full infrastructure regression output.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeActivityMonitor.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeActivityMonitorTests.cs`
- Modify: `docs/specs/12-PROVIDER-OPENCODE.md` and/or `tasks/prd-provider-opencode/techspec.md` if the scope decision narrows the contract
- Create: A focused OpenCode SQLite activity reader only if the HIL decision retains database telemetry

## Observability and recovery

- Operational signal: Preserve structured activity-state transitions and diagnostics for read-only database failures.
- Recovery: Revert the monitor/reader/tests as a unit, or revert only the source narrowing if the HIL decision changes before implementation.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: CR-03 implemented under the retained provider-spec contract. OpenCode now validates a live process, reads the latest `session.time_updated` through `SafeSqliteReader`, reports `Busy` only within 60 seconds, `Idle` for stale/unavailable activity, and null when no validated process is live.
- Changed files: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeActivityMonitor.cs`; `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeActivityReader.cs`; `src/TokenHound.Infrastructure/System/ProcessDiscovery.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeActivityMonitorTests.cs`; `tasks/prd-provider-opencode/techspec.md`; this task's `## Handoff` only.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` passed (3 projects, 0 errors, 0 warnings). Native MTP focused command with the exact class filter `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "TokenHound.Infrastructure.Tests.Providers.OpenCode.OpenCodeActivityMonitorTests"` passed (13 tests). The task's wildcard discovery form returned zero tests/exit 5; the fully qualified class filter was used and verified. Full infrastructure native MTP passed (672 tests); Core native MTP passed (89 tests); `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` passed (4 projects, 0 errors, 0 warnings); `rtk git diff --check` passed.
- Manual checks: No live OpenCode credential/network call or HUD/E2E desktop check was run. Deterministic temporary SQLite tests exercised WAL reading and immutable fallback after removing `-shm`; E2E remains omitted under desktop .NET policy.
- Validated state: `.NET SDK 10.0.400`, `net10.0`, native Microsoft.Testing.Platform from `global.json`, current worktree after the CR-03 implementation; `codereview_1` and T07 source context were read once at the current version.
- HIL decision: RETAIN the provider-spec `opencode.db` recent-activity contract; TechSpec reconciled without narrowing it.
- Open items: Independent correction review remains pending; task remains at `tasks/prd-provider-opencode/task_07.md` and E2E/manual HUD validation is intentionally unexecuted.
