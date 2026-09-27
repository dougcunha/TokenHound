# Code review report — Provider Status Window

## Summary

- Status: APPROVED WITH RESERVATIONS
- Git scope: `53da1817ebfc7f76110762dec6da664d0943affa..working tree` (uncommitted T01 + T02 + corrections T03 and T04 under `src/TokenHound.App` and `tests/TokenHound.Infrastructure.Tests`, tracked and untracked; HEAD is still `53da181`)
- Previous review: `tasks/prd-09-provider-status-window/codereview_2/codereview.md` (APPROVED WITH RESERVATIONS; its CR-01 was corrected in round 2 under workflow DEC-07)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-09-provider-status-window/prd.md` (sha256 `0922887f…1886`, matches approval DEC-03) | read |
| TechSpec | `tasks/prd-09-provider-status-window/techspec.md` (sha256 `9d385cda…55b5`, matches approval DEC-04) | read |
| Manifest | `tasks/prd-09-provider-status-window/tasks.md` (State and Problems and solutions updated after rounds 1 and 2; DAG and contract unchanged) | read |
| Implementation | `git status --porcelain -- src tests`: 21 modified + 15 untracked files; handoffs `done/task_01.md`, `done/task_02.md`, `codereview_1/done/task_03.md`, `codereview_2/done/task_04.md` | delimited |

- Changed since `codereview_2` (written 14:43): only the four T04 files (`ProviderStatusAccount.cs`, `ProviderStatusProjection.cs`, `ProviderStatusWindow.xaml`, `ProviderStatusProjectionTests.cs`, modified 14:57). Every other file in scope has an earlier modification time, so the `codereview_2` code evidence for them still holds.
- Out of scope: the pre-existing `.agents/skills/**` changes recorded in `workflow.md#REC-01`.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Tray entry opens single window | `TrayMenuModel.BuildDescriptor`, `TrayIconViewModel.ShowProviderStatus`, `ProviderStatusDialog` activate-or-create | `TrayMenuModelTests`, `TrayIconViewModelTests.Invoke_ProviderStatus_CallsHudActionOnce` | conformant | Unchanged since `codereview_2`; MA-01 pass (`done/task_02.md#Handoff`) |
| FR-02 | HUD menu entry opens same window | `NotchWindow.xaml` `ProviderStatusMenuItem`, `NotchWindow.xaml.cs:OnProviderStatusClick`, `HudActionsViewModel.ShowProviderStatus` | `HudActionsViewModelTests.ShowProviderStatus_*` | conformant | Unchanged; MA-02 pass |
| FR-03 | Enabled providers only, live enable/disable | `ProviderStatusViewModel` (`RegisteredProviderIds` filtered by `IsProviderEnabled`) | `ProviderStatusViewModelTests` (TC-01, TC-02) | conformant | Unchanged; MA-04 pass |
| FR-04 | Group by family with count | `ProviderStatusProjection.Group` (`ProviderStatusProjection.cs:54-67`), `ProviderCatalog.ResolveFamilyName` | `Group_WhenClaudeProfilesAndOtherProvider_GroupsByFamily` | conformant | MA-03 pass |
| FR-05 | Display name + HUD status message | `ProviderStatusProjection.cs:28,43` | `CreateAccount_WhenNeedsAuth_ReusesHudStatusMessage` | conformant | Reuses `ResolveDefaultName` and `ProviderRingViewModel.ResolveStatusMessage` |
| FR-06 | Columns = HUD rows, same order and label | `ProviderStatusProjection.cs:33-35` → `ProviderUsageRowFactory.CreateRows` | `..._ColumnsMirrorHudRows` for quota, Copilot, and Cline | conformant | — |
| FR-07 | Label, used %, bar, reset line; quantity text without fraction | `ProviderStatusProjection.CreateColumn` (`:82-95`), `ProviderStatusWindow.xaml` `QuotaColumnTemplate` | `..._ShowsQuantityWithoutBar`, Copilot/Cline `HasBar` false, `FormatPercent_RoundsLikeTheHud` | conformant | — |
| FR-08 | Relative + absolute reset / `No reset pending` | `ProviderStatusFormatter.FormatResetLine` | `FormatResetLine_*` | conformant | — |
| FR-09 | Colour by ring states | `ProviderStatusFormatter.ResolveLevel`, `UsageLevelToBrushConverter` (`ProviderStatusWindow.xaml:32-36`) | `ResolveLevel_FollowsRingColourStates` | conformant | — |
| FR-10 | Exhausted: back in + dimmed + alert reset line | `ProviderStatusProjection.cs:37,45-46,94`, `ProviderStatusWindow.xaml:141-145,160-163` | `CreateAccount_WhenWindowsExhausted_ShowsEarliestComeback`, `..._WhenExhaustedWithoutReset_ShowsLimitReached` | conformant (visual not verifiable) | Unit-proven (TC-08); MA-05 still pending. See CR-01 |
| FR-11 | Live update + minute refresh | `ProviderStatusViewModel` store handlers and `TimeProvider` timer | `StoreEvents_WhileOpen_*`, `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | — |
| FR-12 | Empty and pending states | `ProviderStatusViewModel.IsEmpty`, `ProviderStatusProjection.CreatePendingAccount` (`:73-80`), `ProviderStatusWindow.xaml:95-98` | `Constructor_WhenNoProviders_IsEmpty`, `CreateAccount_WhenNoSnapshot_ReturnsPendingAccount` | conformant | — |
| PRD §User experience | Status message grey, or alert colour when the account is blocked | `ProviderStatusAccount.IsBlocked` (`ProviderStatusAccount.cs:24`), `ProviderStatusProjection.ResolveBlocked` (`:69-71`), `ProviderStatusWindow.xaml:134-140,164-166` | `CreateAccount_ByStatus_MarksBlockedLikeTheHud` (5 cases), `CreateAccount_WithActiveBlock_FollowsItsBlockedFlag` (2 cases), `IsBlocked` false in the NeedsAuth and pending tests (`ProviderStatusProjectionTests.cs:135,221`) | conformant (visual not verifiable) | Authorized by workflow DEC-07. Rule = HUD tooltip error rule (`TooltipCard.xaml.cs:160`, `RateLimited or AccessDenied`) plus `ActiveBlock.IsBlocked`. `UsageBlock.Reason` is `required`, so a blocked `ActiveBlock` always yields a visible message (`ResolveStatusMessage` returns it). Not yet seen on screen |
| NFR-01 | No Core change, no new dependency | — | diff scope | conformant | `git status --porcelain` lists no `src/TokenHound.Core`, Infrastructure, `.props`, or production `.csproj` change |
| NFR-02 | No invented percentage | `CreateColumn`, `ProviderStatusFormatter.FormatBackIn` | `..._ShowsQuantityWithoutBar`; `IsBlocked` reads only existing snapshot fields | conformant | — |
| NFR-03 | Consistency with HUD | Reuse of `CreateRows`, `ResolveStatusMessage`, `ResolveDefaultName`, HUD rounding; blocked colour follows the tooltip's error rule | TC-05, T04 status cases | conformant | Text is identical to the HUD. The window additionally paints an `ActiveBlock.IsBlocked` message in the alert colour, where the tooltip keeps its default style. This is inside DEC-07 ("account-level blocked flag") |
| NFR-04 | Dark style; readable at 100% and 150% | `ProviderStatusWindow.xaml` | MA-06 | not verifiable (150%) | 100% seen in MA-03; 150% not run |
| NFR-05 | Single instance; release on close; closed at shutdown | `ProviderStatusDialog`, `DialogService.CloseAll` | `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | Unchanged since `codereview_2` |
| NFR-06 | Scroll, ≥ 10 rows usable | `ProviderStatusWindow.xaml:100-175` | MA-06 | not verifiable (≥ 10 rows) | Scrolling seen with 6 accounts |
| NFR-07 | AGENTS.md rules; `App.xaml.cs` growth | all touched files | QA-06, QA-07 | conformant | `App.xaml.cs` 449 = baseline 448 + 1 (TechSpec DEC-03, workflow DEC-04); `CreateAccount` 28 lines; T04 files 41/96/177 lines |
| TC-01..TC-10 | Unit scenarios and entry-point tests | as above | `ProviderStatus*Tests`, `ProviderUsageRowFactory*Tests`, tray and HUD action tests | conformant | 828 tests pass in this session |
| TC-11 | MA-01..MA-06 | — | Windows MCP (coordinator) | pending | MA-01..MA-04 pass; MA-05 pending; MA-06 partial; T04 visual added |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (AGENTS.md) | OK | No Core file in the diff |
| Never invent limits or denominators | OK | `IsBlocked` derives from `Snapshot.Status` and `ActiveBlock.IsBlocked` only |
| Never write other tools' credentials | N/A | No credential access added |
| Sealed classes, one class per file, file-scoped namespaces, XML docs | OK | `ProviderStatusAccount.IsBlocked` documented; `ResolveBlocked` private expression-bodied with `=>` on the next line |
| File ≤ 300 lines / method ≤ 30 lines | OK | T04 files 41, 96, 177 lines; other touched files ≤ 288 except `App.xaml.cs` (baseline + DEC-03) |
| `is` patterns, switch/collection expressions, `static` lambdas | OK | `ProviderStatusProjection.cs:70` uses `is … or …` |
| Line endings | OK | T04 files are LF (`file` reports no CRLF) |
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
| QA-07 | 4+ parameters | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 2 new of 5: `UsageLevelToBrushConverter.cs:28,38` | justified — signature imposed by `IValueConverter`; 3 pre-existing baseline hits |

- `$files`: the 23 `.cs` files changed or added under `src/` since `53da181`; `$tfiles`: the 10 changed or added test `.cs` files. T04 adds no hit.
- Terrain baseline: applied from the TechSpec.
- Hits discounted by baseline: 4 (`App.xaml.cs` 448 lines, `App.xaml.cs:366`, `NotchWindow.xaml.cs:105`, `TrayIconViewModel.cs:30`).
- Reservations accumulated in the feature: 3 (QA-06 `App.xaml.cs` +1 and QA-07 ×2, all justified). `codereview_2/CR-01` is resolved and no longer counts.
- Suggested escalation: no trigger fired (fewer than 8 reservations, no touched file above 500 lines, no duplication in 3+ places).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 columns from `CreateRows` | YES | `ProviderStatusProjection.cs:33-35` |
| DEC-02 `ResetTimeUtc` at 3 sites | YES | `ProviderUsageRowFactory.cs`, `.Copilot.cs`, `.Cline.cs`; TC-09 asserts all three |
| DEC-03 `App.xaml.cs` +1 line | YES | 449 lines |
| DEC-04 `ResolveStatusMessage` internal | YES | `ProviderRingViewModel.Status.cs:9` |
| DEC-05 family name and ordering | YES | `ProviderCatalog.ResolveFamilyName`, `ProviderStatusProjection.Group` |
| DEC-06 exhausted = any column ≥ 1.0; earliest reset; `limit reached` | YES | `ProviderStatusProjection.cs:37,46,94`; T04 left it untouched, as its boundary requires |
| DEC-07 percent, level thresholds, no bar without fraction | YES | `ProviderStatusFormatter` |
| DEC-08 reset line format | YES | `ProviderStatusFormatter.FormatResetLine` |
| DEC-09 `TimeProvider` 60 s timer, dispatcher, dispose | YES | `ProviderStatusViewModel` |
| DEC-10 key order and headers | YES | `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `NotchWindow.xaml` |
| DEC-11 window style and layout | YES | `ProviderStatusWindow.xaml`; cell width 220 instead of ≈ 260, recorded in the manifest |
| CMP-04 records | YES (extended) | `ProviderStatusAccount.IsBlocked` added by T04 under workflow DEC-07 |
| CMP-08 location | PARTIAL (accepted) | Lifecycle in `ProviderStatusDialog.cs` under the TechSpec risk mitigation; `DialogService.cs` 274 lines |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Code unchanged since `codereview_2`; TC-01..TC-09 pass |
| T02 | `done/task_02.md` | COMPLETE | TC-10 present; MA-05 pending and MA-06 partial, carried to HIL 3 |
| T03 (correction) | `codereview_1/done/task_03.md` | COMPLETE | Unchanged; resolved `codereview_1/CR-01` (verified in `codereview_2`) |
| T04 (correction) | `codereview_2/done/task_04.md` | COMPLETE | All four work items checked. Handoff claims match the code: `IsBlocked` at `ProviderStatusAccount.cs:24`, `ResolveBlocked` at `ProviderStatusProjection.cs:69-71`, `DataTrigger` at `ProviderStatusWindow.xaml:164-166`, 7 new test cases. Focused `*ProviderStatus*` run: 42 passed. Full run: 828 passed, matching the handoff. No Core or Infrastructure file changed |

Manifest links (`tasks.md` → `done/task_01.md`, `done/task_02.md`) resolve. The State section cites `codereview_1/done/task_03.md` and `codereview_2/done/task_04.md`, and both exist. The DAG (T01 → T02) is consistent.

## Executed validations

- Profile and exclusions: `TokenHound.App` is net10.0-windows WPF. The tests are `tests/TokenHound.Infrastructure.Tests` (net10.0, xUnit v3 on MTP) and `tests/TokenHound.Core.Tests`. E2E is omitted by .NET desktop policy.
- Validated state: base `53da181` plus the current working tree (T01–T04), Debug, run in this review session on 2026-09-27. The installed `D:\Apps\TokenHound\TokenHound.App.exe` was running and did not lock the repository output.
- Reused evidence: MA-01..MA-04 and the partial MA-06 from `done/task_02.md#Handoff`. T04 changed only the status-message foreground trigger and one record property, so layout, entry points, and lifecycle observed in those runs are unaffected.
- Manual acceptance: MA-05 pending (no exhausted account); MA-06 150% scaling and ≥ 10 rows not verified; T04 alert-coloured status for a rate-limited, access-denied, or blocked account not seen on screen. Owner: the user, at HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 errors, 0 warnings (XAML compiles) | NFR-07, T02, T04 XAML |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | QA-05 (net10.0 link) |
| `rtk dotnet build tests/TokenHound.Core.Tests --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | NFR-01 regression |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 828 tests, exit 0 | TC-01..TC-10, T04 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderStatus*"` | passed, 42 tests, exit 0 | TC-01..TC-08, T04 |
| `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 92 tests, exit 0 | NFR-01 regression |
| `git diff --check 53da181 -- src tests` | no whitespace errors | — |
| Quality profile QA-01..QA-07 | as tabulated | QA-01..QA-07 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Info | FR-10, TechSpec DEC-06, US-03 | `ProviderStatusProjection.cs:46` counts `back in` down to the **earliest** reset among exhausted windows, as DEC-06 states. `ProviderStatusProjection.cs:94` requires a used fraction, so a Cline free-limit block (`UsedFraction` null) is not dimmed and gets no `back in`. Since T04, that block's status message does turn alert-coloured (`IsBlocked`) | With two exhausted windows, `back in` names the earlier reset although the account stays blocked until the later one. A fraction-less block is flagged by colour but not dimmed. Both weaken US-03 without breaking the approved contract | No code change within the approved contract. Product decision at HIL 3: keep DEC-06, or switch to the latest reset and/or treat `ActiveBlock.IsBlocked` as exhausted |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_2/CR-01 | resolved | `ProviderStatusAccount.IsBlocked` + `ProviderStatusProjection.ResolveBlocked` + `ProviderStatusWindow.xaml:164-166` `DataTrigger` to `StatusAlertBrush`; tests `CreateAccount_ByStatus_MarksBlockedLikeTheHud`, `CreateAccount_WithActiveBlock_FollowsItsBlockedFlag`; 828 pass. The on-screen colour remains a manual HIL 3 item |
| codereview_2/CR-02 | persistent | Now `codereview_3/CR-01`; DEC-06 code unchanged. Deferred to HIL 3 by `workflow.md#REV-02` and `#DEC-07` |

## Limitations and open items

- Independence: this review ran after `/clear` under the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that implemented T01/T02, issued `codereview_1` and `codereview_2`, and executed T03 and T04 (workflow DEC-05, DEC-06, DEC-08). This context held none of those conversations. It loaded only the snapshot header, next step brief, open threads, and `on-run` entries, and it derived every state from the sources, the code, and commands run here.
- The status is APPROVED WITH RESERVATIONS, not APPROVED, because obligations remain not verifiable by manual evidence. None of them is a code defect, and the flow presents pending manual acceptance at HIL 3 by design (owner: user):
  - MA-05: dimming and the alert `back in` have not been seen on screen; FR-10 is unit-proven (TC-08) and the XAML triggers were inspected.
  - MA-06: 150% scaling (NFR-04) and ≥ 10 rows (NFR-06) are not verified.
  - T04: the alert-coloured status message for a blocked account has not been seen on screen; the trigger was inspected and the XAML compiles.
- `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryClineTests.cs` keeps CRLF line endings in the working copy, while the index is LF. Git normalizes them on commit, and `git diff --check` is clean. This is not a finding.
- Jev (shadow): J4 and J5 run only after this report is written and do not inform it.

## Conclusion

Correction T04 resolves `codereview_2/CR-01`. Blocked accounts (`RateLimited`, `AccessDenied`, or an active block) now show their status message in the alert colour, and seven new test cases pin the rule both ways. T04 stayed within its DEC-07 boundary and changed only its four files. T01–T04 are complete. Every FR, TechSpec decision, and TC-01..TC-10 is conformant by code and passing tests: the App builds with 0 warnings, Infrastructure.Tests passes 828 tests, and Core.Tests passes 92. The quality profile has no unjustified hit, and no escalation trigger fired. No code finding remains. CR-01 is a product observation for HIL 3, and MA-05, the rest of MA-06, and the T04 visual check remain manual items for the user.
