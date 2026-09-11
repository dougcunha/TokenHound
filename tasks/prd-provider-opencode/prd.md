# PRD — OpenCode Provider Integration

## Problem and context

Developers utilizing **OpenCode** (open-source AI coding agent by Anomaly, available as CLI, TUI, and Desktop) currently lack real-time visibility into their subscription quotas, rate-limit thresholds, and active agent sessions. 

OpenCode offers a curated model subscription called **OpenCode Go** ($10/month), which enforces three distinct quota windows: a 5-hour rolling window, a weekly quota, and a monthly quota. While OpenCode tracks session history locally in SQLite (`opencode.db`) and stores credentials in `auth.json`, users must either wait for CLI errors or check web consoles to assess their quota burn rate.

Integrating OpenCode into TokenHound enables instant, floating HUD telemetry for OpenCode Go limits, provides early warning before hitting the 5-hour rolling throttle, and tracks agent process activity without stealing focus from developer workflows.

---

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Support OpenCode as a first-class `IUsageProvider` | Provider ID `opencode` registered, discovers credentials, and serves valid snapshots. |
| OBJ-02 | Zero-configuration onboarding | Automatically extracts API key from `%USERPROFILE%\.local\share\opencode\auth.json` without user prompt. |
| OBJ-03 | Accurate 3-tier quota telemetry | Snapshot produces three `LimitWindow` records: 5-Hour Rolling, Weekly, and Monthly with authoritative reset times. |
| OBJ-04 | Rate-limit block persistence & backoff | HTTP 429 triggers `UsageBlock` with `BlockedReason.RateLimitReached` and respects `Retry-After`. |
| OBJ-05 | Real-time agent process liveness | HUD ring indicates active state when `opencode.exe` or `OpenCode.exe` is running. |

---

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | OpenCode Go subscriber | View remaining quota on desktop HUD | Avoid unexpected throttling mid-coding | TokenHound reads `auth.json`, fetches `zen/go/v1/usage`, displays rolling/weekly/monthly rings on hover. |
| US-02 | User hitting 5-hour rolling limit | See countdown until reset | Know exactly when to resume tasks | API returns HTTP 429 or `status: "rate-limited"`; HUD ring turns red and displays `resetsAt` countdown. |
| US-03 | User without OpenCode installed | Clean status without crashes | Clear guidance to configure | `auth.json` missing; provider reports `ProviderStatus.NeedsAuth` without crashing or spamming logs. |
| US-04 | Developer running CLI prompts | Activity status on HUD | Visual feedback that agent is processing | TokenHound monitors `opencode` process; status reflects `Busy` during prompt execution. |

---

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Credential Discovery | Borrow API key from `%USERPROFILE%\.local\share\opencode\auth.json` (keys `opencode-go` or `opencode`). Support override via `OPENCODE_GO_API_KEY` / `OPENCODE_API_KEY`. |
| FR-02 | Read-Only Credential Access | File opened with `FileAccess.Read` and `FileShare.ReadWrite \| FileShare.Delete`. TokenHound never modifies or refreshes `auth.json`. |
| FR-03 | Quota Endpoint Telemetry | Query `GET https://opencode.ai/zen/go/v1/usage` with `Authorization: Bearer <key>` and custom `User-Agent: TokenHound`. |
| FR-04 | Multi-Window Parsing | Parse `usage.rolling`, `usage.weekly`, and `usage.monthly` into distinct `LimitWindow` instances with exact percentage, reset UTC, and period. |
| FR-05 | Rate-Limit & 429 Handling | When response is 429 or `status == "rate-limited"`, parse `Retry-After` or `resetsAt`, set `ActiveBlock`, and apply backoff via `RateLimitPolicy`. |
| FR-06 | Process Liveness Monitoring | Implement `OpenCodeActivityMonitor : IActivityMonitor` tracking `opencode` and `OpenCode` process presence. |
| FR-07 | Status State Machine | Transition to `ProviderStatus.Ok` on success, `ProviderStatus.NeedsAuth` on missing/unauthorized key (401), `ProviderStatus.RateLimited` on 429, and `ProviderStatus.Stale` when network fails with cached reading. |
| FR-08 | Configuration & Registration | Add `OpenCode` to default provider configurations in `appsettings.json` and settings schemas. |

---

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Core Purity | `TokenHound.Core` remains 100% pure; all HTTP, file system, and process discovery code resides in `TokenHound.Infrastructure`. |
| NFR-02 | Zero Fake Data | If fields are missing or null, leave values `null`; never invent denominators or assumptions. `UsedFraction` strictly mapped from `percent / 100.0`. |
| NFR-03 | Borrow-Don't-Own | TokenHound never writes credentials or creates competing accounts. |
| NFR-04 | Network Resilience & Timeout | HTTP client timeout <= 10s. Exponential backoff and Retry-After floor enforced. |
| NFR-05 | Testability | Complete unit test coverage for JSON discovery, API response parsing, 429 handling, and error states using in-memory mock handlers. |

---

## User experience

- **HUD Capsule / Ring**:
  - Displays primary `5-Hour Rolling` percentage on the provider ring.
  - Ring color transitions to warning/blocked palette if rolling, weekly, or monthly quota exceeds threshold.
- **Hover Tooltip Card**:
  - Displays 3 distinct progress bars:
    - `5-Hour Rolling`: `{percent}%` (resets in `{time}`)
    - `Weekly`: `{percent}%` (resets in `{time}`)
    - `Monthly`: `{percent}%` (resets in `{time}`)
- **Error State**:
  - If unauthenticated, displays "OpenCode: Needs Login (`opencode /connect`)".

---

## Constraints and dependencies

- Target framework: `.NET 10` / C# 13.
- Relies on OpenCode Go API endpoint `https://opencode.ai/zen/go/v1/usage`.
- Standard Windows file location: `%USERPROFILE%\.local\share\opencode\auth.json`.

---

## Out of scope

- Direct manipulation of OpenCode local SQLite sessions (`opencode.db` modifications).
- Scraping OpenCode web console via browser automation.
- Managing OpenCode Zen Pay-As-You-Go Stripe billing deposits.

---

## Assumptions and sources

- Assumption: OpenCode Go usage API endpoint format (`zen/go/v1/usage`) remains stable as implemented in `anomalyco/opencode`.
- External source: [OpenCode Documentation](https://opencode.ai/docs), [anomalyco/opencode GitHub](https://github.com/anomalyco/opencode), [Provider Specification 12](file:///D:/MyProjects/TokenHound/docs/specs/12-PROVIDER-OPENCODE.md).

---

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from the user and repository architectural invariants.
- [x] Implementation details remain in the TechSpec.
