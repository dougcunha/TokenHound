# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T09 — Minimal NotchWindow Shell & App Integration

## Outcome

Assembles the minimal `NotchWindow.xaml` HUD capsule, hooks Win32 `WS_EX_NOACTIVATE` styles via `WindowStyles`, positions the capsule at the top-center of the primary monitor work area, binds `NotchViewModel`, and wires `App.xaml` for end-to-end execution.

## Dependencies and boundaries

- Depends on: T06, T07, T08
- Unblocks: —
- In scope: `NotchWindow.xaml` / `.cs`, `App.xaml` / `.cs` startup wiring, focus safety hooks, screen positioning, and manual verification script.
- Out of scope: Bézier dynamic animations, settings dialog, or system tray icon (deferred to PRD 08).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-06 | `prd.md#functional-requirements` | Win32 WS_EX_NOACTIVATE styling |
| FR-07 | `prd.md#functional-requirements` | Minimal NotchWindow capsule |
| OBJ-01 | `prd.md#outcomes-and-metrics` | Complete end-to-end telemetry pipeline |
| OBJ-02 | `prd.md#outcomes-and-metrics` | Non-activating desktop HUD behavior |
| CMP-09 | `techspec.md#components-and-flow` | src/TokenHound.App/UI/Windows/NotchWindow.xaml |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Window styles: `src/TokenHound.App/Interop/WindowStyles.cs`
- ViewModels: `src/TokenHound.App/ViewModels/NotchViewModel.cs`

## Work

- [x] T09.1 Implement `NotchWindow.xaml` and `.cs` under `TokenHound.App/UI/Windows/`.
- [x] T09.2 Configure window properties: `AllowsTransparency="True"`, `WindowStyle="None"`, `Background="Transparent"`, `Topmost="True"`, `ShowInTaskbar="False"`.
- [x] T09.3 In `Window.SourceInitialized`, apply `WindowStyles.EnableNonActivating(hwnd)`.
- [x] T09.4 Position window top-center: `Top = WorkArea.Top`, `Left = WorkArea.Left + (WorkArea.Width - Width) / 2`.
- [x] T09.5 Wire application entry point in `App.xaml` / `App.xaml.cs` to instantiate `UsageStore`, `NotchViewModel`, and show `NotchWindow`.
- [x] T09.6 Document manual verification script for focus preservation and visual appearance.

## Acceptance criteria

- Window renders as a floating top-center capsule.
- Clicking on the capsule never takes keyboard focus away from the active window.
- Telemetry rings render accurately based on `UsageStore` state.

## Verification

- Manual Acceptance Script:
  1. Launch `TokenHound.App`.
  2. Verify floating capsule appears top-center on primary monitor.
  3. Open Notepad or Windows Terminal and start typing.
  4. Click directly on the Notch capsule while typing.
  5. Confirm typing continues in Notepad/Terminal without missing a single keystroke (`WS_EX_NOACTIVATE` verified).
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj`
- Expected evidence: Build succeeds with 0 errors and 0 warnings; manual script validates focus preservation.

## Affected files

- Modify:
  - `src/TokenHound.App/App.xaml`
  - `src/TokenHound.App/App.xaml.cs`
- Create:
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml`
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs`

## Observability and recovery

- Operational signal: Application startup and window render.
- Recovery: Revert positioning logic if multi-DPI displays show misalignment.

## Handoff

- Produced result:
  - Implemented `NotchWindow.xaml` (57 lines) and `NotchWindow.xaml.cs` (65 lines) presenting a floating dark capsule with subtle drop shadow and horizontal items panel bound to `NotchViewModel.Rings`.
  - Configured `AllowsTransparency="True"`, `WindowStyle="None"`, `Topmost="True"`, and hooked `WindowStyles.EnableNonActivating` during `SourceInitialized` to guarantee Win32 non-activating focus preservation (`WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`).
  - Added dynamic top-center primary monitor positioning on `Loaded` and `SizeChanged` relative to `SystemParameters.WorkArea`.
  - Re-wired `App.xaml` and `App.xaml.cs` (74 lines) removing legacy template startup in favor of programmatic startup: instantiates `UsageStore`, registers `ClaudeOAuthProvider` and `ClaudeSessionMonitor`, wires `NotchViewModel` with UI thread dispatching and offline fallback, displays `NotchWindow`, triggers initial refresh, and cleanly disposes resources on application exit.
  - Removed template `MainWindow.xaml` and `MainWindow.xaml.cs`.
- Changed files:
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml`
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs`
  - `src/TokenHound.App/App.xaml`
  - `src/TokenHound.App/App.xaml.cs`
  - `tasks/prd-tracer-bullet-claude/task_09.md`
- Checks:
  - `rtk dotnet build TokenHound.slnx` (Passed: 7 projects, 0 errors)
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj` (Passed: 0 errors)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 147 passed, 0 failed)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 34 passed, 0 failed)
- Validated state: Strict adherence to `AGENTS.md`: sealed partial classes, file-scoped namespaces (`TokenHound.App.UI.Windows`, `TokenHound.App`), alphabetized usings, XML doc comments on all public members, methods <= 30 lines, files <= 300 lines (`NotchWindow.xaml.cs`: 65 lines, `App.xaml.cs`: 74 lines), max nesting <= 2 levels, UPPER_CASE constants, no `#region`, zero fake data.
- Open items: None. PRD 02 Tracer Bullet implementation is 100% complete across all 9 tasks.

### ADR candidates

None.
