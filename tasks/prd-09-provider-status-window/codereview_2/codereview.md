# Code review report — Provider Status Window

## Summary

- Status: APPROVED WITH RESERVATIONS
- Git scope: `53da1817ebfc7f76110762dec6da664d0943affa..working tree` (uncommitted T01 + T02 + correction T03 changes under `src/TokenHound.App` and `tests/TokenHound.Infrastructure.Tests`, tracked and untracked; HEAD is still `53da181`)
- Previous review: `tasks/prd-09-provider-status-window/codereview_1/codereview.md` (REJECTED, correction round 1)

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-09-provider-status-window/prd.md` (sha256 `0922887f…1886`, matches approval DEC-03) | read |
| TechSpec | `tasks/prd-09-provider-status-window/techspec.md` (sha256 `9d385cda…55b5`, matches approval DEC-04) | read |
| Manifest | `tasks/prd-09-provider-status-window/tasks.md` (State and Problems and solutions updated after round 1; DAG and contract unchanged) | read |
| Implementation | `git diff 53da181 -- src tests` (21 modified files) + 15 untracked files; handoffs `done/task_01.md`, `done/task_02.md`, `codereview_1/done/task_03.md` | delimited |

Out of scope: the pre-existing `.agents/skills/**` changes recorded in `workflow.md#REC-01`, which are unrelated to this feature.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Tray entry opens single window | `TrayMenuModel.cs:BuildDescriptor`, `TrayIconViewModel.cs:ShowProviderStatus`, `ProviderStatusDialog.cs:25-43` | `TrayMenuModelTests`, `TrayIconViewModelTests.Invoke_ProviderStatus_CallsHudActionOnce` | conformant | Activate-or-create on `_window`; MA-01 pass (`done/task_02.md#Handoff`) |
| FR-02 | HUD menu entry opens same window | `NotchWindow.xaml:37-40`, `NotchWindow.xaml.cs:OnProviderStatusClick`, `HudActionsViewModel.ShowProviderStatus` | `HudActionsViewModelTests.ShowProviderStatus_*` | conformant | Same delegate → same `DialogService._providerStatus`; MA-02 pass |
| FR-03 | Enabled providers only, live enable/disable | `ProviderStatusViewModel.cs:108-125` (`RegisteredProviderIds.Where(IsProviderEnabled)`) | `ProviderStatusViewModelTests` (TC-01, TC-02) | conformant | MA-04 pass |
| FR-04 | Group by family with count | `ProviderStatusProjection.cs:53-66`, `ProviderCatalog.ResolveFamilyName` | `Group_WhenClaudeProfilesAndOtherProvider_GroupsByFamily` | conformant | Default Claude profile first, then display name; MA-03 pass |
| FR-05 | Display name + HUD status message | `ProviderStatusProjection.cs:28,43` | `CreateAccount_WhenNeedsAuth_ReusesHudStatusMessage` | conformant | Reuses `ProviderCatalog.ResolveDefaultName` and `ProviderRingViewModel.ResolveStatusMessage` |
| FR-06 | Columns = HUD rows, same order and label | `ProviderStatusProjection.cs:33-35` → `ProviderUsageRowFactory.CreateRows` | `CreateAccount_WhenSnapshotHasWindows_ColumnsMirrorHudRows`, `..._WhenCopilotCredits_ColumnsMirrorHudRows`, `..._WhenClineFreeLimit_ColumnsMirrorHudRows` | conformant | Key and label sequences asserted equal to `CreateRows` for quota, Copilot, and Cline snapshots (`ProviderStatusProjectionTests.cs:74-123`) |
| FR-07 | Label, used %, bar, reset line; quantity text without fraction | `ProviderStatusProjection.cs:77-90`, `ProviderStatusWindow.xaml:59-91` | `CreateAccount_WhenWindowHasNoFraction_ShowsQuantityWithoutBar`, Copilot/Cline tests (`HasBar` false), `FormatPercent_RoundsLikeTheHud` | conformant | Bar hidden by `HasBar` trigger (`ProviderStatusWindow.xaml:87-89`) |
| FR-08 | Relative + absolute reset / `No reset pending` | `ProviderStatusFormatter.cs:148-160,200-218` | `FormatResetLine_*`; Copilot `in 22 days · 10/18, 22:23`, Cline `in 1 hour · 09/26, 23:53` | conformant | — |
| FR-09 | Colour by ring states | `ProviderStatusFormatter.ResolveLevel`, `ProviderStatusWindow.xaml:32-36` | `ResolveLevel_FollowsRingColourStates` | conformant | Thresholds 0.50/0.80 and brushes per the design doc |
| FR-10 | Exhausted: back in + dimmed + alert reset line | `ProviderStatusProjection.cs:37,44-45,89`, `ProviderStatusWindow.xaml:84-86,158-162` | `CreateAccount_WhenWindowsExhausted_ShowsEarliestComeback`, `..._WhenExhaustedWithoutReset_ShowsLimitReached` | conformant (visual not verifiable) | Unit-proven (TC-08); MA-05 still pending. See CR-02 |
| FR-11 | Live update + minute refresh | `ProviderStatusViewModel.cs:43-50,90-106` | `StoreEvents_WhileOpen_*`, `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | Background events marshalled through `_uiDispatcher`; `_disposed` rechecked on the UI side |
| FR-12 | Empty and pending states | `ProviderStatusViewModel.IsEmpty`, `ProviderStatusProjection.cs:68-75`, `ProviderStatusWindow.xaml:95-98` | `Constructor_WhenNoProviders_IsEmpty`, `CreateAccount_WhenNoSnapshot_ReturnsPendingAccount` | conformant | — |
| NFR-01 | No Core change, no new dependency | — | diff scope | conformant | `git status --porcelain -- src tests` lists no `src/TokenHound.Core` or Infrastructure file and no package change |
| NFR-02 | No invented percentage | `ProviderStatusProjection.cs:82-86`, `ProviderStatusFormatter.FormatBackIn` | `..._ShowsQuantityWithoutBar`, Copilot usage-only and Cline credits columns | conformant | Null fraction → quantity text, level `None`, no bar |
| NFR-03 | Consistency with HUD | Reuse of `CreateRows`, `ResolveStatusMessage`, `ResolveDefaultName`, HUD rounding | TC-05 (quota, Copilot, Cline, NeedsAuth) | conformant | — |
| NFR-04 | Dark style; readable at 100% and 150% | `ProviderStatusWindow.xaml` | MA-06 | not verifiable (150%) | 100% seen in MA-03; 150% scaling not run (`done/task_02.md#Handoff`) |
| NFR-05 | Single instance; release on close; closed at shutdown | `ProviderStatusDialog.cs:46-98`, `DialogService.CloseAll` | `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | VM disposed once via `DisposeViewModel`; tray Exit with window open closed cleanly (handoff T02) |
| NFR-06 | Scroll, ≥ 10 rows usable | `ProviderStatusWindow.xaml:100-171` | MA-06 | not verifiable (≥ 10 rows) | Scrolling seen with 6 accounts; the 10-row case was not exercised |
| NFR-07 | AGENTS.md rules; `App.xaml.cs` growth | all touched files | QA-06, QA-07 | conformant | `App.xaml.cs` 449 = baseline 448 + 1, accepted by TechSpec DEC-03 (workflow DEC-04) |
| TC-01..TC-04 | Unit scenarios | as above | `ProviderStatusViewModelTests`, `ProviderStatusProjectionTests` | conformant | — |
| TC-05 | Claude three windows; Copilot; Cline; NeedsAuth | `ProviderStatusProjection.CreateAccount` | `ProviderStatusProjectionTests.cs:38,74,96,126` | conformant | Copilot and Cline scenarios added by T03 |
| TC-06..TC-08 | Formatter and exhausted scenarios | as above | `ProviderStatusFormatterTests`, `ProviderStatusProjectionTests` | conformant | — |
| TC-09 | `ResetTimeUtc` equals the instant behind `ResetText` at the 3 sites | `ProviderUsageRowFactory.cs:63`, `.Copilot.cs:33`, `.Cline.cs:63` | `CreateRows_WhenQuotaWindowsHaveResets_ExposesResetInstant`; `ProviderUsageRowFactoryCopilotTests.cs:68,121`; `ProviderUsageRowFactoryClineTests.cs:53,88` | conformant | Copilot period reset `FIXED_NOW.AddDays(22)` / null; Cline free-limit `now.AddMinutes(90)` / credits null |
| TC-10 | Entry-point tests | — | `TrayMenuModelTests`, `TrayIconViewModelTests`, `HudActionsViewModelTests` | conformant | Includes the shutdown guard |
| TC-11 | MA-01..MA-06 | — | Windows MCP (coordinator) | pending | MA-01..MA-04 pass; MA-05 pending; MA-06 partial |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (AGENTS.md) | OK | No Core file in the diff |
| Never invent limits or denominators | OK | `ProviderStatusProjection.cs:82-86`; `FormatBackIn(null)` → `limit reached` |
| Never write other tools' credentials | N/A | No credential access added |
| Sealed classes, one class per file, file-scoped namespaces, XML docs | OK | New types are sealed classes/records, a static class, or an enum, all documented |
| File ≤ 300 lines / method ≤ 30 lines | OK | Largest new source file `ProviderStatusWindow.xaml` 173 lines; touched files ≤ 288 except `App.xaml.cs` (baseline + DEC-03) |
| `nameof`, `is null`, collection expressions, `static` lambdas | OK | e.g. `ProviderStatusProjection.cs:37,54-66`, `TrayIconViewModel.ShowProviderStatus` |
| HUD no-activation invariants | N/A | The status window is a normal activating dialog (TechSpec DEC-11); `NotchWindow` HUD hooks untouched |
| MTP test commands with `--minimum-expected-tests 1` | OK | Commands below |
| `dotnet-efficient-validation`, `repository-cli-efficiency` | OK | Loaded in this session before builds, tests, and searches |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void`, `.Result`, `.Wait()`, `GetResult()` | blocking | `rg -n 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 | OK |
| QA-02 | No empty `catch` | blocking | `rg -n 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | 0 | OK |
| QA-03 | No `#nullable disable` / `#pragma warning disable` | blocking | `rg -n '#nullable disable\|#pragma warning disable' $files` | 0 | OK |
| QA-04 | No `DateTime.Now/UtcNow` | reservation | `rg -n 'DateTime\.(Now\|UtcNow)' $files` | 0 | OK |
| QA-05 | No WPF types in CMP-01..CMP-04 | blocking | `rg -n 'System\.Windows\|Dispatcher' src/TokenHound.App/ViewModels/ProviderStatus*.cs src/TokenHound.App/ViewModels/UsageLevel.cs` | 5 lines, all the `_uiDispatcher` `Action<Action>` identifier | OK — regex false positive; no WPF type, proven by the `net10.0` test build |
| QA-06 | File ≤ 300, method ≤ 30 lines | reservation | `wc -l $files` + method review | `App.xaml.cs` 449 (+1 over baseline) | justified by TechSpec `DEC-03` (HIL 2, workflow DEC-04) |
| QA-07 | 4+ parameters | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 2 new of 5: `UsageLevelToBrushConverter.cs:28,38` | justified — signature imposed by `IValueConverter`; 3 pre-existing baseline hits |

- `$files`: the 23 `.cs` files changed or added under `src/` since `53da181`. QA-01..QA-04 were also run over the changed and new test files: 0 hits.
- Terrain baseline: applied from the TechSpec.
- Hits discounted by baseline: 4 (`App.xaml.cs` 448 lines, `App.xaml.cs:366`, `NotchWindow.xaml.cs:105`, `TrayIconViewModel.cs:30`).
- Reservations accumulated in the feature: 4 (QA-06 `App.xaml.cs` +1 and QA-07 ×2, all justified; plus CR-01 below).
- Suggested escalation: no trigger fired (fewer than 8 reservations, no touched file above 500 lines, no duplication in 3+ places).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 columns from `CreateRows` | YES | `ProviderStatusProjection.cs:33-35` |
| DEC-02 `ResetTimeUtc` at 3 sites | YES | `ProviderUsageRowFactory.cs:63`, `.Copilot.cs:33`, `.Cline.cs:63`, all three now asserted (TC-09) |
| DEC-03 `App.xaml.cs` +1 line | YES | `git diff 53da181 -- src/TokenHound.App/App.xaml.cs`: +2/−1 |
| DEC-04 `ResolveStatusMessage` internal | YES | `ProviderRingViewModel.Status.cs:9` |
| DEC-05 family name and ordering | YES | `ProviderCatalog.ResolveFamilyName`, `ProviderStatusProjection.Group` |
| DEC-06 exhausted = any column ≥ 1.0; earliest reset; `limit reached` | YES | `ProviderStatusProjection.cs:37,45,89` |
| DEC-07 percent, level thresholds, no bar without fraction | YES | `ProviderStatusFormatter.cs:130-143` |
| DEC-08 reset line format | YES | `ProviderStatusFormatter.cs:148-218`; culture pattern choice recorded in the T01 handoff |
| DEC-09 `TimeProvider` 60 s timer, dispatcher, dispose | YES | `ProviderStatusViewModel.cs:45-50,78-88` |
| DEC-10 key order and headers | YES | `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `NotchWindow.xaml:37-40` |
| DEC-11 window style and layout | YES | `ProviderStatusWindow.xaml:10-24,100-171`; cell width 220 instead of ≈ 260, recorded in the manifest's Problems and solutions |
| CMP-08 location | PARTIAL (accepted) | Lifecycle moved to `ProviderStatusDialog.cs` under the TechSpec risk mitigation; `DialogService.cs` 274 lines |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Code unchanged since `codereview_1`; its "TC-01..TC-09 pass" criterion is now met through correction T03 |
| T02 | `done/task_02.md` | COMPLETE | TC-10 present; MA-05 pending and MA-06 partial, carried to HIL 3 as the task and manifest record |
| T03 (correction) | `codereview_1/done/task_03.md` | COMPLETE | Tests only. `git status --porcelain -- src` lists the same 25 entries as the reviewed T01/T02 state; the 4 T03 assertions and 2 T03 tests are present and pass |

Manifest links (`tasks.md` → `done/task_01.md`, `done/task_02.md`) resolve. The State section cites `codereview_1/done/task_03.md`. The DAG (T01 → T02) is consistent.

## Executed validations

- Profile and exclusions: `TokenHound.App` is net10.0-windows WPF. The tests are `tests/TokenHound.Infrastructure.Tests` (net10.0, xUnit v3 on MTP) and `tests/TokenHound.Core.Tests`. E2E is omitted by .NET desktop policy.
- Validated state: base `53da181` plus the current working tree, Debug, run in this review session on 2026-09-27.
- Reused evidence: MA-01..MA-04 and the partial MA-06 from `done/task_02.md#Handoff`. They remain valid because T03 changed only tests, and no `src/` or XAML file changed after T02's final run.
- Manual acceptance: MA-05 is pending (no exhausted account). MA-06 150% scaling and ≥ 10 rows are not verified. Owner: the user, at HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | NFR-07, T02 build |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | QA-05 (net10.0 link) |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 821 tests, exit 0 | TC-01..TC-10 |
| `rtk dotnet build tests/TokenHound.Core.Tests --no-restore --nologo --verbosity:minimal` + `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 92 tests, exit 0 | NFR-01 regression |
| `git diff --check 53da181 -- src tests` | no whitespace errors | — |
| Quality profile QA-01..QA-07 | as tabulated | QA-01..QA-07 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Low (reservation) | PRD §User experience | `ProviderStatusWindow.xaml:134-139` always paints `StatusMessage` with `TextSecondaryBrush`; `ProviderStatusAccount` has no blocked flag | The PRD UX asks for the alert colour on status messages "when the account is blocked". The TechSpec did not carry this bullet, so no FR fails. A rate-limited or blocked account still looks the same as one with an informational message | Optional: add an `IsBlocked` flag (e.g. from `Snapshot.ActiveBlock?.IsBlocked`) to `ProviderStatusAccount` and a `DataTrigger` that uses `StatusAlertBrush` |
| CR-02 | Info | FR-10, TechSpec DEC-06, US-03 | `ProviderStatusProjection.cs:45` uses the **earliest** reset among exhausted windows, as DEC-06 states. `ProviderStatusProjection.cs:89` requires a used fraction, so a Cline free-limit block (`UsedFraction` null) is neither dimmed nor given `back in` | With two exhausted windows, `back in` names the earlier reset although the account stays blocked until the later one. A fraction-less block does not count as exhausted. Both weaken US-03 without breaking the approved contract | No code change within the approved contract. A product decision at HIL 3 could switch DEC-06 to the latest reset and/or treat `ActiveBlock.IsBlocked` as exhausted |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_1/CR-01 | resolved | TC-05: `ProviderStatusProjectionTests.cs:74-123` (Copilot and Cline mirroring, quantity text without bar). TC-09: `ProviderUsageRowFactoryCopilotTests.cs:68,121`, `ProviderUsageRowFactoryClineTests.cs:53,88`. 821 tests pass. No `src/` change |
| codereview_1/CR-02 | persistent | Now `codereview_2/CR-01`; `ProviderStatusWindow.xaml:134-139` unchanged. Deferred to HIL 3 by `workflow.md#REV-01` |
| codereview_1/CR-03 | persistent | Now `codereview_2/CR-02`, adding the fraction-less block observation from the T03 handoff; deferred to HIL 3 by `workflow.md#REV-01` |

## Limitations and open items

- Independence: this review ran after `/clear` under the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that implemented T01/T02, issued `codereview_1`, and executed T03 (workflow DEC-05, DEC-06). This context held none of those conversations. It loaded only the snapshot header, next step brief, open threads, and `on-run` entries, and it derived every state from the sources, the code, and commands run here.
- Manual acceptance still open for HIL 3 (owner: user). None of these items is a code defect, and the flow presents pending manual acceptance at HIL 3 by design:
  - MA-05: dimming and the alert `back in` have not been seen on screen, because no exhausted account exists and there is no simulation hook. FR-10 is unit-proven (TC-08), and the XAML triggers were inspected.
  - MA-06: 150% scaling (NFR-04) and ≥ 10 rows (NFR-06) are not verified.
- `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryClineTests.cs` keeps CRLF line endings in the working copy, while the index is LF. Git normalizes them on commit, and `git diff --check` is clean. This is not a finding.
- Jev (shadow): J4 and J5 run only after this report is written and do not inform it.

## Conclusion

Correction T03 resolves `codereview_1/CR-01`. TC-05 and TC-09 now cover the quota, Copilot, and Cline scenarios the TechSpec names, so T01's acceptance is met. T01, T02, and T03 are complete. Every FR, TechSpec decision, and TC-01..TC-10 is conformant by code and passing tests: the App builds with 0 warnings, Infrastructure.Tests passes 821 tests, and Core.Tests passes 92. The quality profile has no unjustified hit, and no escalation trigger fired. The status is **APPROVED WITH RESERVATIONS**. The only open code item is the optional CR-01 (alert colour for blocked accounts). CR-02 is a product observation. MA-05 and the rest of MA-06 remain manual items for the user at HIL 3.
