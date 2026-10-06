# Code review report — HUD multi-monitor and edge docking

## Summary

- Status: APPROVED WITH RESERVATIONS
- Execution: delegated reviewer
- Git scope: `c4b55b9cb1b372f819f59639e6836388e78ebed5..worktree` (HEAD = base; the whole feature is uncommitted: 15 modified and 21 new files under `src/` and `tests/`, plus the feature folder)
- Previous review: `—`

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-14-hud-multimonitor-docking/prd.md` | read (approved, DEC-03) |
| TechSpec | `tasks/prd-14-hud-multimonitor-docking/techspec.md` | read (approved, DEC-04) |
| Manifest | `tasks/prd-14-hud-multimonitor-docking/tasks.md` | read; links to `done/task_01.md`..`done/task_07.md` all resolve; State T01..T07 `[x]` |
| Handoffs | `done/task_01.md`..`done/task_07.md` | read; every work item `[x]`, handoff filled |
| Workflow | `workflow.md` (DEC-01..DEC-05, Events) | read |
| Snapshot | `context-snapshot.md` | loaded through the independent-stage filter (header, next step brief, open threads, `on-run` L-02..L-05); `Decisions` and `Code map` skipped. Header: `covers_through: T07` matches the manifest; `git_head c4b55b9` matches `git rev-parse HEAD`; worktree matches. Next step brief and O-01 are stale: the visual check was approved afterwards (`workflow.md` DEC-05) |
| Implementation | `git diff c4b55b9 -- src tests` + untracked files under `src/`, `tests/` | delimited |

Excluded from the reviewable set (pre-existing, not owned by this flow per `workflow.md` Feature Context): `.agents/settings.json`, `.agents/hooks/`, `.agents/settings.local.json`, `.context-brake/`, `.opencode/`, `context-brake.config.json`. `tasks/triage-log.jsonl` has one line appended by this flow (no code).

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Six modes, applied immediately | `HudDockMode.cs`; `HudPlacementSettingsViewModel.MODES`/`SelectedMode` (`:29-37`, `:80-94`); `NotchWindow.Placement.cs:OnPlacementChanged` | `HudPlacementSettingsViewModelTests.SelectingMode_AppliesOnceAndEnablesDisplaySelector` | conformant | Service `Changed` → `ApplyChrome` + `ApplyPlacement`, no restart; MA-2, MA-5 (T05/T07 handoffs) |
| FR-02 | Top docks flush, aligned ≤ 1 px | `NotchPlacement.Dock` (`NotchPlacement.cs:60-83`); `ApplyDockedPlacement` (`NotchWindow.Placement.cs:115-146`); gutter `NotchWindow.Dock.cs:47-55` | `NotchPlacementTests.Dock_TopModes_*`, `Dock_TopCenter_*` | conformant | Whole-pixel size (`Math.Round(Actual* × DPI)`), MA-2 (capsule 813..1107 on 1920) |
| FR-03 | Side docks vertical, centered, same order | `Dock` side branch; `HudDockLayout.Orientation`; bound `ItemsPanel` orientation | `Dock_SideModes_AreFlushAndVerticallyCentered`, `HudEdgeLayoutTests` | conformant | MA-2 side edges (T06: 393..686 on 1080) |
| FR-04 | Outline follows edge | `NotchWindow.Dock.cs:CornersFor`/`BorderFor` (`:57-71`) | `HudEdgeLayoutTests` (mapping); visual | conformant | Top/Free: square top, rounded bottom; Right: rounded left; Left: rounded right; MA-2 |
| FR-05 | Tooltip/popup inward, not clipped | `HudDockLayout.TooltipPlacement` (`:52-58`), `ProviderRing.xaml:25`, `ApplyStatusPopupPlacement` (`NotchWindow.Dock.cs:35-44`) | manual | conformant | MA-3 (T06 handoff) |
| FR-06 | Primary or specific display, labeled list | `DisplayCatalog.GetDisplays`; `HudPlacementSettingsViewModel.BuildDisplayOptions`/`FormatDisplay` | `Displays_ListPrimaryOptionThenEachConnectedDisplay`, `SelectingDisplay_AppliesOnce`, `DisplayResolverTests` | conformant | MA-5 on three displays (T07 handoff) |
| FR-07 | Primary follows Windows primary ≤ 2 s | `DisplayResolver.Resolve` (null pref → live primary); `WM_DISPLAYCHANGE` → 300 ms timer (`NotchWindow.xaml.cs:115-116`) | `Resolve_WhenPreferenceIsNull_ReturnsPrimary` | conformant | Human visual check, DEC-05 |
| FR-08 | Disconnected preference falls back, kept, returns | `Resolve` fallback; VM disconnected option (`HudPlacementSettingsViewModel.cs:167-170`) | `Resolve_WhenPreferredDisplayIsDisconnected_FallsBackToPrimary`, `Displays_WhenPreferredDisplayIsDisconnected_AddsAndSelectsItOnce` | conformant | MA-6 by the human, DEC-05 |
| FR-09 | Same monitor recognized again | `DisplayCatalog.Targets.cs` (device path + EDID key); `DisplayResolver.FindMatch` | `Resolve_WhenDevicePathMatches_*`, `Resolve_WhenDevicePathChangedButEdidIsUnique_*`, `Resolve_WhenEdidMatchIsAmbiguous_*` | conformant | Restart identity covered by MA-5/MA-6 sessions; port swap of identical monitors is the accepted DEC-05 limitation |
| FR-10 | Drag → Free, stored | `OnMouseLeftButtonDown` (`NotchWindow.xaml.cs`, DEC-10 moved check); `HudPlacementService.RecordDrag` | `RecordDrag_SwitchesToFreeAndKeepsDisplay` | conformant | MA-4 (click kept `RightEdge`; drag stored Free; restart kept it) |
| FR-11 | Free keeps `c4b55b9` clamp | `ApplyFreePlacement` → `ApplyMonitorPlacement` + `UpdateFreePosition` | existing `NotchPlacementTests.Clamp*`, `UpdateFreePosition_KeepsLegacyModeUnset` | conformant | Clamp path unchanged in substance |
| FR-12 | Display selector disabled in Free, hint | `IsDisplayEnabled`/`DisplayHint`; `SettingsWindow.xaml` `IsEnabled` binding | `FreeMode_DisablesDisplaySelectorWithHint` | conformant | MA-8 |
| FR-13 | Re-anchor on work area, resolution, DPI, size, provider count | `WM_SETTINGCHANGE`+`SPI_SETWORKAREA`, `WM_DISPLAYCHANGE` → timer; `SizeChanged` → `ApplyPlacement` | manual | conformant | MA-7 (cross-scale by agent; taskbar + 125% size by the human, DEC-05) |
| FR-14 | Fresh install → Top Center | `ResolveMode` (`HudPositionSettings.cs:54-55`) | `ResolveMode_WhenNothingIsStored_ReturnsTopCenter` | conformant | MA-1 |
| FR-15 | `Left`/`Top`-only → Free | `ResolveMode` migration, no write on load | `ResolveMode_WhenOnlyCoordinatesAreStored_MigratesToFree`, `Load_WhenFileHasOnlyCoordinates_DoesNotRewriteFile` | conformant | MA-8 |
| FR-16 | Unknown value → Top Center + one warning naming the field | `ResolveMode` warning uses `nameof(Mode)`; `LogModeWarning` once (`NotchWindow.Placement.cs:165-173`) | `HudPositionSettingsTests` theories, `Load_WhenModeIsUnknown_KeepsStoredCoordinates` | conformant | Warning text contains `Mode` |
| FR-17 | Context menu "Position" submenu | `NotchWindow.xaml` submenu; `NotchWindow.Menu.cs` | manual | conformant | MA-2 (current item checked) |
| FR-18 | Free → docked keeps hosting display | `HudPlacementService.PreferHosting` (`:111-122`); VM `SyncDisplayWithService` | `SelectMode_FromFreeOnNonPreferredDisplay_PrefersHostingDisplay`, `SelectMode_FromFreeOnPrimaryUnderPrimaryPreference_KeepsFollowingPrimary`, `SelectingMode_FromFreeOnOtherDisplay_SelectsHostingDisplay` | conformant | Compares against the resolved preference, as DEC-09 specifies |
| NFR-01 | No activation; invariants kept | `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` still first in `WndProc`; `MoveWindowTo` uses `SWP_NOSIZE\|SWP_NOZORDER\|SWP_NOACTIVATE` (`WindowPlacement.cs:106`) | QA-04 grep | conformant | QA-04 empty on the diff; visual checks reported no focus change |
| NFR-02 | Mixed DPI flush, no oscillation | Physical-pixel docking (DEC-03); skip when already at target (`NotchWindow.Placement.cs:135`) | manual | conformant | MA-7 cross-scale 150% → 100% (T07) + human DEC-05 |
| NFR-03 | ≤ 1 write per display change, no loop | Docked passes never call the service; `_applyingPlacement` guard; coalescing timer | `SelectMode_WhenAlreadyStored_DoesNotSaveOrNotify` (counting save) | conformant | `ApplyDockedPlacement` has no save path |
| NFR-04 | Forward/backward settings compatibility | String `Mode` + resolver (DEC-01); `with` writes | `Save_ThenLoad_RoundTripsModeAndDisplay`, `Save_WithMode_PreservesHudSizeSection` | conformant | QA-07: no production `new HudPositionSettings` |
| NFR-05 | Rules unit-tested without a display | Pure files linked in test csproj | 81 tests in feature classes | conformant | Filtered run below |
| NFR-06 | Windows 11, existing app and targets | No project/target changes | App rebuild | conformant | 0 warnings, 0 errors |
| TC-01..TC-07 | Unit scenarios | as above | 81 passed | conformant | Filtered run |
| TC-08, TC-09 | Manual MA-1..MA-8 | — | manual | conformant | Handoffs T05..T07 + DEC-05; code unchanged since (last source mtime 20:46, before the visual check) |
| TC-10 | Focus + QA-04 | — | manual + grep | conformant | QA-04 empty; DEC-05 |
| TC-11 | Docked re-anchor never saves | — | unit + code | conformant | See NFR-03 |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `TokenHound.Core` pure | N/A | Core not touched |
| `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` on HUD | OK | `NotchWindow.xaml.cs:104-116` unchanged ordering |
| `SWP_NOZORDER`, no `SWP_SHOWWINDOW` | OK | `WindowPlacement.cs:16-18,106`; QA-04 empty |
| One class per file, sealed by default | OK | New types sealed; `HudPlacementSettingsViewModel` nested records only |
| File ≤ 300 / method ≤ 30 lines | NOT OK (reservation) | See QA-06 |
| XML docs on public members | OK | All new public members documented |
| File-scoped namespaces, alphabetized usings | OK | New files |
| ≥ 4 arguments split across lines | OK | New 4+ calls split (`Dock`, `SetWindowPos`, logs); remaining hits are declarations, see QA-05 |
| `string.Equals(..., OrdinalIgnoreCase)` | OK | `DisplayResolver.cs:87,97`, `HudPlacementService.cs:77,119` |
| Records with `init`/`required` for DTOs | OK | `DisplayInfo`, `HudDisplayPreference`, `HudPlacementContext` |
| Structured logging | OK | Typed placeholders throughout |
| `dotnet-efficient-validation` | OK | MTP native route (`global.json` `test.runner`), `--minimum-expected-tests 1` |
| `repository-cli-efficiency` | OK | Stat-first diff, scoped greps |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Empty/swallowed catch | blocking | `rg -n 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` (+ multiline variant) | 0 | OK |
| QA-02 | `#pragma warning disable` / `#nullable disable` | blocking | `rg -n '#nullable disable\|#pragma warning disable' $files` | 0 | OK |
| QA-03 | `async void` / sync-over-async | blocking | `rg -n 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 | OK |
| QA-04 | Activation / z-order invariants | blocking | `rg -n 'SWP_SHOWWINDOW\|0x0040\|Activate\(\)' $files` | 0 | OK |
| QA-05 | 4+ parameter list unsplit | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 1 new of 13 | 1 new: `DisplayCatalog.cs:177` `MonitorEntry` positional record with 4 parameters on one line. Justified: `DisplayCatalog.Native.cs:17,21` (P/Invoke), `NotchWindow.xaml.cs:104` (`WndProc`, baseline). Pre-existing: `App.xaml.cs:390`. False positives (commas in collection expressions, initializers, JSON literal, generic type): `DisplayCatalog.cs:101`, `DisplayResolverTests.cs:56,80`, `HudPositionStoreTests.cs:241`, `HudPlacementServiceTests.cs:77,79,122` |
| QA-06 | File > 300 / method > 30 lines | reservation | `rg -c '^' $files` + method-length scan | 1 new, 2 aggravated | New: `DisplayCatalog.Targets.cs:10` `QueryTargets` 39 lines. Aggravated pre-existing: `App.xaml.cs` 468 → 481 lines; `App.xaml.cs:381` `InitializeUi` 35 → 38 lines. Pre-existing unchanged: `App.xaml.cs` `OnStartup`, `RegisterClaude`, `InitializeTray`; `WindowPlacement.cs` `GetWorkArea`, `PositionInWorkArea`. `NotchWindow.Placement.cs:115` `ApplyDockedPlacement` has a 30-line body (at the limit) |
| QA-07 | `new HudPositionSettings` persisted | blocking | `rg -n 'new HudPositionSettings' $files` | 0 in production | OK (all 30 hits in tests, excepted by the TechSpec) |

- Terrain baseline: applied from TechSpec (measured at `c4b55b9`); `DisplayCatalog.*` and other new files are unmeasured (new, so every hit counts as new).
- Hits discounted by baseline: 7 (QA-05 `App.xaml.cs:390`, `NotchWindow.xaml.cs:104`; QA-06 `App.xaml.cs` `OnStartup`, `RegisterClaude`, `InitializeTray`, `WindowPlacement.cs` `GetWorkArea`, `PositionInWorkArea`).
- Reservations accumulated in the feature: 4 (QA-05 `MonitorEntry`; QA-06 `QueryTargets`, `App.xaml.cs` file size, `InitializeUi`).
- Suggested escalation: the TechSpec trigger "a touched file above 500 lines" is met literally by `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (949 lines at baseline, 1051 now, +102 for one card). The file was already above 500 before this feature (the generic profile wording "crossed 500 lines" does not fire). Suggestion for the HIL, not executed: `sdd-plan-refactoring` to split the Settings window into section controls. Reservations (4) are below the 8-hit trigger; no block duplicated in 3+ places was found.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 string `Mode` + `Display`, `Left`/`Top` kept | YES | `HudPositionSettings.cs:13-28` |
| DEC-02 resolution table | YES | `HudPositionSettings.cs:49-64`; name-based parse rejects numerics |
| DEC-03 physical-pixel docking | YES | `GetMonitorInfo` work area + `SetWindowPos`; size from `ActualWidth × DPI` because `GetWindowRect` is stale during `SizeChanged` (documented in `tasks.md` Problems) |
| DEC-04 Free keeps DIP clamp | YES | `ApplyMonitorPlacement` |
| DEC-05 identity match order | YES | `DisplayResolver.FindMatch` |
| DEC-06 enumeration + fallback names | YES | `DisplayCatalog.cs:27-44,110-112`; `Targets.cs` failure logs |
| DEC-07 triggers + 300 ms coalescing | YES | `NotchWindow.Placement.cs:16,58-77`; `NotchWindow.xaml.cs:115` |
| DEC-08 single owner, `with` writes | YES | `HudPlacementService` |
| DEC-09 hosting display rule | YES | `PreferHosting` compares with the resolved preference |
| DEC-10 drag only when moved | YES | `NotchWindow.xaml.cs` `OnMouseLeftButtonDown` |
| DEC-11 pure edge layout + shared observable | YES | `HudEdgeLayout`, `HudDockLayout` |
| DEC-12 gutter per edge | YES | `NotchWindow.Dock.cs:47-55` matches the table |
| DEC-13 live apply, list built on open | YES | VM setters; `Create(getDisplays)` |
| DEC-14 tooltip/popup placement | YES | `HudDockLayout.TooltipPlacement`; `ApplyStatusPopupPlacement` |
| Contract `Hud` section | YES | Store round-trip tests |
| CMP-08 file split | PARTIAL (accepted) | Extra partial `DisplayCatalog.Targets.cs` and `HudPlacementContext.cs` (keeps `SelectMode` at 2 parameters); additive, recorded in T03/T04 handoffs |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Contract + resolver; 27 tests in `*HudPosition*` |
| T02 | `done/task_02.md` | COMPLETE | Pure rules + 5 links; `Dock`, resolver, edge tests |
| T03 | `done/task_03.md` | COMPLETE | Service + context record; 10 tests |
| T04 | `done/task_04.md` | COMPLETE | Catalog + pixel helpers; startup log with 3 displays, SystemAware noted |
| T05 | `done/task_05.md` | COMPLETE | Engine, submenu, App wiring; MA-1, MA-2, MA-4 |
| T06 | `done/task_06.md` | COMPLETE | Edge chrome; MA-2 sides, MA-3, MA-1 regression |
| T07 | `done/task_07.md` | COMPLETE | Settings section; 8 VM tests; MA-5, MA-8, part of MA-7 |

## Executed validations

- Profile and exclusions: .NET 10 WPF desktop; E2E omitted by .NET desktop policy (not a defect, not approved testing). Runner: native MTP via `dotnet test` (`global.json` `test.runner: Microsoft.Testing.Platform`, SDK 10.0.401).
- Validated state: worktree at `c4b55b9` + uncommitted feature diff, Debug configuration; only the installed `D:\Apps\TokenHound\TokenHound.App.exe` (PID 28760) was running and was not touched; no dev HUD was running, so the dev `bin/` was not locked.
- Reused evidence: manual acceptance MA-1..MA-5, MA-8, cross-scale MA-7 (handoffs T05..T07) and MA-6, rest of MA-7, FR-07 (human, `workflow.md` DEC-05). Still valid: no reviewable source file changed after 20:46, and the visual check came after that.
- Manual acceptance: not re-executed by this reviewer (the contract forbids launching the app).

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --no-incremental` | passed, 0 warnings, 0 errors | NFR-06, compile of all CMPs |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --no-incremental` | passed, 0 warnings, 0 errors | NFR-05 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed, 1043 tests, exit 0 | Full regression, FR-11 |
| same with `--filter-class` for `HudPositionSettingsTests`, `HudPositionStoreTests`, `DisplayResolverTests`, `HudEdgeLayoutTests`, `NotchPlacementTests`, `HudPlacementServiceTests`, `HudPlacementSettingsViewModelTests` | passed, 81 tests, exit 0 | TC-01..TC-07, TC-11 |
| QA-01..QA-07 greps over the 36 reviewable files | see Quality profile | QA-01..QA-07, TC-10 |

## Findings

No actionable findings: no non-conformant obligation, failing test, or blocking profile hit.

Optional improvements (reservations, not blocking):

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Low | QA-06 | `src/TokenHound.App/Interop/DisplayCatalog.Targets.cs:10` — `QueryTargets` is 39 lines (limit 30) | Maintainability only | Extract the `QueryDisplayConfig` buffer query (lines ~24-44) into a helper returning the path array or `null` |
| CR-02 | Low | QA-06 | `src/TokenHound.App/App.xaml.cs` 468 → 481 lines; `InitializeUi` (`:381`) 35 → 38 lines | Grows a file already over the limit (TechSpec judged it "no contact") | Optional: move the placement/Settings wiring into a factory method when `App.xaml.cs` is next refactored |
| CR-03 | Low | QA-05 | `src/TokenHound.App/Interop/DisplayCatalog.cs:177` — `MonitorEntry(string Device, RECT Work, RECT Bounds, bool IsPrimary)` on one line | Style only (private positional record) | Split the parameter list one per line, or use an `init`-property record |

## Previous findings (re-review only)

Not applicable (first review).

## Limitations and open items

- Manual acceptance (TC-08, TC-09, TC-10) was not re-executed by this reviewer: the contract forbids launching the app. States rely on handoff evidence (T05..T07) and the human approval DEC-05, valid for the current code (no source change after it).
- Escalation trigger "a touched file above 500 lines" is met literally by `SettingsWindow.xaml` (pre-existing 949 → 1051). Recorded as a suggestion for the HIL (`sdd-plan-refactoring`), not as a finding.
- FR-18 is checked against the resolved preferred display (DEC-09). When the stored specific display is disconnected and the HUD is dragged onto the primary, leaving Free keeps the disconnected preference, so the HUD returns to it on reconnection. The PRD wording ("not the stored preference") could also be read as switching the preference to the primary. The TechSpec decided this case and HIL 2 approved it, so it is not a finding; the PRD owner may want to confirm it.
- Snapshot `context-snapshot.md` next step brief and O-01 are stale (visual check already approved, DEC-05). O-02 (restore or keep the user's settings; backup in the authoring session's scratchpad) stays open for HIL 3. The reviewer did not update the snapshot or `workflow.md` (contract).
- Documented product limitations, accepted by the TechSpec: identical monitors swapped between ports fall back to the primary (DEC-05); a display plugged in while Settings is open appears the next time Settings opens (DEC-13).

## Conclusion

All 18 functional and 6 non-functional obligations are conformant. Evidence comes from 81 feature unit tests (1043 in the full suite, all passing), clean non-incremental builds with 0 warnings, and manual acceptance that is still valid for this code (agent MA runs plus the human visual check, DEC-05). All seven tasks are complete with consistent links and states. The blocking quality rules QA-01..QA-04 and QA-07 have no hits. The HUD non-activating invariants are intact. Four reservation hits remain as optional improvements (CR-01..CR-03), and one literal escalation trigger is listed for the HIL. Status: APPROVED WITH RESERVATIONS.
