# TechSpec — Provider Enablement, Status Visibility & Engine Gating in Settings

## Sources and traceability

- PRD: `tasks/prd-settings-01-provider-management/prd.md`
- Applicable instructions and skills: `AGENTS.md` (architecture invariants, C# style, MTP test commands), `dotnet-efficient-validation` (+ `references/mtp.md`), `repository-cli-efficiency`, `no-workarounds`
- Evidence in existing code:
  - `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs` — `JsonNode` DOM read-modify-write preserving sibling sections; `ResolveFilePath`; swallow-and-default on corrupt JSON
  - `src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs`, `RefreshSettings.cs` — read-only section store + `record` with `init` and resolved fallbacks
  - `src/TokenHound.Infrastructure/Engine/UsageStore.cs:207`, `UsageStore.Refresh.cs`, `UsageStore.Activity.cs` — `partial` engine; `_providers`/`_monitors` registries; `RefreshCoreAsync` is the single choke point for `TickAsync` and `RefreshNowAsync`; `IsRateLimited` guards dispatch
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs:202` — `ObservableCollection<ProviderRingViewModel> Rings`, `SnapshotUpdated`/`ActivityUpdated` subscription, `UpdateOrAddRing`
  - `src/TokenHound.App/ViewModels/HudActionsViewModel.cs` — VM pattern: injected `Func`/`Action` delegates, `INotifyPropertyChanged`, **no `ICommand` anywhere in the repository**
  - `src/TokenHound.App/ViewModels/ProviderCatalog.cs` — `ResolveDefaultName` / `ResolveDefaultBadge` / `ResolveGlyphKey` / `ResolveGlyphScale`, already mapping `copilot` and treating `gemini` and `antigravity` as one provider
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` — shell with empty `Border` body, `DialogButtonStyle`, `Esc` handling, `WindowPlacement.EnableDarkMode`
  - `src/TokenHound.App/UI/Windows/DialogService.cs:189` — single-instance modeless lifecycle, `new SettingsWindow()` with no DataContext
  - `src/TokenHound.App/UI/Styles/DialogResources.xaml` — dark palette brushes and control templates
  - `src/TokenHound.App/App.xaml.cs:205` — `CreateUsageStore` → `RegisterProviders` → `InitializeUi` → `ScheduleInitialRefresh`
  - `src/TokenHound.App/UI/Windows/NotchWindow.xaml:58` — `ItemsControl ItemsSource="{Binding Rings}"`; window is `SizeToContent="WidthAndHeight"` with `MinWidth="180" MinHeight="48"`
  - `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` — App ViewModels reached by `<Compile Include Link=...>` into a `net10.0` project (no `UseWPF`)

### PRD reconciliation (decided with the user this session)

| # | PRD statement | Repository reality | Resolution |
| --- | --- | --- | --- |
| R-1 | "Exactly the four registered providers" (FR-01, Constraints) | `App.xaml.cs:RegisterProviders` registers **five**; Copilot added by merge `95e65bb` after the PRD was written | FR-01 is read as "all registered providers". Settings enumerates `UsageStore` registrations dynamically — Copilot included, provider #6 needs no Settings change. |
| R-2 | A-01 persists the key `"antigravity"` | `AntigravityUsageProvider.PROVIDER_ID` / `AntigravityActivityMonitor.PROVIDER_ID` are `"gemini"` | Canonical persisted key is the runtime `IUsageProvider.ProviderId` (`"gemini"`). A legacy `"antigravity"` key is honored on read and rewritten as `"gemini"` on the next save. No config-key→provider-id translation exists in the gating path. |
| R-3 | OBJ-02/FR-05 claim `ProviderStatus` contains `Disabled` | `ProviderStatus` is `Ok, Stale, NeedsAuth, AccessDenied, RateLimited, **Unsupported**` — no `Disabled`, and `Unsupported` has no badge in the PRD | `Disabled` and `Checking…` are presentation states, not provider health; they live in a new App-layer enum (DEC-04). `Unsupported` gains a badge the PRD omitted — see OPEN-01. |

## Solution summary

Enabled state becomes engine truth. `UsageStore` gains a gating registry keyed by `IUsageProvider.ProviderId`, consulted at the two existing choke points — `RefreshCoreAsync` (covering both `TickAsync` and `RefreshNowAsync`) and `PollActivityAsync`/`CheckAnyBusyAsync` — so a disabled provider issues no HTTP call, IPC command, SQLite read, or credential file read from either the usage path or the activity path. The store raises `ProviderEnablementChanged`, which both view models observe; this reuses the event-driven shape `NotchViewModel` already uses for `SnapshotUpdated`/`ActivityUpdated` instead of introducing a coordinator type. Re-enabling calls a new `RefreshProviderNowAsync` that routes through the unchanged `RefreshProviderAsync`, so the existing `IsRateLimited` guard keeps honoring unexpired 429 deadlines (NFR-06).

Persistence follows `HudPositionStore` exactly: a new `ProviderSettingsStore` reads and writes only the `"Providers"` object of `appsettings.json` through a `JsonNode` DOM, leaving `Hud`, `Refresh`, and `Log` untouched, and defaulting every absent key to enabled. Because two stores now write the same file, both take a shared per-path async gate (DEC-07). The Settings dialog gets a real body: a `SettingsViewModel` built from `UsageStore.RegisteredProviderIds`, one `ProviderToggleViewModel` per provider carrying a `ProviderBadgeState` pill and a two-way `IsMonitored` property whose setter drives the whole chain — engine gate, HUD ring, async disk write — with no Apply button. Both view models are written free of WPF types so they link into the existing `net10.0` test project.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-06, FR-07, NFR-05, A-01 | New `ProviderSettingsStore` + `ProviderSettings` record in `TokenHound.Infrastructure/Configuration`, reading/writing only the `"Providers"` section via `JsonNode` DOM; unreadable/corrupt file returns all-enabled defaults | Mirrors `HudPositionStore` line for line (`ReadRoot`, `ResolveFilePath`, `JSON_OPTIONS` with `WriteIndented`), which the PRD names as the proven pattern and which `HudPositionStoreTests` already pins. Keeps Core pure (NFR-05) | Binding the whole file to a POCO and reserializing — would drop `Log` sub-objects and comments. Separate `providers.json` — splits settings across files and contradicts FR-06 |
| DEC-02 | R-2, FR-06, FR-07 | Canonical key = runtime `IUsageProvider.ProviderId`. `"antigravity"` accepted as a read-only alias for `"gemini"`, normalized on the next write. Lookup uses `StringComparer.OrdinalIgnoreCase` | Gating matches providers by `ProviderId`; any other key space needs a translation layer that can silently fail to gate. Alias preserves any hand-edited config already using the PRD's spelling. `ProviderCatalog` already collapses `gemini`/`antigravity` to one display name | Renaming `PROVIDER_ID` to `antigravity` — touches the adapter, its monitor, their tests, and archived snapshot keys in `UsageArchive`. Persisting `"antigravity"` and mapping inward — extra drift surface in the hot gating path |
| DEC-03 | FR-04, NFR-04, NFR-06 | Gating registry lives in `UsageStore` as a new partial `UsageStore.Gating.cs`: `ConcurrentDictionary<string,bool>` (absent ⇒ enabled), `SetProviderEnabled`, `IsProviderEnabled`, `RegisteredProviderIds`, `RefreshProviderNowAsync`, `event ProviderEnablementChanged`. Gate applied in `RefreshCoreAsync`, `PollActivityAsync`, and `CheckAnyBusyAsync` | `RefreshCoreAsync` is the sole path for both `TickAsync` and `RefreshNowAsync`, so one guard satisfies FR-04 for both. Activity monitors do process and file inspection, so FR-04's "no file reads" requires gating them too. `UsageStore.cs` is at 207 of the 300-line cap in `AGENTS.md`; the class is already `partial` across `.Refresh.cs`/`.Activity.cs` | An injected `Func<string,bool>` predicate — pushes truth back into App and leaves the engine unable to answer `IsProviderEnabled` for tests. Unregistering the provider on disable — loses the adapter instance and its `IDisposable` ownership, and breaks re-enable |
| DEC-04 | OBJ-02, FR-05, NFR-05, A-03, A-04, R-3 | New App-layer `ProviderBadgeState` enum (`Checking, Ok, Stale, NeedsAuth, RateLimited, AccessDenied, Unsupported, Disabled`) plus a static `ProviderBadgeResolver` mapping `(isMonitored, Snapshot?)` → state, label, and brush keys. `TokenHound.Core.Models.ProviderStatus` is **not** modified | `Disabled` is user configuration and `Checking…` is a UI lifecycle state; neither is provider health, and adding them to the Core enum would force every `switch` over `ProviderStatus` (`ProviderRingViewModel.Status.cs`, `ProviderRing.xaml`, retention policy) to handle non-health cases. NFR-05 requires Core purity | Extending `ProviderStatus` — pollutes the domain and silently changes HUD ring rendering. A bool + nullable status pair in the VM — pushes the mapping into XAML converters, defeating NFR-02's "text, not color alone" |
| DEC-05 | FR-03, OBJ-03, FR-10 | `NotchViewModel` keeps every known ring in a private `_ringsByProvider` dictionary plus a first-seen `_ringOrder` list; the bound `Rings` collection holds only monitored providers, re-inserted at their original index on enable | `Rings` is bound directly by `NotchWindow.xaml:58`, so add/remove gives the immediate HUD effect FR-03 demands with no `CollectionView` plumbing. Index-preserving re-insert stops rings from jumping to the end of the capsule on every toggle — HUD ring re-ordering is explicitly out of scope | `ICollectionViewSource` filtering — needs `Refresh()` marshalling and drags a `WindowsBase` dependency into a VM that must stay linkable into the `net10.0` test project. Hiding via `Visibility` converter — leaves `StackPanel` gaps |
| DEC-06 | FR-02, FR-09, OBJ-03, NFR-02 | `SettingsViewModel` + `ProviderToggleViewModel` in `TokenHound.App/ViewModels`, using injected `Action`/`Func` delegates and a two-way `IsMonitored` property — no `ICommand`, no WPF types. Toggle setter order: engine gate → badge refresh → fire-and-forget persist | The repository contains no `ICommand` or `RelayCommand`; `HudActionsViewModel` and `NotchViewModel` both take delegates and are compiled into `TokenHound.Infrastructure.Tests` (`net10.0`, no `UseWPF`), where `System.Windows.Input.ICommand` is unavailable. Engine-first ordering gives the <50ms visual response of NFR-04 without waiting on disk | An MVVM toolkit package — new dependency for one dialog, and `AGENTS.md` requires justifying dependencies with a proven gap. Code-behind event handlers only — untestable, blocking TC-13/TC-14 |
| DEC-07 | FR-06, NFR-04 | New internal `SettingsFileGate` holding a `SemaphoreSlim` per resolved absolute path; `ProviderSettingsStore.SaveAsync` and `HudPositionStore.Save` both acquire it | Both stores now do read-modify-write on `appsettings.json`. A HUD drag saving `Hud` while a toggle saves `Providers` is a genuine lost-update: whichever reads first overwrites the other's section. FR-06 requires preserving all other sections | Accepting last-writer-wins — silently drops a user's toggle or HUD position. A named OS mutex — cross-process protection this single-instance app does not need, plus abandoned-mutex handling |
| DEC-08 | FR-07, OBJ-04, US-05 | Enablement is loaded and applied in `App.OnStartup` **between** `RegisterProviders` and `InitializeUi` | `UsageStore`'s constructor calls `LoadArchive()`, which populates `_snapshots` from disk; `NotchViewModel`'s constructor then builds a ring for every restored snapshot. Applying the gate after `InitializeUi` would flash rings for disabled providers on every restart. `autoStart: true` starts the timer but the first tick is one `ActiveInterval` (180s) away, so no race with `ScheduleInitialRefresh` | Filtering inside `NotchViewModel`'s constructor from its own store read — duplicates config loading in the App layer and breaks the single source of truth |
| DEC-09 | FR-04, NFR-04 | Disabling a provider clears its `_activityStates` entry (under `_activityLock`) and raises `ActivityUpdated` with a null session; its snapshot stays in `CurrentSnapshots` | `HasBusyActivity` scans `_activityStates` and drives `RefreshSchedulePolicy.ShouldRefresh`; a stale `Busy` entry for a provider no longer being polled would pin the whole store to the active cadence forever — an overhead NFR-04 forbids. Keeping the snapshot lets the ring and badge reappear instantly on re-enable, before the dispatched refresh returns | Clearing snapshots too — forces a blank ring and a network round-trip on every re-enable |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.Infrastructure/Configuration/ProviderSettings.cs` | New | `record` holding `IReadOnlyDictionary<string, bool>`; `IsEnabled(providerId)` defaulting to `true` | — |
| CMP-02 | `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs` | New | Load/`SaveAsync` the `"Providers"` section; alias normalization; section-preserving DOM write | CMP-01, CMP-03 |
| CMP-03 | `src/TokenHound.Infrastructure/Configuration/SettingsFileGate.cs` | New | Per-path `SemaphoreSlim` serializing writes to `appsettings.json` | — |
| CMP-04 | `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs` | Modified | `Save` acquires CMP-03 before its read-modify-write | CMP-03 |
| CMP-05 | `src/TokenHound.Infrastructure/Engine/UsageStore.Gating.cs` | New (partial of existing class) | Enablement registry, `RegisteredProviderIds`, `SetProviderEnabled`, `IsProviderEnabled`, `RefreshProviderNowAsync`, `ProviderEnablementChanged` | CMP-06 |
| CMP-06 | `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs` / `UsageStore.Activity.cs` | Modified | Skip disabled providers in `RefreshCoreAsync`, `PollActivityAsync`, `CheckAnyBusyAsync`; clear activity state on disable | CMP-05 |
| CMP-07 | `src/TokenHound.App/ViewModels/ProviderBadgeState.cs` | New | Presentation state enum (DEC-04) | — |
| CMP-08 | `src/TokenHound.App/ViewModels/ProviderBadgeResolver.cs` | New | `(bool isMonitored, Snapshot?)` → state, display label, background/foreground resource keys | CMP-07, `ProviderStatus` |
| CMP-09 | `src/TokenHound.App/ViewModels/ProviderToggleViewModel.cs` | New | One row: id, display name, glyph, badge, two-way `IsMonitored` | CMP-08, `ProviderCatalog` |
| CMP-10 | `src/TokenHound.App/ViewModels/SettingsViewModel.cs` | New | Builds rows from `RegisteredProviderIds`; routes toggles; subscribes to `SnapshotUpdated` for live badges; `IDisposable` | CMP-05, CMP-09, CMP-02 |
| CMP-11 | `src/TokenHound.App/ViewModels/NotchViewModel.cs` | Modified | Ring filtering by enablement (DEC-05); subscribe/unsubscribe `ProviderEnablementChanged` | CMP-05 |
| CMP-12 | `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` (+ `.cs`) | Modified | Real body: header, provider card `ItemsControl`, footer; `Height` raised for 5 rows | CMP-10, CMP-13 |
| CMP-13 | `src/TokenHound.App/UI/Styles/DialogResources.xaml` | Modified | Badge pill brushes/style per NFR-01 palette; accessible toggle style with `DialogFocusVisualStyle` | — |
| CMP-14 | `src/TokenHound.App/UI/Windows/DialogService.cs` | Modified | `ShowSettings` accepts a `Func<SettingsViewModel>` factory; disposes the VM on close | CMP-10 |
| CMP-15 | `src/TokenHound.App/App.xaml.cs` | Modified | Load settings, apply gate before `InitializeUi` (DEC-08), supply the VM factory | CMP-02, CMP-05, CMP-14 |
| CMP-16 | `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` | Modified | `<Compile Include Link=…>` entries for CMP-07 – CMP-10 | — |

### Flow

**Startup (FR-07, DEC-08).** `OnStartup` creates the store (archive restore populates `_snapshots`), registers the five adapters and monitors, then loads `ProviderSettings` and calls `SetProviderEnabled` for each registered id. Only then is `NotchViewModel` constructed, so it builds rings solely for monitored providers. `ScheduleInitialRefresh` runs `RefreshNowAsync`, which the gate already filters.

**Toggle off (FR-03, FR-04, FR-09, NFR-04).** The checkbox writes `IsMonitored = false`. The setter calls `UsageStore.SetProviderEnabled(id, false)`, which flips the registry, clears the activity entry (DEC-09), and raises `ProviderEnablementChanged`. `NotchViewModel` removes the ring from `Rings` — the HUD reflows immediately via `SizeToContent`. `SettingsViewModel` re-resolves the row's badge to `Disabled`. Persistence is dispatched as an untracked `SaveAsync` behind `SettingsFileGate`, so the UI thread never touches disk. The next `TickAsync` skips the provider entirely.

**Toggle on (US-04, NFR-06).** The registry flips, the ring is re-inserted at its original index from the cached snapshot, the badge becomes `Checking…` if no snapshot exists yet, and `RefreshProviderNowAsync(id)` is dispatched. That call enters through `RefreshProviderAsync`, whose existing `IsRateLimited` check returns early when a persisted 429 deadline has not elapsed — no premature retry, no new code path around the rate limiter.

**Zero enabled (FR-10, A-05).** `Rings` empties; `NotchWindow`'s `SizeToContent="WidthAndHeight"` with `MinWidth="180" MinHeight="48"` collapses the capsule to its minimum while keeping it visible, draggable, and right-clickable. `NotchViewModel`'s mock fallback stays dormant because `App.xaml.cs` constructs it with `mockProvider: null`, and every `LoadMockFallback` trigger is guarded by `_mockProvider is not null`.

## Contracts and data

### `appsettings.json` — new `"Providers"` section

Sibling of the existing `Hud`, `Refresh`, and `Log` objects; all three are preserved byte-for-byte in structure by the DOM write.

```json
{
  "Providers": {
    "claude":  { "Enabled": true },
    "gemini":  { "Enabled": false },
    "codex":   { "Enabled": true },
    "cursor":  { "Enabled": true },
    "copilot": { "Enabled": true }
  }
}
```

| Field | Type | Required | Validation | Compatibility |
| --- | --- | --- | --- | --- |
| `Providers` | object | No | Absent ⇒ every provider enabled | New section; older builds ignore it |
| `Providers.<providerId>` | object | No | Key matched case-insensitively against `IUsageProvider.ProviderId`. Unknown keys are preserved on write, never deleted | `"antigravity"` read as `"gemini"`; rewritten canonically on next save. Both keys present ⇒ `"gemini"` wins |
| `Providers.<providerId>.Enabled` | bool | No | Non-boolean or absent ⇒ `true` | — |

### `UsageStore` public surface additions

| Member | Signature | Notes |
| --- | --- | --- |
| `RegisteredProviderIds` | `IReadOnlyCollection<string>` | Snapshot of `_providers.Keys`; ordering is not guaranteed by `ConcurrentDictionary`, so CMP-10 sorts by `ProviderCatalog.ResolveDefaultName` for a stable dialog |
| `IsProviderEnabled` | `bool IsProviderEnabled(string providerId)` | Unknown id ⇒ `true` |
| `SetProviderEnabled` | `void SetProviderEnabled(string providerId, bool isEnabled)` | Idempotent; raises the event only on an actual change |
| `RefreshProviderNowAsync` | `Task RefreshProviderNowAsync(string providerId, CancellationToken ct = default)` | No-op for an unknown or disabled id; honors `_refreshLock` and `IsRateLimited` |
| `ProviderEnablementChanged` | `event EventHandler<ProviderEnablementChangedEventArgs>?` | `record`-style args carrying `ProviderId` and `IsEnabled`, matching `ProviderActivityChangedEventArgs` |

## Integrations and interfaces

- **Settings dialog (FR-01, FR-02, FR-05, FR-08, FR-09).** `SettingsWindow` binds `ItemsControl.ItemsSource` to `SettingsViewModel.Providers`. Each row: glyph `Path` from `ProviderGlyphs.xaml` via `GlyphKey`, display name, badge pill, right-aligned `CheckBox` bound two-way to `IsMonitored` with `UpdateSourceTrigger=PropertyChanged`. `Height` rises from 280 to ~420 DIPs for five rows plus header and footer; `MinHeight` follows. `DialogService` single-instance behavior, `IsCancel="True"` Close, and the `Esc` handler in `SettingsWindow.OnKeyDown` are already implemented and unchanged.
- **HUD (FR-03, NFR-03).** Only `NotchViewModel.Rings` membership changes. `NotchWindow.xaml`, `WindowStyles.EnableNonActivating`, and the `WM_MOUSEACTIVATE` hook are untouched, so the `MA_NOACTIVATE` invariant holds by construction.
- **Provider adapters.** `IUsageProvider` and `IActivityMonitor` are unchanged. Gating happens strictly upstream of `GetSnapshotAsync` and `CheckLivenessAsync`; no adapter learns it was disabled.
- **Accessibility (NFR-02).** Each `CheckBox` carries `AutomationProperties.Name` = provider display name and `AutomationProperties.HelpText` = badge label; the badge `TextBlock` carries its own name so state reaches a screen reader as text. Tab order is row-by-row then Close, via natural document order.

## Errors, security, and recovery

- **Errors and edges.** Missing file, missing `"Providers"`, missing key, non-boolean `Enabled`, and malformed JSON all resolve to *enabled* — never a silent disable. `SaveAsync` failures are logged with structured Serilog arguments and swallowed, matching `HudPositionStore.Save`'s `bool` contract; the in-memory gate stays authoritative for the session, so a read-only settings file degrades to "toggles work, do not survive restart" rather than a crash. `SetProviderEnabled` on an unregistered id is a no-op. `RefreshProviderNowAsync` inherits `ThrowIfDisposedOrStopping`, so a toggle racing shutdown throws `ObjectDisposedException` into a fire-and-forget task — caught and logged at the call site, since `DialogService.CloseAll()` already runs before `UsageStore.StopAsync` in `ApplicationLifetime.ShutdownCoreAsync`.
- **Authorization and sensitive data.** No credential is read, written, or refreshed by any component here; gating strictly *reduces* credential access. `appsettings.json` gains only provider ids and booleans — no tokens, no paths. The `FileShare.ReadWrite | FileShare.Delete` read-only borrowing invariant is untouched because no adapter changes.
- **Concurrency and idempotency.** The registry is a `ConcurrentDictionary`; `_activityStates` mutation stays under the existing `_activityLock`. `SetProviderEnabled` is idempotent. File writes serialize through `SettingsFileGate` (DEC-07). `RefreshProviderNowAsync` shares `_refreshLock` with `RefreshNowCoreAsync`, so a toggle during a periodic tick queues rather than interleaves. Rapid toggling is safe: each write reads the current DOM under the gate, and the last write reflects the final in-memory state.
- **Rollback or reversal.** Deleting the `"Providers"` section from `appsettings.json` restores all-enabled behavior. The feature is additive — no migration, no schema version, no data destroyed. Reverting the commit leaves a stale `"Providers"` section that older builds ignore.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| S-1 `ProviderSettings` + `ProviderSettingsStore` + `SettingsFileGate`; `HudPositionStore.Save` takes the gate | — | TC-01 – TC-04 pass; `HudPositionStoreTests` still green |
| S-2 `UsageStore.Gating.cs` + guards in `RefreshCoreAsync` / `PollActivityAsync` / `CheckAnyBusyAsync` | S-1 (none technically; ordered for review) | TC-05 – TC-09, TC-15 pass; `UsageStoreTests`, `UsageStoreActivityTests`, `UsageStoreLifecycleTests` still green |
| S-3 `ProviderBadgeState` + `ProviderBadgeResolver`; csproj `Compile` links | — | TC-12 passes |
| S-4 `NotchViewModel` ring filtering | S-2 | TC-10, TC-11 pass; `NotchViewModelTests` still green |
| S-5 `SettingsViewModel` + `ProviderToggleViewModel` | S-1, S-2, S-3 | TC-13, TC-14 pass |
| S-6 `SettingsWindow.xaml` body + `DialogResources.xaml` badge/toggle styles + `DialogService` VM factory | S-5 | Solution builds; MAN-01 – MAN-03 executable |
| S-7 `App.xaml.cs` startup wiring (DEC-08) | S-1 – S-6 | MAN-04 passes end to end |

## Test approach

- **Profile.** Three source projects, all .NET 10, SDK pinned by `global.json` to `10.0.400` (`rollForward: latestFeature`; installed SDK confirmed `10.0.400`). `TokenHound.Core` (`net10.0`) and `TokenHound.Infrastructure` (`net10.0`) are libraries. `TokenHound.App` is a **WPF desktop application**: `net10.0-windows`, `<UseWPF>true</UseWPF>`, `<OutputType>WinExe</OutputType>`. Solution file is `TokenHound.slnx`. Two test projects, `TokenHound.Core.Tests` and `TokenHound.Infrastructure.Tests`, both `net10.0`, `OutputType=Exe`, `UseMicrosoftTestingPlatformRunner=true`, `xunit.v3.mtp-v2` 4.0.0 with AwesomeAssertions and NSubstitute. `global.json` declares `"test": { "runner": "Microsoft.Testing.Platform" }`, so the route is **native MTP via `dotnet test --project`** (`references/mtp.md`, first mode). App-layer view models are already tested by `<Compile Include Link=…>` into `TokenHound.Infrastructure.Tests`; that project has no `UseWPF`, which is why DEC-04/DEC-05/DEC-06 forbid WPF types (`ICommand`, `CollectionViewSource`, `Visibility`) in the new view models.
- **E2E: omitted by .NET desktop policy.** No full-application launch, no WPF UI automation, no aggregate suite that would trigger one. Existing tests are left in place. Everything reachable without a message pump is covered by unit and integration tests below; the residue is the manual script.
- **Command prerequisites and exclusions.** Windows 11, .NET SDK 10.0.400+. No network, no provider credentials, no live provider needed — every gating test substitutes `IUsageProvider`/`IActivityMonitor` with NSubstitute, and every persistence test writes to a fresh `Path.GetTempPath()` directory (the `HudPositionStoreTests` fixture pattern). `TokenHound.App` is never executed by the test suite; it is only compiled. The two test projects share no `bin`/`obj` or fixture, so they may run in either order, but `--minimum-expected-tests 1` is mandatory on each per `AGENTS.md`. Note the substitution limit: NSubstitute proves the *dispatch decision* (whether `GetSnapshotAsync` was invoked), not that a real adapter performs no socket or SQLite I/O. The end-to-end "zero overhead for disabled providers" claim in NFR-04/NFR-06 is closed by MAN-05, not by a unit test.

```powershell
rtk dotnet restore TokenHound.slnx --nologo --verbosity:minimal
rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal
rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1
rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1
```

Scope a single class while iterating with `-- --minimum-expected-tests 1 --filter-class "*ProviderSettingsStoreTests*"`. Building `TokenHound.App` is required even though it has no tests — it is the only project that compiles the XAML, so a broken binding path or missing resource key surfaces nowhere else. `$LASTEXITCODE` must be checked after each command; zero executed tests is never a pass.

- **Manual acceptance.** Owner: Douglas Cunha, on the primary Windows 11 desktop. Launch per `AGENTS.md` via Windows MCP `App` (`mode="launch_executable"`) — `Start-Process` renders to an isolated desktop — and capture with Windows MCP `Screenshot` (`display: [2]`). Until executed, NFR-01 – NFR-04 acceptance stays pending.

| ID | Obligations | Script | Expected result |
| --- | --- | --- | --- |
| MAN-01 | NFR-02, FR-09, US-06 | Open Settings from the HUD context menu. Tab through every row and the Close button. Press Space on a focused row. Press Esc. | Focus visual is visible on each stop in row order then Close; Space toggles the row; Esc closes; the app keeps running |
| MAN-02 | NFR-01 | View Settings at 100%, 150%, 200% Windows scaling; screenshot each | Dark Fluent surface; glyphs, names, and badge pills legible and unclipped; no light-mode flash on open |
| MAN-03 | NFR-03 | With an editor focused, open Settings, toggle a provider, close it, then click and drag the HUD capsule | Settings takes focus when opened; after closing, HUD click and drag never steal focus from the editor |
| MAN-04 | OBJ-04, US-05, FR-06 | Disable Antigravity and Codex; inspect `appsettings.json`; restart; reopen Settings | `Providers` shows `gemini`/`codex` false with `Hud`, `Refresh`, `Log` intact; after restart only enabled rings appear and toggles match |
| MAN-05 | OBJ-01, NFR-04, NFR-06 | Toggle a provider off, observe the HUD, then watch the log for two full polling cycles | Ring disappears with no perceptible delay; no fetch, credential-read, or fault entry logged for the disabled provider |

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-06, OBJ-04 | Unit | `SaveAsync` a `Providers` map into a file already holding `Hud`, `Refresh`, `Log` | All three sections survive verbatim; `Providers` round-trips through `Load` | `tests/TokenHound.Infrastructure.Tests` — `Configuration/ProviderSettingsStoreTests` |
| TC-02 | FR-07 | Unit | Load with (a) no file, (b) no `Providers` section, (c) a key absent from the section | Every case reports enabled | idem |
| TC-03 | FR-06, A-01, DEC-02 | Unit | Load a file whose section uses `"antigravity"`; then save | Read resolves as `gemini`; the written file uses `"gemini"` and drops the alias | idem |
| TC-04 | FR-06 | Unit | Load a corrupt file (`{ not json`); load a non-boolean `Enabled` | Defaults to enabled, no exception | idem |
| TC-05 | FR-04, OBJ-01 | Integration | Two substituted providers, one disabled; `RefreshNowAsync` | Disabled provider's `GetSnapshotAsync` never called; enabled one called once | `tests/TokenHound.Infrastructure.Tests` — `Engine/UsageStoreGatingTests` |
| TC-06 | FR-04 | Integration | Same, driven through `TickAsync` with an eligible schedule | Identical gating on the periodic path | idem |
| TC-07 | FR-04, NFR-06 | Integration | Substituted `IActivityMonitor` for a disabled provider; `TickAsync` | `CheckLivenessAsync` never called for it | idem |
| TC-08 | FR-04, US-04, OBJ-03 | Integration | Disable, then `SetProviderEnabled(id, true)` | `ProviderEnablementChanged` raised once; `RefreshProviderNowAsync` fetches that provider only | idem |
| TC-09 | NFR-06 | Integration | Provider with an unexpired persisted 429 deadline is re-enabled | No `GetSnapshotAsync` dispatch until the deadline passes | idem |
| TC-15 | FR-04, NFR-04, DEC-09 | Integration | Provider reports `Busy`, then is disabled | Its activity entry is cleared and it no longer forces the active cadence | idem |
| TC-10 | FR-03, OBJ-01 | Unit | Rings for three providers; disable the middle one, then re-enable | Ring leaves `Rings` on disable and returns at its original index | `tests/TokenHound.Infrastructure.Tests` — `ViewModels/NotchViewModelTests` |
| TC-11 | FR-10, A-05 | Unit | Disable every provider | `Rings` is empty, no exception, `IsFallbackActive` stays false | idem |
| TC-12 | FR-05, A-03, A-04, R-3 | Unit | Resolve badges for each `ProviderStatus`, for unmonitored, and for monitored-with-no-snapshot | `Disabled`, `Checking…`, and a label for every enum member including `Unsupported`; each label is non-empty text | `tests/TokenHound.Infrastructure.Tests` — `ViewModels/ProviderBadgeResolverTests` |
| TC-13 | FR-02, FR-09, OBJ-03 | Unit | Set `IsMonitored = false` on a row | Engine gate flipped, badge becomes `Disabled`, persistence callback invoked once with the new map | `tests/TokenHound.Infrastructure.Tests` — `ViewModels/SettingsViewModelTests` |
| TC-14 | FR-01, R-1 | Unit | Build the VM over a store with five registered providers | Five rows in stable order with catalog display names and glyph keys; a sixth registration appears with no code change | idem |

## Observability and rollout

- **Signals.** Serilog structured entries with typed arguments, matching `App.xaml.cs` and `ProviderFaultLog`: provider enablement resolved at startup (id + state), each toggle, and each `SaveAsync` failure. No new metric or health check. The absence of `ProviderFaultLog` entries for a disabled provider is itself the MAN-05 evidence.
- **Migration and compatibility.** None required. The section is additive and its absence is the documented default; a build without this feature ignores it.
- **Rollout and rollback.** Ships in the normal desktop build. Gate: the four commands above green with a non-zero test count, plus MAN-01 – MAN-05 signed off. Rollback is a commit revert; any `"Providers"` section left on disk is inert.

## Risks and open items

- **Risk — concurrent `appsettings.json` writers** (medium probability, high impact): HUD drag and provider toggle both read-modify-write the same file. Mitigated by DEC-07's shared gate; regression covered by keeping `HudPositionStoreTests` green after CMP-04.
- **Risk — 300-line file cap** (`AGENTS.md`) (medium, low): `NotchViewModel.cs` is at 202 lines and DEC-05 adds a dictionary, an order list, a subscription, and filtering. If it crosses 300, split ring management into a `NotchViewModel.Rings.cs` partial, mirroring what `UsageStore` already does. `UsageStore.cs` (207) is protected by putting all gating in a new partial from the start.
- **Risk — substituted providers do not prove real I/O silence** (low, medium): TC-05 – TC-07 prove the dispatch decision only. NFR-04's "zero measurable overhead" and NFR-06's "completely eliminates credential reads" are closed by MAN-05 log inspection. Recorded rather than papered over.
- **Risk — `RegisteredProviderIds` ordering** (low, low): `ConcurrentDictionary` does not guarantee enumeration order, so the dialog could reorder between launches. Mitigated by sorting on display name in CMP-10; TC-14 pins it.
- **OPEN-01 — `Unsupported` badge wording.** PRD FR-05 enumerates six states and omits `ProviderStatus.Unsupported`, which exists in Core and is produced by adapters (`ProviderRingViewModel.Status.cs` maps it to "No usable quota available"). This spec adds a seventh pill labeled **"No Quota"** with the neutral slate treatment used for `Stale`. Owner: Douglas Cunha. Affects FR-05, TC-12, CMP-08. Confirm the label, or the PRD should state the state is deliberately unrepresented.
- **OPEN-02 — dialog height.** The PRD sizes the window at ~460×380 DIPs for four providers; five rows plus header and footer need roughly 420 DIPs of height. This spec raises `Height`/`MinHeight` accordingly and keeps `ResizeMode="NoResize"`. If provider count keeps growing, the card needs a `ScrollViewer` with a capped height — deferred, not designed here. Owner: Douglas Cunha. Affects CMP-12, NFR-01.

## Relevant files

- Modify: `src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs`
- Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs`
- Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.Activity.cs`
- Modify: `src/TokenHound.App/ViewModels/NotchViewModel.cs`
- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
- Modify: `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`
- Modify: `src/TokenHound.App/UI/Windows/DialogService.cs`
- Modify: `src/TokenHound.App/UI/Styles/DialogResources.xaml`
- Modify: `src/TokenHound.App/App.xaml.cs`
- Modify: `src/TokenHound.App/appsettings.json`
- Modify: `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
- Create: `src/TokenHound.Infrastructure/Configuration/ProviderSettings.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`
- Create: `src/TokenHound.Infrastructure/Configuration/SettingsFileGate.cs`
- Create: `src/TokenHound.Infrastructure/Engine/UsageStore.Gating.cs`
- Create: `src/TokenHound.Infrastructure/Engine/ProviderEnablementChangedEventArgs.cs`
- Create: `src/TokenHound.App/ViewModels/ProviderBadgeState.cs`
- Create: `src/TokenHound.App/ViewModels/ProviderBadgeResolver.cs`
- Create: `src/TokenHound.App/ViewModels/ProviderToggleViewModel.cs`
- Create: `src/TokenHound.App/ViewModels/SettingsViewModel.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreGatingTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderBadgeResolverTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/ViewModels/SettingsViewModelTests.cs`
