# Code review report — HUD Edge Geometry and Contour Hit Testing

## Summary

- Status: REJECTED
- Execution: delegated reviewer
- Git scope: `6f2e92b0f4cc40c5cde1326149f000768fe3a781..2bc32ef` (commit `2bc32ef`) plus the uncommitted worktree: `ARCHITECTURE.md`, `docs/ROADMAP.md`, the feature's SDD records and evidence files, and the `codereview_01/` correction reports. No staged changes. No uncommitted `src/` or `tests/` changes.
- Previous review: `tasks/prd-16-hud-geometry-hit-testing/codereview_01/codereview.md` (REJECTED). Corrections: `codereview_01/task_01.md`, `codereview_01/task_02.md`.

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-16-hud-geometry-hit-testing/prd.md` | read |
| TechSpec | `tasks/prd-16-hud-geometry-hit-testing/techspec.md` | read, including the quality profile, Terrain baseline and T03 addendum |
| Manifest | `tasks/prd-16-hud-geometry-hit-testing/tasks.md` | read. T01..T03 links resolve to `done/`. The root `task_03.md` is deleted in the worktree and `done/task_03.md` is present. |
| Handoffs | `done/task_03.md`, plus the correction handoffs `codereview_01/task_01.md` and `task_02.md` | read. `done/task_01.md` and `done/task_02.md` are unchanged since review 01. |
| Evidence | `validation.md` (T03 and correction round 1 sections), `workflow.md`, `checkpoint.json` (read only), `cr01-*.png` (3 opened), `cr01-native-*.json` (2 opened) | read |
| Snapshot | `context-snapshot.md` | Loaded with the independent-stage filter: header, next step brief, open threads and on-run entries only. Decisions and Code map were skipped. `git_head` 2bc32ef matches HEAD. `covers_through` matches the manifest and the correction folder. |
| Implementation | `git diff 6f2e92b HEAD`: 17 C# files, 2 XAML files and the test csproj. The worktree has no code delta. | delimited |

The code under review has not changed since codereview_01: the same HEAD, and no `src/` or `tests/` worktree change. The correction round changed only evidence and records.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 / FR-04 / US-02 / PD-04 | Outside points, including shadow, reach a different process | `HudContourDecorator` (zero alpha outside the envelope); `HudShadowWindow` + `HudShadowInterop.EnablePassive` (`WindowStyles.ApplyShadowStyles` adds WS_EX_TRANSPARENT/LAYERED) | TC-04 manual | conformant | `validation.md:55-63`: six modes, plus Right edge at 150%, with left, right and double clicks in gutters, joins, corners and shadow. Companion ExStyle `0x080800A8` in `cr01-native-*.json` |
| FR-05 | Fill and stroke are interactive; shadow is not | `HudContourDecorator` HitTestCore; passive companion | TC-04 manual | conformant | The inside right-click opened the menu inward in every mode (`validation.md:57-63`). The stroke anti-aliasing capture at (865,381) is explained in line 61 |
| FR-01 / OBJ-02 / US-01 | Smooth mirrored inverse joins | `HudContourLayout.cs` | `HudContourLayoutTests` | conformant | Full suite passed in this review. Visual evidence: `validation.md:65`, `t03-visual-gate-20261009.png`, human gate (`workflow.md:53`) |
| FR-02 / PD-03 | Top corners omit the unavailable continuation and stay in the work area | `HudContourLayout.cs:152-156`; `NotchWindow.Placement.cs` uses `HudContourTransform.PixelExtent` | PixelExtent and layout tests | conformant | Right=1920 after the fix (`validation.md:53`) |
| FR-03 / PD-02 | Free is horizontal and fully rounded | `HudContourLayout.cs` (wings = 0 for Free) | Layout tests | conformant | `validation.md:61,67`; `cr01-empty-free.png` shows a fully rounded empty pill |
| FR-06 | A placement switch updates shape and hit area together | `NotchWindow.Dock.cs` (`CapsuleBorder.Mode = mode`); controller `FrameChanged` | TC-03/04 | conformant | Six-mode switching with matching owner and companion bounds (`validation.md:53-68`) |
| FR-07 / US-03 | Drag to Free and restore after restart | Existing `RecordDrag` (unchanged) | TC-05 manual | conformant | `validation.md:67` (restart PID 27116, `Mode=Free`, `Display=null`) |
| FR-08 / US-04 | Sizes, DPI, negative coordinates, preferred-display disconnection | `HudContourController.ContourTransform`; `HudContourTransform` | `HudContourTransformTests` | not verifiable (disconnection part) | Sizes, three DPIs and negative coordinates are evidenced (`validation.md:66-68`). Physical disconnection is not executed (`validation.md:71`). See CR-01 |
| FR-09 | Placement schema unchanged | No change in `src/TokenHound.Core` or `src/TokenHound.Infrastructure` | Placement and settings regressions | conformant | `git diff --name-only 6f2e92b HEAD` lists no Core or Infrastructure source |
| FR-10 / US-05 | Hover details, StatusPopup and menus unchanged and inward | `NotchWindow.xaml` keeps `CapsuleBorder`, its ContextMenu and the StatusPopup anchor | TC-05 manual | conformant | Hover and menus: `validation.md:65`. StatusPopup docked and Free, inward, with dismissal: `validation.md:42-43`; `cr01-top-center-status-popup.png` (opened and checked: the popup is below the capsule and not clipped) |
| FR-11 | Provider layout, badges, busy pulse, empty/minimum layout | `HudContourDecorator` Measure/Arrange; the XAML diff does not touch Busy, Pulse, Badge, Storyboard or Animation lines | Layout empty-content tests | conformant | Empty HUD docked (Right edge) and Free: `validation.md:46`, `cr01-empty-right-edge.png`, `cr01-empty-free.png`, owner and companion bounds equal in `cr01-native-empty-*.json`. Busy state captured during a refresh (`validation.md:44`). The badge limitation is stated (`validation.md:45`). The busy and badge templates are unchanged in code |
| NFR-01 | No focus theft; non-activation invariants | `NotchWindow.xaml.cs:106-114` (unchanged hook); `HudShadowWindow.xaml.cs:62-70`; `HudShadowInterop.SWP_FLAGS = 0x0010 | 0x0004` | `HudContourStyleTests`, `WindowStylesTests` | conformant | Keys reached the target after the menus in every mode (`validation.md:57-63`) |
| NFR-02 | No polling, no global hook, no idle redraw | Event-driven controller; `Schedule` coalesces pending work | QA-05 | conformant | No new timer or hook. Idle CPU was below the v0.1.13 baseline (`validation.md:69`) |
| NFR-03 | Core and data boundaries | No Core or Infrastructure change | Scoped diff | conformant | Diff scope |
| NFR-04 | Keyboard access and contrast preserved | No change to Settings, tray or dialogs | — | conformant | Diff scope |
| NFR-05 | Honest evidence; E2E omitted | `validation.md` | — | conformant | Coordinator and human evidence are separate, and limitations are named |
| PD-01 | Existing palette, stroke and shadow | `NotchWindow.xaml`; `HudShadowWindow.xaml` | — | conformant | Human visual gate approved (`workflow.md:53,71`) |
| TC-01..03 | Unit and regression tests | 3 new test files | Full Infrastructure run | conformant | 1,082/1,082 passed in this review |
| TC-04..06 | Manual matrix | — | — | partial | Step 6 (disconnection) is not executed: CR-01. Graceful tray Exit is reused from T02: see limitations |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity | OK | No Core change |
| WM_MOUSEACTIVATE -> MA_NOACTIVATE on HUD windows | OK | `NotchWindow.xaml.cs:109-114`, `HudShadowWindow.xaml.cs:65-70` |
| `EnableNonActivating` flags unchanged; new helper without SWP_SHOWWINDOW | OK | `WindowStyles.cs:37` unchanged; `HudShadowInterop.cs:10` uses NOACTIVATE \| NOZORDER |
| One class per file, sealed, <=300 lines | OK | Largest C# file 290 lines (`HudContourLayout.cs`) |
| Blank line before `return` | NOT OK (minor, persistent) | `HudContourLayout.cs:157-158` |
| dotnet-efficient-validation; MTP arguments after `--`; `--minimum-expected-tests 1` | OK | Commands below |
| E2E policy | N/A | Omitted under the .NET desktop policy. This is not a defect and not approved testing. |

## Quality profile

Scope: the 17 C# files from `git diff --name-only 6f2e92b HEAD -- '*.cs'`. QA-03 also scans the two XAML files and the test csproj.

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Async / sync-over-async | blocking | `rg -n 'async void\|\.Result\b\|\.Wait\(\|GetAwaiter\(\)\.GetResult\(' <files>` | 0 | OK |
| QA-02 | Native failures | blocking | `rg -n 'catch\|SetWindowLong\|SetWindowPos' <files>` | 0 new defects of 13 hits | OK. `WindowStyles.cs:77-97` is baseline debt. `HudShadowInterop.cs:24-71` checks every return value and captures the last error. `HudContourController.cs:110-116` logs NativeErrorCode, hides the shadow and sets `_failed`, so it does not retry. A failure in `EnablePassive`, called from `SourceInitialized` through `EnsureHandle`, is inside the same try. |
| QA-03 | Suppressions | blocking | `rg -n '#nullable disable\|#pragma warning disable\|NoWarn' <files>` | 0 | OK |
| QA-04 | Focus, composition and input scope | blocking | `rg -n 'WM_MOUSEACTIVATE\|MA_NOACTIVATE\|SWP_\|WS_EX_TRANSPARENT\|SetLayeredWindowAttributes' <files>` | 0 new defects | OK. WS_EX_TRANSPARENT is used only in `ApplyShadowStyles` (`WindowStyles.cs:51`). Tests assert that the main HUD does not have it. No SetLayeredWindowAttributes. |
| QA-05 | Polling, hooks and disposal | blocking | `rg -n 'DispatcherTimer\|LayoutUpdated\|CompositionTarget\|SetWindowsHookEx\|Closed\|Dispose\|\+=' <files>` | 0 new defects | OK. `DispatcherTimer` is the existing placement timer. The controller's six subscriptions (`HudContourController.cs:33-38`) are removed in `Dispose` (`:50-55`). The shadow window removes its handlers in `OnClosed` (`HudShadowWindow.xaml.cs:53-60`). |
| QA-06 | Size and complexity | reservation | `rg -c '^' <files>` | 0 | OK. The maximum is 290 lines. |
| QA-07 | Long parameter bundles | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' <files>` | 1 new of 10 | `HudShadowInterop.cs:12` `Bounds(int Left, int Top, int Width, int Height)` is a cohesive native rectangle: reservation. The WndProc signatures are prescribed by Win32. The remaining hits are false positives: InlineData, an object initializer, and a 3-argument call whose nested commas match the pattern (`HudContourController.cs:195`). |

- Terrain baseline: applied from the TechSpec, including the T03 addendum
- Hits discounted by baseline: 7 (`WindowStyles.cs:77-97` native calls; `NotchWindow.xaml.cs:106` callback signature)
- Reservations accumulated in the feature: 1
- Suggested escalation: `no trigger fired` (1 < 8 reservations; no touched file above 500 lines; no block repeated in 3+ diff locations)
- `git diff --check 6f2e92b HEAD`: clean (exit 0)

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 pure geometry in App/UI/Placement | YES | `HudContour{Layout,Frame,Segment,Point,Transform}.cs`, linked into the test csproj |
| DEC-02 per-pixel alpha, no HTTRANSPARENT | YES | No WM_NCHITTEST handling. The main decorator has no DropShadowEffect. |
| DEC-03 passive WS_EX_TRANSPARENT companion with an exclusion mask | YES | `HudShadowWindow.SetContour` subtracts the transformed envelope from the window rectangle |
| DEC-04 event-driven sync with unchanged-frame suppression | YES | `HudContourController.Schedule` and `Synchronize(HudContourGeometry)` compare bounds, contour, transform and DPI before native or render work |
| DEC-05 keep the CapsuleBorder name, menu and popup anchor | YES | `NotchWindow.Dock.cs` (`CapsuleBorder.Mode`) |
| DEC-06 coordinate semantics; scale and DPI applied once | YES | `HudContourTransform`; owner and companion bounds match across DPIs |
| DEC-07 reuse the test executable | YES | csproj source links only |
| DEC-08 new controller, not a NotchWindow partial | YES | `HudContourController.cs`; NotchWindow gains one construction line |
| Errors: check, log, hide, no retry loop | YES | `HudShadowInterop`; `_failed` flag |
| Manual script steps 3 and 5 | YES | The correction round added StatusPopup and empty-state evidence. Step 5 is complete except graceful Exit, which is reused from T02. |
| Manual script step 6 (physical disconnection) | NO | Not executed. The human deferred it to HIL 3 (`workflow.md:55,72`). See CR-01. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Baseline recorded and geometry tests in place. Unchanged since review 01. |
| T02 | `done/task_02.md` | COMPLETE | Rendering, companion and native integration, with six-mode click/focus, hide/show and graceful Exit evidence. Unchanged since review 01. |
| T03 | `done/task_03.md` | COMPLETE with open item | T03.4 carries "physical disconnection pending". "Produced result" (line 69) now records the approved visual gate. Lines 72-73 still describe the visual gate as pending: see CR-02. |
| codereview_01/T01 | `codereview_01/task_01.md` | COMPLETE | `validation.md:39-47` and the `cr01-*` files exist. The checked screenshots and JSON match the text. |
| codereview_01/T02 | `codereview_01/task_02.md` | INCOMPLETE against its own acceptance criterion | The gates table (`workflow.md:53-55`), the snapshot and `done/task_03.md:69` were updated. The criterion "No record states the visual gate as pending" is not met: see CR-02. |

## Executed validations

- Profile and exclusions: .NET 10 desktop (a WPF App and a net10.0 MTP executable with xunit.v3 and `UseMicrosoftTestingPlatformRunner=true`). E2E is omitted by the .NET desktop policy.
- Validated state: HEAD `2bc32ef`, no staged changes, no uncommitted `src/` or `tests/` changes, Release configuration.
- Reused evidence: the desktop matrix in `validation.md` "T03 desktop acceptance, 2026-10-09" and "Correction round 1 evidence". Both were recorded on the `2bc32ef` build, and production code has not changed since, so they remain valid. Graceful Exit and hide/show come from T02, and the lifecycle paths are unchanged since then.
- Manual acceptance: the human visual gate is approved (`workflow.md:53,71`). Physical disconnection is not executed (CR-01). By contract, the reviewer did not launch the app or use the desktop.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal` | passed: 0 errors, 0 warnings, exit 0 | All production code |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` | passed: 0 errors, 0 warnings, exit 0 | CMP-07 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --no-banner` | passed: 1,082 total, 1,082 succeeded, 0 failed, 0 skipped, exit 0 | TC-01, TC-02, TC-03 and existing regressions |
| `git diff --check 6f2e92b HEAD` | clean, exit 0 | Repository hygiene |
| QA-01..07 commands above | see Quality profile | QA-01..07 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Medium | FR-08, US-04, TechSpec manual step 6 (persistent from codereview_01/CR-01) | `validation.md:71`: physical disconnection and reconnection of the preferred display are "Not covered by the coordinator". `techspec.md:112`: "a resolver unit test does not close this desktop case". `workflow.md:55,72`: the human chose "Aceitar sem esse teste" and deferred the item to HIL 3. No TechSpec `DEC-NN` covers it. | Essential manual evidence for FR-08 is still missing, so the review cannot treat it as passed. | Get human evidence of disconnection and reconnection, or have HIL 3 explicitly accept the gap as residual risk. No code change is indicated. |
| CR-02 | Low | SDD state consistency (remainder of codereview_01/CR-03; acceptance criterion of `codereview_01/task_02.md:47`) | `done/task_03.md:72` still says "No full manual matrix, human visuals or final acceptance is claimed". `done/task_03.md:73` lists "human visual gate" and "visual gate, Anti Slop gate, independent review and HIL 3 remain pending". `tasks.md:89` says "Desktop unavailable at the human's request". `validation.md:37` says "Human visual approval and independent review remain pending". The Anti Slop delivery gate (workflow DEC-04) is listed as pending in `validation.md:84,114,126` and `done/task_03.md:73`, and no record shows it closed. | A later session can read the wrong gate state. The Anti Slop gate outcome cannot be verified from the records. | The coordinator should update the T03 handoff "Validated state" and "Open items", the manifest state line and the stale `validation.md` "Integrated desktop acceptance" paragraph from the recorded events, and record the Anti Slop gate's outcome or its closure. No code change. |

### Optional improvements

- QA-07 reservation: `HudShadowInterop.cs:12` `Bounds(int Left, int Top, int Width, int Height)`. It is a cohesive native rectangle, so keeping it is acceptable.
- Style (persistent): `HudContourLayout.cs:157-158` has no blank line before `return new Shape`.
- Persistent from review 01, not defects because rendering is event-driven: `HudContourDecorator.ArrangeOverride` builds the layout twice when the request changes, and `OnRender` allocates a frozen Pen on each render.
- `codereview_01/task_01.md:80-83` and `task_02.md:73-76` share identical "Changed files", "Checks" and "Validated state" text that mixes both tasks. Separate handoffs would trace better.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_01/CR-01 | persistent | No disconnection evidence was added. The human deferral is recorded for HIL 3 (`workflow.md:55`). Carried as CR-01. |
| codereview_01/CR-02 | resolved | `validation.md:39-47`: StatusPopup docked and Free with dismissal, busy state during refresh, badge limitation stated, empty HUD in Right edge and Free with owner and companion bounds matching (`cr01-*.png`, `cr01-native-empty-*.json`, checked). The XAML diff does not touch the busy or badge templates. |
| codereview_01/CR-03 | persistent | Partially corrected: the gates table (`workflow.md:48-55`), `done/task_03.md:69` and the snapshot header are corrected. Stale pending-gate statements remain in `done/task_03.md:72-73`, `tasks.md:89` and `validation.md:37`. Carried as CR-02. |

## Limitations and open items

- By contract, the reviewer did not launch the app or interact with the desktop. All manual acceptance comes from the coordinator's recorded evidence and the human visual approval. The reviewer opened 3 of the 5 `cr01-*.png` screenshots and both `cr01-native-*.json` files, and did not re-measure on screen.
- Graceful tray Exit and tray hide/show were not re-driven at T03 (`validation.md:70`). The T02 evidence is reused because the lifecycle code is unchanged since T02.
- HUD sizes are a continuous 50..150% range. FR-08's "every existing size selection" is treated as satisfied by sampling the minimum, default and maximum sizes plus 125%, as in review 01.
- The busy pulse motion cannot be shown in still captures (`validation.md:44`). The pulse template is unchanged in the diff, so FR-11 rests on code identity plus the captured busy period.
- In the `cr01-native-*.json` files, the installed instance (PID 8068) also exposes a "TokenHound shadow" window. That instance is outside the reviewed build, and this review does not interpret it.
- Under the independent-stage filter, the snapshot's Decisions and Code map entries were skipped. Open thread O-07 (pre-existing idle CPU) is outside this feature.
- ContextBrake asked for a session handoff under `.context-brake/`. It was not written, because the delegated-reviewer contract allows writing only this report.

## Conclusion

The code under review is identical to the code in codereview_01. It follows the approved TechSpec: a shared pure contour, per-pixel alpha pass-through, a passive WS_EX_TRANSPARENT shadow companion, checked native calls, event-driven synchronization, and no Core, schema or focus-invariant regression. Both Release builds are clean, the full Infrastructure suite passes (1,082/1,082), and the quality profile has no blocking hit and 1 reservation.

The correction round resolved CR-02 with desktop evidence for the StatusPopup and the empty HUD, and corrected most of the gate records. The status remains REJECTED because essential FR-08 manual evidence is still missing: physical preferred-display disconnection (CR-01) is not executed and is not covered by a TechSpec `DEC-NN`. There is also a Low records inconsistency (CR-02). Evidence or an explicit HIL 3 acceptance can resolve both; no code change is indicated.
