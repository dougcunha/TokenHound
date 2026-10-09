# Code review report — HUD Edge Geometry and Contour Hit Testing

## Summary

- Status: APPROVED WITH RESERVATIONS
- Execution: delegated reviewer
- Git scope: `6f2e92b0f4cc40c5cde1326149f000768fe3a781..2bc32ef80e0e3aff4070bcbd54a45be73ac23855` (one commit, `2bc32ef`) plus the uncommitted worktree. The worktree has no staged changes and no `src/` or `tests/` changes. Its feature changes are SDD records and evidence (`techspec.md`, `tasks.md`, `validation.md`, `workflow.md`, `done/task_03.md`, the root `task_03.md` deletion, `anti-slop-delivery-gate.md`, `cr01-*` and `t03-*` evidence) plus `ARCHITECTURE.md`. The `docs/ROADMAP.md` worktree change concerns PRD 17 and is outside this feature.
- Previous review: `tasks/prd-16-hud-geometry-hit-testing/codereview_02/codereview.md` (REJECTED). Earlier: `codereview_01/codereview.md` (REJECTED), with correction tasks `codereview_01/task_01.md` and `task_02.md`.

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-16-hud-geometry-hit-testing/prd.md` | read. SHA-256 `218c9fc6...346e8` matches `checkpoint.json` `approved_sources` (DEC-02). |
| TechSpec | `tasks/prd-16-hud-geometry-hit-testing/techspec.md` | read, including the quality profile, Terrain baseline and T03 addendum. SHA-256 `5b6749bf...85d9a1` matches the checkpoint. The only worktree change is the waiver annotation on manual step 6 (`techspec.md:112`). |
| Manifest | `tasks/prd-16-hud-geometry-hit-testing/tasks.md` | read. SHA-256 matches the checkpoint. The T01..T03 links resolve to `done/`. The State section marks all three complete (`tasks.md:87-89`). |
| Handoffs | `done/task_01.md`, `done/task_02.md` (hashes unchanged since review 02), `done/task_03.md` (read) | read |
| Evidence and records | `validation.md`, `workflow.md` (including DEC-05), `anti-slop-delivery-gate.md`, `checkpoint.json` (read only) | read |
| Snapshot | `context-snapshot.md` | Loaded with the independent-stage filter: header, next step brief, open threads and on-run entries only. Decisions and Code map were skipped. `git_head` 2bc32ef matches HEAD. The header is behind the stage source: `covers_through` ends at correction round 1, and `next_step` names `codereview_02`. The next step brief was treated as stale. |
| Implementation | `git diff 6f2e92b HEAD`: 17 C# files, 2 XAML files and the test csproj (1,490 insertions, 35 deletions) | delimited |

The production and test code is the same as in codereview_01 and codereview_02: the HEAD is the same, and the worktree has no `src/` or `tests/` change. Since review 02, the round-2 corrections changed only records, evidence and the TechSpec waiver annotation.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 / FR-04 / US-02 / PD-04 | Mouse input outside the contour, including shadow pixels, reaches another process | `HudContourDecorator` paints only `Contour.Fill` and the stroke, with `Clip = Envelope`, so pixels outside it have zero alpha. `HudShadowWindow` + `HudShadowInterop.EnablePassive` → `WindowStyles.ApplyShadowStyles` (adds `WS_EX_TRANSPARENT \| WS_EX_LAYERED`) | `HudContourStyleTests`; TC-04 manual | conformant | `validation.md:59-67`: in six modes plus Right edge at 150%, left, right and double clicks at gutter, join, corner and shadow points reached a different-process target |
| FR-05 | Fill and stroke are interactive; the shadow is not | `HudContourDecorator.HitTestCore:120-122` (Envelope = fill ∪ widened stroke) | TC-04 manual | conformant | The inside right click opened the HUD menu in every mode (`validation.md:61-67`). The stroke anti-aliasing hit at (865,381) is explained at `validation.md:65` |
| FR-01 / OBJ-02 / US-01 | Smooth, mirrored inverse joins on docked edges | `HudContourLayout.BuildSegments/TopRight/TopLeft/Bottom` with control factor 0.5522847498307936. Side modes rotate the canonical top shape (`Transform:250-266`) | `HudContourLayoutTests` | conformant | 1,082/1,082 tests passed in this review. Coordinator visuals (`validation.md:69`) and the human visual gate (`workflow.md:53`) |
| FR-02 / PD-03 | Top corners omit the unavailable continuation and stay inside the work area | `HudContourLayout.CompleteShape:152-156` (one wing for TopLeft/TopRight); `NotchWindow.Placement.cs:131-132` uses `HudContourTransform.PixelExtent` (ceiling) | `PixelExtent` and layout tests | conformant | Right = 1920 after the fix, in Right edge and Top right (`validation.md:57`) |
| FR-03 / PD-02 | Free is horizontal and fully rounded | `HudContourLayout` (`wings = 0`; `Free` rounds all four corners) | Layout tests | conformant | `validation.md:65,71`; `cr01-empty-free.png` (`validation.md:50`) |
| FR-06 | A placement switch updates shape and hit area together | `NotchWindow.Dock.cs` sets `CapsuleBorder.Mode`. `OnLayoutPropertyChanged` clears the contour and raises `FrameChanged`. The controller hides the stale shadow (`HudContourController.Schedule:82-83`) until the next arrange | TC-03/04 | conformant | Six-mode switching with matching owner and companion bounds (`validation.md:57-67`, `t03-native-*.json`) |
| FR-07 / US-03 | Drag to Free; restore after restart | Existing `RecordDrag` path (not in the diff) | TC-05 manual | conformant | `validation.md:71` (restart PID 27116, `Mode=Free`, `Display=null`, same bounds) |
| FR-08 / US-04 | Sizes, DPI, negative coordinates and preferred-display disconnection | `HudContourController.ContourTransform` → `HudContourTransform.ForDpi`; `PixelExtent` | `HudContourTransformTests` | conformant for sizes, DPI and negative coordinates. The disconnection part is not verifiable, and workflow DEC-05 accepts it as residual risk | Sizes 50/80/100/125/150 %, three DPIs and negative coordinates: `validation.md:70-72`. Disconnection was not executed (`validation.md:75`). The waiver is recorded at `workflow.md:67-73` and `techspec.md:112` |
| FR-09 | Placement schema unchanged | No change in `src/TokenHound.Core` or `src/TokenHound.Infrastructure` | Placement and settings regressions in the full suite | conformant | `git diff --stat 6f2e92b HEAD -- src tests` lists only App files and tests |
| FR-10 / US-05 | Hover details, StatusPopup and menus unchanged and inward | `NotchWindow.xaml` keeps the `CapsuleBorder` name, its ContextMenu and the StatusPopup `PlacementTarget` | TC-05 manual | conformant | Hover and menus: `validation.md:69`. StatusPopup docked and in Free, with dismissal: `validation.md:46-47` |
| FR-11 | Provider layout, badges, busy pulse and empty/minimum layout | `HudContourDecorator.MeasureOverride/ArrangeOverride`. The XAML diff does not touch the ring template | Empty-content layout tests | conformant | Empty HUD in Right edge and Free: `validation.md:50`. Busy state: `validation.md:48`. The badge limitation is stated at `validation.md:49` |
| NFR-01 | No focus theft; non-activation invariants | `NotchWindow.xaml.cs:106-114` (hook unchanged); `HudShadowWindow.WndProc:62-71` returns `MA_NOACTIVATE`; `HudShadowInterop.SWP_FLAGS = SWP_NOACTIVATE \| SWP_NOZORDER`, without `SWP_SHOWWINDOW` | `HudContourStyleTests`, `WindowStylesTests` | conformant | After each inside menu, the keys reached the target (`validation.md:61-67`) |
| NFR-02 | No polling, no global hook, no idle redraw | Event-driven controller. `Schedule` coalesces work while an operation is pending. `Synchronize` compares bounds, contour, transform and DPI before native or render work | QA-05 | conformant | No new timer or hook. Idle CPU was below the v0.1.13 baseline (`validation.md:73`) |
| NFR-03 | Core and data boundaries | No Core or Infrastructure change | Scoped diff | conformant | Diff scope |
| NFR-04 | Keyboard access and contrast preserved | No change to Settings, tray or dialogs | — | conformant | Diff scope |
| NFR-05 | Honest, separated evidence; E2E omitted | `validation.md` | — | conformant | Coordinator, human and automated evidence are separated, and limitations are named (`validation.md:5,41,75`) |
| PD-01 | Existing palette, stroke and shadow | `NotchWindow.xaml` (`#18181B`/`#3F3F46`); shadow companion | — | conformant | Human visual gate (`workflow.md:53`). Anti Slop gate PASS (`anti-slop-delivery-gate.md:78`) |
| TC-01..03 | Unit and regression tests | `HudContourLayoutTests`, `HudContourTransformTests`, `HudContourStyleTests` | Full Infrastructure run | conformant | 1,082/1,082 passed, exit 0 (this review) |
| TC-04..06 | Manual matrix | — | — | conformant, with the residual risk accepted for step 6 | Steps 2-5 and 7: `validation.md:53-75`. Step 6 is waived by DEC-05. Graceful Exit and hide/show are reused from T02 (`validation.md:74,128`) |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity | OK | No Core change |
| `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` on HUD windows | OK | `NotchWindow.xaml.cs:109-114`, `HudShadowWindow.xaml.cs:65-70` |
| `EnableNonActivating` flags unchanged; no `SWP_SHOWWINDOW` in the new helper | OK | `WindowStyles.cs:37` is not in the diff. `HudShadowInterop.cs:10` uses `0x0010 \| 0x0004` |
| One class per file; sealed; ≤ 300 lines | OK | The largest changed C# file has 290 lines (`HudContourLayout.cs`) |
| Blank line before `return` | NOT OK (minor, persistent) | `HudContourLayout.cs:157-158` |
| Fully qualified type instead of a `using` directive | Minor | `HudContourGeometry.cs:41` uses `Infrastructure.Configuration.HudDockMode.Free` |
| dotnet-efficient-validation; MTP arguments after `--`; `--minimum-expected-tests 1` | OK | Commands below |
| E2E policy | N/A | Omitted under the .NET desktop policy. This is neither a defect nor approved testing. |

## Quality profile

Scope: the 17 C# files from `git diff --name-only 6f2e92b HEAD -- '*.cs'`. QA-03 also scans the two XAML files and the test csproj.

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Async / sync-over-async | blocking | `rg -n 'async void\|\.Result\b\|\.Wait\(\|GetAwaiter\(\)\.GetResult\(' <files>` | 0 | OK |
| QA-02 | Native failures | blocking | `rg -n 'catch\|SetWindowLong\|SetWindowPos' <files>` | 0 new defects of 13 hits | OK. `WindowStyles.cs:77-97` is baseline debt. `HudShadowInterop.cs:24-71` checks every return value and captures the last error. `HudContourController.cs:110-116` logs `NativeErrorCode`, hides the shadow and sets `_failed`, so it does not retry. |
| QA-03 | Suppressions | blocking | `rg -n '#nullable disable\|#pragma warning disable\|NoWarn' <files>` | 0 | OK |
| QA-04 | Focus, composition and input scope | blocking | `rg -n 'WM_MOUSEACTIVATE\|MA_NOACTIVATE\|SWP_\|WS_EX_TRANSPARENT\|SetLayeredWindowAttributes' <files>` | 0 new defects | OK. `WS_EX_TRANSPARENT` is added only in `ApplyShadowStyles` (`WindowStyles.cs:51`). `HudContourStyleTests.cs:31` asserts that the main HUD does not have it. No `SetLayeredWindowAttributes`. |
| QA-05 | Polling, hooks and disposal | blocking | `rg -n 'DispatcherTimer\|LayoutUpdated\|CompositionTarget\|SetWindowsHookEx\|Closed\|Dispose\|\+=' <files>` | 0 new defects | OK. `DispatcherTimer` is the existing placement timer (`NotchWindow.Placement.cs:21,61`). The six controller subscriptions (`HudContourController.cs:33-38`) are removed in `Dispose` (`:50-55`). `HudShadowWindow.OnClosed` removes its hook and handlers. |
| QA-06 | Size and complexity | reservation | `rg -c '^' <files>` | 0 | OK. Maximum 290 lines. `NotchWindow.xaml.cs` grows from 246 to 248. |
| QA-07 | Long parameter bundles | reservation | `rg -nP '\w+\((?:[^),]+,){3,}[^)]*\)' <files>` | 1 new of 10 | `HudShadowInterop.cs:12` `Bounds(int Left, int Top, int Width, int Height)`: a cohesive native rectangle, recorded as a reservation. WndProc signatures are prescribed by Win32. The other hits are false positives: InlineData, an object initializer, and nested calls with three or fewer arguments. |

- Terrain baseline: applied from the TechSpec, including the T03 addendum.
- Hits discounted by baseline: 7 (`WindowStyles.cs:77-97` native calls; the `NotchWindow.xaml.cs:106` callback signature).
- Reservations accumulated in the feature: 1.
- Suggested escalation: `no trigger fired` (1 < 8 reservations; no touched file above 500 lines; no block repeated in 3+ diff locations).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01: pure geometry in App/UI/Placement | YES | `HudContour{Layout,Frame,Segment,Point,Transform}.cs` have no WPF or Win32 types and are linked into the test csproj |
| DEC-02: per-pixel alpha, no HTTRANSPARENT | YES | No `WM_NCHITTEST` handling. The main decorator has no `DropShadowEffect`; it was removed from `NotchWindow.xaml` |
| DEC-03: passive `WS_EX_TRANSPARENT` companion with an exclusion mask | YES | `HudShadowWindow.SetContour:26-42` subtracts the transformed envelope from the window rectangle |
| DEC-04: event-driven sync with unchanged-frame suppression | YES | `HudContourController.Schedule:76-89`, `Synchronize:119-142` |
| DEC-05: keep the `CapsuleBorder` name, menu and popup anchor | YES | `NotchWindow.xaml` keeps `x:Name="CapsuleBorder"`, the ContextMenu and `PlacementTarget` |
| DEC-06: coordinate semantics; scale and DPI applied once | YES | `HudContourTransform.ForDpi` applies only the owner-to-shadow DPI ratio. Owner and companion bounds match across three DPIs |
| DEC-07: reuse the test executable | YES | csproj source links only |
| DEC-08: a new controller, not a NotchWindow partial | YES | `HudContourController.cs`. NotchWindow gains one construction line (`NotchWindow.xaml.cs:45`) |
| Errors: check, log, hide, no retry loop | YES | `HudShadowInterop` throws `Win32Exception`, and the controller catches it once and sets `_failed` |
| Manual script steps 2-5 and 7 | YES | `validation.md:43-75`; visual gate at `workflow.md:53` |
| Manual script step 6 (physical disconnection) | Waived | Not executed. Workflow DEC-05 (`workflow.md:67-73`) records the human's acceptance of the residual risk. `techspec.md:112` is annotated to match. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Hash matches the checkpoint and is unchanged since review 02. Baseline in `validation.md:7-29`; geometry tests pass |
| T02 | `done/task_02.md` | COMPLETE | Hash unchanged. Rendering, companion, six-mode input, hide/show and graceful Exit (`validation.md:120-129`) |
| T03 | `done/task_03.md` | COMPLETE | All six items are checked. The handoff records validated state and open items (`done/task_03.md:69-73`). The T03.4 label still says "physical disconnection pending human evidence" (`:36`); DEC-05 now waives that step |
| codereview_01/T01, T02 | `codereview_01/task_01.md`, `task_02.md` | COMPLETE | Verified in review 02. The remainder of codereview_01/T02 was carried as codereview_02/CR-02, which is resolved below |
| Round 2 (codereview_02) | No task file | Applied directly by the coordinator | `workflow.md:79` records the round. The resulting records were checked: see the previous findings |

## Executed validations

- Profile and exclusions: .NET 10 desktop (a WPF App, and a net10.0 MTP executable with xunit.v3 and `UseMicrosoftTestingPlatformRunner=true`). E2E is omitted by the .NET desktop policy.
- Validated state: HEAD `2bc32ef`, no staged changes, no uncommitted `src/` or `tests/` changes, Release configuration, this machine.
- Reused evidence: the desktop matrix in `validation.md` "T03 desktop acceptance, 2026-10-09" and "Correction round 1 evidence". Both were recorded on the `2bc32ef` build, and production code has not changed since, so they remain valid. Graceful Exit and hide/show are reused from T02: the lifecycle code has not changed since T02.
- Manual acceptance: the human visual gate is approved (`workflow.md:53`). Physical disconnection was not executed; the residual risk is accepted by DEC-05. Under the contract, the reviewer did not launch the app or use the desktop.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal` | passed: 0 errors, 0 warnings, exit 0 | All production code |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` | passed: 0 errors, 0 warnings, exit 0 | CMP-07 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --no-banner` | passed: 1,082 total, 1,082 succeeded, 0 failed, 0 skipped, exit 0 | TC-01, TC-02, TC-03 and existing regressions |
| `git diff --check 6f2e92b HEAD` | clean, exit 0 | Repository hygiene |
| QA-01..07 commands above | see Quality profile | QA-01..07 |
| `sha256sum` of the PRD, TechSpec, manifest and the three handoffs | all six match `checkpoint.json` `approved_sources` | Source integrity |

## Findings

No new actionable finding. This review found no code defect, no blocking profile hit and no contradiction between records.

### Optional improvements

- QA-07 reservation: `HudShadowInterop.cs:12` `Bounds(int Left, int Top, int Width, int Height)`. It is a cohesive native rectangle, so keeping it is acceptable.
- Records precision, not contradiction. DEC-05 does not conflict with HIL 3, which still applies, but the following lines predate DEC-05 and do not mention it:
  - `validation.md:5,41`: "for the HIL 3 decision" and "decided at HIL 3"
  - `tasks.md:89`: "accepted as an open item for HIL 3"
  - `done/task_03.md:36,69,73`: "pending human evidence" and "to be confirmed at HIL 3"
  - The gates table at `workflow.md:54` still reads "Round 1 REJECTED; corrections done; re-review pending" and omits round 2, which `workflow.md:79` records.
  - `techspec.md:161-162` (Risks) still says "physical disconnection evidence are pending" and "Human visual approval and HIL 2 are pending". These are planning-time statements that DEC-03, DEC-05 and the visual gate superseded.
- Traceability: `checkpoint.json` lists `techspec.md` under `decision_id: DEC-03`, but its latest content change, the step 6 waiver, comes from DEC-05. DEC-05 sits under the "HIL 1 material" heading in `workflow.md` (lines 67-74) instead of under Decisions.
- Persistent style item: `HudContourLayout.cs:157-158` has no blank line before `return new Shape`. `HudContourGeometry.cs:41` uses a fully qualified `HudDockMode`.
- Persistent from review 01. These are not defects, because rendering is event-driven: `HudContourDecorator.ArrangeOverride` builds the layout twice when the request changes, and `OnRender` allocates a frozen Pen on each render.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_02/CR-01 (= codereview_01/CR-01) | resolved by human decision | Workflow DEC-05 (`workflow.md:67-73`) records the human's acceptance of the residual risk ("Aceitar como risco residual (Recomendado)") with reason, scope and provenance. `techspec.md:112` is annotated to match, and the TechSpec hash matches the checkpoint. This is the resolution path both previous reviews recommended. No disconnection evidence was produced: the FR-08 disconnection part remains not verifiable and is carried as residual risk for HIL 3. |
| codereview_02/CR-02 (remainder of codereview_01/CR-03) | resolved | `done/task_03.md:72-73` now record the validated state, the approved visual gate, the Anti Slop PASS and the open items. `tasks.md:89` marks T03 complete. `validation.md:5,41` record completion. The older "pending" paragraphs at `validation.md:88,118,130` are tagged "Superseded". `anti-slop-delivery-gate.md:78` records "Result: PASS", which closes workflow DEC-04. |
| codereview_01/CR-02 | resolved (unchanged since review 02) | `validation.md:43-51` |

## Limitations and open items

- Under the contract, the reviewer did not launch the app or interact with the desktop. All manual acceptance comes from the coordinator's recorded evidence and the human visual approval. This review did not reopen screenshots or native JSON files; review 02 inspected them on the same build.
- FR-08 preferred-display disconnection was never executed. It passes only through the residual-risk acceptance in workflow DEC-05, which HIL 3 should confirm. This is the only reason the status is not plain APPROVED.
- DEC-05 provenance is an AskUserQuestion answer. `workflow.md:35,113` record earlier sessions where tool questions did not render for this user. The reviewer takes the workflow record as the source and cannot verify the answer independently.
- The round-2 corrections after codereview_02 have no `task_NN.md` in `codereview_02/`. `sdd-plan-corrections` step 4 expects task files. Traceability rests on `workflow.md:79` and the changed records, which this review checked.
- Graceful tray Exit and tray hide/show were not re-driven at T03 (`validation.md:74`). The T02 evidence is reused because the lifecycle code is unchanged since T02.
- HUD sizes are a continuous 50..150 % range. FR-08 "every existing size selection" is treated as satisfied by samples at 50, 80, 100, 125 and 150 %.
- The busy pulse motion cannot be shown in still captures (`validation.md:48`). The pulse template is unchanged in the diff.
- `HudShadowWindow.xaml`, `HudContourFrame.cs`, `HudContourSegment.cs`, `HudContourPoint.cs` and the three test files were not reopened this round. They are byte-identical to the state analyzed in reviews 01 and 02 (same HEAD, no worktree delta), and the full suite passed.
- `HudContourController.Synchronize:97-103` hides the shadow when arrange is invalid and relies on a later `FrameChanged`, `LocationChanged`, `SizeChanged`, `DpiChanged` or visibility event to show it again. The desktop matrix shows no case where the shadow failed to return. No reproducible cause was established, so this is recorded as an observation, not a finding.
- The snapshot header is behind the stage source (round 1, `codereview_02` as the next step). Under the independent-stage filter, its Decisions and Code map were skipped. Open thread O-07 (pre-existing idle CPU) is outside this feature.

## Conclusion

The code under review is unchanged since codereview_01 and follows the approved TechSpec: a shared pure contour, per-pixel alpha pass-through, a passive `WS_EX_TRANSPARENT` shadow companion, checked native calls with log-and-hide failure handling, event-driven synchronization with unchanged-frame suppression, and no Core, schema or focus-invariant regression. Both Release builds are clean, the full Infrastructure suite passes (1,082/1,082), and the quality profile has no blocking hit and 1 reservation.

Round 2 resolved the records finding (codereview_02/CR-02) and recorded the Anti Slop gate PASS. The persistent evidence finding (CR-01) is resolved by the human's explicit residual-risk acceptance in workflow DEC-05. Disconnection evidence still does not exist, so it stays open for HIL 3 confirmation. With only reservations and records-precision improvements left, the status is APPROVED WITH RESERVATIONS.
