# Stable execution context

Load in this exact order:

1. `tasks/prd-system-tray-notifyicon/prd.md`
2. `tasks/prd-system-tray-notifyicon/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T03 — Package, TaskbarIconAdapter, App composition, integration test, MAN-01

## Outcome

The tray ships end to end: the `Hardcodet.NotifyIcon.Wpf 2.0.1` `PackageReference`; the sole `TaskbarIconAdapter` implementation of `ITrayIcon`; `App.InitializeUi` building and owning `NotchVisibilityController` + `TrayIconViewModel` + `TrayIconHost` (registered in `disposableResources`); the `App.ShutdownAsync()` wrapper — `private Task ShutdownAsync()` whose body is `{ _visibility!.AllowClose(); return _lifetime!.ShutdownAsync(); }` — wired as the `HudActionsViewModel` terminal delegate; the five `<Compile Include>` links that make the seam tests compile and execute; an integration test proving `UsageStore` polling continues while the Notch is hidden; the full aggregate validation run; and the PRD 11-step manual acceptance script handed to the coordinator.

## Dependencies and boundaries

- Depends on: T01, T02
- Unblocks: —
- In scope: the two `.csproj` edits and `src/TokenHound.App/App.xaml.cs` (this task is their sole writer, per the write-scope rule); `src/TokenHound.App/UI/Tray/TaskbarIconAdapter.cs`; `tests/TokenHound.Infrastructure.Tests/Engine/NotchHiddenPollingTests.cs`; running the full aggregate validation and handing MAN-01 to the coordinator.
- Out of scope: `ARCHITECTURE.md` and `docs/ROADMAP.md` reconciliation and `graft build` (coordinator — recorded as pending items in `tasks.md`); any change to `TokenHound.Core` / `TokenHound.Infrastructure` source; any change to the T01/T02 seam files; a new `TokenHound.App.Tests` project or a `TokenHound.slnx` edit (DEC-05).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01..06 | `prd.md#outcomes-and-metrics` | The full outcome set, verified by MAN-01 and the aggregate suites |
| FR-01, FR-08 | `prd.md#functional-requirements` | Single icon lifecycle; "Exit" as the sole idempotent shutdown, with the `AllowClose()` bypass |
| FR-10, FR-11 | `prd.md#functional-requirements` | No Notch-close path terminates the app; polling continues while hidden |
| FR-12 | `prd.md#functional-requirements` | The composed `TrayIconHost` keeps the Notch primary if the icon fails |
| NFR-01..11 | `prd.md#non-functional-requirements` | Layer purity confirmed by the linked seam building without `UseWPF`; TFM/`OutputType`/`UseWPF` unchanged; dispatcher marshalling; structured logs; accessible OS menu; multi-frame icon |
| DEC-01, DEC-03, DEC-04, DEC-05, DEC-06, DEC-08 | `techspec.md#technical-decisions` | Pinned package; close bypass; `App.InitializeUi` wiring; `<Compile Include>` links; adapter owns the WPF `ContextMenu` and pack-URI icon |
| CMP-02, CMP-10, CMP-11, CMP-12, CMP-13 | `techspec.md#components-and-flow` | `TaskbarIconAdapter`; `App.xaml.cs` wiring + wrapper; package ref; test links; `NotchHiddenPollingTests` |
| TC-11, TC-15, TC-16 | `techspec.md#test-approach` | Polling-continuity integration test; structural WPF-free compile; manual acceptance MAN-01 |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation` (before every build/test), `repository-cli-efficiency`, `no-workarounds`; `references/mtp.md` for the runner. Launch/verify per AGENTS.md "Running & validating desktop HUD".
- Existing code: `src/TokenHound.App/App.xaml.cs` — `InitializeUi` (`_notchWindow.Show()`, `MainWindow = _notchWindow`), `DispatchUiAction(Action)`, `OnExit` (`_lifetime?.Dispose()`), and the `HudActionsViewModel(usageStore.RefreshNowAsync, _lifetime.ShutdownAsync, showSettings, showAbout, snapshots)` construction (the second positional arg is the terminal delegate — this task swaps it for `ShutdownAsync`). `src/TokenHound.App/ApplicationLifetime.cs` — `ShutdownAsync` idempotent, `DisposeResources()` over the injected `IReadOnlyList<IDisposable>`. `src/TokenHound.App/TokenHound.App.csproj` — the `Serilog 4.4.0` `ItemGroup`; `Assets\logo.ico` as `<Resource>`. `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` — explicit per-file `<Compile Include ... Link="...">` entries. `src/TokenHound.Infrastructure/Engine/UsageStore.cs` — the periodic timer lives in `_lifetime`, with no window reference (basis for TC-11's decoupling assertion).
- Contract or integration: `techspec.md#contracts-and-data` (`TrayIconHost` ctor, `NotchWindow` additions from T01); `techspec.md#integrations-and-interfaces`; `techspec.md#sequencing` steps 6–7; `techspec.md#test-approach` PowerShell block; `prd.md` "Manual acceptance script (Windows 11 interactive desktop)".
- Note: `src/TokenHound.App/App.xaml.cs` is 343 lines (above the 300-line soft cap). If review enforces the cap, extract `src/TokenHound.App/UI/Tray/TrayComposition.cs` (a static factory) and add it to this task's sole-writer set; default is the in-place CMP-10 wiring. See `tasks.md` pending items.

## Work

- [ ] T03.1 `src/TokenHound.App/TokenHound.App.csproj`: add `<PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="2.0.1" />` to the `Serilog` `ItemGroup`. No `TargetFramework` / `OutputType` / `UseWPF` / `ApplicationIcon` change.
- [ ] T03.2 Create `src/TokenHound.App/UI/Tray/TaskbarIconAdapter.cs`: `sealed class TaskbarIconAdapter : ITrayIcon` owning a `Hardcodet.Wpf.TaskbarNotification.TaskbarIcon`; `IconSource = new Uri("pack://application:,,,/Assets/logo.ico")`; build a `ContextMenu` (`HudContextMenuStyle` / `HudMenuItemStyle` from the merged `DialogResources.xaml`) from `TrayMenuDescriptor`, mapping `MenuItem.Click` → `MenuItemInvoked`; map `TrayLeftMouseUp` → `LeftClicked`, `TrayMouseDoubleClick` → `DoubleClicked`, `ContextMenuOpening` → `MenuOpening`; `UpdateToggleHeader` retargets the toggle `MenuItem.Header`; `Dispose()` disposes the `TaskbarIcon` (`NIM_DELETE`), idempotent. This is the only file referencing the package or `System.Windows.Controls`.
- [ ] T03.3 `src/TokenHound.App/App.xaml.cs` `InitializeUi` (after `_notchWindow.Show()`): build `NotchVisibilityController` with `showNotch: () => DispatchUiAction(() => _notchWindow.ShowNotch())`, `hideNotch: () => DispatchUiAction(() => _notchWindow.HideNotch())`, `initiallyVisible: true`; `new TrayIconViewModel(_actionsViewModel, _visibility, new TrayMenuModel())`; `new TrayIconHost(new TaskbarIconAdapter(), trayIconViewModel, DispatchUiAction, new Uri("pack://application:,,,/Assets/logo.ico"), Log.Logger)`; `disposableResources.Add(_trayHost)`; `_trayHost.Initialize()`; set `_notchWindow.ShouldInterceptClose = () => _visibility.ShouldInterceptClose` and `_notchWindow.CloseIntercepted = _visibility.Hide`. Add fields `NotchVisibilityController? _visibility` and `TrayIconHost? _trayHost`.
- [ ] T03.4 `src/TokenHound.App/App.xaml.cs`: change the `HudActionsViewModel` second constructor argument from `_lifetime.ShutdownAsync` to a new `private Task ShutdownAsync()` whose body is `{ _visibility!.AllowClose(); return _lifetime!.ShutdownAsync(); }`.
- [ ] T03.5 `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`: add five `<Compile Include="..\..\src\TokenHound.App\UI\Tray\X.cs" Link="Tray\X.cs" />` entries for `NotchVisibilityController.cs`, `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `TrayIconViewModel.cs`, `TrayIconHost.cs` (not `ITrayIcon.cs` or `TaskbarIconAdapter.cs`).
- [ ] T03.6 Create `tests/TokenHound.Infrastructure.Tests/Engine/NotchHiddenPollingTests.cs` (TC-11): drive a `UsageStore` with a fake provider at a short interval, call `NotchVisibilityController.Hide()`, wait, assert >= 2 poll ticks recorded after the hide and that `NotchVisibilityController` holds no `UsageStore` reference.
- [ ] T03.7 Run the full aggregate command set below, preserving and checking `$LASTEXITCODE` after each; record outputs in the Handoff. Hand MAN-01 to the coordinator for HIL 3.

## Acceptance criteria

- `src/TokenHound.App` and `tests/TokenHound.Infrastructure.Tests` build in Release with no new warning; no `TargetFramework` / `OutputType` / `UseWPF` change.
- All six focused suites (`NotchVisibilityControllerTests`, `TrayMenuModelTests`, `TrayIconViewModelTests`, `TrayIconHostTests`, `HudActionsViewModelTests`, `NotchHiddenPollingTests`) and the aggregate run green with `--minimum-expected-tests 1`; `$LASTEXITCODE` preserved and checked after each; nothing piped to `Out-Null`.
- `NotchHiddenPollingTests` shows polling continues after `Hide()` (TC-11); the test project compiling with the five links and no `UseWPF` is the structural proof (TC-15).
- MAN-01: launch shows exactly one "TokenHound" icon; hide/show works without focus loss; tray "Exit" removes the icon (no ghost) and the log shows "Application shutdown completed successfully." (TC-16).

## Verification

- Unit + integration (aggregate — the `tasks.md` command set from `techspec.md#test-approach`):
  ```
  rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo -v minimal
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchVisibilityControllerTests*"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayMenuModelTests*"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayIconViewModelTests*"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayIconHostTests*"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudActionsViewModelTests*"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchHiddenPollingTests*"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  ```
  Never `Out-Null`. If RTK hides a failure detail, re-run that one command with `rtk proxy dotnet ...`. Equivalent MTP route if `dotnet test` project selection is unavailable: `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*<Name>*"`.
- E2E: omitted by desktop .NET policy; no aggregate suite pulls E2E — the whole-project run above is unit + integration only.
- Manual: MAN-01 — the PRD "Manual acceptance script (Windows 11 interactive desktop)" steps 1–11 (TC-16). Owner: coordinator at HIL 3 — not an executor step. Launch `TokenHound.App` via the Windows MCP `App` tool (`mode="launch_executable"`); verify with `Screenshot` on `display: [2]`. Until it runs, acceptance for FR-01 / FR-03 / FR-05 / FR-08 / FR-09 / NFR-06 / NFR-10 stays pending.
- Environment dependency: .NET SDK 10.0.400 + restored output for the automated set; Windows 11 interactive desktop + existing provider configuration for MAN-01. Implementation on the `main` working tree (D-006); no credential writes; no commit/push/PR without an explicit user request.
- Expected evidence: green Release build of both projects; each focused suite and the aggregate report >= 1 executed test, 0 failed, exit 0; the coordinator's MAN-01 capture showing one icon, no ghost, and "Application shutdown completed successfully.".

## Affected files

Exclusive write scope for this task (sole writer):

- Modify: `src/TokenHound.App/TokenHound.App.csproj`
- Modify: `src/TokenHound.App/App.xaml.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
- Create: `src/TokenHound.App/UI/Tray/TaskbarIconAdapter.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Engine/NotchHiddenPollingTests.cs`
- Contingent (only if review enforces the 300-line cap on `App.xaml.cs`): Create `src/TokenHound.App/UI/Tray/TrayComposition.cs`

## Observability and recovery

- Operational signal: `TrayIconHost` `Information` for `created` (with tooltip) and each action, `Warning` for `creation failed`; `ApplicationLifetime`'s "Application shutdown completed successfully." is the "Exit" acceptance marker; the Notch refresh popup reflects a tray-initiated refresh while the Notch is visible.
- Recovery: revert CMP-08..CMP-14 and drop the `PackageReference` — behaviour returns to "Notch Close = shutdown, no tray". No persisted state, no migration. If a .NET 10 runtime issue hits `Hardcodet 2.0.1`, swap only `TaskbarIconAdapter` for the DEC-01 `Shell_NotifyIcon` P/Invoke wrapper behind `ITrayIcon` — the seam and all tests are library-agnostic.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result:
  - `Hardcodet.NotifyIcon.Wpf 2.0.1` package reference pinned in `src/TokenHound.App/TokenHound.App.csproj` within the Serilog ItemGroup; target frameworks, output type, and WPF enablement preserved unchanged.
  - Implemented `src/TokenHound.App/UI/Tray/TaskbarIconAdapter.cs` (`sealed class TaskbarIconAdapter : ITrayIcon`), wrapping `Hardcodet.Wpf.TaskbarNotification.TaskbarIcon`, building WPF `ContextMenu` with `HudContextMenuStyle` / `HudMenuItemStyle`, mapping item clicks to `MenuItemInvoked`, separators, `TrayLeftMouseUp` -> `LeftClicked`, `TrayMouseDoubleClick` -> `DoubleClicked`, `TrayContextMenuOpen` -> `MenuOpening`, `UpdateToggleHeader`, and idempotent `Dispose()` issuing `NIM_DELETE`.
  - Wired `src/TokenHound.App/App.xaml.cs` `InitializeUi`: instantiated `NotchVisibilityController` (bound to `ShowNotch`/`HideNotch` via `DispatchUiAction`), `TrayIconViewModel`, and `TrayIconHost` (registered in `disposableResources` and initialized); bound `_notchWindow.ShouldInterceptClose` and `CloseIntercepted`; replaced `HudActionsViewModel` close delegate with private `ShutdownAsync()` wrapper calling `_visibility!.AllowClose()` and `_lifetime!.ShutdownAsync()`. Methods kept <= 30 lines.
  - Linked WPF-free seam classes in `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (`ITrayIcon.cs`, `NotchVisibilityController.cs`, `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `TrayIconViewModel.cs`, `TrayIconHost.cs`).
  - Created integration test `tests/TokenHound.Infrastructure.Tests/Engine/NotchHiddenPollingTests.cs` (TC-11) verifying that hiding the Notch does not pause or interrupt `UsageStore` background polling (asserting >= 2 poll ticks recorded after `Hide()`) and asserting via reflection that `NotchVisibilityController` holds no `UsageStore` reference.
  - Executed full aggregate validation suite preserving `$LASTEXITCODE`; 100% green across all six focused suites and whole-project aggregate (606 tests passed, 0 failed, 0 warnings).
  - Handed MAN-01 (PRD steps 1–11) to coordinator for HIL 3 execution.
- Changed files:
  - `src/TokenHound.App/TokenHound.App.csproj`
  - `src/TokenHound.App/UI/Tray/TaskbarIconAdapter.cs`
  - `src/TokenHound.App/App.xaml.cs`
  - `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
  - `tests/TokenHound.Infrastructure.Tests/Engine/NotchHiddenPollingTests.cs`
  - `tasks/prd-system-tray-notifyicon/task_03.md`
- Checks:
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal` -> Exit 0 (0 errors, 0 warnings)
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo -v minimal` -> Exit 0 (0 errors, 0 warnings)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchVisibilityControllerTests*"` -> Exit 0 (8 passed)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayMenuModelTests*"` -> Exit 0 (4 passed)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayIconViewModelTests*"` -> Exit 0 (6 passed)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayIconHostTests*"` -> Exit 0 (5 passed)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudActionsViewModelTests*"` -> Exit 0 (9 passed)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchHiddenPollingTests*"` -> Exit 0 (2 passed)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` -> Exit 0 (606 passed, 0 failed, 0 skipped)
- Validated state:
  - Working tree on `main` (D-006), .NET SDK 10.0.400, Release and Debug configurations fully compilable and passing.
  - No credential writes, no Core / Infrastructure production changes, no unauthorized file mutations.
- Open items:
  - Manual acceptance script MAN-01 (steps 1–11 on Windows 11 desktop via Windows MCP `App` tool and `Screenshot` on `display: [2]`) is handed to coordinator for HIL 3.
  - Coordinator items outside T03 write scope: `ARCHITECTURE.md` folder tree reconciliation (`App.xaml.cs` + `UI/Tray/`), `docs/ROADMAP.md` PRD 11 status update, and `graft build` execution.

### ADR candidates

- `T03-ADR-01`: Link `ITrayIcon.cs` into `TokenHound.Infrastructure.Tests`
  - Title: Link `ITrayIcon.cs` into `TokenHound.Infrastructure.Tests.csproj`
  - Context: Task specification note stated `(not ITrayIcon.cs or TaskbarIconAdapter.cs as they reference WPF)`. However, `ITrayIcon.cs` contains zero WPF or OS dependencies (only pure contracts with `System.Uri`, `System.IDisposable`, and seam records/enums). Both `TrayIconHost.cs` and `TrayIconHostTests.cs` (which implements a `FakeTrayIcon : ITrayIcon`) require `ITrayIcon` to compile in the headless `net10.0` test project without referencing the `TokenHound.App` WinExe project directly (DEC-05).
  - Decision: Link `ITrayIcon.cs` into `TokenHound.Infrastructure.Tests.csproj` alongside the other seam classes (`NotchVisibilityController.cs`, `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `TrayIconViewModel.cs`, `TrayIconHost.cs`).
  - Alternatives: (1) Re-declare `ITrayIcon` inside test project (rejected: violates DRY and allows interface drift); (2) Create `TokenHound.App.Tests` project (rejected by DEC-05 for v1).
  - Consequences: All seam classes and their test suites compile cleanly under `net10.0` without WPF.
  - Evidence: `tests/TokenHound.Infrastructure.Tests` builds cleanly in Release without WPF; all 606 tests pass.
  - TechSpec relationship: Aligns with DEC-05, CMP-01, CMP-12, and TC-15.

- `T03-ADR-02`: Headless `pack://` URI scheme registration via `[ModuleInitializer]` in test assembly
  - Title: Headless `pack://` URI scheme registration via `[ModuleInitializer]` in test assembly
  - Context: In `TokenHound.App` (WPF), the `pack://` URI scheme is registered automatically by WPF at startup. In `TokenHound.Infrastructure.Tests` (`net10.0`, headless without WPF), parsing pack URIs such as `new Uri("pack://application:,,,/Assets/logo.ico")` throws `System.UriFormatException: Invalid URI: Invalid port specified` because `UriParser` does not recognize the custom scheme. `TrayIconHostTests` tests the host with this exact pack URI.
  - Decision: Add a `[ModuleInitializer]` in `NotchHiddenPollingTests.cs` (within the test project) that registers the `pack` scheme via `UriParser.Register(new GenericUriParser(GenericUriParserOptions.GenericAuthority), "pack", -1)` if not already known.
  - Alternatives: (1) Change `TrayIconHostTests.cs` to use file URI (rejected: `TrayIconHostTests.cs` was written in T02 and is outside T03's exclusive write scope); (2) Avoid using pack URI in production (rejected: pack URI is required to load `logo.ico` resource in WPF).
  - Consequences: Headless unit tests can construct and validate pack URIs without loading WPF assemblies or stealing test execution focus.
  - Evidence: `TrayIconHostTests` 5/5 tests green, aggregate suite 606/606 green.
  - TechSpec relationship: Preserves headless testability (NFR-11) and library-agnostic seam testing (DEC-06, TC-01, TC-15).
