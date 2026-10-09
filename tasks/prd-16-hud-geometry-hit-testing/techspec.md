# TechSpec: HUD Edge Geometry and Contour Hit Testing

## Sources and traceability

- Product authority: [approved PRD](prd.md), workflow DEC-02; approved SHA-256 `218c9fc6e172bd8f6aced85ffd0f6f63e70f16627b4691f2ea92dc5279c346e8`.
- Rules: AGENTS.md, RTK.md, repository-cli-efficiency, dotnet-efficient-validation, no-workarounds, sdd-create-techspec, sdd-plan-tasks, and the SDD flow/checkpoint/snapshot protocols.
- Architecture: `ARCHITECTURE.md`, WPF/.NET 10 selection and Core/Infrastructure/App separation. Its proposed HTTRANSPARENT mechanism is superseded by DEC-02 below; its speculative performance figures are not acceptance evidence.
- Design: `docs/design/2026-08-28-usage-notch-design.md`, shape and interaction sections. Apply the approved Windows palette/content decisions, not the reference's macOS platform or black/no-shadow theme.
- Code: `NotchWindow.Dock.cs:20-72` (chrome), `NotchWindow.xaml.cs:90-150` (native hook/lifecycle/drag), `NotchWindow.Placement.cs:105-163` (DPI-aware placement), `NotchWindow.Scale.cs:29-39`, and `WindowStyles.cs:57-96`.
- Graft traces: ApplyChrome callers are OnLoaded and OnPlacementChanged. EnableNonActivating callers include OnSourceInitialized and its zero-handle regression test. Event-handler references require direct event-wiring inspection because the graph has no incoming edge for OnMouseLeftButtonDown.

## Solution summary

Render a shared, closed contour around the existing provider content. Docked modes add inverse quarter-round joins; Free uses a fully rounded capsule. Keep the main WPF layered window free of external shadow effects so pixels outside its painted fill/stroke have zero alpha. Windows then routes mouse input through that space. Preserve the existing mouse hook and normal WPF input inside the contour.

Move the existing HUD shadow to one owned, non-activating layered companion window with WS_EX_TRANSPARENT. It renders only the shadow outside the shared painted envelope and receives no input. A controller owns its lifecycle and synchronizes it on actual layout, location, DPI, and visibility changes. This is a technical proposal for HIL 2, not implemented or desktop-verified behavior.

## Technical decisions

| ID | PRD obligations | Decision | Evidence and trade-off |
| --- | --- | --- | --- |
| DEC-01 | FR-01..03, FR-11, PD-01..03 | Keep shape mathematics in App/UI/Placement without WPF/Win32 dependencies; adapt the result to frozen WPF geometries in App/UI/Controls. | Existing linked-source placement tests support this split. Core and provider models remain untouched. One canonical definition supplies rendering and shadow exclusion. |
| DEC-02 | FR-04..06, NFR-01..02 | Use main-window per-pixel transparency, not HTTRANSPARENT as the cross-process input mechanism. | [Layered-window documentation](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#layered-windows) specifies mouse pass-through for zero-alpha pixels. [WM_NCHITTEST](https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-nchittest) restricts HTTRANSPARENT forwarding to the same thread. Documented semantics support this design; actual WPF desktop behavior remains TC-04 acceptance. |
| DEC-03 | FR-04..05, PD-01, PD-04 | Render the existing black shadow in an owned WS_EX_TRANSPARENT companion, with an exclusion mask over the capsule. | Layered WS_EX_TRANSPARENT passes input through the whole companion. A single shadowed HWND would paint nonzero alpha outside the contour. [SetWindowRgn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowrgn) clips drawing too, so trimming the main window region would remove that shadow. The companion costs one HWND and requires explicit lifecycle/DPI acceptance. |
| DEC-04 | FR-06..09, NFR-02 | Update an immutable contour frame on Arrange/mode/size changes; synchronize the companion through UI-thread events with reentrancy protection and unchanged-frame suppression. | Reuse the existing placement/display coalescing path. No timer, global hook, pointer polling, or permanent LayoutUpdated/CompositionTarget.Rendering subscription is added. |
| DEC-05 | FR-05, FR-07, FR-10, NFR-01 | Retain CapsuleBorder's name, ContextMenu, padding, content, and popup anchor while replacing its painted Border with a contour decorator. Retain DragMove and RecordDrag semantics. | Existing context-menu opening and popup placement keep their control references. A contour-aware WPF hit-test override includes painted chrome and excludes unpainted gaps; it complements native alpha routing rather than replacing it. |
| DEC-06 | FR-08..09, NFR-03 | Preserve existing window Left/Top coordinate semantics and placement service; apply HUD scale and monitor DPI once at their respective boundaries. | Free remains persisted in the existing WPF coordinates; docking still uses work-area physical pixels. No settings migration or new preference. |
| DEC-07 | NFR-03, NFR-05 | Reuse the net10.0 Infrastructure test executable for pure contour/layout and style-policy tests. Link new pure App files as the project already does. | No new package, test runner, test project, or WPF dependency in Core/Infrastructure. Actual WPF alpha, focus, and cross-process behavior require manual evidence. |
| DEC-08 | NFR-01..02 | Absorb contour/shadow ownership into a new controller, not another large NotchWindow partial. | A local responsibility boundary fits the feature. Existing partial-window debt is recorded below; no unrelated preparatory refactor is proposed. |

TechSpec DEC IDs are local to this document; workflow DEC IDs record human approvals separately.

## Components and flow

| ID | Component | State | Responsibility and dependencies |
| --- | --- | --- | --- |
| CMP-01 | `UI/Placement/HudContourLayout.cs`, `HudContourFrame.cs`, `HudContourSegment.cs`, `HudContourPoint.cs` | New | Finite dimensions, body rectangle, mode-specific joins, canonical path segments, and stroke-edge flags; existing HudDockMode/HudEdgeLayout. |
| CMP-02 | `UI/Controls/HudContourGeometry.cs`, `HudContourDecorator.cs` | New | Measure/arrange existing child without deformation; convert CMP-01 to frozen fill/stroke/envelope geometries; draw palette/stroke; expose frame changes and WPF hit testing. |
| CMP-03 | `UI/Windows/HudShadowWindow.xaml`, `.xaml.cs` | New | Render the existing shadow outside the painted envelope; transparent background, ShowActivated=false, no controls or input; independent from provider popups. |
| CMP-04 | `UI/Windows/HudContourController.cs` | New | Own CMP-03, subscribe/unsubscribe layout/location/DPI/visibility/closed events, synchronize geometry/transforms/native bounds, and dispose once; CMP-02 and CMP-05. |
| CMP-05 | `Interop/WindowStyles.cs`, `Interop/HudShadowInterop.cs` | Modified/new | Preserve existing non-activation styles and SWP flags; add a separate transparent-shadow style operation and checked native bounds update. Never set WS_EX_TRANSPARENT on the interactive HUD. |
| CMP-06 | `NotchWindow.xaml`, `.xaml.cs`, `.Dock.cs`, `.Placement.cs`, `.Scale.cs` | Modified | Integrate decorator/controller and keep orientation, drag, placement, menu, scale, and popup behavior. Change only necessary call sites. |
| CMP-07 | Infrastructure test project and contour/style tests | Modified/new | Pure geometry/layout policy and regression evidence; no application startup or desktop automation. |

The decorator measures the existing provider child plus its existing padding. Its body keeps that required content rectangle; decorative joins occupy reserved space outside it. Arrange creates one frame. The controller translates that frame from decorator-local unscaled DIPs through the actual ancestor transform into owner client DIPs, then uses the owner HWND's physical bounds/current DPI to synchronize the shadow. Main rendering and companion masking consume the same generation and transform. A mode switch updates orientation and contour before placement measures the final size.

### Geometry contract

- Internal immutable records use required/init members; one type per file. Frame inputs are mode, content-required width/height, available arranged bounds, and existing stroke/padding metrics, all in unscaled DIPs. Reject nonfinite/negative values; a zero-sized not-yet-arranged host publishes no drawable frame.
- Retain convex radius 24 DIPs, stroke 1.5 DIPs, and the existing horizontal/vertical padding. Clamp convex radius to half the body width/height. Use inverse radius 12 DIPs, bounded by available continuation space and body dimensions. These numbers derive from the existing 24-DIP corner and 12-DIP gutter; screenshots validate the proposed pairing.
- Use tangent-continuous quarter-circle cubic segments (control factor 0.5522847498307936), straight segments, and an explicit close operation. Build canonical top geometry; rotate/reflect it for side modes so the curve profile stays identical.
- TopLeft omits the unavailable left continuation; TopRight omits the unavailable right continuation. Side modes add continuations above/below the body. Free has no continuation and rounds all four corners.
- Reserve continuation space in the host without reducing the measured provider content. Keep zero outer gutter on a docked work-area edge, including both touched edges at top corners; retain the existing shadow gutter elsewhere. Free keeps its coordinate origin and existing outer margin semantics.
- Omit stroke on the attached work-area edge, as today; Free has a closed stroke. Compute the painted envelope from fill plus the actual widened stroke, including its half-width and transform. Mask the companion with the outer window rectangle minus that envelope. Never approximate its exclusion with a rectangle or a separate hand-written radius calculation.
- Keep child content inside the safe body rectangle. Clip only where necessary against the shared envelope; no clip is applied to separate tooltip/menu/popup HWNDs.
- WPF native alpha owns the final input boundary, including antialiasing pixels that actually paint fill/stroke. WPF HitTestCore uses the same fill/stroke geometries; no alpha cutoff expands or shrinks the product contour.

### Lifecycle and native interface

Create at most one companion per HUD. Set its owner to the main window, keeping it above its owner according to [owned-window semantics](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features#owned-windows). Clip the companion's rendered output after its shadow effect so its source fill cannot cover providers. Keep shadow color black, blur 20, depth 4, opacity 0.65, direction 270. The main decorator has no DropShadowEffect; the existing StatusPopup shadow remains unchanged.

Before showing the companion, ensure its native styles are WS_EX_LAYERED, WS_EX_TRANSPARENT, WS_EX_NOACTIVATE, WS_EX_TOOLWINDOW, and WS_EX_TOPMOST, preserving existing bits. Retain WM_MOUSEACTIVATE -> MA_NOACTIVATE on both HUD surfaces. A new checked helper applies bounds with SWP_NOACTIVATE | SWP_NOZORDER; it neither alters the existing EnableNonActivating flags nor uses SWP_SHOWWINDOW. WPF controls visibility with ShowActivated=false. Never use SetLayeredWindowAttributes on the WPF-owned layered surfaces.

Synchronize from actual arranged frame changes, owner LocationChanged/SizeChanged/DpiChanged, and visibility changes. If layout is unsettled, hide the companion until a valid current frame exists; publish a coherent final frame without an arbitrary delay. Preserve provider popups above the HUD surfaces without reordering them on geometry updates. Hide the companion with the owner; close and detach on true owner destruction, not intercepted Hide. Compare frame/bounds before native calls and guard native callbacks against recursive layout/placement. UI dispatcher confinement owns all WPF state; no asynchronous background renderer is introduced.

## Contracts and data

Only App-internal render/layout contracts change, as defined above. Persisted HudPlacementSettings, monitor identity, Free Left/Top, HUD size, provider snapshots, APIs, and credentials retain their current schema and meaning. No JSON field, migration, or configuration option is added.

## Errors, security, and recovery

- Native style/position calls must check return values and capture the last Win32 error immediately. Surface a specific failure through the existing app error/log boundary; hide the companion on synchronization failure. Do not silently claim shadow/input acceptance or repeatedly retry a deterministic error.
- Invalid numeric frames are rejected at their source; legitimate zero-sized pre-layout state is explicit. Main input is never made globally transparent as a fallback.
- Display loss and off-screen coordinates continue through DisplayResolver and NotchPlacement. Drag capture keeps its normal release lifecycle; after a real drag, switching to horizontal Free regenerates the frame at the recorded location.
- No credential/file/database/network access or new process is introduced by production geometry code.
- Reversal restores the prior XAML/Window integration and removes the companion/controller; existing settings remain readable. Keep unrelated worktree changes intact.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| Capture desktop baseline and implement pure contour definition | Approved HIL 2 | Baseline record plus deterministic geometry tests; no visible integration yet. |
| Integrate contour rendering and click-through shadow | Prior geometry | Existing provider HUD renders the new contour; cross-process input and focus smoke evidence. |
| Complete placement/scale/restart acceptance and document delivery | Integrated contour | Unit/regression results and full manual matrix; human visual gate before independent review. |

## Test approach

- App: net10.0-windows, UseWPF=true, WinExe. Infrastructure tests: net10.0 executable, xunit.v3.mtp-v2 4.0.0, UseMicrosoftTestingPlatformRunner=true. global.json selects SDK 10.0.400/latestFeature and Microsoft.Testing.Platform. No root Directory.Build.props/targets or Directory.Packages.props was present during planning.
- E2E: omitted by .NET desktop policy. Do not run a full-app automation suite or replace manual checks with simulated SendMessage/SendInput unit tests.
- Build route after implementation: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release` and `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release`; serialize shared outputs and reuse current evidence. Read dotnet-efficient-validation before running.
- Targeted MTP route: `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*HudContour*"`. Native filter follows `--`. Assert nonzero test count and preserve/check `$LASTEXITCODE`.
- Integrated regression route: the same executable command without the class filter. It covers existing placement, scale, style, tray, and provider presentation regressions. No Core production change is planned; rerun Core tests only if a new change actually affects it.

| ID | Obligations | Level | Scenario and expected result |
| --- | --- | --- | --- |
| TC-01 | FR-01..03, FR-11, PD-02..03 | Unit | Six modes, minimum/empty/populated content; closed finite non-self-crossing contours, expected tangents/mirrors, bounded/omitted corner wings, and content-safe body bounds. Assert known boundary samples and invariants, not a duplicate builder implementation. |
| TC-02 | FR-08..09, NFR-03 | Unit/regression | Scale and physical-pixel frame conversion apply each transform once; negative origins, fractional DPI, unchanged-frame suppression, dock anchoring, and existing Free configuration/monitor resolution remain valid. |
| TC-03 | NFR-01..02, FR-05..06 | Unit/regression | Shadow-only style composition preserves existing bits, adds transparent/non-activation flags only on the companion, and leaves existing main style/zero-handle behavior intact. Lifecycle calculations suppress duplicate work. |
| TC-04 | OBJ-01, FR-04..06, PD-04, NFR-01 | Manual | Different-process target receives left/right/double clicks outside fill/stroke, including actual visible shadow. Inside hover/menu/drag remains usable and keyboard focus stays with the previously active application. |
| TC-05 | OBJ-02..03, FR-01..03, FR-07..11, PD-01..03, NFR-04 | Manual | Screenshot and interaction matrix covers all modes/sizes, live switches, vertical-to-Free drag, restart, popups, provider states, DPI transitions, and display fallback with no content clipping or stray shadow HWND. |
| TC-06 | NFR-02, NFR-05 | Manual/inspection | Idle HUD introduces no periodic geometry redraw/input interception; hide/show/exit releases the companion and handlers. Separate automated, coordinator, and human evidence; essential missing cases remain pending. |

### Manual acceptance script

Owner: coordinator performs desktop checks and records evidence; the human supplies visual approval and physical display-disconnection checks when needed. Record executable/commit or diff fingerprint, monitor inventory, coordinates, screenshots, focused app, and results in `validation.md`. Baseline and final results are separate tables; neither is prefilled as passing.

1. Before changing production code, launch/inspect the accepted v0.1.13 HUD on the user desktop via Windows MCP App (`launch_executable`). Record existing contour/outside clicks, shadow clicks, focus, menus, and dock-to-Free drag against a different-process editor or other disposable target. Use no real user document as the click target.
2. After integration, use the same target. For each of the six modes, click inside fill and stroke and outside inverse joins, rounded corners, gutters, and visible shadow; repeat left/right/double clicks. Confirm the target's caret/menu/selection changes at outside points, and HUD actions occur only inside. Continue typing in the target after HUD hover/right-click/drag to check focus retention.
3. Screenshot every mode, including minimum/empty and populated HUD states. Check smooth joins, omitted corner continuation, fully rounded Free, palette/stroke/shadow, badges, busy pulse, and no overlaps. Open provider hover details, StatusPopup, and all existing context-menu actions; verify inward placement and dismissal.
4. Cycle all existing size choices and repeat representative inside/outside points for top/side/Free. Switch modes repeatedly, then drag from each side into Free; record/drop/restart and verify saved coordinates and horizontal orientation.
5. Move across the available 100%, 125%, and 150% displays, including negative coordinates and the monitor with a taskbar/work-area offset. Repeat point/focus/shape checks after DPI transitions and dock changes. Hide/show from the tray, then exit: there must be no remaining shadow surface. Idle inspection must show no recurring geometry updates.
6. (Waived for this delivery by workflow DEC-05, 2026-10-09: accepted residual risk.) Disconnect/reconnect the preferred display and verify existing primary fallback and return behavior. If physical disconnection is unavailable to the coordinator, obtain human evidence; a resolver unit test does not close this desktop case.
7. Present screenshots and pending items at the human visual gate. Only after that approval, delegate independent code review. Final delivery/HIL 3 requires essential evidence and no blocking review findings.

Read-only DisplayInventory on 2026-10-08 found: DISPLAY1, 1920x1080 at (-1920,-1440), 125%; primary DISPLAY2, 1920x1080 at (0,0), 150%; DISPLAY3, 3440x1440 at (0,-1440), 100%, work area ending at y=-48. Screenshot's display numbering differs from inventory indexing; AGENTS.md requires `display: [2]` for the primary screenshot. Confirm the mapping visually rather than assuming index equality. Baseline interactions and physical disconnection remain unexecuted.

## Quality profile

Scope every search to C# files changed by the current task, excluding bin/obj/generated files. Inspect matches; a pattern match alone is not a defect. Existing justified signatures and recorded debt are not new findings.

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | No unobserved async or sync-over-async in UI lifecycle | Blocking | `rtk rg -n 'async void|\.Result\b|\.Wait\(|GetAwaiter\(\)\.GetResult\(' <changed-cs-files>` | No new async work needed; event-handler exceptions must remain visible. |
| QA-02 | Do not swallow native failures | Blocking | `rtk rg -n 'catch|SetWindowLong|SetWindowPos' <changed-cs-files>` plus return/last-error ownership review | Existing EnableNonActivating implementation is baseline; new companion operations are checked. |
| QA-03 | Do not suppress warnings/nullability | Blocking | `rtk rg -n '#nullable disable|#pragma warning disable|NoWarn' <changed-files>` | None. |
| QA-04 | Preserve native focus/composition and input scope | Blocking | `rtk rg -n 'WM_MOUSEACTIVATE|MA_NOACTIVATE|SWP_|WS_EX_TRANSPARENT|SetLayeredWindowAttributes' <changed-cs-files>` plus TC-03/04 | Existing native callback has the required Win32 signature. |
| QA-05 | No new polling/global input interception; dispose owned events/HWND | Blocking | `rtk rg -n 'DispatcherTimer|LayoutUpdated|CompositionTarget|SetWindowsHookEx|Closed|Dispose|\+=' <changed-cs-files>` plus lifecycle review/TC-06 | Existing placement timer remains unchanged, not a new geometry timer. |
| QA-06 | Respect repository size/complexity boundaries | Reservation unless it conceals a defect | `rtk rg -c '^' <changed-cs-files>` plus methods/nesting/public XML review | New classes/files <=300, methods <=30, nesting <=3; baseline partial-window class debt is not extended with a new rendering responsibility. |
| QA-07 | Keep long parameter bundles explicit and cohesive | Reservation | `rtk rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' <changed-cs-files>` plus multiline-signature review | Native API/callback signatures are prescribed; custom render inputs use records. |

Escalation remains a human decision if there are 8+ new reservations, a touched file above 500 lines, or a repeated block in 3+ diff locations; no heavy audit starts inside this feature.

### Terrain baseline

Measured at base `6f2e92b`, before production changes. Public members include properties/constants; method count excludes the constructor.

| Existing target | Lines | Public members (methods) | Constructor dependencies | Switch cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `NotchWindow.xaml.cs` | 246 | 6 (2) | 0 | 0 | QA-07 line 104: Win32 callback; existing Window partial totals exceed 300 lines across files | Record callback exception; absorb new ownership in DEC-08 |
| `NotchWindow.Dock.cs` | 72 | 0 (0) | 0 | 0 | No selected grep hits | Record |
| `NotchWindow.Placement.cs` | 205 | 3 (1) | 0 | 0 | Existing placement debounce/reentrancy; no selected suppression/async hits | Record; preserve timer and persistence semantics |
| `NotchWindow.Scale.cs` | 40 | 0 (0) | 0 | 0 | No selected grep hits | Record |
| `WindowStyles.cs` | 96 | 7 (3) | 0 | 0 | Existing EnableNonActivating native return handling, lines 62-77; prescribed multiline interop signatures | Record; do not use this debt to excuse new unchecked operations |

`NotchWindow.xaml` and the Infrastructure test csproj are non-C# targets, outside these structural measures. New C# targets have no prior debt; all hits are new. Switch expressions are also reviewed directly: Dock has three small mode/edge mappings, with at most five explicit arms, not a saturated ten-case structure.

T03 baseline addendum: before touching `NotchWindow.Placement.cs`, it still has 205 lines and the same existing timer/reentrancy and native-call debt listed above. The current `HudContourController.cs` baseline has 192 lines from T02. T03 extracts its numeric DPI conversion and fixes two docking extent expressions: WPF's non-layout-rounded `SizeToContent` uses ceiling, while the existing placement used nearest rounding. At 50% HUD scale on 150% DPI, both right modes extended one native pixel outside the work area. This preserves placement coordinates/schema and addresses FR-02/08; no new timer or dependency is added.

Preparatory refactoring: not recommended as a separate workstream. No existing target crosses the reference's 500-line/10-public-member/6-dependency threshold. New rendering/lifecycle responsibility is isolated locally by DEC-08. Remeasure if another change edits these targets before execution; baseline does not grant blanket exemptions.

## Observability and rollout

- Structured debug logs on actual contour generation/mode/DPI changes and shadow lifecycle; no pointer/per-frame logging. Native failures include operation and numeric Win32 error, with no credentials or provider payloads.
- Rollout: HIL 2 -> baseline and serial tasks -> full validation -> human visual approval -> delegated review -> HIL 3. Updating documentation/checkpoints does not authorize code beforehand.
- No installer, release, commit, push, package, configuration migration, or ADR promotion is part of this feature. Rollback affects App chrome only.

## Risks and open items

- Companion synchronization is the main added complexity: test mixed-DPI dragging, hide/show, menus, exit, and transient layout explicitly. Current display inventory supports DPI/negative-coordinate checks.
- WPF antialiasing and effect clipping are framework behavior, not proven by pure tests. Desktop input and screenshot acceptance remain essential. If the proposed two-surface mechanism fails those checks, fix its cause; a different mechanism or lost shadow needs an exception HIL.
- (Planning-time, superseded 2026-10-09: baseline clicks recorded in T01; physical disconnection waived by DEC-05.) Baseline clicks and physical disconnection evidence are pending. HIL 2 approval authorizes the plan; it does not turn these unperformed checks into passes.
- (Planning-time, superseded: HIL 2 approved by DEC-03; visual gate approved 2026-10-09.) Human visual approval and HIL 2 are pending. Existing unrelated local tooling changes must be preserved.

## Relevant files

- Modify: the five existing C# targets in Terrain baseline, `src/TokenHound.App/UI/Windows/NotchWindow.xaml` and `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`. Existing WindowStylesTests remain regression coverage without planned edits.
- Create: the CMP-01..05 new files under `src/TokenHound.App/`, plus `tests/TokenHound.Infrastructure.Tests/Placement/HudContourLayoutTests.cs`, `HudContourTransformTests.cs`, and `Interop/HudContourStyleTests.cs`.
- Delivery documentation: this feature's `validation.md`, `README.md`, `docs/ROADMAP.md`, and the applicable Windows hit-testing statement in `ARCHITECTURE.md`.
