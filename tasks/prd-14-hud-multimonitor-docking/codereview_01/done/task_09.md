# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/codereview_01/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T09 — Move HUD placement wiring out of App.xaml.cs

## Outcome

The feature's HUD wiring lives in a new `App.Hud.cs` partial: `App.xaml.cs` is no longer than its 468-line baseline and `InitializeUi` is at most 30 lines, with identical startup behavior.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T10
- In scope: move the `_displayCatalog` and `_hudPlacement` fields, the `HudPlacementSettingsViewModel` creation, the startup display enumeration, and the `NotchWindow` creation and show into factory methods in `App.Hud.cs`, following the `App.Mcp.cs` / `App.Updates.cs` partial pattern.
- Out of scope: provider registration, tray initialization, settings view model members other than `HudPlacement`, and any further reduction of `App.xaml.cs` toward 300 lines.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_01/CR-02 | `codereview.md#Findings` | `App.xaml.cs` 468 → 481 lines; `InitializeUi` 35 → 38 lines (QA-06) |

## Requirements

- `_displayCatalog.GetDisplays()` still runs before the HUD window is shown (startup display log, T04).
- `NotchWindow` still receives `DataContext`, `ActionsViewModel`, `Placement`, and `Displays` before `Show()`; `MainWindow` is set before `Show()`.
- Settings `HudPlacement` remains `null` when no HUD window exists.
- XML summary on the new partial, as in `App.Mcp.cs`.

## Context to recover on demand

- TechSpec: `techspec.md` App wiring components (T05, T07).
- Rules and skills: `CLAUDE.md` C# structure and style; `dotnet-efficient-validation`.
- Code: `src/TokenHound.App/App.xaml.cs:354` `CreateSettingsViewModel`, `:381` `InitializeUi`; `src/TokenHound.App/App.Mcp.cs` partial pattern.

## Work

- [x] T09.1 Create `src/TokenHound.App/App.Hud.cs` with the two fields, `CreateHudPlacementSettingsViewModel()`, and a method that enumerates displays, creates the `NotchWindow`, assigns `MainWindow`, and shows it.
- [x] T09.2 Replace the inline code in `App.xaml.cs` with calls; drop the now-unused `using TokenHound.App.Interop`.

## Acceptance criteria

- `App.xaml.cs` <= 468 lines; `InitializeUi` <= 30 lines; new methods <= 30 lines.
- App builds with 0 warnings; full Infrastructure test suite still passes.

## Verification

- Unit: not applicable (the App composition root is not linked into tests).
- Integration: App build.
- E2E: omitted by .NET desktop policy.
- Manual: HUD starts at its configured placement and Settings shows the HUD placement section (smoke at HIL 3, owner: human).
- Environment dependency: none.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: build output with 0 warnings and 0 errors; test summary with 0 failed; line count of `App.xaml.cs`.

## Affected files

- Modify: `src/TokenHound.App/App.xaml.cs`
- Create: `src/TokenHound.App/App.Hud.cs`

## Observability and recovery

- Operational signal: existing "HUD window displayed successfully." and display enumeration logs.
- Recovery: revert both files.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: New partial `App.Hud.cs` (44 lines) holds `_displayCatalog`, `_hudPlacement`, `CreateHudPlacementSettingsViewModel()` (8 lines), and `ShowNotchWindow(notchViewModel, actionsViewModel)` (19 lines: display enumeration, window creation with `DataContext`, `ActionsViewModel`, `Placement`, `Displays`, `MainWindow`, `Show()`, log). `App.xaml.cs` is 459 lines (baseline 468) and `InitializeUi` is 25 lines (baseline 35); the unused `using TokenHound.App.Interop` was removed. Display enumeration still precedes window creation and `Show()`; it now runs after `ApplicationLifetime` and `HudActionsViewModel` creation, which do not read displays.
- Changed files: `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/App.Hud.cs` (new)
- Checks: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` → 4 projects, 0 errors, 0 warnings, exit 0; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore` → 0 errors, 0 warnings, exit 0; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → 1043 passed, 0 failed, exit 0 (one integrated run after T08..T10; the changed files are not linked into the test project, so the suite is a regression guard).
- Validated state: Uncommitted worktree on base c4b55b9 (HEAD c4b55b9), Debug, net10.0-windows, Windows 11; integrated state after T08, T09, and T10.
- Open items: Manual smoke (HUD starts at its configured placement; Settings shows the HUD placement section) left to the human at HIL 3; Windows MCP is disconnected in this session.
