# Stable execution context

Load in this exact order:

1. `tasks/prd-12-windows-autostart/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T04 — Startup failure logs name the failing operation

## Outcome

`StartupSettingsViewModel` logs failures with the TechSpec signal `"Startup shortcut {Operation} failed"`, with `Operation` set to `IsEnabled`, `Enable`, or `Disable`, so read failures and apply failures can be told apart.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T06
- In scope: the `TryRun` signature and log template in `StartupSettingsViewModel`.
- Out of scope: user-facing error text (`APPLY_ERROR` unchanged).

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/CR-02 | `codereview.md#findings` | Failure log lacks `{Operation}` and does not tell read from apply |

## Requirements

- TechSpec "Observability and rollout": `Log.Warning(ex, "Startup shortcut {Operation} failed", ...)`.

## Context to recover on demand

- Code: `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs:88-126`

## Work

- [ ] T04.1 Pass the operation name (`nameof(IStartupLaunchService.IsEnabled)`, `nameof(IStartupLaunchService.Enable)`, or `nameof(IStartupLaunchService.Disable)`) into `TryRun`, and log `"Startup shortcut {Operation} failed for {ExecutablePath}"`.

## Acceptance criteria

- The template `Startup shortcut {Operation} failed` is present, and each `TryRun` call passes a distinct operation name.
- TC-07..TC-09 still pass.

## Verification

- Unit: `--filter-class "*StartupSettings*"` (TC-07..TC-09 unchanged).
- E2E: omitted by .NET desktop policy.
- Environment dependency: none.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests`, then run the filtered class.
- Expected evidence: grep hit, test output.

## Affected files

- Modify: `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs`

## Observability and recovery

- Operational signal: the corrected `Log.Warning` template.
- Recovery: revert.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `TryRun(string operationName, Action operation)` now receives `nameof(IStartupLaunchService.IsEnabled|Enable|Disable)` and logs `"Startup shortcut {Operation} failed for {ExecutablePath}"`. The 4-argument `LOGGER.Warning` call is split one argument per line, per the CR-03 rule.
- Changed files: `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs`.
- Checks: the `--no-incremental` build of Infrastructure.Tests has 0 warnings. `--filter-class "*StartupSettings*"` passed 6/6 (TC-07..TC-09). QA-01..03 are empty. J3 (active): 2/2 verified at `auto`.
- Validated state: base b9bffe6 plus the feature worktree.
- Open items: none.
