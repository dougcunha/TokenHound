# PRD — HUD size setting

## Problem and context

The HUD capsule (`NotchWindow`) is sized to its content (`SizeToContent="WidthAndHeight"`, `NotchWindow.xaml:15`), and its rings, margins, and fonts use fixed device-independent sizes. On some resolutions and display scales the capsule looks too large, and the user has no way to shrink it. This feature adds a Settings option that scales the HUD, so the user can fit it to their screen.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | The user can make the HUD smaller (or larger) from Settings, without editing files | Applying a new size changes the capsule's rendered size (unit tests on the scale value and MA-1) |
| OBJ-02 | The chosen size survives restarts and never leaves the HUD off-screen | Restarting keeps the size, and the HUD stays inside the screen at every allowed size (MA-2, MA-3) |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | User on a high-resolution or low-density display | Shrink the HUD | The HUD takes less of the screen | Settings → General → HUD size → pick a smaller size → Apply → the capsule shrinks in place |
| US-02 | User who wants a bigger HUD | Enlarge the HUD | Rings are easier to read | Same flow with a larger size |
| US-03 | Any user | Return to the default | Undo an experiment | Choose 100% → Apply → the HUD looks as it does today |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | The HUD size is a percentage of the current size, from 50% to 150% in steps of 5%, with a default of 100% | Unit tests: values outside 50–150 are clamped, a missing or invalid value reads as 100, and 100 renders exactly as before |
| FR-02 | Settings shows the size as a slider with a live percentage label, plus quick-pick presets "Small" (75%), "Default" (100%), and "Large" (125%) that set the slider | Unit tests on the ViewModel: each preset sets its value, and moving the slider updates the label |
| FR-03 | The size uses the Apply pattern of the other tabs: Apply is enabled only when the value differs from the persisted one, and Apply persists and takes effect immediately, with no restart | Unit tests: Apply disabled at load, enabled after a change; MA-1: the visible HUD resizes at Apply |
| FR-04 | The size scales the whole HUD capsule uniformly: rings, glyphs, spacing, border, and corner radius | MA-1: at 50% and 150% no element is clipped, distorted, or misaligned |
| FR-05 | The status popup and tooltip cards scale with the HUD | MA-1: hover a ring and open the status popup at 75% and 125%; both follow the HUD scale |
| FR-06 | After a size change the HUD keeps its anchor: the top-center position stays centered on the same point (or the dragged position keeps its top-left), and the HUD is clamped inside the virtual screen | Unit tests on the placement clamp for the new width; MA-2: change size on the default and on a dragged position |
| FR-07 | The persisted size is stored in the user settings JSON in its own section, written with the same store pattern as the other settings | Unit tests: round-trip through the store with a temporary file, absent section reads as default |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Architecture | `TokenHound.Core` keeps zero UI or OS dependencies. Persistence lives in `TokenHound.Infrastructure`, the scaling in `TokenHound.App` |
| NFR-02 | HUD invariants | Non-activating behavior is unchanged: the window still returns `MA_NOACTIVATE` on `WM_MOUSEACTIVATE` and keeps its layered-window setup. Scaling must not steal focus or break drag |
| NFR-03 | Rendering | Text and borders stay crisp at every step (no blurry bitmap scaling) |
| NFR-04 | Accessibility | The slider and presets have automation names and help text, like the existing tabs |

## User experience

Settings → General gets a "HUD size" section: a slider (50%–150%), a label showing the current percentage, and three preset buttons (Small, Default, Large). The existing Apply button commits it. No HUD or tray menu change.

## Constraints and dependencies

- Product decision proposed here: a percentage slider with three presets, which covers both options in the request ("2 pre-defined sizes or a percentage"). The alternative is presets only (smaller scope, no slider).
- Settings follows the `CadenceSettingsViewModel` / `RefreshSettingsStore` pattern, and the General tab introduced by PRD 12 (`SettingsWindow.xaml:255`).
- Read `docs/design/` before changing HUD geometry, animations, or tooltips (CLAUDE.md); the TechSpec step owns that.

## Out of scope

- Automatic sizing by resolution or monitor DPI, and per-monitor sizes.
- Resizing the Settings, Provider Status, About, and Update windows.
- Changing the ring layout, fonts, or colors beyond uniform scaling.
- A tray-menu or HUD context-menu shortcut for size.

## Assumptions and sources

- Assumption: uniform scaling is acceptable, so no element needs its own minimum size. If a text at 50% is unreadable, the lower bound rises to 60%. Source: request ("too large on some resolutions").
- Assumption: the HUD is not scaled by the existing OS DPI; this setting is on top of it. Source: `NotchWindow.xaml` has no scale transform today.
- Open product question for HIL: slider plus presets (proposed) versus presets only.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
