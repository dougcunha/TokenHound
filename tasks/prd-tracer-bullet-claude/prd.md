# PRD — Tracer Bullet: Minimal Notch HUD and Claude Code Provider

## Problem and context

Following the completion of the foundation layer (`TokenHound.Core` and infrastructure primitives), TokenHound requires an end-to-end vertical validation spike ("Tracer Bullet"). This slice connects presentation, orchestration, and a live AI assistant provider to verify the complete data flow from developer machine telemetry to the desktop user interface.

Claude Code is selected as the canonical reference provider because it features a clean local OAuth credential storage pattern (`.credentials.json`), standard REST telemetry with Bearer token authentication (`/api/oauth/usage`), and session liveness tracking via process PID files. Connecting this provider to a minimal WPF screen-edge HUD capsule validates the non-activating floating window architecture (`WS_EX_NOACTIVATE`) and establishes the integration pattern for all subsequent provider adapters.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Complete end-to-end telemetry pipeline | Real-time Claude Code quota (or Mock fallback) flows from discovery to visual rendering in the WPF HUD. |
| OBJ-02 | Non-activating desktop HUD behavior | Clicking, hovering, or updating the Notch window never steals focus from the foreground terminal or code editor (`WS_EX_NOACTIVATE` verified). |
| OBJ-03 | Real-time agent activity detection | Provider ring visually indicates when a local `claude` CLI process is executing tasks (`Busy`) versus waiting/idle. |
| OBJ-04 | Graceful unauthenticated and error fallback | When `.credentials.json` is missing or expired, HUD transitions to `NeedsAuth` state with actionable terminal instructions ("Execute `claude login`"). |
| OBJ-05 | Low desktop resource utilization | Idle working set memory consumption of the WPF companion remains <= 45 MB. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer using Claude Code | A glanceable floating HUD showing remaining 5-hour rolling session and weekly quotas | Prevents unexpected rate limits mid-task without opening browser dashboards | User looks at screen top-edge to view circular consumption ring and hover card |
| US-02 | Active CLI Developer | A screen companion that never steals window focus or disrupts keyboard typing | Can type terminal commands and hotkeys continuously while HUD refreshes in real-time | HUD updates telemetry and redraws rings without activating its HWND |
| US-03 | Developer without active Claude session | Clear diagnostic state and fallback behavior when Claude is logged out | Knows immediately how to restore authentication without digging through application logs | Ring turns attention color; tooltip card states "Execute `claude login` in terminal" |
| US-04 | Team Member / Offline Tester | Seamless fallback to `MockUsageProvider` when Claude credentials are not present on test machine | Verifies visual layouts, ring arcs, and tooltips in development without live Anthropic accounts | Application loads pre-configured mock scenarios when no live token is found |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Discover Claude Code OAuth credentials | Locates and parses `%USERPROFILE%\.claude\.credentials.json` (and multi-profile `~/.claude-<slug>`) extracting `accessToken` and token expiry. |
| FR-02 | Query Anthropic OAuth usage telemetry | Dispatches `GET https://api.anthropic.com/api/oauth/usage` with `Authorization: Bearer <token>` and `anthropic-beta: oauth-2025-04-20`. |
| FR-03 | Map Claude telemetry to domain `Snapshot` | Extracts 5-hour window (`five_hour`) and 7-day window (`seven_day`) into `LimitWindow` records with utilization fraction, reset UTC, and period. |
| FR-04 | Monitor active Claude Code sessions | Inspects `%USERPROFILE%\.claude\sessions\*.json` and uses `ProcessLiveness.IsProcessAlive(pid, startTimeUtc)` to determine `Busy` or `Idle` state. |
| FR-05 | Coordinate polling in `UsageStore` | Central coordinator orchestrating registered providers, dispatching requests on 60s active vs 300s idle cadences, and updating ViewModels. |
| FR-06 | Implement Win32 `WS_EX_NOACTIVATE` window styling | Injects Win32 extended styles `WS_EX_NOACTIVATE (0x08000000)`, `WS_EX_TOOLWINDOW (0x00000080)`, and `WS_EX_TOPMOST (0x00000008)` via `HwndSource`. |
| FR-07 | Create minimal `NotchWindow` WPF HUD capsule | Borderless transparent window pinned to top-center of primary screen work area, styled as a pill capsule. |
| FR-08 | Implement `ProviderRing` custom control | Renders a circular progress indicator displaying utilization percentage, provider badge ("C" for Claude), and color state (green, amber, red, purple, dim). |
| FR-09 | Implement `TooltipCard` hover details card | Displays provider name, session usage fraction %, weekly usage fraction %, reset countdown timers, and active process PID/status. |
| FR-10 | Implement `NotchViewModel` MVVM state binding | Binds active provider rings and snapshots to the Notch window, automatically falling back to `MockUsageProvider` when live tokens are absent. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Focus Safety | Window must never steal focus from active windows; `WS_EX_NOACTIVATE` and `WS_EX_TOOLWINDOW` must be verified via Win32 `GetWindowLong`. |
| NFR-02 | Memory Efficiency | Application idle footprint <= 45 MB working set on Windows 11. |
| NFR-03 | UI Fluidity | UI updates and tooltip cards must render on the WPF UI thread without blocking network calls (`.ConfigureAwait(false)` in Infra). |
| NFR-04 | Credential Hygiene | OAuth bearer tokens must be held in ephemeral variables and never serialized to persistent logs, diagnostics, or telemetry output. |
| NFR-05 | Test Standards | Unit tests for client parsing, session monitoring, and view models executed via Microsoft Testing Platform with `--minimum-expected-tests 1`. |

## User experience

- **Top-Center Floating Capsule**: Pinned to the top-center edge of the primary monitor. Minimal footprint (~120x36 px) containing the Claude provider ring.
- **Provider Ring Visuals**:
  - `Normal` (<70%): Crisp green ring.
  - `Warning` (70%-90%): Amber ring.
  - `Exhausted / Rate-limited` (>90% or 429): Red ring with pulsating activity indicator.
  - `Unauthenticated` (`NeedsAuth`): Purple attention badge with tooltip directing to `claude login`.
  - `Stale` (>15m no update): Dimmed opacity (50%).
- **Hover Interaction**: Hovering over the ring expands or displays `TooltipCard` with detailed reset timers (e.g., "5-Hour Session: 35% (resets in 2h 15m)", "Weekly Quota: 72% (resets in 3d)").

## Constraints and dependencies

- Target Platform: Windows 11 on .NET 10 (`net10.0-windows` for `TokenHound.App`, `net10.0` for Infra/Core).
- WPF Presentation Framework (`Microsoft.NET.Sdk.WindowsDesktop` with `UseWPF=true`).
- Claude Code OAuth endpoint: `https://api.anthropic.com/api/oauth/usage` with required beta header `oauth-2025-04-20`.
- Interop: `user32.dll` via `HwndSource` for `WS_EX_NOACTIVATE`.

## Out of scope

- Advanced Bézier curvature morphing and dynamic edge snapping (deferred to PRD 08).
- DWM Mica/Acrylic background blur integration (deferred to PRD 08).
- Multi-monitor dragging or edge reconfiguration (deferred to PRD 08).
- Windows System Tray icon (`NotifyIcon`) and preferences dialog (deferred to PRD 08).
- Implementation of remaining provider adapters (Cursor, Codex, Antigravity, GLM, Perplexity deferred to PRDs 03-07).

## Assumptions and sources

- Assumption: Claude Code CLI on Windows writes OAuth credentials to `%USERPROFILE%\.claude\.credentials.json` with fields `accessToken` and `expiresAt`. Source: `docs/specs/03-PROVIDER-CLAUDE-CODE.md`.
- Assumption: Claude Code CLI session files contain PID and state under `%USERPROFILE%\.claude\sessions\`. Source: `docs/specs/03-PROVIDER-CLAUDE-CODE.md`.
- External source: Anthropic API OAuth usage documentation and beta headers (`oauth-2025-04-20`).

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified organizational source.
- [x] Implementation details remain in the TechSpec.
