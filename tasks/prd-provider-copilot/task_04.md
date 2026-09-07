# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-copilot/prd.md`
2. `tasks/prd-provider-copilot/techspec.md`
3. This file

Use the approved PRD and TechSpec versions. T02 is the completed dependency before execution.

---

# T04 - Implement Copilot activity and host detection

## Outcome

Copilot activity is reported Busy only when recent read-only session/log writes and a qualifying live host coexist, with 120 ms burst debounce and deterministic Idle recovery.

## Dependencies and boundaries

- Depends on: T02
- Unblocks: T05
- In scope: CopilotActivityMonitor, CopilotProcessHostDetector, read-only file timestamp checks, watcher debounce, process/extension detection, and focused tests.
- Out of scope: quota HTTP calls, credential parsing, UsageStore timer/event plumbing owned by T02, WPF code, and reading or writing activity file contents.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-09, FR-10 | `prd.md#functional-requirements` | Two-second activity observation, 30-second freshness, host requirement, debounce, and no file mutation. |
| NFR-01, NFR-05, NFR-06 | `prd.md#non-functional-requirements` | Shared read access, specified timing, and Infrastructure-only process/file code. |
| AC-07 | `prd.md#acceptance-criteria` | Recent/old/absent signal combinations and burst writes. |
| DEC-01, DEC-07, DEC-08 | `techspec.md#technical-decisions` | Activity boundary, host liveness, and existing event plumbing. |
| CMP-05, CMP-06 | `techspec.md#components-and-flow` | Activity monitor and host detector components. |
| TC-08, TC-09 | `techspec.md#test-approach` | Activity and polling evidence. |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`.
- Existing code: representative activity monitors under `src/TokenHound.Infrastructure/Providers`, `ProcessLiveness`, and `SharedFileReader`.
- Contract or integration: TechSpec sections `Activity integration`, `Activity event`, and `Errors, security, and recovery`.

## Work

- [ ] T04.1 Implement read-only timestamp inspection over `.copilot/session-state/*/events.jsonl` and `.copilot/logs/` with shared file access and a 30-second freshness threshold.
- [ ] T04.2 Implement `CopilotProcessHostDetector` for `copilot`, `gh`, and `Code.exe` only when a supported GitHub Copilot extension directory exists; validate process liveness and avoid process-content writes.
- [ ] T04.3 Implement `FileSystemWatcher` session-state observation with 120 ms debounce, safe disposal, and no UI work in watcher callbacks.
- [ ] T04.4 Return Busy only for recent write plus qualifying host; return Idle/null on either missing signal, stale data, access failure, or abrupt host exit.
- [ ] T04.5 Add temporary-file, injected-detector, clock, burst-write, unsupported-extension, and disposal tests without modifying fixture timestamps/content.

## Acceptance criteria

- Recent write without live qualifying host is Idle; live host without recent write is Idle; both together are Busy.
- After 30 seconds without a qualifying write the state becomes Idle, and a process exit cannot leave Busy latched.
- Burst notifications within 120 ms produce one consolidated update and watcher disposal releases resources.
- No activity file contents, timestamps, or metadata are changed by the monitor.
- Code.exe qualifies only when the Copilot extension is found in supported locations; discovery failures produce Idle rather than a false Busy.

## Verification

- Unit: monitor state matrix, freshness boundary, debounce, extension discovery, process liveness, access failure, and disposal tests.
- Integration: temporary session/log directory fixtures and injected host detector; no real Copilot process is required.
- E2E: omitted by .NET desktop policy.
- Manual: Activity transition is exercised through T06 after App integration.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CopilotActivity*"`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CopilotProcess*"`.
- Environment dependency: .NET SDK 10.0.400, native MTP runner, deterministic TimeProvider/clock, and temporary directories.
- Expected evidence: non-zero activity/host test counts, unchanged fixture metadata, and no leaked watcher/process handles.

## Affected files

- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotActivityMonitor.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotProcessHostDetector.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotActivityMonitorTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotProcessHostDetectorTests.cs`

## Observability and recovery

- Operational signal: ActivityUpdated receives Busy/Idle state; no file contents or user paths beyond necessary diagnostics are logged.
- Recovery: stop and dispose the monitor if watcher/process access fails. Do not repair, touch, truncate, or delete Copilot session/log files.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution (code/diff, configuration, projects, and environment).
- Open items: Pending execution.

### ADR candidates

Pending execution. `sdd-execute-task` replaces this text with structured candidates or `None - direct TechSpec implementation or local decision`.
