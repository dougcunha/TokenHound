# Stable execution context

Load in this exact order:

1. `tasks/prd-system-tray-notifyicon/prd.md`
2. `tasks/prd-system-tray-notifyicon/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Tray seam, Notch hide, and HudActionsViewModel rename

## Outcome

The WPF-free tray seam exists and is proven headlessly, the Notch's "Close" path becomes a hide, and `HudActionsViewModel`'s terminal member names its real meaning:

- The `ITrayIcon` boundary, `TrayMenuItemKey`, `TrayMenuModel` (with `TrayMenuEntry` / `TrayMenuDescriptor` records) carrying the exact menu order and `UPPER_CASE` copy constants, and `NotchVisibilityController` — a visibility state machine that invokes exactly one show/hide delegate per transition, raises `VisibilityChanged`, and holds the one-way close-bypass flag — all compile inside `src/TokenHound.App` with no `System.Windows`, `Hardcodet`, or OS dependency.
- `NotchWindow`'s context-menu item reads "Hide"; `OnCloseClick` and every `Window.Closing` (menu, `Alt+F4`, programmatic, OS-initiated) route to an injected hide delegate with `e.Cancel = true`; the single `NotchWindow` instance — its HWND, the `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST` styling, and the `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` hook — is preserved for a later re-show. `ShowNotch()` re-applies persisted placement without activating; `HideNotch()` hides.
- `HudActionsViewModel`'s terminal member is renamed to shutdown semantics with identical idempotent-latch behaviour (`closeAction`→`shutdownAction`, `_closeAction`→`_shutdownAction`, `_closeTask`→`_shutdownTask`, `_isClosing`→`_isShuttingDown`, `IsClosing`→`IsShuttingDown`, `CloseAsync`→`ShutdownAsync`, XML docs), reachable only from tray "Exit". No logic, method-count, or signature-shape change; the three affected tests are renamed and stay green with the same assertions.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02, T03
- In scope: the four seam source files under `src/TokenHound.App/UI/Tray/` and their two headless test files; menu order/headers/constants; visibility transitions and `AllowClose()` / `ShouldInterceptClose`. The `NotchWindow.xaml` header relabel and `NotchWindow.xaml.cs` intercept + `ShowNotch`/`HideNotch` + delegate fields. The rename across `HudActionsViewModel.cs` and the three affected tests in `HudActionsViewModelTests.cs`.
- Out of scope: any WPF or `Hardcodet` reference; `TaskbarIconAdapter` (T03); `TrayIconViewModel` (T02); `TrayIconHost` (T02); `App` wiring and the `<Compile Include>` links (T03); binding the two `NotchWindow` delegates to `NotchVisibilityController` (T03, `App.xaml.cs`); the `App.ShutdownAsync` bypass wrapper (T03); any behaviour change in `HudActionsViewModel`; `RefreshAsync` / `ShowSettings` / `ShowAbout` bodies; `ShowInTaskbar`, window-style, or placement-algorithm changes; anything under `docs/design/`.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-02 | `prd.md#functional-requirements` | Tray menu contents and fixed order |
| FR-03, FR-04 | `prd.md#functional-requirements` | One transition per toggle; header string per state |
| FR-05, NFR-06 | `prd.md#functional-requirements`, `prd.md#non-functional-requirements` | Re-show via `ApplyPlacement`, never `Activate`; non-activating styles and hook survive hide/show; the show path exposes no activate/focus delegate |
| FR-08, OBJ-04, NFR-07 | `prd.md#functional-requirements`, `prd.md#outcomes-and-metrics`, `prd.md#non-functional-requirements` | "Exit" is the sole, idempotent coordinated shutdown; the member is honestly named for it |
| FR-09, FR-10, OBJ-02, OBJ-03, OBJ-06 | `prd.md#functional-requirements`, `prd.md#outcomes-and-metrics` | Notch "Close" relabelled "Hide"; any close request hides and preserves the instance; `ShouldInterceptClose` one-way; consistent labels |
| A-03 | `prd.md#explicit-assumptions` | Repurposing `HudActionsViewModel`'s second delegate is a rename, not a behaviour change |
| DEC-02 | `techspec.md#technical-decisions` | Keep `HudActionsViewModel` behaviourally intact; rename only the terminal member (full rename, settled at HIL 2 / D-005) |
| DEC-03, DEC-04, DEC-08 | `techspec.md#technical-decisions` | Intercept in `NotchWindow.Closing`; `e.Cancel = true` + hide; never recreate the window; close-interception flag + `AllowClose()`; `UI/Tray` namespace/layout; `UPPER_CASE` copy constants |
| CMP-01, CMP-03, CMP-04, CMP-05 | `techspec.md#components-and-flow` | `ITrayIcon`, `NotchVisibilityController`, `TrayMenuItemKey`, `TrayMenuModel` |
| CMP-08, CMP-14 | `techspec.md#components-and-flow` | Behaviour-preserving rename in the class and in the 3 affected tests |
| CMP-09 | `techspec.md#components-and-flow` | `NotchWindow.xaml` relabel + code-behind intercept + `ShowNotch`/`HideNotch` + delegates |
| TC-02, TC-03, TC-04, TC-05, TC-09, TC-10 | `techspec.md#test-approach` | Menu descriptor, transitions, header, show-spy, hide path, bypass flag |
| TC-08 (`*HudActionsViewModelTests*` half) | `techspec.md#test-approach` | Shutdown delegate runs once under repeat; no other path reaches it |
| NFR-01, NFR-02, NFR-11 | `prd.md#non-functional-requirements` | WPF-free, one sealed class per file, headless tests |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation` (before any build/test), `repository-cli-efficiency`, `no-workarounds`. Read `docs/design/` before touching HUD geometry — this task must not.
- Existing code:
  - `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` — the delegate-injection precedent the seam mirrors; `CloseAsync()` (idempotent `_closeTask` latch, sets `_isClosing`, invokes the injected delegate, `OnPropertyChanged(nameof(IsClosing))`), `IsClosing` property, constructor's second parameter `closeAction`.
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs` — the test style (xUnit v3, AwesomeAssertions, NSubstitute); `viewModel.CloseAsync()` in `RefreshAsync_WhenCanceledDuringClosing_DoesNotSetErrorStatus` and `CloseAsync_GuardsRepeatedCalls_AndPreventsSubsequentActions`, `viewModel.IsClosing.Should().BeTrue()` in both, plus the required-argument-null constructor test that names the parameter.
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml` — `CloseMenuItem` with `Header="Close"`, `x:Name="CloseMenuItem"`, `Click="OnCloseClick"`. `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` — `OnCloseClick` (currently `_ = _actionsViewModel?.CloseAsync();`), `OnSourceInitialized` sets the non-activating styles + `WndProc` hook, `ApplyPlacement()` (private, stays private) restores the persisted position; the file is 247 lines.
- Contract or integration: `techspec.md#contracts-and-data` — the C# seam block is authoritative for every signature; `NotchWindow` additions (`Func<bool>? ShouldInterceptClose`, `Action? CloseIntercepted`, `void ShowNotch()`, `void HideNotch()`) are all set/called only from `App.xaml.cs` (T03). "rename only. Callers to update: `App.xaml.cs` (constructor arg — T03), `NotchWindow.xaml.cs` (no longer calls it — this task), `HudActionsViewModelTests.cs` (this task)." The positional constructor call in `App.xaml.cs` still compiles after the member rename.

## Work

Follow this order — it keeps `src/TokenHound.App` buildable at every step and each owned file under one writer.

### Step A — seam DTOs and state machine

- [ ] T01.1 Create `src/TokenHound.App/UI/Tray/ITrayIcon.cs`: `interface ITrayIcon : IDisposable` with `void Show(Uri iconSource, string tooltip, TrayMenuDescriptor menu)`, `void UpdateToggleHeader(string header)`, events `LeftClicked` / `DoubleClicked` / `MenuOpening` (`EventHandler`) and `MenuItemInvoked` (`EventHandler<TrayMenuItemKey>`). BCL types only; XML docs; file-scoped namespace `TokenHound.App.UI.Tray`.
- [ ] T01.2 Create `TrayMenuItemKey.cs`: `enum TrayMenuItemKey { ToggleNotch, RefreshNow, Settings, About, Exit }`.
- [ ] T01.3 Create `TrayMenuModel.cs`: `UPPER_CASE` constants `TOOLTIP = "TokenHound"`, `HIDE_NOTCH_HEADER = "Hide Notch"`, `SHOW_NOTCH_HEADER = "Show Notch"`, `REFRESH_HEADER = "Refresh Now"`, `SETTINGS_HEADER = "Settings…"`, `ABOUT_HEADER = "About…"`, `EXIT_HEADER = "Exit"`; `sealed record TrayMenuEntry { required TrayMenuItemKey Key; required string Header; bool PrecededBySeparator; }` and `sealed record TrayMenuDescriptor { required IReadOnlyList<TrayMenuEntry> Entries; }` (nested in this file); `IReadOnlyList<TrayMenuEntry> BuildDescriptor(bool notchVisible)` returning `ToggleNotch, RefreshNow, Settings, About, [separator] Exit` in fixed order with `Exit.PrecededBySeparator == true` and every other entry `false`; `static string ResolveToggleHeader(bool notchVisible)` → `HIDE_NOTCH_HEADER` / `SHOW_NOTCH_HEADER`.
- [ ] T01.4 Create `NotchVisibilityController.cs`: ctor `(Action showNotch, Action hideNotch, bool initiallyVisible = true)` (null-checked); `bool IsNotchVisible` (single source of truth); `Show()` / `Hide()` — no-op when already in that state, else exactly one delegate call + one `VisibilityChanged`; `Toggle()` — exactly one transition; `bool ShouldInterceptClose => !_shutdownRequested`; `void AllowClose()` — one-way, sets `_shutdownRequested`; `event EventHandler? VisibilityChanged`; nesting <= 1; expose no activate/focus delegate.
- [ ] T01.5 Create `tests/TokenHound.Infrastructure.Tests/Tray/NotchVisibilityControllerTests.cs` (TC-03, TC-05, TC-09, TC-10) and `tests/TokenHound.Infrastructure.Tests/Tray/TrayMenuModelTests.cs` (TC-02, TC-04) using spy `Action` delegates and counters; assert exactly-once invocation per transition, no-op on redundant `Show()`/`Hide()`, header strings for both states, descriptor order/separator/headers, and one-way `AllowClose()`.

### Step B — Notch close becomes hide

- [ ] T01.6 `NotchWindow.xaml`: change `CloseMenuItem` `Header="Close"` to `Header="Hide"`; keep `x:Name="CloseMenuItem"` and `Click="OnCloseClick"`; no other XAML change.
- [ ] T01.7 `NotchWindow.xaml.cs`: add `public Func<bool>? ShouldInterceptClose;` and `public Action? CloseIntercepted;` with XML docs.
- [ ] T01.8 Change `OnCloseClick` to invoke `CloseIntercepted?.Invoke();` and drop the `_actionsViewModel?.CloseAsync()` call — this removes the last non-test caller of the member renamed in Step C.
- [ ] T01.9 Subscribe `Closing += OnClosing` in the constructor; implement `OnClosing`: `if (ShouldInterceptClose?.Invoke() == true) { e.Cancel = true; CloseIntercepted?.Invoke(); }` (nesting <= 3, method <= 30 lines).
- [ ] T01.10 Add `public void ShowNotch()` => `Show(); ApplyPlacement();` (no `Activate()`); `public void HideNotch()` => `Hide();`. Put `=>` on the next line for any expression-bodied member.
- [ ] T01.11 Confirm `NotchWindow.xaml.cs` stays <= 300 lines, methods <= 30, nesting <= 3, alphabetised usings, file-scoped namespace.

### Step C — HudActionsViewModel rename

- [ ] T01.12 In `HudActionsViewModel.cs`: rename constructor parameter `closeAction` → `shutdownAction`, field `_closeAction` → `_shutdownAction`, `_closeTask` → `_shutdownTask`, `_isClosing` → `_isShuttingDown`, property `IsClosing` → `IsShuttingDown`, method `CloseAsync` → `ShutdownAsync`, and every `nameof(IsClosing)` → `nameof(IsShuttingDown)`. Update XML docs to shutdown wording. No control-flow edit.
- [ ] T01.13 In `HudActionsViewModelTests.cs`: rename the same references in the three affected tests and their doc comments (`CloseAsync_GuardsRepeatedCalls_AndPreventsSubsequentActions` → `ShutdownAsync_GuardsRepeatedCalls_AndPreventsSubsequentActions`, `viewModel.CloseAsync()` → `viewModel.ShutdownAsync()`, `viewModel.IsClosing` → `viewModel.IsShuttingDown`, the null-arg case's parameter name). Keep every assertion and the guard semantics.
- [ ] T01.14 Confirm `RefreshAsync` / `ShowSettings` / `ShowAbout` still early-return or no-op while `_isShuttingDown` (unchanged) and the latch still caches the first task.

## Acceptance criteria

- `BuildDescriptor(true)` yields exactly `ToggleNotch, RefreshNow, Settings, About, Exit` with `Exit.PrecededBySeparator == true`, all others `false`, and every header equal to its copy constant; `BuildDescriptor(false)` differs only in the toggle header.
- On a controller seeded visible: `Toggle()` → `IsNotchVisible == false` + one `hideNotch` + one `VisibilityChanged`; a second `Toggle()` reverses via one `showNotch`; `Hide()` while already hidden invokes no delegate and raises no event.
- `ShouldInterceptClose` is `true` until `AllowClose()`, then permanently `false`; the controller exposes no delegate that activates or focuses a window.
- The Notch context menu's last item reads "Hide"; clicking it invokes `CloseIntercepted` and never `ApplicationLifetime.ShutdownAsync` or any `HudActionsViewModel` terminal member. With `ShouldInterceptClose` returning `true`, any `Window.Closing` is cancelled (`e.Cancel == true`) and `CloseIntercepted` runs once; with it `null` or returning `false`, the close proceeds uncancelled.
- `ShowNotch()` calls `Show()` then `ApplyPlacement()` and never `Activate()`; the HWND, the non-activating extended styles, and the `WM_MOUSEACTIVATE` hook survive a hide/show cycle (the window is never recreated).
- `ShutdownAsync()` is idempotent — repeated calls return the cached task and run the injected delegate once; `IsShuttingDown` flips `true` on the first call; `RefreshAsync` / `ShowSettings` / `ShowAbout` are inert afterwards. No symbol named `CloseAsync`, `IsClosing`, `_closeTask`, `_closeAction`, `_isClosing`, or `closeAction` remains in `HudActionsViewModel.cs` or `HudActionsViewModelTests.cs`.
- The four seam files compile in `src/TokenHound.App` with no `System.Windows` / `Hardcodet` / OS reference; one sealed class per file; <= 300 lines/file, <= 30 lines/method, <= 3 nesting levels; `nameof` over literals; alphabetised usings; file-scoped namespace. `NotchWindow.xaml.cs` is <= 300 lines.
- `src/TokenHound.App` builds in Release; `*HudActionsViewModelTests*` runs green with the same executed-test count as before the rename.

## Verification

- Unit: `*HudActionsViewModelTests*` — the three renamed tests plus the untouched ones, all green (TC-08 `*HudActionsViewModelTests*` half). Runnable in this task: both files are already compiled into the test project. `NotchVisibilityControllerTests` (TC-03, TC-05, TC-09, TC-10) and `TrayMenuModelTests` (TC-02, TC-04) — authored here; first executed under T03 once CMP-12 adds the `<Compile Include>` links for `NotchVisibilityController.cs`, `TrayMenuModel.cs`, and `TrayMenuItemKey.cs`.
- Integration: none.
- E2E: omitted by desktop .NET policy.
- Manual: none directly. Covered later by MAN-01 / TC-16 (coordinator, under T03): steps 3 (Hide keeps the app running), 4 (re-show at the persisted position, keyboard focus retained), 9 (the menu's last item reads "Hide" and clicking it hides without quitting), 10 (tray "Exit" is the only shutdown).
- Commands:
  ```
  rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudActionsViewModelTests*"
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*NotchVisibilityControllerTests*"
  rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*TrayMenuModelTests*"
  ```
  The build and `*HudActionsViewModelTests*` must pass in this task. Preserve `$LASTEXITCODE` after every command; never `Out-Null`. The `*NotchVisibilityControllerTests*` and `*TrayMenuModelTests*` runs report zero matched tests until T03 links the sources — that is expected here and is not a pass on its own; T03 re-runs both with `--minimum-expected-tests 1`.
- Environment dependency: .NET SDK 10.0.400 and restored build output.
- Expected evidence: green `src/TokenHound.App` Release build; MTP reports `HudActionsViewModelTests` executing >= 1 test, 0 failed; `git diff` shows the "Close" → "Hide" relabel, the two `NotchWindow` delegate fields, the `OnClosing` handler, `ShowNotch`/`HideNotch`, and the full `HudActionsViewModel` rename; the four seam files and the two new test files present under `src/TokenHound.App/UI/Tray/` and `tests/TokenHound.Infrastructure.Tests/Tray/`.

## Affected files

Exclusive write scope for this task (sole writer):

- Create: `src/TokenHound.App/UI/Tray/ITrayIcon.cs`
- Create: `src/TokenHound.App/UI/Tray/TrayMenuItemKey.cs`
- Create: `src/TokenHound.App/UI/Tray/TrayMenuModel.cs`
- Create: `src/TokenHound.App/UI/Tray/NotchVisibilityController.cs`
- Modify: `src/TokenHound.App/UI/Windows/NotchWindow.xaml`
- Modify: `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs`
- Modify: `src/TokenHound.App/ViewModels/HudActionsViewModel.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Tray/NotchVisibilityControllerTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Tray/TrayMenuModelTests.cs`

## Observability and recovery

- Operational signal: none in the seam or `NotchWindow` itself; `TrayIconHost` (T02) and `App` (T03) emit the Serilog entries for hide/show/exit.
- Recovery: delete the six new files; revert `NotchWindow.xaml` / `NotchWindow.xaml.cs` (restores "Close" = `CloseAsync` = shutdown) and `HudActionsViewModel.cs` / `HudActionsViewModelTests.cs` (restores the `CloseAsync` / `IsClosing` names). Nothing else references the seam types until T02.

## Handoff

> Updated by `sdd-execute-task` during implementation (D-006: `main` working tree, no commit/stage/PR).

### Produced result

The WPF-free tray seam exists and its two headless test suites are authored; the Notch "Close"
path is now a hide; and `HudActionsViewModel`'s terminal member is renamed to shutdown semantics
with identical behaviour. `src/TokenHound.App` builds clean in Release. The pre-existing
`*HudActionsViewModelTests*` suite runs green (9/9) with the same executed-test count as before
the rename. The two new `tests/.../Tray/*Tests.cs` files intentionally do not compile into the
test project yet — T03 (CMP-12) owns the `<Compile Include>` links that make the seam sources
visible there.

### Changed files (10, exclusive write scope)

Created:
- `src/TokenHound.App/UI/Tray/ITrayIcon.cs` — seam boundary; BCL types only; XML docs on every member; no `using` directives (ImplicitUsings covers `System` / `System.Collections.Generic`).
- `src/TokenHound.App/UI/Tray/TrayMenuItemKey.cs` — `enum { ToggleNotch, RefreshNow, Settings, About, Exit }`, fixed order, XML doc on the enum and each member.
- `src/TokenHound.App/UI/Tray/TrayMenuModel.cs` — `sealed class TrayMenuModel` with the `UPPER_CASE` copy constants (`SETTINGS_HEADER` / `ABOUT_HEADER` use the literal `…` escape, not the glyph), instance `IReadOnlyList<TrayMenuEntry> BuildDescriptor(bool)` (collection expression, `Exit.PrecededBySeparator == true`, all others `false`), `static string ResolveToggleHeader(bool)`; plus top-level `sealed record TrayMenuEntry` and `sealed record TrayMenuDescriptor` co-located in the same file (see T01-ADR-01).
- `src/TokenHound.App/UI/Tray/NotchVisibilityController.cs` — `sealed class`; ctor `(Action showNotch, Action hideNotch, bool initiallyVisible = true)` with `ArgumentNullException.ThrowIfNull` on both delegates; `IsNotchVisible` seeded from `initiallyVisible`; `ShouldInterceptClose => !_shutdownRequested` (expression-bodied, `=>` on next line); `Show()` / `Hide()` no-op when already in state else one delegate + one `VisibilityChanged`; `Toggle()` one transition; `AllowClose()` one-way latch; nesting <= 1; no activate/focus delegate.
- `tests/TokenHound.Infrastructure.Tests/Tray/NotchVisibilityControllerTests.cs` — 8 `[Fact]`s covering TC-03 (toggle alternation + redundant `Show`/`Hide` no-ops), TC-05 (show-from-hidden spy once; constructor injects only `showNotch`/`hideNotch`/`initiallyVisible`), TC-09 (`Action closeIntercepted = sut.Hide` flips via the hide spy once), TC-10 (`ShouldInterceptClose` one-way; intercept branch invokes hide spy once). Spy `Action` delegates + `int` counters; `VisibilityChanged += (_, _) => eventCount++`; counter-capturing lambdas are not `static`.
- `tests/TokenHound.Infrastructure.Tests/Tray/TrayMenuModelTests.cs` — 4 `[Fact]`s covering TC-02 (order `[ToggleNotch, RefreshNow, Settings, About, Exit]`; `Exit.PrecededBySeparator == true`, others `false`; each header == its copy constant; `BuildDescriptor(false)` differs only by the toggle header) and TC-04 T01 half (`ResolveToggleHeader(true) == "Hide Notch"`, `(false) == "Show Notch"`), plus a copy-constant text assertion.

Modified:
- `src/TokenHound.App/UI/Windows/NotchWindow.xaml` — `CloseMenuItem` `Header="Close"` -> `Header="Hide"`; `x:Name` and `Click="OnCloseClick"` unchanged; no other XAML edit.
- `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` — added `public Func<bool>? ShouldInterceptClose;` and `public Action? CloseIntercepted;` (fields, XML docs); `Closing += OnClosing;` in the ctor; `OnClosing(object?, CancelEventArgs)` -> `if (ShouldInterceptClose?.Invoke() == true) { e.Cancel = true; CloseIntercepted?.Invoke(); }`; `OnCloseClick` body now `CloseIntercepted?.Invoke();` (dropped `_actionsViewModel?.CloseAsync()` — last non-test caller of the renamed member); added `public void ShowNotch()` (`Show(); ApplyPlacement();`, no `Activate()`) and `public void HideNotch() => Hide();`. `OnSourceInitialized`, the `WM_MOUSEACTIVATE` -> `MA_NOACTIVATE` hook, `WindowStyles.EnableNonActivating`, and `ApplyPlacement` (still private) untouched. File is 282 lines (<= 300).
- `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` — behaviour-preserving rename only: `closeAction` -> `shutdownAction`, `_closeAction` -> `_shutdownAction`, `_closeTask` -> `_shutdownTask`, `_isClosing` -> `_isShuttingDown`, `IsClosing` -> `IsShuttingDown`, `CloseAsync` -> `ShutdownAsync`, `nameof(IsClosing)` -> `nameof(IsShuttingDown)`; `<param name="closeAction">` -> `<param name="shutdownAction">` and the `ShutdownAsync` summary reworded to coordinated-shutdown wording; the `IsShuttingDown` summary already read "shutting down". No control-flow / method-count / signature-shape change; 247 lines (unchanged). Grep confirms zero remaining `CloseAsync` / `IsClosing` / `_closeTask` / `_closeAction` / `_isClosing` / `closeAction` symbols. `RefreshAsync` / `ShowSettings` / `ShowAbout` still early-return / no-op while `_isShuttingDown`; the latch still caches the first task (T01.14 confirmed by reading the post-rename bodies).
- `tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs` — `RefreshAsync_WhenCanceledDuringClosing_DoesNotSetErrorStatus`: `viewModel.CloseAsync()` -> `viewModel.ShutdownAsync()`, `viewModel.IsClosing` -> `viewModel.IsShuttingDown` (method name unchanged per contract). `CloseAsync_GuardsRepeatedCalls_AndPreventsSubsequentActions` -> method renamed to `ShutdownAsync_GuardsRepeatedCalls_AndPreventsSubsequentActions`; both `viewModel.CloseAsync()` calls -> `ShutdownAsync()`; `viewModel.IsClosing` -> `viewModel.IsShuttingDown`; `<summary>` reworded from "CloseAsync"/"close action" to "ShutdownAsync"/"shutdown action". `Constructor_WhenRequiredArgumentsNull_ThrowsArgumentNullException` left unchanged: it passes `null!` positionally and makes NO parameter-name assertion (`.WithParameterName` absent), so there is nothing to rename. All 9 `[Fact]` assertions and the executed-test count preserved. Grep confirms zero remaining `CloseAsync` / `IsClosing` symbols.

### Commands run (chronological; `$LASTEXITCODE` preserved and checked after each; nothing piped to `Out-Null`)

Phase 1 (after the 4 seam files):
1. `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal` -> exit 0 ("4 projects, 0 errors, 0 warnings").

Phase 2 (after `NotchWindow.xaml` + `.xaml.cs`):
2. `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal` -> exit 0 ("4 projects, 0 errors, 0 warnings"). `NotchWindow.xaml.cs` line count = 282 (`Get-Content | Measure-Object -Line`).

Phase 3 — rename gate (test project still clean; this is the T01 in-task gate):
3. `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal` -> exit 0 ("4 projects, 0 errors, 0 warnings").
4. `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo -v minimal` -> exit 0 ("3 projects, 0 errors, 0 warnings").
5. `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo -v minimal` (Debug, so the `--no-build` run below has a current assembly) -> exit 0 ("3 projects, 0 errors, 0 warnings").
6. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudActionsViewModelTests*"` -> exit 0 ("9 tests passed").
7. `rtk proxy dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudActionsViewModelTests*"` -> exit 0. Full MTP summary (pt-BR): **total: 9, falhou (failed): 0, concluído com êxito (passed): 9, ignorados (skipped): 0 — "Aprovado!"**.

Phase 4 — seam test files added last (documented hand-off state):
8. `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo -v minimal` -> exit 0 ("3 projects, 0 errors, 0 warnings" — App unaffected by the new test files).
9. `rtk proxy dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo -v minimal` -> **exit 1 (EXPECTED)**. Complete, deduplicated error list — exactly 2, both the `using TokenHound.App.UI.Tray;` line of the new Tray test files:
   - `tests/TokenHound.Infrastructure.Tests/Tray/NotchVisibilityControllerTests.cs(4,25): error CS0234: The type or namespace name 'Tray' does not exist in the namespace 'TokenHound.App.UI'`
   - `tests/TokenHound.Infrastructure.Tests/Tray/TrayMenuModelTests.cs(3,25): error CS0234: The type or namespace name 'Tray' does not exist in the namespace 'TokenHound.App.UI'`
   No error in `HudActionsViewModelTests.cs`, `HudActionsViewModel.cs`, or any other file. The error code is `CS0234` (unresolved `using` directive) rather than the `CS0246` anticipated in the plan text — same root cause (the `src/TokenHound.App/UI/Tray/*.cs` seam sources are not linked into the test project until T03 / CMP-12), the compiler simply stops at the unresolvable `using` before it can emit per-type `CS0246` for `NotchVisibilityController` / `TrayMenuModel` / `TrayMenuItemKey` / `TrayMenuEntry` / `TrayMenuDescriptor`. Not a defect: no `<Compile Include>` link added, no attempt to make this build pass. Per task Verification and the plan Executability gate, `*NotchVisibilityControllerTests*` / `*TrayMenuModelTests*` are NOT run as a gate in T01.

Numbering deviation (by design, called out per the executor sequence): task_01.md lists T01.5 (create the two Tray test files) under Step A, before Step B/Step C. The execution sequence runs it as Phase 4 — after Step C's rename and the Phase 3 rename gate — so the test project stays compilable at the one moment the rename-gate evidence is captured (step 7 above). Every other subtask ran in task_01.md order (T01.1-T01.4 -> T01.6-T01.11 -> T01.12-T01.14 -> T01.5).

### Validated state

- Code / diff: `git diff --stat` = `NotchWindow.xaml` (+1/-1), `NotchWindow.xaml.cs` (+37/-2), `HudActionsViewModel.cs` (25 lines changed, no net line delta), `HudActionsViewModelTests.cs` (+7/-7); 4 new seam files + 2 new seam test files untracked under `src/TokenHound.App/UI/Tray/` and `tests/TokenHound.Infrastructure.Tests/Tray/`. Nothing staged, no commit, no branch (D-006). The staged `tasks/prd-system-tray-notifyicon/*` entries and the `AM` state on `checkpoint.json` / `workflow.md` pre-date this task (orchestrator setup) and were not touched.
- Configuration: no change to any `.csproj`, `TokenHound.slnx`, or `global.json`. CMP-11 (`Hardcodet.NotifyIcon.Wpf` `PackageReference`) and CMP-12 (5 `<Compile Include>` links) remain T03's exclusive scope and are untouched. The 4 seam files carry no `using` directive; they compile under `net10.0-windows` today and (once T03 links 3 of them) will compile under `net10.0` with no `UseWPF` — the NFR-01 proof. Grep of `src/TokenHound.App/UI/Tray/` for `System.Windows` / `Hardcodet` / `System.Drawing` / `Microsoft.Win32` / `DllImport` -> no matches.
- Projects: `src/TokenHound.App` (`net10.0-windows`, WPF, `WinExe`) builds Release, 0 warnings, verified 4x. `tests/TokenHound.Infrastructure.Tests` (`net10.0`, no `UseWPF`, MTP) builds Debug + Release only until the Tray test files were added; after that it is blocked exactly as documented above until T03 / CMP-12.
- Environment: .NET SDK 10.0.400 (`global.json`, `rollForward: latestFeature`); MTP runner (`global.json` `test.runner = Microsoft.Testing.Platform`); `xunit.v3.mtp-v2` 4.0.0, `AwesomeAssertions` 9.6.0, `NSubstitute` 6.2.0; Windows 11, PowerShell, `rtk` proxy.

### Open items

- The test project does not `dotnet build` until T03 adds the CMP-12 `<Compile Include>` links; verified the only build errors are CS0234 (equivalently CS0246 in intent) for the seam types in `tests/.../Tray/*.cs` — specifically the `using TokenHound.App.UI.Tray;` line of `NotchVisibilityControllerTests.cs` and `TrayMenuModelTests.cs` — and nothing else.
- `NotchVisibilityControllerTests` (TC-03, TC-05, TC-09, TC-10) and `TrayMenuModelTests` (TC-02, TC-04 T01 half) are authored and Release-clean in intent but first execute under T03 once `NotchVisibilityController.cs`, `TrayMenuModel.cs`, and `TrayMenuItemKey.cs` are linked; T03 re-runs both with `--minimum-expected-tests 1`.
- `TrayMenuEntry` / `TrayMenuDescriptor` are top-level records co-located in `TrayMenuModel.cs` rather than nested inside the class — resolves a wording tension against the authoritative TechSpec seam block (see T01-ADR-01). T02's `new TrayMenuDescriptor { Entries = menu.BuildDescriptor(...) }` and `ITrayIcon`'s unqualified `TrayMenuDescriptor` parameter work unchanged.
- Manual acceptance MAN-01 / TC-16 (PRD steps 1-11) remains pending — coordinator, under T03.
- `App.xaml.cs` still constructs `HudActionsViewModel` with the 2nd argument positionally, so the member rename does not break it (T03 repoints that argument to `App.ShutdownAsync`). Confirmed indirectly by the green `src/TokenHound.App` Release builds.

### ADR candidates

**T01-ADR-01 — `TrayMenuEntry` / `TrayMenuDescriptor` declared as top-level records co-located in `TrayMenuModel.cs`**

- Context: task_01.md's "Contracts" note and TechSpec CMP-05 / T01.3 call `TrayMenuEntry` and `TrayMenuDescriptor` "nested" records "in this file". The authoritative TechSpec "Contracts and data" C# seam block, however, declares both at namespace scope (`namespace TokenHound.App.UI.Tray;`, not indented inside any type), and every consumer reference is unqualified with no `using static`: `ITrayIcon.Show(Uri, string, TrayMenuDescriptor menu)`; task_02.md T02.4 `TrayMenuDescriptor BuildMenu() => new TrayMenuDescriptor { Entries = menu.BuildDescriptor(visibility.IsNotchVisible) }`; task_02.md line 26 lists `TrayMenuDescriptor` as a distinct seam type beside `TrayMenuModel`.
- Decision: `public sealed record TrayMenuEntry` and `public sealed record TrayMenuDescriptor` are top-level types in namespace `TokenHound.App.UI.Tray`, co-located in `TrayMenuModel.cs` alongside `public sealed class TrayMenuModel`. `TrayMenuModel.BuildDescriptor(bool)` returns `IReadOnlyList<TrayMenuEntry>` (the entry list), not a `TrayMenuDescriptor`.
- Alternatives: (a) Nest both inside `TrayMenuModel` — forces `TrayMenuModel.TrayMenuDescriptor` or a `using static TokenHound.App.UI.Tray.TrayMenuModel;` in `ITrayIcon.cs`, `TrayIconViewModel.cs`, `TrayIconHost.cs`, and both Tray test files, contradicting the authoritative seam block and task_02's literal code. (b) One file per record — contradicts CMP-05 ("record DTOs in this file").
- Consequences: three top-level types in one 81-line file — a deliberate exception to "one class per file", explicitly permitted by TechSpec CMP-05 ("nested types allowed"). Every downstream consumer (T02, T03) uses the unqualified names exactly as their task text already assumes; no rework propagated.
- Evidence: TechSpec "Contracts and data" (lines ~90-149); task_02.md line 26 and T02.4; `src/TokenHound.App` Release build green 4x with the seam files present.
- TechSpec relationship: implements CMP-01 / CMP-04 / CMP-05 as written in the authoritative seam block; reconciles the looser "nested" phrasing used elsewhere in the plan.
