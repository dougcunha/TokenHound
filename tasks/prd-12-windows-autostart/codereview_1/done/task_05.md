# Stable execution context

Load in this exact order:

1. `tasks/prd-12-windows-autostart/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T05 — Split four-argument calls per repository style

## Outcome

The calls with four or more arguments in the feature's new code are split one argument per line, as `CLAUDE.md` requires.

## Dependencies and boundaries

- Depends on: T03 (same file, `StartupLaunchService.cs`)
- Unblocks: T06
- In scope: `StartupLaunchService.cs` (`ShellLink.Save` call) and `ShellLink.cs` (`GetPath` call). Also any other call with four or more arguments on one line in the feature's new `.cs` files.
- Out of scope: pre-existing code.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/CR-03 | `codereview.md#findings` | Four-argument calls on one line |

## Requirements

- `CLAUDE.md`: split calls with 4 or more arguments across lines: method name and opening parenthesis, one argument per line, closing parenthesis on its own line.

## Context to recover on demand

- Code: `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs:69`, `src/TokenHound.Infrastructure/Startup/ShellLink.cs:60`

## Work

- [ ] T05.1 Split both calls, and sweep the feature's new `.cs` files for other one-line calls with four or more arguments.

## Acceptance criteria

- No call with four or more arguments stays on one line in `src/TokenHound.Infrastructure/Startup/*.cs` or `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs`. Method declarations and COM interface signatures are not calls.
- The build has 0 warnings, and the Startup tests pass.

## Verification

- E2E: omitted by .NET desktop policy.
- Environment dependency: none.
- Commands: build, then `--filter-class "*Startup*"`.
- Expected evidence: sweep output, test output.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs`, `src/TokenHound.Infrastructure/Startup/ShellLink.cs`

## Observability and recovery

- Operational signal: none.
- Recovery: formatting only.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `ShellLink.Save(...)` in `StartupLaunchService.Enable` and `link.GetPath(...)` in `ShellLink.ReadTarget` are split one argument per line. T04 had already split the `LOGGER.Warning` call. The sweep regex over `src/TokenHound.Infrastructure/Startup/*.cs` and `StartupSettingsViewModel.cs` (declarations excluded) is now empty.
- Changed files: `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs`, `src/TokenHound.Infrastructure/Startup/ShellLink.cs`.
- Checks:
  - The `--no-incremental` builds of Infrastructure.Tests and `src/TokenHound.App` have 0 warnings.
  - `--filter-class "*Startup*"` passed 12/12.
  - Full runs: Core.Tests 166/166 and Infrastructure.Tests 951/951.
  - J3 (active): criterion 1 was verified at 0.94. Criterion 2 came back `review` at 0.77, because its evidence had only Infrastructure filtered tests; it is justified by the full runs above, which include the Core `StartupApprovalPolicyTests`.
- Validated state: base b9bffe6 plus the feature worktree.
- Open items: none.
