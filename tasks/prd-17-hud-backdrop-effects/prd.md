# PRD — HUD Backdrop Effects

Process level: `sdd-lean` (workflow DEC-01). HIL 1 and HIL 2 are merged into one decision.

## Problem and context

The HUD capsule is painted with a flat `#18181B` fill (`src/TokenHound.App/UI/Windows/NotchWindow.xaml:29`). On Windows 11 it looks foreign next to system surfaces such as context menus and flyouts, which use the Acrylic material. The ROADMAP lists Mica/Acrylic as a deferred follow-up whose compatibility with the transparent, non-activating WPF window still needs validation (`docs/ROADMAP.md`, Separate Follow-up Candidates).

The constraint that shapes this feature: the HUD is a WPF layered window (`AllowsTransparency="True"`, `NotchWindow.xaml:12`), and PRD 16 routes clicks outside the visible contour to other applications through that per-pixel transparency (PRD 16 TechSpec DEC-02). The documented Windows 11 backdrop API targets non-layered windows. The material must therefore be added without giving up what PRD 16 delivers.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | The capsule shows Acrylic on supported Windows 11 systems | Desktop screenshots in every docking mode show the content behind the capsule blurred and tinted inside the contour only |
| OBJ-02 | PRD 16 behavior is preserved | The PRD 16 outside-click, focus, drag, and menu checks pass unchanged with the effect on |
| OBJ-03 | The user controls the effect, and unsupported systems degrade cleanly | The Settings toggle and every fallback condition produce the current solid fill with no artifacts |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer on Windows 11 | See the HUD as a native translucent surface | The HUD blends with the desktop | HUD docked at the top over an IDE or browser |
| US-02 | Developer who prefers the current look | Turn the effect off | Keeps the flat, high-contrast capsule | Settings > General toggle |
| US-03 | User with transparency effects off, energy saver on, or an unsupported build | Get a working HUD | No glitches or unreadable text | Windows setting changes while the HUD is running |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Paint the capsule with an Acrylic material when the effect is enabled and supported | In all six docking modes (Top left/center/right, Left, Right, Free), content behind the capsule is visibly blurred and dark-tinted |
| FR-02 | Limit the material to the PRD 16 contour | Nothing outside the contour shows blur or tint, including transparent corners, inverse joins, and the shadow area; the edge quality is decided from spike evidence (PD-04) |
| FR-03 | Keep cross-process click-through outside the contour | Left, right, and double clicks just outside the contour reach the application behind, as in PRD 16 FR-04 |
| FR-04 | Add a "Translucent background" toggle to Settings > General, on by default | Toggling applies immediately without restart and persists across launches; a settings file without the section reads as on |
| FR-05 | Fall back to the current solid fill when the effect is unavailable | With Windows "Transparency effects" off, energy saver active, Windows 10, or an unsupported Windows 11 build, the capsule shows `#18181B` exactly as today |
| FR-06 | Follow live changes of availability | Turning Windows "Transparency effects" or energy saver on or off while the HUD runs switches between material and fallback within 2 seconds, with no restart |
| FR-07 | Keep the material coherent with HUD changes | HUD size changes, docking mode changes, drag to Free, DPI changes, and moving between monitors keep the material aligned with the contour with no visible lag after the move settles |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Focus and composition | The HUD never takes focus (WM_MOUSEACTIVATE returns MA_NOACTIVATE) and `SWP_NOZORDER` placement without `SWP_SHOWWINDOW` still holds (AGENTS.md) |
| NFR-02 | Readability | Over both a pure white and a pure black background behind the HUD, provider icons and text keep a contrast ratio of at least 4.5:1 and ring arcs in every state keep at least 3:1 (WCAG 1.4.11 non-text). Amended at workflow DEC-07: the solid HUD already had grey and purple arcs below 4.5:1 |
| NFR-03 | Resources | No polling: availability changes arrive through Windows notifications. Idle CPU and memory stay within 10% of the PRD 16 baseline with the effect on |
| NFR-04 | Platform risk | If the chosen mechanism relies on an undocumented Windows API, it is isolated behind one interop boundary, fails closed to FR-05 on any error, and is recorded as a risk in the TechSpec |
| NFR-05 | Boundaries | `TokenHound.Core` stays free of UI and OS dependencies; the setting follows the existing JSON section pattern (`src/TokenHound.Infrastructure/Configuration/HudSizeStore.cs`) |

## User experience

- PD-01: the material is Acrylic: blur plus a dark tint over what is behind the HUD. Mica is out of scope.
- PD-02: the Settings toggle sits in the General tab next to the HUD size section and is on by default, so existing users see the effect after the update.
- PD-03: the fallback is today's solid fill. It is silent, with no notification or tray message.
- PD-04: if the only viable mechanism costs anti-aliased contour edges or depends on an undocumented API, the spike task (T01) stops at an exception HIL with screenshots and risks, and the human chooses there before any production change.
- The status popup and tooltip card keep their current solid fill.

## Constraints and dependencies

- Depends on PRD 16 (contour geometry, shadow companion, layered-window input path). Implementation starts only after PRD 16 is accepted (DEC-01).
- Windows 11 WPF on .NET 10; no new NuGet package unless the TechSpec justifies it.
- Desktop validation needs the user's screen and three mixed-DPI monitors, like PRD 16.

## Out of scope

- Mica, Mica Alt, or a material picker.
- Backdrops for the status popup, tooltip card, Settings window, or Provider Status window.
- New animations, including smooth ring interpolation.
- Light theme or accent-color tinting.

## Assumptions and sources

- Assumption: an Acrylic material that preserves per-pixel click-through is feasible on Windows 11. If the spike disproves it, PD-04 applies; if no option is acceptable, the feature returns to HIL with a recommendation to drop it.
- External source: [DWM_SYSTEMBACKDROP_TYPE](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type) defines the documented Windows 11 backdrop types; its applicability to layered windows is verified in the TechSpec.
- External source: [Layered windows](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#layered-windows) define the per-pixel hit testing PRD 16 relies on.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified project source.
- [x] Implementation details remain in the TechSpec.
