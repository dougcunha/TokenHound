# Stable execution context

Load in this exact order:

1. `tasks/prd-14-hud-multimonitor-docking/prd.md`
2. `tasks/prd-14-hud-multimonitor-docking/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T05 — HUD docking engine and Position submenu

## Outcome

The HUD applies the resolved mode: docked modes move the window flush to the chosen display's work-area edge in pixels; display and work-area changes re-anchor through a coalescing timer; a real drag switches to Free; the context menu has a "Position" submenu with the current mode checked. Free keeps today's clamp path. The capsule keeps today's horizontal chrome in every mode until T06.

## Dependencies and boundaries

- Depends on: T03, T04
- Unblocks: T06
- In scope: `NotchWindow.Placement.cs` rewrite, coalescing timer, `WM_SETTINGCHANGE` handling, drag → Free rule, `Placement` property on `NotchWindow`, Position submenu, `App` wiring that creates `HudPlacementService` and hands it to the window.
- Out of scope: edge chrome, vertical orientation, tooltip and popup direction, orientation-aware minimums (T06); Settings section (T07).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01, FR-02, FR-07, FR-08, FR-10, FR-11, FR-13, FR-14, FR-16, FR-17, FR-18 | `prd.md#functional-requirements` | Docking positions, display events, drag, submenu, defaults and fallback |
| NFR-01, NFR-02, NFR-03 | `prd.md#non-functional-requirements` | No activation; pixel space; single write |
| DEC-03, DEC-04, DEC-07, DEC-08, DEC-09, DEC-10 | `techspec.md#technical-decisions` | Pixel docking; Free path; coalescing; service owner; display rule; drag rule |
| CMP-12, CMP-14, CMP-17 (App wiring part) | `techspec.md#components-and-flow` | Window placement and wiring |
| TC-08 (MA-1, MA-2 top docks, MA-4), TC-10, TC-11 | `techspec.md#test-approach` | Manual checks; focus; coalescing |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (C# style, ≤ 300 lines per file, ≤ 30 per method; HUD non-activating invariants), `dotnet-efficient-validation`, TechSpec quality profile QA-01..QA-07.
- Existing code: `NotchWindow.xaml` (36-62 context menu); `NotchWindow.xaml.cs` (`WndProc`, `OnMouseLeftButtonDown`, `PersistPosition`, `OnCapsuleContextMenuOpening`); `NotchWindow.Placement.cs` (`_applyingPlacement`); `App.xaml.cs:376-400` (window creation).
- Validation: `AGENTS.md` "Running & validating desktop HUD" (Windows MCP `App` launch, `Screenshot` with `display: [2]`).

## Work

- [x] T05.1 Rewrite `NotchWindow.Placement.cs`: Free → existing clamp + `HudPlacementService.UpdateFreePosition`; docked → `DisplayCatalog` → `DisplayResolver` → `NotchPlacement.Dock` → `WindowPlacement.MoveWindowTo` (skip when already there); log the DEC-02 warning once; add the 300 ms coalescing timer.
- [x] T05.2 Update `NotchWindow.xaml(.cs)`: Position submenu (six `Tag`ged checkable items, check state on menu opening), `WM_SETTINGCHANGE`/`SPI_SETWORKAREA` and `WM_DISPLAYCHANGE` restart the timer, drag calls `RecordDrag` only when the window moved (DEC-10), a `Placement` property subscribes to `Changed`; remove the direct `HudPositionStore` use. Put the submenu handlers in a new partial if `NotchWindow.xaml.cs` would pass 300 lines.
- [x] T05.3 `App.xaml.cs`: create one `HudPlacementService` from `HudPositionStore`, keep it in a field for T07, assign it to the window before `Show()`.
- [x] T05.4 Build, run all tests, stop any running instance, launch through Windows MCP, and run MA-1, the top-dock part of MA-2, and MA-4 on the primary display with screenshots.

## Acceptance criteria

- MA-1 passes; Top Left, Top Center, and Top Right dock flush on the primary display; Left edge and Right edge dock flush to their edge, vertically centered (still horizontal chrome until T06).
- A plain click on a docked HUD keeps the mode; a drag switches to Free and survives a restart (MA-4).
- Docked passes never call the save function (no "Persisted HUD position" debug line during docked re-anchors).
- `NotchWindow.xaml.cs` stays ≤ 300 lines; QA-04 and QA-07 greps clean.

## Verification

- Unit: none new (logic tested in T01–T03); the full suite must stay green.
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: MA-1, MA-2 (top docks and side positions), MA-4 by the agent on the primary display.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`.
- Environment dependency: Windows MCP desktop access; stop the running HUD before building (MSB3027).
- Expected evidence: builds 0 warnings; test count; screenshots of Top Left, Top Right, and Right edge positions; log excerpt with the placement lines.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/NotchWindow.xaml`, `NotchWindow.xaml.cs`, `NotchWindow.Placement.cs`, `src/TokenHound.App/App.xaml.cs`
- Create (if needed for the line limit): `src/TokenHound.App/UI/Windows/NotchWindow.Menu.cs`

## Observability and recovery

- Operational signal: `Log.Debug("HUD placement {Mode} on {Display} at {X},{Y}")`; DEC-02 warning once per load.
- Recovery: choosing Top Center reproduces today's placement; Free reproduces today's drag behavior.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: docking engine. `NotchWindow.Placement.cs` resolves the mode through `HudPlacementService`; Free keeps the `c4b55b9` clamp and stores corrections with `UpdateFreePosition`; docked modes resolve the display, compute the pixel position with `NotchPlacement.Dock`, and call `WindowPlacement.MoveWindowTo` (skipped when already there; never saves). `WM_DISPLAYCHANGE` and `WM_SETTINGCHANGE`/`SPI_SETWORKAREA` restart a 300 ms coalescing `DispatcherTimer`. A drag calls `RecordDrag` only when `Left`/`Top` changed. Position submenu with six checkable items (`NotchWindow.Menu.cs`), checks refreshed on menu opening. `App` creates one `HudPlacementService` and passes it and the `DisplayCatalog` to the window. The direct `HudPositionStore` use and `PersistPosition` were removed from `NotchWindow.xaml.cs`.
- Deviation inside the contract: `HudMenuItemStyle` has no submenu popup or check mark, so `DialogResources.xaml` gained `HudSubmenuItemStyle` and `HudCheckMenuItemStyle` (same look, `PART_Popup`, Segoe MDL2 chevron and check glyphs).
- Fix found in manual testing: the first launch centered the capsule ~32 px off because `GetWindowRect` still returns the previous size during `SizeChanged`; docking now uses `ActualWidth`/`ActualHeight` × the window DPI scale, and the next launch centered exactly (capsule 813..1107 on a 1920 display).
- Changed files: `src/TokenHound.App/UI/Windows/NotchWindow.Placement.cs` (rewritten), `NotchWindow.Menu.cs` (new), `NotchWindow.xaml.cs`, `NotchWindow.xaml`, `src/TokenHound.App/UI/Styles/DialogResources.xaml`, `src/TokenHound.App/App.xaml.cs`.
- Checks: App build 0 warnings; test build 0 warnings; Infrastructure.Tests 1035 passed. Manual on the primary display (dev build via Windows MCP, user settings backed up): MA-1 pass (no `Hud` section → Top Center on primary, flush top, centered; no `Hud` section written by docked passes); MA-2 positions pass (submenu shows the current mode checked; Top Left flush to the corner window; Right edge flush and vertically centered; capsule chrome still horizontal with the 18 px gutter until T06); MA-4 pass (plain click kept `RightEdge`; drag stored `{"Left":592,"Top":308.67,"Mode":"Free","Display":null}`; restart restored the dropped position). No focus change was observed (no foreground window reported by the screenshots).
- Validated state: working tree at `c4b55b9` plus T01..T05.
- Quality profile: QA-01..QA-04, QA-07 clean. QA-05: `App.xaml.cs:383` and `NotchWindow.xaml.cs:104` are baseline hits; the new 4-argument log call was split. QA-06: `App.xaml.cs` 474 lines (baseline 468); `NotchWindow.xaml.cs` 245; `NotchWindow.Placement.cs` 196.
- Open items: the submenu was captured mid-fade once (translucent); fully opaque on the next capture.

### ADR candidates

None - direct TechSpec implementation or local decision.
