# Implementation plan — HUD multi-monitor and edge docking

## Stable sources

- PRD: `tasks/prd-14-hud-multimonitor-docking/prd.md`
- TechSpec: `tasks/prd-14-hud-multimonitor-docking/techspec.md`

> Common sources come before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Persisted placement contract: `Mode` + `Display` in the `Hud` section, mode resolution with migration and fallback, tested on a temp settings file | — | T02, T03 |
| T02 | Pure placement rules: docked rectangles, display resolution, edge layout, linked into the test project | T01 | T03, T04 |
| T03 | `HudPlacementService` owns placement state and persistence (drag → Free, Free → docked display rule, field preservation) | T01, T02 | T05, T07 |
| T04 | Display catalog and window move interop: connected displays with stable identity and work areas in pixels, logged at startup; non-activating pixel move | T02 | T05, T07 |
| T05 | Docking engine: the HUD docks at every position on the chosen display, re-anchors on display events, drag → Free, context-menu "Position" submenu | T03, T04 | T06 |
| T06 | Edge chrome: vertical capsule on side edges, outline per edge, tooltips and popup facing inward | T05 | T07 |
| T07 | Settings "HUD placement" section (mode + display, live apply, disconnected preference, Free hint) wired through `App` | T03, T04, T06 | — |

```text
T01 → T02 → T03 ─┐
        └→ T04 ──┴→ T05 → T06 → T07
```

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Six modes, applied immediately | T01, T05, T07 | TC-01; MA-2, MA-5 |
| FR-02 | `prd.md#functional-requirements` | Top docks flush and aligned | T02, T05 | TC-03; MA-2 |
| FR-03 | `prd.md#functional-requirements` | Side docks vertical, centered, same order | T02, T05, T06 | TC-03, TC-05; MA-2 |
| FR-04 | `prd.md#functional-requirements` | Outline follows the docked edge | T02, T06 | TC-05; MA-2 |
| FR-05 | `prd.md#functional-requirements` | Tooltip and popup face inward, not clipped | T06 | MA-3 |
| FR-06 | `prd.md#functional-requirements` | Primary or specific display, labeled list | T02, T04, T07 | TC-04, TC-07; MA-5 |
| FR-07 | `prd.md#functional-requirements` | Primary follows the Windows primary | T02, T05 | TC-04; MA-6 (primary change) |
| FR-08 | `prd.md#functional-requirements` | Disconnected preference falls back and returns | T02, T05, T07 | TC-04, TC-07; MA-6 |
| FR-09 | `prd.md#functional-requirements` | Same physical monitor recognized again | T02, T04 | TC-04; MA-6 |
| FR-10 | `prd.md#functional-requirements` | Drag switches to Free and stores coordinates | T03, T05 | TC-06; MA-4 |
| FR-11 | `prd.md#functional-requirements` | Free keeps the `c4b55b9` clamp | T05 | existing `NotchPlacementTests` |
| FR-12 | `prd.md#functional-requirements` | Display selector disabled in Free, with hint | T07 | TC-07; MA-8 |
| FR-13 | `prd.md#functional-requirements` | Re-anchor on work area, resolution, DPI, size, provider count | T05, T06 | TC-11; MA-7 |
| FR-14 | `prd.md#functional-requirements` | Fresh install → Top Center on primary | T01, T05 | TC-01; MA-1 |
| FR-15 | `prd.md#functional-requirements` | `Left`/`Top`-only file loads as Free | T01, T07 | TC-01; MA-8 |
| FR-16 | `prd.md#functional-requirements` | Unknown value → Top Center + warning | T01, T05 | TC-01, TC-02 |
| FR-17 | `prd.md#functional-requirements` | Context-menu "Position" submenu | T05 | MA-2 |
| FR-18 | `prd.md#functional-requirements` | Free → docked keeps the hosting display | T03, T05 | TC-06 |
| NFR-01 | `prd.md#non-functional-requirements` | No activation, invariants kept | T04, T05 | QA-04; TC-10 |
| NFR-02 | `prd.md#non-functional-requirements` | Mixed DPI flush, no oscillation | T04, T05, T06 | MA-7 |
| NFR-03 | `prd.md#non-functional-requirements` | At most one write per display change | T03, T05 | TC-11 |
| NFR-04 | `prd.md#non-functional-requirements` | Forward and backward settings compatibility | T01 | TC-02 |
| NFR-05 | `prd.md#non-functional-requirements` | Placement rules unit-tested without a display | T01, T02, T03, T07 | TC-01..TC-07 |
| NFR-06 | `prd.md#non-functional-requirements` | Windows 11, existing app and targets | T04, T05, T06 | App build 0 warnings |
| DEC-01..DEC-14 | `techspec.md#technical-decisions` | Technical decisions | per task | see each task |
| TC-01..TC-11 | `techspec.md#test-approach` | Test scenarios | per task | see each task |

## Tasks

- [T01 — Persisted placement mode and display preference](done/task_01.md): the `Hud` settings section carries `Mode` and `Display` and resolves to a placement mode with migration and fallback.
- [T02 — Pure docking, display resolution, and edge layout rules](done/task_02.md): docked rectangles, display matching, and edge layout are computed by tested pure functions.
- [T03 — Placement service](done/task_03.md): one service owns placement state and persistence for drag, Settings, and the context menu.
- [T04 — Display catalog and pixel window move](done/task_04.md): the app lists connected displays with stable identity and moves the HUD in pixels without activating it.
- [T05 — HUD docking engine and Position submenu](done/task_05.md): the HUD docks at every position on the chosen display, re-anchors on display events, switches to Free on drag, and offers the Position submenu.
- [T06 — Edge chrome](done/task_06.md): side docks show a vertical capsule with the matching outline, and tooltips and popup open inward.
- [T07 — Settings HUD placement section](done/task_07.md): Settings chooses mode and display live, shows disconnected preferences, and explains Free mode.
- [T11 — Startup card first in Settings General tab](done/task_11.md): HIL 3 adjustment (DEC-07); the Startup card precedes HUD size and HUD placement.

## Coverage gate

- Coverage: pass. FR-01..FR-18 and NFR-01..NFR-06 map to tasks; FR-11 is preserved behavior covered by existing tests.
- Traceability: pass. Every task cites PRD and TechSpec IDs.
- Dependencies: pass. Acyclic; T04 depends only on T02 (`DisplayInfo`), so it could run before T03, but tasks run one at a time in ID order.
- Atomicity: pass. T01–T04 and T07 are one production concern plus one or two test classes each; window work is split into the docking engine (T05) and the edge chrome (T06), each with its own manual evidence.
- Executability: pass. Commands from `AGENTS.md`; pure files are linked into `TokenHound.Infrastructure.Tests` like the existing App links.
- Validation profile: pass. E2E omitted by .NET desktop policy; T04 is proven by the startup display log; MA-1, MA-2, MA-4 at T05 and MA-2 side edges, MA-3 at T06 run by the agent on the primary display; MA-5..MA-8 at T07 and the human visual check (second display).
- Idempotency: pass. No migration write on load; every task can be rerun on its own diff.

## Assumptions and open items

- Assumption: Win32 display configuration APIs return a device path for every active target on the user's machines; DEC-06 covers the failure path.
- Open item: none blocking.
- Required environment: MA-5..MA-7 (FR-06..FR-09, FR-13, NFR-02) need a second physical or virtual display, ideally at a different scale; owner: the human at the visual check. Without it, those checks stay pending at HIL 3.

## State

- [x] T01 — done
- [x] T02 — done
- [x] T03 — done
- [x] T04 — done
- [x] T05 — done
- [x] T06 — done
- [x] T07 — done
- [x] T11 — done (HIL 3 adjustment, DEC-07)

## Problems and solutions

- T04: the process runs SystemAware (no DPI manifest), so monitor rectangles of non-150% displays are virtualized by the system scale. Docking stays correct because window and monitor rectangles share that space (DEC-03); labels read the real resolution from `EnumDisplaySettingsW`.
- T05: `GetWindowRect` still returns the previous size during `SizeChanged`, which centered the capsule ~32 px off. Solution: dock with `ActualWidth`/`ActualHeight` × the window DPI scale.
- T05: `HudMenuItemStyle` had no submenu popup or check mark. Solution: `HudSubmenuItemStyle` and `HudCheckMenuItemStyle` in `DialogResources.xaml`.
- T06: WPF `Thickness` has no two-value constructor; four-value constructors trip QA-05. Solution: uniform value plus zeroed sides in an object initializer.
- T07: with a custom `ComboBox` template, `DisplayMemberPath` does not reach the selection box (it showed the record `ToString()`). Solution: `LabelItemTemplate` as `ItemTemplate`; also added `DialogComboBoxStyle`, since no dialog combo box style existed.
