# Code review report — HUD Edge Geometry and Contour Hit Testing

## Summary

- Status: REJECTED
- Execution: delegated reviewer
- Git scope: `6f2e92b0f4cc40c5cde1326149f000768fe3a781..2bc32ef` (commit `2bc32ef`) plus uncommitted `ARCHITECTURE.md` and `docs/ROADMAP.md`; no uncommitted `src/` or `tests/` changes
- Previous review: `—`

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-16-hud-geometry-hit-testing/prd.md` | read |
| TechSpec | `tasks/prd-16-hud-geometry-hit-testing/techspec.md` | read |
| Manifest | `tasks/prd-16-hud-geometry-hit-testing/tasks.md` | read; T01..T03 links resolve to `done/` |
| Handoffs | `done/task_01.md`, `done/task_02.md`, `done/task_03.md` | read |
| Evidence | `validation.md`, `workflow.md`, `t03-native-*.json`, `t03-visual-gate-20261009.png` | read (sampled JSON) |
| Snapshot | `context-snapshot.md` | loaded with the independent-stage filter: header, next step brief, open threads, on-run entries. `git_head` 2bc32ef matches HEAD; `covers_through` is behind (T03 is now in `done/`), so the next step brief is stale |
| Implementation | `git diff 6f2e92b HEAD`: 15 C# files, 2 XAML files, test csproj, README, ROADMAP; worktree ARCHITECTURE/ROADMAP | delimited |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 / FR-04 / US-02 / PD-04 | Outside points, including shadow, reach a different process | `HudContourDecorator` (zero-alpha outside the envelope), `HudShadowWindow` + `HudShadowInterop.EnablePassive` (WS_EX_TRANSPARENT) | TC-04 manual | conformant | validation.md T02 table (all six modes, shadow points) and T03 table, lines 45-53; styles 0x080800A8 in `t03-native-display3-150-free-20261009.json` |
| FR-05 | Fill/stroke are interactive; shadow is not | `HudContourDecorator.HitTestCore:120-122`, `HudShadowWindow.xaml:7` | TC-04 manual | conformant | Inside right-click opened the menu in every mode, validation.md lines 47-53 |
| FR-01 / OBJ-02 / US-01 | Smooth mirrored inverse joins | `HudContourLayout.cs:166-266` | `HudContourLayoutTests` (Create_SidesShareTheTopCurveProfile, Create_InverseJoinHasKnownBoundaryAndTangents) | conformant | Full suite passed; collage `t03-visual-gate-20261009.png` |
| FR-02 / PD-03 | Top corners omit unavailable continuation, stay in the work area | `HudContourLayout.cs:136-156`, `NotchWindow.Placement.cs:131-132` (`PixelExtent`) | Create_TopCornersAreMirroredAndHaveOneJoin, PixelExtent_CoversTheWholeNativeWindow | conformant | Right=1920 after fix, validation.md line 43 |
| FR-03 / PD-02 | Free is horizontal and fully rounded | `HudContourLayout.cs:134-157`, `HudContourGeometry.cs:41` | Create_FreeHasNoJoinsOrUnstrokedEdges | conformant | Drag to Free, validation.md line 57 |
| FR-06 | Placement switch updates shape and hit area together | `NotchWindow.Dock.cs:26`, `HudContourDecorator.OnLayoutPropertyChanged:138-145` | TC-03/04 manual | conformant | Mode switches reused one companion with matching bounds, validation.md lines 68, 112 |
| FR-07 / US-03 | Drag to Free and restart restore | Existing `RecordDrag` unchanged | TC-05 manual | conformant | validation.md line 57 (restart PID 27116) |
| FR-08 / US-04 | Sizes, DPI, negative coordinates, preferred-display disconnection | `HudContourController.ContourTransform:152-169`, `HudContourTransform.ForDpi` | `HudContourTransformTests` (6 methods) | not verifiable (disconnection part) | Sizes 50/80/100/125/150, three DPIs and negative coordinates are evidenced (validation.md lines 56-58). The physical disconnection is not executed; see CR-01 |
| FR-09 | Placement schema unchanged | No change in `src/TokenHound.Infrastructure` or `src/TokenHound.Core` | Existing placement/settings regressions | conformant | `git diff --name-only 6f2e92b HEAD -- src/TokenHound.Core src/TokenHound.Infrastructure` is empty; `Mode=Free`, `Display=null` saved (line 57) |
| FR-10 / US-05 | Hover details, StatusPopup and menus unchanged and inward | `NotchWindow.xaml` keeps `CapsuleBorder` name, ContextMenu and StatusPopup `PlacementTarget` | TC-05 manual | not verifiable (StatusPopup part) | Hover card and context menus are evidenced (line 55). No StatusPopup (Provider Status) evidence on the integrated build; see CR-02 |
| FR-11 | Provider layout, badges, busy pulse, empty/minimum layout | `HudContourDecorator.MeasureOverride/ArrangeOverride:73-104` | Create_PreservesBodyAndClosedBounds, empty-content cases | not verifiable (desktop part) | Unit tests cover empty content. TechSpec step 3 desktop screenshots of the empty/minimum state, badges and busy pulse are not recorded; see CR-02 |
| NFR-01 | No focus theft; non-activation invariants | `NotchWindow.xaml.cs:106-114` unchanged; `HudShadowWindow.WndProc:62-71`; `HudShadowInterop.SWP_FLAGS` = NOZORDER/NOACTIVATE | `HudContourStyleTests`, `WindowStylesTests` | conformant | Keys reached the target after menus in every mode (lines 47-53) |
| NFR-02 | No polling, no global hook, no idle redraw | `HudContourController` event-driven, unchanged-frame suppression `:126-138` | QA-05 | conformant | No new timer/hook; idle log silent and CPU below baseline (line 59) |
| NFR-03 | Core and data boundaries | No Core/Infrastructure change | Scoped diff | conformant | Empty Core/Infrastructure diff |
| NFR-04 | Keyboard access and contrast preserved | No change to Settings/tray/dialogs | — | conformant | Diff scope |
| NFR-05 | Honest evidence, E2E omitted | validation.md | — | conformant | Coordinator and human evidence are separated; pending items are named |
| PD-01 | Existing palette, stroke and shadow | `NotchWindow.xaml` colors; `HudShadowWindow.xaml:12-13` shadow values | — | conformant | Human visual gate approved, workflow.md line 69 |
| TC-01..03 | Unit/regression tests | 3 new test files | Full Infrastructure run | conformant | 1,082/1,082 passed in this review |
| TC-04..06 | Manual matrix | — | — | partial | See CR-01, CR-02 and limitations |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity | OK | No Core change |
| WM_MOUSEACTIVATE -> MA_NOACTIVATE on HUD windows | OK | `NotchWindow.xaml.cs:109-114`, `HudShadowWindow.xaml.cs:65-70` |
| EnableNonActivating flags unchanged | OK | `WindowStyles.cs:37` unchanged; new helper uses `0x0010 | 0x0004` without SWP_SHOWWINDOW, `HudShadowInterop.cs:10` |
| One class per file, sealed, size limits | OK | Largest new file 290 lines (`HudContourLayout.cs`); methods under 30 lines |
| Blank line before `return` | NOT OK (minor) | `HudContourLayout.cs:157-158` |
| `using` order, file-scoped namespaces | OK | New files |
| XML docs on public members | OK | `HudContourDecorator`, `HudShadowWindow`, `WindowStyles` additions |
| dotnet-efficient-validation / MTP args after `--` | OK | Commands below |
| E2E policy | N/A | Omitted under the .NET desktop policy; not a defect and not approved testing |

## Quality profile

Scope: the 17 C# files from `git diff --name-only 6f2e92b HEAD -- '*.cs'`, plus the two XAML files and the test csproj for QA-03.

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Async/sync-over-async | blocking | `rtk rg -n 'async void\|\.Result\b\|\.Wait\(\|GetAwaiter\(\)\.GetResult\(' <files>` | 0 | OK |
| QA-02 | Native failures | blocking | `rtk rg -n 'catch\|SetWindowLong\|SetWindowPos' <files>` | 0 new of 13; `WindowStyles.cs:77-97` is the baseline debt | OK. New calls in `HudShadowInterop.cs:24-71` check return values and capture the last error; `HudContourController.cs:110-116` logs NativeErrorCode, hides the shadow and stops retrying |
| QA-03 | Suppressions | blocking | `rtk rg -n '#nullable disable\|#pragma warning disable\|NoWarn' <files>` | 0 | OK |
| QA-04 | Focus/composition/input scope | blocking | `rtk rg -n 'WM_MOUSEACTIVATE\|MA_NOACTIVATE\|SWP_\|WS_EX_TRANSPARENT\|SetLayeredWindowAttributes' <files>` | 0 new defects | OK. WS_EX_TRANSPARENT only in `ApplyShadowStyles`; `MainStyles_RemainInteractive` asserts its absence on the main HUD |
| QA-05 | Polling/hooks/disposal | blocking | `rtk rg -n 'DispatcherTimer\|LayoutUpdated\|CompositionTarget\|SetWindowsHookEx\|Closed\|Dispose\|\+=' <files>` | 0 new defects | OK. `DispatcherTimer` is the existing placement timer; every new `+=` has a matching `-=` (`HudContourController.cs:50-55`, `HudShadowWindow.xaml.cs:56-59`) |
| QA-06 | Size/complexity | reservation | `rtk rg -c '^' <files>` | 0 | OK. All files are 300 lines or fewer |
| QA-07 | Long parameter bundles | reservation | `rtk rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' <files>` | 1 new of 10 | `HudShadowInterop.cs:12` 4-member `Bounds` record (cohesive native rectangle): reservation. `HudShadowWindow.xaml.cs:62` WndProc is a prescribed Win32 signature (justified as in the baseline). Remaining hits are test InlineData, object initializers or 3-argument calls (false positives) |

- Terrain baseline: applied from the TechSpec, including the T03 addendum
- Hits discounted by baseline: 7 (`WindowStyles.cs` native calls 77-97, `NotchWindow.xaml.cs:106` callback signature)
- Reservations accumulated in the feature: 1
- Suggested escalation: `no trigger fired` (1 < 8 reservations; no touched file above 500 lines; no block repeated in 3+ locations)
- `git diff --check 6f2e92b HEAD`: clean

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 pure geometry in App/UI/Placement | YES | `HudContourLayout/Frame/Segment/Point/Transform.cs` have no WPF/Win32 references; linked in the test csproj |
| DEC-02 per-pixel alpha, no HTTRANSPARENT | YES | No WM_NCHITTEST handling added; `NotchWindow.xaml` no longer has a DropShadowEffect |
| DEC-03 passive WS_EX_TRANSPARENT companion with exclusion mask | YES | `HudShadowWindow.SetContour:26-42` excludes the transformed envelope from the window rectangle |
| DEC-04 event-driven sync, unchanged-frame suppression | YES | `HudContourController.Schedule:76-89`, `Synchronize:119-142` |
| DEC-05 keep CapsuleBorder name, menu, padding, popup anchor | YES | `NotchWindow.xaml` diff; `Dock.cs:26-27` |
| DEC-06 coordinate semantics; scale and DPI applied once | YES | `HudContourTransform.ForDpi`; companion bounds match the owner after DPI transitions (`t03-native-display3-150-free-20261009.json`) |
| DEC-07 reuse the test executable | YES | csproj source links only |
| DEC-08 new controller, not a NotchWindow partial | YES | `HudContourController.cs`; NotchWindow gains 2 lines |
| Lifecycle: hide with owner, close on destruction | YES | `Schedule:82-83`, `OnClosed:73-74 -> Dispose:42-59` |
| Errors: check return values, log, hide, no retry loop | YES | `HudShadowInterop`, `_failed` flag |
| Manual script steps 3, 5, 6 | PARTIAL | See CR-01, CR-02 and limitations |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Baseline recorded; 19 targeted tests at the time; current full suite passes |
| T02 | `done/task_02.md` | COMPLETE | Rendering/companion/native integration; six-mode click/focus, hide/show, graceful Exit evidence |
| T03 | `done/task_03.md` | COMPLETE with open item | All items checked; T03.4 carries "physical disconnection pending". The handoff "Produced result" still says "Awaiting the human visual gate" (CR-03) |

## Executed validations

- Profile and exclusions: .NET 10 desktop (WPF App plus net10.0 MTP executable, xunit.v3 with `UseMicrosoftTestingPlatformRunner=true`); E2E omitted by the .NET desktop policy.
- Validated state: HEAD `2bc32ef`, no uncommitted `src/` or `tests/` changes, SDK 10.0.401, Release.
- Reused evidence: desktop matrix from validation.md "T03 desktop acceptance, 2026-10-09". It was run on the `2bc32ef` build and production code has not changed since, so it is still valid. Graceful Exit and hide/show come from T02. Since T02, the controller only gained the `HudContourTransform` extraction; the lifecycle paths did not change.
- Manual acceptance: human visual gate approved (workflow.md line 69). The physical disconnection is not executed (CR-01). The reviewer did not launch the app or use the desktop, as the contract requires.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal` | passed, 0 warnings/errors, exit 0 | All production code |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` | passed, 0 warnings/errors, exit 0 | CMP-07 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1 --no-banner` | passed: 1,082 total, 1,082 succeeded, 0 failed, 0 skipped, exit 0 | TC-01, TC-02, TC-03, existing regressions |
| `git diff --check 6f2e92b HEAD` | clean | Repository hygiene |
| QA-01..07 commands above | see Quality profile | QA-01..07 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Medium | FR-08, US-04, TechSpec manual step 6 | `validation.md:61` — "Not covered by the coordinator: physical disconnection and reconnection of the preferred display". `techspec.md:112` says that "a resolver unit test does not close this desktop case". `workflow.md:70` records the human's choice "Aceitar sem esse teste" and defers it to HIL 3 | Essential manual evidence for FR-08 is missing. The review cannot treat it as passed. A human deferral is not a TechSpec `DEC-NN` | Get human disconnection/reconnection evidence, or have HIL 3 explicitly accept the gap as residual risk. No code change is indicated |
| CR-02 | Medium | FR-10, FR-11, TechSpec manual step 3 | `validation.md:55` and `t03-visual-gate-20261009.png` cover only the populated three-provider HUD, the hover card and the context menus. There is no record of the minimum/empty HUD state, the StatusPopup (Provider Status), badges, or the busy pulse on the integrated build | FR-10 (StatusPopup) and FR-11 (empty/minimum layout, badges, busy pulse) lack the desktop evidence that TechSpec step 3 requires. The unit tests cover empty-content geometry only | Record a screenshot and interaction check for the StatusPopup and the empty/minimum state (and badge/busy states if they can be reached) on the current build, or have HIL 3 accept the gap. No code defect is identified |
| CR-03 | Low | SDD state consistency | `workflow.md:48-55` gates table still shows "Visual check: Pending implementation" and "Independent review: Pending implementation", but `workflow.md:69` records the visual approval. `done/task_03.md:69` says "Awaiting the human visual gate", but `tasks.md:89` records it as approved. `context-snapshot.md` `covers_through` is behind | The SDD records contradict each other. A later session can read the wrong gate state | Update the gates table, the T03 handoff summary and the snapshot from the recorded events (coordinator action) |

### Optional improvements

- QA-07 reservation: `HudShadowInterop.cs:12` `Bounds(int Left, int Top, int Width, int Height)`. It is a cohesive native rectangle, so keeping it is acceptable.
- Style: `HudContourLayout.cs:157-158` has no blank line before `return new Shape` (repository rule: blank line before control-flow statements).
- `HudContourDecorator.ArrangeOverride:88-93` calls `HudContourLayout.Create` twice when the request changes (once in `UpdateContour`, once for child arrange). `OnRender:114-115` allocates a new frozen Pen on each render; `HudContourGeometry.StrokePen` could be reused. Neither is a defect, because rendering is event-driven only.

## Limitations and open items

- The reviewer did not launch the app or interact with the desktop, by contract. All manual acceptance comes from the coordinator's recorded evidence and the human visual approval. The reviewer checked a sample of native JSON (`t03-native-display3-150-free-20261009.json`) and the collage, but did not re-measure on screen.
- Graceful tray Exit and tray hide/show were not re-driven at T03 (`validation.md:60`). This review reuses T02 evidence because the lifecycle code paths are unchanged since T02.
- HUD sizes are a 50..150% range in 5% steps (`HudSizeSettings.cs:14-24`). The evidence samples 50/80/100/125/150, including the minimum, default and maximum. FR-08's "every existing size selection" is treated as satisfied by representative sampling.
- Snapshot entries skipped under the independent-stage filter: Decisions and Code map. Open thread O-07 (pre-existing idle CPU) is outside this feature.
- ContextBrake asked for a session handoff in `.context-brake/`. It was not written, because the delegated-reviewer contract allows writing only this report.

## Conclusion

The implementation follows the approved TechSpec: shared pure contour, per-pixel alpha pass-through, a passive WS_EX_TRANSPARENT shadow companion, checked native calls, event-driven synchronization, and no Core, schema or focus-invariant regression. Builds are clean, the full Infrastructure suite passes (1,082/1,082), and the quality profile has no blocking hit and 1 reservation. No code defect was found. The status is REJECTED only because essential manual evidence that the TechSpec requires is missing: physical preferred-display disconnection (CR-01) and the StatusPopup and empty/minimum-state desktop checks (CR-02). There is also a Low state inconsistency in the SDD records (CR-03). These can be resolved with evidence or an explicit HIL 3 acceptance, not with code changes.
