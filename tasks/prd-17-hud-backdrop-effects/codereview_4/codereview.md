# Code review report — HUD Backdrop Effects (PRD 17), re-review after correction round 3

## Summary

- Status: APPROVED WITH RESERVATIONS
- Execution: delegated reviewer
- Git scope: `2bc32ef80e0e3aff4070bcbd54a45be73ac23855..worktree` (uncommitted and untracked; HEAD = base). Scope is limited to the PRD 17 files: 9 modified tracked files under `src/` and `tests/`, 18 untracked PRD 17 files (14 `src`, 4 `tests`), and the PRD 17 hunks in `ARCHITECTURE.md`, `README.md`, and `docs/ROADMAP.md`. The quality profile covers 23 `.cs` files (17 new, 6 modified).
- Previous review: `tasks/prd-17-hud-backdrop-effects/codereview_3/codereview.md` (APPROVED WITH RESERVATIONS; correction T07 in `codereview_3/done/` under workflow DEC-10)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-17-hud-backdrop-effects/prd.md` | read; SHA-256 `abbe9a6d…` matches checkpoint `approved_sources` (DEC-07) |
| TechSpec | `tasks/prd-17-hud-backdrop-effects/techspec.md` | read; SHA-256 `7188433e…` matches checkpoint (DEC-09). Not amended in round 3. |
| Manifest | `tasks/prd-17-hud-backdrop-effects/tasks.md` | read; SHA-256 `ea60c826…`, unchanged since codereview_1. It differs from the approved `999f4d2c…` only in State, links, and Problems, which are execution records. T01 and T02 are `[x]`; both links resolve to `done/`. |
| Feature tasks | `done/task_01.md` (`7d3ac932…`), `done/task_02.md` (`97ec5638…`) | read (T02 Handoff read directly in this session); unchanged since codereview_1. `task_02.md` differs from the DEC-05 hash `34e8d514…` because the Handoff was filled during execution: execution-record drift, not a contract change. |
| Correction tasks | `codereview_1/done/task_03.md`, `task_04.md`; `codereview_2/done/task_05.md`, `task_06.md`; `codereview_3/done/task_07.md` (DEC-10) | read; all Work items `[x]`; handoffs filled. Only `task_07.md` is new in this round. |
| Workflow, validation, checkpoint | `workflow.md` (DEC-01..10), `validation.md` (last changed 15:09, before round 2), `checkpoint.json` (gen 18, `phase: review`, `correction_round: 3`, `active_work` reviewer for `codereview_4`) | read |
| Snapshot | `context-snapshot.md` | Read whole (< 8 KiB) and loaded through the independent-stage filter: only the header, next step brief, Open threads (O-04, O-06, O-07), and on-run entries (L-03, L-05). D-02 and M-01 were not used. Header valid: `git_head` 2bc32ef = HEAD; `covers_through` (T07 in `codereview_3/done/`) matches the corrections folder; the worktree matches its description. No stale hint this round. |
| Implementation | `git diff 2bc32ef` + untracked PRD 17 files | delimited. `find -newer codereview_3/codereview.md` over `src`, `tests`, `docs`, `README.md`, `ARCHITECTURE.md`, and the feature folder lists only `src/TokenHound.App/UI/Windows/HudBackdropController.cs` (the T07 file) plus SDD records (`checkpoint*.json`, `context-snapshot.md`, `workflow.md`, `codereview_3/done/task_07.md`). Everything else is in the state codereview_3 judged. PRD 16 task records, PRD 16 ROADMAP hunks, and unrelated tooling (`.agents/`, `.codex/`, `.context-brake/`, `context-brake.config.json`) are out of scope. |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 / FR-01 / US-01 | Acrylic in all six docking modes | `HudBackdropWindow`, `HudBackdropComposition`, `HudBackdropController.Synchronize` | TC-04/TC-05 manual; DEC-08 | conformant | `validation.md` T02 matrix (TopCenter, Left, Free); DEC-08 human visual check covers Top left, Top right, Right edge, 150 % primary. T07 changed only log-level and flag handling in `Apply`. |
| FR-02 | Material only inside the contour | `HudBackdropWindow.Flatten` + `HudBackdropInterop.SetRegion` from `contour.Fill` | TC-05 | conformant | Files unchanged since codereview_3; `t02-*-zoom4x.png`; T02.3 region proof |
| FR-03 | Outside left/right/double clicks reach the app behind | Companion `WS_EX_TRANSPARENT` + `HTTRANSPARENT`; region from the same contour in every mode | TC-03 unit; TC-05 manual | conformant (TopCenter, T02.3 docked proof) / **not verifiable** (Left, Right, Top left, Top right, Free) | `validation.md` TC-05/FR-03 row records TopCenter only. Accepted HIL 3 open item A (DEC-09); see limitations. |
| FR-04 / US-02 | Toggle in General, default on, immediate, persisted, missing section reads on | `HudBackdropSettings`, `HudBackdropStore`, `HudBackdropSettingsViewModel`, `HudBackdropSettingsCard`, `SettingsWindow.xaml`, `UserSettingsFile` merge | `HudBackdropStoreTests`, `HudBackdropSettingsViewModelTests`; TC-06 | conformant | 19/19 `HudBackdrop*` tests pass in this session; TC-06 solid within 17 ms, persisted |
| FR-05 / US-03 | Fallback to exactly `#18181B` | `HudBackdropPolicy.Resolve`; `HudBackdropController.Synchronize`/`Hide` | `HudBackdropPolicyTests`; TC-06 | conformant | `Hide` hides the companion, then applies Solid (`HudBackdropController.cs:70-75`); the fill swap `_decorator.Background = … TINT : _solid` (`:124`) is unchanged by T07. Windows 10 and unsupported builds: policy unit tests only. |
| FR-06 | Live availability switch ≤ 2 s | `HudBackdropAvailability` | TC-01; TC-06 | conformant (see limitations) | Transparency < 1.5 s; energy saver ≈ 2 s. File unchanged. |
| FR-07 | Material follows size, mode, drag, DPI, monitor | `HudContourController.Synchronize(HudContourGeometry)` → `SynchronizeShadow`, then `HudBackdropController.Synchronize` | TC-05 | conformant | `HudContourController.cs` unchanged since codereview_3 |
| OBJ-02 / NFR-01 | No focus steal; main-window placement rule unchanged | Companion `MA_NOACTIVATE`, `WS_EX_NOACTIVATE`; `NotchWindow`/`WindowStyles` not in the diff | `HudBackdropStyleTests`; TC-05 | conformant | Unchanged |
| NFR-02 | Icons ≥ 4.5:1, arcs ≥ 3:1 (DEC-07) | `TINT_ALPHA = 0xED` (`HudBackdropController.cs:22`) | TC-07 | conformant | Icons 13.37:1; lowest arc 3.04:1 over white |
| NFR-03 | No polling; idle within 10 % | Event-driven; no timers in new files | TC-08 | conformant (see limitations) | CPU 7.78 s vs 8.36 s / 60 s; working set 182.4 vs 182.5 MB |
| NFR-04 | Undocumented API isolated, fail closed | Documented candidate A only; `TryShow` catches, disposes, falls back (`HudBackdropController.cs:86-110`) | TC-01 | conformant | Unchanged by T07 |
| NFR-05 | Core untouched; JSON section pattern | `HudBackdropStore` mirrors `HudSizeStore` | TC-02 | conformant | No `src/TokenHound.Core` path in the diff |
| PD-01..04 | Acrylic, default on, silent fallback, spike decides | As above; PD-04 resolved at DEC-05 | — | conformant | `workflow.md` DEC-05 |
| TC-01..03 | Policy, store, companion styles | — | `HudBackdrop*` tests (19) | conformant | 19/19 pass in this session |
| TC-05 | Full matrix (modes × sizes × DPIs, clicks, menu) | — | manual + DEC-08 | partially verified | Outside clicks only in TopCenter (FR-03) |
| TC-06..08 | Toggle and live availability, contrast, idle | — | manual | conformant with deviations | `validation.md` T02 matrix |
| T07 (DEC-10) | Flag updated from every call before the same-mode return; Debug only for an arrange-invalid transition or the one right after; `OrdinalIgnoreCase` | `HudBackdropController.Apply` (`:112-126`) | build + suite; code inspection | conformant | `:115` compares with `StringComparison.OrdinalIgnoreCase`; `:116` saves the previous flag as `afterTransient`; `:117` sets `_transient` from the current reason before the early return at `:119-120`; `:122` picks Debug for `transient \|\| afterTransient`. Traces: (a) Material → `Hide("ArrangeInvalid")` → valid Material: Solid logs Debug (`transient`), Material logs Debug (`afterTransient`), flag cleared. (b) Acceptance criterion: arrange-invalid Solid → same-mode Solid with another reason (`"Hidden"`, a policy name) clears the flag at `:117` and returns → the next transition logs Information. (c) Repeated arrange-invalid frames keep the flag set; the first valid frame after them logs Debug. No other reason (`Hidden`, `ShadowFailure`, `Failure`, policy names, `Available`) matches `ArrangeInvalid` ignoring case. Method is 15 lines; file 146 lines. |
| codereview_1 T03 (CR-01) | Every hide path shows the solid fill | `HudContourController.HideCompanions` (`:163`) → `HudBackdropController.Hide` | code inspection | conformant | Callers intact: `:88` `"Hidden"`, `:105` `ARRANGE_INVALID_REASON`, `:119` `"ShadowFailure"` |
| codereview_1 T04 (CR-02) | Destroy the HWND on a failed opt-in; `Create` ≤ 30 lines | `HudBackdropInterop.EnableHostBackdrop` | code inspection | conformant | File unchanged since codereview_2 |
| codereview_2 T05, T06 | `Synchronize` split, docs/card text; transient Debug logging via a shared constant | `HudContourController.cs`, `docs/ROADMAP.md`, `HudBackdropSettingsCard.xaml`, `HudBackdropController.ARRANGE_INVALID_REASON` | code inspection | conformant | T05 files unchanged; T06 constant and Debug pair kept by T07 |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (CLAUDE.md) | OK | No `src/TokenHound.Core` change |
| `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` on HUD windows | OK | `HudBackdropInterop.cs` unchanged |
| `EnableNonActivating` uses `SWP_NOZORDER` without `SWP_SHOWWINDOW` | OK | `WindowStyles.cs` not in the diff |
| String comparison `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` | OK | `HudBackdropController.cs:115` (was `Ordinal` in codereview_3) |
| Blank line inside blocks and before control flow; braces omitted for single-line bodies | OK | `HudBackdropController.cs:114`, `:118`, `:119-120` |
| One class per file, sealed, XML docs, `UPPER_CASE` constants | OK | No new types or members in round 3 |
| Structured logging with typed arguments | OK | `Log.Write(level, "HUD background switched to {Mode} ({Reason})", mode, reason)` (`:125`) |
| `dotnet-efficient-validation` / MTP | OK | Builds with `--no-restore`, then `dotnet run --no-build --no-restore -- --minimum-expected-tests 1` (runner: MTP via `global.json` `test.runner`, executable route) |
| E2E | N/A | Omitted by .NET desktop policy (TechSpec Test approach) |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void` / `.Result` / `.Wait()` | blocking | `rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 of 0 | OK |
| QA-02 | No empty `catch` | blocking | `rg -n --type cs 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | 0 of 0 | OK |
| QA-03 | No `#pragma warning disable` / `#nullable disable` | blocking | `rg -n --type cs '#nullable disable\|#pragma warning disable' $files` | 0 of 0 | OK |
| QA-04 | Files ≤ 300, methods ≤ 30 lines | reservation | `rg -c '^' --type cs $files` + method span check of the changed file | 1 aggravated | NOT OK (reservation): `App.xaml.cs` 462 lines (459 at base, +3; unchanged since codereview_3). `HudBackdropController.cs` 146 lines, `Apply` 15 lines. Largest feature files: `HudBackdropInterop.cs` 239, `HudContourController.cs` 233, `HudBackdropComposition.cs` 232. |
| QA-05 | ≥ 4-argument calls split across lines | reservation | `rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 0 new of 2 call hits | OK: 12 hits in total, as in codereview_3. The two in `HudBackdropController.cs` (`:45`, `:86`) are method declarations. The two call hits (`App.xaml.cs:383`, `HudContourController.cs:221`) are pre-existing. |

- `$files`: the 23 `.cs` files of the PRD 17 diff (17 new, 6 modified).
- Terrain baseline: applied from the TechSpec; `App.xaml.cs` measured at `2bc32ef`.
- Hits discounted by baseline: 4 (`App.xaml.cs` 459 lines at base; `App.xaml.cs:383`; `HudContourController.cs:221`; the pre-existing long `App.xaml.cs` methods counted as one file-level discount)
- Reservations accumulated in the feature: 1 open (QA-04 `App.xaml.cs`); 4 resolved across rounds (QA-04 `Create`, QA-04 `Synchronize`, QA-05 ×2)
- Suggested escalation: the reservation (1 < 8) and file size (462 < 500) triggers do not fire. The duplication trigger (3+ places) fires by count: the `SetWindowPos` P/Invoke is declared in 4 files (`HudShadowInterop.cs`, `WindowStyles.cs`, `WindowPlacement.cs`, `HudBackdropInterop.cs`) against 3 at base. Suggestion to the HIL only: `simplify` for a shared user32 interop. Not executed in this review.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 main HUD stays layered; separate input-transparent companion | YES | `NotchWindow.xaml` unchanged; `HudBackdropInterop.EXTENDED_STYLE` |
| DEC-02 candidate A, region from the flattened contour (variant AR) | YES | `HudBackdropWindow.Flatten` → `CreatePolygonRgn` → `SetWindowRgn` |
| DEC-03 go/no-go with HUD never activated | YES | `validation.md` T01 verdicts |
| DEC-04 translucent tint alpha ≥ 1/255 | YES | `TINT_ALPHA = 0xED` |
| DEC-05 same frame path; unowned topmost companion below the HUD | YES (minor variant) | Sibling `HudBackdropController`, recorded as a deviation in the T02 handoff |
| DEC-06 pure policy, notification-driven | YES | `HudBackdropPolicy.cs`; `HudBackdropAvailability.cs` |
| DEC-07 `HudBackdrop: { Enabled: bool? }`, merged | YES | `HudBackdropSettings.cs`; `UserSettingsFile` merge |
| DEC-08 `net10.0-windows`, hand-written WinRT ABI | YES | `HudBackdropComposition.cs`; App csproj not in the diff |
| CMP-05 `NotchWindow` swaps the fill and forwards messages | PARTIAL (equivalent) | Swap in `HudBackdropController.Apply`; hook in `HudBackdropAvailability`; same behavior |
| Flow: fill and companion swap in the same pass | YES | `Synchronize` (`:45-64`) and `Hide` (`:70-75`) unchanged |
| Errors: any failure → Solid; one log with `Candidate`, `HResult` | PARTIAL (accepted open item C at DEC-09) | Solid on every failure path; the warning carries `HResult` but no `Candidate` (`HudBackdropController.cs:106`) |
| Errors: companion and composition released on HUD close; graceful exit verified | PARTIAL | Release chain in code; no recorded exit evidence |
| Observability: one log per transition; arrange-invalid transient pair at Debug (DEC-09, narrowed by DEC-10) | YES | `Apply` (`:115-125`). `techspec.md:115` still says "Solid, then the next transition"; T07 implements the narrower DEC-10 reading (the transition right after the arrange-invalid call). DEC-10 is the governing human decision; see the optional wording note. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Verdicts in `validation.md#t01-spike-2026-10-09`; DEC-05 |
| T02 | `done/task_02.md` | COMPLETE (open evidence item) | Handoff and manual matrix; FR-03 outside clicks outside TopCenter not recorded (DEC-09 item A) |
| T03 | `codereview_1/done/task_03.md` | COMPLETE | Hide paths intact |
| T04 | `codereview_1/done/task_04.md` | COMPLETE | File unchanged |
| T05 | `codereview_2/done/task_05.md` | COMPLETE | Files unchanged since codereview_3 |
| T06 | `codereview_2/done/task_06.md` | COMPLETE | Constant and Debug pair kept; flag handling refined by T07 |
| T07 | `codereview_3/done/task_07.md` | COMPLETE | T07.1 and T07.2 `[x]`. Code matches the handoff: `afterTransient` saved, `_transient` set before the early return, `OrdinalIgnoreCase`. Only `HudBackdropController.cs` changed, as declared. Log levels verified by code inspection only, as the handoff states. |

## Executed validations

- Profile and exclusions: .NET 10 desktop (WPF App `net10.0-windows`); tests in `tests/TokenHound.Infrastructure.Tests` as an MTP executable (`global.json` `test.runner: Microsoft.Testing.Platform`). E2E omitted by .NET desktop policy.
- Validated state: worktree at HEAD `2bc32ef` with the PRD 17 uncommitted and untracked files, including T03–T07; Release; Windows 10.0.26200; executed in this review session. Both builds were incremental no-ops (≈ 1.4 s). This is valid for the current code: `TokenHound.App.dll` (15:51:32) is newer than the last edit of `HudBackdropController.cs` (15:51:02), and MSBuild checked the inputs.
- Reused evidence: `validation.md` manual matrices (T01, T02.3, T02) and DEC-08. They stay valid because round 3 changed only `Apply`'s flag handling and string comparison. The `_mode` assignment, the fill swap, the companion show/hide, geometry, region, and placement are unchanged (`HudBackdropController.cs:123-124`, `:45-75`; no other source file is newer than codereview_3).
- Manual acceptance: TC-04..08 and DEC-08 as recorded. This reviewer did not drive the desktop: toggling transparency needs a fresh human authorization (DEC-06 covered T02 only).

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal` | passed: 3 projects, 0 errors, 0 warnings, exit 0 | build of all CMPs including T07 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` | passed: 3 projects, 0 errors, 0 warnings, exit 0 | CMP-08 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` | passed: 1101 total, 1101 succeeded, 0 failed, 0 skipped, exit 0 | TC-01..03, FR-04 VM, regression |
| same with `--filter-class "*HudBackdrop*"` | passed: 19 total, 19 succeeded, exit 0 | TC-01, TC-02, TC-03, FR-04 VM |
| QA-01..05 `rg` commands over the 23 `.cs` files; method span of `Apply` | QA-01..03 clean; 1 QA-04 reservation | Quality profile |
| `sha256sum prd.md techspec.md tasks.md done/task_0*.md`; `find -newer codereview_3/codereview.md` | PRD and TechSpec match the checkpoint; only the T07 file changed in code | Source integrity, scope |

## Findings

No blocking findings in this review.

### Optional improvements (not blocking)

- TechSpec wording (new this round, Low): `techspec.md:115` says the transient pair is "Solid, then the next transition". After DEC-10, the code logs at Debug only for the arrange-invalid call's own transition and the transition on the call right after it. Aligning the sentence with DEC-10 removes the ambiguity. The code follows the governing human decision, so this is not a defect.
- Startup note (behavior by design, no action required): if the very first frame is arrange-invalid, the startup Solid and the following Material transition both log at Debug. This matches DEC-10 ("the transition right after"). `LogSettings.MinimumLevel` defaults to `"Debug"`, so nothing is dropped by default.
- `App.xaml.cs` 462 lines (459 at base; pre-existing size debt, +3 lines from this feature). Persistent.
- `Candidate` structured field on the failure warning (`HudBackdropController.cs:106`). Persistent; accepted open item C at DEC-09.
- TC-02: add a merge test with a defaults file that lacks or carries `HudBackdrop`. Persistent; accepted open item C at DEC-09.
- O-06 (pre-existing): `HudContourDecorator.BorderBrush` uses `AddOwner` without `AffectsRender`; never changed at runtime, so no current impact.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_3 optional: `StringComparison.Ordinal` vs `OrdinalIgnoreCase` | resolved | `HudBackdropController.cs:115` |
| codereview_3 optional: `_transient` survives same-mode frames | resolved | `:116-117` set the flag before the early return at `:119-120`; trace (b) in the matrix |
| codereview_3 optional: `App.xaml.cs` 462 lines | persistent | 462 lines; pre-existing debt |
| codereview_3 optional: `Candidate` log field | persistent (accepted, DEC-09 item C) | `HudBackdropController.cs:106` |
| codereview_3 optional: TC-02 merge test | persistent (accepted, DEC-09 item C) | Still only the 4 `HudBackdrop*` test files, no merge case |
| codereview_3 optional: O-06 `BorderBrush` `AddOwner` | persistent (pre-existing) | `HudContourDecorator.cs` unchanged |
| codereview_3 limitation: FR-03 outside clicks outside TopCenter | persistent (not verifiable; accepted, DEC-09 item A) | No new evidence in `validation.md` |
| codereview_1/CR-01 | resolved (still holds) | `Hide` (`:70-75`); callers `HudContourController.cs:88`, `:105`, `:119` → `HideCompanions` (`:163`) |
| codereview_1/CR-02 | resolved (still holds) | `HudBackdropInterop.cs` unchanged; `TryShow` still disposes the window on failure (`:105`) |
| codereview_2/QA-04 `Synchronize(HudContourGeometry)` | resolved (still holds) | `HudContourController.cs` unchanged since codereview_3 |

## Limitations and open items

- FR-03 / TC-05: real outside left, right, and double clicks are recorded only for TopCenter and the T02.3 docked proof. Left, Right, Top left, Top right, and Free stay `not verifiable`. This is why the status is not `REJECTED` for missing essential evidence: the human routed this gap to HIL 3 as accepted open item A (workflow DEC-09). The region is built from the same `contour.Fill` in every mode, the companion is input-transparent, and no code this depends on changed since codereview_3. HIL 3 must either supply the clicks or accept the gap.
- T07 (like T05 and T06) is verified by build, the regression suite, and code inspection only: no runtime log capture, as the handoff states.
- codereview_1 T03 manual hide/show round trip (transparency ON): still not run; needs a fresh human authorization.
- FR-06 energy saver measured at "≈ 2 s" against "within 2 s"; a precise remeasurement is advised at HIL 3.
- TC-08 / NFR-03: 60 s windows instead of 5 minutes, measured against solid mode in the same build instead of the PRD 16 build.
- Graceful exit (TechSpec Errors): disposal chain verified in code only; no recorded exit evidence.
- FR-05 on Windows 10, an unsupported Windows 11 build, and live high contrast: policy unit tests only; not executable on this machine.
- `graft build` (T02.7) not run by this reviewer: it writes `graft/`, outside the permitted outputs.
- Open thread O-04 (product note for HIL 3): with FR-05, this machine (transparency effects off) shows the solid fallback by default.
- Worktree also holds PRD 16 task-record and ROADMAP changes, plus unrelated local tooling; all excluded from scope.

## Conclusion

Correction round 3 delivers the DEC-10 scope in the one file it declares. `Apply` now compares the reason with `OrdinalIgnoreCase` and updates the transient flag on every call before the same-mode return. So only the arrange-invalid transition and the transition on the call right after it log at Debug, and a same-mode frame with another reason clears the flag. The acceptance trace holds. The fill swap, companion show/hide, and every other source file are unchanged since codereview_3, so the earlier manual evidence and the DEC-08 visual check still apply. All earlier fixes still hold. The build is clean, all 1101 tests pass (19 of them for PRD 17), and the blocking quality rules have zero hits.

The remaining items are optional or accepted: the TechSpec Observability wording vs DEC-10, the pre-existing `App.xaml.cs` size, and the accepted DEC-09 items A and C. The `SetWindowPos` duplication trigger is offered to the HIL as a suggestion. Status: `APPROVED WITH RESERVATIONS`.
