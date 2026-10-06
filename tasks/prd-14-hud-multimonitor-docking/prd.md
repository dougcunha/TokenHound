# PRD — HUD multi-monitor and edge docking

## Problem and context

The HUD capsule is a horizontal pill that starts centered on the top edge of the primary work area (`NotchPlacement.CenterOnTopEdge`, `NotchWindow.Placement.cs:24-25`). Users can only drag it freely; the saved placement is a bare `Left`/`Top` pair (`HudPositionSettings.cs:6-17`) with no notion of which display it belongs to or which edge it hugs. Commit `c4b55b9` already clamps a restored position to the nearest monitor work area after a display change, but:

- there is no way to say "keep the HUD on my second monitor" or "keep it on the primary, whatever that is", so the HUD lands wherever the clamp puts it after docking, undocking, or a resolution change;
- the HUD cannot be pinned to a corner or to a side edge; a hand-dragged position drifts away from the edge when the work area or DPI changes;
- the HUD is always horizontal, while the product design (`docs/design/2026-08-28-usage-notch-design.md`, "The shape of the thing") describes a vertical pill on a side edge, which suits wide and ultra-wide screens where vertical space is scarce.

Affected users: anyone with more than one display, laptops that dock and undock, and users who prefer the HUD out of the top edge. ROADMAP lists this as the next Phase 3 item ("PRD 13: Multi-Monitor & Edge Docking") and as a prerequisite for the HUD geometry polish item, which needs a stable docking edge and orientation contract.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | The user chooses where the HUD lives: a docked position on a chosen display, or a free position | Each docked position on each connected display is reachable from Settings and the HUD stays there across restarts (manual acceptance) |
| OBJ-02 | The HUD stays where the user put it when displays change | After connect, disconnect, primary change, resolution, scale, or taskbar change, a docked HUD is on its display and flush with its edge; no manual drag needed (manual acceptance plus placement unit tests) |
| OBJ-03 | Upgrading does not move anyone's HUD | A settings file containing only `Left`/`Top` opens the HUD at the same position as before the upgrade (migration test plus manual acceptance) |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Multi-monitor user | Keep the HUD on a specific display | It is always on the screen they look at | Settings > HUD placement > Display = "DELL U2723QE" and Position = Top Right; restart keeps it there |
| US-02 | Laptop user who docks and undocks | Keep the HUD on the external display when docked and still see it when undocked | The HUD never disappears or ends up in an odd spot | External display unplugged: HUD moves to the primary at the same docked position; plugged back: HUD returns to the external display |
| US-03 | Wide or ultra-wide screen user | Dock the HUD as a vertical pill on the left or right edge | It does not take vertical space above windows | Position = Right edge: the capsule stacks providers vertically, centered on the right edge; tooltips open to its left |
| US-04 | Existing user who already dragged the HUD | Keep the HUD where it is after updating | No surprise after an update | Upgrade with saved `Left`/`Top`: HUD shows at the same place in Free mode |
| US-05 | Any user | Fine-tune by dragging | Quick ad-hoc placement without opening Settings | Dragging a docked HUD switches it to Free and stores the new coordinates |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | The HUD placement mode is one of: Top Left, Top Center, Top Right, Left edge, Right edge, Free | Settings lists exactly these six options; selecting one applies it immediately, without restarting the app |
| FR-02 | Top Left, Top Center, and Top Right dock a horizontal capsule to the top edge of the chosen display's work area: flush with the left edge, horizontally centered, or flush with the right edge | With the capsule docked, its top edge equals the work-area top and the left/center/right alignment is exact to within 1 physical pixel |
| FR-03 | Left edge and Right edge dock a vertical capsule to that edge of the chosen display's work area, vertically centered; providers stack top to bottom in the same order as the horizontal capsule | With the capsule docked, its left (or right) edge equals the work-area edge to within 1 physical pixel, and it is vertically centered |
| FR-04 | The capsule outline follows the docked edge: the corners against the screen edge are square and the opposite corners are rounded | Top docks: rounded bottom corners (today's look). Right edge: rounded left corners. Left edge: rounded right corners. Free: today's look |
| FR-05 | Provider tooltips and the HUD status popup open toward the inside of the screen and stay fully visible | Top docks: below the capsule (today). Free: below, flipping above when there is no room below. Right edge: to the left of the hovered element. Left edge: to its right. No tooltip or popup is clipped by the display edge |
| FR-06 | The display is chosen as "Primary monitor" or a specific connected display; the list shows each display's name, resolution, and which one is primary | Settings shows one entry for "Primary monitor", one per connected display, and the stored preferred display when it is disconnected (FR-08); choosing one moves a docked HUD to that display immediately |
| FR-07 | "Primary monitor" follows the current Windows primary display | Changing the primary display in Windows moves a docked HUD to the new primary within 2 seconds |
| FR-08 | When the chosen specific display is not connected, a docked HUD shows on the primary display at the same docked position, and the chosen display is kept as the preference | Unplugging the chosen display moves the HUD to the primary; Settings still shows the chosen display, marked as disconnected; plugging it back returns the HUD to it within 2 seconds |
| FR-09 | A specific display is recognized again after a restart, reconnection, or port change, as long as Windows reports the same physical monitor | After rebooting with the same monitors, the HUD returns to the chosen display |
| FR-10 | Dragging the HUD switches the mode to Free and stores the dropped coordinates; Free does not re-anchor to an edge | After a drag, Settings shows Free; the HUD stays at the dropped position across restarts |
| FR-11 | In Free mode the HUD keeps today's recovery: when its display changes or disappears it is clamped into the nearest display's work area and the corrected position is stored | Same behavior and tests as commit `c4b55b9` (`NotchPlacementTests`) |
| FR-12 | The display selector applies to docked positions only; in Free mode it is disabled and explains that the display follows the dragged position | In Free mode the selector is not editable and shows a hint; switching back to a docked position re-enables it |
| FR-13 | A docked HUD re-anchors after any change of its display's work area, resolution, DPI scale, HUD size setting, or provider count | After each change the capsule is flush with its edge, fully visible, and at the correct alignment within 2 seconds |
| FR-14 | A fresh install, or a settings file without any HUD placement, starts at Top Center on the primary monitor | First run with no settings file shows the HUD centered on the top edge of the primary display, as today |
| FR-15 | A settings file that contains only `Left`/`Top` loads as Free at those coordinates | After upgrading, the HUD shows at the same position as before; Settings shows Free |
| FR-16 | An unreadable or unknown placement value falls back to Top Center on the primary monitor without crashing and is logged | A settings file with an unknown position name opens the HUD at Top Center; the log has one warning naming the field |
| FR-17 | The HUD context menu offers a "Position" submenu with the six modes, the current one checked | Choosing an item there has the same effect as choosing it in Settings and is reflected in Settings the next time it opens |
| FR-18 | Switching from Free to a docked position keeps the HUD on the display it currently occupies: if that display is not the stored preference (or not the primary when the preference is "Primary monitor"), it becomes the specific preferred display | Drag the HUD to display 2 (preference "Primary monitor"), then choose Top Right: the HUD docks at the top right of display 2 and Settings shows display 2. Drag it on the primary and choose Top Right: preference stays "Primary monitor" |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Focus | Placement changes, re-anchoring, and the context-menu submenu never activate the HUD or steal focus from the foreground app; the HUD keeps `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` and the `SWP_NOZORDER`-without-`SWP_SHOWWINDOW` rule from `AGENTS.md` |
| NFR-02 | DPI | Correct on displays with different scale factors (for example 100% and 150%) and when the HUD moves between them: flush within 1 physical pixel, no clipping, no oscillation between two positions |
| NFR-03 | Stability | A display change produces at most one stored placement write; re-anchoring does not loop (no repeated size/position feedback) |
| NFR-04 | Compatibility | Settings written by this version keep working if read again after a later field is added; older `Left`/`Top` files keep working (FR-15) |
| NFR-05 | Testability | Placement rules (target rectangle per mode, display fallback, migration) are covered by unit tests that do not need a real display |
| NFR-06 | Platform | Windows 11 (ROADMAP target), the same release architectures as today, under the existing .NET 10 WPF app |

## User experience

- **Settings** (General tab, next to HUD size): a "HUD placement" section with a Position choice (the six modes) and a Display choice ("Primary monitor" plus each connected display, e.g. `2 — DELL U2723QE · 3840×2160 · Primary`). Changes apply live, like HUD size. A disconnected preferred display stays selected and shows "(disconnected — showing on primary)".
- **HUD context menu**: a "Position" submenu with the six modes; the current one is checked.
- **Drag**: unchanged gesture; dropping switches to Free silently (no toast).
- **Vertical pill**: same rings, glyphs, colors, and percent labels as the horizontal pill, stacked vertically.
- **Errors**: no dialogs; invalid stored values fall back to the default and are logged.
- **Accessibility**: the new Settings controls are keyboard reachable and labeled like the existing HUD size controls.

## Constraints and dependencies

- .NET 10 WPF app; placement rules stay testable without the WPF window (existing `NotchPlacement` pattern).
- HUD non-activating invariants from `AGENTS.md` are mandatory.
- HUD size setting (`prd-13-hud-size`) scales the capsule; docking must use the scaled size.
- Persistence stays in the user settings JSON through the existing settings store pattern, without breaking the separate `HudSize` section.
- Visual validation through the Windows MCP App launch and primary-monitor screenshot flow; multi-display checks need a second physical or virtual display.

## Out of scope

- Bézier capsule geometry, inverse rounded corners, Mica/Acrylic backdrop, click-through hit testing, and ring animations (ROADMAP "HUD Bézier Geometry & Visual Polish").
- Bottom-edge docking.
- Snapping to a docked position while dragging.
- Showing the HUD on more than one display at the same time.
- An offset along the docked edge (for example "right edge, 30% from top").
- Auto-hide or reveal-on-hover behavior.

## Assumptions and sources

- Assumption: work area (display bounds minus the taskbar and app bars) is the right docking reference, so a top dock sits below a top taskbar. Impact if wrong: the capsule would overlap the taskbar.
- Assumption: Windows exposes a stable per-monitor identity that survives reboot and reconnection for the same physical monitor (FR-09). Impact if wrong: after a reconnection on a different port, the HUD falls back to the primary until the user picks the display again; the TechSpec must confirm the identity source.
- Assumption: the context-menu "Position" submenu (FR-17) is welcome as quick access; it was proposed by the agent, not requested. Impact if wrong: drop FR-17 at HIL 1.
- Product decisions: DEC-02 in `workflow.md` (placement model, default and migration, display selection, docked positions).
- Sources: `docs/ROADMAP.md` (PRD 13 track and Phase 3 sequencing), `docs/design/2026-08-28-usage-notch-design.md` (vertical side pill, tooltip to the left), commit `c4b55b9` (nearest-monitor clamp), `src/TokenHound.Infrastructure/Configuration/HudPositionSettings.cs`, `src/TokenHound.App/UI/Placement/NotchPlacement.cs`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml` (horizontal `StackPanel`, popup `Placement="Bottom"`).

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
