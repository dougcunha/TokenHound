# Code review report — HUD multi-monitor and edge docking (re-review)

## Summary

- Status: APPROVED
- Execution: delegated reviewer
- Git scope: `c4b55b9cb1b372f819f59639e6836388e78ebed5..worktree`. HEAD equals the base, so the whole feature is uncommitted: 15 modified and 22 new files under `src/` and `tests/` (37 reviewable files), plus the feature folder.
- Previous review: `tasks/prd-14-hud-multimonitor-docking/codereview_01/codereview.md` (APPROVED WITH RESERVATIONS, CR-01..CR-03)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-14-hud-multimonitor-docking/prd.md` | read; sha256 `ac90b343d87e…` matches DEC-03 |
| TechSpec | `tasks/prd-14-hud-multimonitor-docking/techspec.md` | read; sha256 `132e117bcb2b…` matches DEC-04 |
| Manifest | `tasks/prd-14-hud-multimonitor-docking/tasks.md` | read. Links to `done/task_01.md`..`done/task_07.md` resolve. State T01..T07 is `[x]`. Its hash differs from DEC-04 because of the State and Problems updates made during execution |
| Handoffs | `done/task_01.md`..`done/task_07.md` | read. No unchecked work item; every handoff is filled. Hashes differ from DEC-04 because handoffs were filled in; the content delta could not be checked against the approved versions |
| Corrections | `codereview_01/done/task_08.md`..`task_10.md` | read. Every work item is `[x]` and every handoff is filled. There is no correction manifest, which matches `sdd-plan-corrections` step 4 |
| Workflow | `workflow.md` (DEC-01..DEC-06, Events) | read. DEC-06 authorizes correction round 1 |
| Checkpoint | `checkpoint.json` | read. `phase: review`, `active_work` reviewer `codereview_02` |
| Snapshot | `context-snapshot.md` | Loaded through the independent-stage filter: header, next step brief, open threads, and `on-run` L-02..L-05. `Decisions` and `Code map` were skipped. `git_head c4b55b9` matches HEAD. `covers_through: T07` is behind the corrections in `codereview_01/done/`, so the next step brief and O-03 (reservations HIL) are stale. O-02 (user settings restore) stays open for HIL 3 |
| Implementation | `git diff c4b55b9 -- src tests` + untracked files under `src/`, `tests/` | delimited |

Excluded from the reviewable set because they pre-date this flow and it does not own them (`workflow.md` Feature Context): `.agents/settings.json`, `.agents/hooks/`, `.agents/settings.local.json`, `.context-brake/`, `.opencode/`, `context-brake.config.json`. `tasks/triage-log.jsonl` has one appended line and no code.

Change since `codereview_01`: by modification time, only `src/TokenHound.App/Interop/DisplayCatalog.Targets.cs`, `DisplayCatalog.cs`, `src/TokenHound.App/App.xaml.cs`, and the new `src/TokenHound.App/App.Hud.cs` changed (2026-10-06 09:15-09:16). Every other reviewable file was last written at or before 2026-10-05 20:46, which is before the visual check (DEC-05) and the previous review.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Six modes, applied immediately | `HudDockMode.cs`; `HudPlacementSettingsViewModel` setters (`:90`, `:107`); `NotchWindow.Placement.cs` `OnPlacementChanged` | `HudPlacementSettingsViewModelTests` | conformant | One service call per selection. `Changed` leads to chrome + placement. MA-2, MA-5 |
| FR-02 | Top docks flush, aligned ≤ 1 px | `NotchPlacement.Dock` (`NotchPlacement.cs:72-82`); size rounded to whole px (`NotchWindow.Placement.cs:131-132`) | `NotchPlacementTests.Dock_*` | conformant | MA-2 (T05) |
| FR-03 | Side docks vertical and centered | `Dock` side branch (`:78-80`); `HudDockLayout` orientation | `Dock_SideModes_*`, `HudEdgeLayoutTests` | conformant | MA-2 side edges (T06) |
| FR-04 | Outline follows the edge | `NotchWindow.Dock.cs:50-54` (gutter), `:60-62` (corners) | `HudEdgeLayoutTests` + visual | conformant | Matches the DEC-12 table; MA-2 |
| FR-05 | Tooltip and popup face inward | `ProviderRing.xaml:25`; `HudDockLayout.cs:53-58`; `NotchWindow.Dock.cs:38-43` | manual | conformant | MA-3 (T06) |
| FR-06 | Primary or specific display, labeled list | `DisplayCatalog.GetDisplays`; VM `BuildDisplayOptions`/`FormatDisplay` | `DisplayResolverTests`, VM tests | conformant | MA-5 (T07) |
| FR-07 | Primary follows the Windows primary | `DisplayResolver.Resolve` (null preference resolves to the live primary); `WM_DISPLAYCHANGE` restarts a 300 ms timer | `Resolve_WhenPreferenceIsNull_ReturnsPrimary` | conformant | Human check, DEC-05 |
| FR-08 | Disconnected preference falls back, is kept, and returns | `Resolve` fallback; VM disconnected option (`:167-170`) | resolver + VM tests | conformant | MA-6, DEC-05 |
| FR-09 | Same monitor recognized again | `DisplayCatalog.Targets.cs` (device path + EDID key); `DisplayResolver` match order | `DisplayResolverTests` | conformant | The ambiguous port swap is the accepted DEC-05 limitation |
| FR-10 | Drag switches to Free and is stored | `NotchWindow.xaml.cs:143-149` (`RecordDrag` only when moved); `HudPlacementService.RecordDrag` | `RecordDrag_SwitchesToFreeAndKeepsDisplay` | conformant | MA-4 |
| FR-11 | Free keeps the `c4b55b9` clamp | `ApplyFreePlacement` → `ApplyMonitorPlacement` + `UpdateFreePosition` (`NotchWindow.Placement.cs:112`) | existing `NotchPlacementTests` | conformant | Full suite passed |
| FR-12 | Display selector disabled in Free, with hint | VM `IsDisplayEnabled`/`DisplayHint` (`:113-118`) | `FreeMode_DisablesDisplaySelectorWithHint` | conformant | MA-8 |
| FR-13 | Re-anchor on work area, resolution, DPI, size, provider count | `WndProc` (`NotchWindow.xaml.cs:115`) → timer; `SizeChanged` → `ApplyPlacement` (`:131-135`) | manual | conformant | MA-7 (T07 cross-scale; DEC-05 taskbar + 125%). The DPI caveat is under limitations |
| FR-14 | Fresh install → Top Center | `HudPositionSettings.ResolveMode` (`:54-55`) | `HudPositionSettingsTests` | conformant | MA-1 |
| FR-15 | `Left`/`Top`-only file → Free | `ResolveMode` migration; no write on load | `ResolveMode_WhenOnlyCoordinatesAreStored_MigratesToFree`, `Load_WhenFileHasOnlyCoordinates_DoesNotRewriteFile` | conformant | MA-8 |
| FR-16 | Unknown value → Top Center + one warning naming the field | `ResolveMode` (`:57-61`, `OrdinalIgnoreCase` `:72`); logged once (`NotchWindow.Placement.cs:165-172`) | theories + `Load_WhenModeIsUnknown_KeepsStoredCoordinates` | conformant | — |
| FR-17 | Context-menu "Position" submenu | `NotchWindow.xaml` submenu; `NotchWindow.Menu.cs` | manual | conformant | MA-2 (current item checked) |
| FR-18 | Free → docked keeps the hosting display | `HudPlacementService.PreferHosting` (`:111-122`), used only when leaving Free (`:80`) | `SelectMode_FromFree*` tests, VM `SelectingMode_FromFreeOnOtherDisplay_SelectsHostingDisplay` | conformant | Compared against the resolved preference, as DEC-09 specifies |
| NFR-01 | No activation; invariants kept | `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` is the first branch (`NotchWindow.xaml.cs:107-113`); `MoveWindowTo` uses `SWP_NOSIZE\|SWP_NOZORDER\|SWP_NOACTIVATE` (`WindowPlacement.cs:106`) | QA-04 | conformant | QA-04 has 0 hits |
| NFR-02 | Mixed DPI flush, no oscillation | Physical-pixel docking (DEC-03); skips the move when already at target (`NotchWindow.Placement.cs:135-136`) | manual | conformant | MA-7 + DEC-05 under the SystemAware runtime (see limitations) |
| NFR-03 | ≤ 1 write per display change, no loop | `ApplyDockedPlacement` (`:115-146`) never calls the service; `_applyingPlacement` guard; coalescing timer | `SelectMode_WhenAlreadyStored_DoesNotSaveOrNotify` | conformant | Every service write uses `Current with {…}` (`HudPlacementService.cs:75,82,91,100,109`) |
| NFR-04 | Forward and backward settings compatibility | String `Mode` + resolver (DEC-01) | `HudPositionStoreTests` round-trip and `HudSize` preservation | conformant | QA-07: 0 production hits |
| NFR-05 | Rules unit-tested without a display | Pure files linked into the test project | 81 feature tests | conformant | Filtered run below |
| NFR-06 | Windows 11, existing app and targets | No project or target changes | App build | conformant | 0 warnings, 0 errors |
| TC-01..TC-07, TC-11 | Unit scenarios | as above | 81 passed | conformant | Filtered run |
| TC-08, TC-09, TC-10 | Manual MA-1..MA-8, focus | — | manual | conformant | Handoffs T05..T07 + DEC-05. Those files have not changed since; see limitations for the startup wiring moved by T09 |
| codereview_01/CR-01..CR-03 | Correction tasks T08..T10 | see Previous findings | build + suite | conformant | Previous findings table |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `TokenHound.Core` pure | N/A | Core not touched |
| `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` on HUD | OK | `NotchWindow.xaml.cs:107-113`, constant 3 at `:19` |
| `SWP_NOZORDER`, no `SWP_SHOWWINDOW` | OK | `WindowPlacement.cs:15-18,106`; QA-04 has 0 hits |
| One class per file, sealed by default | OK | New types are sealed. `App.Hud.cs` is a partial of the unsealed baseline `App`, matching the `App.Mcp.cs` pattern |
| File ≤ 300 / method ≤ 30 lines | OK for the feature | Only pre-existing hits remain; see QA-06 |
| XML docs on public members | OK | `App.Hud.cs` has a summary on the partial and only private members |
| File-scoped namespaces, alphabetized usings | OK | `App.Hud.cs:1-8`, `DisplayCatalog*.cs` |
| ≥ 4 arguments split across lines | OK | `QueryDisplayConfig` call split (`DisplayCatalog.Targets.cs:40-47`); `MonitorEntry` split (`DisplayCatalog.cs:177-182`) |
| Blank-line rules | OK | Corrected files follow the repository's blank line after `{` and before control flow |
| `string.Equals(..., OrdinalIgnoreCase)` | OK | `DisplayResolver`, `HudPlacementService`, VM |
| Structured logging | OK | `{Status}` placeholders kept in both display-config warnings |
| `dotnet-efficient-validation` | OK | Native MTP (`global.json` `test.runner`, SDK 10.0.401); `--minimum-expected-tests 1`; exit codes checked |
| `repository-cli-efficiency` | OK | Stat-first diff; greps scoped to the 37 reviewable files |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Empty or swallowed catch | blocking | `rg -n 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` + multiline `rg -nU 'catch(\s*\([^)]*\))?\s*\{\s*\}'` | 0 | OK |
| QA-02 | `#pragma warning disable` / `#nullable disable` | blocking | `rg -n '#nullable disable\|#pragma warning disable' $files` | 0 | OK |
| QA-03 | `async void` / sync-over-async | blocking | `rg -n 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 | OK |
| QA-04 | Activation / z-order invariants | blocking | `rg -n 'SWP_SHOWWINDOW\|0x0040\|Activate\(\)' $files` (all 37 files) | 0 | OK |
| QA-05 | 4+ parameter list not split | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 0 new of 11 | Justified by the TechSpec: `NotchWindow.xaml.cs:104` (`WndProc`), `DisplayCatalog.Native.cs:17,21` (P/Invoke). Pre-existing: `App.xaml.cs:380` (`ApplicationLifetime`, unchanged baseline line). False positives: `DisplayCatalog.cs:101` (3 parameters; the comma is inside a generic type), `DisplayResolverTests.cs:56,80` (collection expression), `HudPlacementServiceTests.cs:77,79,122` (initializers / nested 2-argument call), `HudPositionStoreTests.cs:241` (JSON literal). `MonitorEntry` no longer matches |
| QA-06 | File > 300 / method > 30 lines | reservation | `wc -l` over `$files` + a brace-span method scan of the changed `.cs` files, compared with the same scan on `git show c4b55b9:` copies | 0 new / aggravated | Files: only `App.xaml.cs` (459) is above 300, which is pre-existing and below its 468 baseline. Methods above 30 lines are all baseline and unchanged: `App.xaml.cs` `RegisterClaude`; `WindowPlacement.cs` `PositionInWorkArea` (and `GetWorkArea` at the limit). `OnStartup`, `InitializeTray`, and `NotchWindow.Placement.cs:115` `ApplyDockedPlacement` have bodies at or just under the limit (29-30 lines). `InitializeUi` dropped from 35 to 25 lines. Test methods: none above the limit |
| QA-07 | `new HudPositionSettings` persisted | blocking | `rg -n 'new HudPositionSettings' $files` | 0 in production | OK. All 33 hits are in tests, which the TechSpec excepts |

- Terrain baseline: applied from the TechSpec (measured at `c4b55b9`). New files have no baseline row, so every hit in them counts as new. `DialogResources.xaml` also has no row; its baseline size (375 lines) was measured here with `git show c4b55b9:`.
- Hits discounted by baseline: 5. QA-05 `App.xaml.cs:380`, `NotchWindow.xaml.cs:104`; QA-06 `App.xaml.cs` file size, `RegisterClaude`, `PositionInWorkArea`.
- Reservations accumulated in the feature: 0 open. The 4 hits recorded in `codereview_01` are all resolved.
- Suggested escalation (for the HIL, not executed): `sdd-plan-refactoring`. Counted triggers:
  - "A touched file above 500 lines": `src/TokenHound.App/UI/Styles/DialogResources.xaml` grew from 375 to 561 lines, crossing 500 within this feature. It gained 186 lines: `DialogComboBoxItemStyle`, `DialogComboBoxStyle`, `HudSubmenuItemStyle`, `HudCheckMenuItemStyle`. The previous review did not record this.
  - The same trigger, pre-existing: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` grew from 949 to 1051 lines, with one new card.
  - "Same block duplicated in 3+ places", borderline: the `ItemBorder` + `IsHighlighted` hover trigger block now appears in 4 control templates. One is pre-existing (`DialogResources.xaml:264`); this feature added three (`:291`, `:417`, `:454`). WPF control templates cannot share trigger blocks without a further abstraction, so this is idiomatic. It is listed for the HIL, not counted as a defect.
  - QA-06 is scoped to `--type cs`, so neither XAML size is a reservation hit.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 string `Mode` + `Display`, `Left`/`Top` kept | YES | `HudPositionSettings.cs` |
| DEC-02 resolution table | YES | `HudPositionSettings.cs:54-61,72` |
| DEC-03 physical-pixel docking | YES | `GetMonitorInfo` work area + `SetWindowPos`. The size comes from `ActualWidth × DPI` because `GetWindowRect` is stale during `SizeChanged` (`tasks.md` Problems) |
| DEC-04 Free keeps the DIP clamp | YES | `ApplyMonitorPlacement` |
| DEC-05 identity match order | YES | `DisplayResolver` |
| DEC-06 enumeration + fallback names | YES | `DisplayCatalog.cs:27-44,110-112`. The query-failure warnings are kept after the CR-01 split (`DisplayCatalog.Targets.cs:29,52`) |
| DEC-07 triggers + 300 ms coalescing | YES | `NotchWindow.xaml.cs:115`; `NotchWindow.Placement.cs:16,58-77` |
| DEC-08 single owner, `with` writes | YES | `HudPlacementService.cs:75-109` |
| DEC-09 hosting display rule | YES | `PreferHosting` `:111-122` |
| DEC-10 drag only when moved | YES | `NotchWindow.xaml.cs:143-149` |
| DEC-11 pure edge layout + shared observable | YES | `HudEdgeLayout`, `HudDockLayout` |
| DEC-12 gutter per edge | YES | `NotchWindow.Dock.cs:50-54` |
| DEC-13 live apply, list built on open | YES | VM setters; `CreateHudPlacementSettingsViewModel` (`App.Hud.cs:16-23`) passes `_displayCatalog.GetDisplays` |
| DEC-14 tooltip / popup placement | YES | `HudDockLayout.cs:53-58`, `NotchWindow.Dock.cs:38-43` |
| CMP-17 App wiring | YES (relocated) | Moved by T09 from `App.xaml.cs` to the `App.Hud.cs` partial. It still creates the service once, hands it to `NotchWindow` and Settings, and enumerates displays before `Show()` |
| CMP-08 file split | PARTIAL (accepted) | Extra partial `DisplayCatalog.Targets.cs` (recorded in `codereview_01`) |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Contract + resolver; tests in `*HudPosition*` |
| T02 | `done/task_02.md` | COMPLETE | Pure rules + links |
| T03 | `done/task_03.md` | COMPLETE | Service + context record |
| T04 | `done/task_04.md` | COMPLETE | Catalog + pixel helpers; SystemAware startup log |
| T05 | `done/task_05.md` | COMPLETE | Engine, submenu, wiring; MA-1, MA-2, MA-4 |
| T06 | `done/task_06.md` | COMPLETE | Edge chrome; MA-2 sides, MA-3 |
| T07 | `done/task_07.md` | COMPLETE | Settings section; MA-5, MA-8, part of MA-7 |
| T08 | `codereview_01/done/task_08.md` | COMPLETE | `QueryTargets` split; build + suite |
| T09 | `codereview_01/done/task_09.md` | COMPLETE | `App.Hud.cs` partial; manual smoke deferred to HIL 3 (see limitations) |
| T10 | `codereview_01/done/task_10.md` | COMPLETE | `MonitorEntry` split; final build + suite |

## Executed validations

- Profile and exclusions: .NET 10 WPF desktop. E2E is omitted under the .NET desktop policy, which is neither a defect nor approved testing. Runner: native MTP through `dotnet test`.
- Validated state: worktree at `c4b55b9` + the uncommitted feature diff including corrections T08..T10, Debug. Only the installed `D:\Apps\TokenHound\TokenHound.App.exe` (PID 3012) was running; it was not touched and does not lock the dev `bin/`.
- Reused evidence: manual acceptance MA-1..MA-5, MA-8, and cross-scale MA-7 (handoffs T05..T07), plus MA-6, the rest of MA-7, and FR-07 (human, DEC-05). It stays valid for every file not touched by the corrections (last written ≤ 2026-10-05 20:46, before DEC-05).
- Manual acceptance: not re-executed. The contract forbids launching the app, and Windows MCP failed to connect in this session.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --no-incremental --nologo --verbosity:minimal` | passed: 4 projects, 0 errors, 0 warnings, exit 0 | NFR-06; compiles all CMPs and T08..T10 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --no-incremental --nologo --verbosity:minimal` | passed: 3 projects, 0 errors, 0 warnings, exit 0 | NFR-05 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed: 1043 tests, exit 0 | Full regression, FR-11 |
| same with `--filter-class` for `HudPositionSettingsTests`, `HudPositionStoreTests`, `DisplayResolverTests`, `HudEdgeLayoutTests`, `NotchPlacementTests`, `HudPlacementServiceTests`, `HudPlacementSettingsViewModelTests` | passed: 81 tests, exit 0 | TC-01..TC-07, TC-11 |
| QA-01..QA-07 commands over the 37 reviewable files; method-span scan against `git show c4b55b9:` copies | see Quality profile | QA-01..QA-07, TC-10 |

## Findings

No actionable findings and no optional improvements. No obligation is non-conformant, no test fails, and there are no blocking or new reservation profile hits.

Observations examined and not raised as findings (no proven reachable cause):

- Cross-DPI resize after a docked move. `ApplyDockedPlacement` sizes the window with the current display's DPI (`NotchWindow.Placement.cs:126-133`), and nothing re-docks on `DpiChanged`. Under per-monitor awareness, a move to a display with another scale could leave the capsule off by the resize. The app runs SystemAware: the dev log line `Display list changed: 3 display(s), DPI awareness SystemAware` (2026-10-05 20:49) confirms it, and there is no DPI manifest. In that mode Windows does not resize the window on a cross-DPI move and WPF raises no `DpiChanged`. MA-7 cross-scale (T07) and DEC-05 passed. The TechSpec risk entry and DEC-03 cover this case.
- `HudPlacementSettingsViewModel.SyncDisplayWithService` (`:197-200`) matches on `DevicePath`. A stored preference without `DevicePath` would match the "Primary monitor" option, whose `Preference` is null. This is reachable only from a hand-edited settings file. The catalog always fills `DevicePath` (`DisplayCatalog.cs:110`), and the service stores only `DisplayResolver.ToPreference(DisplayInfo)`. `FindDisplayOption` (`:186-188`) is unaffected because a resolved `Target.DevicePath` is never null.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_01/CR-01 | resolved | `DisplayCatalog.Targets.cs` is now three methods: `QueryTargets` `:10-19` (10 lines), `QueryActivePaths` `:21-32` (12 lines), and `FillActivePaths` `:34-55` (22 lines). The flags (`QDC_ONLY_ACTIVE_PATHS`), both warning templates with `{Status}`, and the empty result on failure are unchanged. `paths[..(int)pathCount]` consumes only the filled entries |
| codereview_01/CR-02 | resolved | `App.xaml.cs` is 459 lines (baseline 468, previously 481). `InitializeUi` `:371-395` is 25 lines (baseline 35). The new `App.Hud.cs` is 44 lines; `ShowNotchWindow` is 19 lines and `CreateHudPlacementSettingsViewModel` 8. Order is preserved: displays are enumerated before the window is created, `DataContext`/`ActionsViewModel`/`Placement`/`Displays` are set and `MainWindow` is assigned before `Show()`, and Settings `HudPlacement` is `null` without a HUD window (`App.Hud.cs:16-23,25-43`) |
| codereview_01/CR-03 | resolved | `DisplayCatalog.cs:177-182` declares one parameter per line, with the closing parenthesis on its own line; QA-05 no longer matches |

## Limitations and open items

- Not verifiable: the post-correction startup smoke (T08/T09 handoffs: the startup log lists every display, the HUD starts at its configured placement, and Settings shows the HUD placement section). Its owner is the human at HIL 3. The latest dev log (`src/TokenHound.App/bin/Debug/net10.0-windows/logs/TokenHound/TokenHound_20261005.logc`, last written 2026-10-05 21:17) predates the corrections (2026-10-06 09:15), so the corrected startup path has not run anywhere. This does not block the status because:
  - the change is composition-root wiring only;
  - reading `App.Hud.cs` against the baseline block confirmed the same assignments and order;
  - both non-incremental builds are clean;
  - the correction tasks assigned the smoke to HIL 3.
- Manual acceptance (TC-08..TC-10) was not re-executed: the contract forbids launching the app, and Windows MCP failed to connect. The states rely on the T05..T07 handoffs and DEC-05, which remain valid for unchanged files.
- DPI caveat: docking correctness across mixed scales depends on the current SystemAware runtime. If a per-monitor DPI manifest is ever added, `DpiChanged` must also trigger `ApplyPlacement`.
- Escalation for the HIL (`sdd-plan-refactoring`) has two counted triggers:
  - `DialogResources.xaml` crossed 500 lines in this feature (375 → 561).
  - `SettingsWindow.xaml` was already above 500 and grew from 949 to 1051.
  - The hover-trigger block repetition (4 templates) is listed as borderline.
- Source drift: `tasks.md` and `done/task_0N.md` hashes differ from the DEC-04 approval scope. The changes match the State checkboxes and filled handoffs, but the content delta could not be checked independently.
- Snapshot: the next step brief and O-03 are stale because the corrections are complete. O-02 (restore or keep the user's settings; the backup is in the authoring session's scratchpad and in `%LOCALAPPDATA%\TokenHound\settings.json.before-prd14-20261005.bak`) stays open for HIL 3. This reviewer did not update the snapshot, `workflow.md`, or the checkpoint, as the contract requires.
- FR-18 interpretation note from `codereview_01` still applies (DEC-09 compares with the resolved preference; approved at HIL 2).
- Accepted product limitations, documented in the TechSpec:
  - Identical monitors swapped between ports fall back to the primary (DEC-05).
  - A display plugged in while Settings is open appears the next time Settings opens (DEC-13).

## Conclusion

All 18 functional and 6 non-functional obligations are conformant. All ten tasks (T01..T07 and corrections T08..T10) are complete, and their links and states are consistent. The three reservations from `codereview_01` are resolved without introducing new profile hits. Evidence:

- both non-incremental builds have 0 warnings;
- all 1043 tests pass, including the 81 feature tests;
- the blocking rules QA-01..QA-04 and QA-07 have no hits;
- the HUD non-activating invariants are intact;
- the earlier manual acceptance still applies to all code the corrections did not touch.

The only open items are the post-correction startup smoke and the refactoring escalation suggestions, both for HIL 3. Neither is a requirement or essential-evidence gap. Status: APPROVED.
