# Code review report — Provider Status Window

## Summary

- Status: REJECTED
- Git scope: `53da1817ebfc7f76110762dec6da664d0943affa..working tree` (uncommitted T01 + T02 changes under `src/TokenHound.App` and `tests/TokenHound.Infrastructure.Tests`, tracked and untracked)
- Previous review: —

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-09-provider-status-window/prd.md` (sha256 `0922887f…1886`, approved DEC-03) | read |
| TechSpec | `tasks/prd-09-provider-status-window/techspec.md` (sha256 `9d385cda…55b5`, approved DEC-04) | read |
| Manifest | `tasks/prd-09-provider-status-window/tasks.md` | read |
| Implementation | `git diff 53da181 -- src tests` (19 modified files) + 15 untracked files; handoffs `done/task_01.md`, `done/task_02.md` | delimited |

Out of scope: pre-existing `.agents/skills/**` changes recorded in `workflow.md#REC-01`, which are unrelated to this feature.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01 | Tray entry opens single window | `TrayMenuModel.cs:BuildDescriptor`, `TrayIconViewModel.cs:ShowProviderStatus`, `ProviderStatusDialog.cs:Show` | `TrayMenuModelTests`, `TrayIconViewModelTests.Invoke_ProviderStatus_CallsHudActionOnce` | conformant | Activate-or-create in `ProviderStatusDialog.Show`; MA-01 pass (handoff T02) |
| FR-02 | HUD menu entry opens same window | `NotchWindow.xaml:37-40`, `NotchWindow.xaml.cs:OnProviderStatusClick`, `HudActionsViewModel.ShowProviderStatus` | `HudActionsViewModelTests.ShowProviderStatus_*` | conformant | Same `HudActionsViewModel` delegate → same `DialogService`; MA-02 pass |
| FR-03 | Enabled providers only, live enable/disable | `ProviderStatusViewModel.cs:Rebuild` (`RegisteredProviderIds.Where(IsProviderEnabled)`) | `Constructor_WhenProvidersMixed_ShowsEnabledAndPendingOnly`, `StoreEvents_WhileOpen_RebuildThroughDispatcher` | conformant | MA-04 pass |
| FR-04 | Group by family with count | `ProviderStatusProjection.cs:Group`, `ProviderCatalog.ResolveFamilyName` | `Group_WhenClaudeProfilesAndOtherProvider_GroupsByFamily` | conformant | Default Claude first, then display name; MA-03 pass |
| FR-05 | Display name + HUD status message | `ProviderStatusProjection.cs:28,43` | `CreateAccount_WhenNeedsAuth_ReusesHudStatusMessage` | conformant | Same `ResolveDefaultName` as `ProviderRingViewModel.cs:44` |
| FR-06 | Columns = HUD rows, same order and label | `ProviderStatusProjection.cs:CreateAccount` → `ProviderUsageRowFactory.CreateRows` | `CreateAccount_WhenSnapshotHasWindows_ColumnsMirrorHudRows` (Claude only) | conformant | Pass-through of `CreateRows`; see CR-01 for the missing Copilot/Cline scenarios |
| FR-07 | Label, used %, bar, reset line; quantity text without fraction | `ProviderStatusProjection.cs:CreateColumn`, `ProviderStatusWindow.xaml:59-91` | `CreateAccount_WhenWindowHasNoFraction_ShowsQuantityWithoutBar`, `FormatPercent_RoundsLikeTheHud` | conformant | Bar hidden when `HasBar` false |
| FR-08 | Relative + absolute reset / `No reset pending` | `ProviderStatusFormatter.cs:FormatResetLine` | `FormatResetLine_*` (4 cases, invariant and pt-BR) | conformant | — |
| FR-09 | Colour by ring states | `ProviderStatusFormatter.ResolveLevel`, `ProviderStatusWindow.xaml:32-36` | `ResolveLevel_FollowsRingColourStates` | conformant | Brushes equal `docs/design/2026-08-28-usage-notch-design.md:57-59` |
| FR-10 | Exhausted: back in + dimmed + alert reset line | `ProviderStatusProjection.cs:45`, `ProviderStatusWindow.xaml:84-86,158-162` | `CreateAccount_WhenWindowsExhausted_ShowsEarliestComeback`, `..._WithoutReset_ShowsLimitReached` | conformant (visual not verifiable) | Unit-proven; MA-05 pending (no exhausted account). See CR-03 |
| FR-11 | Live update + minute refresh | `ProviderStatusViewModel.cs` events + `TimeProvider.CreateTimer` | `StoreEvents_WhileOpen_*`, `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | — |
| FR-12 | Empty and pending states | `ProviderStatusViewModel.IsEmpty`, `ProviderStatusProjection.CreatePendingAccount`, `ProviderStatusWindow.xaml:95-98` | `Constructor_WhenNoProviders_IsEmpty`, `CreateAccount_WhenNoSnapshot_ReturnsPendingAccount` | conformant | — |
| NFR-01 | No Core change, no new dependency | — | diff scope | conformant | `git diff --stat 53da181` touches no `src/TokenHound.Core` or Infrastructure file and no package reference |
| NFR-02 | No invented percentage | `ProviderStatusProjection.cs:CreateColumn`, `ProviderStatusFormatter.FormatBackIn` | `..._ShowsQuantityWithoutBar`, `FormatBackIn_WhenResetUnknown_ShowsLimitReached` | conformant | — |
| NFR-03 | Consistency with HUD | Reuse of `CreateRows`, `ResolveStatusMessage`, `ResolveDefaultName`, HUD rounding | TC-05 (partial) | conformant | Rounding identical to `ProviderUsageRowFactory.cs:75` |
| NFR-04 | Dark style; readable at 100% and 150% | `ProviderStatusWindow.xaml` | MA-06 | not verifiable (150%) | 100% seen in MA-03; 150% not run (handoff T02) |
| NFR-05 | Single instance; release on close; closed at shutdown | `ProviderStatusDialog.cs`, `DialogService.CloseAll`, `ApplicationLifetime.cs:129` | `TimerTick_RefreshesCountdownsUntilDisposed` | conformant | Idempotent `Dispose`; tray Exit with window open closed cleanly (handoff T02) |
| NFR-06 | Scroll, ≥ 10 rows usable | `ProviderStatusWindow.xaml:100-171` | MA-06 | not verifiable (≥ 10 rows) | Scrolling observed with 6 accounts; 10-row case not exercised |
| NFR-07 | AGENTS.md rules; `App.xaml.cs` growth | all touched files | QA-06, QA-07 | conformant | `App.xaml.cs` 449 = baseline 448 + 1, accepted by TechSpec DEC-03 at HIL 2 |
| TC-01..TC-04 | Unit scenarios | as above | `ProviderStatusViewModelTests`, `ProviderStatusProjectionTests` | conformant | — |
| TC-05 | Claude three windows; **Copilot; Cline**; NeedsAuth | — | `ProviderStatusProjectionTests` | non-conformant | Copilot and Cline scenarios absent (CR-01) |
| TC-06..TC-08 | Formatter and exhausted scenarios | as above | `ProviderStatusFormatterTests`, `ProviderStatusProjectionTests` | conformant | — |
| TC-09 | `ResetTimeUtc` equals the instant behind `ResetText` in **existing factory scenarios** | `ProviderUsageRowFactory.cs:63`, `.Copilot.cs:33`, `.Cline.cs:63` | `CreateRows_WhenQuotaWindowsHaveResets_ExposesResetInstant` | non-conformant | Only the quota site is asserted; Copilot and Cline sites untested (CR-01) |
| TC-10 | Entry-point tests | — | `TrayMenuModelTests`, `TrayIconViewModelTests`, `HudActionsViewModelTests` | conformant | Includes shutdown guard |
| TC-11 | MA-01..MA-06 | — | Windows MCP (coordinator) | pending | MA-01..MA-04 pass; MA-05 pending; MA-06 partial |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Core purity (AGENTS.md) | OK | No Core file in the diff |
| Never invent limits or denominators | OK | `ProviderStatusProjection.cs:CreateColumn` uses quantity text when `UsedFraction` is null; `FormatBackIn(null)` → `limit reached` |
| Never write other tools' credentials | N/A | No credential access added |
| Sealed classes, one class per file, file-scoped namespaces, XML docs | OK | All new types sealed/static/enum/record, documented |
| File ≤ 300 lines / method ≤ 30 lines | OK | Largest new file 173 lines (`ProviderStatusWindow.xaml`); `App.xaml.cs` baseline + DEC-03 |
| `nameof`, `is null`, collection expressions, `static` lambdas | OK | e.g. `ProviderStatusProjection.cs:Group`, `TrayIconViewModel.ShowProviderStatus` |
| HUD no-activation invariants | N/A | Status window is a normal activating dialog (TechSpec DEC-11); `NotchWindow` HUD hooks untouched |
| MTP test commands with `--minimum-expected-tests 1` | OK | Commands below |
| `dotnet-efficient-validation`, `repository-cli-efficiency` | OK | Loaded before build/tests and searches in this review |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `async void`, `.Result`, `.Wait()`, `GetResult()` | blocking | `rg -n --type cs 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | 0 | OK |
| QA-02 | No empty `catch` | blocking | `rg -n --type cs 'catch\s*\{\s*\}\|…' $files` | 0 | OK |
| QA-03 | No `#nullable disable` / `#pragma warning disable` | blocking | `rg -n --type cs '#nullable disable\|#pragma warning disable' $files` | 0 | OK |
| QA-04 | No `DateTime.Now/UtcNow` | reservation | `rg -n --type cs 'DateTime\.(Now\|UtcNow)' $files` | 0 | OK |
| QA-05 | No WPF types in CMP-01..CMP-04 | blocking | `rg -n 'System\.Windows\|Dispatcher' src/TokenHound.App/ViewModels/ProviderStatus*.cs src/TokenHound.App/ViewModels/UsageLevel.cs` | 5 lines, all the `_uiDispatcher` `Action<Action>` identifier | OK — regex false positive; no WPF type, proven by the `net10.0` test build |
| QA-06 | File ≤ 300, method ≤ 30 lines | reservation | `rg -c '^' $files` + method review | `App.xaml.cs` 449 (+1 over baseline) | justified by TechSpec `DEC-03` (HIL 2, workflow DEC-04) |
| QA-07 | 4+ parameters | reservation | `rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | 2 new of 5: `UsageLevelToBrushConverter.cs:28,38` | justified — signature imposed by `IValueConverter`; 3 pre-existing baseline hits |

- `$files`: the 23 `.cs` files changed or added under `src/` since `53da181`.
- Terrain baseline: applied from the TechSpec.
- Hits discounted by baseline: 4 (`App.xaml.cs` 448 lines, `App.xaml.cs:366`, `NotchWindow.xaml.cs:105`, `TrayIconViewModel.cs:30`).
- Reservations accumulated in the feature: 3 (QA-06 `App.xaml.cs` +1; QA-07 ×2 interface-imposed), all justified.
- Suggested escalation: no trigger fired (fewer than 8 reservations, no touched file above 500 lines, no duplication in 3+ places).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 columns from `CreateRows` | YES | `ProviderStatusProjection.cs:32-34` |
| DEC-02 `ResetTimeUtc` at 3 sites | YES | `ProviderUsageRowFactory.cs:63`, `.Copilot.cs:33`, `.Cline.cs:63` (2 sites untested, CR-01) |
| DEC-03 `App.xaml.cs` +1 line | YES | `git diff 53da181 -- src/TokenHound.App/App.xaml.cs`: +2/−1 |
| DEC-04 `ResolveStatusMessage` internal | YES | `ProviderRingViewModel.Status.cs:9` |
| DEC-05 family name and ordering | YES | `ProviderCatalog.ResolveFamilyName`, `ProviderStatusProjection.Group` |
| DEC-06 exhausted = any column ≥ 1.0; earliest reset; `limit reached` | YES | `ProviderStatusProjection.cs:36,45` |
| DEC-07 percent, level thresholds, no bar without fraction | YES | `ProviderStatusFormatter.cs:FormatPercent/ResolveLevel` |
| DEC-08 reset line format | YES | `ProviderStatusFormatter.cs:FormatResetLine`; culture pattern choice recorded in T01 handoff |
| DEC-09 `TimeProvider` 60 s timer, dispatcher, dispose | YES | `ProviderStatusViewModel.cs` constructor/`Dispose` |
| DEC-10 key order and headers | YES | `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `NotchWindow.xaml` |
| DEC-11 window style and layout | YES | `ProviderStatusWindow.xaml:10-24` (1100×640, min 520×300, `ScrollViewer`, `WrapPanel`); cell width 220 instead of ≈ 260, recorded in manifest Problems and solutions |
| CMP-08 location | PARTIAL (accepted) | Lifecycle moved to `ProviderStatusDialog.cs` per the TechSpec risk mitigation; `DialogService.cs` 274 lines |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | INCOMPLETE | Code complete; acceptance criterion "TC-01..TC-09 pass" is not fully met — TC-05 and TC-09 lack the Copilot and Cline scenarios (CR-01) |
| T02 | `done/task_02.md` | COMPLETE | TC-10 present; MA-05 pending and MA-06 partial as the task's acceptance allows, carried to HIL 3 |

Manifest links (`tasks.md` → `done/task_01.md`, `done/task_02.md`) resolve; state and DAG (T01 → T02) are consistent.

## Executed validations

- Profile and exclusions: `TokenHound.App` net10.0-windows WPF; tests `tests/TokenHound.Infrastructure.Tests` (net10.0, xUnit v3 on MTP) and `tests/TokenHound.Core.Tests`. E2E omitted by .NET desktop policy.
- Validated state: base `53da181` + current working tree, Debug, run in this review session on 2026-09-27.
- Reused evidence: manual acceptance MA-01..MA-04 and the partial MA-06 from `done/task_02.md#Handoff` (same code; the only later change was the XAML cell width, which the re-run covered).
- Manual acceptance: MA-05 pending (no exhausted account); MA-06 150% scaling and ≥ 10 rows not verified. Owner: user at HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | NFR-07, T02 build |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | QA-05 (net10.0 link) |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 819 tests | TC-01..TC-10 (as implemented) |
| `rtk dotnet test --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1` | passed, 92 tests | NFR-01 regression |
| Quality profile QA-01..QA-07 | as tabulated | QA-01..QA-07 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | High (blocking) | TC-05, TC-09, T01 acceptance | `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusProjectionTests.cs:37` covers only a Claude snapshot; no test builds a Copilot or Cline snapshot for the projection. `ProviderUsageRowFactoryTests.cs:235` asserts `ResetTimeUtc` only for quota rows; `ProviderUsageRowFactory.Copilot.cs:33` and `ProviderUsageRowFactory.Cline.cs:63` are not asserted anywhere (`rg "\.ResetTimeUtc\.Should"` has no hit for those rows) | Two planned TechSpec test scenarios are missing, while T01 is recorded as meeting "TC-01..TC-09 pass". A regression in the Copilot or Cline reset instants (FR-08, FR-10) or column mirroring (FR-06) would go undetected | Add a Copilot credit snapshot and a Cline free-limit snapshot to `ProviderStatusProjectionTests` (columns equal `CreateRows` keys/labels), and assert `ResetTimeUtc` for the Copilot period reset and the Cline `ActiveBlock` reset in `ProviderUsageRowFactoryTests` |
| CR-02 | Low (reservation) | PRD §User experience | `ProviderStatusWindow.xaml:134-139` always paints `StatusMessage` with `TextSecondaryBrush` | PRD UX says status messages use the alert colour "when the account is blocked"; the TechSpec did not carry this bullet, so no FR fails, but a rate-limited/blocked account is visually identical to an informational message | Optional: add an `IsBlocked` flag (e.g. from `Snapshot.ActiveBlock?.IsBlocked`) to `ProviderStatusAccount` and a trigger to use `StatusAlertBrush` |
| CR-03 | Info | FR-10, TechSpec DEC-06, US-03 | `ProviderStatusProjection.cs:45` uses the **earliest** reset among exhausted windows, exactly as DEC-06 states | When two windows are exhausted (e.g. 5-hour and 7-day), `back in` names the earlier reset although the account stays blocked until the later one, which weakens US-03 ("know when it becomes usable again") | No code change within the approved contract; a product decision at HIL 3 could switch DEC-06 to the latest reset |

## Limitations and open items

- Independence: this review ran after `/clear` in the same host session ID (`session_018B5BxLe8ZPhppy5EJH7b43`) that implemented T01/T02 (workflow DEC-05). The reviewing context contained none of the authoring conversation; it loaded only the snapshot header, brief, open threads, and `on-run` entries, and derived every state from the sources and code.
- MA-05 (exhausted account visual) is not verifiable at review time: no exhausted account and no runtime simulation hook exists. FR-10 is unit-proven (TC-08) and the XAML triggers were inspected.
- NFR-04 150% scaling and NFR-06 ≥ 10 rows are not verified; they require a display-setting change or more accounts. Owner: user at HIL 3.
- Jev (shadow): points J4/J5 run only after this report is written; they do not inform it.

## Conclusion

The implementation meets every functional requirement and TechSpec decision by code and unit evidence, builds with no warnings, and passes 819 + 92 tests. The quality profile has no unjustified hit. The status is **REJECTED** only because of CR-01: T01's acceptance requires TC-05 and TC-09 as specified, and their Copilot and Cline scenarios are missing. The correction is test-only and falls within the HIL 2 authorization. CR-02 is an optional improvement, and CR-03 is a product observation for HIL 3. MA-05 and the rest of MA-06 remain open manual items for HIL 3.
