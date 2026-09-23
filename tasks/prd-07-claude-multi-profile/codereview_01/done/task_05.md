# T05 — Evaluate all Claude rings before mock fallback

## Outcome

One Claude profile needing authentication does not activate mock telemetry when another Claude profile remains valid.

## Dependencies and boundaries

- Depends on: none.
- Unblocks: none.
- In scope: `NotchViewModel` fallback logic and focused tests.
- Out of scope: UI styling and telemetry provider behavior.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_01/CR-02` | `codereview.md#findings` | FR-07 and TC-06 are bypassed by `OnSnapshotUpdated` and its current test. |

## Requirements

- Evaluate all current Claude profile rings before activating a configured mock provider.
- Preserve the existing empty-state fallback when no usable provider is present.
- Keep a valid isolated profile visible and free of mock telemetry when another profile enters `NeedsAuth`.

## Context to recover on demand

- TechSpec: `techspec.md#technical-decisions`, `techspec.md#test-approach`.
- Rules and skills: `AGENTS.md`, `no-workarounds`, `dotnet-efficient-validation`.
- Code: `NotchViewModel.OnSnapshotUpdated`, `ShouldFallbackToMock`, `LoadMockFallback`.

## Work

- [x] T05.1 Route snapshot-triggered fallback through the all-profile decision.
- [x] T05.2 Add a test with a configured mock provider, a valid isolated profile, and another Claude profile in `NeedsAuth`.
- [x] T05.3 Verify fallback when all relevant Claude profiles need authentication.

## Acceptance criteria

- A valid `claude-work` ring prevents mock fallback despite `claude` entering `NeedsAuth`.
- When no valid profile remains, the configured fallback retains its intended behavior.
- A test exercises the configured mock path, not merely the absence of one.

## Verification

- Unit: both mixed-status and all-needs-auth scenarios.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Environment dependency: none.
- Commands: `rtk dotnet build TokenHound.slnx --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"`.
- Expected evidence: nonzero passing test count and no new blocking quality-profile hit.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/NotchViewModel.cs`.
- Modify: `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`.

## Observability and recovery

- Operational signal: `IsFallbackActive` remains false when a valid Claude profile exists.
- Recovery: revert the fallback condition if the test evidence fails.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `OnSnapshotUpdated` now calls `ShouldFallbackToMock` before loading a configured mock. A `claude-work` ring in `Ok` keeps mock inactive when `claude` changes to `NeedsAuth`; mock activates after both profiles enter `NeedsAuth`. The constructor's empty-ring fallback remains covered by its existing test.
- Changed files: `src/TokenHound.App/ViewModels/NotchViewModel.cs`; `tests/TokenHound.Infrastructure.Tests/ViewModels/NotchViewModelTests.cs`.
- Checks: `rtk dotnet build TokenHound.slnx --no-restore --nologo --verbosity:minimal` passed (7 projects, 0 errors, 0 warnings); `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchViewModelTests*"` passed (13 tests); `rtk git diff --check` passed. QA-01, QA-03, QA-04, and QA-06 have no hits in the touched files. QA-02's two synchronous mock snapshot calls at lines 82-83 predate this task. QA-05: the test file was already over 300 lines (370 before T05; 405 after), while `NotchViewModel.cs` is 273 lines.
- Validated state: Git base `3dc0c4e0a83a18149e5bba4524cf84606955de18` plus the uncommitted feature work and T04-T05 corrections on 2026-09-23. The configured mock regression tests exercise actual `UsageStore` refresh events.
- Open items: T06 and T07 remain; original T01-T03 reconciliation and independent re-review remain pending. No T05 manual or integration check is required.
