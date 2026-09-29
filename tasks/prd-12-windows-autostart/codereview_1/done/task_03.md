# Stable execution context

Load in this exact order:

1. `tasks/prd-12-windows-autostart/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T03 — Settings opens even when the Startup folder is missing

## Outcome

`StartupLaunchService.CreateDefault()` resolves the per-user Startup folder path even when that folder does not exist, so opening Settings never throws, and `Enable` creates the folder as specified.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T05, T06
- In scope: `CreateDefault` folder resolution with `SpecialFolderOption.DoNotVerify`, and a test on the resolved path.
- Out of scope: the App-level exception handling policy (`OnDispatcherUnhandledException`), and making `ShellLink` internal (OI-01, informational).

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/CR-01 | `codereview.md#findings` | `CreateDefault` returns an empty path for a missing Startup folder, so the constructor throws on Settings open |

## Requirements

- TechSpec "Errors, security, and recovery": a missing Startup folder is created on `Enable`, and the app keeps running (FR-06 intent).
- `CreateDefault()` never produces an empty folder path on Windows.

## Context to recover on demand

- TechSpec: `techspec.md#errors-security-and-recovery`, DEC-01
- Code: `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs:53` (`CreateDefault`), `:35` (constructor guard)

## Work

- [ ] T03.1 Use `Environment.GetFolderPath(Environment.SpecialFolder.Startup, Environment.SpecialFolderOption.DoNotVerify)` in `CreateDefault`.
- [ ] T03.2 Add a test that `CreateDefault().ShortcutPath` equals the `DoNotVerify` resolution plus `TokenHound.lnk`, and is rooted.

## Acceptance criteria

- `CreateDefault` passes `SpecialFolderOption.DoNotVerify`, and its shortcut path is rooted and non-empty.
- The existing TC-03 still proves that `Enable` creates a missing folder. There are no regressions in the Startup tests.

## Verification

- Unit: `StartupLaunchServiceTests.CreateDefault_ResolvesStartupFolderWithoutVerification`.
- E2E: omitted by .NET desktop policy.
- Environment dependency: none.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests`, then `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*StartupLaunch*"`
- Expected evidence: test output, 0-warning build.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs`, `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs`

## Observability and recovery

- Operational signal: none new.
- Recovery: revert the one-line change.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `StartupLaunchService.CreateDefault()` now uses the new public `ResolveStartupFolder()`, which calls `GetFolderPath(SpecialFolder.Startup, SpecialFolderOption.DoNotVerify)`. A missing Startup folder yields its path rather than `""`, so the constructor no longer throws and `Enable` creates the folder. Added the test `CreateDefault_ResolvesStartupFolderWithoutVerification`.
- Changed files: `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs`, `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs`.
- Checks: the `--no-incremental` build of Infrastructure.Tests has 0 warnings. `--filter-class "*StartupLaunch*"` passed 6/6, including TC-03 (`Enable` over a non-existent `<temp>\Startup`). QA-01..03 are empty. J3 (active): 2/2 verified at `auto`.
- Validated state: base b9bffe6 plus the feature worktree. Windows 11, .NET 10.
- Open items: none.
