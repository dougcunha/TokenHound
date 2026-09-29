# Stable execution context

Load in this exact order:

1. `tasks/prd-12-windows-autostart/prd.md`
2. `tasks/prd-12-windows-autostart/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Settings toggle and installer compatibility

## Outcome

Settings opens on a new first tab, "General". Its "Start TokenHound when I sign in to Windows" checkbox reflects the OS state, and Apply turns start-at-logon on or off through `IStartupLaunchService`, with an inline error on failure. The behavior is the same in installed and portable copies. The installer keeps the user's choice on silent updates, pre-selects its task from the current state in interactive setups (unticked removes the shortcut), and removes the shortcut on uninstall.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope: CMP-07..CMP-11, TC-07..TC-09 (ViewModel tests and the `<Compile Include ... Link>`), TC-10 (installer compile), and the manual scripts MA-1..MA-4 prepared for the visual check.
- Out of scope: service internals (T01), and other startup mechanisms (PRD Out of scope).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-05..FR-09, FR-01 (installer side) | `prd.md#functional-requirements` | Tab, error handling, installer silent, interactive, and uninstall behavior |
| NFR-03, NFR-04 | `prd.md#non-functional-requirements` | No stall, accessibility |
| US-01..US-03, UX | `prd.md` | Journeys and tab layout |
| DEC-06..DEC-10, DEC-12 | `techspec.md#technical-decisions` | ViewModel, tab, installer, mode parity |
| CMP-07..CMP-11 | `techspec.md#components-and-flow` | Files |
| TC-07..TC-11 | `techspec.md#test-approach` | Tests and manual acceptance |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`, `repository-cli-efficiency`, and the C# rules in `CLAUDE.md`
- Existing code: `src/TokenHound.App/ViewModels/UpdateSettingsViewModel.cs` (the pattern to mirror). `src/TokenHound.App/ViewModels/SettingsViewModel.cs:62` (`Updates` init property). `src/TokenHound.App/App.xaml.cs:349-363` (`CreateSettingsViewModel`). `src/TokenHound.App/UI/Windows/SettingsWindow.xaml:255` (first tab) and `:497-600` (Updates card markup to copy). `tests/TokenHound.Infrastructure.Tests/ViewModels/UpdateSettingsViewModelTests.cs` (test style). `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj:71` (Compile Link).
- Installer: `installer/TokenHound.iss:79-121` and `installer/build-installer.ps1`. The self-update arguments are in `src/TokenHound.Infrastructure/Updates/InstallerUpdateLauncher.cs:18`.

## Work

- [ ] T02.1 `StartupSettingsViewModel` (DEC-06) with `StartupSettingsViewModelTests` (TC-07..TC-09) over a fake `IStartupLaunchService`, and the Compile Link in the test csproj.
- [ ] T02.2 `SettingsViewModel.Startup` init property. In `App.CreateSettingsViewModel`, set `Startup` from `StartupLaunchService.CreateDefault()` and `Environment.ProcessPath` (null path → `Startup = null`).
- [ ] T02.3 `SettingsWindow.xaml`: "General" as the first tab, with a Startup card (checkbox, hint, error, Apply, and "Saved" feedback), automation names and help text, and the tab hidden when `Startup` is null.
- [ ] T02.4 `TokenHound.iss`: `StartupShortcutExisted` in `InitializeSetup`; `ShouldCreateStartupShortcut` as the `Check` of the startup icon; one-time `WizardSelectTasks` in `CurPageChanged(wpSelectTasks)`; interactive-only delete in `CurStepChanged(ssPostInstall)`; `[UninstallDelete]`; and cross-reference comments on the shortcut name. Compile through `build-installer.ps1`.

## Acceptance criteria

- At load, `CanApply` is false. Toggling makes it true, and toggling back makes it false again (TC-07).
- Apply calls `Enable(exePath)` or `Disable()`, reads the state back, and sets `IsApplied`. `CanApply` is false afterwards (TC-08).
- `UnauthorizedAccessException`, `IOException`, and `COMException` from the service set `ApplyError`, and `IsEnabled` equals the state read back. No exception escapes (TC-09).
- The ViewModel calls the service synchronously on the UI thread (DEC-11). At the visual check, MA-1 records that opening Settings and pressing Apply show no visible stall (NFR-03).
- Settings shows "General" as the first tab, with the checkbox named "Start TokenHound when I sign in to Windows" and its help text (TC-11, visual check).
- The installer script compiles with ISCC. The `startupicon` task name and the `{userstartup}\{#MyAppName}` icon are unchanged. Silent setup without `/TASKS` neither creates nor deletes the shortcut. Interactive setup pre-selects from the shortcut and deletes it when unticked. Uninstall deletes it (TC-10, MA-3, MA-4).
- The build has 0 warnings, and the full Core.Tests and Infrastructure.Tests runs pass.

## Verification

- Unit: TC-07..TC-09 (Infrastructure.Tests, fake service).
- Integration: installer compile through `installer/build-installer.ps1` (TC-10).
- E2E: omitted by .NET desktop policy.
- Manual (owner: human, visual check before review): MA-1..MA-4 as scripted in `techspec.md#test-approach`.
- Commands: `rtk dotnet build src/TokenHound.App` and `rtk dotnet build tests/TokenHound.Infrastructure.Tests`. Then `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*StartupSettings*"`, followed by full runs of both test projects. Finally `pwsh installer/build-installer.ps1` (flags per the script's help).
- Environment dependency: Windows and ISCC (present locally). The human runs the manual scripts.
- Expected evidence: test output with TC names, a 0-warning build, the ISCC compile log, and the MA results recorded at the visual check.

## Affected files

- Create: `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/StartupSettingsViewModelTests.cs`
- Modify: `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `installer/TokenHound.iss`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`

## Observability and recovery

- Operational signal: `Log.Warning` on Apply failure, through the T01 service logs.
- Recovery: revert the commit. A shortcut left by a user is plain user state.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result:
  - `StartupSettingsViewModel` (OS-state baseline, `IsDirty`/`CanApply`/`ApplyCommand`/`ApplyError`/`IsApplied`). Apply calls `Enable(path)`/`Disable()` and reads the state back. `IOException`, `UnauthorizedAccessException`, and `COMException` are logged and set `ApplyError`.
  - `SettingsViewModel.Startup` init property. `App.CreateSettingsViewModel` wires it from `StartupLaunchService.CreateDefault()` and `Environment.ProcessPath`, or sets `null` when there is no path.
  - A "General" first tab with the Startup card: checkbox, hint, Apply, "Settings applied", and inline error, with automation names and help text. It is collapsed when `Startup` is null.
  - Installer:
    - `Check: ShouldCreateStartupShortcut` on the startup icon: silent setups without `/TASKS` only keep an existing shortcut.
    - One-time `WizardSelectTasks` from the current shortcut state.
    - Interactive untick deletes the shortcut in `ssPostInstall`.
    - `[UninstallDelete]` for `{userstartup}\{#MyAppName}.lnk`.
    - Cross-reference comments with `SHORTCUT_FILE_NAME`.
- Local deviation within DEC-07 (recorded, no contract change): putting General first shifts the Cadence tab from index 1 to 2. `SettingsWindow.xaml.cs` (Enter-to-apply and focus-on-select) and four XAML button triggers (Reset, Close, Cancel, Apply) keyed on `SelectedIndex == 1`. They now use the named `CadenceTab.IsSelected`. The code-behind also selects `ProvidersTab` when `Startup` is null, so a collapsed General tab is never the selected one. This adds `SettingsWindow.xaml.cs` to the affected files.
- Changed files:
  - Created: `src/TokenHound.App/ViewModels/StartupSettingsViewModel.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/StartupSettingsViewModelTests.cs`
  - Modified: `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`, `installer/TokenHound.iss`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
- Checks:
  - `--no-incremental` builds of `src/TokenHound.App` and `tests/TokenHound.Infrastructure.Tests`: 0 warnings, 0 errors.
  - `--filter-class "*StartupSettings*"`: 6/6 passed:
    - `CanApply_TracksDifferenceFromOperatingSystemState`: TC-07
    - `Apply_WhenTurnedOn_EnablesAndReadsStateBack` and `Apply_WhenTurnedOff_DisablesAndReadsStateBack`: TC-08
    - `Apply_WhenServiceFails_ShowsErrorAndKeepsRealState` × `UnauthorizedAccessException`/`IOException`/`COMException`: TC-09
  - Full runs: Infrastructure.Tests 950/950 and Core.Tests 166/166 passed.
  - `pwsh installer/build-installer.ps1` published and compiled `dist/TokenHound-Setup-0.0.0-dev-win-x64.exe`, and a direct ISCC compile printed no warnings (TC-10 compile part).
  - Quality profile: QA-01..QA-03 and QA-06 are empty over the T02 `.cs` files.
- Validated state: base `c744030` plus T01 and T02 files. Debug build for tests, and Release publish via `build-installer.ps1`. Windows 11, .NET 10 SDK, Inno Setup 6 (per-user install).
- J3 gate (active): 5/8 claims `auto`. Claims 4, 5, and 6 were below threshold:
  - C4 (synchronous, 0.39): code part justified. `rg 'async|Task|await|Dispatcher'` over `StartupSettingsViewModel.cs` is empty.
  - C5 (0.67): markup part justified. `GeneralTab` is at `SettingsWindow.xaml:255`, before `ProvidersTab` at `:342`.
  - C6 (0.76): script part justified by the `.iss` diff and the ISCC compile.
  - The manual parts of C4, C5, and C6 were deferred to the visual check (MA-1..MA-4), which later passed.
  - Rubric-only `escalate` (test_gap/blast_radius, safe_to_apply 0.44 on `TokenHound.iss`) is an unspecific signal. The installer has no automated test harness, so MA-3 and MA-4 cover it.
- Open items:
  - Resolved on 2026-09-29: MA-1..MA-4, the General tab visual and automation check (TC-11, and TC-10 behavior), and NFR-03 (no visible stall) passed at the human visual check. Human text: "Tudo OK" (workflow.md Events). Until then, this item was recorded as awaiting that check.
- Reservation hits:
  - QA-07: `src/TokenHound.App/App.xaml.cs` is 465 lines (pre-existing 461, above 300; +4 lines for the wiring).
  - `SettingsWindow.xaml` is 821 lines (pre-existing 732, above 500; +89 for the General tab, one insertion).
  - Both files are in the Terrain baseline.

### ADR candidates

None - direct TechSpec implementation or local decision.
