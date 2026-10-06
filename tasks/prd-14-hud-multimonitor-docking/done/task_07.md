# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/prd.md`
2. `tasks/prd-14-hud-multimonitor-docking/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T07 — Settings HUD placement section

## Outcome

Settings > General shows a "HUD placement" section under HUD size with a Position list (six modes) and a Display list ("Primary monitor", each connected display labeled with number, name, resolution, and Primary, plus the stored preference marked as disconnected when absent). Selections apply immediately through `HudPlacementService`; Free disables the display list and shows a hint; a save failure shows an inline error.

## Dependencies and boundaries

- Depends on: T03, T04, T06
- Unblocks: —
- In scope: `HudPlacementSettingsViewModel`, `SettingsViewModel.HudPlacement`, the XAML section, `App.CreateSettingsViewModel` wiring, view model tests, test-project link.
- Out of scope: refreshing the display list while Settings stays open (DEC-13 limitation).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-06, FR-08, FR-12, FR-15 | `prd.md#functional-requirements` | Live selection; labeled list; disconnected preference; Free hint; migration visible as Free |
| DEC-13 | `techspec.md#technical-decisions` | Apply on selection; list built on open |
| CMP-16, CMP-17 | `techspec.md#components-and-flow` | View model and wiring |
| TC-07, TC-09 | `techspec.md#test-approach` | VM tests; MA-5..MA-8 |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (C# style, ≤ 300 lines per file, ≤ 30 per method), `dotnet-efficient-validation`, TechSpec quality profile QA-01..QA-07.
- Existing code: `src/TokenHound.App/ViewModels/HudSizeSettingsViewModel.cs` (section VM pattern, `ApplyError`), `SettingsViewModel.cs` (init properties), `SettingsWindow.xaml:276-390` (HUD size card, styles, automation names, error text), `App.xaml.cs:351-370` (`CreateSettingsViewModel`).
- Note: the General tab visibility depends on its section models (`tasks/prd-13-hud-size/workflow.md`, T02 event); keep the tab visible when `HudPlacement` is set.

## Work

- [x] T07.1 Create `HudPlacementSettingsViewModel` with `Create(HudPlacementService, Func<IReadOnlyList<DisplayInfo>>)`: `Modes`, `SelectedMode`, `Displays` (option records with `Label`, `Preference`, `IsDisconnected`), `SelectedDisplay`, `IsDisplayEnabled`, `DisplayHint`, `SaveError`; each selection calls the service once.
- [x] T07.2 Add `SettingsViewModel.HudPlacement`, the XAML card (two labeled `ComboBox`es, hint, error) under HUD size, and the `App` wiring with the service field created in T05 and `DisplayCatalog.GetDisplays`.
- [x] T07.3 Link the VM in the test project; add `HudPlacementSettingsViewModelTests` (TC-07).
- [x] T07.4 Build, run all tests, launch, and check MA-8 and the single-display part of MA-5 on the primary display; leave the multi-display steps of MA-5..MA-7 for the human visual check.

## Acceptance criteria

- Display labels follow `N — Name · W×H`, with ` · Primary` on the primary; a missing name shows `Display N`.
- A disconnected stored preference appears once, labeled "(disconnected — showing on primary)", and stays selected.
- In Free the display list is disabled and the hint says the display follows the dragged position.
- A mode chosen in Settings is checked in the context menu the next time it opens, and a mode chosen in the menu shows in Settings when it reopens.

## Verification

- Unit: TC-07.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: MA-8 and single-display MA-5 by the agent; MA-5..MA-7 by the human at the visual check (second display required).
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` filtered with `--filter-class "*HudPlacementSettings*"`, then unfiltered.
- Environment dependency: second display for MA-5..MA-7 (human).
- Expected evidence: builds 0 warnings; test counts; screenshot of the Settings section in Free and in a docked mode.

## Affected files

- Create: `src/TokenHound.App/ViewModels/HudPlacementSettingsViewModel.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/HudPlacementSettingsViewModelTests.cs`
- Modify: `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/App.xaml.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`

## Observability and recovery

- Operational signal: save failures are logged by the service (T03).
- Recovery: the section only calls the service; without it the context menu remains the control.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `HudPlacementSettingsViewModel` (modes, displays with `N — Name · W×H · Primary` labels, disconnected stored preference listed once and selected, Free disables the display selector with `FREE_HINT`, each selection calls the service once, save failure shows `SAVE_ERROR`, leaving Free re-selects the hosting display chosen by FR-18 without saving twice); `SettingsViewModel.HudPlacement`; "HUD placement" card under HUD size; General tab stays visible when only this section exists; `App` wires it with `_displayCatalog.GetDisplays` and `NotchWindow.CreatePlacementContext` (now public).
- Deviations inside the contract: no dialog `ComboBox` style existed, so `DialogResources.xaml` gained `DialogComboBoxStyle` and `DialogComboBoxItemStyle`; with a custom template `DisplayMemberPath` does not reach the selection box (it showed the record `ToString()`), so both combo boxes use a `LabelItemTemplate` resource instead.
- Changed files: `src/TokenHound.App/ViewModels/HudPlacementSettingsViewModel.cs` (new), `SettingsViewModel.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/UI/Styles/DialogResources.xaml`, `src/TokenHound.App/UI/Windows/NotchWindow.Placement.cs`, `src/TokenHound.App/App.xaml.cs`; `tests/.../ViewModels/HudPlacementSettingsViewModelTests.cs` (new, 8 tests); test project link (+1).
- Checks: App and test builds 0 warnings; `--filter-class "*HudPlacementSettings*"` → 8 passed; unfiltered → 1043 passed. Manual with three displays (Display 1 125%, CX156A primary 150%, LG ULTRAWIDE 100%): MA-8 pass (`{"Left":500,"Top":300}` → Settings shows Free, display selector disabled, hint shown); MA-5 pass (Top right applied immediately on the primary, flush; choosing "3 — LG ULTRAWIDE · 3440×1440" moved the HUD flush to the ultrawide's top-right corner; "Primary monitor" moved it back; stored `{"Mode":"TopRight","Display":null}`); the cross-scale move (150% → 100%) stayed flush (part of MA-7).
- Validated state: working tree at `c4b55b9` plus T01..T07.
- Quality profile: QA-01..QA-04, QA-07 clean; QA-05 only the baseline `App.xaml.cs` hit (new 4-argument test helper calls were split); QA-06: `App.xaml.cs` 481 lines (baseline 468, +13 wiring lines); other touched C# files ≤ 237; `SettingsWindow.xaml` 1051 lines (baseline 949, one card added).
- Open items: MA-6 (disconnect/reconnect the preferred display), the taskbar and HUD-size parts of MA-7, and the primary-display change in FR-07 need the human.

### ADR candidates

None - direct TechSpec implementation or local decision.
