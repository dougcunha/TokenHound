# Code review report — Provider Status Window

## Summary

- Status: APPROVED WITH RESERVATIONS
- Git scope: `53da1817ebfc7f76110762dec6da664d0943affa..working tree` (uncommitted T01 + T02 + corrections T03–T06 under `src/TokenHound.App` and `tests/TokenHound.Infrastructure.Tests`, tracked and untracked; HEAD is still `53da181`)
- Previous review: `tasks/prd-09-provider-status-window/codereview_4/codereview.md` (APPROVED WITH RESERVATIONS; `CR-02` corrected by T06 under workflow DEC-12, `CR-01` accepted in DEC-09)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-09-provider-status-window/prd.md` (sha256 `0922887f…1886`, matches approval DEC-03) | read |
| TechSpec | `tasks/prd-09-provider-status-window/techspec.md` (sha256 `9d385cda…55b5`, matches approval DEC-04) | read |
| Manifest | `tasks/prd-09-provider-status-window/tasks.md` (State and Problems and solutions extended by the corrections; DAG and contract unchanged) | read |
| Correction contract | `codereview_4/done/task_06.md` (T06, workflow DEC-12) | read |
| Implementation | `git status --porcelain -- src tests`: 24 modified + 15 untracked files; handoffs `done/task_01.md`, `done/task_02.md`, `codereview_1/done/task_03.md` … `codereview_4/done/task_06.md` | delimited |

- Changed since `codereview_4`: only `src/TokenHound.App/UI/Styles/DialogResources.xaml`, `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml`, and `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (`find src tests -newer codereview_4/codereview.md`, excluding `bin`/`obj`). These are exactly the three files T06 lists. No `.cs` file changed. Every other file keeps the evidence of `codereview_4`. This session re-ran builds, tests, and the profile over the whole set.
- Out of scope: the pre-existing `.agents/skills/**` changes recorded in `workflow.md#REC-01`.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Tray entry opens single window | `TrayMenuModel.BuildDescriptor`, `TrayIconViewModel`, `ProviderStatusDialog` activate-or-create | `TrayMenuModelTests`, `TrayIconViewModelTests` | conformant | Unchanged since `codereview_4`; MA-01 pass (`done/task_02.md#Handoff`) |
| FR-02 | HUD menu entry opens same window | `NotchWindow.xaml` `ProviderStatusMenuItem`, `NotchWindow.xaml.cs`, `HudActionsViewModel.ShowProviderStatus` | `HudActionsViewModelTests.ShowProviderStatus_*` | conformant | Unchanged; MA-02 pass; MA-08 opened the window through this entry |
| FR-03 | Enabled providers only, live enable/disable | `ProviderStatusViewModel` | TC-01, TC-02 | conformant | Unchanged; MA-04 pass |
| FR-04 | Group by family with count | `ProviderStatusProjection.Group`, `ProviderCatalog.ResolveFamilyName`; header in `ProviderStatusWindow.xaml` | `Group_WhenClaudeProfilesAndOtherProvider_GroupsByFamily` | conformant | Unchanged; T06 only inserts the `ScrollBar` style at `:33` |
| FR-05 | Display name + HUD status message | `ProviderStatusProjection.cs:28,43` | `CreateAccount_WhenNeedsAuth_ReusesHudStatusMessage` | conformant | — |
| FR-06 | Columns = HUD rows, same order and label | `ProviderStatusProjection.cs:33-35` → `ProviderUsageRowFactory.CreateRows` | `..._ColumnsMirrorHudRows` (quota, Copilot, Cline) | conformant | — |
| FR-07 | Label, used %, bar, reset line; quantity text without fraction | `ProviderStatusProjection.CreateColumn`, `QuotaColumnTemplate` | `..._ShowsQuantityWithoutBar`, `FormatPercent_RoundsLikeTheHud` | conformant | — |
| FR-08 | Relative + absolute reset / `No reset pending` | `ProviderStatusFormatter.FormatResetLine` | `FormatResetLine_*` | conformant | — |
| FR-09 | Colour by ring states | `ProviderStatusFormatter.ResolveLevel`, `UsageLevelToBrushConverter` | `ResolveLevel_FollowsRingColourStates` | conformant | — |
| FR-10 | Exhausted: back in + dimmed + alert reset line | `ProviderStatusProjection.cs:37,45-46,94`; dimming trigger `ProviderStatusWindow.xaml:186` | `CreateAccount_WhenWindowsExhausted_ShowsEarliestComeback`, `..._WhenExhaustedWithoutReset_ShowsLimitReached` | conformant (visual not verifiable) | Unit-proven (TC-08); MA-05 still pending. See CR-01 |
| FR-11 | Live update + minute refresh | `ProviderStatusViewModel` handlers and `TimeProvider` timer | `StoreEvents_WhileOpen_*`, `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | — |
| FR-12 | Empty and pending states | `ProviderStatusViewModel.IsEmpty`, `ProviderStatusProjection.CreatePendingAccount` | `Constructor_WhenNoProviders_IsEmpty`, `CreateAccount_WhenNoSnapshot_ReturnsPendingAccount` | conformant | — |
| PRD §User experience (DEC-07) | Status message alert colour when the account is blocked | `ProviderStatusAccount.IsBlocked`, `ProviderStatusProjection.ResolveBlocked`, `ProviderStatusWindow.xaml:188-190` | `CreateAccount_ByStatus_MarksBlockedLikeTheHud`, `CreateAccount_WithActiveBlock_FollowsItsBlockedFlag` | conformant | Trigger intact (shifted by +2 lines). Seen on screen during MA-08: a real `Claude OAuth API rate limit exceeded (HTTP 429)` in the alert colour (`codereview_4/done/task_06.md#Handoff`, workflow REC-07) |
| workflow DEC-10 (1) | Title bar in the body colour; Windows 10 fallback | `WindowPlacement.SetCaptionColors`; `ProviderStatusWindow.xaml.cs` | MA-07 | conformant (Windows 10 fallback not verifiable) | Unchanged since `codereview_4` |
| workflow DEC-10 (2, 3) | Prominent group title at the same size; count badge | `ProviderStatusWindow.xaml` header template, `CountBadgeStyle` | MA-07 | conformant | Unchanged since `codereview_4` |
| workflow DEC-12 / `codereview_4/CR-02` | Slim dark scrollbar on the status window and Settings; other windows unchanged | `DialogResources.xaml:277-373` (`DialogScrollBarPageButtonStyle`, `DialogScrollBarThumbStyle`, `DialogScrollBarStyle`); opt-in `ProviderStatusWindow.xaml:33`, `SettingsWindow.xaml:29` | MA-08 | conformant | 8 px, no arrow buttons, transparent track and page buttons, thumb radius 3 with `SurfaceBorderBrush` #3F3F46, hover `ButtonPressedBackgroundBrush` #52525B, drag `TextMutedBrush` #71717A (the `IsDragging` trigger follows `IsMouseOver`, so it wins while dragging). Vertical `Track` is `IsDirectionReversed="True"` with `PageUp`/`PageDown`; the horizontal trigger swaps in an 8 px high template with `PageLeft`/`PageRight`. All three brushes are defined earlier in the same dictionary (`:21,25,28`). The style is keyed. The only `TargetType="ScrollBar"` implicit styles are the two opt-ins, and `App.xaml` merges the dictionary without one, so the HUD, About, and tooltips are unchanged. MA-08: both windows showed the dark scrollbar; wheel and drag worked |
| NFR-01 | No Core change, no new dependency | — | diff scope | conformant | `git diff --name-only 53da181 -- src/TokenHound.Core src/TokenHound.Infrastructure '*.props' 'src/**/*.csproj'` is empty |
| NFR-02 | No invented percentage | `CreateColumn`, `FormatBackIn` | `..._ShowsQuantityWithoutBar` | conformant | — |
| NFR-03 | Consistency with HUD | reuse of `CreateRows`, `ResolveStatusMessage`, `ResolveDefaultName` | TC-05 | conformant | — |
| NFR-04 | Dark style; readable at 100% and 150% | `ProviderStatusWindow.xaml`; caption per DEC-10; scrollbar per DEC-12 | MA-06, MA-07, MA-08 | not verifiable (150%) | 100% seen (MA-03, MA-07, MA-08); the light scrollbar of `codereview_4/CR-02` is gone; 150% not run |
| NFR-05 | Single instance; release on close; closed at shutdown | `ProviderStatusDialog`, `DialogService.CloseAll` | `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | Unchanged |
| NFR-06 | Scroll, ≥ 10 rows usable | `ProviderStatusWindow.xaml:113-199` | MA-06, MA-08 | not verifiable (≥ 10 rows) | Wheel and drag scrolling seen with the new scrollbar (MA-08); ≥ 10 rows not run |
| NFR-07 | AGENTS.md rules; `App.xaml.cs` growth | all touched files | QA-06, QA-07 | conformant | `App.xaml.cs` 449 = baseline 448 + 1 (TechSpec DEC-03); T06 touches no `.cs` file |
| TC-01..TC-10 | Unit scenarios and entry-point tests | as above | `ProviderStatus*Tests`, `ProviderUsageRowFactory*Tests`, tray and HUD action tests | conformant | 828 tests pass in this session |
| TC-11 | MA-01..MA-06 (+ MA-07 for T05, MA-08 for T06) | — | Windows MCP (coordinator) | pending | MA-01..MA-04, MA-07, MA-08 pass; MA-05 pending; MA-06 partial |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (AGENTS.md) | OK | No Core file in the diff |
| Never invent limits or denominators | OK | T06 touches no data path |
| Never write other tools' credentials | N/A | No credential access added |
| HUD no-activation invariants | N/A | `NotchWindow` untouched by T06; the scrollbar style is not applied to it |
| C# structure and style rules (`*.cs`) | OK | T06 changes no `.cs` file; earlier files as in `codereview_4` |
| File ≤ 300 lines | OK | The rule covers `*.cs` (CLAUDE.md). `DialogResources.xaml` grows from 277 to 375 lines; it is a resource dictionary, not under the rule |
| Line endings | OK | The three T06 files have no CRLF (`file` reports ASCII/UTF-8 text without CRLF) |
| MTP test commands with `--minimum-expected-tests 1` | OK | Commands below |
| `dotnet-efficient-validation`, `repository-cli-efficiency` | OK | Loaded in this session before builds, tests, and searches |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void`, `.Result`, `.Wait()`, `GetResult()` | blocking | `rg -n 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files $tfiles` | 0 | OK |
| QA-02 | No empty `catch` | blocking | `rg -n 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files $tfiles` | 0 | OK |
| QA-03 | No `#nullable disable` / `#pragma warning disable` | blocking | `rg -n '#nullable disable\|#pragma warning disable' $files $tfiles` | 0 | OK |
| QA-04 | No `DateTime.Now/UtcNow` | reservation | `rg -n 'DateTime\.(Now\|UtcNow)' $files $tfiles` | 0 | OK |
| QA-05 | No WPF types in CMP-01..CMP-04 | blocking | `rg -n 'System\.Windows\|Dispatcher' src/TokenHound.App/ViewModels/ProviderStatus*.cs src/TokenHound.App/ViewModels/UsageLevel.cs` | 5 lines in `ProviderStatusViewModel.cs`, all the `_uiDispatcher` `Action<Action>` identifier | OK — regex false positive; no WPF type, proven by the `net10.0` test build |
| QA-06 | File ≤ 300, method ≤ 30 lines | reservation | `wc -l $files` + method review | `App.xaml.cs` 449 (+1 over baseline) | justified by TechSpec `DEC-03` (HIL 2, workflow DEC-04) |
| QA-07 | 4+ parameters | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 5 total: 2 new (`UsageLevelToBrushConverter.cs:28,38`) + 3 baseline | justified — signature imposed by `IValueConverter`. T06 adds none |

- `$files`: the 24 `.cs` files changed or added under `src/` since `53da181`; `$tfiles`: the 10 changed or added test `.cs` files. Same sets as `codereview_4`, because T06 adds no `.cs` file.
- Terrain baseline: applied from the TechSpec.
- Hits discounted by baseline: 4 (`App.xaml.cs` 448 lines, `App.xaml.cs:366`, `NotchWindow.xaml.cs:105`, `TrayIconViewModel.cs:30`).
- Reservations accumulated in the feature: 3 (QA-06 `App.xaml.cs` +1 and QA-07 ×2, all justified).
- Suggested escalation: no trigger fired (fewer than 8 reservations, no touched `.cs` file above 500 lines, no duplication in 3+ places).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 columns from `CreateRows` | YES | `ProviderStatusProjection.cs:33-35` |
| DEC-02 `ResetTimeUtc` at 3 sites | YES | TC-09 asserts all three |
| DEC-03 `App.xaml.cs` +1 line | YES | 449 lines |
| DEC-04 `ResolveStatusMessage` internal | YES | `ProviderRingViewModel.Status.cs:9` |
| DEC-05 family name and ordering | YES | `ProviderCatalog.ResolveFamilyName`, `ProviderStatusProjection.Group` |
| DEC-06 exhausted = any column ≥ 1.0; earliest reset; `limit reached` | YES | `ProviderStatusProjection.cs:37,46,94`; kept by workflow DEC-09 |
| DEC-07 percent, level thresholds, no bar without fraction | YES | `ProviderStatusFormatter` |
| DEC-08 reset line format | YES | `ProviderStatusFormatter.FormatResetLine` |
| DEC-09 `TimeProvider` 60 s timer, dispatcher, dispose | YES | `ProviderStatusViewModel` |
| DEC-10 key order and headers | YES | `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `NotchWindow.xaml` |
| DEC-11 standard activating window, dialog brushes, 1100×640, min 520×300, `ScrollViewer`, `WrapPanel` cells | YES (extended) | Extended by workflow DEC-10 (caption, header) and DEC-12 (dark scrollbar from the dialog palette) |
| CMP-04 records | YES (extended) | `ProviderStatusAccount.IsBlocked` (workflow DEC-07) |
| CMP-08 location | PARTIAL (accepted) | Lifecycle in `ProviderStatusDialog.cs` under the TechSpec risk mitigation; `DialogService.cs` 274 lines |
| T06 boundary: XAML resources only; keyed style with opt-in in the status window and Settings; no HUD, tooltip, About, layout, data, behaviour, code-behind, view-model, Core, or Infrastructure change | YES | Only the three T06 files changed after `codereview_4`; the `ProviderStatusWindow.xaml` and `SettingsWindow.xaml` changes are the single implicit-style line plus a blank line each; no `.cs` changed |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Code unchanged since `codereview_4`; TC-01..TC-09 pass |
| T02 | `done/task_02.md` | COMPLETE | TC-10 present; MA-05 pending and MA-06 partial, carried to HIL 3 |
| T03 (correction) | `codereview_1/done/task_03.md` | COMPLETE | Unchanged; resolved `codereview_1/CR-01` |
| T04 (correction) | `codereview_2/done/task_04.md` | COMPLETE | Unchanged except the trigger's line position (`ProviderStatusWindow.xaml:188-190`); now also seen on screen (REC-07) |
| T05 (correction) | `codereview_3/done/task_05.md` | COMPLETE | Unchanged since `codereview_4` |
| T06 (correction) | `codereview_4/done/task_06.md` | COMPLETE | All four work items checked against the code. The handoff matches it: three styles, 8 px, 1 px/2 px padding, radius 3, the three thumb brushes, horizontal trigger, opt-in lines in both windows; `DialogResources.xaml` 375 lines and `ProviderStatusWindow.xaml` 201 lines match. Acceptance criteria: App 0 warnings (re-run here, non-incremental), 828 + 92 tests pass (re-run here), MA-08 screenshots by the coordinator, no `.cs` change |

Manifest links (`tasks.md` → `done/task_01.md`, `done/task_02.md`) resolve, and the correction handoffs `codereview_1/done/task_03.md` … `codereview_4/done/task_06.md` exist. The DAG (T01 → T02) is consistent, and the corrections depend on no pending task.

## Executed validations

- Profile and exclusions: `TokenHound.App` is net10.0-windows WPF. The tests are `tests/TokenHound.Infrastructure.Tests` (net10.0, xUnit v3 on MTP) and `tests/TokenHound.Core.Tests`. E2E is omitted by .NET desktop policy.
- Validated state: base `53da181` plus the current working tree (T01–T06), Debug, run in this review session on 2026-09-27.
- Reused evidence: MA-01..MA-04 and the partial MA-06 from `done/task_02.md#Handoff`; MA-07 from `codereview_3/done/task_05.md#Handoff`; MA-08 and the T04 visual from `codereview_4/done/task_06.md#Handoff`. T06 changed only scrollbar templates, so the entry points, lifecycle, rows, columns, caption, and header seen in those runs are unaffected. This session did not relaunch the app, because that needs the user to authorize closing the installed instance.
- Manual acceptance: MA-05 pending (no exhausted account); MA-06 150% scaling and ≥ 10 rows not verified; T05 Windows 10 fallback not verified; T06 horizontal scrollbar and the Settings `Cadence & Rate Limits` tab not exercised on screen. Owner: the user, at HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal --no-incremental` | passed, 0 errors, 0 warnings (XAML compiles) | NFR-07, T02, T04–T06 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal --no-incremental` | passed, 0 warnings | QA-05 (net10.0 link) |
| `rtk dotnet build tests/TokenHound.Core.Tests --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | NFR-01 regression |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 828 tests, exit 0 | TC-01..TC-10, T04 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderStatus*"` | passed, 42 tests, exit 0 | TC-01..TC-08, T04 |
| `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 92 tests, exit 0 | NFR-01 regression |
| `git diff --check 53da181 -- src tests` | no whitespace errors | — |
| Quality profile QA-01..QA-07 | as tabulated | QA-01..QA-07 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Info | FR-10, TechSpec DEC-06, US-03 | `ProviderStatusProjection.cs:46` counts `back in` down to the **earliest** reset among exhausted windows, as DEC-06 states. `ProviderStatusProjection.cs:94` requires a used fraction, so a fraction-less block (e.g. Cline free limit) is not dimmed and gets no `back in`, although its status message turns alert-coloured (`IsBlocked`) | With two exhausted windows, `back in` names the earlier reset while the account stays blocked until the later one | None. Accepted as an open item at the reservations HIL (workflow DEC-09); DEC-06 stays as approved |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_4/CR-01 | persistent (accepted) | Now `codereview_5/CR-01`; DEC-06 code unchanged, as workflow DEC-09 decided |
| codereview_4/CR-02 | resolved | `DialogScrollBarStyle` (`DialogResources.xaml:317`) applied by the implicit styles at `ProviderStatusWindow.xaml:33` and `SettingsWindow.xaml:29`; MA-08 on screen |

## Limitations and open items

- Independence: this review ran after `/clear` under the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that implemented T01/T02, issued `codereview_1`..`codereview_4`, and executed T03–T06 (workflow DEC-05, DEC-06, DEC-08, DEC-11, DEC-13). This context held none of those conversations. It loaded only the snapshot header, next step brief, open threads, and `on-run` entries, and it derived every state from the sources, the code, and commands run here.
- The status is APPROVED WITH RESERVATIONS, not APPROVED, because obligations remain not verifiable by manual evidence. None is a code defect, and the flow presents pending manual acceptance at HIL 3 by design (owner: user):
  - MA-05: dimming and the alert `back in` not seen on screen; FR-10 is unit-proven (TC-08) and the XAML triggers were inspected.
  - MA-06: 150% scaling (NFR-04) and ≥ 10 rows (NFR-06) not verified.
  - T05: the Windows 10 fallback (dark title bar only) not verified.
  - T06: the horizontal scrollbar variant and the Settings `Cadence & Rate Limits` tab not exercised on screen; they use the same style, and the horizontal template was inspected.
- The T04 alert visual, MA-07, and MA-08 are the coordinator's on-screen evidence, not re-run here; the user accepts them at HIL 3.
- The implicit `ScrollBar` style in `SettingsWindow.xaml` also reaches scrollbars inside that window's templated controls (e.g. a multi-line `TextBox` `PART_ContentHost` at `SettingsWindow.xaml:131`). This matches DEC-12 ("the Settings window gets the same dark scrollbar"). It is not a finding.
- `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryClineTests.cs` keeps CRLF line endings in the working copy, while the index is LF. Git normalizes them on commit, and `git diff --check` is clean. This is not a finding.
- Jev (shadow): J4 and J5 run only after this report is written and do not inform it.

## Conclusion

Correction T06 delivers workflow DEC-12 within its boundary and resolves `codereview_4/CR-02`. A keyed, slim dark `ScrollBar` style in the dialog palette has correct vertical and horizontal track directions and page commands, and a drag state that takes precedence over hover. It applies only to the provider status window and the Settings window, through implicit opt-in styles, and no `.cs` file changed. Only the three files T06 lists changed after `codereview_4`, and the T04 blocked-status trigger is intact and has now been seen on screen. T01–T06 are complete. Every FR, TechSpec decision, and TC-01..TC-10 is conformant by code and passing tests: the App rebuilds non-incrementally with 0 warnings, Infrastructure.Tests passes 828 tests, and Core.Tests passes 92. The quality profile has no unjustified hit, and no escalation trigger fired. No code finding remains; CR-01 is the already-accepted DEC-06 open item. MA-05, the rest of MA-06, the T05 Windows 10 fallback, and the T06 horizontal variant remain manual items for the user at HIL 3.
