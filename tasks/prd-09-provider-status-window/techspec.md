# TechSpec — Provider Status Window

## Sources and traceability

- PRD: `tasks/prd-09-provider-status-window/prd.md` (approved, DEC-03 in `workflow.md`)
- Applicable instructions, rules, and skills: `AGENTS.md` (C# structure and style, MTP test rules, desktop HUD validation), `dotnet-efficient-validation`, `repository-cli-efficiency`
- Specs and design: `docs/design/2026-08-28-usage-notch-design.md` §Ring colour states; reference screenshot from HIL 0 (DEC-01)
- Evidence in existing code:
  - `src/TokenHound.App/UI/Windows/DialogService.cs:ShowSettings/CreateSettingsWindow/CloseAll` — single-instance modeless dialog with owned, disposed view model
  - `src/TokenHound.App/ViewModels/HudActionsViewModel.cs:ShowSettings/ShowAbout` — shutdown-guarded UI actions shared by the tray and the HUD menu
  - `src/TokenHound.App/UI/Tray/TrayMenuModel.cs:BuildDescriptor`, `TrayMenuItemKey.cs`, `TrayIconViewModel.cs:Invoke` — tray entries
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml:30-51` (`CapsuleContextMenu`), `NotchWindow.xaml.cs:OnSettingsClick` — HUD right-click menu
  - `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs:CreateRows/AddQuotaRows` — the per-provider usage rows (column source)
  - `src/TokenHound.App/ViewModels/ProviderRingViewModel.Status.cs:ResolveStatusMessage` — HUD status text
  - `src/TokenHound.App/ViewModels/ProviderCatalog.cs:ResolveDefaultName` — account display names
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs` constructor, `OnSnapshotUpdated`, `ApplyEnablement` — live update and enablement pattern
  - `src/TokenHound.Infrastructure/Engine/UsageStore.cs:SnapshotUpdated/CurrentSnapshots`, `UsageStore.Gating.cs:RegisteredProviderIds/IsProviderEnabled/ProviderEnablementChanged`
  - `src/TokenHound.App/App.xaml.cs:InitializeUi` — composition root
  - `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` — WPF-free App sources are linked with `<Compile Include>` and tested on `net10.0`

## Solution summary

A new modeless `ProviderStatusWindow` is opened through a new `DialogService.ShowProviderStatus`. The same `HudActionsViewModel.ShowProviderStatus` action serves both entry points: a new tray entry and a new HUD context-menu item. The window binds to a WPF-free `ProviderStatusViewModel`. It builds groups of accounts from `UsageStore` (registered and enabled providers, current snapshots), reacts to `SnapshotUpdated` and `ProviderEnablementChanged` through the UI dispatcher, and re-projects countdowns once a minute through an injected `TimeProvider` timer.

Each account's columns are projected from `ProviderUsageRowFactory.CreateRows`. That keeps labels, order, fractions, and quantity texts identical to the HUD tooltip (NFR-03). The window's own formatter supplies the percentage, bar colour level, reset line, and "back in" text. `TokenHound.Core` and Infrastructure are untouched (NFR-01). All presentation logic lives in WPF-free types linked into the existing test project; the XAML window stays thin.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-06, FR-07, NFR-03 | Columns come from `ProviderUsageRowFactory.CreateRows(snapshot, timeProvider)`, one column per row, same order and label | The factory is the HUD tooltip's single source of rows for every provider (quota, Copilot credits, Cline) | A new per-provider mapping would duplicate labels and drift from the HUD |
| DEC-02 | FR-08, FR-10 | Add `DateTimeOffset? ResetTimeUtc` to the App record `ProviderUsageRow`, set next to each existing `ResetText` assignment (3 sites) | The window needs the instant, not the preformatted "Resets in…" text; `ProviderUsageRow` is an App presentation record, not a Core DTO | Re-deriving reset instants from `LimitWindows` by row key would duplicate the factory's fallback rules (e.g. `ActiveBlock` for index 0) |
| DEC-03 | NFR-07 | `App.xaml.cs` grows by exactly one line: the new `showProviderStatus` argument of `HudActionsViewModel`; the status VM factory is passed to `DialogService.ShowProviderStatus` inside that same lambda | Wiring must happen at the composition root; the one-line argument adds no responsibility. The literal "must not grow further" in NFR-07 cannot be met without an unrelated extraction | Extracting part of `App.xaml.cs` would be an unrequested refactoring; **deviation from the NFR-07 literal, presented at HIL 2** |
| DEC-04 | FR-05, NFR-03 | Change `ProviderRingViewModel.ResolveStatusMessage` from `private static` to `internal static` and reuse it | Same status text as the HUD, one-word change | Copying the switch would duplicate it |
| DEC-05 | FR-04 | Add `ProviderCatalog.ResolveFamilyName(providerId)`: `claude` and `claude-*` → `Claude`; any other id → `ResolveDefaultName(id)`. Groups are ordered by family name (ordinal, case-insensitive); accounts inside a group put the default profile (`claude`) first, then display name | Deterministic order; `RegisteredProviderIds` ordering is not guaranteed (`UsageStore.Gating.cs`) | Reusing the HUD ring order (`_ringOrder`) couples the window to `NotchViewModel` internals |
| DEC-06 | FR-10 | Exhausted = any column with `UsedFraction >= 1.0`; `BackInText` counts down to the earliest `ResetTimeUtc` among exhausted columns; without a reset time the account is still dimmed and shows `limit reached` | PRD assumption; no invented reset | Using `ActiveBlock` alone would miss percent-only windows |
| DEC-07 | FR-07, FR-09, NFR-02 | Percent text = `{round(UsedFraction*100)}%` using the HUD rounding (`Math.Round`); colour level from `UsedFraction`: `< 0.50` Green, `< 0.80` Yellow, otherwise Orange; a column without `UsedFraction` shows `PrimaryQuantityText`, no bar, level `None` | Matches the HUD "% Used" and the design colour table | — |
| DEC-08 | FR-08 | Reset line = `in {relative} · {absolute}`; relative: `< 1h` → `N minutes` (min 1), `< 24h` → `N hours`, else `N days` (floor, singular for 1); absolute: local time via `TimeProvider.LocalTimeZone`, `CultureInfo.CurrentCulture` month/day pattern without year + short time (e.g. `09/27, 13:00`); no reset → `No reset pending`; reset in the past → `resetting now` | Reference format; the clock is injected for deterministic tests | — |
| DEC-09 | FR-11, NFR-05 | Countdowns refresh through `TimeProvider.CreateTimer` every 60 s, marshalled through the UI dispatcher delegate; the VM disposes the timer and unsubscribes the store events in `Dispose`, which `DialogService` calls when the window closes | WPF-free, testable with a fake time provider; follows the Settings VM ownership pattern | `DispatcherTimer` is WPF-only and cannot be linked into the `net10.0` test project |
| DEC-10 | FR-01, FR-02 | New `TrayMenuItemKey.ProviderStatus` declared between `RefreshNow` and `Settings`; tray header `Provider Status…`; HUD menu item `Provider Status` placed before `Settings` | The key order is load-bearing and mirrors the menu layout (`TrayMenuItemKey.cs` summary); the enum is not persisted | Appending at the end would put it after `Exit` |
| DEC-11 | NFR-04, NFR-06 | The window is a standard activating WPF `Window` (not a HUD window) using `DialogResources.xaml` brushes, `SizeToContent="Manual"`, default 1100×640, min 520×300, `ScrollViewer` over the groups, columns in a `WrapPanel` of fixed-width (≈ 260 px) cells | The PRD requires normal modeless behaviour; the HUD no-activation invariants apply only to HUD windows | — |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.App/ViewModels/ProviderStatusViewModel.cs` | New | Holds `ObservableCollection<ProviderStatusGroup> Groups`, `IsEmpty`; builds from `RegisteredProviderIds` + `IsProviderEnabled` + `CurrentSnapshots`; rebuilds on `SnapshotUpdated`, `ProviderEnablementChanged`, and the 60 s timer; `IDisposable` | CMP-02, CMP-03, `UsageStore`, `TimeProvider`, `Action<Action>` dispatcher |
| CMP-02 | `src/TokenHound.App/ViewModels/ProviderStatusProjection.cs` | New | Pure static: `(providerId, Snapshot?, now, TimeProvider) → ProviderStatusAccount`; groups accounts into `ProviderStatusGroup` records (DEC-05, DEC-06, DEC-07) | `ProviderUsageRowFactory`, `ProviderCatalog`, `ProviderRingViewModel.ResolveStatusMessage`, CMP-03 |
| CMP-03 | `src/TokenHound.App/ViewModels/ProviderStatusFormatter.cs` | New | Pure static text formatting: percent, reset line, back-in, colour level (DEC-07, DEC-08) | `TimeProvider`, `CultureInfo` |
| CMP-04 | `src/TokenHound.App/ViewModels/ProviderStatusGroup.cs`, `ProviderStatusAccount.cs`, `ProviderStatusColumn.cs`, `UsageLevel.cs` | New | Immutable records (`required`/`init`) and the colour-level enum bound by the XAML | — |
| CMP-05 | `src/TokenHound.App/ViewModels/ProviderUsageRow.cs`, `ProviderUsageRowFactory.cs`, `.Copilot.cs`, `.Cline.cs` | Modified | Add and set `ResetTimeUtc` (DEC-02) | — |
| CMP-06 | `src/TokenHound.App/ViewModels/ProviderCatalog.cs`, `ProviderRingViewModel.Status.cs` | Modified | `ResolveFamilyName` (DEC-05); `ResolveStatusMessage` visibility (DEC-04) | — |
| CMP-07 | `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml(.cs)` + `src/TokenHound.App/UI/Converters/UsageLevelToBrushConverter.cs` | New | Dark layout per NFR-04; binds CMP-01; dims exhausted accounts (opacity 0.45); level → brush | CMP-01, `DialogResources.xaml` |
| CMP-08 | `src/TokenHound.App/UI/Windows/DialogService.cs` | Modified | `ShowProviderStatus(owner, viewModelFactory)`, `IsProviderStatusOpen`, `CloseProviderStatus`; `CloseAll` also closes it; VM disposed once on close | CMP-07 |
| CMP-09 | `HudActionsViewModel.cs`, `TrayMenuItemKey.cs`, `TrayMenuModel.cs`, `TrayIconViewModel.cs`, `NotchWindow.xaml(.cs)`, `App.xaml.cs` | Modified | Entry points and wiring (DEC-03, DEC-10) | CMP-08 |

Flow: tray `ProviderStatus` or HUD menu item → `HudActionsViewModel.ShowProviderStatus()` (no-op while shutting down) → delegate from `App.InitializeUi` → `DialogService.ShowProviderStatus(_notchWindow, () => new ProviderStatusViewModel(usageStore, DispatchUiAction))` → existing window activated, or new window created with the VM. Store events (background threads) → dispatcher → `Rebuild()` → `ProjectAll(now)` replaces `Groups` content → bindings refresh. Window `Closed` → `DialogService` disposes the VM → events unsubscribed, timer disposed.

## Contracts and data

- `ProviderUsageRow` (App presentation record, not persisted, not serialized): new optional `DateTimeOffset? ResetTimeUtc { get; init; }`. Existing members unchanged; existing consumers unaffected.
- `TrayMenuItemKey`: new member `ProviderStatus`, inserted between `RefreshNow` and `Settings`. Not persisted; `TrayMenuModelTests` expectations change with it.
- `HudActionsViewModel` constructor: new optional trailing parameter `Action? showProviderStatus = null`. Existing call sites and tests compile unchanged.
- No configuration, JSON, snapshot, or Core model change.

## Integrations and interfaces

- `UsageStore` (read-only use): `RegisteredProviderIds`, `IsProviderEnabled`, `CurrentSnapshots`, events `SnapshotUpdated`, `ProviderEnablementChanged`. No refresh is triggered by the window.
- Tray: entry `Provider Status…` → `TrayIconViewModel.Invoke(TrayMenuItemKey.ProviderStatus)`.
- HUD: `MenuItem x:Name="ProviderStatusMenuItem" Header="Provider Status"` in `CapsuleContextMenu`, click → `_actionsViewModel?.ShowProviderStatus()`.

## Errors, security, and recovery

- Errors and edges:
  - No enabled provider → `IsEmpty` true, empty-state text `No providers are enabled. Enable providers in Settings.` (FR-12).
  - Enabled provider without a snapshot → account with `IsPending` and `Waiting for first reading`, no columns (FR-12).
  - Snapshot with zero rows and an error status → status message only.
  - Mock fallback snapshots live in `NotchViewModel` only and never reach the store, so the window never shows mock data.
- Credentials and sensitive data: none read; the account column shows only the existing display name (no email, DEC-02 of workflow).
- Concurrency and idempotency: every store event and timer tick is marshalled through the dispatcher before touching `Groups`. `Dispose` is idempotent; handlers check a `_disposed` flag so a tick that is already queued after dispose is a no-op. A rebuild reads `CurrentSnapshots` fresh, so event order does not matter.
- Rollback or reversal: revert the commits; no persisted state or migration.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| T01 Projection, formatting, and VM (CMP-01..CMP-06) | — | New unit tests green; existing ViewModel tests green |
| T02 Window, dialog service, entry points, wiring (CMP-07..CMP-09) | T01 | Tray/HUD action tests green; App builds; manual acceptance MA-01..MA-06 |

## Test approach

- Profile: `TokenHound.App` is `net10.0-windows` with `UseWPF` (`src/TokenHound.App/TokenHound.App.csproj:28-31`). WPF-free App sources are linked into `tests/TokenHound.Infrastructure.Tests` (`net10.0`, xUnit v3 on MTP, AwesomeAssertions, NSubstitute). New WPF-free files (CMP-01..CMP-04) must be added to that project's `<Compile Include>` list.
- Commands (per `AGENTS.md`, `dotnet-efficient-validation`):
  - Build: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj` and `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
  - Focused: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ProviderStatus*"`
  - Regression: the same project without a filter, and `tests/TokenHound.Core.Tests`
- E2E: omitted by .NET desktop policy.
- Command prerequisites and exclusions: stop a running `TokenHound.App` before building (locked output). No UI automation suites.
- Manual acceptance (owner: user, run by the coordinator via Windows MCP `App` launch + `Screenshot` `display: [2]`):
  - MA-01: tray → `Provider Status…` opens the window; a second click activates the same window. Expected: exactly one window.
  - MA-02: HUD right-click → `Provider Status` activates the same single window.
  - MA-03: with ≥ 2 Claude profiles and one other provider, the `Claude` group shows the right count, and each row shows the HUD's columns with %, bar, and reset line. Expected: values match the HUD tooltip.
  - MA-04: disable a provider in Settings while the window is open. Expected: its row disappears; re-enabling restores it.
  - MA-05: an exhausted account (or a simulated 100% snapshot) is dimmed and shows `back in …` in the alert colour.
  - MA-06: layout is readable at 100% and 150% scaling; narrowing the window wraps columns; many rows scroll.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-03, FR-12 | unit | Store with enabled/disabled/pending providers | Only enabled ones; pending shows waiting text; none → `IsEmpty` | `ProviderStatusViewModelTests` |
| TC-02 | FR-03, FR-11 | unit | Raise `ProviderEnablementChanged` and a new snapshot | Groups rebuilt through the dispatcher | `ProviderStatusViewModelTests` |
| TC-03 | FR-11, NFR-05 | unit | Advance fake time 60 s; then dispose and advance again | Reset lines re-projected; after dispose no rebuild and no handler invoked | `ProviderStatusViewModelTests` |
| TC-04 | FR-04, DEC-05 | unit | `claude`, `claude-work`, `codex` | `Claude` group count 2 with default first; `Codex` group count 1; ordered by name | `ProviderStatusProjectionTests` |
| TC-05 | FR-05, FR-06, NFR-03 | unit | Claude snapshot with three windows; Copilot; Cline; NeedsAuth | Columns equal `CreateRows` labels/order; status equals `ResolveStatusMessage` | `ProviderStatusProjectionTests` |
| TC-06 | FR-07, FR-09, NFR-02 | unit | Fractions 0.21/0.52/0.85/null | `21%` Green, `52%` Yellow, `85%` Orange; null → quantity text, no bar, `None` | `ProviderStatusFormatterTests` |
| TC-07 | FR-08 | unit | Reset in 37 min, 14 h, 3 d, none, past; fixed culture en-US and time zone | `in 37 minutes · …`, `in 14 hours · 09/27, 13:00`, `in 3 days · …`, `No reset pending`, `resetting now` | `ProviderStatusFormatterTests` |
| TC-08 | FR-10 | unit | One column at 1.0 with reset; one at 1.0 without reset | `IsExhausted`, `back in 14h 37m`; without reset → `limit reached` | `ProviderStatusProjectionTests` |
| TC-09 | DEC-02 | unit | Existing factory scenarios | `ResetTimeUtc` equals the instant behind `ResetText` | `ProviderUsageRowFactoryTests` (extended) |
| TC-10 | FR-01, FR-02 | unit | Tray descriptor and `Invoke(ProviderStatus)`; HUD action while shutting down | Entry order/header; delegate called once; no call during shutdown | `TrayMenuModelTests`, `TrayIconViewModelTests`, `HudActionsViewModelTests` |
| TC-11 | FR-01, FR-02, NFR-04..NFR-06 | manual | MA-01..MA-06 | As scripted | Windows MCP |

## Quality profile

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | No `async void` outside event handlers, no `.Result`/`.Wait()`/`GetAwaiter().GetResult()` | blocking | `rtk rg -n --type cs @src 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' $files` | — |
| QA-02 | No empty `catch` | blocking | `rtk rg -n --type cs @src 'catch\s*\{\s*\}\|catch \(Exception\w*\)\s*\{\s*\}' $files` | — |
| QA-03 | No `#nullable disable` / `#pragma warning disable` | blocking | `rtk rg -n --type cs @src '#nullable disable\|#pragma warning disable' $files` | — |
| QA-04 | No `DateTime.Now/UtcNow`; time only through `TimeProvider` | reservation | `rtk rg -n --type cs @src 'DateTime\.(Now\|UtcNow)' $files` | — |
| QA-05 | No WPF types in CMP-01..CMP-04 (linked into `net10.0` tests) | blocking | `rtk rg -n 'System\.Windows\|Dispatcher' src/TokenHound.App/ViewModels/ProviderStatus*.cs src/TokenHound.App/ViewModels/UsageLevel.cs` | — |
| QA-06 | AGENTS.md size limits: file ≤ 300 lines, method ≤ 30 lines | reservation | `rtk rg -c '^' --type cs @src $files` and review of methods | `App.xaml.cs` pre-existing 448 lines (baseline) |
| QA-07 | Parameter list with 4+ parameters | reservation | `rtk rg -n --type cs @src '\w+\((?:[^),]+,){3,}[^)]*\)' $files` | — |

- Verification scope: files in each task diff.
- Escalation trigger: 8+ reservations, a touched file above 500 lines, or duplication in 3+ places.

### Terrain baseline

| File | Lines | Public members | Constructor deps | Cases | Pre-existing hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.App/App.xaml.cs` | 448 | 0 | 0 | 0 | QA-06: 448 lines (> 300); QA-07: `App.xaml.cs:366` | recorded; +1 line accepted in DEC-03 |
| `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs` | 282 | 2 | 0 | 0 | QA-07: `NotchWindow.xaml.cs:105` (`WndProc`) | recorded |
| `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` | 247 | 6 | 5 | 0 | — | recorded |
| `src/TokenHound.App/UI/Windows/DialogService.cs` | 225 | 5 | 1 | 0 | — | recorded; expected ≈ 280 lines after CMP-08, within 300 |
| `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs` | 166 | 7 | 4 | 5 | QA-07: `TrayIconViewModel.cs:30` | recorded |
| `src/TokenHound.App/ViewModels/ProviderCatalog.cs` | 136 | 3 | 0 | 0 | — | recorded |
| `src/TokenHound.App/UI/Tray/TrayMenuModel.cs` | 81 | 2 | 0 | 0 | — | recorded |
| `src/TokenHound.App/ViewModels/ProviderRingViewModel.Status.cs` | 37 | 0 | 0 | 0 | — | recorded |
| `src/TokenHound.App/UI/Tray/TrayMenuItemKey.cs` | 22 | 0 | 0 | 0 | — | recorded |
| `src/TokenHound.App/ViewModels/ProviderUsageRow.cs`, `ProviderUsageRowFactory*.cs` | ≤ 176 | — | 0 | 0 | not measured by the greppable set; no threshold crossed | recorded |

- Preparatory refactoring: not recommended. No target file crosses a structural threshold (500 lines, 10 public members, 6 dependencies, 10 cases). `DialogService` gains a third dialog pair within its 300-line limit.

## Observability and rollout

- Signals: `Log.Information("Provider status window opened")` and `closed`, with no payload; existing store logs cover the data.
- Migration and compatibility: none.
- Rollout and rollback: ships with the app build; rollback by reverting the commits.

## Risks and open items

- Risk: `DialogService` approaches 300 lines (medium, maintenance only). Mitigation: shared private helper for activate-or-create if the count passes 300 during T02.
- Risk: culture month/day pattern varies (low). Mitigation: TC-07 pins `en-US`; other cultures use their own `MonthDayPattern` stripped of the month name when the pattern is textual — if it proves awkward, fall back to `MM/dd` (decide in T01 and record in the handoff).
- Open item: DEC-03 deviation from the NFR-07 literal needs acceptance at HIL 2 (owner: user).

## Relevant files

- Modify: `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/UI/Windows/DialogService.cs`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml`, `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs`, `src/TokenHound.App/UI/Tray/TrayMenuItemKey.cs`, `src/TokenHound.App/UI/Tray/TrayMenuModel.cs`, `src/TokenHound.App/UI/Tray/TrayIconViewModel.cs`, `src/TokenHound.App/ViewModels/HudActionsViewModel.cs`, `src/TokenHound.App/ViewModels/ProviderCatalog.cs`, `src/TokenHound.App/ViewModels/ProviderRingViewModel.Status.cs`, `src/TokenHound.App/ViewModels/ProviderUsageRow.cs`, `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs`, `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Copilot.cs`, `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.Cline.cs`, `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`, `tests/TokenHound.Infrastructure.Tests/Tray/TrayMenuModelTests.cs`, `tests/TokenHound.Infrastructure.Tests/Tray/TrayIconViewModelTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/HudActionsViewModelTests.cs`, `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderUsageRowFactoryTests.cs`
- Create: `src/TokenHound.App/ViewModels/ProviderStatusViewModel.cs`, `ProviderStatusProjection.cs`, `ProviderStatusFormatter.cs`, `ProviderStatusGroup.cs`, `ProviderStatusAccount.cs`, `ProviderStatusColumn.cs`, `UsageLevel.cs`; `src/TokenHound.App/UI/Windows/ProviderStatusWindow.xaml(.cs)`; `src/TokenHound.App/UI/Converters/UsageLevelToBrushConverter.cs`; `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderStatusViewModelTests.cs`, `ProviderStatusProjectionTests.cs`, `ProviderStatusFormatterTests.cs`
