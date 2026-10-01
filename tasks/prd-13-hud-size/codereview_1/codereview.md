# Code review report — HUD size setting (prd-13-hud-size)

## Summary

- Status: APPROVED WITH RESERVATIONS
- Execution: delegated reviewer
- Git scope: `2fe5515c3793e83f6c97c2328d77dba997b640f8..working tree` (nothing committed; modified and untracked files of the feature)
- Previous review: —

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-13-hud-size/prd.md` | read |
| TechSpec | `tasks/prd-13-hud-size/techspec.md` | read |
| Manifest | `tasks/prd-13-hud-size/tasks.md` | read |
| Handoffs | `done/task_01.md`, `done/task_02.md`, `workflow.md` | read |
| Implementation | git diff vs base plus untracked files listed in the manifest | delimited |

No `context-snapshot.md` exists. `tasks/triage-log.jsonl` changed in the worktree; it is a flow artifact, not product code, and was not reviewed.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | 50-150, step 5, default 100, clamp | `HudSizeSettings.cs:Normalize/ResolvedPercent` | `HudSizeSettingsTests` (TC-01) | conformant | constants lines 12-28; build and 980 tests pass |
| FR-02 | Slider with label, presets 75/100/125 | `HudSizeSettingsViewModel.cs:Label,SetPreset,PresetCommand`; `SettingsWindow.xaml` HUD size section | `HudSizeSettingsViewModelTests` (TC-04) | conformant | presets bound with CommandParameter 75/100/125 |
| FR-03 | Apply pattern, immediate effect | `HudSizeSettingsViewModel.cs:Apply` sets `HudScale.Percent` after save | TC-05, TC-06 | conformant | `CanApply => IsDirty`; failed save keeps baseline |
| FR-04 | Whole capsule scales uniformly | `NotchWindow.xaml` root `Grid.LayoutTransform` | MA-1 | conformant | human check recorded in `workflow.md` (see limitations) |
| FR-05 | Popup and tooltip scale | `NotchWindow.xaml` StatusPopup `Border.LayoutTransform`; `TooltipCard.xaml` root `Border.LayoutTransform` | MA-1 | conformant | same |
| FR-06 | Anchor kept and clamped | unchanged `ApplyPlacement` on `SizeChanged`; `NotchWindow.Scale.cs:ApplyScaledMinimums` | `NotchPlacementTests` (+3 cases, TC-08), MA-2 | conformant | tests pass; MA-2 recorded |
| FR-07 | Own settings section, round-trip | `HudSizeStore.cs`, `UserSettings.cs:HudSize`, `UserSettingsFile.cs:182` | `HudSizeStoreTests` (TC-02, TC-03) | conformant | MergeWithDefaults line prevents dropping the section |
| NFR-01 | Core pure; layering | Infrastructure/App only | QA-03 | conformant | `git diff --stat -- src/TokenHound.Core` empty |
| NFR-02 | Non-activating invariants unchanged | no `WndProc`/`WindowStyles` diff | MA-4 | conformant | `NotchWindow.xaml.cs` diff is one line (`InitializeScale();`) |
| NFR-03 | Crisp text | `LayoutTransform` (vector re-layout) | MA-1 | conformant | DEC-04; MA-1 recorded |
| NFR-04 | Automation names and help text | `SettingsWindow.xaml` slider, presets, Apply, status, error | MA-1 | conformant | AutomationProperties on every control; slider has HelpText |
| US-01..US-03 | Shrink, enlarge, default | as above | MA-1..MA-3 | conformant | MA recorded |
| DEC-09 | General tab always visible | `SettingsWindow.xaml` MultiDataTrigger; `SettingsWindow.xaml.cs:129` | MA-4 | conformant | tab collapses only when both models are null |
| DEC-10 | Load stored size before first layout | `App.xaml.cs:377` precedes `new NotchWindow` (392) and `Show()` (399) | MA-3 | conformant | order verified in source |
| Observability | `Log.Debug` of applied percentage on scale change (TechSpec, T02) | none found | — | non-conformant (Low) | see CR-01 |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| HUD invariants (CLAUDE.md) | OK | no change to `WndProc`, `WindowStyles`, window flags |
| Core purity | OK | no Core diff |
| One class per file, sealed, file-scoped namespaces | OK | new files; `RelayCommand` nested |
| Size limits (files <= 300, methods <= 30) | OK | new/touched files <= 295, except pre-existing `App.xaml.cs` 467 (+2) |
| UPPER_CASE constants, XML docs on public members | OK | `HudSize*.cs`, `HudScale.cs` documented |
| 4+ argument calls split | OK | only pre-existing hits (`App.xaml.cs:378`, `WndProc`) |
| `nameof` instead of strings | OK | `HudSizeStore` literal `"HudSize"` follows the `RefreshSettingsStore` precedent (`"Refresh"`) |
| dotnet-efficient-validation / MTP rules | OK | `--minimum-expected-tests 1` used |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void`, `.Result`, `.Wait()` | blocking | grep over diff files | 0 | OK |
| QA-02 | No empty catch, no `#pragma warning disable` | blocking | grep over diff files | 0 | OK |
| QA-03 | Core unchanged | blocking | `git diff --stat 2fe5515 -- src/TokenHound.Core` | 0 | OK |
| QA-04 | C# style, sizes | reservation | `wc -l` over diff files | 0 new (`App.xaml.cs` 467 pre-existing, baseline recorded) | OK |
| QA-05 | 4+ argument calls split | reservation | regex over diff files | 3 matches, 0 new (`App.xaml.cs:378`, `WndProc` untouched; the ViewModel constructor has 3 parameters, generic comma) | OK |

- Terrain baseline: applied from TechSpec
- Hits discounted by baseline: 2 (`App.xaml.cs:378`, `NotchWindow.xaml.cs:106`), plus the 467-line `App.xaml.cs` and 821-line `SettingsWindow.xaml`
- Reservations accumulated in the feature: 0 profile hits (CR-01 and CR-02 are optional improvements, not profile hits)
- Suggested escalation: no trigger fired

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 own `HudSize` section/store | YES | `HudSizeStore.cs`, `UserSettings.cs` |
| DEC-02 bounds, step 5 (amended from 10, recorded) | YES | `HudSizeSettings.cs`; TechSpec text already updated |
| DEC-03 static `HudScale.Current` | YES | `HudScale.cs` |
| DEC-04/05 LayoutTransform on capsule, popup, tooltip | YES | three XAML bindings |
| DEC-06 scaled minimums | YES | `NotchWindow.Scale.cs` (partial file to respect the 300-line limit; recorded deviation) |
| DEC-07/08 ViewModel and exposure | YES | `HudSizeSettingsViewModel.cs`, `SettingsViewModel.cs:68` |
| DEC-11 invariants untouched | YES | diff |
| Observability `Log.Debug` | NO | CR-01 |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | criteria verified in source and tests; checks reproduced |
| T02 | `done/task_02.md` | COMPLETE | criteria verified in source; MA-1..MA-4 recorded as human-approved in `workflow.md` |

Manifest state, links, and the T01 -> T02 dependency are intact; no orphan obligation.

## Executed validations

- Profile and exclusions: C#/.NET WPF desktop; E2E omitted by .NET desktop policy.
- Validated state: base `2fe5515` plus the uncommitted worktree as found; no code changed during review.
- Reused evidence: build and test rerun here. Manual acceptance MA-1..MA-4 reused from `workflow.md` (human text "Tudo OK").
- Manual acceptance: not re-executed by the reviewer; see limitations.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests --no-restore` | passed, 0 warnings | unit level |
| `rtk dotnet build src/TokenHound.App --no-restore` | passed, 0 errors, 0 warnings | XAML and App wiring |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 980 tests, exit 0 | FR-01..FR-03, FR-06, FR-07 |
| QA-01..QA-05 commands | see quality profile | QA rules |

Core.Tests were not rerun: Core has no diff.

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Low | TechSpec Observability; task_02 "Operational signal" | `NotchWindow.Scale.cs:25-29` and `HudScale.cs:30-34` have no logging; no `Log.Debug` of the applied percentage anywhere in the diff (`HudSizeSettingsViewModel.cs` logs only the failure warning) | Missing debug signal only; no functional effect. Optional improvement | Add a `Log.Debug` of the applied percentage in `HudSizeSettingsViewModel.Apply` |
| CR-02 | Low | Code quality (optional) | `App.xaml.cs:364,377` create two `HudSizeStore` instances and read the settings file twice at startup | Negligible redundant IO | Optional: load once and reuse |

## Previous findings (re-review only)

Not applicable (first review).

## Limitations and open items

- MA-1..MA-4 (visual acceptance) were not re-executed by this reviewer; the evidence is the human approval recorded in `workflow.md` (line 32, "Tudo OK"). FR-04..FR-06 and NFR-02..NFR-04 rest on it.
- `workflow.md` line 26 still says "step 10" in a historical entry, superseded by the recorded step-5 deviation. No action.
- `tasks/triage-log.jsonl` modification was out of scope.

## Conclusion

All functional and non-functional obligations are conformant in source, tests (980 passed), build (0 warnings), and the quality profile (no blocking or reservation hit beyond the pre-existing baseline). Only two Low optional improvements remain (missing debug log, duplicate store load), with no requirement, security, or essential evidence pending beyond the human-attested manual check. Status: APPROVED WITH RESERVATIONS.
