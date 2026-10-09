# Code review report — HUD Backdrop Effects (PRD 17)

## Summary

- Status: REJECTED
- Execution: delegated reviewer
- Git scope: `2bc32ef80e0e3aff4070bcbd54a45be73ac23855..worktree`, uncommitted and untracked. The scope is limited to the PRD 17 files listed in `done/task_02.md` Handoff and the matching hunks of the modified files.
- Previous review: `—`

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-17-hud-backdrop-effects/prd.md` | read; SHA-256 `abbe9a6d…` matches checkpoint `approved_sources` (DEC-07) |
| TechSpec | `tasks/prd-17-hud-backdrop-effects/techspec.md` | read; SHA-256 `538d032b…` matches checkpoint (DEC-07) |
| Manifest | `tasks/prd-17-hud-backdrop-effects/tasks.md` | read; SHA-256 `ea60c826…` differs from approved `999f4d2c…`. The changes are the State, links, and Problems sections, which workflow Events record as execution records. |
| Task files | `done/task_01.md`, `done/task_02.md` | read; both links resolve. Hashes differ from the approved ones (`9615…`, `34e8…`) only in the Work checkboxes and the Handoff, which workflow Events (T01) and `sdd-execute-task` record as execution records. |
| Workflow, validation, checkpoint | `workflow.md` (DEC-01..08), `validation.md`, `checkpoint.json` (gen 12) | read |
| Snapshot | `context-snapshot.md` | Loaded through the independent-stage filter: header, next step brief, open threads (O-04, O-06), and on-run entries (L-03, L-05). Decisions and Code map were skipped. The header is valid: `git_head` 2bc32ef = HEAD, `covers_through` T02 agrees with `tasks.md` State, and the worktree matches its description. No suspect entries. |
| Implementation | `git diff 2bc32ef` of 12 tracked files plus 18 untracked PRD 17 files (14 src, 4 tests) | delimited. Unrelated tooling (`.agents/`, `.codex/`, `.context-brake/`, `context-brake.config.json`) and the PRD 16 task records are out of scope. |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 / FR-01 / US-01 | Acrylic in all six docking modes | `HudBackdropWindow`, `HudBackdropComposition`, `HudBackdropController.Synchronize` | TC-04/TC-05 manual; DEC-08 visual check | conformant | `validation.md` T02 matrix: TopCenter, Left, and Free run by the coordinator. Top left, Top right, Right edge, and the 150 % primary monitor were approved by the human at workflow DEC-08. |
| FR-02 | Material only inside the contour | `HudBackdropWindow.Flatten` + `HudBackdropInterop.SetRegion` (region from `contour.Fill`) | TC-05 | conformant | `HudContourController.cs:149-154` passes `contour.Fill`. Screenshots `t02-*-zoom4x.png`; T02.3 region proof. |
| FR-03 | Outside left/right/double clicks reach the app behind | Companion `WS_EX_TRANSPARENT`, `HTTRANSPARENT`, region-clipped (`HudBackdropInterop.cs:14-18,158-169`) | TC-03 unit; TC-05 manual | conformant for TopCenter and the T02.3 docked proof; **not verifiable** for Left, Right, Top left, Top right, and Free | `validation.md` TC-05/FR-03 covers TopCenter only. DEC-08 is a visual check. The TechSpec risk register asks for outside-click verification against the region polygon. |
| FR-04 / US-02 | Toggle in General, default on, immediate, persisted, missing section reads on | `HudBackdropSettings`, `HudBackdropStore`, `HudBackdropSettingsViewModel`, `HudBackdropSettingsCard`, `SettingsWindow.xaml:93`, `UserSettingsFile.cs:183` | `HudBackdropStoreTests` (3), `HudBackdropSettingsViewModelTests` (3); TC-06 | conformant | Tests passed. TC-06: solid within 17 ms, `"HudBackdrop": { "Enabled": false }` persisted. |
| FR-05 / US-03 | Fallback to exactly `#18181B` when unavailable | `HudBackdropPolicy.Resolve`; `HudBackdropController.Apply` (`_solid` = XAML `#18181B`, captured after `InitializeComponent`, `NotchWindow.xaml.cs:42-44`) | `HudBackdropPolicyTests` (10 cases); TC-06 | **non-conformant** (CR-01) | The policy paths are correct. The shadow-failure path leaves the 93 % tint with no material for the rest of the session (CR-01). Windows 10 is not verifiable on this machine; the policy unit test covers build 19045. |
| FR-06 | Live availability switch ≤ 2 s, no restart | `HudBackdropAvailability` (WM_SETTINGCHANGE `ImmersiveColorSet`, power-setting notifications, `SystemParameters.StaticPropertyChanged`) | TC-01 unit; TC-06 manual | conformant (see limitations) | Transparency < 1.5 s. Energy saver measured at "≈ 2 s", at the limit. The path is notification-driven with no added app delay (`HudBackdropAvailability.cs:91-123`). |
| FR-07 | Material follows size, mode, drag, DPI, and monitor changes | Same frame path as the shadow: `HudContourController.Synchronize` → `HudBackdropWindow.Synchronize` (resize, region, `Place`) | TC-05 manual | conformant | Cross-monitor drag DISPLAY1→DISPLAY3: the companion rect equals the HUD rect. Sizes 50/150 % approved at DEC-08. |
| OBJ-02 / NFR-01 | No focus steal; main-window `SWP_NOZORDER` without `SWP_SHOWWINDOW` unchanged | Companion `MA_NOACTIVATE` and `WS_EX_NOACTIVATE`. `NotchWindow` and `WindowStyles` are not in the diff. | `HudBackdropStyleTests` (2); TC-05 | conformant | The diff does not touch the HUD placement. During the drag the HUD never became foreground (`validation.md`). |
| NFR-02 | Icons ≥ 4.5:1, arcs ≥ 3:1 over white and black (DEC-07) | `TINT_ALPHA = 0xED` (`HudBackdropController.cs:19`); `ProviderRing.xaml:45` disc → Transparent | TC-07 | conformant | Icons 13.37:1. Lowest arc over white: grey 3.04:1. |
| NFR-03 | No polling; idle cost within 10 % | Event-driven only; no timers in the new files | TC-08 | conformant (see limitations) | CPU 7.78 s vs 8.36 s per 60 s; working set 182.4 vs 182.5 MB. The run deviates from the TC-08 script (limitations). |
| NFR-04 | Undocumented API isolated, fail closed | Only documented APIs (candidate A, non-layered companion). All backdrop calls go through `TryShow`, which catches and falls back (`HudBackdropController.cs:69-93`). | TC-01 | conformant | B (undocumented) was not used. A warning is logged with `HResult`. |
| NFR-05 | Core untouched; JSON section pattern | `HudBackdropStore` mirrors `HudSizeStore`; no `TokenHound.Core` file changed | TC-02 | conformant | `git diff --stat 2bc32ef` lists no Core path. |
| PD-01..04 | Acrylic, default on, silent fallback, spike decides | As above; the fallback logs only. PD-04 was resolved at DEC-05. | — | conformant | `workflow.md` DEC-05 |
| DEC-01..08 | See TechSpec adherence | — | — | see below | — |
| TC-01 | Policy over the inputs | — | `HudBackdropPolicyTests` | conformant | Each disabling input alone plus all inputs together; first-match switch. |
| TC-02 | Store read/write, missing, null, merge | — | `HudBackdropStoreTests` | conformant | Round trip through `UserSettingsFile` merge; sibling sections preserved. No dedicated test with appsettings defaults (optional improvement). |
| TC-03 | Companion styles | — | `HudBackdropStyleTests` | conformant | — |
| TC-05 | Full matrix (6 modes × 3 sizes × 3 DPIs, clicks, menu) | — | manual + DEC-08 | partially verified | Visuals were covered by the coordinator plus DEC-08. Outside clicks were run only in TopCenter (FR-03). |
| TC-06..08 | Toggle and live availability, contrast, idle | — | manual | conformant with deviations | `validation.md` T02 matrix |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (CLAUDE.md) | OK | No `src/TokenHound.Core` change |
| `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` on HUD windows | OK | `HudBackdropInterop.cs:161` |
| `EnableNonActivating` uses `SWP_NOZORDER` without `SWP_SHOWWINDOW` | OK | `WindowStyles.cs` is not in the diff. The companion's own `Place` uses `SWP_SHOWWINDOW` on a raw non-layered HWND, which the rule does not cover. |
| One class per file, sealed by default | OK | New types are `sealed` or `static`; `HudBackdropMode` is an enum |
| XML docs on public members | OK | The public types and members in the new files carry XML comments |
| File-scoped namespace, alphabetized usings | NOT OK (minor, matches existing files) | `HudBackdropController.cs:1` and `HudBackdropSettingsViewModel.cs:1` place `using Serilog;` before `using System;`. The existing `HudContourController.cs:1-10` uses the same ordering. |
| Constants `UPPER_CASE`, `nameof` | OK | e.g. `HudBackdropPolicy.cs:28-32` |
| ≥ 4-argument calls split | NOT OK (reservation) | QA-05 below |
| `dotnet-efficient-validation` / MTP | OK | Builds, then `dotnet run --no-build --no-restore -- --minimum-expected-tests 1` |
| E2E | N/A | Omitted by .NET desktop policy (TechSpec Test approach) |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void` / `.Result` / `.Wait()` | blocking | `rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 of 0 | OK |
| QA-02 | No empty `catch` | blocking | `rg -n --type cs 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | 0 of 0 | OK. `HudBackdropComposition.cs:75-81` disposes and rethrows. |
| QA-03 | No `#pragma warning disable` / `#nullable disable` | blocking | `rg -n --type cs '#nullable disable\|#pragma warning disable' $files` | 0 of 0 | OK |
| QA-04 | Files ≤ 300, methods ≤ 30 lines | reservation | `rg -c '^' --type cs $files` + method span inspection | 2 new or aggravated of 2 | NOT OK (reservation): `App.xaml.cs` is 462 lines (459 at base: pre-existing, aggravated by +3); `HudBackdropInterop.Create` spans 33 lines (`HudBackdropInterop.cs:63-95`). |
| QA-05 | ≥ 4-argument calls split across lines | reservation | `rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 2 new of 4 call hits | NOT OK (reservation): `HudBackdropController.cs:47` (`TryShow(hud, contour, pixelTransform, bounds)`) and `:112` (`Color.FromArgb(alpha, 0x18, 0x18, 0x1B)`). Pre-existing: `App.xaml.cs:383` (base line 380). `HudContourController.cs:215` is a 3-argument call matched through a nested call and lies outside the diff hunks. The other 9 matches are method, delegate, or extern declarations, which the call rule does not cover. |

- `$files`: the 17 new `.cs` files plus the 6 modified `.cs` files of the PRD 17 diff.
- Terrain baseline: applied from the TechSpec. `App.xaml.cs` is not in the baseline table (it lists `App.Hud.cs`), so its base state was measured from `git show 2bc32ef`.
- Hits discounted by baseline: 2 (`App.xaml.cs` 459 lines at base; `App.xaml.cs:383` call)
- Reservations accumulated in the feature: 4 (QA-04: 2, QA-05: 2)
- Suggested escalation: the reservation count (4 < 8) and the file size (462 < 500) triggers do not fire. The duplication trigger (3+ places) fires by count: the `SetWindowPos` P/Invoke now appears in 4 files (`HudShadowInterop.cs:85`, `WindowStyles.cs:97`, `WindowPlacement.cs:229`, new `HudBackdropInterop.cs:195`), against 3 at base. The TechSpec names no escalation skill. As an HIL suggestion only, the nearest available skill is `simplify` for a shared user32 interop. The pattern was already at the threshold before this feature, so the value is low.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 main HUD stays layered; separate input-transparent companion | YES | `NotchWindow.xaml` unchanged; `HudBackdropInterop.EXTENDED_STYLE` |
| DEC-02 candidate A, window region from the flattened contour (DEC-05 variant AR) | YES | `HudBackdropWindow.Flatten` (0.25 px tolerance, one figure) → `CreatePolygonRgn` WINDING → `SetWindowRgn` |
| DEC-03 go/no-go with HUD never activated | YES | `validation.md` T01 verdicts; T02 TC-05/NFR-01 |
| DEC-04 translucent tint alpha ≥ 1/255 inside the contour | YES | `TINT_ALPHA = 0xED`. The `ProviderRing` disc is now Transparent, but the decorator fill underneath keeps alpha 0xED. |
| DEC-05 same frame path, unowned topmost companion placed after the HUD | YES (with a minor variant) | `HudContourController.cs:149-154`; `HudBackdropInterop.Place` uses `insertAfter = hud` with `SWP_NOACTIVATE \| SWP_SHOWWINDOW` (the TechSpec text names `SWP_NOACTIVATE` only). The sibling extraction `HudBackdropController` is recorded as a deviation in the handoff. |
| DEC-06 pure policy over five inputs, notification-driven, no polling | YES | `HudBackdropPolicy.cs:25-34`; `HudBackdropAvailability.cs:53-56,81-101` |
| DEC-07 `HudBackdrop: { Enabled: bool? }`, null/missing = enabled, merged | YES | `HudBackdropSettings.cs:13-20`; `UserSettingsFile.cs:183` |
| DEC-08 `net10.0-windows`, hand-written WinRT ABI | YES | `HudBackdropComposition.cs`; App csproj not in the diff |
| CMP-05 `NotchWindow` swaps the fill and forwards messages | PARTIAL (equivalent) | `NotchWindow.xaml(.cs)` is unchanged. The fill swap lives in `HudBackdropController.Apply` and the message hook in `HudBackdropAvailability` (HwndSource hook on the HUD), with the same behavior. |
| Flow: fill and companion swap in the same pass; no frame with tint and no blur | NO (failure path) | CR-01 |
| Errors: any failure → Solid; one log with `Candidate`, `HResult` | PARTIAL | Backdrop failures resolve to Solid with one warning carrying `HResult`; there is no `Candidate` field (optional improvement). A shadow failure leaves the tint (CR-01). |
| Errors: companion and composition released on HUD close; graceful exit verified | PARTIAL | Code: `HudContourController.Dispose` → `HudBackdropController.Dispose` → `HudBackdropWindow.Dispose` (composition release, `DestroyWindow`). There is no recorded exit evidence in `validation.md`. Leak on a creation failure: CR-02. |
| Observability: one log per transition with the deciding input | YES | `HudBackdropController.cs:95-104` |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Verdict table and evidence files in `validation.md#t01-spike-2026-10-09`; no `src/` change; exception HIL DEC-05 recorded |
| T02 | `done/task_02.md` | COMPLETE (with an open evidence item) | All work items checked. The handoff lists the files, the deviations, 1101 tests, and quality notes. The TC-05 cases the coordinator did not run went to DEC-08. Outside clicks in non-TopCenter modes remain unverified (FR-03). |

## Executed validations

- Profile and exclusions: .NET 10 desktop (WPF App `net10.0-windows`), tests in `tests/TokenHound.Infrastructure.Tests` as an MTP executable. E2E is omitted by .NET desktop policy.
- Validated state: worktree at HEAD `2bc32ef` with the PRD 17 uncommitted and untracked files, Release configuration, Windows 10.0.26200, executed in this review session.
- Reused evidence: the manual matrices in `validation.md` (T01, T02.3 proof, T02 matrix) and workflow DEC-08 (human visual check). They stay valid because the code under review is the state they were produced on: the snapshot and handoff record no later code change, and the visual check followed T02.
- Manual acceptance: TC-04..08 are recorded and the DEC-08 visual check was approved. Outside-click checks outside TopCenter are an open item. This reviewer did not drive the desktop: DEC-06 authorized the coordinator only.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo --verbosity:minimal` | passed (0 errors, 0 warnings), exit 0 | build of all CMPs |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo --verbosity:minimal` | passed (0 errors, 0 warnings), exit 0 | CMP-08 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` | passed: 1101 total, 1101 succeeded, 0 failed, 0 skipped, exit 0 | TC-01..03, FR-04 VM, regression |
| same with `--filter-class "*HudBackdrop*"` | passed: 19 total, 19 succeeded, exit 0 | TC-01, TC-02, TC-03, FR-04 VM |
| QA-01..05 `rg` commands (above) | QA-01..03 clean; QA-04/05 4 reservations | Quality profile |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Low | FR-05; TechSpec Flow ("no frame shows tint without blur") and Errors ("resolves to Solid") | `HudBackdropController.cs:57-58`: `Hide()` only calls `_window.Hide()` and leaves `_mode` and `_decorator.Background` unchanged. `HudContourController.cs:115-120`: on a shadow `Win32Exception` it sets `_failed = true` and calls `HideCompanions()`; `:84` then returns early from every later `Schedule()`, so the fill is never re-applied. The arrange-invalid branch (`:102-108`) has the same gap for one frame. | When the mode is Material and the shadow sync fails, the capsule keeps the 0xED tint with no material for the rest of the session. It renders `#28282B` over white (TC-07), not `#18181B` "exactly as today". | Make the hide path that ends synchronization restore the solid fill. For example, `HudBackdropController.Hide()` (or a failure-specific method called from the `catch`) applies `HudBackdropMode.Solid` with a reason before hiding the window. Keep the transient arrange-invalid case aligned with the TechSpec flow. |
| CR-02 | Low | TechSpec Errors (release native resources; fail closed) | `HudBackdropInterop.cs:67-92`: `Create` throws on a failed `DwmSetWindowAttribute` (`:92`) after `CreateWindowExW` succeeded, without destroying the handle. `HudBackdropWindow.cs:94` never receives it, so `Dispose` (`:60-61`) cannot destroy it. | The fallback still works (`TryShow` catches). A hidden companion HWND leaks for the process lifetime on that failure path. | In `HudBackdropInterop.Create`, destroy the handle before rethrowing when `DwmSetWindowAttribute` fails. |

### Optional improvements (not blocking)

- QA-04/QA-05 reservations: split `HudBackdropController.cs:47` and `:112` across lines; trim `HudBackdropInterop.Create` (33 lines) to ≤ 30; `App.xaml.cs` grew from 459 to 462 lines.
- Add the `Candidate` structured field to the failure warning (`HudBackdropController.cs:89`) to match the TechSpec Errors text.
- `HudBackdropSettingsCard.xaml:32` description omits high contrast, which README and the policy include.
- `docs/ROADMAP.md` PRD 17 row still says "Awaiting the visual check"; DEC-08 approved it.
- TC-02: add a merge test with a defaults file that lacks or carries `HudBackdrop`.
- O-06 (open thread, pre-existing): `HudContourDecorator.BORDER_BRUSH_PROPERTY` uses `AddOwner` without `AffectsRender`, the same latent defect fixed for `Background`. It is not changed at runtime, so there is no current impact.

## Previous findings (re-review only)

Not applicable: first review.

## Limitations and open items

- FR-03 / TC-05: real outside left, right, and double clicks were exercised only in TopCenter (plus the T02.3 docked proof). Left edge, Right edge, Top left, Top right, and Free are `not verifiable` for click-through: DEC-08 is visual only, and the TechSpec risk register names this check. This does not block alone, since the region comes from the same contour in every mode. It is required evidence for HIL 3.
- FR-06 energy saver: measured at "≈ 2 s" against "within 2 s"; transparency passes at < 1.5 s. Not counted as a finding (OS notification latency, no app-side delay). A precise remeasurement is advised at HIL 3.
- TC-08 / NFR-03: run with 60 s windows instead of 5 minutes, and against solid mode in the same build instead of the PRD 16 build. The result is within 10 %. "No polling" is verified statically.
- Graceful exit (TechSpec Errors) has no recorded evidence; the disposal chain was verified in code only.
- FR-05 on Windows 10 or an unsupported Windows 11 build, and high contrast live: not executable on this machine; covered by policy unit tests only.
- `graft build` (T02.7) was not run by this reviewer: it writes `graft/`, outside the permitted outputs.
- Open thread O-04 (product note for HIL 3): with FR-05, this user's machine (transparency effects off) shows the solid fallback by default.
- Worktree also holds PRD 16 task-record changes and unrelated local tooling; both are excluded from scope.

## Conclusion

The implementation meets the PRD 17 design: candidate A with hand-written WinRT interop, a contour-region companion, a pure policy, a notification-driven availability tracker, and a persisted toggle. The build is clean, 1101 tests pass (19 new), and the blocking quality rules are clean. The manual evidence plus the DEC-08 visual check covers the material in every mode. CR-01 is a proven non-conformance with FR-05 and the TechSpec fallback contract on the shadow-failure path, so under the status rules the review is `REJECTED`. CR-02 is a narrow leak on a failure path. Both are small, localized fixes. After them, and with the outside-click evidence for the remaining modes, the feature is expected to qualify for `APPROVED WITH RESERVATIONS`.
