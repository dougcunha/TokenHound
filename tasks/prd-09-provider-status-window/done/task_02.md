# Stable execution context

Load in this exact order:

1. `tasks/prd-09-provider-status-window/prd.md`
2. `tasks/prd-09-provider-status-window/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Status window and entry points

## Outcome

The user opens one dark `Provider Status` window from the tray (`Provider Status…`) or the notch's right-click menu (`Provider Status`). A second invocation brings the same window to the front. The window shows the T01 groups: account rows with wrapping quota columns, bars coloured by level, dimmed exhausted rows, and empty and pending states. It scrolls when rows overflow. Closing it disposes its view model, and application shutdown closes it with the other dialogs.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope:
  - CMP-07..CMP-09: window XAML and code-behind, level-to-brush converter.
  - `DialogService` show/close/`CloseAll`.
  - `HudActionsViewModel.ShowProviderStatus`.
  - `TrayMenuItemKey`, `TrayMenuModel`, `TrayIconViewModel` entries.
  - `NotchWindow` menu item.
  - One-line wiring in `App.xaml.cs` (DEC-03).
  - Entry-point tests; manual acceptance MA-01..MA-06.
- Out of scope: projection or formatting changes (T01 contracts; a defect found here reopens T01 through the DAG owner); plan tier, email, arrow action, hotkey, persisted window placement.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-02 | `prd.md#functional-requirements` | Entry points, single instance |
| FR-03..FR-12 (visual part) | `prd.md#functional-requirements` | Rendering of T01 data |
| NFR-04..NFR-07 | `prd.md#non-functional-requirements` | Style, lifecycle, scroll, code rules |
| DEC-03, DEC-10, DEC-11 | `techspec.md#technical-decisions` | Wiring, keys, window |
| CMP-07..CMP-09 | `techspec.md#components-and-flow` | Components |
| TC-10, TC-11 | `techspec.md#test-approach` | Entry-point tests, manual script |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (C# rules; desktop HUD validation via Windows MCP `App` `launch_executable` and `Screenshot` `display: [2]`); `dotnet-efficient-validation`; `docs/design/2026-08-28-usage-notch-design.md` §Ring colour states.
- Existing code:
  - `src/TokenHound.App/UI/Windows/DialogService.cs` (Settings pattern: activate-or-create, owner, `WindowPlacement.PositionInWorkArea`, dispose VM once).
  - `SettingsWindow.xaml` (dialog look and `DialogResources.xaml` usage).
  - `NotchWindow.xaml:30-51` and `NotchWindow.xaml.cs:OnSettingsClick`.
  - `TrayMenuModel.cs:BuildDescriptor`, `TrayIconViewModel.cs:Invoke/ShowSettings`, `HudActionsViewModel.cs:ShowSettings`.
  - `App.xaml.cs:InitializeUi`.
  - `ApplicationLifetime.cs` (`CloseAll` on shutdown).
- Tests to update: `tests/TokenHound.Infrastructure.Tests/Tray/TrayMenuModelTests.cs`, `Tray/TrayIconViewModelTests.cs`, `ViewModels/HudActionsViewModelTests.cs`.

## Work

- [ ] T02.1 Add `TrayMenuItemKey.ProviderStatus` (between `RefreshNow` and `Settings`), the `Provider Status…` header in `TrayMenuModel`, and the `Invoke` case plus `ShowProviderStatus` in `TrayIconViewModel`. Add `HudActionsViewModel.ShowProviderStatus` with the optional `showProviderStatus` constructor parameter and the shutdown guard. Update the tests (TC-10).
- [ ] T02.2 Add `DialogService.ShowProviderStatus(owner, viewModelFactory)`, `IsProviderStatusOpen`, and `CloseProviderStatus`; add the new close to `CloseAll`. Keep the file ≤ 300 lines (shared helper if needed).
- [ ] T02.3 Create `ProviderStatusWindow.xaml(.cs)` and `UsageLevelToBrushConverter` per DEC-11 and NFR-04: groups with header and count, account column (monospace name, status, alert `back in`), wrapping column cells (label, %, bar, reset line), dim at 0.45 opacity, empty and pending states, `ScrollViewer`. Log opened/closed.
- [ ] T02.4 Add `ProviderStatusMenuItem` to `CapsuleContextMenu` before `Settings`, with its click handler. Wire `App.xaml.cs` with the single new argument (DEC-03).
- [ ] T02.5 Build, run the tests, launch the app through Windows MCP, and run MA-01..MA-06 with screenshots.

## Acceptance criteria

- TC-10 passes; the full `tests/TokenHound.Infrastructure.Tests` and `tests/TokenHound.Core.Tests` pass; `TokenHound.App` builds with no new warnings.
- Tray and HUD entries open exactly one window; re-invoking activates it; nothing opens during shutdown.
- Closing the window disposes the VM exactly once; shutdown closes an open status window.
- `App.xaml.cs` grows by exactly one line (DEC-03). `DialogService.cs` is ≤ 300 lines.
- MA-01..MA-06 produce the expected results, with screenshots. MA-05 is marked pending, with the reason, if no exhausted account is available.

## Verification

- Unit: TC-10.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: MA-01..MA-06 from the TechSpec. Owner: the user; executed by the coordinator via Windows MCP `App` (`mode="launch_executable"`) and `Screenshot` (`display: [2]`).
- Commands:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj`
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1`
  - `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: an interactive desktop with Windows MCP; at least two Claude profiles plus one other enabled provider for MA-03.
- Expected evidence: exit codes and test counts; screenshots for each MA step; `git diff --stat` showing `App.xaml.cs` +1.

## Affected files

- Modify: `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Windows/DialogService.cs`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs`, `src/TokenHound.App/UI/Tray/TrayMenuItemKey.cs`, `src/TokenHound.App/UI/Tray/TrayMenuModel.cs`, `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs`, `src/TokenHound.App/ViewModels/HudActionsViewModel.cs`; `tests/TokenHound.Infrastructure.Tests/Tray/TrayMenuModelTests.cs`, `Tray/TrayIconViewModelTests.cs`, `ViewModels/HudActionsViewModelTests.cs`
- Create: `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml`, `ProviderStatusWindow.xaml.cs`; `src/TokenHound.App/UI/Converters/UsageLevelToBrushConverter.cs`

## Observability and recovery

- Operational signal: `Log.Information("Provider status window opened")` / `closed`.
- Recovery: revert the task diff; no persisted state.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result:
  - Entry points: tray entry `Provider Status…` (`TrayMenuItemKey.ProviderStatus` between RefreshNow and Settings) and HUD menu item `Provider Status` before Settings. Both call `HudActionsViewModel.ShowProviderStatus`, which is guarded during shutdown.
  - `DialogService.ShowProviderStatus`, `CloseProviderStatus`, and `IsProviderStatusOpen` delegate to a new `ProviderStatusDialog`, which handles activate-or-create, owner, placement, one-time VM disposal, and opened/closed logs. `CloseAll` also closes the status window.
  - `ProviderStatusWindow` is a dark dialog: group header with a count, monospace account name, status and alert `back in` text, wrapping 220 px quota cells with a level-coloured bar and reset line, bar hidden without a fraction, exhausted rows dimmed, empty state, and vertical `ScrollViewer`.
  - `UsageLevelToBrushConverter` maps levels to the ring colours from the design doc.
  - `App.xaml.cs` wiring adds the single `showProviderStatus` argument (DEC-03).
- Changed files:
  - Modified: `src/TokenHound.App/App.xaml.cs` (+2/−1, net +1 → 449 lines), `UI/Windows/DialogService.cs` (274 lines), `UI/Windows/NotchWindow.xaml`, `UI/Windows/NotchWindow.xaml.cs`, `UI/Tray/TrayMenuItemKey.cs`, `UI/Tray/TrayMenuModel.cs`, `UI/Tray/TrayIconViewModel.cs`, `ViewModels/HudActionsViewModel.cs`; `tests/TokenHound.Infrastructure.Tests/Tray/TrayMenuModelTests.cs`, `Tray/TrayIconViewModelTests.cs`, `ViewModels/HudActionsViewModelTests.cs`.
  - Created: `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml(.cs)`, `UI/Windows/ProviderStatusDialog.cs`, `UI/Converters/UsageLevelToBrushConverter.cs`.
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj`: 0 errors, 0 warnings, including a rebuild after the final XAML width change.
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/...csproj`: 0 errors, 0 warnings.
  - Focused runs (`--filter-class` TrayMenuModelTests, TrayIconViewModelTests, HudActionsViewModelTests): 22 passed.
  - Full `tests/TokenHound.Infrastructure.Tests`: 819 passed (816 + 3 new), exit 0.
  - `tests/TokenHound.Core.Tests`: 92 passed, exit 0.
  - The final XAML-only width change does not affect the linked test sources.
- Manual acceptance (Windows MCP, primary display 3440×1440 at 100% scale; the user authorized closing the installed instance during the test and it was relaunched afterwards):
  - MA-01 pass: the tray `Provider Status…` entry activated the already-open window. One `Provider Status` window (same HWND 592534) was in the foreground.
  - MA-02 pass: the HUD right-click menu shows `Provider Status` before Settings and opens the window.
  - MA-03 pass: groups Antigravity 1, Claude 2 (`Claude Code` default first, then `Claude Code (pessoal)`), Codex 1, OpenCode 1. Columns match the HUD rows per provider (Claude 3 or 2, Codex 2, OpenCode 3, Antigravity 1 without a bar), with used %, green bars, and reset lines like `in 37 minutes · 27/09, 15:00` (pt-BR). The first run wrapped Claude's third column at the default width; cell width went from 240/28 to 220/24, and a re-run shows three columns on one line.
  - MA-04 pass: unchecking Codex in Settings removed its group immediately and the HUD dropped to 4 rings. Re-checking restored both; the user's setting is back to its original value.
  - MA-05 pending: no account was exhausted during the test. The exhausted projection is covered by unit TC-08, but dimming and the alert `back in` have not been seen on screen.
  - MA-06 partial: narrowed to 620×520, columns wrap one per line and the vertical scrollbar appears. 150% scaling is not verified because it would require changing the user's display settings. The ≥ 10-row case was not exercised (6 accounts available); scrolling was.
  - NFR-05: tray Exit with the status window open terminated the process cleanly.
- Validated state: git base `53da181` plus the T01 and T02 working-tree diff; Debug configuration; net10.0-windows.
- Quality profile:
  - QA-01..QA-04: no hits.
  - QA-06: `App.xaml.cs` 449 (baseline 448, +1 accepted by DEC-03); all other touched files ≤ 288.
  - QA-07 new reservation hits: `UsageLevelToBrushConverter.Convert/ConvertBack` have 4 parameters, a signature `IValueConverter` imposes (same as `ResourceKeyConverter`). Other hits are baseline (`App.xaml.cs:366`, `NotchWindow.xaml.cs:105`, `TrayIconViewModel.cs:30`).
- Open items: MA-05 (visual check of an exhausted account) and the 150% scaling part of MA-06 are left for the user at HIL 3.

### ADR candidates

None - direct TechSpec implementation or local decision.
