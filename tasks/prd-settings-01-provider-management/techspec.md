# TechSpec — Provider Enablement, Status Visibility & Engine Gating in Settings

## Sources and traceability

- PRD: [prd.md](file:///D:/MyProjects/TokenHound/tasks/prd-settings-01-provider-management/prd.md), read September 7, 2026. This is a new TechSpec for slice `settings-01-provider-management`.
- Repository constraints: [AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md), [ARCHITECTURE.md](file:///D:/MyProjects/TokenHound/ARCHITECTURE.md), and global RTK command rules.
- Skills: `sdd-create-techspec` and its .NET profile reference (`references/dotnet.md`), `repository-cli-efficiency`, and `dotnet-efficient-validation`.
- Application and presentation evidence:
  - [SettingsWindow.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml) and [SettingsWindow.xaml.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs) (empty shell placeholder from [prd-main-window-context-menu](file:///D:/MyProjects/TokenHound/tasks/prd-main-window-context-menu/prd.md)).
  - [DialogService.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/DialogService.cs) (`ShowSettings` modeless lifecycle and single-instance activation).
  - [ProviderCatalog.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/ProviderCatalog.cs) (display names, badge initials, monochrome vector glyphs, and scale factors).
  - [ProviderRingViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/ProviderRingViewModel.cs) and [NotchViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/NotchViewModel.cs) (HUD ring collection and dynamic update).
  - [App.xaml.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/App.xaml.cs) and [ApplicationLifetime.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ApplicationLifetime.cs) (startup composition, provider registration, and shutdown).
  - [DialogResources.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Styles/DialogResources.xaml) (Dark surface brushes, typography, button styles).
- Engine and configuration evidence:
  - [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) (periodic polling scheduler, `TickAsync`, `RefreshNowAsync`, and `SnapshotUpdated` events).
  - [HudPositionStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs) (JSON DOM node reading and writing preserving sibling sections in `appsettings.json`).
  - [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) (active configuration file with `Hud`, `Refresh`, and `Log` sections).
- Core model evidence:
  - [ProviderStatus.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/ProviderStatus.cs) (`Ok`, `Stale`, `NeedsAuth`, `AccessDenied`, `RateLimited`).
  - [Snapshot.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/Snapshot.cs) (immutable telemetry snapshot).

## Solution summary

Transform the empty [SettingsWindow.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml) shell into a functional, modeless Settings dialog backed by a dedicated `SettingsViewModel` and item view models. The dialog presents all four registered providers (Claude Code, Antigravity, Codex, Cursor) with their vector glyph marks, official display names, styled status badges reflecting live operational health, and interactive switches/checkboxes.

Provider enablement states are persisted to [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) under a dedicated `"Providers"` section using the proven non-destructive JSON DOM manipulation pattern. At runtime, toggling a provider instantly updates [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) to gate background polling (preventing network, disk, and IPC overhead for disabled providers) and updates [NotchViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/NotchViewModel.cs) to immediately add or remove the corresponding ring from the desktop HUD notch.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-01, FR-02, FR-05, NFR-01, NFR-02 | Introduce `SettingsViewModel` and `ProviderSettingItemViewModel` in `TokenHound.App.ViewModels`, keeping them testable in memory without opening WPF windows. | Separates UI presentation logic, status badge resolution, and persistence dispatch from the code-behind, following the established `ProviderRingViewModel` and `HudActionsViewModel` pattern. | Merging presentation logic into `SettingsWindow.xaml.cs` code-behind would prevent headless unit testing in `TokenHound.Infrastructure.Tests`. |
| DEC-02 | FR-06, FR-07, NFR-04 | Store provider enablement states under a top-level `"Providers"` section in [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) via a new `ProviderSettingsStore` in `TokenHound.Infrastructure.Configuration`. | Follows the proven JSON DOM pattern in [HudPositionStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs), preserving existing sections (`Hud`, `Refresh`, `Log`), comments, and formatting. | Using `Microsoft.Extensions.Configuration.Json` with write-backs would require extra heavy dependencies and risk stripping comments/formatting. |
| DEC-03 | FR-04, NFR-04, NFR-06 | Add thread-safe provider enablement gating directly to [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) using a `ConcurrentDictionary<string, bool> _enabledStates`. | Disabled providers are skipped in both periodic `TickAsync` and on-demand `RefreshNowAsync`, preventing disk I/O, IPC calls, and network requests. Re-enabling a provider immediately dispatches a targeted refresh for that provider. | Unregistering/re-registering provider instances dynamically would complicate resource disposal, activity monitor mappings, and rate-limit gate caches. |
| DEC-04 | FR-03, FR-10, OBJ-01, US-01, US-04 | Synchronize HUD ring visibility reactively in [NotchViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/NotchViewModel.cs) by filtering `Rings` based on provider enabled state and reacting to enablement change notifications. | When disabled, the provider's `ProviderRingViewModel` is removed from `Rings` immediately. When enabled, a refresh snapshot restores the ring. Zero enabled providers leaves an empty standby capsule without errors or fallback mock injection. | Setting a `Visibility` property on `ProviderRingViewModel` would leave empty slot spacing in the capsule layout; removing from the observable collection ensures clean Bézier capsule resizing. |
| DEC-05 | FR-05, OBJ-02, US-02, US-03, NFR-01 | Map [ProviderStatus.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/ProviderStatus.cs) and enablement state to human-readable status badge text and semantic styling in `ProviderSettingItemViewModel`. | Reuses the established status color palette from [TooltipCard.xaml.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs): Green (`OK`), Amber (`Needs Auth`, `Rate Limited`), Muted Slate (`Stale`), Red (`Access Denied`), Gray (`Disabled`), and Cyan/Gray (`Checking...`). | Showing raw enum names (`NeedsAuth`) looks unpolished; custom descriptive labels provide immediate diagnostic value. |
| DEC-06 | FR-08, FR-09, NFR-03 | Maintain modeless single-instance lifecycle in [DialogService.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/DialogService.cs), instantiating `SettingsWindow` with `SettingsViewModel` and restoring/activating if already open. | Modeless execution preserves non-activating HUD interaction and ensures the HUD context menu and Close action remain responsive while Settings is open. | `ShowDialog()` modal execution would block the WPF dispatcher and disable the main HUD window. |
| DEC-07 | FR-09, OBJ-03, US-01 | Apply toggle changes immediately in memory and persist asynchronously upon toggle interaction without a modal "Save" or "Apply" button. | Conforms to modern Windows 11 Fluent settings ergonomics where toggles take immediate effect. Escape or Close simply dismisses the dialog. | Requiring an "Apply" button introduces dirty state tracking, cancel confirmation prompts, and user confusion when closing. |

## Components and flow

### Components inventory

| ID | Component | State | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.Infrastructure/Configuration/ProviderSettings.cs` | Create | Immutable DTO recording per-provider enablement flags. | None (`Core`/`Infrastructure` pure) |
| CMP-02 | `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs` | Create | Non-destructive reader and writer for `"Providers"` section in `appsettings.json`. | [HudPositionStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs) pattern |
| CMP-03 | `src/TokenHound.Infrastructure/Engine/UsageStore.cs` | Modify | Add `SetProviderEnabled`, `IsProviderEnabled`, and gate `TickAsync` and `RefreshNowAsync`. | CMP-01 |
| CMP-04 | `src/TokenHound.App/ViewModels/ProviderSettingItemViewModel.cs` | Create | Observable view model for a provider row with toggle, glyph, and live status badge. | [ProviderCatalog.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/ProviderCatalog.cs), [ProviderStatus.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/ProviderStatus.cs) |
| CMP-05 | `src/TokenHound.App/ViewModels/SettingsViewModel.cs` | Create | Main view model coordinating provider items, persistence dispatch, and live HUD synchronization. | CMP-02, CMP-03, CMP-04, [NotchViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/NotchViewModel.cs) |
| CMP-06 | `src/TokenHound.App/UI/Windows/SettingsWindow.xaml` and `.xaml.cs` | Modify | Polish Settings dialog UI with card container, provider rows, toggles, styled status badges, and Close button. | CMP-05, [DialogResources.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Styles/DialogResources.xaml) |
| CMP-07 | `src/TokenHound.App/ViewModels/NotchViewModel.cs` | Modify | Filter `Rings` based on enabled status; remove disabled rings immediately; restore on enable; handle empty rings cleanly. | [ProviderRingViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/ProviderRingViewModel.cs) |
| CMP-08 | `src/TokenHound.App/UI/Windows/DialogService.cs` | Modify | Update `ShowSettings` to accept or resolve `SettingsViewModel` and bind it to `SettingsWindow`. | CMP-05, CMP-06 |
| CMP-09 | `src/TokenHound.App/App.xaml.cs` | Modify | Wire `ProviderSettingsStore`, initialize `UsageStore` with enabled states, compose `SettingsViewModel` and pass to `DialogService`. | CMP-02, CMP-03, CMP-05, CMP-08 |
| CMP-10 | `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs` | Create | Unit tests for loading, saving, JSON section preservation, and fallback defaults. | CMP-01, CMP-02 |
| CMP-11 | `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreGatingTests.cs` | Create | Unit tests verifying disabled providers are skipped in polling and refreshes. | CMP-03 |
| CMP-12 | `tests/TokenHound.Infrastructure.Tests/ViewModels/SettingsViewModelTests.cs` | Create | Headless tests verifying toggles update store, save settings, and change badge text. | CMP-04, CMP-05 |

## Contracts and data

### ProviderSettings Record

```csharp
namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted provider enablement configuration mapping provider identifiers to enabled state.
/// </summary>
public sealed record ProviderSettings
{
    /// <summary>
    /// Gets the dictionary of provider enablement states keyed by provider identifier.
    /// </summary>
    public IReadOnlyDictionary<string, bool> EnabledStates { get; init; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether the specified provider is enabled, defaulting to true if unconfigured.
    /// </summary>
    /// <param name="providerId">The unique provider identifier.</param>
    /// <returns><see langword="true"/> if enabled or unconfigured; otherwise <see langword="false"/>.</returns>
    public bool IsEnabled(string providerId)
        => !EnabledStates.TryGetValue(providerId, out var enabled) || enabled;
}
```

### Configuration JSON Schema in `appsettings.json`

```json
{
  "Hud": {
    "Left": null,
    "Top": null
  },
  "Refresh": {
    "ActiveIntervalSeconds": 180,
    "IdleIntervalSeconds": 300
  },
  "Providers": {
    "claude": true,
    "antigravity": true,
    "codex": true,
    "cursor": false
  },
  "Log": {
    "ApplicationName": "{ApplicationName}"
  }
}
```

## Integrations and interfaces

### UsageStore Gating Extensions

```csharp
// TokenHound.Infrastructure.Engine.UsageStore
public void SetProviderEnabled(string providerId, bool isEnabled);
public bool IsProviderEnabled(string providerId);
public event EventHandler<ProviderEnablementChangedEventArgs>? ProviderEnablementChanged;
```

When `SetProviderEnabled(providerId, isEnabled)` is invoked:
1. `_providerEnabledState[providerId] = isEnabled;`
2. `ProviderEnablementChanged?.Invoke(this, new ProviderEnablementChangedEventArgs(providerId, isEnabled));`
3. If `isEnabled` is `true`, dispatches `_ = RefreshProviderByIdAsync(providerId);` to retrieve immediate telemetry.
4. During periodic `TickAsync` and `RefreshNowAsync`, `RefreshCoreAsync` checks `if (!IsProviderEnabled(provider.ProviderId)) continue;`.

### ProviderSettingItemViewModel Interface

```csharp
public sealed class ProviderSettingItemViewModel : INotifyPropertyChanged
{
    public string ProviderId { get; }
    public string ProviderName { get; }
    public string? GlyphKey { get; }
    public double GlyphScale { get; }
    public bool IsEnabled { get; set; } // Two-way bound to toggle switch/checkbox
    public ProviderStatus Status { get; set; }
    public string StatusBadgeText { get; } // "OK", "Needs Auth", "Disabled", etc.
    public string StatusBadgeStyleKey { get; } // References semantic color style
}
```

## Errors, security, and recovery

- **Missing or Corrupt Configuration**: If `appsettings.json` is missing or unreadable, `ProviderSettingsStore.Load()` safely catches the exception and returns a default `ProviderSettings` where all providers are `true` (Enabled).
- **Corrupted `"Providers"` Section**: If `"Providers"` is invalid JSON or of unexpected type, it defaults to all providers enabled without throwing or failing startup.
- **Atomic Persistence**: Writing settings uses the established DOM replace method in `HudPositionStore`: parses existing file, sets `root["Providers"] = jsonObject`, and writes UTF-8 text with indentation.
- **Zero Enabled Providers**: When all four providers are toggled off, `UsageStore` polling ticks become no-ops, `NotchViewModel.Rings` is empty, and the HUD notch capsule collapses to an empty standby pill. No fallback mock provider is loaded. Re-enabling any provider immediately repopulates the ring and resumes polling.
- **Focus and Window Invariants**: `SettingsWindow` is an owned modeless dialog with normal activating style. It never calls `WindowStyles.EnableNonActivating`. The main HUD retains `WM_MOUSEACTIVATE` returning `MA_NOACTIVATE`, ensuring focus is never stolen from editors during HUD drag/click.
- **Credential Protection**: Gating off a provider ceases all credential file sharing and token access for that provider. No tokens or secrets are displayed or stored in settings.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| 1. Configuration Model & Store | — | `ProviderSettings` and `ProviderSettingsStore` created with unit tests verifying read/write and section preservation. |
| 2. UsageStore Gating | Step 1 | `UsageStore` updated with `SetProviderEnabled` and polling filters; unit tests prove disabled providers receive no queries. |
| 3. Presentation ViewModels | Step 2 | `ProviderSettingItemViewModel` and `SettingsViewModel` implemented with unit tests verifying toggle reactivity and status badge mapping. |
| 4. NotchViewModel Synchronization | Step 3 | `NotchViewModel` handles provider enablement change events, removing disabled rings and restoring enabled rings. |
| 5. SettingsWindow WPF UI | Step 4 | `SettingsWindow.xaml` markup updated with card container, provider rows, switches, status badges, and styling. |
| 6. Composition & Integration | Step 5 | `App.xaml.cs` and `DialogService.cs` wired; desktop acceptance tests pass. |

## Test approach

### Validation profile

- Projects: `src/TokenHound.App` (net10.0-windows, WPF), `src/TokenHound.Infrastructure` (net10.0), `src/TokenHound.Core` (net10.0).
- Test runner: Microsoft.Testing.Platform runner (`UseMicrosoftTestingPlatformRunner=true`) with `xunit.v3.mtp-v2` in `tests/TokenHound.Infrastructure.Tests` (net10.0).
- Presentation testing strategy: Link WPF-independent view models (`SettingsViewModel.cs`, `ProviderSettingItemViewModel.cs`) into `TokenHound.Infrastructure.Tests` following the established `HudActionsViewModelTests` pattern.
- **E2E: omitted by desktop .NET policy.** Desktop UI automation is omitted per repository guidelines. Functional verification is covered by unit tests, integration tests, and the manual acceptance script below.

### Test execution commands

Build and test commands must use `rtk` per global repository instructions:

```powershell
# Restore and build affected projects
rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo --verbosity:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Run focused MTP test classes
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*ProviderSettingsStoreTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreGatingTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*SettingsViewModelTests*"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

### Test matrix

| ID | Obligations | Level | Scenario and expected evidence | Project/class or manual script |
| --- | --- | --- | --- | --- |
| TC-01 | FR-06, FR-07, OBJ-04 | Unit | Load settings from JSON missing `"Providers"` section; verify all 4 providers default to `true`. Save new state with `cursor: false`; reload and verify `cursor` is `false` while `Hud`, `Refresh`, `Log` sections remain unchanged. | `ProviderSettingsStoreTests` |
| TC-02 | FR-04, OBJ-01, NFR-06 | Unit | Register 4 mock providers in `UsageStore`. Disable 2 providers via `SetProviderEnabled`. Run `TickAsync` and `RefreshNowAsync`. Verify only enabled providers receive `GetSnapshotAsync` calls. | `UsageStoreGatingTests` |
| TC-03 | FR-04, US-04 | Unit | Re-enable a disabled provider in `UsageStore`. Verify an immediate background refresh is dispatched for that specific provider. | `UsageStoreGatingTests` |
| TC-04 | FR-01, FR-02, FR-05, OBJ-02 | Unit | Initialize `SettingsViewModel`. Verify all 4 providers are present with correct display names from `ProviderCatalog`. Verify status badges map `NeedsAuth` to "Needs Auth", `Ok` to "OK", and disabled to "Disabled". | `SettingsViewModelTests` |
| TC-05 | FR-03, OBJ-01, US-01 | Unit | Toggle a provider to disabled in `SettingsViewModel`. Verify `NotchViewModel.Rings` immediately removes the corresponding ring. Toggle back to enabled; verify ring is restored upon snapshot receipt. | `SettingsViewModelTests` |
| TC-06 | FR-10 | Unit | Disable all providers in `SettingsViewModel`. Verify `NotchViewModel.Rings` has count 0 and no exceptions are thrown. Verify mock fallback is not loaded. | `SettingsViewModelTests` |
| TC-07 | FR-01, FR-05, NFR-01, MAN | Manual | Open Settings via HUD context menu. Verify visual presentation of 4 provider rows, monochrome marks, display names, and distinct color-coded status badges. Verify layout at 100%, 150%, and 200% DPI. | `MAN-01` |
| TC-08 | FR-02, FR-03, FR-09, MAN | Manual | Toggle a provider off. Verify HUD ring immediately disappears and status badge turns gray ("Disabled"). Press Esc to close. Re-launch app; verify toggle state persisted and ring remains hidden. | `MAN-02` |
| TC-09 | FR-08, NFR-02, NFR-03, MAN | Manual | Test keyboard navigation: Tab moves between switches and Close button; Space toggles state; Esc dismisses dialog. Verify HUD non-activating behavior (clicks on HUD do not steal focus from editor). | `MAN-03` |

### Manual acceptance script

- **MAN-01 (Visual & Scaling Check)**: Launch TokenHound on the desktop via Windows MCP `App` tool (`mode="launch_executable"`). Right-click HUD -> select "Settings". Confirm all 4 providers appear with marks and status badges. Capture screenshot on primary monitor (`display: [2]`) and verify at 100% and 150% scaling.
- **MAN-02 (Live Toggle & Persistence Walkthrough)**: Toggle off Cursor. Confirm the Cursor ring immediately disappears from the HUD notch. Verify status badge displays "Disabled". Close Settings with Esc. Close app and re-launch. Confirm Cursor remains disabled in Settings and absent from the HUD. Re-enable Cursor; verify ring returns.
- **MAN-03 (Focus & Keyboard Interaction)**: Keep Visual Studio or terminal window active. Right-click HUD -> Settings. Confirm Settings receives focus. Use Tab to move through controls, Space to toggle, and Esc to close. Verify ordinary HUD dragging does not steal active editor focus.

## Observability and rollout

- **Observability**: Status badge states are displayed directly in the Settings UI. [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) logs `Information` when provider enablement changes: `"Provider {ProviderId} enablement changed to {IsEnabled}"`.
- **Migration & Compatibility**: Seamless backward compatibility. If `"Providers"` is missing in existing `appsettings.json`, it defaults to all providers enabled (`true`), matching previous application behavior.
- **Rollback**: Reverting the feature commits preserves the existing `appsettings.json` because extra JSON keys are ignored by older versions.

## Risks and open items

- **Risk 1: Rapid Toggle Burst**: Rapidly toggling a switch on and off could dispatch redundant refresh queries.
  - *Mitigation*: Debounce or gate targeted provider refresh in `UsageStore` so re-enabling a provider ignores redundant requests while an update is already pending.
- **Risk 2: Modeless Window Placement**: Opening Settings on multi-monitor setups could position the dialog off-screen if the HUD is moved.
  - *Mitigation*: Reuse `WindowPlacement.PositionInWorkArea(window, owner)` from [DialogService.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/DialogService.cs) to ensure placement is always clamped to the active monitor work area.
- **Open Items**: None. Sibling slice `settings-02-cadence-and-retries` handles cadence and 429 retry parameters.

## Relevant files

- Modify:
  - `src/TokenHound.Infrastructure/Engine/UsageStore.cs`
  - `src/TokenHound.App/ViewModels/NotchViewModel.cs`
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml`
  - `src/TokenHound.App/UI/Windows/SettingsWindow.xaml.cs`
  - `src/TokenHound.App/UI/Windows/DialogService.cs`
  - `src/TokenHound.App/App.xaml.cs`
  - `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj`
- Create:
  - `src/TokenHound.Infrastructure/Configuration/ProviderSettings.cs`
  - `src/TokenHound.Infrastructure/Configuration/ProviderSettingsStore.cs`
  - `src/TokenHound.App/ViewModels/ProviderSettingItemViewModel.cs`
  - `src/TokenHound.App/ViewModels/SettingsViewModel.cs`
  - `tests/TokenHound.Infrastructure.Tests/Configuration/ProviderSettingsStoreTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreGatingTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/ViewModels/SettingsViewModelTests.cs`
