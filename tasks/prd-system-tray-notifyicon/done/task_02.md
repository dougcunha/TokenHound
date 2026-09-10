# Stable execution context

Load in this exact order:

1. `tasks/prd-system-tray-notifyicon/prd.md`
2. `tasks/prd-system-tray-notifyicon/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — TrayIconViewModel and TrayIconHost

## Outcome

The tray's WPF-free logic is complete behind the seam:

- One `TrayIconViewModel` turns each `TrayMenuItemKey` into a reused action with no duplicated command body: `ToggleNotch` → `NotchVisibilityController.Toggle()`; `RefreshNow` → `HudActionsViewModel.RefreshAsync()` (its `_isRefreshing` guard satisfies FR-06); `Settings` / `About` → `HudActionsViewModel.ShowSettings()` / `ShowAbout()`; `Exit` → `HudActionsViewModel.ShutdownAsync()`. `ToggleHeader` tracks `VisibilityChanged` and raises `PropertyChanged`; `BuildMenu()` returns the current `TrayMenuDescriptor`.
- One `TrayIconHost : IDisposable` owns the tray lifecycle against `ITrayIcon`: `Initialize()` shows one icon with tooltip "TokenHound" and the current menu, guards a second call, wraps `ITrayIcon.Show` in `try/catch` (logs a structured `Warning` with the exception, sets `IsDegraded = true`, never rethrows — FR-12), routes every tray callback through the injected `Action<Action>` dispatcher (NFR-08), rebuilds the descriptor and pushes the toggle header on `MenuOpening`, and logs one structured entry per action and per `created` / `removed` / `creation failed` event (NFR-09). `Dispose()` is idempotent and disposes the icon (`NIM_DELETE`).

Both files reference no `System.Windows` or `Hardcodet` type.

## Dependencies and boundaries

- Depends on: T01 — seam types (`NotchVisibilityController`, `TrayMenuModel`, `TrayMenuItemKey`, `TrayMenuDescriptor`, `ITrayIcon`) and the renamed `HudActionsViewModel.ShutdownAsync`.
- Unblocks: T03
- In scope: `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs` (CMP-06), `src/TokenHound.App/UI/Tray/TrayIconHost.cs` (CMP-07), and their two test files `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconViewModelTests.cs` and `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconHostTests.cs` with a hand-written `FakeTrayIcon` (records `Show` / `Dispose`, can throw on `Show`) and an `Action<Action>` dispatcher spy.
- Out of scope: `TaskbarIconAdapter` and the `Hardcodet` package (T03 — neither file may reference them); `App` wiring and the `App.ShutdownAsync` wrapper (T03); the `<Compile Include>` links (T03); the WPF `ContextMenu` (T03 adapter); the integration polling test (T03); any change to `HudActionsViewModel`, `NotchVisibilityController`, `TrayMenuModel`, or `ITrayIcon`.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, OBJ-01, US-08 | `prd.md#functional-requirements`, `prd.md#outcomes-and-metrics`, `prd.md#stories-and-journeys` | Icon created once with the resource + tooltip; guarded against double-create/double-dispose |
| FR-03, FR-04 | `prd.md#functional-requirements` | Toggle maps to one transition; header reflects visibility with `PropertyChanged` |
| FR-06 | `prd.md#functional-requirements` | "Refresh Now" reuses `HudActionsViewModel.RefreshAsync` and its concurrency guard |
| FR-07 | `prd.md#functional-requirements` | "Settings…" / "About…" reuse `HudActionsViewModel.ShowSettings` / `ShowAbout` (no-op while shutting down) |
| FR-08 (`*TrayIconViewModelTests*` half), OBJ-04 | `prd.md#functional-requirements`, `prd.md#outcomes-and-metrics` | Only `Exit` reaches the shutdown delegate; idempotent under repeat |
| FR-12 | `prd.md#functional-requirements` | Graceful degradation: warning + continue with the Notch as primary surface |
| OBJ-05, OBJ-06 | `prd.md#outcomes-and-metrics` | Reuse HUD command logic; consistent lifecycle labels |
| NFR-07 | `prd.md#non-functional-requirements` | Icon always removed on exit; `Dispose` idempotent |
| NFR-08 | `prd.md#non-functional-requirements` | Tray click and menu callbacks marshalled to the dispatcher |
| NFR-09 | `prd.md#non-functional-requirements` | Structured Serilog entry per action and per lifecycle event; `nameof` tokens |
| DEC-02 | `techspec.md#technical-decisions` | Compose `HudActionsViewModel` + `NotchVisibilityController` + `TrayMenuModel`; no duplicated bodies |
| DEC-04, DEC-06, DEC-07 | `techspec.md#technical-decisions` | WPF-free coordinator; linkable; `try/catch` fallback with `IsDegraded` |
| CMP-06, CMP-07 | `techspec.md#components-and-flow` | `TrayIconViewModel` and `TrayIconHost` responsibilities and members |
| TC-01, TC-04, TC-06, TC-07, TC-08, TC-12, TC-13, TC-14 | `techspec.md#test-approach` | Init/idempotent/dispose; header + `PropertyChanged`; refresh guard; settings/about before/after Exit; Exit-once; fallback path; dispatcher spy; logging |
| NFR-01, NFR-05, NFR-11 | `prd.md#non-functional-requirements` | WPF-free; event-driven, no timer; headless-testable with `FakeTrayIcon` |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`.
- Existing code:
  - `techspec.md#contracts-and-data` — the `TrayIconViewModel` block (`Invoke(TrayMenuItemKey)` single `switch`, `ToggleHeader`, `BuildMenu()`, `ToggleNotch`, `RefreshNow`, `ShowSettings`, `ShowAbout`, `Exit`) and the `TrayIconHost` block (ctor `(ITrayIcon trayIcon, TrayIconViewModel viewModel, Action<Action> dispatch, Uri iconSource, ILogger logger)`, `bool IsDegraded`, `void Initialize()`, `void Dispose()`) are authoritative.
  - `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` — `RefreshAsync` reused as-is; its `_isRefreshing` / `_isShuttingDown` guard under `_syncLock` satisfies FR-06; after `ShutdownAsync()` the latch makes `RefreshNow` / `ShowSettings` / `ShowAbout` no-ops and `Exit` returns the cached task.
  - `src/TokenHound.App/App.xaml.cs` `DispatchUiAction(Action)` — the dispatcher shape passed in by T03.
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs` — the `TaskCompletionSource` delegate-injection pattern for the refresh-guard test and the Serilog test-sink precedent.
- Contract or integration: `techspec.md#components-and-flow` CMP-06 / CMP-07; `techspec.md#errors-security-and-recovery` — second `Initialize()` is a no-op; icon removed exactly once (`TrayIconHost.Dispose` idempotent + adapter `Dispose` idempotent).

## Work

### TrayIconViewModel

- [ ] T02.1 Create `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs`: `sealed class TrayIconViewModel : INotifyPropertyChanged`; ctor `(HudActionsViewModel hudActions, NotchVisibilityController visibility, TrayMenuModel menu)` with null-checked args; file-scoped namespace `TokenHound.App.UI.Tray`; XML docs.
- [ ] T02.2 `void Invoke(TrayMenuItemKey key)` — one `switch` expression/statement mapping each key to `ToggleNotch()` / `RefreshNow()` / `ShowSettings()` / `ShowAbout()` / `Exit()`; no command logic inline.
- [ ] T02.3 `ToggleNotch()` → `visibility.Toggle()`; `Task RefreshNow()` → `hudActions.RefreshAsync()`; `ShowSettings()` / `ShowAbout()` → the matching `hudActions` calls; `Task Exit()` → `hudActions.ShutdownAsync()`.
- [ ] T02.4 `string ToggleHeader` => `TrayMenuModel.ResolveToggleHeader(visibility.IsNotchVisible)`; subscribe `visibility.VisibilityChanged` in the ctor and raise `PropertyChanged(nameof(ToggleHeader))` on each transition; `TrayMenuDescriptor BuildMenu()` => `new TrayMenuDescriptor { Entries = menu.BuildDescriptor(visibility.IsNotchVisible) }`.
- [ ] T02.5 Emit one structured Serilog entry per invoked action with a `nameof` state token (NFR-09), consistent with `TrayIconHost`'s logging: the ViewModel logs the semantic action, the Host logs lifecycle + dispatch.
- [ ] T02.6 Create `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconViewModelTests.cs`: TC-04 (header string per state; `PropertyChanged(nameof(ToggleHeader))` on toggle; `BuildMenu()` reflects the new header), TC-06 (two `RefreshNow()` while the injected refresh `TaskCompletionSource` is pending → one underlying dispatch), TC-07 (`ShowSettings` / `ShowAbout` call the matching delegate once before `Exit()`, no-op after), TC-08 (`Invoke(Exit)` twice → one shutdown-delegate call; every other key never reaches it), TC-14 (one structured entry per action via a Serilog test sink).

### TrayIconHost

- [ ] T02.7 Create `src/TokenHound.App/UI/Tray/TrayIconHost.cs`: `sealed class TrayIconHost : IDisposable`; ctor stores null-checked deps; `bool IsDegraded { get; private set; }`; `_initialized` / `_disposed` guard fields; file-scoped namespace; XML docs.
- [ ] T02.8 `Initialize()`: return if `_initialized`; set `_initialized`; `try { _trayIcon.Show(_iconSource, TrayMenuModel.TOOLTIP, _viewModel.BuildMenu()); _logger.Information(... "created" ..., TrayMenuModel.TOOLTIP); } catch (Exception ex) { _logger.Warning(ex, ... nameof context ...); IsDegraded = true; return; }`; then subscribe the interface events.
- [ ] T02.9 Subscribe: `LeftClicked` and `DoubleClicked` → `_dispatch(() => _viewModel.ToggleNotch())`; `MenuItemInvoked` → `_dispatch(() => _viewModel.Invoke(key))`; `MenuOpening` → `_dispatch(() => _trayIcon.UpdateToggleHeader(_viewModel.ToggleHeader))` after rebuilding the descriptor. Keep nesting <= 3; mark stateless lambdas `static` where possible.
- [ ] T02.10 Log one structured entry per action (`nameof` state token) and per lifecycle event (`created`, `removed`, `creation failed`); no hard-coded state strings.
- [ ] T02.11 `Dispose()`: return if `_disposed`; set `_disposed`; unsubscribe the events; `_trayIcon.Dispose()`; `_logger.Information(... "removed" ...)`.
- [ ] T02.12 Create `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconHostTests.cs` with `FakeTrayIcon` + dispatcher spy: TC-01 (`Show` called once with the `logo.ico` URI and tooltip "TokenHound"; second `Initialize()` a no-op; `Dispose()` disposes the icon once; second `Dispose()` safe), TC-12 (`FakeTrayIcon` throws on `Show` → no exception propagates, exactly one `Warning` carrying the exception, `IsDegraded == true`, no further wiring), TC-13 (every click/menu callback body runs inside the dispatcher spy — invocation counts match), TC-14 (one structured entry per `TrayMenuItemKey` and per `created` / `removed` / `creation failed` via a Serilog test sink).

## Acceptance criteria

- Each `TrayMenuItemKey` invokes exactly its mapped action and nothing else; only `Exit` reaches the shutdown delegate, and only once under repeat.
- Concurrent `RefreshNow()` produces a single underlying refresh (guard reused from `HudActionsViewModel`, not reimplemented). After `Exit()` the latch makes `RefreshNow` / `ShowSettings` / `ShowAbout` no-ops.
- `ToggleHeader` is "Hide Notch" when the Notch is visible and "Show Notch" when hidden, with `PropertyChanged` raised on every transition; `BuildMenu()` reflects the current header.
- `Initialize()` calls `ITrayIcon.Show` exactly once; a second call is a no-op; `Dispose()` disposes the icon once and a second `Dispose()` is safe.
- A throwing `Show` yields no propagated exception, exactly one `Warning` carrying the exception, `IsDegraded == true`, and no event subscriptions.
- Every click and menu callback is invoked inside the injected `Action<Action>` dispatcher.
- Each `TrayMenuItemKey` and each of `created` / `removed` / `creation failed` produces exactly one structured Serilog entry with `nameof` tokens.
- Both files reference no `System.Windows` / `Hardcodet` type; one sealed class per file; <= 300 / <= 30 / <= 3; `nameof` over literals.

## Verification

- Unit: `*TrayIconViewModelTests*` (TC-04, TC-06, TC-07, TC-08, TC-14) and `*TrayIconHostTests*` (TC-01, TC-12, TC-13, TC-14) — authored here; first executed under T03 once CMP-12 links `TrayIconViewModel.cs` and `TrayIconHost.cs`.
- Integration: none (the polling-continuity test is T03).
- E2E: omitted by desktop .NET policy.
- Manual: none here. MAN-01 steps 1–2, 5–8, 10–11 run under T03 (coordinator).
- Commands:
  ```
  rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayIconViewModelTests*"
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayIconHostTests*"
  ```
  Preserve `$LASTEXITCODE` after each command; never `Out-Null`. The build passes here; the two `--filter-class` runs report zero matched tests until T03 links the sources — expected, not a pass — and T03 re-runs both with `--minimum-expected-tests 1`.
- Environment dependency: .NET SDK 10.0.400 and restored build output.
- Expected evidence: green `src/TokenHound.App` Release build; `TrayIconViewModel.cs`, `TrayIconHost.cs`, `TrayIconViewModelTests.cs`, and `TrayIconHostTests.cs` present; `FakeTrayIcon` lives only in the test file/folder.

## Affected files

Exclusive write scope for this task (sole writer):

- Create: `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs`
- Create: `src/TokenHound.App/UI/Tray/TrayIconHost.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconViewModelTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconHostTests.cs`

## Observability and recovery

- Operational signal: `TrayIconViewModel` — one structured Serilog entry per invoked tray action with a `nameof` state token. `TrayIconHost` — `Information` for `created` (with tooltip) and each action, `Warning` for `creation failed` with the exception, `Information` for `removed`.
- Recovery: delete the four files; `App` (T03) does not yet reference `TrayIconViewModel` or `TrayIconHost`.

## Handoff

> Updated by `sdd-execute-task` during implementation (D-006: `main` working tree, no commit/stage/PR).

### Produced result

`TrayIconViewModel` and `TrayIconHost` are fully implemented behind the WPF-free seam, and their headless test suites `TrayIconViewModelTests` and `TrayIconHostTests` are authored:
- `TrayIconViewModel` maps each `TrayMenuItemKey` to its reused action with zero duplicated logic (`ToggleNotch` -> `NotchVisibilityController.Toggle()`, `RefreshNow` -> `HudActionsViewModel.RefreshAsync()`, `Settings`/`About` -> `HudActionsViewModel.ShowSettings()`/`ShowAbout()`, `Exit` -> `HudActionsViewModel.ShutdownAsync()`). `ToggleHeader` updates on `VisibilityChanged` and raises `PropertyChanged(nameof(ToggleHeader))`. `BuildMenu()` constructs a `TrayMenuDescriptor` reflecting current visibility. Structured Serilog entries are emitted for each invoked action with `nameof` tokens.
- `TrayIconHost` coordinates the notification icon lifecycle against `ITrayIcon`. `Initialize()` displays the icon with tooltip "TokenHound" and initial menu descriptor, guarded against repeated calls. Failures during `Show` degrade gracefully (`IsDegraded = true`, structured `Warning` with exception logged, no throw). All icon callbacks (`LeftClicked`, `DoubleClicked`, `MenuItemInvoked`, `MenuOpening`) are marshalled through the injected `Action<Action>` dispatcher. `Dispose()` unsubscribes events, disposes the icon, and logs removal idempotently.
- `TrayIconViewModelTests` covers TC-04, TC-06, TC-07, TC-08, TC-14.
- `TrayIconHostTests` covers TC-01, TC-12, TC-13, TC-14 using `FakeTrayIcon` and `TestLogSink`.
- `src/TokenHound.App` builds clean in Release (exit code 0, 0 errors, 0 warnings).

### Changed files (4, exclusive write scope)

Created:
- `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs` — `sealed class TrayIconViewModel : INotifyPropertyChanged` (167 lines).
- `src/TokenHound.App/UI/Tray/TrayIconHost.cs` — `sealed class TrayIconHost : IDisposable` (143 lines).
- `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconViewModelTests.cs` — 6 test methods covering TC-04, TC-06, TC-07, TC-08, TC-14 (243 lines).
- `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconHostTests.cs` — 5 test methods covering TC-01, TC-12, TC-13, TC-14 with `FakeTrayIcon` and `TestLogSink` (291 lines).

### Commands run (chronological; `$LASTEXITCODE` preserved and checked after each; nothing piped to `Out-Null`)

1. `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal` -> exit 0 ("3 projects, 0 errors, 0 warnings") (baseline check before new files).
2. `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal` -> exit 0 ("4 projects, 0 errors, 0 warnings") (after creating `TrayIconViewModel.cs` and `TrayIconHost.cs`).
3. `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal` -> exit 0 ("3 projects, 0 errors, 0 warnings") (after creating both test files).
4. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayIconViewModelTests*"` -> exit 1 (Zero tests ran, expected until T03 links the sources).
5. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayIconHostTests*"` -> exit 1 (Zero tests ran, expected until T03 links the sources).

### Validated state

- Code / diff: `git status` shows `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs`, `src/TokenHound.App/UI/Tray/TrayIconHost.cs`, `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconViewModelTests.cs`, `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconHostTests.cs` created. All files <= 300 lines, methods <= 30 lines, nesting <= 3 levels.
- Configuration: No `.csproj` or configuration files modified.
- Projects: `src/TokenHound.App` (`net10.0-windows`, WPF, `WinExe`) builds Release with 0 errors and 0 warnings.
- Seam purity: Neither `TrayIconViewModel.cs` nor `TrayIconHost.cs` references any `System.Windows` or `Hardcodet` type.
- Environment: .NET SDK 10.0.400, MTP test runner, Windows 11.

### Open items

- Test project compile links: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` does not yet contain `<Compile Include>` links for the `Tray/` seam files (`NotchVisibilityController.cs`, `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `TrayIconViewModel.cs`, `TrayIconHost.cs`). T03 (CMP-12) will add these links and run all test suites with `--minimum-expected-tests 1`.
- Package and UI Adapter wiring: `Hardcodet.NotifyIcon.Wpf` package reference, `TaskbarIconAdapter`, and `App.xaml.cs` tray initialization will be wired in T03.
- Manual verification: MAN-01 (11-step manual script) will be executed under T03 / HIL 3.

### ADR candidates

None - direct TechSpec implementation of CMP-06, CMP-07, DEC-02, DEC-04, DEC-06, DEC-07, and NFR-09.
