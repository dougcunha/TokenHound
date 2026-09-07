# PRD — Provider Enablement, Status Visibility & Engine Gating in Settings

## Problem and context

TokenHound currently registers and monitors four AI coding providers by default: Claude Code, Antigravity, Codex, and Cursor. Background polling cycles run continuously, and corresponding status rings are rendered on the desktop HUD notch for all registered providers. However, developers frequently use only a subset of these tools. Unused, uninstalled, or unauthenticated providers generate unnecessary background polling loops, redundant disk/IPC inspection cycles, and log noise, while cluttering the compact HUD capsule with inactive or unauthenticated rings.

Furthermore, users currently lack a consolidated interface to inspect provider operational health (such as credential readiness, rate-limit pauses, or stale telemetry) without hovering over individual HUD rings. While the main window context menu provides an entry point to "Settings", [SettingsWindow.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml) exists only as an empty shell placeholder created in [prd-main-window-context-menu](file:///D:/MyProjects/TokenHound/tasks/prd-main-window-context-menu/prd.md).

This slice (`settings-01-provider-management`) implements provider management inside the Settings dialog. Users can view all registered providers, observe real-time styled status badges reflecting operational state, toggle monitoring on or off per provider, have their preferences persist across restarts in [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json), and immediately see disabled providers excluded from HUD rings and background polling in [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs).

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Individual provider gating | Users can toggle monitoring on or off for each of the four registered providers (Claude Code, Antigravity, Codex, Cursor). Toggling off immediately removes the provider ring from the desktop HUD and halts its background polling in [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs). |
| OBJ-02 | Real-time status transparency | Each provider row in Settings presents a distinct styled status badge reflecting its initialization and operational health matching [ProviderStatus.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/ProviderStatus.cs) (`Ok`, `NeedsAuth`, `Stale`, `RateLimited`, `AccessDenied`, or `Disabled`). |
| OBJ-03 | Immediate live synchronization | Enabling or disabling a provider takes effect immediately in both Settings and HUD without requiring an application restart or manual "Apply" / "Save" action. |
| OBJ-04 | Configuration persistence | Provider enabled/disabled toggles persist across application restarts in [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json), preserving all existing configuration sections (`Hud`, `Refresh`, `Log`). |
| OBJ-05 | Windows 11 Dark Mode & accessibility | Settings dialog adheres to Windows 11 Fluent dark mode styling, provides full keyboard navigation (Tab, Space, Enter, Esc), preserves non-activating HUD behavior, and enforces a single-instance modeless lifecycle via [DialogService.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/DialogService.cs). |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer using only Claude Code and Cursor | Disable uninstalled or unused providers (Antigravity and Codex) | Free up screen real estate on the HUD notch and eliminate background polling overhead | Right-click HUD -> select "Settings". In Settings dialog, uncheck Antigravity and Codex. Their rings immediately disappear from the HUD; background polling for both ceases immediately. Changes persist to disk. User presses Esc to return to work. |
| US-02 | Developer with expired credentials | Understand why a provider shows no usage | Diagnose authentication problems quickly without reviewing logs | Open Settings. Claude Code displays a prominent "Needs Auth" status badge. The developer recognizes that CLI login is required. After completing login and triggering a refresh, the status badge reactively transitions to "OK". |
| US-03 | Developer encountering rate limits | Confirm rate-limit state across tools | Identify paused providers and distinguish rate limits from network failures | Open Settings. Codex displays a "Rate Limited" status badge. The developer verifies that the provider is healthy but temporarily cooling down, without needing to inspect raw log files. |
| US-04 | Developer re-enabling a provider | Resume monitoring for a previously disabled tool | Bring back telemetry and HUD presence on demand | Open Settings, check the toggle switch for Antigravity. Its status updates from "Disabled" to active, an immediate background refresh is dispatched, and its ring reappears on the HUD. |
| US-05 | Developer restarting TokenHound | Retain personalized provider configuration | Maintain desired workspace layout without repeating configuration | Close TokenHound. Upon relaunch, TokenHound reads [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) and initializes polling and HUD rings only for previously enabled providers. |
| US-06 | Keyboard-only user | Navigate and manage settings using keyboard | Full accessibility compliance | Open Settings. Focus enters the dialog. User presses Tab to navigate between provider checkboxes/switches, presses Space to toggle, and presses Esc to dismiss the dialog. |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | List all registered providers in Settings dialog | The Settings dialog displays all four registered providers: Claude Code (`claude`), Antigravity (`antigravity`), Codex (`codex`), and Cursor (`cursor`). Each row shows the official display name and monochrome vector glyph or badge resolved from [ProviderCatalog.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/ProviderCatalog.cs). |
| FR-02 | Individual activation toggle per provider | Each provider row features an interactive toggle switch or checkbox indicating its monitoring status (Enabled vs. Disabled). By default, all registered providers are Enabled (`true`) if no stored preference exists. |
| FR-03 | Immediate HUD ring visibility synchronization | When a provider is toggled to Disabled, its corresponding ring in [NotchViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/NotchViewModel.cs) is immediately removed or hidden from the main HUD capsule. When toggled back to Enabled, its ring is immediately restored to the HUD. |
| FR-04 | Engine background polling gating | [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) gates polling based on provider enabled state. Disabled providers are skipped during periodic ticks (`TickAsync`) and manual refreshes (`RefreshNowAsync`). No network requests, IPC commands, SQLite queries, or file reads are initiated for disabled providers. Re-enabling a provider queues an immediate background refresh for that provider. |
| FR-05 | Real-time provider status badge | Next to each provider name, Settings displays a styled status badge/pill indicating its operational state: `Ok` ("OK"), `NeedsAuth` ("Needs Auth"), `Stale` ("Stale"), `RateLimited` ("Rate Limited"), `AccessDenied` ("Access Denied"), or `Disabled` ("Disabled") when toggled off. When enabled but awaiting initial fetch, a transient "Checking..." badge is displayed. Badges update reactively when snapshots are received. |
| FR-06 | Persistence in `appsettings.json` | Provider enabled/disabled states are persisted to [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) under a dedicated `"Providers"` section (e.g. `{"Providers": {"claude": {"Enabled": true}, ...}}`). Persistence operations preserve all other sections (`Hud`, `Refresh`, `Log`) and maintain valid JSON formatting. |
| FR-07 | Startup configuration resolution | On application startup, provider enablement settings are loaded from [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json). If the file, the `"Providers"` section, or any provider key is missing, missing providers default to enabled (`true`). Initial background polling and HUD rings are only instantiated for enabled providers. |
| FR-08 | Modeless dialog lifecycle | Settings opens modelessly via [DialogService.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/DialogService.cs). If already open, re-invoking Settings restores and activates the existing window without creating duplicate instances. Closing Settings does not terminate the application. Closing TokenHound cleanly terminates the Settings window. |
| FR-09 | Immediate dismissal via Esc and Close button | Pressing the Escape key or clicking the "Close" button immediately closes the Settings dialog. Toggle changes apply live in memory and are saved to disk upon user interaction without requiring an explicit "Apply" or "Save" button. |
| FR-10 | Graceful handling of zero enabled providers | If the user disables all providers, the HUD cleanly presents an empty or standby notch capsule without throwing exceptions, crashing, or activating fallback mock providers unless explicitly configured. Re-enabling any provider restores standard HUD visualization. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Visual styling and Windows 11 dark mode | Settings window uses application dark theme brushes (`SurfaceBackgroundBrush`, `SurfaceCardBackgroundBrush`, `SurfaceBorderSubtleBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`). Status badges use distinct semantic color treatments (subtle green for OK, amber/orange for NeedsAuth and RateLimited, muted red for AccessDenied, neutral gray for Disabled/Checking) with accessible contrast. Geometry and text scale cleanly without clipping at 100%, 150%, and 200% Windows scaling. |
| NFR-02 | Accessibility and keyboard navigation | Full keyboard navigation: logical Tab order through each provider toggle and the Close button. Space or Enter toggles the selected provider. Escape dismisses the window. Every interactive element has an `AutomationProperties.Name` and role. Status badges convey state through visible text, not color alone. |
| NFR-03 | HUD non-activating focus invariant | Opening, interacting with, or closing Settings must not alter or break the non-activating behavior of the main HUD window (`WM_MOUSEACTIVATE` returning `MA_NOACTIVATE`). Settings receives standard dialog focus when opened, while normal HUD clicks and dragging continue to avoid stealing focus from active editors or terminals. |
| NFR-04 | Performance and responsiveness | Toggling a provider produces instantaneous visual response (< 50ms) in Settings and the desktop HUD. Disk persistence in [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) executes asynchronously or non-blocking to prevent UI micro-stutters. Polling cycles in [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) incur zero measurable overhead for disabled providers. |
| NFR-05 | Clean architecture and layer purity | [TokenHound.Core](file:///D:/MyProjects/TokenHound/src/TokenHound.Core) remains pure with zero UI or OS dependencies. Configuration data models and persistence stores reside in [TokenHound.Infrastructure](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration). ViewModels, converters, and window markup reside in [TokenHound.App](file:///D:/MyProjects/TokenHound/src/TokenHound.App). |
| NFR-06 | Credential and rate-limit invariants | Gating a provider off completely eliminates credential reads and network calls for that provider. Re-enabling a provider honors any unexpired rate-limit reset deadlines stored in `RateLimitGate` without triggering premature retries. All credentials remain borrowed strictly read-only (`FileShare.ReadWrite | FileShare.Delete`). |

## User experience

### Settings Window Layout

The Settings window is a compact, modeless dialog (approx. 460x380 DIPs) styled consistently with [AboutWindow.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/AboutWindow.xaml) and the main HUD:

1. **Header**: Title "Settings" in semi-bold 16pt primary text, with an explanatory subtitle "Manage monitored AI coding assistants and provider telemetry."
2. **Provider Card Container**: A rounded card border (`SurfaceCardBackgroundBrush`, `CornerRadius="8"`) containing the list of providers.
3. **Provider Row Item**: Each row contains:
   - **Vector Glyph / Icon**: The provider's monochrome mark (scaled via [ProviderCatalog.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/ProviderCatalog.cs)), positioned on the left.
   - **Provider Information**: Primary text showing the official display name ("Claude Code", "Antigravity", "Codex", "Cursor").
   - **Status Badge / Pill**: A compact, rounded pill badge displaying the current operational state:
     - `OK`: Subtle green pill (`#202E7D32` background, `#81C784` text).
     - `Needs Auth`: Amber pill (`#20E65100` background, `#FFB74D` text).
     - `Rate Limited`: Amber/yellow pill (`#20F57F17` background, `#FFF176` text).
     - `Stale`: Slate/neutral pill (`#20546E7A` background, `#B0BEC5` text).
     - `Access Denied`: Red pill (`#20C62828` background, `#E57373` text).
     - `Disabled`: Muted gray pill (`#1AFFFFFF` background, `#757575` text).
     - `Checking...`: Subtle cyan/gray pill (`#2000838F` background, `#80DEEA` text) when active but waiting for first snapshot.
   - **Interactive Toggle**: A right-aligned checkbox or switch control indicating enabled/disabled state.
4. **Footer**: Bottom area containing the "Close" button aligned to the right.

### Interaction Flow

- **Opening**: Right-clicking the HUD and selecting "Settings" opens the window centered relative to the active work area.
- **Toggling**: Clicking a checkbox or pressing Space on a focused item immediately toggles the state:
  - If toggled off: The row's status badge switches to "Disabled" (gray), the HUD ring immediately disappears, background polling stops, and the change is saved to [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json).
  - If toggled on: The row's status badge switches to active, the HUD ring immediately reappears, a refresh is dispatched to update telemetry, and the change is saved.
- **Dismissal**: Pressing Esc, clicking "Close", or clicking the title bar 'X' dismisses the dialog. All changes persist automatically.

## Constraints and dependencies

- Target Platform: Windows 11 with .NET 10 (`net10.0-windows` for App, `net10.0` for Core and Infrastructure).
- Existing Providers: Exactly the four registered providers: Claude Code (`claude`), Antigravity (`antigravity`), Codex (`codex`), and Cursor (`cursor`).
- Configuration File: [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) in application base directory, read/written via `System.Text.Json` nodes preserving comments and other sections.
- Existing Infrastructure Components:
  - [DialogService.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/DialogService.cs) for modeless lifecycle management.
  - [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) for provider polling coordination.
  - [ProviderCatalog.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/ProviderCatalog.cs) for names, badges, and glyph keys.
  - [NotchViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/NotchViewModel.cs) for HUD ring collection management.
  - [HudPositionStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs) pattern for section-isolated JSON settings persistence.
- Architectural Invariants:
  - Keep `TokenHound.Core` pure: zero UI or OS dependencies.
  - Zero fake data: never invent denominators, quotas, or percentages.
  - Borrow credentials read-only: never write or refresh third-party tokens.
  - Honor rate-limit deadlines before dispatching network calls.

## Out of scope

- Polling cadence configuration (ActiveInterval and IdleInterval settings) — owned by sibling slice `settings-02-cadence-and-retries`.
- HTTP 429 retry timeout and exponential backoff configuration — owned by sibling slice `settings-02-cadence-and-retries`.
- In-app login flows, token entry forms, or credential renewal dialogs (users authenticate externally using official tool CLIs).
- Adding new or custom third-party providers beyond the existing registered four.
- HUD geometry customization (capsule width, ring order re-ordering, ring thickness).
- System tray icon and tray context menu integration (owned by dedicated system tray task).

## Assumptions and sources

### User decisions

- The user defined the slice scope: provider enablement, status visibility, and engine gating in the Settings dialog.
- Four registered providers must be represented: Claude Code, Antigravity, Codex, and Cursor.
- Toggling a provider off must immediately hide its ring from the desktop HUD and halt background polling in `UsageStore`.
- Re-enabling a provider immediately restores polling and HUD visualization.
- Configuration must persist across restarts in `appsettings.json` while preserving existing sections.
- Status badges must reflect initialization and operational liveness matching `ProviderStatus`.
- Dialog lifecycle must be modeless, single-instance, dismissible via Esc, and non-activating to the HUD.

### Local evidence

- [Repository instructions](file:///D:/MyProjects/TokenHound/AGENTS.md): Strict pure Core layer, read-only credential borrowing, rate-limit preservation, and non-activating HUD focus rules.
- [SettingsWindow.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml): Shell window with dark background, Esc key handling, and Close button.
- [DialogService.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/DialogService.cs): Existing modeless dialog manager providing `ShowSettings` and single-instance activation.
- [HudPositionStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/HudPositionStore.cs): Proven JSON DOM reading/writing pattern for `appsettings.json` preserving all sibling sections.
- [ProviderCatalog.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/ProviderCatalog.cs): Centralized lookup for provider display names, glyph keys, and badges.
- [ProviderStatus.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Models/ProviderStatus.cs): Enum defining operational states (`Ok`, `Stale`, `NeedsAuth`, `AccessDenied`, `RateLimited`).
- [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs): Central coordinator managing periodic polling ticks and snapshot events.
- [NotchViewModel.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.App/ViewModels/NotchViewModel.cs): Main HUD view model exposing the observable `Rings` collection.
- [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json): Target settings file containing existing `"Hud"`, `"Refresh"`, and `"Log"` sections.

### Explicit assumptions

- A-01: Provider enablement settings are stored under a top-level `"Providers"` object in `appsettings.json`, structured as `{"Providers": {"claude": {"Enabled": true}, ...}}`. Unlisted providers default to `Enabled: true`.
- A-02: Settings changes apply immediately in memory upon toggle; no explicit "Apply" or "Save" button is needed, aligning with modern Windows 11 fluent settings patterns.
- A-03: When a provider is disabled, its status badge displays "Disabled" with neutral styling, clearly differentiating user deactivation from an authentication failure or network error.
- A-04: When an enabled provider has not completed its first telemetry fetch, it displays a neutral "Checking..." status pill until the initial snapshot is resolved.
- A-05: Disabling all providers leaves the HUD notch capsule visible in an empty/standby state; it remains draggable and the context menu remains accessible to reopen Settings.

## PRD acceptance gate

- [x] Every requirement has a unique ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified repository source.
- [x] Additional product choices are identified as assumptions.
- [x] Implementation details remain in the TechSpec.
- [x] Sibling slice boundaries (`settings-02-cadence-and-retries`) are clearly separated.
