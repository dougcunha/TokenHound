# Code review report — HUD Backdrop Effects (PRD 17), re-review after correction round 1

## Summary

- Status: APPROVED WITH RESERVATIONS
- Execution: delegated reviewer
- Git scope: `2bc32ef80e0e3aff4070bcbd54a45be73ac23855..worktree` (uncommitted and untracked; HEAD = base). Scope limited to the PRD 17 files: 9 modified tracked files under `src/` and `tests/`, 18 untracked PRD 17 files (14 `src`, 4 `tests`), plus the docs hunks in `ARCHITECTURE.md`, `README.md`, `docs/ROADMAP.md`.
- Previous review: `tasks/prd-17-hud-backdrop-effects/codereview_1/codereview.md` (REJECTED: CR-01, CR-02)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-17-hud-backdrop-effects/prd.md` | read; SHA-256 `abbe9a6d…` matches checkpoint `approved_sources` (DEC-07) |
| TechSpec | `tasks/prd-17-hud-backdrop-effects/techspec.md` | read; SHA-256 `538d032b…` matches checkpoint (DEC-07) |
| Manifest | `tasks/prd-17-hud-backdrop-effects/tasks.md` | read; SHA-256 `ea60c826…`, the same as at codereview_1. It differs from approved `999f4d2c…` only in State, links, and Problems, which are execution records (workflow Events). |
| Feature tasks | `done/task_01.md`, `done/task_02.md` | read; links resolve; last changed before codereview_1 (13:58, 15:13 vs report 15:22) |
| Correction tasks | `codereview_1/done/task_03.md` (CR-01, QA-05), `codereview_1/done/task_04.md` (CR-02, QA-04) | read; all Work items checked; handoffs filled. No correction manifest, as `sdd-plan-corrections` step 4 prescribes. |
| Workflow, validation, checkpoint | `workflow.md` (DEC-01..08, round-1 events), `validation.md`, `checkpoint.json` (gen 14, `phase: review`, `correction_round: 1`) | read |
| Snapshot | `context-snapshot.md` | The whole file was read in one call (< 8 KiB), but only the header, next step brief, Open threads (O-04, O-06, O-07), and on-run entries (L-03, L-05) were used. Decisions (D-02) and Code map (M-01) were not used as evidence. Header valid: `git_head` 2bc32ef = HEAD; `covers_through` (T03, T04 in `codereview_1/done/`) matches the corrections folder; the worktree matches its description. No suspect entries. |
| Implementation | `git diff 2bc32ef` + untracked PRD 17 files | delimited. `find -newer codereview_1/codereview.md` over `src`, `tests`, `docs`, `README.md`, `ARCHITECTURE.md` lists only `HudBackdropInterop.cs`, `HudBackdropController.cs`, `HudContourController.cs`: the files T03/T04 declare. Every other file is in the state codereview_1 and DEC-08 judged. Unrelated tooling (`.agents/`, `.codex/`, `.context-brake/`, `context-brake.config.json`) and PRD 16 task records are out of scope. |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 / FR-01 / US-01 | Acrylic in all six docking modes | `HudBackdropWindow`, `HudBackdropComposition`, `HudBackdropController.Synchronize` | TC-04/TC-05 manual; DEC-08 | conformant | `validation.md` T02 matrix (TopCenter, Left, Free); DEC-08 human visual check (Top left, Top right, Right edge, 150 % primary). Unchanged since codereview_1. |
| FR-02 | Material only inside the contour | `HudBackdropWindow.Flatten` + `HudBackdropInterop.SetRegion` from `contour.Fill` | TC-05 | conformant | `HudContourController.cs:149-154` passes `contour.Fill`; `t02-*-zoom4x.png`; T02.3 region proof |
| FR-03 | Outside left/right/double clicks reach the app behind | Companion `WS_EX_TRANSPARENT` + `HTTRANSPARENT` (`HudBackdropInterop.cs:14-18,169-180`), region from the same contour in every mode | TC-03 unit; TC-05 manual | conformant (TopCenter, T02.3 docked proof) / **not verifiable** (Left, Right, Top left, Top right, Free) | Evidence state unchanged since codereview_1 (`validation.md:70,78`). See limitations: held as a required HIL 3 item, not a rejection trigger. |
| FR-04 / US-02 | Toggle in General, default on, immediate, persisted, missing section reads on | `HudBackdropSettings`, `HudBackdropStore`, `HudBackdropSettingsViewModel`, `HudBackdropSettingsCard`, `SettingsWindow.xaml:93`, `UserSettingsFile.cs:183` | `HudBackdropStoreTests`, `HudBackdropSettingsViewModelTests`; TC-06 | conformant | Tests pass in this session; TC-06 solid within 17 ms, persisted |
| FR-05 / US-03 | Fallback to exactly `#18181B` | `HudBackdropPolicy.Resolve`; `HudBackdropController.Apply`/`Hide` | `HudBackdropPolicyTests`; TC-06 | conformant | CR-01 resolved: every hide path applies the solid brush (`HudBackdropController.cs:66-71`). Windows 10 and unsupported builds: policy unit tests only. |
| FR-06 | Live availability switch ≤ 2 s | `HudBackdropAvailability` (WM_SETTINGCHANGE, power-setting notifications, `SystemParameters.StaticPropertyChanged`) | TC-01; TC-06 | conformant (see limitations) | Transparency < 1.5 s; energy saver ≈ 2 s (OS notification latency; `HudBackdropAvailability.cs:91-123` adds no delay) |
| FR-07 | Material follows size, mode, drag, DPI, monitor | `HudContourController.Synchronize` → `HudBackdropWindow.Synchronize` | TC-05 | conformant | Cross-monitor drag evidence; DEC-08 sizes. T03 changed no geometry, region, or placement code. |
| OBJ-02 / NFR-01 | No focus steal; main-window placement rule unchanged | Companion `MA_NOACTIVATE`, `WS_EX_NOACTIVATE`; `NotchWindow`/`WindowStyles` not in the diff | `HudBackdropStyleTests`; TC-05 | conformant | `HudBackdropInterop.cs:172`; drag never activated the HUD |
| NFR-02 | Icons ≥ 4.5:1, arcs ≥ 3:1 (DEC-07) | `TINT_ALPHA = 0xED` (`HudBackdropController.cs:19`) | TC-07 | conformant | Icons 13.37:1; lowest arc 3.04:1 over white |
| NFR-03 | No polling; idle within 10 % | Event-driven; no timers in new files | TC-08 | conformant (see limitations) | CPU 7.78 s vs 8.36 s / 60 s; working set 182.4 vs 182.5 MB |
| NFR-04 | Undocumented API isolated, fail closed | Documented candidate A only; `TryShow` catches and falls back (`HudBackdropController.cs:82-106`) | TC-01 | conformant | Native creation failure now also releases the HWND (CR-02 resolved) |
| NFR-05 | Core untouched; JSON section pattern | `HudBackdropStore` mirrors `HudSizeStore` | TC-02 | conformant | `git diff --stat 2bc32ef` lists no `TokenHound.Core` path |
| PD-01..04 | Acrylic, default on, silent fallback, spike decides | As above; PD-04 resolved at DEC-05 | — | conformant | `workflow.md` DEC-05 |
| TC-01..03 | Policy, store, companion styles | — | `HudBackdrop*` tests (19) | conformant | 19/19 pass in this session |
| TC-05 | Full matrix (modes × sizes × DPIs, clicks, menu) | — | manual + DEC-08 | partially verified | Outside clicks only in TopCenter (FR-03) |
| TC-06..08 | Toggle and live availability, contrast, idle | — | manual | conformant with deviations | `validation.md` T02 matrix |
| T03 (CR-01) | Every companion hide path shows the solid fill; no re-tint after a shadow failure; next Material frame restores the tint | `HudBackdropController.Hide(string)`; `HudContourController.HideCompanions(string)` | build + regression suite; code inspection | conformant (code); manual round trip not verifiable | `HudContourController.cs:88,105,119` → `:157-162` → `HudBackdropController.cs:66-71`; `_failed` early returns at `HudContourController.cs:84,99` |
| T04 (CR-02) | Destroy the HWND when the backdrop opt-in fails; `Create` ≤ 30 lines | `HudBackdropInterop.EnableHostBackdrop` | build + regression suite; code inspection | conformant | `HudBackdropInterop.cs:151-167`: `DestroyWindow` before `Marshal.ThrowExceptionForHR`; `Create` spans 26 lines (`:63-88`) |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (CLAUDE.md) | OK | No `src/TokenHound.Core` change |
| `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` on HUD windows | OK | `HudBackdropInterop.cs:172` |
| `EnableNonActivating` uses `SWP_NOZORDER` without `SWP_SHOWWINDOW` | OK | `WindowStyles.cs` not in the diff; the raw companion's own `Place` (`HudBackdropInterop.cs:91-106`) is outside the rule |
| One class per file, sealed by default | OK | New types are `sealed`, `static`, a record, or an enum |
| XML docs on public members | OK | New public/internal APIs documented |
| File-scoped namespace, alphabetized usings | OK | `using Serilog;` before `using System;` (`HudBackdropController.cs:1-2`, `HudBackdropSettingsViewModel.cs:1-2`) is ordinal-alphabetical ("Se" < "Sy"). codereview_1 marked this NOT OK; that judgment is not carried forward. |
| Braces, blank lines, `=>` placement in changed code | OK | `HudBackdropController.cs`, `HudContourController.cs:157-162`, `HudBackdropInterop.cs:151-167` |
| ≥ 4-argument calls split | OK | The two T03 splits are in place (`HudBackdropController.cs:48-53`, `:125-132`) |
| `dotnet-efficient-validation` / MTP | OK | Builds, then `dotnet run --no-build --no-restore -- --minimum-expected-tests 1` (runner: MTP via `global.json`, executable route) |
| E2E | N/A | Omitted by .NET desktop policy (TechSpec Test approach) |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void` / `.Result` / `.Wait()` | blocking | `rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 of 0 | OK |
| QA-02 | No empty `catch` | blocking | `rg -n --type cs 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | 0 of 0 | OK |
| QA-03 | No `#pragma warning disable` / `#nullable disable` | blocking | `rg -n --type cs '#nullable disable\|#pragma warning disable' $files` | 0 of 0 | OK |
| QA-04 | Files ≤ 300, methods ≤ 30 lines | reservation | `rg -c '^' --type cs $files` + method span scan (signature to closing brace, inclusive, the convention codereview_1 used) | 2 new or aggravated | NOT OK (reservation): `App.xaml.cs` 462 lines (459 at base, +3); `HudContourController.Synchronize(HudContourGeometry)` spans 32 lines (`HudContourController.cs:124-155`; 24 at base), not flagged by codereview_1. `HudBackdropInterop.Create` is resolved (26 lines). Three `App.xaml.cs` methods above 30 lines (`:47-78`, `:190-228`, `:401-432`) are pre-existing; the feature's +3 lines fall outside them. |
| QA-05 | ≥ 4-argument calls split across lines | reservation | `rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 0 new of 2 call hits | OK: both codereview_1 hits are split. Remaining call hits: `App.xaml.cs:383` (pre-existing) and `HudContourController.cs:215` (3 arguments, nested-comma match, pre-existing). The other 10 hits are method, delegate, or extern declarations. |

- `$files`: the 23 `.cs` files of the PRD 17 diff (17 new, 6 modified).
- Terrain baseline: applied from the TechSpec; `App.xaml.cs` and the base `Synchronize` span measured from `git show 2bc32ef`.
- Hits discounted by baseline: 4 (`App.xaml.cs` 459 lines at base; `App.xaml.cs:383`; `HudContourController.cs:215`; three pre-existing long `App.xaml.cs` methods counted as one file-level discount)
- Reservations accumulated in the feature: 2 open (QA-04: 2); 3 from codereview_1 resolved (QA-04 `Create`, QA-05 ×2)
- Suggested escalation: the reservation (2 < 8) and file size (462 < 500) triggers do not fire. The duplication trigger (3+ places) still fires by count: the `SetWindowPos` P/Invoke appears in 4 files (`HudShadowInterop.cs`, `WindowStyles.cs`, `WindowPlacement.cs`, `HudBackdropInterop.cs:206`) against 3 at base. As an HIL suggestion only: `simplify` for a shared user32 interop. The pattern was at the threshold before this feature.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 main HUD stays layered; separate input-transparent companion | YES | `NotchWindow.xaml` unchanged; `HudBackdropInterop.EXTENDED_STYLE` |
| DEC-02 candidate A, region from the flattened contour (variant AR) | YES | `HudBackdropWindow.Flatten` → `CreatePolygonRgn` WINDING → `SetWindowRgn` |
| DEC-03 go/no-go with HUD never activated | YES | `validation.md` T01 verdicts |
| DEC-04 translucent tint alpha ≥ 1/255 | YES | `TINT_ALPHA = 0xED` |
| DEC-05 same frame path; unowned topmost companion below the HUD | YES (minor variant, as in codereview_1) | `HudContourController.cs:149-154`; `Place` uses `SWP_NOACTIVATE \| SWP_SHOWWINDOW`; sibling `HudBackdropController` recorded as a deviation in the T02 handoff |
| DEC-06 pure policy, notification-driven | YES | `HudBackdropPolicy.cs`; `HudBackdropAvailability.cs` |
| DEC-07 `HudBackdrop: { Enabled: bool? }`, merged | YES | `HudBackdropSettings.cs`; `UserSettingsFile.cs:183` |
| DEC-08 `net10.0-windows`, hand-written WinRT ABI | YES | `HudBackdropComposition.cs`; App csproj not in the diff |
| CMP-05 `NotchWindow` swaps fill and forwards messages | PARTIAL (equivalent) | Swap in `HudBackdropController.Apply`; hook in `HudBackdropAvailability`; same behavior |
| Flow: fill and companion swap in the same pass; no frame with tint and no blur | YES | `Synchronize` places the companion, then applies Material in the same call (`HudBackdropController.cs:47-59`); `Hide` hides, then applies Solid (`:66-71`). Both are synchronous on the dispatcher thread. |
| Errors: any failure → Solid; one log with `Candidate`, `HResult` | PARTIAL (optional) | Solid on every failure path; warning carries `HResult` but no `Candidate` field (`HudBackdropController.cs:102`) |
| Errors: companion and composition released on HUD close; graceful exit verified | PARTIAL | Release chain in code (`HudContourController.Dispose` → `HudBackdropController.Dispose` → `HudBackdropWindow.Dispose`); creation-failure leak closed by T04; no recorded exit evidence |
| Observability: one log per transition with the deciding input | YES | `HudBackdropController.cs:116`; hide paths now log `Hidden`, `ArrangeInvalid`, `ShadowFailure` |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Verdicts and evidence in `validation.md#t01-spike-2026-10-09`; DEC-05 |
| T02 | `done/task_02.md` | COMPLETE (open evidence item) | Handoff, 1101 tests, manual matrix; FR-03 outside clicks outside TopCenter not recorded |
| T03 | `codereview_1/done/task_03.md` | COMPLETE | T03.1–T03.3 checked; code matches the handoff (`HudBackdropController.cs` 137 lines, `HudContourController.cs` 227 lines). Its manual hide/show round trip was not run; the task's Verification section marks it optional without a new authorization. |
| T04 | `codereview_1/done/task_04.md` | COMPLETE | T04.1–T04.2 checked; `Create` 26 lines; file 239 lines |

## Executed validations

- Profile and exclusions: .NET 10 desktop (WPF App `net10.0-windows`); tests in `tests/TokenHound.Infrastructure.Tests` as an MTP executable (`global.json` `test.runner: Microsoft.Testing.Platform`). E2E omitted by .NET desktop policy.
- Validated state: worktree at HEAD `2bc32ef` with PRD 17 uncommitted and untracked files including T03 and T04, Release, Windows 10.0.26200, executed in this review session.
- Reused evidence: `validation.md` manual matrices (T01, T02.3, T02) and DEC-08. They remain valid for the unchanged files; for the three changed files, T03/T04 alter only the fill on hide paths and the failure-path HWND release, not geometry, region, placement, or the success path the matrices exercised.
- Manual acceptance: TC-04..08 and DEC-08 as recorded. This reviewer did not drive the desktop: toggling transparency needs a fresh human authorization (DEC-06 covered T02 only).

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo --verbosity:minimal` | passed: 0 errors, 0 warnings, exit 0 | build of all CMPs incl. T03/T04 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo --verbosity:minimal` | passed: 0 errors, 0 warnings, exit 0 | CMP-08 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` | passed: 1101 total, 1101 succeeded, 0 failed, 0 skipped, exit 0 | TC-01..03, FR-04 VM, regression |
| same with `--filter-class "*HudBackdrop*"` | passed: 19 total, 19 succeeded, exit 0 | TC-01, TC-02, TC-03, FR-04 VM |
| QA-01..05 `rg` commands and method span scan (above) | QA-01..03 clean; 2 QA-04 reservations | Quality profile |
| `sha256sum prd.md techspec.md tasks.md` | PRD/TechSpec match checkpoint; tasks.md unchanged since codereview_1 | Source integrity |

## Findings

No blocking findings in this review.

### Optional improvements (not blocking)

- QA-04: `HudContourController.Synchronize(HudContourGeometry)` spans 32 lines (`HudContourController.cs:124-155`); extracting the shadow part (`:129-147`) into a helper would bring it under 30. `App.xaml.cs` grew from 459 to 462 lines (pre-existing size debt).
- Add the `Candidate` structured field to the failure warning (`HudBackdropController.cs:102`) to match the TechSpec Errors text (persistent from codereview_1).
- `HudBackdropSettingsCard.xaml:32` description names transparency effects and energy saver but not high contrast, which the policy and README include (persistent).
- `docs/ROADMAP.md:72` PRD 17 row still says "Awaiting the visual check"; DEC-08 approved it (persistent).
- TC-02: add a merge test with a defaults file that lacks or carries `HudBackdrop` (persistent).
- O-06 (pre-existing): `HudContourDecorator.BORDER_BRUSH_PROPERTY` uses `AddOwner` without `AffectsRender`; no runtime change, no current impact.
- The arrange-invalid path now logs two Information transitions (`Solid (ArrangeInvalid)`, then `Material`) per invalid frame. That is the TechSpec Flow behavior; it is not recurring at idle (TC-08), but it may add log lines during resize.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_1/CR-01 | resolved | `HudBackdropController.Hide(string)` hides the companion and calls `Apply(HudBackdropMode.Solid, reason)` (`HudBackdropController.cs:66-71`). All three hide callers in `HudContourController` (`:88` Hidden, `:105` ArrangeInvalid, `:119` ShadowFailure) go through `HideCompanions` (`:157-162`). After a shadow failure, `_failed` returns early at `:84` and `:99`, so no later frame re-applies the tint. The next successful Material frame re-applies the tint in the same pass (`HudBackdropController.cs:47-59`). Code inspection; the `Win32Exception` cannot be injected. |
| codereview_1/CR-02 | resolved | `HudBackdropInterop.EnableHostBackdrop` calls `DestroyWindow(handle)` before `Marshal.ThrowExceptionForHR(result)` (`HudBackdropInterop.cs:151-167`). A later failure in `HudBackdropComposition.Create` is covered by `TryShow` → `_window.Dispose()` (`HudBackdropController.cs:101`), which destroys the stored handle. |
| codereview_1/QA-04 (`HudBackdropInterop.Create` 33 lines) | resolved | 26 lines (`HudBackdropInterop.cs:63-88`) |
| codereview_1/QA-04 (`App.xaml.cs` 462 lines) | persistent | 462 lines (459 at base) |
| codereview_1/QA-05 (`HudBackdropController.cs:47`, `:112`) | resolved | Split one argument per line (`:48-53`, `:125-132`) |
| codereview_1 "alphabetized usings NOT OK" | not carried forward | `Serilog` < `System` in ordinal order; compliant |
| codereview_1 FR-03 limitation (outside clicks outside TopCenter) | persistent (not verifiable) | No new evidence in `validation.md` (last changed 15:09, before codereview_1) |

## Limitations and open items

- FR-03 / TC-05: real outside left, right, and double clicks are recorded only for TopCenter plus the T02.3 docked proof; Left, Right, Top left, Top right, and Free stay `not verifiable`. Decision for this status: this is a required HIL 3 item, not a rejection trigger. The evidence state is the same one codereview_1 classed as non-blocking; the region is built from the same `contour.Fill` in every mode and the companion is input-transparent; and no correction task could produce it without a new desktop authorization. codereview_1's conclusion conditioned approval on this evidence; this review does not adopt that condition and records the gap for the human instead. HIL 3 must either supply the clicks or accept the gap.
- T03 manual hide/show round trip (transparency ON): not run; needs a fresh human authorization. Covered by code inspection only.
- FR-06 energy saver measured at "≈ 2 s" against "within 2 s"; precise remeasurement advised at HIL 3.
- TC-08 / NFR-03: 60 s windows instead of 5 minutes, against solid mode in the same build instead of the PRD 16 build.
- Graceful exit (TechSpec Errors): disposal chain verified in code only; no recorded exit evidence.
- FR-05 on Windows 10, an unsupported Windows 11 build, and live high contrast: policy unit tests only; not executable on this machine.
- `graft build` (T02.7) not run by this reviewer: it writes `graft/`, outside the permitted outputs.
- Open thread O-04 (product note for HIL 3): with FR-05, this machine (transparency effects off) shows the solid fallback by default.
- Worktree also holds PRD 16 task-record changes and unrelated local tooling; both excluded from scope.

## Conclusion

Both codereview_1 findings are resolved in code: every companion hide path now restores the solid `#18181B` fill, and a failed host-backdrop opt-in destroys its HWND. The T03/T04 changes are confined to the three declared files and do not touch geometry, region, or placement, so the earlier manual evidence and the DEC-08 visual check still apply. The build is clean, 1101 tests pass (19 PRD 17), and the blocking quality rules have zero hits. Two QA-04 reservations remain, and the optional items above are carried forward. Outside-click evidence for five docking modes is still missing. It is recorded as a required HIL 3 item and is not counted as a non-conformance. Status: `APPROVED WITH RESERVATIONS`.
