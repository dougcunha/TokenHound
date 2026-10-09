# Validation — HUD Backdrop Effects

## T01 spike (2026-10-09)

Scratch prototype `BackdropSpike` (outside the repository, in the session scratchpad; not committed). Each run starts a separate-process click-target panel (white, black, and busy columns that log mouse events) and one candidate capsule. The capsule is a layered WPF window (`AllowsTransparency`, `WS_EX_NOACTIVATE`, `WM_MOUSEACTIVATE` → `MA_NOACTIVATE`) with a `#80` `#18181B` tint. Its companion is a raw `CreateWindowEx` HWND (`WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST`) inserted directly below the capsule.

### Environment (T01.1)

| Item | Value |
| --- | --- |
| Windows build | 10.0.26200.9457 (Windows 11) |
| Transparency effects | `EnableTransparency = 0` at start; set to 1 for the spike and restored to 0 afterwards (twice, workflow DEC-04) |
| High contrast | off |
| Energy saver | not determined; power scheme "Full", battery 100% |
| Displays | DISPLAY1 1920×1080 @125% (index 0), DISPLAY2 1920×1080 @150% primary (index 1), DISPLAY3 3440×1440 @100% (index 2) |
| Test monitor | Primary (150%) |

### Candidate verdicts (DEC-03, TC-04)

| Candidate | Variant | Translucent blur while never activated | Outside clicks reach another process | Edge | Verdict |
| --- | --- | --- | --- | --- | --- |
| A: host backdrop brush | A: non-layered, geometric rounded clip, no region | Pass | **Fail**: full-rect non-layered companion catches corner clicks | AA | Rejected variant |
| A | AL: `WS_EX_LAYERED` (alpha 255) + `WS_EX_TRANSPARENT`, geometric clip | Pass | Pass: left, right, double | AA (composition clip + capsule tint) | Pass |
| A | AR: non-layered + `SetWindowRgn`, no geometric clip | Pass | Pass: left, right, double | 1-bit blur boundary under the capsule's AA tint; near-identical to AL at 8× | Pass |
| A | ALR: layered + transparent + `SetWindowRgn` | Pass | Pass: left, right, double | Same as AR | Pass |
| B: accent Acrylic | — | Not evaluated: T01.3 runs only if A fails | — | — | Not evaluated |
| C: `DWMSBT_TRANSIENTWINDOW` | — | Not evaluated: T01.4 runs only if A and B fail | — | — | Not evaluated |

The log shows no capsule activation before the drag in every run. The foreground stayed on the panel process during the screenshots and clicks. A secondary observation: WPF `DragMove` activated the spike capsule during the drag (`capsule AL ACTIVATED`), and the material stayed translucent after the foreground returned to the panel.

### Evidence

| File | Shows |
| --- | --- |
| `t01-A-panel-foreground.png` | A: blur over white, black, and busy content while the panel holds the foreground |
| `t01-A-edge.png` | A: left edge zoom |
| `t01-AL-panel-foreground-clicks.png` | AL: blur; panel status line records the pass-through clicks |
| `t01-AR-ALR-panel-foreground-clicks.png` | AR (top) and ALR (bottom): blur; panel status records the last pass-through double click |
| `t01-AL-edge-zoom8x.png`, `t01-AR-edge-zoom8x.png`, `t01-edge-compare-AL-left-AR-right.png` | 8× nearest-neighbour edge over busy content: geometric clip (left) versus window region (right) |
| `t01-AL-drag-settled.png` | AL after a ~400 px drag: material aligned with the capsule |
| `t01-spike-log-20261009.txt` | Companion HRESULTs (all `S_OK`), panel click log, the single DragMove activation |

Edge sample over white, row through the left curve (both AL and AR): `255 … 255 249 208 167 139 …`, a three-pixel anti-aliased ramp from the capsule tint.

### Costs of candidate A

| Cost | Measurement or finding |
| --- | --- |
| Publish size (DEC-08) | `release.yml` command (framework-dependent single file, win-x64): App today 7.1 MB (3.0 MB zipped). The WinRT projection (`Microsoft.Windows.SDK.NET.dll` 24.9 MB + `WinRT.Runtime.dll` 0.5 MB) adds ≈ 25.4 MB to the exe (≈ 6.7 MB zipped). WPF cannot be trimmed, so the full projection ships. |
| Dispatcher queue | A `Compositor` on the WPF thread needs `CreateDispatcherQueueController` (CoreMessaging) first. |
| Desktop interop | `ICompositorDesktopInterop::CreateDesktopWindowTarget` via a manual vtable call; no projected API. |
| Companion type | Must be a raw HWND (`WS_EX_NOREDIRECTIONBITMAP`), not a WPF `Window`; TechSpec CMP-03 (`HudBackdropWindow.xaml`) needs amending. |
| Z-order | Owned windows always sit above their owner, so the companion cannot be owned by the HUD like the shadow. It is topmost and kept directly below the HUD with `SetWindowPos(companion, hud, …)` on every frame sync (DEC-05). |
| Click-through | A non-layered full-rect companion blocks clicks; a window region (AR/ALR) or a layered companion (AL/ALR) is required. AR's region must not extend beyond pixels the capsule paints, or those pixels block clicks. |
| Clip shape | Proven only with `CreateRoundRectRgn` / `CompositionRoundedRectangleGeometry`. The PRD 16 docked contour has inverse joins: a region needs a polygon from the flattened contour (`CreatePolygonRgn`), untested; an AA composition path needs `IGeometrySource2D` (D2D interop or Win2D), untested. |
| Documentation | `DWMWA_USE_HOSTBACKDROPBRUSH` is documented for non-layered windows; AL/ALR work empirically on build 26200 only. |
| Drag | Companion followed via `LocationChanged`; no misalignment in the captured frames; per-frame lag not measured (DEC-05 hide-during-move fallback remains available). |
| Activation | Material survives a never-activated capsule; the Microsoft "solid when inactive" note did not apply to the host backdrop brush in a desktop window target. |

### Recommendation

Candidate A passes DEC-03, so B and C are unnecessary (no undocumented API). Recommended T02 shape: A with the AR or ALR variant (window region from the flattened contour). Two choices remain for the human because they trade cost against risk within TechSpec alternatives: the projection route (DEC-08) and the clip route (DEC-02/PD-04).

## T02

### T02.3 interop and region proof (2026-10-09, scratch `InteropProof`, `net10.0-windows`, no projection)

- Hand-written WinRT ABI: `CreateDispatcherQueueController`, `WindowsCreateString` + `RoActivateInstance("Windows.UI.Composition.Compositor")`, QI `ICompositor` → `CreateSpriteVisual` (slot 22), QI `ICompositor3` → `CreateHostBackdropBrush` (slot 6), QI `ICompositionBrush` → `ISpriteVisual.put_Brush` (slot 7), QI `IVisual` → `put_Size` (slot 36), `ICompositorDesktopInterop::CreateDesktopWindowTarget` (slot 3), QI `ICompositionTarget` → `put_Root` (slot 7). All 16 steps returned `S_OK`. Slots and IIDs read from `C:\Windows\System32\WinMetadata\Windows.UI.winmd`.
- Region: top-docked contour with inverse flares, flattened at 0.25 px tolerance into a 37-point `CreatePolygonRgn` (winding) on the non-layered companion.
- Results: blur visible inside the contour (`t02-interop-proof-docked-contour.png`). DPI-aware hit tests put the first capsule pixel at the contour edge (x = 581 at y = 340); outside points in the flare concavity, bottom corner, and right end hit the panel. Real clicks there were logged by the different-process panel: left at the concavity, right at the bottom corner, double at the right end. No capsule activation was logged.
- Verdict: DEC-08 hand interop and the DEC-02 region route are proven; T02 continues.

### T02 manual matrix (2026-10-09, dev build `src/TokenHound.App/bin/Release`)

| ID | Scenario | Result |
| --- | --- | --- |
| TC-04/FR-01 | TopCenter on DISPLAY1 (125 %), Left edge on DISPLAY1, Free on DISPLAY3 (100 %) after a cross-monitor drag | Material inside the contour in each; companion `TokenHoundHudBackdrop` always at the HUD rect with styles `0x082000A8` (`t02-hud-tint93-white-black-zoom4x.png`, `t02-hud-left-edge-display1-zoom4x.png`, `t02-hud-free-display3-100-zoom4x.png`) |
| TC-05/FR-03 | Real clicks just outside the TopCenter contour (left at the flare, right below the body, double at the right side) | All three logged by the different-process panel; foreground stayed on the panel; the HUD context menu still opens inside the contour |
| TC-05/NFR-01 | Cross-monitor drag of the Free HUD | HUD never became foreground; companion followed (same rect after the move) |
| TC-06/FR-04 | Settings toggle off/on | Solid within 17 ms of the click, `"HudBackdrop": { "Enabled": false }` persisted; back to material on re-enable |
| TC-06/FR-06 | Transparency effects 0/1 live (`WM_SETTINGCHANGE ImmersiveColorSet`) | Solid (`TransparencyEnabled`) and back to material, each < 1.5 s |
| TC-06/FR-06 | Energy saver "always on" on/off live (Windows Settings) | Starts solid with energy saver on (`EnergySaverActive`, from the registration notification); material ≈ 2 s after turning it off. Energy saver restored to its original "off" |
| TC-07/NFR-02 | Fill measured over pure white `#28282B` and pure black `#161619` | Icons 13.37:1; arcs over white: green 5.79, red 3.91, purple 3.47, grey 3.04 (all ≥ 3:1, DEC-07) |
| TC-08/NFR-03 | 60 s idle each, material vs solid | CPU 7.78 s vs 8.36 s per 60 s (1.08 % vs 1.16 % of the machine); working set 182.4 vs 182.5 MB; no recurring log output from the backdrop |

Not run by the coordinator (folded into the visual check script): Top left, Top right, Right edge, HUD sizes 50 % and 150 %, and the 150 % primary monitor (another automation session was using it).
