# PRD — System Tray NotifyIcon

## Problem and context

`TokenHound.App` is designed to run continuously in the background: `App.OnStartup` sets
`ShutdownMode.OnExplicitShutdown` and the Notch window sets `ShowInTaskbar="False"`. Yet the
application has **no notification-area presence**. Once the Notch is hidden or dismissed there is
**no way to bring it back**, and the Notch context menu's `Close` item is wired to
`HudActionsViewModel.CloseAsync` → `ApplicationLifetime.ShutdownAsync`, so the only "close" the user
can reach performs a **full application shutdown**. Hiding the Notch is therefore a dead end and
dismissing it is indistinguishable from quitting.

A native Windows notification-area icon (tray icon / `NotifyIcon`) closes this gap. It gives the
background process a persistent, discoverable handle: a context menu to show or hide the Notch,
force a refresh, open Settings or About, and exit; and a click target that toggles Notch
visibility. With the tray in place, hiding or closing the Notch becomes a safe background state and
`Exit` becomes the single, explicit path to the coordinated shutdown.

All tray code lives in `TokenHound.App`. `TokenHound.Core` and `TokenHound.Infrastructure` are not
touched. The tray reuses the existing `HudActionsViewModel` command logic rather than duplicating
it, and adds only a visibility-toggle action and an `Exit` action.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | The background process has a persistent, single notification-area presence whenever it runs. | Launching `TokenHound.App` shows exactly one TokenHound tray icon with tooltip "TokenHound"; it remains present after the Notch is hidden or closed and until `Exit`. |
| OBJ-02 | Users can restore the Notch after it has been hidden or closed. | With the Notch hidden, a tray left-click, double-click, or the "Show Notch" menu item makes the Notch visible again at its persisted position without stealing keyboard focus. |
| OBJ-03 | Hiding or closing the Notch no longer terminates the application. | Invoking the Notch menu's "Hide" item, or any Notch-close path, leaves the process running with the tray icon present; `ApplicationLifetime.ShutdownAsync` is not called. |
| OBJ-04 | `Exit` is the single coordinated shutdown path. | The tray "Exit" item invokes `ApplicationLifetime.ShutdownAsync` exactly once, drains the `UsageStore`, disposes tracked resources, removes the tray icon, and terminates the app; no other tray or Notch action calls it. |
| OBJ-05 | Tray actions reuse existing HUD command logic. | "Refresh Now", "Settings…", and "About…" dispatch through the same `HudActionsViewModel` delegates as the Notch context menu, with no duplicated command implementations; verified by unit tests on the command-to-action mapping. |
| OBJ-06 | The Notch menu and the tray menu present a consistent lifecycle. | The Notch context menu's former "Close" item reads "Hide" and hides the Notch; the tray menu's toggle item reflects current Notch visibility ("Hide Notch" / "Show Notch"). |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Background user who hid the Notch | Bring the Notch back | Regain the at-a-glance view without restarting the app | Left-clicks (or double-clicks) the tray icon; the Notch reappears at its persisted position; focus stays in the foreground editor/terminal. |
| US-02 | User pausing the on-screen view | Get the Notch off screen while monitoring continues | A quiet screen without losing provider tracking | Opens the Notch context menu, clicks "Hide"; the Notch disappears, the tray icon stays, `UsageStore` polling keeps running. |
| US-03 | User done for the session | Fully quit the application | Clean shutdown with no leftover process or icon | Opens the tray menu, clicks "Exit"; the app drains and terminates and the tray icon disappears with no ghost. |
| US-04 | User wanting fresh numbers | Force a refresh without opening the Notch | Up-to-date readings on demand | Opens the tray menu, clicks "Refresh Now"; providers refresh once; invoking it again while a refresh is in flight does nothing. |
| US-05 | User adjusting configuration | Reach Settings from the tray | Configure providers/cadence without the Notch visible | Opens the tray menu, clicks "Settings…"; the modeless Settings dialog opens, or the already-open one is activated. |
| US-06 | User checking the build | See the app version and About info | Confirm which version is running | Opens the tray menu, clicks "About…"; the About dialog opens. |
| US-07 | User double-clicking the tray icon | Toggle Notch visibility predictably | No accidental double-toggle or no-op | One left-click or one double-click produces exactly one visibility transition. |
| US-08 | First-time user | Discover the running app | Know the app is alive and how to reach it | On startup one tray icon appears; hovering it shows the tooltip "TokenHound". |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Tray icon presence, identity, and single lifecycle. | On startup `TokenHound.App` registers exactly one notification-area icon using `src/TokenHound.App/Assets/logo.ico` with tooltip text "TokenHound". The icon is created once during UI initialisation and removed exactly once on shutdown; showing/hiding the Notch or opening dialogs never creates a second icon. Observable: unit test asserts the tray host initialises with the icon resource and the tooltip constant and guards against double-create/double-dispose; manual: exactly one icon in the Windows 11 notification area, hover shows "TokenHound". |
| FR-02 | Tray context menu contents and order. | The tray icon's context menu contains, in order: a visibility toggle item ("Hide Notch" / "Show Notch"), "Refresh Now", "Settings…", "About…", a separator, "Exit". Observable: unit test on the menu model asserts item keys, headers, and order; manual: right-click shows exactly these items. |
| FR-03 | Toggle Notch visibility from the tray icon. | A single left-click or a double-click on the tray icon toggles Notch visibility, producing exactly one transition per gesture (visible → hidden or hidden → visible). Observable: unit test on the visibility controller asserts `Toggle` flips state and invokes the matching show or hide delegate exactly once; manual: click hides a visible Notch, click again shows it, double-click toggles once. |
| FR-04 | Toggle menu item header reflects Notch visibility. | The toggle item header is "Hide Notch" when the Notch is visible and "Show Notch" when it is hidden, refreshed when the menu opens and whenever visibility changes. Observable: unit test asserts the header string for both states. |
| FR-05 | Showing the Notch restores placement without stealing focus. | Showing the Notch from the tray makes the existing `NotchWindow` instance visible at its persisted position via the existing placement path (`ApplyPlacement`) and does not activate it or move keyboard focus from the foreground application; the `WS_EX_NOACTIVATE` styling and the `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` (3) hook remain in force. Observable: unit test asserts the show path routes through the non-activating placement routine; manual: type into another app while showing the Notch — no characters lost, focus not taken. |
| FR-06 | "Refresh Now" reuses the HUD refresh path with its concurrency guard. | The tray "Refresh Now" item dispatches the same `HudActionsViewModel.RefreshAsync` path (→ `UsageStore.RefreshNowAsync`) used by the Notch menu's "Refresh" item. While a refresh is in progress the tray item is disabled or a no-op and does not start a second refresh. Observable: unit test asserts a single underlying refresh dispatch under concurrent invocation; manual: two quick clicks refresh once (log shows one cycle). |
| FR-07 | "Settings…" and "About…" reuse the existing modeless dialogs. | The tray items invoke `HudActionsViewModel.ShowSettings` / `ShowAbout`, opening the existing modeless dialogs through `DialogService`; if a dialog is already open, its existing instance is activated/restored rather than duplicated; both items are no-ops while a coordinated shutdown is in progress. Observable: unit test on the command mapping; manual: only one Settings window exists, re-invoking activates it. |
| FR-08 | "Exit" is the sole coordinated shutdown path and is idempotent. | The tray "Exit" item invokes `ApplicationLifetime.ShutdownAsync` exactly once (guarded against repeat), which cancels the lifetime token, closes dialogs, drains the startup task and `UsageStore`, disposes tracked resources, removes the tray icon, and terminates the WPF application. No other tray or Notch action calls `ShutdownAsync`. Observable: unit test asserts "Exit" maps to the shutdown delegate and repeated invocation runs it once; manual: "Exit" closes the app, the tray icon disappears, no ghost icon, log shows "Application shutdown completed successfully." |
| FR-09 | The Notch context menu's "Close" item becomes "Hide". | The Notch context-menu item currently labelled "Close" is relabelled "Hide". Invoking it hides the Notch and keeps the process and the single `NotchWindow` instance alive; it no longer calls `ApplicationLifetime.ShutdownAsync`. Observable: unit test asserts the Notch hide action maps to the hide delegate, not the shutdown delegate; manual: the menu's last-but-one item reads "Hide" and clicking it removes the Notch while the tray icon and process remain. |
| FR-10 | No Notch-close path terminates the application. | Any close request targeting the Notch (context-menu "Hide", a programmatic `Close`, or an OS-initiated close) results in the Notch being hidden, not the process ending, and the `NotchWindow` instance is preserved so a later "Show Notch" restores it. Coordinated shutdown via "Exit" is the only exception. Observable: unit/integration test on the close-intercept behaviour; manual: attempting to close the Notch keeps the app running. |
| FR-11 | Background lifecycle continuity while the Notch is hidden. | While the Notch is hidden, `UsageStore` polling, activity monitoring, and 429 deadline handling continue unchanged. Observable: integration test shows scheduled polling ticks continue with the Notch hidden; manual: hide the Notch and confirm a subsequent scheduled refresh still occurs (log evidence). |
| FR-12 | Graceful degradation if the tray icon cannot be created. | If notification-area registration fails, the application logs a structured warning and continues running with the Notch shown as the primary surface, so the user is never left with no surface. Observable: unit test simulates a failed tray host initialisation and asserts the app still shows the Notch and logs the warning; not routinely testable manually, covered by code review. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Layer purity | No tray, `NotifyIcon`, or Win32 shell code in `TokenHound.Core` or `TokenHound.Infrastructure`; all tray code resides in `TokenHound.App`. The visibility-toggle state machine and command-to-action mapping sit behind a seam with no dependency on a live `NotifyIcon`, mirroring how `HudActionsViewModel` isolates HUD command logic. |
| NFR-02 | Code style and complexity ([AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md)) | One class per file; classes sealed by default; files/classes <= 300 lines; methods <= 30 lines; nesting <= 3 levels; XML docs on public members; file-scoped namespaces with alphabetised usings; `UPPER_CASE` constants; `nameof` instead of literals; no `#region`. |
| NFR-03 | Tray dependency constraint | If a tray library is introduced it must be `Hardcodet.NotifyIcon.Wpf` (the library [ARCHITECTURE.md](file:///D:/MyProjects/TokenHound/ARCHITECTURE.md) sanctions), added as a version-pinned `PackageReference` consistent with the existing `Serilog` reference. The alternative is a zero-dependency Win32 `Shell_NotifyIcon` P/Invoke. No other tray library. The choice is deferred to the TechSpec; this PRD stays implementation-agnostic. |
| NFR-04 | Target frameworks unchanged | `net10.0-windows` for `TokenHound.App`, `net10.0` for `TokenHound.Core` / `TokenHound.Infrastructure`. No new target framework; no change to `OutputType` or `UseWPF`. |
| NFR-05 | Idle footprint | The tray presence must not materially raise the ~30–45 MB idle memory target from [ARCHITECTURE.md](file:///D:/MyProjects/TokenHound/ARCHITECTURE.md). No busy-wait or polling loop for visibility state; tray interaction is event-driven only. |
| NFR-06 | Focus safety | No tray interaction may activate the Notch or steal keyboard focus from the foreground application. The `WS_EX_NOACTIVATE` styling and the `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` (3) hook remain in force; `WindowStyles.EnableNonActivating` keeps `SWP_NOZORDER` and omits `SWP_SHOWWINDOW`. |
| NFR-07 | Shutdown integrity | Exactly one `ApplicationLifetime.ShutdownAsync` execution per process lifetime. The tray icon is always removed on exit (no orphaned notification-area icon); on abnormal termination the OS reclaims it (no persistent ghost). |
| NFR-08 | Thread safety | Tray click and menu callbacks marshal Notch and dialog operations onto the WPF dispatcher, following the existing `App.DispatchUiAction` pattern; no cross-thread access to `NotchWindow`. |
| NFR-09 | Observability | Every tray action (toggle, refresh, settings, about, exit) and tray lifecycle event (created, removed, creation failed) emits a structured Serilog entry with typed properties; state names use `nameof`, not hard-coded strings. |
| NFR-10 | Accessibility | The tray context menu is the standard Windows notification-area menu (keyboard navigable, screen-reader friendly by OS default); item labels are self-describing; `logo.ico` provides 16 px and 32 px frames for crisp rendering at 100%, 150%, and 200% display scaling. |
| NFR-11 | Testability | The visibility-toggle state machine and command mapping are unit-testable without a live `NotifyIcon` or a running message loop, using delegate injection as in [HudActionsViewModelTests](file:///D:/MyProjects/TokenHound/tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs). New tests attach to `tests/TokenHound.Infrastructure.Tests` (current home of App view-model tests) unless the TechSpec adds a dedicated App test project. Validation excludes E2E. |

## User experience

The feature adds one surface (the tray icon and its menu) and makes one label change on an existing
surface (the Notch context menu). It introduces no new windows and no visual change to the Notch or
the dialogs.

### Tray icon and menu

- A single TokenHound icon sits in the Windows 11 notification area for the life of the process.
  Hovering shows the tooltip `TokenHound`.
- Left-click or double-click toggles Notch visibility.
- Right-click opens the context menu:

```
+---------------------------+
|  Hide Notch               |   <- reads "Show Notch" when the Notch is hidden
|  Refresh Now              |   <- inert while a refresh is already running
|  Settings...              |
|  About...                 |
| ------------------------- |
|  Exit                     |   <- the only coordinated shutdown
+---------------------------+
```

### Notch context menu (after this feature)

```
+---------------------------+
|  Refresh                  |
|  Settings                 |
|  About                    |
| ------------------------- |
|  Hide                     |   <- was "Close"; now hides the Notch, does not quit
+---------------------------+
```

### Feedback and error states

- Showing the Notch reuses `ApplyPlacement`, so it reappears exactly where the user last left it.
- "Refresh Now" and the Notch "Refresh" share `HudActionsViewModel` state, so the existing Notch
  status popup ("Refreshing usage…", "Refresh completed", …) still reflects a tray-initiated
  refresh whenever the Notch is visible.
- If the tray icon cannot be registered, the app logs a structured warning and keeps the Notch
  visible as the primary surface (FR-12).

### Manual acceptance script (Windows 11 interactive desktop)

Launch `TokenHound.App` via the Windows MCP `App` tool (`mode="launch_executable"`); the shell
`Start-Process` runs on an isolated desktop and will not render to the user screen.

1. Launch the app. Expect: the Notch appears at its persisted position; exactly one TokenHound icon
   appears in the notification area; hovering it shows "TokenHound".
2. Right-click the tray icon. Expect the menu: "Hide Notch", "Refresh Now", "Settings…", "About…",
   separator, "Exit".
3. Click "Hide Notch" (or left-click the tray icon). Expect: the Notch disappears; the tray icon
   remains; the app keeps running (confirm via a later scheduled refresh in the log).
4. Left-click (or double-click) the tray icon. Expect: the Notch reappears at the same position;
   keyboard focus stays in the app that was in front (type into a text editor while clicking — no
   characters lost).
5. Re-open the tray menu. Expect the toggle item reads "Hide Notch" again.
6. Click "Refresh Now" twice in quick succession. Expect: providers refresh once (log shows a
   single cycle), no error.
7. Click "Settings…". Expect: the modeless Settings dialog opens. Click "Settings…" again. Expect:
   the same window is activated, not a second one. Close it.
8. Click "About…". Expect: the About dialog opens showing the app version. Close it.
9. Right-click the Notch. Expect the last item reads "Hide", not "Close". Click it. Expect: the
   Notch hides; the app keeps running; the tray icon remains.
10. Restore the Notch from the tray, then click tray "Exit". Expect: the Notch closes, the app
    terminates within a couple of seconds, the tray icon disappears with no ghost, and the log
    shows "Application shutdown completed successfully."
11. Relaunch and confirm a single icon again. (Launching twice today yields two icons — see
    Out of scope and the single-instance follow-up.)

## Constraints and dependencies

- **Repo invariant — layer purity**: `TokenHound.Core` stays 100% pure (no UI/OS); all tray code
  lives in `TokenHound.App`. Source: [AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md),
  [ARCHITECTURE.md](file:///D:/MyProjects/TokenHound/ARCHITECTURE.md).
- **Repo invariant — non-activating HUD**: hook `WM_MOUSEACTIVATE` returning `MA_NOACTIVATE` (3);
  `WindowStyles.EnableNonActivating` uses `SWP_NOZORDER` (0x0004) and omits `SWP_SHOWWINDOW`.
- **Repo invariants — style and complexity**: see NFR-02.
- **Existing lifecycle**: `App.OnStartup` sets `ShutdownMode.OnExplicitShutdown`; `NotchWindow` has
  `ShowInTaskbar="False"`; `ApplicationLifetime.ShutdownAsync` is the coordinated shutdown and is
  already idempotent.
- **Reuse, do not duplicate**: `HudActionsViewModel` (Refresh / ShowSettings / ShowAbout, plus the
  `IsClosing` / guarded terminal-action pattern), `DialogService` (modeless Settings/About with
  activate-existing behaviour and `CloseAll`), and the `App.DispatchUiAction` dispatcher pattern.
- **Icon resource**: `src/TokenHound.App/Assets/logo.ico` already exists and is declared as
  `<Resource>` and `<ApplicationIcon>` in `TokenHound.App.csproj`.
- **Tray library**: `Hardcodet.NotifyIcon.Wpf` is the sanctioned option per
  [ARCHITECTURE.md](file:///D:/MyProjects/TokenHound/ARCHITECTURE.md); a raw Win32 `Shell_NotifyIcon`
  P/Invoke is the zero-dependency alternative. The decision is left to the TechSpec. Any package
  added must be a version-pinned `PackageReference` like the existing `Serilog` entry.
- **Platform stack**: Windows 11, .NET 10, WPF, Segoe UI, dark surface styling; English UI copy.
- **Persistence**: none required for v1 (no new settings). Any future persisted option (for
  example "remember the Notch hidden across restarts") would use `System.Text.Json` per
  [AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md); out of scope now.
- **Validation**: .NET desktop policy — no E2E; unit + integration tests plus the manual
  acceptance script above. MTP runner per the `dotnet-efficient-validation` skill.

## Out of scope

- Timed "Hide for 1 hour" auto-restore and auto-hide on fullscreen or on a covering window
  (mentioned in [docs/design/2026-08-28-usage-notch-design.md](file:///D:/MyProjects/TokenHound/docs/design/2026-08-28-usage-notch-design.md)) — future.
- **Single-instance guard** (prevent a second launch adding a duplicate tray icon and Notch).
  Recommended near-term follow-up; see "Recommended near-term follow-up" below. Not scoped here.
- Status-driven tray icon tinting, badge, or overlay (for example colour by the most-constrained
  provider) — future.
- Balloon tips or toast notifications raised from the tray icon (for example "Claude limit
  reached") — future.
- Rich status text in the tray tooltip (per-provider summary). Optional nice-to-have; the v1
  tooltip is just "TokenHound".
- Any change to `TokenHound.Core`, the `TokenHound.Infrastructure` engine internals, or provider
  adapters.
- Persisting Notch visibility across restarts; the app always starts with the Notch shown.
- Localisation of menu strings (English only, per repo policy).
- Restyling the Notch's own context menu beyond the "Close" → "Hide" relabel (for example adding
  ellipses to "Settings"/"About").
- Architecture diagrams, implementation tasks, and code changes in this PRD artifact.

## Assumptions and sources

### User decisions

- Feature selected via **D-001** ([workflow.md](file:///D:/MyProjects/TokenHound/tasks/prd-system-tray-notifyicon/workflow.md)):
  PRD 11 chosen by cost/benefit; App-layer only; `TokenHound.Core` stays pure; reuse
  `HudActionsViewModel`.
- **D-002**: authorised to produce this PRD; the HIL 1 product gate still applies.
- Tray menu actions are fixed as: Show/Hide Notch (toggle), Refresh Now, Settings…, About…, Exit
  (product framing + ROADMAP PRD 11).
- Left-click and double-click both toggle Notch visibility (product framing).
- "Exit" is the only path that runs `ApplicationLifetime.ShutdownAsync`; hiding or closing the
  Notch keeps the process alive in the tray (product framing + ROADMAP PRD 11).
- The Notch context menu's "Close" item becomes "Hide" for surface consistency (product framing).
- The tray tooltip is "TokenHound"; a richer status tooltip is optional and not part of v1
  (product framing).

### Local evidence

- [docs/ROADMAP.md](file:///D:/MyProjects/TokenHound/docs/ROADMAP.md) — Phase 3, "PRD 11 System Tray
  Integration": `NotifyIcon` via `Hardcodet.NotifyIcon.Wpf` or Win32 `Shell_NotifyIcon`; menu
  "Show/Hide Notch", "Refresh Now" (→ `UsageStore.RefreshNowAsync`), "Settings...", "About...",
  "Exit"; double- or left-click toggles the Notch; background lifecycle under
  `ShutdownMode.OnExplicitShutdown`.
- [ARCHITECTURE.md](file:///D:/MyProjects/TokenHound/ARCHITECTURE.md) — `Hardcodet.NotifyIcon.Wpf`
  named as the consolidated WPF tray option; idle footprint target ~30–45 MB "running continuously
  in the system tray"; folder-tree note "App.xaml / Program.cs — Entry point, Single-Instance, and
  System Tray"; Core purity invariant.
- [docs/design/2026-08-28-usage-notch-design.md](file:///D:/MyProjects/TokenHound/docs/design/2026-08-28-usage-notch-design.md)
  — right-click menu intent ("Settings…, Refresh now, Hide for 1 hour, Quit"); the Notch is
  always-visible and click-through; "Hide for 1 hour" and auto-hide options are design-doc futures.
- [CONTEXT.md](file:///D:/MyProjects/TokenHound/CONTEXT.md) — domain terms: "Notch", "Provider
  Ring", "Provider", "Snapshot"; avoid "window / overlay / widget / tray daemon" synonyms.
- [src/TokenHound.App/App.xaml.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/App.xaml.cs)
  — `OnStartup` sets `ShutdownMode.OnExplicitShutdown`; `InitializeUi` builds `DialogService`,
  `NotchViewModel`, `ApplicationLifetime`, and
  `HudActionsViewModel(usageStore.RefreshNowAsync, _lifetime.ShutdownAsync, ShowSettings, ShowAbout, snapshots)`;
  `MainWindow = _notchWindow`; `DispatchUiAction` marshals to the dispatcher.
- [src/TokenHound.App/ViewModels/HudActionsViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/HudActionsViewModel.cs)
  — `RefreshAsync` (concurrency guard via `_isRefreshing` / `_isClosing`), `CloseAsync`
  (idempotent, sets `IsClosing`, calls the injected close delegate — today `ShutdownAsync`),
  `ShowSettings` / `ShowAbout` (no-ops while closing).
- [src/TokenHound.App/ApplicationLifetime.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ApplicationLifetime.cs)
  — `ShutdownAsync` is idempotent; cancels the lifetime token, calls `DialogService.CloseAll()`,
  disposes `NotchViewModel`, drains the startup task and `UsageStore`, disposes tracked resources,
  then `Application.Shutdown()`.
- [src/TokenHound.App/UI/Windows/NotchWindow.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/NotchWindow.xaml)
  — `CapsuleContextMenu` items "Refresh", "Settings", "About", separator, "Close";
  `ShowInTaskbar="False"`; `Icon="../../Assets/logo.ico"`; `Title="TokenHound"`.
- [src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs)
  — `OnCloseClick` calls `_actionsViewModel?.CloseAsync()`; `WM_MOUSEACTIVATE` / `MA_NOACTIVATE`
  hook; `ApplyPlacement` restores the persisted position.
- [src/TokenHound.App/UI/Windows/DialogService.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/DialogService.cs)
  — modeless Settings/About; re-invocation activates the existing window; `CloseAll()`.
- [src/TokenHound.App/TokenHound.App.csproj](file:///D:/MyProjects/TokenHound/src/TokenHound.App/TokenHound.App.csproj)
  — `net10.0-windows`, `UseWPF`, `OutputType=WinExe`, `Serilog` as a version-pinned
  `PackageReference`, `Assets\logo.ico` as `<Resource>` and `<ApplicationIcon>`.
- [tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs](file:///D:/MyProjects/TokenHound/tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs)
  — precedent: App view-model logic tested with delegate injection, xUnit + AwesomeAssertions,
  MTP runner.
- [AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md) — style/complexity invariants;
  `System.Text.Json` for config; `.ConfigureAwait(false)` in Core/Infrastructure; structured
  logging.

### Explicit assumptions

- **A-01**: There is one long-lived `NotchWindow` instance owned by `App`. "Hide" makes that
  instance invisible; "Show" makes it visible and re-applies placement. No per-toggle window
  recreation. Impact if wrong: the show/hide contract and FR-05/FR-10 wording would need revision.
- **A-02**: `logo.ico` already contains notification-area-appropriate resolutions (16/32 px). If it
  does not, icons may look soft at high DPI — cheap to regenerate; does not change scope.
- **A-03**: Reusing `HudActionsViewModel` entails repurposing its second constructor delegate from
  "shutdown" to "hide Notch" (and likely renaming `closeAction` / `CloseAsync` / `IsClosing` to
  hide semantics), with a separate `Exit` action wired to `ApplicationLifetime.ShutdownAsync`. This
  touches `HudActionsViewModel.cs` and `HudActionsViewModelTests.cs`. The alternative — leave
  `HudActionsViewModel` untouched and add a thin tray view-model that composes it plus hide/exit
  actions — is equally valid. The choice belongs to the TechSpec.
- **A-04**: `ShutdownMode.OnExplicitShutdown` stays. With `MainWindow = _notchWindow`, hiding or
  closing the Notch already does not end the process; the tray only needs to add the way back and
  move the real shutdown onto "Exit".
- **A-05**: Windows collapses a single-click-then-double-click sequence into one logical toggle via
  a standard debounce; the observable contract is "one gesture → one transition" (FR-03).
- **A-06**: The manual acceptance script runs on a Windows 11 interactive desktop, launching via
  the Windows MCP `App` tool per [AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md).
- **A-07**: No dedicated `TokenHound.App.Tests` project exists; new unit tests attach to
  `tests/TokenHound.Infrastructure.Tests` unless the TechSpec adds an App test project.

### Derived artifacts to revalidate (not modified by this PRD)

- `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` — "Close" semantics become "Hide"; add or
  redirect an `Exit` action.
- `tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs` — the `CloseAsync`
  / `IsClosing` tests will need renaming or retargeting to hide semantics.
- `src/TokenHound.App/UI/Windows/NotchWindow.xaml` + `NotchWindow.xaml.cs` — relabel "Close" →
  "Hide"; `OnCloseClick` hides instead of shutting down; possible `OnClosing` intercept for FR-10.
- `src/TokenHound.App/App.xaml.cs` (`InitializeUi`) — construct and own the tray host; repoint the
  hide delegate; wire `Exit` to `ApplicationLifetime.ShutdownAsync`.
- `src/TokenHound.App/TokenHound.App.csproj` — a version-pinned `PackageReference` if
  `Hardcodet.NotifyIcon.Wpf` is chosen (deferred to the TechSpec).
- `ARCHITECTURE.md` — the folder-tree line cites a non-existent `Program.cs` for "Single-Instance,
  and System Tray"; if the TechSpec adds a tray bootstrap the tree and notes should be reconciled.
- `docs/ROADMAP.md` — PRD 11 status is "[Planned]"; the coordinator may advance it after HIL.
- `graft/` context graph — rerun `graft build` after implementation.

### Recommended near-term follow-up (not scoped here)

- **Single-instance guard**: without one, a second launch of `TokenHound.App` adds a second tray
  icon and a second Notch, and "Exit" on one instance leaves the other's icon behind.
  [ARCHITECTURE.md](file:///D:/MyProjects/TokenHound/ARCHITECTURE.md) already lists "Single-Instance"
  as an intended entry-point concern. Recommended as the immediate next slice after this feature.
  **Not blocking for v1 of the tray icon**: the tray feature is correct and testable for the
  normal single-instance case, and nothing here makes a duplicate launch worse than it is today
  (a second launch already produces a second Notch). It rises in priority precisely because a
  stray tray icon is more visible and more confusing than a stray Notch, so it should ship soon
  after — but it is a separate concern with its own acceptance surface and does not need to gate
  this PRD.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified organizational source.
- [x] Implementation details remain in the TechSpec.
