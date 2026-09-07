# PRD — Polling Cadence and Rate-Limit Retry Configuration in Settings

## Problem and context

TokenHound monitors AI provider usage, rate limits, and token counters across developer workflows on Windows. To provide accurate data without causing service disruptions, the application periodically polls provider endpoints based on whether local agent sessions are active or idle, and applies exponential backoff when encountering HTTP 429 Too Many Requests responses.

Currently, the polling intervals (`ActiveIntervalSeconds = 180`, `IdleIntervalSeconds = 300`) are stored in [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) and read-only at startup via [`RefreshSettingsStore`](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs). The 429 rate-limit backoff parameters (`MINIMUM_RETRY_FLOOR = 60s`, `MAXIMUM_CEILING = 3600s`) are hard-coded in [`RateLimitPolicy`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RateLimitPolicy.cs) and [`BackoffCalculator`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/BackoffCalculator.cs). Users cannot adjust their refresh cadence or rate-limit retry floor from the application interface, cannot persist changes through the UI, and must restart the application to apply manual edits made to `appsettings.json`.

Developers working with different provider subscriptions, quota windows, and network constraints need the ability to adjust polling frequency and rate-limit retry floors within the [SettingsWindow](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml). Crucially, these adjustments must respect strict safety floors (`MINIMUM_INTERVAL_SECONDS = 30s` and `MINIMUM_RETRY_FLOOR = 60s`, never retrying immediately on `Retry-After: 0`), persist safely to configuration without overwriting unrelated sections, and apply dynamically to the running [`UsageStore`](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) and rate-limit policies without requiring an application restart.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Users can view and configure active and idle polling intervals via the Settings dialog. | The Settings dialog exposes input controls for Active and Idle refresh intervals with legible units (seconds); valid entries are accepted and persisted. |
| OBJ-02 | Users can view and configure HTTP 429 rate-limit retry and backoff parameters via the Settings dialog. | The Settings dialog exposes input controls for 429 Minimum Retry Floor with safety validation; valid entries are accepted and persisted. |
| OBJ-03 | Safety floors are enforced in real time with clear visual validation. | Entering intervals < 30 seconds or retry floors < 60 seconds triggers immediate inline error feedback and disables saving/applying. |
| OBJ-04 | Configuration changes are persisted safely to `appsettings.json` without modifying unrelated sections. | Saving updates the `Refresh` and `RateLimit` JSON sections in [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) while preserving `Hud`, `Log`, and provider sections intact. |
| OBJ-05 | Polling cadence and rate-limit policies update dynamically at runtime without restarting the application. | Applying changes updates the active timer and idle interval in [`UsageStore`](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) and the effective retry floor in [`RateLimitPolicy`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RateLimitPolicy.cs) immediately, verified via automated tests and runtime telemetry. |
| OBJ-06 | Users can restore recommended factory default values with a single action. | Clicking "Reset to Defaults" restores Active: 180s, Idle: 300s, and Retry Floor: 60s in the form. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Active AI developer | Decrease the active polling interval from 180s to 60s | See token usage update more quickly during active coding sessions | Opens Settings, navigates to Cadence & Rate Limits, changes Active Interval to 60s, clicks Apply. Usage store immediately adopts the 60s timer interval without restarting the app. |
| US-02 | Developer on strict quota | Increase idle polling interval to 600s (10 min) | Minimize background API quota consumption while sessions are idle | Opens Settings, changes Idle Interval to 600s, clicks Apply. Settings are saved to `appsettings.json` and applied to `UsageStore` immediately. |
| US-03 | Developer hitting aggressive provider throttling | Increase the 429 retry floor from 60s to 120s | Give throttled provider APIs breathing room before retrying | Opens Settings, changes 429 Minimum Retry Floor to 120s, clicks Apply. Future 429 responses enforce at least a 120s backoff penalty. |
| US-04 | User entering invalid or dangerous interval | Enter an interval below the safety floor (e.g., 5 seconds) | Prevent accidental API hammering and account bans | Types "5" in Active Interval. UI immediately shows validation error: "Active interval must be at least 30 seconds." Apply button is disabled until corrected. |
| US-05 | User resetting customizations | Restore recommended defaults after experimenting | Easily revert to stable, known settings | Clicks "Reset to Defaults". Inputs populate with 180s (active), 300s (idle), and 60s (retry floor). User clicks Apply to persist. |
| US-06 | User canceling changes | Discard unsaved cadence and retry modifications | Avoid unintended configuration changes | Edits values, then closes dialog via Cancel, Escape, or title bar 'X'. Unsaved changes are discarded; running timers and stored settings remain unchanged. |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Provide a dedicated "Cadence & Rate Limits" section in the Settings dialog. | Opening Settings displays a distinct, titled section or card for "Cadence & Rate Limits" containing controls for Active Interval, Idle Interval, 429 Retry Floor, and action buttons. |
| FR-02 | Allow editing Active Polling Interval with safety floor enforcement (`MINIMUM_INTERVAL_SECONDS = 30s`). | The Active Interval input accepts positive integer second values. Inputs >= 30 are accepted. Inputs < 30, empty inputs, or non-numeric characters trigger an inline validation error ("Active interval must be at least 30 seconds") and disable Apply. |
| FR-03 | Allow editing Idle Polling Interval with safety floor enforcement (`MINIMUM_INTERVAL_SECONDS = 30s`). | The Idle Interval input accepts positive integer second values. Inputs >= 30 are accepted. Inputs < 30, empty inputs, or non-numeric characters trigger an inline validation error ("Idle interval must be at least 30 seconds") and disable Apply. |
| FR-04 | Enforce cadence consistency between idle and active intervals. | If the configured Idle Interval is less than the Active Interval, the UI displays a validation warning or error ("Idle interval should not be less than active interval") and prevents saving conflicting configurations. |
| FR-05 | Allow configuring HTTP 429 Rate-Limit Minimum Retry Floor with safety floor enforcement (`MINIMUM_RETRY_FLOOR = 60s`). | The 429 Retry Floor input accepts positive integer second values. Inputs >= 60 are accepted. Inputs < 60, negative numbers, or non-numeric characters trigger an inline validation error ("Retry floor must be at least 60 seconds") and disable Apply. Retrying immediately or setting floor to 0 is strictly forbidden. |
| FR-06 | Provide real-time validation feedback and gating on save/apply actions. | Validation occurs dynamically as the user types or adjusts values. While any field contains an error, the Apply/Save button is visually and operationally disabled, and the offending field is highlighted with an accessible error message. |
| FR-07 | Provide an action to restore default cadence and retry values. | Clicking "Reset to Defaults" resets form fields to Active: 180s ([`RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs#L15)), Idle: 300s ([`RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs#L20)), and Retry Floor: 60s ([`RateLimitPolicy.MINIMUM_RETRY_FLOOR`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RateLimitPolicy.cs#L13)). |
| FR-08 | Persist cadence and rate-limit settings to `appsettings.json` preserving existing sections. | Saving writes the updated values under `"Refresh"` (`ActiveIntervalSeconds`, `IdleIntervalSeconds`) and `"RateLimit"` (`MinimumRetryFloorSeconds`) in [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json). All other existing sections (`Hud`, `Log`, provider configurations) and comments/formatting are preserved without corruption or data loss. |
| FR-09 | Dynamically update running `UsageStore` polling cadence without application restart. | Upon applying valid settings, the running [`UsageStore`](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) instance dynamically updates its active polling timer interval and idle threshold in memory. The next scheduled ticks adhere to the new cadence without recreating the store or interrupting ongoing operations. |
| FR-10 | Dynamically update running `RateLimitPolicy` evaluation without application restart. | Upon applying valid settings, rate-limit backoff evaluation dynamically adopts the new minimum retry floor for subsequent 429 evaluations. Any already-recorded active rate-limit deadline is preserved and never truncated below the safety floor. |
| FR-11 | Support cancellation and discard of unapplied changes. | Dismissing the dialog via Close, Cancel, window 'X', or Escape key discards unapplied edits. Reopening Settings displays the currently active persisted values. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Core architecture purity | All models, contracts, and calculation policies for cadence and rate limits reside in `TokenHound.Core` with zero dependencies on WPF, Windows OS, or file systems, complying with [AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md). |
| NFR-02 | Invariant safety enforcement | The system must never violate safety floors: `MINIMUM_INTERVAL_SECONDS = 30s` and `MINIMUM_RETRY_FLOOR = 60s`. If `appsettings.json` is modified externally with values below these floors, deserialization and loading clamp or fall back to safe defaults without crashing. |
| NFR-03 | Visual consistency and accessibility | The Cadence & Rate Limits section uses dark surface styling consistent with the existing HUD and dialog design (`SurfaceBackgroundBrush`, `SurfaceCardBackgroundBrush`, `TextPrimaryBrush`). All fields have explicit `AutomationProperties.Name`, logical Tab order, high-contrast focus indicators, and support 100%, 150%, and 200% Windows display scaling without clipping. |
| NFR-04 | Atomic file persistence | Writing to [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) must be atomic and fault-tolerant (e.g., write to temporary file followed by atomic replace) to prevent file corruption in the event of abrupt termination or power failure. |
| NFR-05 | Thread safety & zero UI blocking | Applying settings to [`UsageStore`](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) and policy components must complete in < 20ms and must not block the UI thread, deadlock with concurrent timer ticks, or disrupt active provider queries. |
| NFR-06 | Code style & complexity limits | Adheres strictly to [AGENTS.md](file:///D:/MyProjects/TokenHound/AGENTS.md): classes <= 300 lines, methods <= 30 lines, nesting <= 3 levels, sealed classes, XML documentation on all public members, file-scoped namespaces, and explicit `CancellationToken` propagation. |

## User experience

### Settings dialog integration
The Cadence & Rate Limits controls are presented inside the application [SettingsWindow](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml) established by sibling slice `settings-01-provider-management`.

```
+-------------------------------------------------------------+
| Settings                                                [X] |
+-------------------------------------------------------------+
|                                                             |
| [ Providers ]  [ Cadence & Rate Limits ]                    |
|                                                             |
| Polling Cadence                                             |
| Configure how frequently TokenHound checks for quota data.  |
|                                                             |
| Active Refresh Interval (seconds)                           |
| [  180  ]  Used when an AI coding agent is busy (min 30s)   |
|                                                             |
| Idle Refresh Interval (seconds)                             |
| [  300  ]  Used when all agent sessions are idle (min 30s)  |
|                                                             |
| ----------------------------------------------------------- |
| Rate-Limit Resilience (HTTP 429)                            |
| Configure backoff penalties when provider APIs throttle.    |
|                                                             |
| Minimum Retry Floor (seconds)                               |
| [   60  ]  Minimum wait before retrying a 429 (min 60s)     |
|                                                             |
| [Reset to Defaults]                         [Cancel] [Save] |
+-------------------------------------------------------------+
```

### Feedback and error states
- **Real-time inline validation**: If a user enters `15` in the Active Refresh Interval, an inline error indicator appears below the input: *"Active interval must be at least 30 seconds."* The input field receives an error border highlight, and the Save/Apply button is disabled.
- **Relational validation**: If a user sets Active to `300` and Idle to `120`, an error indicator explains: *"Idle interval cannot be shorter than active interval."*
- **429 Floor warning**: If a user attempts to enter `0` or `30` in the Retry Floor, an error indicator states: *"Minimum retry floor must be at least 60 seconds to prevent API abuse."*
- **Success notification / state**: Upon clicking Save, changes are written to disk and live-applied to the runtime store. The dialog can either close (Save & Close) or display a brief subtle confirmation indicator (e.g., "Settings applied").

### Keyboard and accessibility navigation
- Keyboard focus moves logically via `Tab` / `Shift+Tab`:
  1. Active Interval input
  2. Idle Interval input
  3. Retry Floor input
  4. Reset to Defaults button
  5. Cancel button
  6. Save / Apply button
- `Escape` invokes Cancel and dismisses the dialog without saving.
- `Enter` within an input field triggers Save / Apply if the form is valid.
- All controls have explicit accessible names (`AutomationProperties.Name` and `AutomationProperties.HelpText`).

## Constraints and dependencies

- **Depends on `settings-01-provider-management`**: Sibling slice `settings-01-provider-management` establishes the host [SettingsWindow](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml) layout, ViewModel architecture, and configuration persistence foundation.
- **Repository Invariant — Core Purity**: [`TokenHound.Core`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core) remains completely pure with zero UI or OS dependencies. Configuration models and validation contracts are defined without WPF dependencies.
- **Repository Invariant — 429 Rate-Limit Enforcement**: Persist and respect 429 rate-limit deadlines before dispatching network calls; never retry immediately on `Retry-After: 0`.
- **Repository Invariant — Safety Floors**: Hard minimum floors are inviolable: `MINIMUM_INTERVAL_SECONDS = 30s` ([`RefreshSettings.MINIMUM_INTERVAL_SECONDS`](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/RefreshSettings.cs#L14)) and `MINIMUM_RETRY_FLOOR = 60s` ([`RateLimitPolicy.MINIMUM_RETRY_FLOOR`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RateLimitPolicy.cs#L13)).
- **Platform Stack**: Windows 11 desktop, .NET 10.0-windows, WPF with Segoe UI and dark surface styling.
- **Language**: English documentation, code, and UI copy.

## Out of scope

- Provider enablement switches and provider status badges (owned by sibling slice `settings-01-provider-management`).
- Bypassing rate-limit deadlines or retrying immediately on 429 (violates repository invariants).
- Per-request network timeout configuration (managed internally by individual provider HTTP clients).
- Provider-specific custom polling intervals (unified polling cadence applies across all registered providers in this slice).
- Secret management or credential editing within Settings (managed by external provider CLI login flows).
- Architecture diagrams, implementation tasks, and code modifications in this PRD artifact.

## Assumptions and sources

### User decisions
- The user explicitly requested cadence and retry configuration in Settings for slice `settings-02-cadence-and-retries`.
- Safety floors (`MINIMUM_INTERVAL_SECONDS = 30s`, `MINIMUM_RETRY_FLOOR = 60s`) are mandatory and must be validated.
- Configuration must persist to [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json) without destroying existing sections.
- Updates must apply dynamically to [`UsageStore`](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs) and [`RateLimitPolicy`](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RateLimitPolicy.cs) without requiring application restart.

### Local evidence
- [Repository instructions](file:///D:/MyProjects/TokenHound/AGENTS.md): Architecture invariants, rate-limit deadlines, safety floor constants (`MINIMUM_INTERVAL_SECONDS = 30s`, `MINIMUM_RETRY_FLOOR = 60s`).
- [RefreshSettings.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/RefreshSettings.cs): Defines `ActiveIntervalSeconds`, `IdleIntervalSeconds`, and `MINIMUM_INTERVAL_SECONDS = 30`.
- [RefreshSettingsStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Configuration/RefreshSettingsStore.cs): Deserializes `Refresh` settings from `appsettings.json`; currently lacks persistence/saving capability.
- [RateLimitPolicy.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RateLimitPolicy.cs): Implements rate-limit evaluation and `MINIMUM_RETRY_FLOOR = TimeSpan.FromSeconds(60)`.
- [BackoffCalculator.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/BackoffCalculator.cs): Computes exponential backoff with `Floor = 60s` and `Ceiling = 3600s`.
- [RefreshSchedulePolicy.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs): Defines `DEFAULT_ACTIVE_INTERVAL = 180s` and `DEFAULT_IDLE_INTERVAL = 300s`.
- [UsageStore.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Engine/UsageStore.cs): Manages background periodic polling timer and refresh scheduling.
- [appsettings.json](file:///D:/MyProjects/TokenHound/src/TokenHound.App/appsettings.json): Contains existing `Hud`, `Refresh`, and `Log` configuration blocks.
- [SettingsWindow.xaml](file:///D:/MyProjects/TokenHound/src/TokenHound.App/UI/Windows/SettingsWindow.xaml): Modeless dialog shell created in `prd-main-window-context-menu`.

### Explicit assumptions
- **A-01**: Input durations are configured in integer seconds, directly mapping to `ActiveIntervalSeconds`, `IdleIntervalSeconds`, and `MinimumRetryFloorSeconds`.
- **A-02**: The sibling slice `settings-01-provider-management` provides the Settings dialog container and ViewModel framework into which this section integrates.
- **A-03**: In `appsettings.json`, rate-limit retry settings are stored under a distinct `"RateLimit"` section (e.g., `{"RateLimit": {"MinimumRetryFloorSeconds": 60}}`), maintaining clean configuration separation.
- **A-04**: Idle interval should be greater than or equal to active interval to maintain logical polling hierarchy (active polling when busy, throttled polling when idle).
- **A-05**: Dynamic update of `UsageStore` updates the interval of the active timer without resetting the consecutive failure counts or clearing currently cached provider snapshots.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified organizational source.
- [x] Implementation details remain in the TechSpec.
