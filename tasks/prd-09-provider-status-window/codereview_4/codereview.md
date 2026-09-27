# Code review report — Provider Status Window

## Summary

- Status: APPROVED WITH RESERVATIONS
- Git scope: `53da1817ebfc7f76110762dec6da664d0943affa..working tree` (uncommitted T01 + T02 + corrections T03, T04, and T05 under `src/TokenHound.App` and `tests/TokenHound.Infrastructure.Tests`, tracked and untracked; HEAD is still `53da181`)
- Previous review: `tasks/prd-09-provider-status-window/codereview_3/codereview.md` (APPROVED WITH RESERVATIONS; reservations finalized in workflow DEC-09; HIL 3 then not accepted and correction T05 authorized by workflow DEC-10)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-09-provider-status-window/prd.md` (sha256 `0922887f…1886`, matches approval DEC-03) | read |
| TechSpec | `tasks/prd-09-provider-status-window/techspec.md` (sha256 `9d385cda…55b5`, matches approval DEC-04) | read |
| Manifest | `tasks/prd-09-provider-status-window/tasks.md` (State and Problems and solutions extended with T04 and T05; DAG and contract unchanged) | read |
| Correction contract | `codereview_3/done/task_05.md` (T05, workflow DEC-10) | read |
| Implementation | `git status --porcelain -- src tests`: 22 modified + 15 untracked files; handoffs `done/task_01.md`, `done/task_02.md`, `codereview_1/done/task_03.md`, `codereview_2/done/task_04.md`, `codereview_3/done/task_05.md` | delimited |

- Changed since `codereview_3` (written 15:05): only `src/TokenHound.App/Interop/WindowPlacement.cs`, `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml`, and `ProviderStatusWindow.xaml.cs` (`find src tests -newer codereview_3/codereview.md`). These are exactly the three files T05 lists. Every other file keeps the evidence of `codereview_3`, and this session re-ran builds, tests, and the profile over the whole set.
- Out of scope: the pre-existing `.agents/skills/**` changes recorded in `workflow.md#REC-01`.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Tray entry opens single window | `TrayMenuModel.BuildDescriptor`, `TrayIconViewModel`, `ProviderStatusDialog` activate-or-create | `TrayMenuModelTests`, `TrayIconViewModelTests` | conformant | Unchanged since `codereview_3`; MA-01 pass (`done/task_02.md#Handoff`) |
| FR-02 | HUD menu entry opens same window | `NotchWindow.xaml` `ProviderStatusMenuItem`, `NotchWindow.xaml.cs`, `HudActionsViewModel.ShowProviderStatus` | `HudActionsViewModelTests.ShowProviderStatus_*` | conformant | Unchanged; MA-02 pass; T05 MA-07 opened the window through this entry |
| FR-03 | Enabled providers only, live enable/disable | `ProviderStatusViewModel` | TC-01, TC-02 | conformant | Unchanged; MA-04 pass |
| FR-04 | Group by family with count | `ProviderStatusProjection.Group`, `ProviderCatalog.ResolveFamilyName`; header `ProviderStatusWindow.xaml:117-137` | `Group_WhenClaudeProfilesAndOtherProvider_GroupsByFamily` | conformant | T05 restyles the header only; it still binds `FamilyName` (`:125`) and `Count` (`:130`) |
| FR-05 | Display name + HUD status message | `ProviderStatusProjection.cs:28,43`; `ProviderStatusWindow.xaml:150-162` | `CreateAccount_WhenNeedsAuth_ReusesHudStatusMessage` | conformant | — |
| FR-06 | Columns = HUD rows, same order and label | `ProviderStatusProjection.cs:33-35` → `ProviderUsageRowFactory.CreateRows` | `..._ColumnsMirrorHudRows` (quota, Copilot, Cline) | conformant | — |
| FR-07 | Label, used %, bar, reset line; quantity text without fraction | `ProviderStatusProjection.CreateColumn`, `ProviderStatusWindow.xaml:70-102` | `..._ShowsQuantityWithoutBar`, `FormatPercent_RoundsLikeTheHud` | conformant | `QuotaColumnTemplate` unchanged in content by T05 |
| FR-08 | Relative + absolute reset / `No reset pending` | `ProviderStatusFormatter.FormatResetLine` | `FormatResetLine_*` | conformant | — |
| FR-09 | Colour by ring states | `ProviderStatusFormatter.ResolveLevel`, `UsageLevelToBrushConverter` (`ProviderStatusWindow.xaml:34-38`) | `ResolveLevel_FollowsRingColourStates` | conformant | — |
| FR-10 | Exhausted: back in + dimmed + alert reset line | `ProviderStatusProjection.cs:37,45-46,94`; `ProviderStatusWindow.xaml:95-97,163-167,182-185` | `CreateAccount_WhenWindowsExhausted_ShowsEarliestComeback`, `..._WhenExhaustedWithoutReset_ShowsLimitReached` | conformant (visual not verifiable) | Unit-proven (TC-08); MA-05 still pending. See CR-01 |
| FR-11 | Live update + minute refresh | `ProviderStatusViewModel` handlers and `TimeProvider` timer | `StoreEvents_WhileOpen_*`, `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | — |
| FR-12 | Empty and pending states | `ProviderStatusViewModel.IsEmpty`, `ProviderStatusProjection.CreatePendingAccount`, `ProviderStatusWindow.xaml:106-109` | `Constructor_WhenNoProviders_IsEmpty`, `CreateAccount_WhenNoSnapshot_ReturnsPendingAccount` | conformant | — |
| PRD §User experience (DEC-07) | Status message alert colour when the account is blocked | `ProviderStatusAccount.IsBlocked`, `ProviderStatusProjection.ResolveBlocked`, `ProviderStatusWindow.xaml:186-188` | `CreateAccount_ByStatus_MarksBlockedLikeTheHud`, `CreateAccount_WithActiveBlock_FollowsItsBlockedFlag` | conformant (visual not verifiable) | The T04 trigger survives the T05 edit, now at `:186-188`; not yet seen on screen |
| workflow DEC-10 (1) | Title bar in the body colour; Windows 10 fallback | `WindowPlacement.SetCaptionColors` / `SetColorAttribute` (`WindowPlacement.cs:141-170`); `ProviderStatusWindow.xaml.cs:27-36` | none (interop; T05 declares no unit test) | conformant (Windows 10 fallback not verifiable) | DWM attributes 35 (caption), 34 (border), 36 (text) set as COLORREF `R | G<<8 | B<<16`, from the `SurfaceBackgroundColor` #18181B and `TextPrimaryColor` #F4F4F5 resources (`DialogResources.xaml:5,9`). `EnableDarkMode` is applied first, as in `SettingsWindow.xaml.cs:88`; DWM results are ignored, so an unsupported attribute cannot throw. MA-07: caption and body pixels both #18181B (`codereview_3/done/task_05.md#Handoff`) |
| workflow DEC-10 (2, 3) | Prominent group title at the same size; count badge | `ProviderStatusWindow.xaml:29-30,61-68,117-137` | MA-07 | conformant | 3 px rounded `GroupAccentBrush` bar, `FontSize="16"` `FontWeight="Bold"`, `CountBadgeStyle` (corner radius 9, #27272A, 11 px semibold secondary text, vertically centred), 1 px `SurfaceBorderBrush` bottom line |
| NFR-01 | No Core change, no new dependency | — | diff scope | conformant | `git diff --name-only 53da181 -- src/TokenHound.Core src/TokenHound.Infrastructure '*.props' 'src/**/*.csproj'` is empty |
| NFR-02 | No invented percentage | `CreateColumn`, `FormatBackIn` | `..._ShowsQuantityWithoutBar` | conformant | — |
| NFR-03 | Consistency with HUD | reuse of `CreateRows`, `ResolveStatusMessage`, `ResolveDefaultName` | TC-05 | conformant | — |
| NFR-04 | Dark style; readable at 100% and 150% | `ProviderStatusWindow.xaml`; caption per DEC-10 | MA-06, MA-07 | not verifiable (150%) | 100% seen (MA-03, MA-07); 150% not run. The vertical scrollbar is unstyled (CR-02) |
| NFR-05 | Single instance; release on close; closed at shutdown | `ProviderStatusDialog`, `DialogService.CloseAll` | `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | Unchanged. The `SourceInitialized` handler lives as long as the window, so it holds nothing beyond it |
| NFR-06 | Scroll, ≥ 10 rows usable | `ProviderStatusWindow.xaml:111-197` | MA-06 | not verifiable (≥ 10 rows) | Scrolling seen with 6 accounts |
| NFR-07 | AGENTS.md rules; `App.xaml.cs` growth | all touched files | QA-06, QA-07 | conformant | `App.xaml.cs` 449 = baseline 448 + 1 (TechSpec DEC-03); `WindowPlacement.cs` 204 lines, new methods 10 and 12 lines; `ProviderStatusWindow.xaml.cs` 37 lines |
| TC-01..TC-10 | Unit scenarios and entry-point tests | as above | `ProviderStatus*Tests`, `ProviderUsageRowFactory*Tests`, tray and HUD action tests | conformant | 828 tests pass in this session |
| TC-11 | MA-01..MA-06 (+ MA-07 for T05) | — | Windows MCP (coordinator) | pending | MA-01..MA-04 and MA-07 pass; MA-05 pending; MA-06 partial; T04 visual not seen |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (AGENTS.md) | OK | No Core file in the diff |
| Never invent limits or denominators | OK | T05 touches no data path |
| Never write other tools' credentials | N/A | No credential access added |
| HUD no-activation invariants | N/A | The status window is not a HUD window (TechSpec DEC-11); `NotchWindow` is untouched by T05 |
| Sealed classes, file-scoped namespaces, XML docs, `UPPER_CASE` constants | OK | `ProviderStatusWindow` is `sealed partial`; `SetCaptionColors` documented; `DWMWA_*`, `CAPTION_*_KEY` constants |
| Blank line inside multi-line blocks; ≥ 4 arguments split | OK | `WindowPlacement.cs:150,161`; `DwmSetWindowAttribute(` call split one argument per line (`:164-169`) |
| File ≤ 300 lines / method ≤ 30 lines | OK | T05 files 204, 199 (XAML), 37 lines; other touched files ≤ 288 except `App.xaml.cs` (baseline + DEC-03) |
| Line endings | OK | T05 files are LF (`file` reports ASCII text without CRLF) |
| MTP test commands with `--minimum-expected-tests 1` | OK | Commands below |
| `dotnet-efficient-validation`, `repository-cli-efficiency` | OK | Loaded in this session before builds, tests, and searches |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void`, `.Result`, `.Wait()`, `GetResult()` | blocking | `rg -n 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files $tfiles` | 0 | OK |
| QA-02 | No empty `catch` | blocking | `rg -n 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files $tfiles` | 0 | OK |
| QA-03 | No `#nullable disable` / `#pragma warning disable` | blocking | `rg -n '#nullable disable\|#pragma warning disable' $files $tfiles` | 0 | OK |
| QA-04 | No `DateTime.Now/UtcNow` | reservation | `rg -n 'DateTime\.(Now\|UtcNow)' $files $tfiles` | 0 | OK |
| QA-05 | No WPF types in CMP-01..CMP-04 | blocking | `rg -n 'System\.Windows\|Dispatcher' src/TokenHound.App/ViewModels/ProviderStatus*.cs src/TokenHound.App/ViewModels/UsageLevel.cs` | 5 lines, all the `_uiDispatcher` `Action<Action>` identifier | OK — regex false positive; no WPF type, proven by the `net10.0` test build |
| QA-06 | File ≤ 300, method ≤ 30 lines | reservation | `wc -l $files` + method review | `App.xaml.cs` 449 (+1 over baseline) | justified by TechSpec `DEC-03` (HIL 2, workflow DEC-04) |
| QA-07 | 4+ parameters | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 2 new of 5: `UsageLevelToBrushConverter.cs:28,38` | justified — signature imposed by `IValueConverter`; 3 pre-existing baseline hits. T05 adds none |

- `$files`: the 24 `.cs` files changed or added under `src/` since `53da181` (T05 adds `WindowPlacement.cs`); `$tfiles`: the 10 changed or added test `.cs` files.
- Terrain baseline: applied from the TechSpec. `WindowPlacement.cs` is not in the baseline table; it had no hit before T05 and has none now.
- Hits discounted by baseline: 4 (`App.xaml.cs` 448 lines, `App.xaml.cs:366`, `NotchWindow.xaml.cs:105`, `TrayIconViewModel.cs:30`).
- Reservations accumulated in the feature: 3 (QA-06 `App.xaml.cs` +1 and QA-07 ×2, all justified).
- Suggested escalation: no trigger fired (fewer than 8 reservations, no touched file above 500 lines, no duplication in 3+ places).

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
| DEC-11 standard activating window, dialog brushes, 1100×640, min 520×300, `ScrollViewer`, `WrapPanel` cells | YES (extended) | `ProviderStatusWindow.xaml:12-24,111-178`; cell width 220 instead of ≈ 260 (manifest). T05 adds the DWM caption colours and the header style under workflow DEC-10, still a standard `SingleBorderWindow` |
| CMP-04 records | YES (extended) | `ProviderStatusAccount.IsBlocked` (workflow DEC-07) |
| CMP-08 location | PARTIAL (accepted) | Lifecycle in `ProviderStatusDialog.cs` under the TechSpec risk mitigation; `DialogService.cs` 274 lines |
| T05 boundary: no rows, columns, bar or status colours, size, behaviour, Settings/About, Core, Infrastructure, or view-model change | YES | Only the three T05 files changed after `codereview_3`; `SettingsWindow.xaml.cs` and `AboutWindow.xaml.cs` untouched; column template, account template, and triggers keep their content |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Code unchanged since `codereview_3`; TC-01..TC-09 pass |
| T02 | `done/task_02.md` | COMPLETE | TC-10 present; MA-05 pending and MA-06 partial, carried to HIL 3 |
| T03 (correction) | `codereview_1/done/task_03.md` | COMPLETE | Unchanged; resolved `codereview_1/CR-01` |
| T04 (correction) | `codereview_2/done/task_04.md` | COMPLETE | Unchanged except the trigger's line position (`ProviderStatusWindow.xaml:186-188`) |
| T05 (correction) | `codereview_3/done/task_05.md` | COMPLETE | All four work items checked against the code. The handoff matches it: `SetCaptionColors` with attributes 35/34/36 through `SetColorAttribute`; `OnSourceInitialized` applies `EnableDarkMode` then the caption colours from resources; header with accent bar, bold 16 px name, `CountBadgeStyle`, and separator. File sizes 204 / 37 / 199 match. Acceptance criteria: App 0 warnings (re-run here, non-incremental), 828 + 92 tests pass (re-run here), MA-07 screenshot by the coordinator, no new QA hit |

Manifest links (`tasks.md` → `done/task_01.md`, `done/task_02.md`) resolve. The State section cites `codereview_1/done/task_03.md`, `codereview_2/done/task_04.md`, and `codereview_3/done/task_05.md`; all exist. The DAG (T01 → T02) is consistent, and the corrections depend on no pending task.

## Executed validations

- Profile and exclusions: `TokenHound.App` is net10.0-windows WPF. The tests are `tests/TokenHound.Infrastructure.Tests` (net10.0, xUnit v3 on MTP) and `tests/TokenHound.Core.Tests`. E2E is omitted by .NET desktop policy.
- Validated state: base `53da181` plus the current working tree (T01–T05), Debug, run in this review session on 2026-09-27.
- Reused evidence: MA-01..MA-04 and the partial MA-06 from `done/task_02.md#Handoff`; MA-07 from `codereview_3/done/task_05.md#Handoff`. T05 changed only the caption colours and the group header, so the entry points, lifecycle, rows, and columns observed in those runs are unaffected. This session did not relaunch the app, because that needs the user to authorize closing the installed instance.
- Manual acceptance: MA-05 pending (no exhausted account); MA-06 150% scaling and ≥ 10 rows not verified; T04 alert-coloured status for a blocked account not seen on screen; T05 Windows 10 fallback not verified. Owner: the user, at HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal --no-incremental` | passed, 0 errors, 0 warnings (XAML compiles) | NFR-07, T02, T04, T05 |
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
| CR-02 | Info | PRD NFR-04, PRD §User experience | `ProviderStatusWindow.xaml:111-112`: the `ScrollViewer` has no scrollbar style, and neither `DialogResources.xaml` nor `App.xaml` defines a `ScrollBar` or `ScrollViewer` style (`rg -n 'ScrollBar\|ScrollViewer' src/TokenHound.App/UI/Styles src/TokenHound.App/App.xaml` is empty). The T05 handoff observed the default light WPF scrollbar on screen | When the rows overflow, a light system scrollbar appears on the dark panel. No functional effect; NFR-06 scrolling works | No change within the approved contracts (T05 and DEC-10 exclude it). Visual decision for HIL 3: keep the default, or authorize a dark scrollbar style for this window |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_3/CR-01 | persistent (accepted) | Now `codereview_4/CR-01`; DEC-06 code unchanged, as workflow DEC-09 decided |

## Limitations and open items

- Independence: this review ran after `/clear` under the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that implemented T01/T02, issued `codereview_1`..`codereview_3`, and executed T03, T04, and T05 (workflow DEC-05, DEC-06, DEC-08, DEC-11). This context held none of those conversations. It loaded only the snapshot header, next step brief, open threads, and `on-run` entries, and it derived every state from the sources, the code, and commands run here.
- The status is APPROVED WITH RESERVATIONS, not APPROVED, because obligations remain not verifiable by manual evidence. None is a code defect, and the flow presents pending manual acceptance at HIL 3 by design (owner: user):
  - MA-05: dimming and the alert `back in` not seen on screen; FR-10 is unit-proven (TC-08) and the XAML triggers were inspected.
  - MA-06: 150% scaling (NFR-04) and ≥ 10 rows (NFR-06) not verified.
  - T04: the alert-coloured status message for a blocked account not seen on screen.
  - T05: the Windows 10 fallback (dark title bar only) not verified; the code path is the same `EnableDarkMode` call Settings and About use, and the failing DWM calls are ignored.
- The T05 visual (MA-07) is the coordinator's evidence, not re-run here; the user accepts it at HIL 3.
- `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryClineTests.cs` keeps CRLF line endings in the working copy, while the index is LF. Git normalizes them on commit, and `git diff --check` is clean. This is not a finding.
- Jev (shadow): J4 and J5 run only after this report is written and do not inform it.

## Conclusion

Correction T05 delivers workflow DEC-10 within its boundary. The status window's native caption and border take the body colour, with light caption text, through three DWM attributes. The Windows 10 fallback is the existing immersive dark mode. Group headers gain an accent bar, a bold name at the same 16 px, a rounded count badge, and a separator. Only the three files T05 lists changed after `codereview_3`, and the T04 blocked-status trigger is intact. T01–T05 are complete. Every FR, TechSpec decision, and TC-01..TC-10 is conformant by code and passing tests: the App rebuilds with 0 warnings, Infrastructure.Tests passes 828 tests, and Core.Tests passes 92. The quality profile has no unjustified hit, and no escalation trigger fired. No code finding remains. CR-01 is the already-accepted DEC-06 open item, and CR-02 (light scrollbar) is a visual decision for HIL 3. MA-05, the rest of MA-06, the T04 visual, and the T05 Windows 10 fallback remain manual items for the user.
