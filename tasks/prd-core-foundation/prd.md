# PRD — Core Domain Models, Contracts, and Infrastructure Primitives

## Problem and context

TokenHound monitors real-time quota, rate limits, and agent execution across six distinct AI coding assistants on Windows 11. To allow multiple independent agents to implement provider adapters concurrently without architectural drift, merge collisions, or code duplication, the solution requires a unified foundation layer.

This foundation establishes pure domain models, system contracts, resilience and polling policies in `TokenHound.Core`, alongside reusable, non-locking OS access primitives (SQLite WAL reader, Windows Credential Manager wrapper, shared file reader, and process liveness detector) and an offline mock provider in `TokenHound.Infrastructure`.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Completely decoupled domain core | `TokenHound.Core.csproj` targets pure `net10.0` with zero references to WPF, Win32, or presentation libraries. |
| OBJ-02 | Non-locking concurrent storage access | `SafeSqliteReader` successfully reads WAL databases concurrently with external writers without throwing `database is locked` errors. |
| OBJ-03 | Resilient rate-limit deadline enforcement | Rate-limit policy persists deadlines and refuses to dispatch requests prior to deadline expiry, even when `Retry-After: 0` is received. |
| OBJ-04 | 100% offline testability of HUD states | `MockUsageProvider` simulates all canonical states (`Ok`, `Stale`, `NeedsAuth`, `RateLimited`, and null denominator) without live network credentials. |
| OBJ-05 | Comprehensive test suite under MTP | Test project passes with at least 25 automated tests executed via `rtk dotnet test --minimum-expected-tests 1`. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Provider Adapter Developer | Standardized immutable records (`Snapshot`, `LimitWindow`, `UsageBlock`) and interfaces (`IUsageProvider`, `IActivityMonitor`) | Can implement a provider adapter in isolation without touching core orchestration or UI code | Adapter implements `IUsageProvider.GetSnapshotAsync()` and returns immutable `Snapshot` |
| US-02 | Polling Engine Coordinator | Pure scheduling and rate-limit policies (`RefreshSchedulePolicy`, `RateLimitPolicy`, `BackoffCalculator`) | Automatically adjusts polling cadence (60s active vs 300s idle) and enforces HTTP 429 penalties | Evaluates `ShouldRefresh(isBusy, timeSinceLastAttempt, idleInterval)` on every tick |
| US-03 | Provider Adapter Reader | Non-intrusive file and database readers (`SharedFileReader`, `SafeSqliteReader`) | Reads local developer databases and configuration files without locking out official tools or crashing on shared access | Opens SQLite with `Mode=ReadOnly`, falling back to `immutable=1` when `-shm` sidecar is absent |
| US-04 | UI Developer / Automated Agent | Deterministic `MockUsageProvider` with canned snapshot fixtures | Develops and validates presentation controls (Notch capsule, rings, tooltips) without requiring live third-party subscriptions | Toggles simulated states (`Normal`, `HighUsage`, `RateLimited`, `Unauthenticated`) at runtime |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Define immutable domain models (`Snapshot`, `LimitWindow`, `UsageBlock`, `AgentSession`, and enums `ProviderStatus`, `Fidelity`) | Implemented as C# records with `init` and `required` properties; sealed where applicable; XML-documented. |
| FR-02 | Enforce the 'Zero Fake Data' policy in `LimitWindow` | When a provider reports only remaining units without a maximum limit, `UsedFraction` must be `null`; never calculate synthetic percentages. |
| FR-03 | Define standard system contracts (`IUsageProvider`, `IActivityMonitor`, `ICredentialStore`) | Interfaces accept `CancellationToken`, return `ValueTask` or `Task`, and adhere to file-scoped namespaces and clean separation. |
| FR-04 | Implement `BackoffCalculator` with exponential backoff and jitter | Computes backoff intervals starting at a 60-second floor up to a defined ceiling, with deterministic randomization for jitter. |
| FR-05 | Implement `RateLimitPolicy` with unexpired deadline protection | Returns false for dispatch requests if the current UTC time is before the recorded rate-limit deadline; ignores `Retry-After: 0`. |
| FR-06 | Implement `RefreshSchedulePolicy` | Pure evaluation of `ShouldRefresh(isBusy, timeSinceLastAttempt, idleInterval)` returning true if any agent is busy or idle interval elapsed. |
| FR-07 | Implement `SafeSqliteReader` for read-only concurrent access | Opens SQLite connections with `Mode=ReadOnly`; falls back to `immutable=1` when sidecar files (`-shm`, `-wal`) are unavailable. |
| FR-08 | Implement `SharedFileReader` for shared file access | Reads files using `FileShare.ReadWrite | FileShare.Delete` with non-blocking stream reading. |
| FR-09 | Implement `WindowsCredentialManager` wrapper | Safely invokes `advapi32.dll` (`CredReadW` and `CredFree`) to read target credentials (e.g. `gemini:antigravity`) without prompting UI. |
| FR-10 | Implement `ProcessLiveness` and `ProcessDiscovery` | Checks process existence by PID and validates `StartTimeUtc` to prevent misidentifying recycled PIDs on Windows. |
| FR-11 | Implement `MockUsageProvider` and static test fixtures | Implements `IUsageProvider` with configurable states (`Ok`, `Warning`, `RateLimited`, `NeedsAuth`, `Stale`) and pre-baked JSON fixtures. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Code Architecture & Style | All classes sealed by default; one class per file; <= 300 lines per file; methods <= 30 lines; nesting <= 3 levels; alphabetized usings. |
| NFR-02 | Platform Purity | `TokenHound.Core` must have zero dependencies on Windows APIs, WPF, or native libraries; 100% executable on any standard .NET 10 runtime. |
| NFR-03 | Credential Security | Credentials in memory must never be written to stdout, telemetry, or unencrypted persistent logs. |
| NFR-04 | Async & Thread Safety | All I/O operations must be asynchronous with `.ConfigureAwait(false)` in Core and Infrastructure layers. |
| NFR-05 | Test Runner Standard | All test projects must execute under Microsoft Testing Platform (MTP) using `--minimum-expected-tests 1`. |

## User experience

While `TokenHound.Core` and Infrastructure primitives do not render UI directly, they provide the domain primitives (`ProviderStatus`, `LimitWindow`, `UsageBlock`) and error semantics that drive the Notch HUD visual indicators:
- `NeedsAuth`: Prompts the UI to show an attention badge and tooltip with terminal guidance (e.g., "Run `claude login`").
- `RateLimited`: Provides the exact `ResetTimeUtc` countdown for the UI tooltip timer.
- `Stale`: Informs the UI to dim or grey-out the provider ring when data exceeds the 15-minute freshness threshold.

## Constraints and dependencies

- Target Framework: `net10.0` (Core and Infrastructure).
- Microsoft.Data.Sqlite 10.x for local SQLite reads.
- System.Security.Cryptography.ProtectedData for local DPAPI access.
- advapi32.dll P/Invoke (`CredReadW`) on Windows.
- No third-party heavy DI containers or ORMs; keep dependencies minimal and lightweight.

## Out of scope

- WPF window rendering, XAML controls, or Win32 window message hooks (deferred to PRD 02 and PRD 08).
- Implementation of specific external provider adapters (Claude, Cursor, Codex, Antigravity, GLM, Perplexity deferred to PRDs 02-07).
- Persistent disk storage of historical snapshots across days (deferred to Engine Archive task).
- System tray icons and native OS notifications (deferred to PRD 08).

## Assumptions and sources

- Assumption: Developer tools on Windows (Cursor, Codex, Claude) store local tokens and logs under `%USERPROFILE%` and `%APPDATA%` in standard SQLite WAL or JSON format. Source: `docs/specs/02-WINDOWS-CREDENTIALS-SECURITY.md`.
- External source: Microsoft Learn - SQLite in .NET with `Microsoft.Data.Sqlite` read-only connection strings.
- External source: Microsoft Learn - Win32 `CredReadW` function documentation.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user or an identified organizational source.
- [x] Implementation details remain in the TechSpec.
