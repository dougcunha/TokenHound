# Code review report — HUD multi-monitor and edge docking (re-review after T11)

## Summary

- Status: APPROVED
- Execution: delegated reviewer
- Git scope: `c4b55b9cb1b372f819f59639e6836388e78ebed5..worktree`. HEAD is still the base, so the whole feature is uncommitted: 15 modified and 22 new files under `src/` and `tests/` (37 reviewable files), plus the feature folder. The only reviewable file changed since `codereview_02` is `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (task T11).
- Previous review: `tasks/prd-14-hud-multimonitor-docking/codereview_02/codereview.md` (APPROVED, no findings)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-14-hud-multimonitor-docking/prd.md` | read; sha256 `ac90b343d87e…` matches DEC-03 |
| TechSpec | `tasks/prd-14-hud-multimonitor-docking/techspec.md` | read; sha256 `132e117bcb2b…` matches DEC-04 |
| Manifest | `tasks/prd-14-hud-multimonitor-docking/tasks.md` | read. The links to `done/task_01.md`..`done/task_07.md` and `done/task_11.md` all resolve. State shows T01..T07 and T11 as `[x]`. T11 is new in the Tasks and State lists and is authorized by DEC-07, not by the DEC-04 hash scope |
| Handoffs | `done/task_01.md`..`task_07.md`, `done/task_11.md` | read. No unchecked work item exists (`grep '- \[ \]'` has 0 hits). Each file has a `## Handoff` with Produced result, Changed files, and Checks |
| Corrections (round 1) | `codereview_01/done/task_08.md`..`task_10.md` | read. Every work item is `[x]` and every handoff is filled. These files are unchanged since `codereview_02` |
| Workflow | `workflow.md` (DEC-01..DEC-07, Events) | read. DEC-07 (HIL 3) asks for the Startup card first (T11). It also closes O-02 (keep the current settings) and defers `sdd-plan-refactoring` until after PRD-14 closes |
| Checkpoint | `checkpoint.json` | read. `phase: review`; `active_work` is reviewer `codereview_03`, running |
| Snapshot | `context-snapshot.md` | Loaded through the independent-stage filter: header, next step brief, open threads, and the `on-run` entries L-02..L-05. `Decisions` and `Code map` were skipped. `git_head c4b55b9` matches HEAD. `covers_through: T07` is behind T08..T11, so the next step brief is stale. Its open threads O-02 and O-03 were decided by DEC-07 |
| Implementation | `git diff c4b55b9 -- src tests` + untracked files under `src/`, `tests/` | delimited |

Excluded from the reviewable set because they pre-date this flow and it does not own them (`workflow.md` Feature Context): `.agents/settings.json`, `.agents/hooks/`, `.agents/settings.local.json`, `.context-brake/`, `.opencode/`, `context-brake.config.json`. `tasks/triage-log.jsonl` has one appended line and no code.

Change since `codereview_02`: `find src tests -newer codereview_02/codereview.md` (excluding `bin/`, `obj/`, and `TestResults/`) returns only `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (mtime 2026-10-06 09:34:20). The diff stat against the base is unchanged except for that file. The per-file evidence `codereview_02` recorded for every `.cs` file is therefore reused.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Six modes, applied immediately | `HudDockMode.cs`; `HudPlacementSettingsViewModel` setters; `NotchWindow.Placement.cs` `OnPlacementChanged` | `HudPlacementSettingsViewModelTests` | conformant | Code unchanged since `codereview_02`. The HUD placement card still binds `SelectedMode`/`SelectedDisplay` TwoWay (`SettingsWindow.xaml:478`ff). MA-2, MA-5 |
| FR-02 | Top docks flush, aligned ≤ 1 px | `NotchPlacement.Dock` | `NotchPlacementTests.Dock_*` | conformant | Unchanged; MA-2 (T05) |
| FR-03 | Side docks vertical and centered | `Dock` side branch; `HudDockLayout` | `Dock_SideModes_*`, `HudEdgeLayoutTests` | conformant | Unchanged; MA-2 sides (T06) |
| FR-04 | Outline follows the edge | `NotchWindow.Dock.cs` | `HudEdgeLayoutTests` + visual | conformant | Unchanged; MA-2 |
| FR-05 | Tooltip and popup face inward | `ProviderRing.xaml`; `HudDockLayout`; `NotchWindow.Dock.cs` | manual | conformant | Unchanged; MA-3 (T06) |
| FR-06 | Primary or specific display, labeled list | `DisplayCatalog`; VM `BuildDisplayOptions` | `DisplayResolverTests`, VM tests | conformant | The 2026-10-06 dev log enumerates 3 displays with names and resolutions (`TokenHound_20261006.logc:27-30,65-68`); MA-5 |
| FR-07 | Primary follows the Windows primary | `DisplayResolver.Resolve`; `WM_DISPLAYCHANGE` timer | resolver tests | conformant | Human check, DEC-05 |
| FR-08 | Disconnected preference falls back and returns | `Resolve` fallback; VM disconnected option | resolver + VM tests | conformant | MA-6, DEC-05 |
| FR-09 | Same monitor recognized again | `DisplayCatalog.Targets.cs`; resolver match order | `DisplayResolverTests` | conformant | The dev log restores the stored specific display (`:31-32`, "TopCenter" on Display 1) |
| FR-10 | Drag switches to Free and is stored | `NotchWindow.xaml.cs` `RecordDrag` only when moved | `RecordDrag_SwitchesToFreeAndKeepsDisplay` | conformant | Unchanged; MA-4 |
| FR-11 | Free keeps the `c4b55b9` clamp | `ApplyFreePlacement` → `ApplyMonitorPlacement` | existing `NotchPlacementTests` | conformant | Full suite passed |
| FR-12 | Display selector disabled in Free, with hint | VM `IsDisplayEnabled`/`DisplayHint`; XAML `IsEnabled` + hint `TextBlock` | `FreeMode_DisablesDisplaySelectorWithHint` | conformant | The XAML bindings are unchanged by T11; MA-8 |
| FR-13 | Re-anchor on work area, resolution, DPI, size, provider count | `WndProc` → timer; `SizeChanged` → `ApplyPlacement` | manual | conformant | MA-7 + DEC-05. The dev log shows `SizeChanged` re-anchors converging during startup (`:32-33,38`) |
| FR-14 | Fresh install → Top Center | `HudPositionSettings.ResolveMode` | `HudPositionSettingsTests` | conformant | MA-1 |
| FR-15 | `Left`/`Top`-only file → Free | `ResolveMode` migration; no write on load | `ResolveMode_WhenOnlyCoordinatesAreStored_MigratesToFree`, `Load_WhenFileHasOnlyCoordinates_DoesNotRewriteFile` | conformant | MA-8 |
| FR-16 | Unknown value → Top Center + one warning | `ResolveMode`; logged once | theories + `Load_WhenModeIsUnknown_KeepsStoredCoordinates` | conformant | — |
| FR-17 | Context-menu "Position" submenu | `NotchWindow.xaml`; `NotchWindow.Menu.cs` | manual | conformant | Unchanged; MA-2. The post-correction smoke (workflow Events, 2026-10-06) also shows it |
| FR-18 | Free → docked keeps the hosting display | `HudPlacementService.PreferHosting` | `SelectMode_FromFree*`, VM test | conformant | Unchanged; DEC-09 interpretation (see limitations) |
| NFR-01 | No activation; invariants kept | `NotchWindow.xaml.cs:107-112` (`WM_MOUSEACTIVATE` → `MA_NOACTIVATE`, constant 3 at `:19`); `MoveWindowTo` flags | QA-04 | conformant | QA-04 has 0 hits in this session |
| NFR-02 | Mixed DPI flush, no oscillation | Physical-pixel docking (DEC-03) | manual | conformant | MA-7 + DEC-05 under the SystemAware runtime (log `:27`). See limitations |
| NFR-03 | ≤ 1 write per display change, no loop | Docked passes never call the service; guard + coalescing timer | `SelectMode_WhenAlreadyStored_DoesNotSaveOrNotify` | conformant | Unchanged |
| NFR-04 | Forward and backward settings compatibility | String `Mode` + resolver | `HudPositionStoreTests` | conformant | QA-07: 0 production hits |
| NFR-05 | Rules unit-tested without a display | Linked pure files | 81 feature tests | conformant | Filtered run below |
| NFR-06 | Windows 11, existing app and targets | No project or target changes | App build | conformant | 0 warnings, 0 errors |
| TC-01..TC-07, TC-11 | Unit scenarios | as above | 81 passed | conformant | Filtered run |
| TC-08, TC-09, TC-10 | Manual MA-1..MA-8, focus | — | manual | conformant | Reused from the T05..T07 handoffs and DEC-05. The code paths are unchanged since then, except the startup wiring, which the 2026-10-06 log now proves |
| DEC-07 / T11 | General tab order Startup, HUD size, HUD placement. Spacing 12/12/4. Each card collapses on a `null` view model | `SettingsWindow.xaml:284-290` (Startup, `0,0,0,12`), `:359-365` (HudSize, `0,0,0,12`), `:471-478` (HudPlacement, `0,0,0,4`); tab collapse `MultiDataTrigger` `:268-271` lists all three | App build (XAML compile) + full suite | conformant | The Startup and HudSize card bodies match the base after whitespace normalization. Each card keeps its `DataTrigger Binding="{Binding}" Value="{x:Null}"` → Collapsed. `git diff --check` is clean. File is 1051 lines (unchanged). The visual confirmation is under limitations |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `TokenHound.Core` pure | N/A | Core not touched |
| `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` on HUD | OK | `NotchWindow.xaml.cs:18-19,107-112` |
| `SWP_NOZORDER`, no `SWP_SHOWWINDOW` | OK | QA-04 has 0 hits over all 37 files |
| C# structure and style (one class per file, sealed, ≤ 300/30, XML docs, usings, splits, blank lines) | OK | No `.cs` file changed since `codereview_02`, which verified these rules. T11 is XAML only |
| `dotnet-efficient-validation` | OK | Native MTP (`global.json` `test.runner: Microsoft.Testing.Platform`, SDK 10.0.401). `--minimum-expected-tests 1` on every run; `$LASTEXITCODE` checked |
| `repository-cli-efficiency` | OK | Diff stat first; greps scoped to the 37 reviewable files |
| HUD validation via Windows MCP (`CLAUDE.md`) | N/A for this reviewer | The contract forbids launching the app. The authoring session's T11 check used Windows MCP (`done/task_11.md` Handoff) |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Empty or swallowed catch | blocking | `rg -n 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $cs` + multiline `rg -nU 'catch(\s*\([^)]*\))?\s*\{\s*\}' $cs` | 0 | OK |
| QA-02 | `#pragma warning disable` / `#nullable disable` | blocking | `rg -n '#nullable disable\|#pragma warning disable' $cs` | 0 | OK |
| QA-03 | `async void` / sync-over-async | blocking | `rg -n 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $cs` | 0 | OK |
| QA-04 | Activation / z-order invariants | blocking | `rg -n 'SWP_SHOWWINDOW\|0x0040\|Activate\(\)' $files` (all 37 files, XAML included) | 0 | OK |
| QA-05 | 4+ parameter list not split | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' $cs` | 0 new of 11 | The same 11 hits as `codereview_02`, with no new line. Justified by the TechSpec: `NotchWindow.xaml.cs:104` (`WndProc`), `DisplayCatalog.Native.cs:17,21` (P/Invoke). Pre-existing: `App.xaml.cs:380`. False positives: `DisplayCatalog.cs:101` (3 parameters; the comma is inside a generic), `DisplayResolverTests.cs:56,80`, `HudPlacementServiceTests.cs:77,79,122`, `HudPositionStoreTests.cs:241` |
| QA-06 | File > 300 / method > 30 lines | reservation | `wc -l` over `$cs`. The method-span scan is reused from `codereview_02` because no `.cs` file changed | 0 new / aggravated | Only `App.xaml.cs` (459) is above 300, which is pre-existing and below its 468 baseline. Every method above 30 lines is baseline and unchanged |
| QA-07 | `new HudPositionSettings` persisted | blocking | `rg -c 'new HudPositionSettings' $cs` | 0 in production | OK. All 33 hits are in tests, which the TechSpec excepts |

- Terrain baseline: applied from the TechSpec (measured at `c4b55b9`). `DialogResources.xaml` has no row; `codereview_02` measured its base size as 375 lines.
- Hits discounted by baseline: 5. QA-05 `App.xaml.cs:380`, `NotchWindow.xaml.cs:104`; QA-06 `App.xaml.cs` file size, `RegisterClaude`, `PositionInWorkArea`.
- Reservations accumulated in the feature: 0 open. The `codereview_01` reservations CR-01..CR-03 were resolved in round 1.
- Suggested escalation: `sdd-plan-refactoring`. The trigger "a touched file above 500 lines" still fires twice:
  - `src/TokenHound.App/UI/Styles/DialogResources.xaml`, 375 → 561 lines (crossed in this feature);
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`, 949 → 1051 lines (T11 leaves it at 1051).

  The HIL already decided this at DEC-07: plan it as its own feature after PRD-14 closes. It is recorded here for traceability, not as a new HIL request. QA-06 is scoped to `--type cs`, so neither XAML size is a reservation hit.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01..DEC-14 | YES | No `.cs` file changed since `codereview_02`, which verified each decision with `file:line` evidence. That evidence is reused |
| DEC-13 Settings section in the General tab "under HUD size", live apply | YES | After T11, HUD placement still directly follows HUD size (`SettingsWindow.xaml:359-478`), with no Apply button. Startup moved above both per DEC-07, which the HIL authorized |
| CMP-17 App wiring (relocated to `App.Hud.cs` by T09) | YES | Unchanged. The startup path now has runtime evidence: `TokenHound_20261006.logc:27-34` (09:29) and `:65-72` (09:35). Both runs are after the `App.Hud.cs` mtime (09:16), and the second is after the T11 edit (09:34) |
| CMP-08 file split | PARTIAL (accepted) | Extra partial `DisplayCatalog.Targets.cs`, as recorded in `codereview_01` |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Contract + resolver; `*HudPosition*` tests |
| T02 | `done/task_02.md` | COMPLETE | Pure rules + links |
| T03 | `done/task_03.md` | COMPLETE | Service + context record |
| T04 | `done/task_04.md` | COMPLETE | Catalog + pixel helpers; SystemAware startup log |
| T05 | `done/task_05.md` | COMPLETE | Engine, submenu, wiring; MA-1, MA-2, MA-4 |
| T06 | `done/task_06.md` | COMPLETE | Edge chrome; MA-2 sides, MA-3 |
| T07 | `done/task_07.md` | COMPLETE | Settings section; MA-5, MA-8, part of MA-7 |
| T08 | `codereview_01/done/task_08.md` | COMPLETE | `QueryTargets` split (resolved CR-01) |
| T09 | `codereview_01/done/task_09.md` | COMPLETE | `App.Hud.cs` partial (resolved CR-02). The startup smoke is now proven by the dev log |
| T10 | `codereview_01/done/task_10.md` | COMPLETE | `MonitorEntry` split (resolved CR-03) |
| T11 | `done/task_11.md` | COMPLETE | The Startup card moved first and the margins were swapped. The handoff records both builds at 0 warnings, 1043 tests, and a Windows MCP screenshot. This review re-proved the structure and the builds |

## Executed validations

- Profile and exclusions: .NET 10 WPF desktop. E2E is omitted under the .NET desktop policy, which is neither a defect nor approved testing. Runner: native MTP through `dotnet test`.
- Validated state: worktree at `c4b55b9` + the uncommitted feature diff, including corrections T08..T10 and T11, in the Debug configuration. Only the installed `D:\Apps\TokenHound\TokenHound.App.exe` (PID 28772) was running. It was not touched and does not lock the dev `bin/`.
- Reused evidence:
  - `codereview_02` per-file `.cs` evidence: the method-span scan (QA-06), the style checks, and the `file:line` evidence for DEC-01..DEC-14. It stays valid because `find -newer` shows no `.cs` change since that report.
  - Manual acceptance MA-1..MA-8 from the T05..T07 handoffs and DEC-05, for unchanged code paths.
- Manual acceptance:
  - Post-correction startup: proven by `src/TokenHound.App/bin/Debug/net10.0-windows/logs/TokenHound/TokenHound_20261006.logc`. It records two startups (09:29 and 09:35). Each lists 3 displays (SystemAware), restores "TopCenter" on Display 1, and logs "HUD window displayed successfully." The file has 0 WARN or ERROR lines (16 DEBUG, 60 INFO).
  - T11 visual order: not re-executed by this reviewer (see limitations).

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --no-incremental --nologo --verbosity:minimal` | passed: 4 projects, 0 errors, 0 warnings, exit 0 | NFR-06; XAML compile for T11; all CMPs |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --no-incremental --nologo --verbosity:minimal` | passed: 3 projects, 0 errors, 0 warnings, exit 0 | NFR-05 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed: 1043 tests, exit 0 | Full regression, FR-11, T11 regression guard |
| same with `--no-banner` and `--filter-class` for `*HudPositionSettingsTests`, `*HudPositionStoreTests`, `*DisplayResolverTests`, `*HudEdgeLayoutTests`, `*NotchPlacementTests`, `*HudPlacementServiceTests`, `*HudPlacementSettingsViewModelTests` | passed: 81 total, 0 failed, 0 skipped, exit 0 | TC-01..TC-07, TC-11 |
| QA-01..QA-07 over the 37 reviewable files; `git diff --check c4b55b9 -- SettingsWindow.xaml` | see Quality profile; `--check` clean | QA-01..QA-07, TC-10, T11 |
| Card-content comparison of `SettingsWindow.xaml` against `git show c4b55b9:` (whitespace-normalized) | Startup and HudSize card bodies identical | T11 "bindings and behavior unchanged" |

## Findings

No actionable findings and no optional improvements. No obligation is non-conformant, no test fails, and there are no blocking or new reservation profile hits.

Observations examined and not raised as findings (no proven defect):

- The dev log places the HUD on Display 1 while Display 2 is the Windows primary (`TokenHound_20261006.logc:29,32`). This is the stored specific preference: DEC-07 (O-02) keeps Top Center on Display 1. It is not a resolver fallback.
- Each startup runs three docked passes with X −1850 → −1860 → −1894 and then stays stable (`:32-33,38`). These are `SizeChanged` re-anchors as provider content loads and widens the capsule, which DEC-07 and FR-13 call for. They converge, so this is not NFR-03 or NFR-02 oscillation.
- The two observations `codereview_02` examined (cross-DPI resize under a future per-monitor manifest, and a `DisplayPreference` without `DevicePath` reachable only from hand edits) still apply unchanged. They are not findings.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_02 (no CR raised) | — | `codereview_02` had no findings and no optional improvements |
| codereview_02 limitation: post-correction startup smoke not verified | resolved | `TokenHound_20261006.logc:27-34` (09:29:15-09:29:18) and `:65-72` (09:35:17-09:35:20) show the corrected `DisplayCatalog` and `App.Hud.cs` path working: displays enumerated, placement restored, HUD displayed, no warnings. Both runs are after the corrections (`App.Hud.cs` mtime 09:16). Workflow Events 2026-10-06 records the matching Windows MCP smoke |
| codereview_01/CR-01 | resolved | `DisplayCatalog.Targets.cs` unchanged since `codereview_02` verified the split |
| codereview_01/CR-02 | resolved | `App.xaml.cs` (459 lines) and `App.Hud.cs` unchanged since `codereview_02` |
| codereview_01/CR-03 | resolved | `DisplayCatalog.cs` `MonitorEntry` split unchanged; QA-05 does not match it |

## Limitations and open items

- Not verifiable by this reviewer: T11 visual confirmation that the General tab renders Startup, HUD size, and HUD placement in that order. The contract forbids launching the app, and the T11 screenshot (`t11-settings-general.png`) is in the authoring session's scratchpad. Structural evidence covers the acceptance criterion: XAML element order, margins 12/12/4, identical card bodies, collapse triggers, and a clean XAML compile. The owner of the visual confirmation is the human at HIL 3. This does not block the status.
- Manual acceptance TC-08..TC-10 was not re-executed. The states rely on the T05..T07 handoffs and DEC-05, which remain valid for unchanged code, plus the 2026-10-06 startup log.
- DPI caveat carried from `codereview_02`: docking correctness across mixed scales depends on the current SystemAware runtime. If a per-monitor DPI manifest is ever added, `DpiChanged` must also trigger `ApplyPlacement`.
- Escalation (`sdd-plan-refactoring` for `DialogResources.xaml` 561 and `SettingsWindow.xaml` 1051 lines) was already decided at DEC-07: plan it after PRD-14 closes.
- Source drift:
  - `tasks.md` differs from the DEC-04 hash scope. It adds T11 to Tasks and State, and the State and Problems sections were updated during execution.
  - `done/task_11.md` is outside DEC-04; DEC-07 authorizes it.
  - The handoff files differ from their DEC-04 hashes because the handoffs were filled in. The content delta could not be checked independently.
- Snapshot is stale:
  - `covers_through: T07` does not reflect T08..T11, so the next step brief and O-03 are out of date.
  - O-02 was closed by DEC-07, which keeps the current settings.
  - As the contract requires, this reviewer did not update the snapshot, `workflow.md`, or the checkpoint.
- FR-18 interpretation note from `codereview_01` still applies: DEC-09 compares against the resolved preference, as approved at HIL 2.
- Accepted product limitations, documented in the TechSpec:
  - Identical monitors swapped between ports fall back to the primary (DEC-05).
  - A display plugged in while Settings is open appears the next time Settings opens (DEC-13).

## Conclusion

T11 (DEC-07) reorders the Settings General tab to Startup, HUD size, HUD placement. It keeps the card bodies, bindings, and collapse triggers identical and the spacing at 12/12/4, and it is the only reviewable change since `codereview_02`. Evidence:

- Both non-incremental builds have 0 warnings.
- All 1043 tests pass, including the 81 feature tests.
- The blocking profile rules QA-01..QA-04 and QA-07 have no hits.
- QA-05 and QA-06 have no new hits.
- The HUD non-activating invariants are intact.

The `codereview_02` limitation on the post-correction startup smoke is now resolved by the 2026-10-06 dev log. All 18 functional and 6 non-functional obligations remain conformant. All eleven tasks (T01..T07, T08..T10, T11) are complete with consistent links and states. The open items are a human visual confirmation of the new card order at HIL 3 and the refactoring that DEC-07 already scheduled. Neither is a requirement or essential-evidence gap. Status: APPROVED.
