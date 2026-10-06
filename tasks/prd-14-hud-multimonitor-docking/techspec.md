# TechSpec — HUD multi-monitor and edge docking

## Sources and traceability

- PRD: `tasks/prd-14-hud-multimonitor-docking/prd.md` (approved, DEC-03 in `workflow.md`).
- Applicable instructions, rules, and skills: `AGENTS.md` (HUD non-activating invariants, C# style, file ≤ 300 lines, method ≤ 30 lines, MTP commands), `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Specs and design: `docs/design/2026-08-28-usage-notch-design.md` ("The shape of the thing", "Hover state"), `docs/ROADMAP.md` (PRD 13 track, Phase 3 sequencing).
- Evidence in existing code:
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml` — horizontal `StackPanel` (line 75), `Grid Margin="12,0,12,12"` shadow gutter (line 22), `CornerRadius="0,0,24,24"` and `BorderThickness="1.5,0,1.5,1.5"` (lines 30-31), `StatusPopup Placement="Bottom"` (line 94), context menu (lines 36-62).
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` — `WndProc` (`WM_MOUSEACTIVATE`, `WM_DISPLAYCHANGE`), `OnMouseLeftButtonDown` (`DragMove` + `PersistPosition`), `PersistPosition` (writes a new `HudPositionSettings { Left, Top }`), 258 lines.
  - `src/TokenHound.App/UI/Windows/NotchWindow.Placement.cs` — `ApplyPlacement`, `ApplyMonitorPlacement`, `_applyingPlacement` reentrancy guard (commit `c4b55b9`).
  - `src/TokenHound.App/UI/Windows/NotchWindow.Scale.cs` — `BASE_MIN_WIDTH = 180`, `BASE_MIN_HEIGHT = 48` scaled by `HudScale`.
  - `src/TokenHound.App/UI/Placement/NotchPlacement.cs`, `ScreenBounds.cs` — pure placement math, compiled into `TokenHound.Infrastructure.Tests` through `<Compile Include … Link>` (`TokenHound.Infrastructure.Tests.csproj:42-65`).
  - `src/TokenHound.App/Interop/WindowPlacement.cs:GetWorkArea` — nearest-monitor work area divided by the window's current DPI.
  - `src/TokenHound.Infrastructure/Configuration/HudPositionSettings.cs`, `HudPositionStore.cs` (section `Hud` through `SectionStore<T>`), `UserSettings.cs`, `UserSettingsFile.cs:MergeWithDefaults` (copies `Hud` whole).
  - `src/TokenHound.App/Presentation/HudScale.cs` — shared static observable used by XAML in separate visual trees (tooltips, popup); `HudSizeSettingsViewModel` — settings section pattern with `Create(store, …)` and a `Func<T, bool> save`.
  - `src/TokenHound.App/UI/Controls/ProviderRing.xaml` — tooltip `TooltipCard` hosted in `UserControl.ToolTip` with default placement.
  - `src/TokenHound.App/App.xaml.cs:351-400` — `CreateSettingsViewModel`, `NotchWindow` creation.

## Solution summary

The HUD placement becomes a persisted **mode** (Top Left, Top Center, Top Right, Left edge, Right edge, Free) plus an optional **preferred display**. Docked modes are computed, never stored as coordinates: on every relevant event the HUD resolves its target display from the live display list, computes the docked window rectangle in physical pixels with pure functions, and moves the window with `SetWindowPos` (no activation, no z-order change, no size change). Free mode keeps today's DIP `Left`/`Top` and the `c4b55b9` nearest-monitor clamp unchanged. A small application service owns the placement state and its persistence so the drag gesture, the Settings section, and the context-menu submenu all change the same state.

The capsule chrome (stack orientation, shadow gutter, rounded corners, border sides, minimum size, tooltip and popup direction) follows the docked edge through a pure edge-layout mapping and one shared observable for the separate tooltip visual trees. Display identity comes from the Windows display configuration API (monitor device path plus an EDID key), so a preferred display survives restarts and, when unique, a port change. Bézier geometry, backdrop, click-through, and animation stay out (ROADMAP PRD 12).

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-01, FR-14, FR-15, FR-16, NFR-04 | Extend `HudPositionSettings` with `Mode` (`string?`) and `Display` (`HudDisplayPreference?`), keep `Left`/`Top`. Parse `Mode` with a resolver instead of a JSON enum converter | An unknown enum string would make `System.Text.Json` throw and `SectionStore` would drop the whole `Hud` section, losing `Left`/`Top`; a string keeps the section and lets the resolver fall back and log (FR-16) | `JsonStringEnumConverter`: simpler, but fails the whole section on an unknown value |
| DEC-02 | FR-14, FR-15, FR-16 | Mode resolution: `Mode` null and finite `Left`/`Top` → Free (migration); `Mode` null without coordinates → Top Center; unknown `Mode` → Top Center + one warning; `Mode = Free` without finite coordinates → Top Center + one warning | Matches DEC-02 product decision 2 in `workflow.md`; no write happens on load, so an upgrade does not touch the file until the user changes something | Migrating by rewriting the file on first load: adds a write with no user benefit |
| DEC-03 | FR-02, FR-03, FR-13, NFR-02 | Docked placement is computed and applied in **physical pixels**: target work area from `GetMonitorInfo`, window size from `GetWindowRect`, move with `SetWindowPos(SWP_NOSIZE \| SWP_NOZORDER \| SWP_NOACTIVATE)` | `GetWorkArea` divides by the window's *current* DPI, which is wrong when the target display has another scale; physical coordinates are consistent across displays in whatever DPI awareness mode the process runs. After a cross-DPI move WPF resizes the window and `SizeChanged` re-anchors once more with the final size | Setting WPF `Left`/`Top` in DIPs: off by the DPI ratio on mixed-scale setups |
| DEC-04 | FR-10, FR-11 | Free mode keeps today's DIP `Left`/`Top` and the existing `ApplyMonitorPlacement` clamp path | Preserves the behavior and tests from `c4b55b9`; stored values stay compatible with older files | Converting Free to physical pixels: would break existing stored coordinates |
| DEC-05 | FR-06, FR-08, FR-09 | Display identity: `DevicePath` (`DISPLAYCONFIG_TARGET_DEVICE_NAME.monitorDevicePath`) as primary key, `EdidKey` (`edidManufactureId:edidProductCodeId` in hex) as fallback key, and `Name` (`monitorFriendlyDeviceName`) for display only. Match order: exact `DevicePath`; else a single connected display with the same `EdidKey`; else not connected | The device path is stable for the same monitor on the same connector across reboots; the EDID key survives a port change when it is unique. Two identical monitors after a port swap are ambiguous and fall back to the primary (documented limitation) | GDI name `\\.\DISPLAYn`: renumbers on reconnect. EDID serial: not exposed by this API |
| DEC-06 | FR-06, FR-08, FR-09 | Enumerate displays with `EnumDisplayMonitors` + `GetMonitorInfo(MONITORINFOEX)` (work area, bounds, primary flag, GDI name) joined to `QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS)` + `DisplayConfigGetDeviceInfo` (source GDI name → target device path, EDID, friendly name). When the display config query fails, the display still appears with the GDI name as `DevicePath` and the name `Display N` | One join gives geometry plus stable identity; the fallback keeps the HUD working on drivers that refuse the query | `System.Windows.Forms.Screen`: adds WinForms, no stable identity |
| DEC-07 | FR-07, FR-08, FR-13, NFR-03 | Re-anchor triggers: `WM_DISPLAYCHANGE`, `WM_SETTINGCHANGE` with `SPI_SETWORKAREA` (0x002F), `SizeChanged` (covers DPI, HUD size, provider count), and placement state changes. Window messages are coalesced by a 300 ms `DispatcherTimer` restarted on each message | Display changes arrive as bursts; one coalesced pass keeps the 2 s target and avoids repeated work. Docked modes never write the settings file on re-anchor, so a display change writes at most once (only Free can correct-and-persist, as today) | Reacting to each message: repeated passes during a burst |
| DEC-08 | FR-01, FR-10, FR-12, FR-17, FR-18 | New `HudPlacementService` (App, `Presentation`) owns the current `HudPositionSettings`, persists through an injected `Func<HudPositionSettings, bool>`, and raises `Changed`. Operations: `SelectMode(mode, hostingDisplay)`, `SelectDisplay(preference)`, `RecordDrag(left, top)`. Every write uses `with` on the current record so no field is dropped | Today `PersistPosition` writes a new `{ Left, Top }` record, which would erase `Mode` and `Display`; one owner removes that class of bug and lets Settings, menu, and drag agree | Static `Current` singleton like `HudScale`: hides the persistence dependency from the signature |
| DEC-09 | FR-18 | `SelectMode` from Free to a docked mode receives the display that hosts the HUD; when it differs from the resolved preferred display, the preference becomes that specific display. Choosing Free stores the window's current `Left`/`Top` | PRD FR-18 and the HUD stays visually put when switching to Free | Ignoring the hosting display: HUD jumps back to the old display |
| DEC-10 | FR-10 | A drag switches to Free only when the window actually moved (`Left`/`Top` differ after `DragMove`) | `DragMove` also runs on a plain click; a click on a docked HUD must not change the mode | Switching on any mouse down: clicks would undock the HUD |
| DEC-11 | FR-03, FR-04, FR-05 | Pure `HudEdgeLayout.For(mode)` returns the edge (`Top`, `Left`, `Right`, `None` for Free) and `IsVertical`; the WPF mapping (stack orientation, `Grid` gutter, `CornerRadius`, `BorderThickness`, ring margin, min size swap, popup placement) lives in a new `NotchWindow.Dock.cs` partial. Tooltips read the shared observable `HudDockLayout.Current.TooltipPlacement` | Tooltips render in their own visual tree (same reason `HudScale` is static, see `tasks/prd-13-hud-size/workflow.md`). Keeping the mapping pure makes it testable in the net10.0 test project, which cannot reference WPF types | A `DataTrigger` set in XAML only: not reachable from the tooltip tree, not testable |
| DEC-12 | FR-02, FR-03, FR-04 | Shadow gutter per edge: Top Center and Free `12,0,12,12`; Top Left `0,0,12,12`; Top Right `12,0,0,12`; Right edge `12,12,0,12`; Left edge `0,12,12,12`. The window rectangle is docked flush; the gutter on the docked sides is zero, so the capsule is flush too | Today the gutter is part of the window, so a flush window with a left gutter would leave a 12 DIP gap at Top Left | Offsetting the window by the gutter: pushes part of the window off the work area into the neighbor display |
| DEC-13 | FR-01, FR-06, FR-12 | Settings section "HUD placement" in the General tab applies on selection (no Apply button), with an inline save error; the display list is built when Settings opens | PRD FR-01 requires immediate application; `SettingsViewModel` already applies provider toggles immediately. UX note: the adjacent HUD size card keeps its Apply button, so the General tab mixes both models (the PRD UX line "like HUD size" refers to the live HUD effect, not the button). A display plugged in while Settings is open shows up the next time Settings opens (limitation) | Apply button like HUD size: contradicts FR-01 |
| DEC-14 | FR-05 | Tooltip placement: top docks and Free keep today's default; Right edge uses `PlacementMode.Left`; Left edge uses `PlacementMode.Right`. Status popup: `Bottom` for top docks and Free (WPF flips it above when there is no room), `Left`/`Right` for side docks | WPF popups reposition inside the screen when the requested side does not fit, which covers FR-05's "no clipping" | Custom placement callback: more code for the same result |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.Infrastructure/Configuration/HudDockMode.cs` | New | Enum `TopLeft, TopCenter, TopRight, LeftEdge, RightEdge, Free` | — |
| CMP-02 | `src/TokenHound.Infrastructure/Configuration/HudDisplayPreference.cs` | New | Record `DevicePath`, `EdidKey`, `Name` (all `string?`) | — |
| CMP-03 | `src/TokenHound.Infrastructure/Configuration/HudPositionSettings.cs` | Modified | Adds `Mode`, `Display`, and `ResolveMode()` returning `(HudDockMode Mode, string? Warning)` per DEC-02 | CMP-01, CMP-02 |
| CMP-04 | `src/TokenHound.App/UI/Placement/DisplayInfo.cs` | New | Pure record: `DevicePath`, `EdidKey`, `Name`, `IsPrimary`, `Number`, `WorkArea` (`ScreenBounds`, physical px), `Width`/`Height` (mode resolution in px) | `ScreenBounds` |
| CMP-05 | `src/TokenHound.App/UI/Placement/DisplayResolver.cs` | New | Pure: `Resolve(HudDisplayPreference?, IReadOnlyList<DisplayInfo>)` → `DisplayResolution { Target, IsFallback }` per DEC-05; `ToPreference(DisplayInfo)`; `FindHosting(displays, windowCenter)` | CMP-02, CMP-04 |
| CMP-06 | `src/TokenHound.App/UI/Placement/NotchPlacement.cs` | Modified | Adds `Dock(HudDockMode, ScreenBounds workArea, double width, double height)` → `(Left, Top)` for the five docked modes (rounded to whole pixels) | CMP-01 |
| CMP-07 | `src/TokenHound.App/UI/Placement/HudEdgeLayout.cs` | New | Pure: `For(HudDockMode)` → `HudEdge Edge` (`None`, `Top`, `Left`, `Right`) and `IsVertical` | CMP-01 |
| CMP-08 | `src/TokenHound.App/Interop/DisplayCatalog.cs` (+ `DisplayCatalog.Native.cs` for P/Invoke) | New | `GetDisplays()` per DEC-06; never throws, returns at least the primary display from `GetMonitorInfo` on failure and logs | CMP-04 |
| CMP-09 | `src/TokenHound.App/Interop/WindowPlacement.cs` | Modified | Adds `GetWindowPixelBounds(hwnd)` (`GetWindowRect`) and `MoveWindowTo(hwnd, x, y)` (`SetWindowPos` with `SWP_NOSIZE \| SWP_NOZORDER \| SWP_NOACTIVATE`, never `SWP_SHOWWINDOW`) | — |
| CMP-10 | `src/TokenHound.App/Presentation/HudPlacementService.cs` | New | Owns placement state and persistence per DEC-08/09; `Changed` event; `Create(HudPositionStore)` factory | CMP-03, CMP-05 |
| CMP-11 | `src/TokenHound.App/Presentation/HudDockLayout.cs` | New | Shared observable `Current` with `TooltipPlacement` (`PlacementMode`) for tooltip trees | CMP-07 |
| CMP-12 | `src/TokenHound.App/UI/Windows/NotchWindow.Placement.cs` | Modified | `ApplyPlacement`: Free → existing clamp path; docked → catalog → resolver → `Dock` → `MoveWindowTo`; coalescing timer (DEC-07) | CMP-05, CMP-06, CMP-08, CMP-09, CMP-10 |
| CMP-13 | `src/TokenHound.App/UI/Windows/NotchWindow.Dock.cs` | New | Applies edge chrome (DEC-11/12/14), orientation-aware minimums (replaces the fixed pair in `NotchWindow.Scale.cs`), the "Position" submenu handlers and check state (FR-17) | CMP-07, CMP-10, CMP-11 |
| CMP-14 | `src/TokenHound.App/UI/Windows/NotchWindow.xaml` / `.xaml.cs` / `.Scale.cs` | Modified | Named elements for the chrome (`RootGrid`, `RingsPanel` via `ItemsPanel` binding), "Position" submenu with six `Tag`ged items, `WM_SETTINGCHANGE` handling, drag → `RecordDrag` only when moved (DEC-10), `Placement` property receiving CMP-10 | CMP-10, CMP-13 |
| CMP-15 | `src/TokenHound.App/UI/Controls/ProviderRing.xaml` | Modified | `ToolTip Placement` bound to `HudDockLayout.Current.TooltipPlacement` | CMP-11 |
| CMP-16 | `src/TokenHound.App/ViewModels/HudPlacementSettingsViewModel.cs` | New | Settings section: `Modes`, `SelectedMode`, `Displays` (Primary monitor + connected + disconnected preference), `SelectedDisplay`, `IsDisplayEnabled`, `DisplayHint`, `SaveError`; applies through CMP-10 on selection | CMP-04, CMP-05, CMP-10 |
| CMP-17 | `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `UI/Windows/SettingsWindow.xaml`, `App.xaml.cs` | Modified | `HudPlacement` init property; "HUD placement" section in the General tab under HUD size; App creates CMP-10 once, hands it to `NotchWindow` and to `CreateSettingsViewModel` | CMP-10, CMP-16 |
| CMP-18 | `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` | Modified | Links CMP-04..07, CMP-10, CMP-16 like the existing App links | — |

Flow. At startup `App` creates `HudPlacementService` from `HudPositionStore.Load()` and passes it to `NotchWindow` and to each Settings view model. `NotchWindow.ApplyPlacement` asks the service for `ResolveMode()`; it logs the warning once when present. Free runs the existing DIP clamp path and persists a corrected position as today. A docked mode reads `DisplayCatalog.GetDisplays()`, resolves the target with `DisplayResolver`, reads the window's pixel size, computes the docked rectangle with `NotchPlacement.Dock`, and calls `WindowPlacement.MoveWindowTo`; nothing is written. A mode change also updates `HudDockLayout.Current` and the window chrome before the move, so the size used for docking already reflects the new orientation (the following `SizeChanged` re-anchors once more). Settings, the context menu, and the drag gesture call the service; the service persists and raises `Changed`; `NotchWindow` handles `Changed` by applying chrome and placement.

## Contracts and data

Section `Hud` of the user settings JSON (`HudPositionStore`, unchanged section name and store):

| Field | Type | Required | Meaning and validation |
| --- | --- | --- | --- |
| `Left`, `Top` | number? | no | Free-mode position in DIPs (unchanged). Ignored by docked modes but preserved |
| `Mode` | string? | no | One of `TopLeft`, `TopCenter`, `TopRight`, `LeftEdge`, `RightEdge`, `Free`, compared with `OrdinalIgnoreCase`. Resolution per DEC-02 |
| `Display` | object? | no | `null` = "Primary monitor". Otherwise `DevicePath` (string), `EdidKey` (string, `"MMMM:PPPP"` hex), `Name` (string, display only) |

Examples:

```json
"Hud": { "Left": 1210, "Top": 0 }
```
Loads as Free at (1210, 0) — an existing file from the previous version (FR-15).

```json
"Hud": { "Left": 1210, "Top": 0, "Mode": "RightEdge", "Display": { "DevicePath": "\\\\?\\DISPLAY#DELA1F2#5&2b3c...#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}", "EdidKey": "10AC:A1F2", "Name": "DELL U2723QE" } }
```

Compatibility: older versions read this section with only `Left`/`Top` and ignore the extra fields (`PropertyNameCaseInsensitive`, no strict members), so a downgrade keeps a usable position. `UserSettingsFile.MergeWithDefaults` copies `Hud` whole, so new fields survive load and save. The shipped `appsettings.json` defaults stay without a `Hud` section (FR-14).

## Integrations and interfaces

- **Win32 display APIs** (CMP-08): `EnumDisplayMonitors`, `GetMonitorInfoW` with `MONITORINFOEXW` (`rcWork`, `dwFlags & MONITORINFOF_PRIMARY`, `szDevice`), `GetDisplayConfigBufferSizes` + `QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS)`, `DisplayConfigGetDeviceInfo` with `DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME` (`viewGdiDeviceName`) and `DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME` (`monitorFriendlyDeviceName`, `monitorDevicePath`, `edidManufactureId`, `edidProductCodeId`, `flags.edidIdsValid`). Mode resolution from `EnumDisplaySettingsW(szDevice, ENUM_CURRENT_SETTINGS)`. All read-only, synchronous, sub-millisecond; called on the UI thread only from the coalesced placement pass and when Settings opens. Reference: Microsoft Learn, "QueryDisplayConfig" and "DISPLAYCONFIG_TARGET_DEVICE_NAME".
- **Window positioning** (CMP-09): `GetWindowRect`, `SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE)`. `SWP_SHOWWINDOW` is never used (AGENTS.md).
- **Window messages** (CMP-14): existing `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` stays first in `WndProc`; `WM_DISPLAYCHANGE` and `WM_SETTINGCHANGE` (`wParam == SPI_SETWORKAREA`) restart the coalescing timer.
- **HUD UI**: context menu gains a "Position" submenu (six checkable items, `HudMenuItemStyle`); Settings General tab gains "HUD placement" (two `ComboBox`es with `AutomationProperties.Name`, hint text, error text).

## Errors, security, and recovery

- Errors and edges:
  - Display query failure → DEC-06 fallback; total enumeration failure → the primary from `MonitorFromPoint(0,0, MONITOR_DEFAULTTOPRIMARY)`; logged once per pass at warning level.
  - Preferred display missing → primary at the same docked mode (FR-08); preference kept; Settings shows "(disconnected — showing on primary)".
  - HUD larger than the work area (tiny display, huge scale) → `Dock` clamps to the work area origin on the overflowing axis.
  - Settings save failure → state still applied for the session, `SaveError` shown, warning logged (same as HUD size).
  - Unknown or inconsistent stored values → DEC-02 fallback with one warning (FR-16).
- Credentials and sensitive data: none. Display names and device paths are not sensitive; logged at debug level only.
- Concurrency and idempotency: everything runs on the UI dispatcher. `_applyingPlacement` keeps guarding reentrancy from `SizeChanged` and DPI changes; the coalescing timer collapses message bursts; docked passes are idempotent (same inputs → same `SetWindowPos`, skipped when the window is already at the target pixel position).
- Rollback or reversal: removing the feature leaves `Mode`/`Display` as unknown fields that older code ignores; `Left`/`Top` still drive the older placement. A user can return to today's behavior by choosing Top Center or Free.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| 1. Settings contract (CMP-01..03) | — | `HudPositionStore` round-trip and DEC-02 resolution tests pass |
| 2. Pure placement (CMP-04..07, CMP-18 links) | 1 | Dock rectangles, display resolution, and edge layout tests pass |
| 3. Placement service (CMP-10) | 1, 2 | Service tests for FR-10, FR-18, Free selection, preserved fields pass |
| 4. Display catalog and window move interop (CMP-08, CMP-09) | 2 | App builds with 0 warnings; startup logs the connected displays at debug level |
| 5. Docking engine (CMP-12, CMP-14 placement part, CMP-17 wiring) | 3, 4 | HUD docks at all positions on the primary display; drag → Free; submenu (MA-1, MA-2 positions, MA-4) |
| 6. Edge chrome (CMP-11, CMP-13, CMP-14 chrome part, CMP-15) | 5 | Vertical capsule, outline, tooltips, and popup follow the edge (MA-2 side edges, MA-3) |
| 7. Settings section (CMP-16, CMP-17) | 3, 4, 6 | View model tests pass; Settings controls drive the HUD live (MA-5..MA-8) |

## Test approach

- Profile: `TokenHound.App` is a WPF desktop app (`net10.0-windows`, `UseWPF`); `TokenHound.Infrastructure.Tests` (`net10.0`, xUnit + AwesomeAssertions on Microsoft.Testing.Platform) links the pure App files it tests. Commands from `AGENTS.md`:
  - Build: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore` and `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`.
  - Tests: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`, filtered with `--filter-class "*<Name>*"` while iterating.
- E2E: omitted by .NET desktop policy.
- Command prerequisites and exclusions: the App must not be running when building (MSB3027 file lock); stop the HUD process before building. No UI automation suites exist.
- Manual acceptance (owner: the human, at the visual check; the agent runs MA-1..MA-4 on the primary display through the Windows MCP `App` launch and `Screenshot` with `display: [2]`):

| MA | Steps | Expected result |
| --- | --- | --- |
| MA-1 | Remove the `Hud` section from the user settings file; start the app | HUD centered on the top edge of the primary display, as before (FR-14) |
| MA-2 | Context menu > Position > each of Top Left, Top Right, Left edge, Right edge, Top Center | HUD jumps to each position flush with the work-area edge; side edges show a vertical capsule with rounded inner corners; the current item is checked (FR-02..04, FR-17) |
| MA-3 | At Right edge and Left edge, hover each ring and trigger Refresh | Tooltip opens toward the inside of the screen; status popup opens beside the capsule, nothing clipped (FR-05) |
| MA-4 | Docked at Right edge, click the HUD without moving; then drag it to the middle | Click keeps Right edge; drag switches to Free (Position submenu shows Free), restart keeps the dropped position (FR-10, DEC-10) |
| MA-5 | Settings > General > HUD placement: pick each display, then "Primary monitor" | HUD moves to that display at the same mode immediately; the list shows name, resolution, and Primary (FR-06) |
| MA-6 | With a specific secondary display chosen, disconnect it; reconnect it | HUD moves to the primary within 2 s; Settings shows the display as disconnected; on reconnect the HUD returns within 2 s (FR-08) |
| MA-7 | Docked Top Right on a 150% display next to a 100% display; move the taskbar to the top; change HUD size to 125%; switch the preferred display between them | Capsule stays flush (≤ 1 px) and fully visible after each change, no oscillation (FR-13, NFR-02) |
| MA-8 | Upgrade path: put `"Hud": { "Left": 500, "Top": 300 }` in the settings file; start | HUD at (500, 300), Settings shows Free and a disabled display selector with the hint (FR-12, FR-15) |

MA-5..MA-7 need a second display (physical or virtual); without it they remain pending at HIL 3.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-14, FR-15, FR-16, DEC-02 | unit | `ResolveMode` for: empty; only `Left`/`Top`; each valid mode in any casing; unknown string; `Free` without coordinates | TopCenter; Free; that mode; TopCenter + warning; TopCenter + warning | Infrastructure.Tests `HudPositionSettingsTests` |
| TC-02 | NFR-04, DEC-01 | unit | Store round-trip of `Mode` + `Display`; loading a file with an unknown `Mode` keeps `Left`/`Top`; saving `Hud` keeps `HudSize` and other sections | Values preserved; no section lost | Infrastructure.Tests `HudPositionStoreTests` |
| TC-03 | FR-02, FR-03, DEC-12 | unit | `Dock` for the five modes on offset and negative-origin work areas, odd sizes, and a window larger than the work area | Flush edges, exact center (whole pixels), clamped origin on overflow | `NotchPlacementTests` |
| TC-04 | FR-06, FR-07, FR-08, FR-09, DEC-05 | unit | `DisplayResolver.Resolve`: null preference; exact device path; device path missing but unique EDID key; ambiguous EDID key; nothing matches; no primary flagged | Primary; that display; EDID match; primary + fallback; primary + fallback; first display | `DisplayResolverTests` |
| TC-05 | FR-03, FR-04, DEC-11 | unit | `HudEdgeLayout.For` for the six modes | Top/None/Left/Right edges and `IsVertical` only for side edges | `HudEdgeLayoutTests` |
| TC-06 | FR-10, FR-18, DEC-08, DEC-09 | unit | Service: `RecordDrag` sets Free + coordinates and keeps `Display`; `SelectMode` from Free with a hosting display that differs from the preference; same display under "Primary monitor"; choosing Free stores current coordinates; failed save raises `Changed` and reports failure | Fields preserved, preference rule as in FR-18, one save per operation | `HudPlacementServiceTests` |
| TC-07 | FR-06, FR-08, FR-12, DEC-13 | unit | View model: display list with primary, secondary, and a disconnected preference; Free disables the display selector with the hint; selecting a mode or display calls the service once; save failure shows `SaveError` | List order and labels as specified; enable state and hint; single call per selection | `HudPlacementSettingsViewModelTests` |
| TC-08 | FR-01..FR-05, FR-17 | manual | MA-1..MA-4 | As in the manual table | Windows MCP launch + screenshot |
| TC-09 | FR-06..FR-09, FR-12, FR-13, FR-15, NFR-02 | manual | MA-5..MA-8 | As in the manual table | Human visual check |
| TC-10 | NFR-01 | manual + grep | During MA-2..MA-5, the foreground app keeps focus; QA-04 grep on the diff | No activation; no `SWP_SHOWWINDOW` | Visual check + QA-04 |
| TC-11 | NFR-03, DEC-07 | unit + manual | Docked re-anchor never calls save (service test with a counting save); MA-6 log shows one coalesced pass per burst | Zero writes in docked passes | `HudPlacementServiceTests` + log at MA-6 |

## Quality profile

Rules this feature can violate. A blocking hit prevents task completion and rejects the review; a reservation becomes an optional improvement and counts toward escalation. A hit covered by `DEC-NN` is expected, not a finding.

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | Empty `catch` or swallowed `Exception` around interop | blocking | `rtk rg -n --type cs @src 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | — |
| QA-02 | `#pragma warning disable` / `#nullable disable` (P/Invoke structs tempt this) | blocking | `rtk rg -n --type cs @src '#nullable disable\|#pragma warning disable' $files` | — |
| QA-03 | `async void` or sync-over-async | blocking | `rtk rg -n --type cs @src 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | WPF event handlers excepted |
| QA-04 | HUD activation or z-order invariants broken | blocking | `rtk rg -n @src 'SWP_SHOWWINDOW\|0x0040\|Activate\(\)' $files` | — |
| QA-05 | Parameter list of 4+ (call or declaration) not split per `AGENTS.md`, or a 4+ parameter API that should be a record | reservation | `rtk rg -n --type cs @src '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | `WndProc` signature is fixed by WPF; Win32 P/Invoke signatures mirror the native API |
| QA-06 | File above 300 lines or method above 30 lines (`AGENTS.md`) | reservation | `rtk rg -c '^' --type cs @src $files` | — |
| QA-07 | Persisting `HudPositionSettings` built with `new` instead of `with` (drops fields) | blocking | `rtk rg -n --type cs @src 'new HudPositionSettings' $files` | The empty default in `UserSettingsFile.MergeWithDefaults` and tests are excepted |

- Verification scope: files in the task diff (`$files`).
- Escalation trigger: 8+ reservation hits in the feature, a touched file above 500 lines, or the same block duplicated in 3+ places.

### Terrain baseline

Measured at `c4b55b9`. A target file without a row counts as unmeasured.

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.App/App.xaml.cs` | 468 | — | — | 0 | QA-06: file above 300 lines; QA-05: `App.xaml.cs:379` | recorded (contact: two lines of wiring) |
| `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` | 949 | — | — | — | above 500 lines (XAML) | recorded (contact: one new section) |
| `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` | 258 | 2 | 0 | 0 | QA-05: `NotchWindow.xaml.cs:106` (`WndProc`, fixed signature) | recorded; new code goes to partials to stay ≤ 300 |
| `src/TokenHound.App/UI/Windows/NotchWindow.Placement.cs` | 66 | 0 | 0 | 0 | — | — |
| `src/TokenHound.App/UI/Windows/NotchWindow.Scale.cs` | 33 | 0 | 0 | 0 | — | — |
| `src/TokenHound.App/UI/Windows/NotchWindow.xaml` | 131 | — | — | — | — | — |
| `src/TokenHound.App/UI/Placement/NotchPlacement.cs` | 49 | 2 | 0 | 0 | — | — |
| `src/TokenHound.App/Interop/WindowPlacement.cs` | 204 | 4 | 0 | 0 | — | — |
| `src/TokenHound.App/UI/Controls/ProviderRing.xaml` | 60+ | — | — | — | — | — (contact: one binding) |
| `src/TokenHound.App/ViewModels/SettingsViewModel.cs` | 182 | 1 | 4 | 0 | — | — |
| `src/TokenHound.Infrastructure/Configuration/HudPositionSettings.cs` | 32 | 1 | 0 | 0 | — | — |
| `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs` | 100 | 5 | 0 | 0 | — | — (not modified unless a test needs it) |
| `tests/TokenHound.Infrastructure.Tests/Placement/NotchPlacementTests.cs` | 173 | 10 | 0 | 0 | — | — |
| `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionStoreTests.cs` | 206 | 11 | 0 | 0 | — | — |

- Preparatory refactoring: not recommended. `App.xaml.cs` and `SettingsWindow.xaml` cross a structural threshold but the feature touches each in one or two places (no contact). `NotchWindow.xaml.cs` is healthy and new behavior goes to partial files, keeping it below 300 lines.

## Observability and rollout

- Signals: `Log.Debug("HUD placement {Mode} on {Display} at {X},{Y}")` per applied docked pass; `Log.Debug` of enumerated displays (name, device path, primary) when the list changes; `Log.Warning` for DEC-02 fallbacks (once per load), display query failures (once per pass), and save failures.
- Migration and compatibility: no write on load; `Left`/`Top`-only files resolve to Free (FR-15); new fields are ignored by older builds.
- Rollout and rollback: ships with the next release; no feature flag. Rollback is a version downgrade (compatible, see Contracts).

## Risks and open items

- Risk: the process DPI awareness mode is not declared in a manifest. DEC-03 keeps docking correct in either mode because all docked math is in the same physical coordinate space as `GetMonitorInfo` and `SetWindowPos`; MA-7 confirms on mixed-scale displays. Probability low, impact medium (off-by-scale placement).
- Risk: some drivers return empty friendly names or no EDID ids. Mitigation: DEC-06 fallback names (`Display N`) and device-path-only matching. Probability medium, impact low (generic name in Settings).
- Risk: two identical monitors swapped between ports resolve as ambiguous and fall back to the primary (DEC-05). Accepted; the user can pick the display again.
- Risk: a cross-DPI move shows one frame at the pre-resize size before `SizeChanged` re-anchors. Accepted; NFR-02 forbids oscillation, not a single correction pass.
- Open item: none blocking. MA-5..MA-7 need a second display at the visual check.

## Relevant files

- Modify: `src/TokenHound.Infrastructure/Configuration/HudPositionSettings.cs`, `src/TokenHound.App/UI/Placement/NotchPlacement.cs`, `src/TokenHound.App/Interop/WindowPlacement.cs`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml`, `NotchWindow.xaml.cs`, `NotchWindow.Placement.cs`, `NotchWindow.Scale.cs`, `src/TokenHound.App/UI/Controls/ProviderRing.xaml`, `src/TokenHound.App/ViewModels/SettingsViewModel.cs`, `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, `src/TokenHound.App/App.xaml.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`, `tests/TokenHound.Infrastructure.Tests/Placement/NotchPlacementTests.cs`, `tests/TokenHound.Infrastructure.Tests/Configuration/HudPositionStoreTests.cs`.
- Create: `src/TokenHound.Infrastructure/Configuration/HudDockMode.cs`, `HudDisplayPreference.cs`; `src/TokenHound.App/UI/Placement/DisplayInfo.cs`, `DisplayResolver.cs`, `HudEdgeLayout.cs`; `src/TokenHound.App/Interop/DisplayCatalog.cs`, `DisplayCatalog.Native.cs`; `src/TokenHound.App/Presentation/HudPlacementService.cs`, `HudDockLayout.cs`; `src/TokenHound.App/UI/Windows/NotchWindow.Dock.cs`; `src/TokenHound.App/ViewModels/HudPlacementSettingsViewModel.cs`; tests `HudPositionSettingsTests`, `DisplayResolverTests`, `HudEdgeLayoutTests`, `HudPlacementServiceTests`, `HudPlacementSettingsViewModelTests` under `tests/TokenHound.Infrastructure.Tests/`.
