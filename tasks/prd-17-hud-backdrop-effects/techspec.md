# TechSpec — HUD Backdrop Effects

Process level: `sdd-lean` (workflow DEC-01). This TechSpec and the plan are presented with the PRD at the merged HIL 1+2.

## Sources and traceability

- PRD: `tasks/prd-17-hud-backdrop-effects/prd.md`
- Instructions: `AGENTS.md`/`CLAUDE.md` HUD invariants (MA_NOACTIVATE, `SWP_NOZORDER` without `SWP_SHOWWINDOW`), `dotnet-efficient-validation`.
- Predecessor contracts: PRD 16 TechSpec DEC-02 (per-pixel transparency routes outside clicks), DEC-03 (owned `WS_EX_TRANSPARENT` shadow companion), DEC-04 (event-driven contour frame sync).
- Code evidence: `NotchWindow.xaml:11-14` (layered window), `NotchWindow.xaml:29` (capsule fill), `HudContourController.cs:14-207` (contour/companion sync), `HudShadowWindow.xaml.cs:10-72` (passive companion), `WindowPlacement.cs:181-244` (`DwmSetWindowAttribute` wrapper), `HudSizeSettings.cs`/`HudSizeStore.cs` (settings section pattern), `UserSettingsFile.cs:182` (manual section merge), `HudSizeSettingsCard.xaml` (General tab card).

## Solution summary

The HUD stays a layered window so PRD 16's per-pixel click-through is untouched. The Acrylic material is drawn by a new owned companion window that sits directly under the capsule, in the same way the shadow companion does: it never receives input (`WS_EX_TRANSPARENT | WS_EX_NOACTIVATE`), and the existing contour controller keeps its bounds and clip in sync. When the material is active, the capsule fill switches from opaque `#18181B` to a translucent tint, so the blur shows through while every contour pixel keeps non-zero alpha and still catches clicks.

The mechanism that paints the material is not yet proven. Microsoft documents that background Acrylic turns solid when its window is inactive, and the HUD is never activated. T01 is therefore a desktop spike with an explicit go/no-go across three ranked candidates. It ends at an exception HIL when the only viable candidate costs anti-aliased edges or relies on an undocumented API (PRD PD-04), or when none works. T02 implements the chosen candidate, the setting, and the fallback.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-02, FR-03, NFR-01 | Keep the main HUD window layered and unchanged in its input path; draw the material in a separate owned, input-transparent companion under it. | Per-pixel hit testing is documented only for layered windows ([Window Features](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#layered-windows)). WPF refuses system backdrops when `AllowsTransparency` is true (`WindowBackdropManager.SetBackdrop`, dotnet/wpf). | Backdrop on the main window would require a non-layered window, which hit-tests by rectangle or region and breaks PRD 16 FR-04. |
| DEC-02 | FR-01, FR-02 | Candidate order for T01: (A) Windows.UI.Composition host backdrop brush in a `WS_EX_NOREDIRECTIONBITMAP` companion, clipped by a path geometry; (B) undocumented `SetWindowCompositionAttribute` Acrylic accent on a region-clipped companion; (C) `DWMSBT_TRANSIENTWINDOW` on a non-layered companion clipped by `SetWindowRgn`. | (A) uses documented APIs ([DWMWA_USE_HOSTBACKDROPBRUSH](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute), [CreateHostBackdropBrush](https://learn.microsoft.com/en-us/uwp/api/windows.ui.composition.compositor.createhostbackdropbrush)) and allows an anti-aliased clip. (B) is undocumented; FluentWPF disabled it on Windows 11 and reports drag lag. (C) is official but region clipping is 1-bit (aliased). T01 result (`validation.md#t01-spike-2026-10-09`): A passes; B and C not evaluated. Workflow DEC-05 selects A clipped by a window region built from the flattened contour (spike variant AR: non-layered companion, the documented host-backdrop case). The 1-bit blur boundary sits under the capsule's anti-aliased tint and was visually equivalent to an AA composition clip at 8× zoom. | A self-drawn blur from screen capture is rejected: cost, flicker, and capture side effects. An AA `CompositionPathGeometry` clip (D2D interop or Win2D) was declined at DEC-05. |
| DEC-03 | FR-01 | Go/no-go per candidate: translucent blur visible while the HUD and companion are never activated, with another app in the foreground. | Background Acrylic "turns solid when an app window on desktop deactivates" ([Acrylic](https://learn.microsoft.com/en-us/windows/apps/design/style/acrylic)); the input-active override exists only in the Windows App SDK. | If no candidate passes, the feature returns to an exception HIL with a recommendation to drop it. |
| DEC-04 | FR-03, NFR-01 | When the material is active, the capsule fill uses a translucent tint (alpha ≥ 1/255 everywhere inside the contour, final value from NFR-02 measurement). The contour decorator, padding, menus, and popups are unchanged. | Layered hit testing passes only zero-alpha pixels, so any non-zero alpha keeps inside clicks. | A fully transparent fill would turn the capsule interior into click-through. |
| DEC-05 | FR-02, FR-07, NFR-03 | Extend `HudContourController` to drive the backdrop companion from the same frame (contour, matrix, bounds, DPI) and the same unchanged-frame suppression it uses for the shadow. Z-order: shadow, then backdrop, then HUD. The backdrop companion is not owned by the HUD (owned windows always sit above their owner); it is topmost and placed with `SetWindowPos(companion, hud, …, SWP_NOACTIVATE)` on every frame sync so it stays directly below the HUD. | Reuses PRD 16 DEC-04 synchronization with no new timer. Keeps `SWP_NOZORDER` placement of the main window; only the companion moves in z-order (T01 spike). | A second controller would duplicate frame conversion. If the controller would exceed 300 lines, extract the companion sync into a sibling class within T02. |
| DEC-06 | FR-05, FR-06, NFR-03 | Decide material versus fallback in a pure policy from inputs: setting enabled, OS build, transparency effects, energy saver, high contrast. Refresh inputs on `WM_SETTINGCHANGE` (`ImmersiveColorSet`), the energy saver power-setting notification, and `SystemParameters` high-contrast changes, all already on the HUD window procedure. No polling. | Acrylic is documented to go solid with transparency off, energy saver, and high contrast; the app must gate candidate (B) itself because the accent path may ignore these. | `UISettings.AdvancedEffectsEnabledChanged` is documented but needs WinRT; usable if T01 already adopts the projection for candidate A. |
| DEC-07 | FR-04, NFR-05 | Persist `HudBackdrop: { "Enabled": bool? }` as its own settings section with a store following `HudSizeStore`; `null` or a missing section resolves to enabled. Carry it through `UserSettingsFile.MergeWithDefaults`. | Matches the existing section pattern and keeps HUD position and size writes independent. | A field inside `HudSize` would couple unrelated preferences. |
| DEC-08 | NFR-05 | The App keeps `net10.0-windows`. Candidate A's composition calls go through hand-written WinRT ABI interop in CMP-02: `RoActivateInstance("Windows.UI.Composition.Compositor")`, `CreateDispatcherQueueController`, `ICompositorDesktopInterop::CreateDesktopWindowTarget`, and the few `ICompositor`/`IVisual`/`ISpriteVisual`/`ICompositionTarget`/`IVisualCollection` vtable calls needed for one host-backdrop sprite. T02 proves the calls in the scratch spike before production code; if the proof fails, T02 stops at an exception HIL with evidence (workflow DEC-05). | T01 measured the projection route: ≈ +25.4 MB exe (≈ +6.7 MB zipped) over a 7.1 MB app, since WPF cannot be trimmed. Windows.UI.Composition ships with the OS, so no runtime is redistributed (ARCHITECTURE.md packaging row). | Versioned TFM (`net10.0-windows10.0.22621.0`): proven and simpler, declined at DEC-05 for size. |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.App/UI/Placement/HudBackdropPolicy.cs` | New (pure) | Map availability inputs to `Material` or `Solid` | DEC-06 |
| CMP-02 | `src/TokenHound.App/Interop/HudBackdropInterop.cs` (split into sibling interop files if it would pass 300 lines) | New | Hand-written WinRT ABI and Win32 calls for candidate A, contour polygon region (`CreatePolygonRgn` + `SetWindowRgn`); fails closed to `Solid` on any error | DEC-02, DEC-08 |
| CMP-03 | `src/TokenHound.App/UI/Windows/HudBackdropWindow.cs` | New | Raw `CreateWindowEx` HWND (not a WPF `Window`: `WS_EX_NOREDIRECTIONBITMAP` content comes only from composition) with `WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`, `WM_MOUSEACTIVATE` → `MA_NOACTIVATE`, `DWMWA_USE_HOSTBACKDROPBRUSH`; unowned, kept below the HUD; paints the material clipped by the contour region | DEC-01, DEC-02, DEC-05 |
| CMP-04 | `src/TokenHound.App/UI/Windows/HudContourController.cs` | Modified | Create, synchronize, show/hide, and dispose the backdrop companion with the shadow | DEC-05 |
| CMP-05 | `NotchWindow.xaml` / `NotchWindow.xaml.cs` | Modified | Switch the capsule fill between opaque and tint; forward availability messages | DEC-04, DEC-06 |
| CMP-06 | `HudBackdropSettings`, `HudBackdropStore`, `UserSettings`, `UserSettingsFile` (Infrastructure) | New/modified | Persist the toggle | DEC-07 |
| CMP-07 | `HudBackdropSettingsViewModel`, `HudBackdropSettingsCard.xaml`, Settings General tab | New/modified | Toggle UI applied immediately | DEC-07 |
| CMP-08 | Tests in `tests/TokenHound.Infrastructure.Tests` | New | Policy, settings store, and companion style tests | TC-01..03 |

Flow: settings or Windows availability change → policy (CMP-01) → controller (CMP-04) shows or hides the backdrop companion and NotchWindow (CMP-05) swaps the fill in the same dispatcher pass, so no frame shows tint without blur or blur under an opaque fill. Geometry changes reuse the existing contour frame path.

## Contracts and data

- New settings section `HudBackdrop` in the user settings JSON: `Enabled` (`bool?`, optional). `null` or a missing section means enabled. No schema version change; older builds ignore the section through `ExtensionData`.
- Example: `"HudBackdrop": { "Enabled": false }`.

## Errors, security, and recovery

- Any interop failure, unsupported build, or missing composition support resolves to `Solid` and logs once with structured fields (`Candidate`, `HResult`). The HUD never shows a black or empty capsule.
- Disposal: the backdrop companion and composition objects are released with the controller on HUD close; T02 verifies graceful exit.
- Rollback: turning the setting off restores today's rendering; reverting T02 removes the section, which older builds ignore.
- No credentials or provider data involved.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| T01 spike | PRD 16 acceptance, desktop availability, merged HIL 1+2 | Candidate verdicts with screenshots; exception HIL when PD-04 applies |
| T02 implementation | T01 verdict (and exception HIL decision if raised) | Material, setting, fallback, sync; tests and manual script pass |

## Test approach

- Profile: App `net10.0-windows` WPF (`UseWPF`), tests in `tests/TokenHound.Infrastructure.Tests` (MTP executable, `global.json` runner Microsoft.Testing.Platform, links pure App files as PRD 16 does).
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release`, `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release`, then `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1`.
- E2E: omitted by .NET desktop policy.
- Desktop prerequisites: the user's desktop released for automation (Windows MCP), three mixed-DPI monitors, a disposable click target as in PRD 16.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-05, FR-06, DEC-06 | Unit | Policy over every input combination | `Solid` whenever any input disables the material | Infrastructure tests |
| TC-02 | FR-04, DEC-07 | Unit | Store read/write, missing section, `null`, merge with defaults | Missing or `null` reads as enabled; writes do not touch other sections | Infrastructure tests |
| TC-03 | FR-03, NFR-01 | Unit | Companion extended styles | `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`, tool window; never activates | Infrastructure tests (style policy, like `HudContourStyleTests`) |
| TC-04 | FR-01, FR-02, DEC-03 | Manual (spike, T01) | Each candidate with another app focused, over white, black, and busy content | Verdict per candidate with screenshots | Windows MCP |
| TC-05 | FR-01..03, FR-07, NFR-01 | Manual (T02) | Six modes × sizes 50/100/150 % × three DPIs; drag to Free; outside left/right/double clicks; inside menu | Material inside contour only, PRD 16 input checks unchanged, focus retained | Windows MCP |
| TC-06 | FR-04..06 | Manual (T02) | Toggle setting; turn Transparency effects and energy saver on/off live; restart | Switch within 2 s, no artifacts, setting persists | Windows MCP |
| TC-07 | NFR-02 | Manual (T02) | Contrast of icons and ring arcs over the HUD with white and black behind | Icons ≥ 4.5:1, ring arcs ≥ 3:1 in every state, measured with the contrast checker | Screenshot + `check_contrast` |
| TC-08 | NFR-03 | Manual (T02) | Idle 5 minutes with effect on versus PRD 16 baseline | CPU/memory within 10 %; no recurring log output | Process inspection |

## Quality profile

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | No `async void` / `.Result` / `.Wait()` | blocking | `rtk rg -n --type cs 'async void|\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)' $files` | — |
| QA-02 | No empty `catch` (interop must log and fall back) | blocking | `rtk rg -n --type cs 'catch\s*\{\s*\}|catch \(Exception\w*\)\s*\{\s*\}' $files` | — |
| QA-03 | No `#pragma warning disable` / `#nullable disable` | blocking | `rtk rg -n --type cs '#nullable disable|#pragma warning disable' $files` | — |
| QA-04 | Files ≤ 300 lines, methods ≤ 30 lines (AGENTS.md) | reservation | `rtk rg -c '^' --type cs $files` | — |
| QA-05 | 4+ argument calls split across lines (AGENTS.md) | reservation | `rtk rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | — |

- Verification scope: files in each task diff.
- Escalation trigger: 8+ reservations, a touched file above 500 lines, or duplication in 3+ places.

### Terrain baseline

Measured 2026-10-09 at `2bc32ef`.

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` | 248 | 2 | 0 | 0 | none | recorded |
| `src/TokenHound.App/UI/Windows/HudContourController.cs` | 207 | 1 | 2 | 0 | none | recorded; DEC-05 extraction if it would pass 300 |
| `src/TokenHound.App/UI/Windows/HudShadowWindow.xaml.cs` | 72 | 0 | 0 | 0 | none | recorded |
| `src/TokenHound.App/Interop/WindowPlacement.cs` | 268 | 5 | 0 | 0 | none | recorded; new interop goes to CMP-02, not here |
| `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs` | 184 | 0 | 0 | 0 | none | recorded |
| `src/TokenHound.App/App.Hud.cs` | 44 | 0 | 0 | 0 | none | recorded |
| `src/TokenHound.Infrastructure/Configuration/UserSettings.cs` | 118 | 1 | 0 | 0 | none | recorded |
| `src/TokenHound.Infrastructure/Configuration/UserSettingsFile.cs` | 190 | 6 | 0 | 0 | none | recorded |

- Preparatory refactoring: not recommended. No target crosses a structural threshold.

## Observability and rollout

- Signals: one structured log on each material/solid transition with the deciding input; one warning on interop failure. A transition logs at Debug instead of Information only when its own reason is an arrange-invalid frame or it comes on the call right after one; a same-mode call with another reason clears that state (DEC-09, narrowed by DEC-10).
- Migration: none; new optional section.
- Rollout: default on (PD-02). Rollback by toggle or revert.

## Risks and open items

- Risk (high probability, blocks the feature): Acrylic renders solid on a never-activated window (DEC-03). Mitigation: T01 tests it first for each candidate; if all fail, exception HIL recommends dropping the feature.
- Risk (medium): candidate B is undocumented and may break on a future Windows build. Mitigation: fail closed (NFR-04), and present it only through the PD-04 exception HIL.
- Risk (medium): drag lag of the material during Free drag. Mitigation: T01 measures it; acceptable fallback is to hide the material during `WM_ENTERSIZEMOVE`/`WM_EXITSIZEMOVE`.
- Risk (medium): single-file size growth from the WinRT projection (DEC-08). T01 measures it and reports the delta at its verdict.
- Resolved (workflow DEC-07): tint alpha 0xED (93%), the lowest opacity keeping every ring state at least 3:1 over pure white; icons stay above 4.5:1.
- Risk (medium, from T01): the hand-written WinRT ABI interop (DEC-08) is unproven; a wrong vtable slot crashes or fails. Mitigation: prove it in the scratch spike first, wrap every call to fail closed, and stop at an exception HIL if the proof fails.
- Risk (medium, from T01): the region polygon from the flattened PRD 16 contour (inverse joins) is untested. A region pixel outside what the capsule paints with non-zero alpha blocks outside clicks; build the region from the same contour and verify outside clicks in TC-05.
- Risk (low, from T01): drag lag measured only as "no misalignment in captured frames"; the hide-during-move fallback stays available.

## Relevant files

- Create: `src/TokenHound.App/UI/Placement/HudBackdropPolicy.cs`, `src/TokenHound.App/Interop/HudBackdropInterop.cs`, `src/TokenHound.App/UI/Windows/HudBackdropWindow.cs`, `src/TokenHound.Infrastructure/Configuration/HudBackdropSettings.cs`, `HudBackdropStore.cs`, `src/TokenHound.App/ViewModels/HudBackdropSettingsViewModel.cs`, `src/TokenHound.App/UI/Controls/Settings/HudBackdropSettingsCard.xaml(.cs)`, tests.
- Modify: `NotchWindow.xaml`, `NotchWindow.xaml.cs`, `HudContourController.cs`, `UserSettings.cs`, `UserSettingsFile.cs`, `SettingsWindow.xaml(.cs)`, `App.Hud.cs`, test `.csproj` links. `TokenHound.App.csproj` keeps `net10.0-windows` (DEC-08).
- Spike scratch: T01 prototypes live in a throwaway branch or scratch project and are not merged.
