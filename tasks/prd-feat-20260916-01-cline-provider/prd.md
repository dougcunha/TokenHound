# PRD — Cline usage provider (borrowed telemetry + local evidence)

## Problem and context

TokenHound renders usage and liveness for Claude, Codex, Cursor, Copilot, Antigravity/Gemini, GLM, Perplexity, and OpenCode, but not for Cline, whose CLI/desktop app is widely used and stores a borrowed session locally. Cline publishes no authoritative token quota; the useful signals are the remaining credit balance, locally recorded token usage, the free-tier model limit, and whether Cline is running or busy. The provider API contract was verified live (2026-09-15): `User-Agent: TokenHound` returns HTTP 200 on `api.cline.bot/api/v1/users/me`, the free plan returns 404, and the Bearer token must keep its `workos:` prefix verbatim.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Signed-in users see the Cline credit balance and plan scope in the HUD | `cline:credits` row rendered from `Snapshot.ClineAccount`; no progress fraction |
| OBJ-02 | Users see locally derived token usage for the sampled window | `cline:local` row built from `Snapshot.ClineLocal` (24 h window) |
| OBJ-03 | Free-tier exhaustion is visible with a reset countdown | `cline:freelimit` row only while the parsed local limit is active |
| OBJ-04 | Cline liveness/busy state participates in activity aggregation | `ClineActivityMonitor` hub-first liveness; busy when `MAX(updated_at)` ≤ 60 s |
| OBJ-05 | No fabricated numbers ever display | `UsedFraction` stays null without a denominator; Pass windows emitted only under proven coverage |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Cline Pass subscriber | See remaining credits without opening Cline | HUD row with balance and plan scope | Pass caps add windows only when sampled transactions provably cover them |
| US-02 | Free-tier user | Know when the free model limit blocks work | "Limit reached" row with countdown | Delay parsed from the local session message; block expires silently |
| US-03 | Cline CLI user | See whether Cline is busy | Hub lock PID validated; process-name fallback | Activity taken from `sessions.db` `MAX(updated_at)` |
| US-04 | User without Cline | TokenHound stays quiet | NeedsAuth snapshot with guidance | No network call without a credential |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| RF-01 | Discover the borrowed credential from `CLINE_API_KEY` (prefixing `workos:` when bare), else `%USERPROFILE%\.cline\data\settings\providers.json` keys `cline`/`cline-pass` with `settings.auth.{accessToken,expiresAt,accountId}` | Environment wins; file read read-only; partial writes tolerated (null, retried next poll) |
| RF-02 | Fetch account telemetry: `GET api.cline.bot/api/v1/users/me` with Bearer token (prefix verbatim), `User-Agent: TokenHound`, `Accept: application/json` | HTTP 200 → `ClineAccountUsage{BalanceCredits, PlanName, HasPassSubscription}` |
| RF-03 | Map failures: 401 → NeedsAuth, 403 → AccessDenied, 429 → RateLimited with persisted Retry-After deadline, timeout → Stale | Snapshot statuses plus typed exceptions per failure class |
| RF-04 | Map Cline Pass caps (`inferenceCapThreshold` `last5/7/30days...CostUSDPerUser`) to limit windows | Fraction plus `TotalUnits`/`RemainingUnits` only when sampled transactions provably cover the window; truncated page or dateless transaction skips the window |
| RF-05 | Aggregate local token usage from `sessions.db` | Input/output/cache read/cache write tokens, model calls, window start, last activity |
| RF-06 | Detect the free model limit from local session messages ("free limit reached on model … try again in X") | Block reported only while `IsActive(now)`; delay becomes the reset countdown |
| RF-07 | Liveness via hub lock `.cline\data\locks\hub\production.json` (pid/startedAt) validated first; names `cline`/`cline-cli`/`cline.exe` as fallback | Busy when `MAX(updated_at)` is ≤ 60 s old; Idle otherwise |
| RF-08 | Present rows `cline:credits`, `cline:local`, `cline:freelimit`; catalog name "Cline", badge "Cl", glyph `Glyph.Cline` | Row content per spec; countdown formatted by the shared `FormatResetCountdown` |
| RF-09 | Register provider and monitor, enabled by default, provider disposed on shutdown | `cline` in `DEFAULT_PROVIDERS` and `appsettings.json`; provider added to disposable resources |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| RNF-01 | Security | Never write or refresh Cline-owned credentials; borrow read-only (`FileShare.ReadWrite \| FileShare.Delete` via `SharedFileReader`) |
| RNF-02 | Data honesty | Never invent limits or denominators; `usedFraction = null` when the API reports only remaining values |
| RNF-03 | Rate-limit safety | Persist and respect 429 deadlines before dispatching; never retry immediately on `Retry-After: 0` |
| RNF-04 | Architecture | `TokenHound.Core` stays pure: models and contracts only, zero UI/OS dependencies |
| RNF-05 | Performance | Local reads bounded (24 h) and lock-free; SQLite opened `Mode=ReadOnly` with `immutable=1` fallback when `-shm` is missing |
| RNF-06 | Maintainability | Files ≤ 300 lines (partial classes), methods ≤ 30 lines |

## User experience

Rows appear in the HUD notch: "Cline credits" (e.g. "12.5 remaining", scope = plan name), "Local tokens (24h)" ("125 tokens", "2 model calls"), and "Free model limit" ("Limit reached", "Resets in 1h 30m"). Without a credential the HUD shows "Run the Cline CLI or app and sign in to lend TokenHound a session."; an expired borrowed token shows "Cline access token expired; use Cline once so it refreshes the borrowed token." HUD rendering is verified manually via Windows MCP screenshot (desktop .NET policy; no automated E2E).

## Constraints and dependencies

- The `workos:` prefix must be preserved verbatim; the token expires in ~1 h and only Cline refreshes it.
- `api.cline.bot` requires `User-Agent: TokenHound` (verified 200; free plan → 404).
- Engine contracts: `IUsageProvider`, `IActivityMonitor`, `RateLimitPolicy`, `UsageStore` registration.
- .NET desktop policy: E2E omitted.

## Out of scope

- Writing or refreshing Cline credentials; provider settings UI; inventing token quotas; non-Windows platforms; cost analytics beyond the three Pass caps.

## Assumptions and sources

- Fact (verified live, 2026-09-15): `User-Agent: TokenHound` → 200; free plan → 404; `workos:` prefix required. Impact if changed: client/discovery adjustments required.
- Assumption: Pass caps share the `costUsd` unit with sampled transactions. Impact if wrong: the mapper skips the window instead of showing a wrong fraction (fail-safe already implemented).
- Assumption: the hub lock path `.cline\data\locks\hub\production.json` is stable. Mitigation: process-name fallback.
- External source: verified empirically against `api.cline.bot` (no public docs relied on); local schema observed in `%USERPROFILE%\.cline\data`.

## PRD acceptance gate

- [x] Every requirement has an ID and an observable criterion.
- [x] Metrics, boundaries, and out-of-scope items are explicit.
- [x] Internal rules came from AGENTS.md invariants and the verified API contract.
- [x] Implementation details remain in the TechSpec.