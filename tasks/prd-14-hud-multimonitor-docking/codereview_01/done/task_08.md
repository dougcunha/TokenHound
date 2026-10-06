# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/codereview_01/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T08 — Split the display-config buffer query out of QueryTargets

## Outcome

`DisplayCatalog.QueryTargets` is at most 30 lines; the display target names it returns, and the warnings it logs on failure, are unchanged.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T10
- In scope: extract the `GetDisplayConfigBufferSizes` + `QueryDisplayConfig` sequence (current lines ~14-42) into a private static helper that returns the active paths, or an empty result after logging the same warning.
- Out of scope: P/Invoke declarations, `AddTarget`, `ReadSourceName`, `ReadTargetName`, and display resolution behavior.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_01/CR-01 | `codereview.md#Findings` | `QueryTargets` is 39 lines (QA-06, limit 30) |

## Requirements

- Same flags (`QDC_ONLY_ACTIVE_PATHS`), same warning messages and `{Status}` argument, same empty-dictionary result on either failure.
- Only the first `pathCount` entries returned by `QueryDisplayConfig` are consumed, as today.
- Repository C# style: methods <= 30 lines, calls with >= 4 arguments split one per line, blank-line rules.

## Context to recover on demand

- TechSpec: `techspec.md` DisplayCatalog component (T04).
- Rules and skills: `CLAUDE.md` C# structure and style; `dotnet-efficient-validation`.
- Code: `src/TokenHound.App/Interop/DisplayCatalog.Targets.cs:10` — `QueryTargets`.

## Work

- [x] T08.1 Add a helper (for example `QueryActivePaths`) that returns the active path entries actually filled by `QueryDisplayConfig`, or an empty array after logging the existing warning.
- [x] T08.2 Reduce `QueryTargets` to the dictionary creation, helper call, and loop.

## Acceptance criteria

- `QueryTargets` and the new helper are each <= 30 lines.
- App builds with 0 warnings; full Infrastructure test suite still passes.

## Verification

- Unit: not applicable (Win32 interop, not linked into tests); the suite run guards regressions in linked placement code.
- Integration: App build.
- E2E: omitted by .NET desktop policy.
- Manual: startup log still lists every display with its device path (smoke at HIL 3, owner: human).
- Environment dependency: none.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: build output with 0 warnings and 0 errors; test summary with 0 failed.

## Affected files

- Modify: `src/TokenHound.App/Interop/DisplayCatalog.Targets.cs`

## Observability and recovery

- Operational signal: existing `GetDisplayConfigBufferSizes failed` / `QueryDisplayConfig failed` warnings.
- Recovery: revert the file.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `QueryTargets` (10 lines) now builds the dictionary from `QueryActivePaths()` (12 lines), which checks `GetDisplayConfigBufferSizes` and delegates to `FillActivePaths(pathCount, modeCount)` (22 lines), which calls `QueryDisplayConfig` and returns only the filled entries (`paths[..(int)pathCount]`). Same flags, same warning templates and `{Status}` argument, empty result on either failure.
- Changed files: `src/TokenHound.App/Interop/DisplayCatalog.Targets.cs`
- Checks: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` → 4 projects, 0 errors, 0 warnings, exit 0; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore` → 0 errors, 0 warnings, exit 0; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → 1043 passed, 0 failed, exit 0 (one integrated run after T08..T10; the changed files are not linked into the test project, so the suite is a regression guard).
- Validated state: Uncommitted worktree on base c4b55b9 (HEAD c4b55b9), Debug, net10.0-windows, Windows 11; integrated state after T08, T09, and T10.
- Open items: Manual smoke (startup log lists every display with its device path) left to the human at HIL 3; Windows MCP is disconnected in this session.
