# Code review report — HUD Backdrop Effects (PRD 17), re-review after correction round 2

## Summary

- Status: APPROVED WITH RESERVATIONS
- Execution: delegated reviewer
- Git scope: `2bc32ef80e0e3aff4070bcbd54a45be73ac23855..worktree` (uncommitted and untracked; HEAD = base). Scope is limited to the PRD 17 files: 9 modified tracked files under `src/` and `tests/`, 18 untracked PRD 17 files (14 `src`, 4 `tests`), and the PRD 17 hunks in `ARCHITECTURE.md`, `README.md`, and `docs/ROADMAP.md`.
- Previous review: `tasks/prd-17-hud-backdrop-effects/codereview_2/codereview.md` (APPROVED WITH RESERVATIONS; corrections T05, T06 in `codereview_2/done/` under workflow DEC-09)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-17-hud-backdrop-effects/prd.md` | read; SHA-256 `abbe9a6d…` matches checkpoint `approved_sources` (DEC-07) |
| TechSpec | `tasks/prd-17-hud-backdrop-effects/techspec.md` | read; SHA-256 `7188433e…` matches checkpoint (DEC-09). Observability line amended for the arrange-invalid transient pair (`techspec.md:115`). |
| Manifest | `tasks/prd-17-hud-backdrop-effects/tasks.md` | read; SHA-256 `ea60c826…`, unchanged since codereview_1. It differs from the approved `999f4d2c…` only in State, links, and Problems, which are execution records. |
| Feature tasks | `done/task_01.md`, `done/task_02.md` | read; links resolve; both last changed before codereview_1. `done/task_02.md` (`97ec5638…`) differs from the DEC-05 hash `34e8d514…` because the Handoff was filled during execution. That is execution-record drift, not a contract change. |
| Correction tasks | `codereview_1/done/task_03.md`, `task_04.md` (round 1); `codereview_2/done/task_05.md` (DEC-09 B), `task_06.md` (DEC-09 D) | read; all Work items checked; handoffs filled |
| Workflow, validation, checkpoint | `workflow.md` (DEC-01..09, round-2 events), `validation.md`, `checkpoint.json` (gen 16, `phase: review`, `correction_round: 2`, `active_work` reviewer for `codereview_3`) | read |
| Snapshot | `context-snapshot.md` | Read whole (< 8 KiB). Only the header, next step brief, Open threads (O-04, O-06, O-07), and on-run entries (L-03, L-05) were used; D-02 and M-01 were not used as evidence. Header valid: `git_head` 2bc32ef = HEAD; `covers_through` (T05, T06 in `codereview_2/done/`) matches the corrections folder; the worktree matches its description. Stale hint: the next step brief names `codereview_1/` as the previous review; the header and the caller give `codereview_2`. No impact. |
| Implementation | `git diff 2bc32ef` + untracked PRD 17 files | delimited. `find -newer codereview_2/codereview.md` over `src`, `tests`, `docs`, `README.md`, `ARCHITECTURE.md` lists only `README.md`, `docs/ROADMAP.md`, `HudBackdropSettingsCard.xaml`, `HudBackdropController.cs`, and `HudContourController.cs`: the files T05 and T06 declare. Everything else is in the state codereview_2 judged. PRD 16 hunks in `docs/ROADMAP.md`, PRD 16 task records, and unrelated tooling (`.agents/`, `.codex/`, `.context-brake/`, `context-brake.config.json`) are out of scope. |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01 / FR-01 / US-01 | Acrylic in all six docking modes | `HudBackdropWindow`, `HudBackdropComposition`, `HudBackdropController.Synchronize` | TC-04/TC-05 manual; DEC-08 | conformant | `validation.md` T02 matrix (TopCenter, Left, Free); DEC-08 human visual check (Top left, Top right, Right edge, 150 % primary). T05/T06 changed no geometry, region, or placement code. |
| FR-02 | Material only inside the contour | `HudBackdropWindow.Flatten` + `HudBackdropInterop.SetRegion` from `contour.Fill` | TC-05 | conformant | `HudContourController.cs:131-136` still passes `contour.Fill` with the unit-scale transform; `t02-*-zoom4x.png`; T02.3 region proof |
| FR-03 | Outside left/right/double clicks reach the app behind | Companion `WS_EX_TRANSPARENT` + `HTTRANSPARENT`; region from the same contour in every mode | TC-03 unit; TC-05 manual | conformant (TopCenter, T02.3 docked proof) / **not verifiable** (Left, Right, Top left, Top right, Free) | Evidence unchanged (`validation.md` last changed 15:09). Accepted HIL 3 open item A at DEC-09; see limitations. |
| FR-04 / US-02 | Toggle in General, default on, immediate, persisted, missing section reads on | `HudBackdropSettings`, `HudBackdropStore`, `HudBackdropSettingsViewModel`, `HudBackdropSettingsCard`, `SettingsWindow.xaml`, `UserSettingsFile.cs:183` | `HudBackdropStoreTests`, `HudBackdropSettingsViewModelTests`; TC-06 | conformant | Tests pass in this session; TC-06 solid within 17 ms, persisted |
| FR-05 / US-03 | Fallback to exactly `#18181B` | `HudBackdropPolicy.Resolve`; `HudBackdropController.Synchronize`/`Hide` | `HudBackdropPolicyTests`; TC-06 | conformant | `Hide` still hides the companion, then applies Solid (`HudBackdropController.cs:70-75`); T06 changed only the log level in `Apply` (`:112-124`). Windows 10 and unsupported builds: policy unit tests only. |
| FR-06 | Live availability switch ≤ 2 s | `HudBackdropAvailability` | TC-01; TC-06 | conformant (see limitations) | Transparency < 1.5 s; energy saver ≈ 2 s. File unchanged since codereview_2. |
| FR-07 | Material follows size, mode, drag, DPI, monitor | `HudContourController.Synchronize(HudContourGeometry)` → `SynchronizeShadow` then `HudBackdropController.Synchronize` | TC-05 | conformant | T05 extraction keeps the order: shadow bounds (`:145-146`), geometry (`:151-157`), visibility (`:159-160`), then the backdrop (`:131-136`), on the same conditions. |
| OBJ-02 / NFR-01 | No focus steal; main-window placement rule unchanged | Companion `MA_NOACTIVATE`, `WS_EX_NOACTIVATE`; `NotchWindow`/`WindowStyles` not in the diff | `HudBackdropStyleTests`; TC-05 | conformant | Unchanged since codereview_2 |
| NFR-02 | Icons ≥ 4.5:1, arcs ≥ 3:1 (DEC-07) | `TINT_ALPHA = 0xED` (`HudBackdropController.cs:22`) | TC-07 | conformant | Icons 13.37:1; lowest arc 3.04:1 over white |
| NFR-03 | No polling; idle within 10 % | Event-driven; no timers in new files | TC-08 | conformant (see limitations) | CPU 7.78 s vs 8.36 s / 60 s; working set 182.4 vs 182.5 MB |
| NFR-04 | Undocumented API isolated, fail closed | Documented candidate A only; `TryShow` catches, disposes, and falls back (`HudBackdropController.cs:86-110`) | TC-01 | conformant | Unchanged by T06 |
| NFR-05 | Core untouched; JSON section pattern | `HudBackdropStore` mirrors `HudSizeStore` | TC-02 | conformant | No `src/TokenHound.Core` path in `git diff --stat 2bc32ef` |
| PD-01..04 | Acrylic, default on, silent fallback, spike decides | As above; PD-04 resolved at DEC-05 | — | conformant | `workflow.md` DEC-05 |
| TC-01..03 | Policy, store, companion styles | — | `HudBackdrop*` tests (19) | conformant | 19/19 pass in this session |
| TC-05 | Full matrix (modes × sizes × DPIs, clicks, menu) | — | manual + DEC-08 | partially verified | Outside clicks only in TopCenter (FR-03) |
| TC-06..08 | Toggle and live availability, contrast, idle | — | manual | conformant with deviations | `validation.md` T02 matrix |
| T05 (DEC-09 B) | `Synchronize(HudContourGeometry)` ≤ 30 lines, same behavior; ROADMAP row; card text names high contrast | `HudContourController.cs:124-161`; `docs/ROADMAP.md:72`; `HudBackdropSettingsCard.xaml:32`; `README.md:24` | build + suite; code inspection | conformant | `Synchronize` spans 14 lines (`:124-137`), `SynchronizeShadow` 23 (`:139-161`), file 233 lines. ROADMAP row: "Visual check and independent review done (approved with reservations); awaiting acceptance". Card: "…when Windows transparency effects are off, or when energy saver or high contrast is on", matching `HudBackdropPolicy.Reason` (`HudBackdropPolicy.cs:25-34`). README now states the policy correctly ("are off, or while energy saver or high contrast is on"); recorded as a same-cause scope extension in the T05 handoff. |
| T06 (DEC-09 D) | Arrange-invalid Solid and the next transition log at Debug; one shared reason constant; behavior unchanged | `HudBackdropController.ARRANGE_INVALID_REASON` (`:21`), `Apply` (`:112-124`), `HudContourController.cs:105` | build + suite; code inspection | conformant | `Apply` returns before touching `_transient` on a same-mode call (`:115-116`), so a Solid→Solid frame does not set or clear the flag. The arrange-invalid transition sets `_transient` and logs at Debug; the next transition logs at Debug and clears it (`:118-121`). No other literal `"ArrangeInvalid"` exists under `src/`. Fill assignment unchanged (`:122`). |
| codereview_1 T03 (CR-01) | Every hide path shows the solid fill | `HudContourController.HideCompanions` (`:163-168`) → `HudBackdropController.Hide` | code inspection | conformant | All three callers intact: `:88` Hidden, `:105` ArrangeInvalid, `:119` ShadowFailure |
| codereview_1 T04 (CR-02) | Destroy the HWND on a failed opt-in; `Create` ≤ 30 lines | `HudBackdropInterop.EnableHostBackdrop` | code inspection | conformant | File unchanged since codereview_2 |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (CLAUDE.md) | OK | No `src/TokenHound.Core` change |
| `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` on HUD windows | OK | `HudBackdropInterop.cs` unchanged since codereview_2 |
| `EnableNonActivating` uses `SWP_NOZORDER` without `SWP_SHOWWINDOW` | OK | `WindowStyles.cs` not in the diff |
| One class per file, sealed by default | OK | No new types in round 2 |
| XML docs on public and internal members | OK | `ARRANGE_INVALID_REASON` documented (`HudBackdropController.cs:20`); `Hide` doc updated (`:66-69`) |
| `UPPER_CASE` constants; no duplicated literal for the shared reason | OK | `ARRANGE_INVALID_REASON` (`:21`); used at `HudContourController.cs:105` |
| String comparison `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` | NOT OK (optional) | New in T06: `string.Equals(reason, ARRANGE_INVALID_REASON, StringComparison.Ordinal)` (`HudBackdropController.cs:118`). CLAUDE.md prescribes `OrdinalIgnoreCase`, and `HudBackdropPolicy.cs:18` follows it. No functional effect: both sides come from code constants. |
| Braces, blank lines, `=>` placement, ≥ 4-argument call splits in changed code | OK | `HudContourController.cs:124-168`; `HudBackdropController.cs:112-124` |
| Alphabetized usings | OK | `Serilog`, `Serilog.Events`, `System…` in ordinal order (`HudBackdropController.cs:1-9`) |
| Structured logging with typed arguments | OK | `Log.Write(level, "HUD background switched to {Mode} ({Reason})", mode, reason)` (`:123`) |
| `dotnet-efficient-validation` / MTP | OK | Builds, then `dotnet run --no-build --no-restore -- --minimum-expected-tests 1` (runner: MTP via `global.json`, executable route) |
| E2E | N/A | Omitted by .NET desktop policy (TechSpec Test approach) |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void` / `.Result` / `.Wait()` | blocking | `rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 of 0 | OK |
| QA-02 | No empty `catch` | blocking | `rg -n --type cs 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | 0 of 0 | OK |
| QA-03 | No `#pragma warning disable` / `#nullable disable` | blocking | `rg -n --type cs '#nullable disable\|#pragma warning disable' $files` | 0 of 0 | OK |
| QA-04 | Files ≤ 300, methods ≤ 30 lines | reservation | `rg -c '^' --type cs $files` + method span scan (signature to closing brace, inclusive) | 1 aggravated | NOT OK (reservation): `App.xaml.cs` 462 lines (459 at base, +3). `HudContourController.Synchronize(HudContourGeometry)` resolved (14 lines). The only methods above 30 lines are pre-existing `App.xaml.cs:47-78`, `:190-228`, `:401-432`; the feature's 3 lines (`:34`, `:367`, `:382`) are outside them. Largest feature files: `HudBackdropInterop.cs` 239, `HudContourController.cs` 233, `HudBackdropComposition.cs` 232. |
| QA-05 | ≥ 4-argument calls split across lines | reservation | `rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 0 new of 2 call hits | OK: `App.xaml.cs:383` and `HudContourController.cs:221` (3 arguments, nested-comma match, in unchanged `UpdateShadow`) are pre-existing. The other 10 hits are method, delegate, or extern declarations. |

- `$files`: the 23 `.cs` files of the PRD 17 diff (17 new, 6 modified).
- Terrain baseline: applied from the TechSpec; `App.xaml.cs` and its long methods measured at `2bc32ef`.
- Hits discounted by baseline: 4 (`App.xaml.cs` 459 lines at base; `App.xaml.cs:383`; `HudContourController.cs:221`; the three pre-existing long `App.xaml.cs` methods counted as one file-level discount)
- Reservations accumulated in the feature: 1 open (QA-04 `App.xaml.cs`); 4 resolved across rounds (QA-04 `Create`, QA-04 `Synchronize`, QA-05 ×2)
- Suggested escalation: the reservation (1 < 8) and file size (462 < 500) triggers do not fire. The duplication trigger (3+ places) fires by count: the `SetWindowPos` P/Invoke is declared in 4 files (`HudShadowInterop.cs:85`, `WindowStyles.cs:97`, `WindowPlacement.cs:229`, `HudBackdropInterop.cs:206`) against 3 at base. Suggestion to the HIL only: `simplify` for a shared user32 interop. The pattern was already at the threshold before this feature.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 main HUD stays layered; separate input-transparent companion | YES | `NotchWindow.xaml` unchanged; `HudBackdropInterop.EXTENDED_STYLE` |
| DEC-02 candidate A, region from the flattened contour (variant AR) | YES | `HudBackdropWindow.Flatten` → `CreatePolygonRgn` WINDING → `SetWindowRgn` |
| DEC-03 go/no-go with HUD never activated | YES | `validation.md` T01 verdicts |
| DEC-04 translucent tint alpha ≥ 1/255 | YES | `TINT_ALPHA = 0xED` |
| DEC-05 same frame path; unowned topmost companion below the HUD | YES (minor variant, as in codereview_1/2) | Shadow then backdrop in the same frame (`HudContourController.cs:124-137`); sibling `HudBackdropController` recorded as a deviation in the T02 handoff |
| DEC-06 pure policy, notification-driven | YES | `HudBackdropPolicy.cs`; `HudBackdropAvailability.cs` |
| DEC-07 `HudBackdrop: { Enabled: bool? }`, merged | YES | `HudBackdropSettings.cs`; `UserSettingsFile.cs:183` |
| DEC-08 `net10.0-windows`, hand-written WinRT ABI | YES | `HudBackdropComposition.cs`; App csproj not in the diff |
| CMP-05 `NotchWindow` swaps the fill and forwards messages | PARTIAL (equivalent) | Swap in `HudBackdropController.Apply`; hook in `HudBackdropAvailability`; same behavior |
| Flow: fill and companion swap in the same pass | YES | `Synchronize` places or hides the companion, then applies the mode in the same call (`HudBackdropController.cs:45-64`); `Hide` hides, then applies Solid (`:70-75`) |
| Errors: any failure → Solid; one log with `Candidate`, `HResult` | PARTIAL (accepted open item C at DEC-09) | Solid on every failure path; the warning carries `HResult` but no `Candidate` field (`HudBackdropController.cs:106`) |
| Errors: companion and composition released on HUD close; graceful exit verified | PARTIAL | Release chain in code (`HudContourController.Dispose` → `HudBackdropController.Dispose` → `HudBackdropWindow.Dispose`); no recorded exit evidence |
| Observability: one log per transition with the deciding input; transient arrange-invalid pair at Debug (DEC-09) | YES | `HudBackdropController.cs:118-123`; see the optional note on the flag carry-over |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Verdicts in `validation.md#t01-spike-2026-10-09`; DEC-05 |
| T02 | `done/task_02.md` | COMPLETE (open evidence item) | Handoff, manual matrix; FR-03 outside clicks outside TopCenter not recorded (DEC-09 item A) |
| T03 | `codereview_1/done/task_03.md` | COMPLETE | Hide paths intact after T06 |
| T04 | `codereview_1/done/task_04.md` | COMPLETE | File unchanged since codereview_2 |
| T05 | `codereview_2/done/task_05.md` | COMPLETE | T05.1–T05.3 checked; code matches the handoff (`HudContourController.cs` 233 lines, `Synchronize` 14 lines); README extension justified by the same cause and listed in Affected files |
| T06 | `codereview_2/done/task_06.md` | COMPLETE | T06.1–T06.3 checked; code matches the handoff (`HudBackdropController.cs` 144 lines); TechSpec Observability amended and re-hashed under DEC-09 (`7188433e…`, matches checkpoint). Log levels verified by code inspection only, as the handoff states. |

## Executed validations

- Profile and exclusions: .NET 10 desktop (WPF App `net10.0-windows`); tests in `tests/TokenHound.Infrastructure.Tests` as an MTP executable (`global.json` `test.runner: Microsoft.Testing.Platform`). E2E omitted by .NET desktop policy.
- Validated state: worktree at HEAD `2bc32ef` with the PRD 17 uncommitted and untracked files, including T03–T06; Release; Windows 10.0.26200; executed in this review session.
- Reused evidence: `validation.md` manual matrices (T01, T02.3, T02) and DEC-08. They stay valid because round 2 changed only the shadow-sync method layout (same order and conditions), the log level of one transition pair, and text in the card, README, and ROADMAP. Geometry, region, placement, fill, and the success path the matrices exercised are unchanged.
- Manual acceptance: TC-04..08 and DEC-08 as recorded. This reviewer did not drive the desktop: toggling transparency needs a fresh human authorization (DEC-06 covered T02 only).

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo --verbosity:minimal` | passed: 3 projects, 0 errors, 0 warnings, exit 0 | build of all CMPs including T05/T06 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo --verbosity:minimal` | passed: 3 projects, 0 errors, 0 warnings, exit 0 | CMP-08 |
| `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` | passed: 1101 total, 1101 succeeded, 0 failed, 0 skipped, exit 0 | TC-01..03, FR-04 VM, regression |
| same with `--filter-class "*HudBackdrop*"` | passed: 19 total, 19 succeeded, exit 0 | TC-01, TC-02, TC-03, FR-04 VM |
| QA-01..05 `rg` commands and method span scan over the 23 `.cs` files | QA-01..03 clean; 1 QA-04 reservation | Quality profile |
| `sha256sum prd.md techspec.md tasks.md done/task_0*.md` | PRD and TechSpec match the checkpoint; tasks.md unchanged since codereview_1 | Source integrity |

## Findings

No blocking findings in this review.

### Optional improvements (not blocking)

- Repository rule (CLAUDE.md string comparison), new in T06: `HudBackdropController.cs:118` uses `StringComparison.Ordinal`; the rule prescribes `StringComparison.OrdinalIgnoreCase`. No functional effect; aligning it matches `HudBackdropPolicy.cs:18`.
- T06 flag carry-over (observability, Low): a same-mode `Apply` returns before clearing `_transient` (`HudBackdropController.cs:115-121`). So after an arrange-invalid Solid frame, any later Solid frames (hidden HUD, setting off, transparency off) log nothing, and the next real transition logs at Debug, even if it comes much later and has another cause. Likewise, if the very first frame is arrange-invalid, the startup Material transition logs at Debug. This matches the amended TechSpec wording ("Solid, then the next transition"). `LogSettings.MinimumLevel` defaults to `"Debug"` (`src/TokenHound.Infrastructure/Logging/LogSettings.cs:48`), so no line is dropped by default. The effect shows only when the level is raised to Information.
- `App.xaml.cs` 462 lines (459 at base; pre-existing size debt, +3 lines from this feature). Persistent; out of T05 scope.
- `Candidate` structured field on the failure warning (`HudBackdropController.cs:106`). Persistent; accepted open item C at DEC-09.
- TC-02: add a merge test with a defaults file that lacks or carries `HudBackdrop`. Persistent; accepted open item C at DEC-09.
- O-06 (pre-existing): `HudContourDecorator.BORDER_BRUSH_PROPERTY` uses `AddOwner` without `AffectsRender` (`HudContourDecorator.cs:37`); never changed at runtime, so no current impact.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_1/CR-01 | resolved (still holds) | `HudBackdropController.Hide` hides, then applies Solid (`:70-75`); callers `HudContourController.cs:88`, `:105`, `:119` → `HideCompanions` (`:163-168`) |
| codereview_1/CR-02 | resolved (still holds) | `HudBackdropInterop.cs` unchanged since codereview_2; `TryShow` still disposes the window on failure (`HudBackdropController.cs:105`) |
| codereview_2/QA-04 `Synchronize(HudContourGeometry)` 32 lines | resolved | 14 lines (`HudContourController.cs:124-137`); shadow part in `SynchronizeShadow` (`:139-161`) |
| codereview_2/QA-04 `App.xaml.cs` 462 lines | persistent | 462 lines; pre-existing debt, not in DEC-09 scope |
| codereview_2 optional: `Candidate` log field | persistent (accepted, DEC-09 item C) | `HudBackdropController.cs:106` |
| codereview_2 optional: card text omits high contrast | resolved | `HudBackdropSettingsCard.xaml:32` |
| codereview_2 optional: ROADMAP PRD 17 row "Awaiting the visual check" | resolved | `docs/ROADMAP.md:72` |
| codereview_2 optional: TC-02 merge test | persistent (accepted, DEC-09 item C) | No `HudBackdrop` merge case in the tests; only the 4 `HudBackdrop*` test files |
| codereview_2 optional: O-06 `BorderBrush` `AddOwner` | persistent (pre-existing) | `HudContourDecorator.cs:37` |
| codereview_2 optional: two Information lines per arrange-invalid frame | resolved | Pair logs at Debug (`HudBackdropController.cs:118-123`); see the carry-over note |
| codereview_2 limitation: FR-03 outside clicks outside TopCenter | persistent (not verifiable; accepted, DEC-09 item A) | No new evidence in `validation.md` |

## Limitations and open items

- FR-03 / TC-05: real outside left, right, and double clicks are recorded only for TopCenter and the T02.3 docked proof. Left, Right, Top left, Top right, and Free stay `not verifiable`. Basis for this status: the human routed this gap to HIL 3 as accepted open item A (workflow DEC-09). The region is built from the same `contour.Fill` in every mode, the companion is input-transparent, and round 2 changed none of that code. No correction task could produce the evidence without a new desktop authorization. HIL 3 must either supply the clicks or accept the gap.
- T05 and T06 are verified by build, the regression suite, and code inspection only: no runtime log capture and no manual round trip, as both handoffs state.
- codereview_1 T03 manual hide/show round trip (transparency ON): still not run; needs a fresh human authorization.
- FR-06 energy saver measured at "≈ 2 s" against "within 2 s"; a precise remeasurement is advised at HIL 3.
- TC-08 / NFR-03: 60 s windows instead of 5 minutes, measured against solid mode in the same build instead of the PRD 16 build.
- Graceful exit (TechSpec Errors): disposal chain verified in code only; no recorded exit evidence.
- FR-05 on Windows 10, an unsupported Windows 11 build, and live high contrast: policy unit tests only; not executable on this machine.
- `graft build` (T02.7) not run by this reviewer: it writes `graft/`, outside the permitted outputs.
- Open thread O-04 (product note for HIL 3): with FR-05, this machine (transparency effects off) shows the solid fallback by default.
- Snapshot next step brief names `codereview_1/` as the previous review (stale hint); the header and the caller name `codereview_2`. Not used as evidence.
- Worktree also holds PRD 16 task-record and ROADMAP changes, plus unrelated local tooling; all excluded from scope.

## Conclusion

Correction round 2 delivers the DEC-09 scope. `Synchronize(HudContourGeometry)` is down to 14 lines, and the shadow and backdrop updates keep their order and conditions. The ROADMAP row, card text, and README now match the policy and the review state. The arrange-invalid transition pair logs at Debug through one shared constant, and the fill and companion behavior is unchanged. Only the five files the two tasks declare changed since codereview_2, so the earlier manual evidence and the DEC-08 visual check still apply. Both codereview_1 fixes still hold. The build is clean, all 1101 tests pass (19 of them for PRD 17), and the blocking quality rules have zero hits.

The remaining items are optional:

- one new style deviation: `Ordinal` instead of `OrdinalIgnoreCase` at `HudBackdropController.cs:118`;
- the T06 flag carry-over note;
- the pre-existing `App.xaml.cs` size;
- the accepted DEC-09 items A and C.

The duplication trigger for a shared `SetWindowPos` interop is offered to the HIL as a suggestion. Status: `APPROVED WITH RESERVATIONS`.
