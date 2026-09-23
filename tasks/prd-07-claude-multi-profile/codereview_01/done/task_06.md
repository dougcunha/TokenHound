# T06 — Prove isolated session activity attribution

## Outcome

A live session in an isolated profile directory produces activity attributed to that profile's provider ID.

## Dependencies and boundaries

- Depends on: none.
- Unblocks: none.
- In scope: focused `ClaudeSessionMonitor` and `UsageStore` validation; code changes only if the positive path fails.
- Out of scope: alternate Claude session formats and UI animation styling.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_01/CR-03` | `codereview.md#findings` | TC-04 only tests an empty custom directory. |

## Requirements

- Place a live-process session JSON fixture inside an isolated profile's `sessions` directory.
- Verify the custom monitor reads that file and carries the matching provider ID into provider-scoped activity.

## Context to recover on demand

- TechSpec: `techspec.md#test-approach`, `docs/specs/03-PROVIDER-CLAUDE-CODE.md#4-session-monitoring-and-live-agent-activity`.
- Rules and skills: `AGENTS.md`, `dotnet-efficient-validation`.
- Code: `ClaudeSessionMonitor.CheckLivenessAsync`, `UsageStore.PollActivityAsync`, existing live-process fixture.

## Work

- [x] T06.1 Replace or extend the empty custom-directory test with a live-session fixture.
- [x] T06.2 Assert the returned session and provider-scoped activity event use the isolated provider ID.
- [x] T06.3 Verify the focused tests and quality profile for any changed source file.

## Acceptance criteria

- The test fails if the monitor scans the default directory instead of the custom directory.
- The activity event identifies `claude-work` and carries the live session.

## Verification

- Unit: `ClaudeSessionMonitorTests` and focused `UsageStore` activity integration.
- Integration: provider-scoped activity event, if the existing test harness supports it.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Environment dependency: current test process provides the live PID.
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeSessionMonitorTests*"`.
- Expected evidence: nonzero passing test count and a positive custom-directory assertion.

## Affected files

- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeSessionMonitorTests.cs`.
- Modify if needed: `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs` or its existing activity-test file.
- Modify if needed after a failing test: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs`.

## Observability and recovery

- Operational signal: `ProviderActivityChangedEventArgs.ProviderId` identifies the isolated profile.
- Recovery: preserve the prior passing session tests and diagnose any new positive-path failure.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: A live JSON session with the current process PID in `.claude-work/sessions` yields a busy `AgentSession`. The same `ClaudeSessionMonitor` publishes an activity event from `UsageStore` with `ProviderId` `claude-work` and the live PID. The previous empty custom-directory test was replaced by this positive path.
- Changed files: `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeSessionMonitorTests.cs` (removed the empty custom-directory test); new `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeMultiProfileActivityTests.cs` (positive monitor and activity event test). No production source changed.
- Checks: `rtk dotnet build TokenHound.slnx --no-restore --nologo --verbosity:minimal` passed (6 projects, 0 errors, 0 warnings); `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Claude*"` passed (68 tests); the same command with `--filter-class "*ClaudeMultiProfileActivityTests*"` passed (1 test); `rtk git diff --check` passed. QA-01 to QA-04 and QA-06 scans had no hits in the changed test files; QA-05 has no hit (297 and 80 lines).
- Validated state: Git base `3dc0c4e0a83a18149e5bba4524cf84606955de18` plus the uncommitted feature work and T04-T06 corrections on 2026-09-23. The test's temporary isolated directory is removed in `finally`.
- Open items: T07, original T01-T03 reconciliation, and independent re-review remain pending. No T06 manual check is required.
