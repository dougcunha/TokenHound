# PRD 10 — Antigravity Quota Retrieval Warmup

## 1. Problem Statement
When Google Antigravity (or the `agy` CLI) launches or remains idle, the embedded Language Server's internal `QuotaSummaryCache` may be unpopulated. When TokenHound queries `RetrieveUserQuotaSummary` in this state, the local server returns an empty JSON payload (`{}`) containing no quota groups.

Because TokenHound currently treats any empty group list as a failure of Layer 1 without attempting a session handshake, it immediately drops through Layer 2 to Layer 3 (`AntigravityTranscriptReader`). Consequently, the desktop HUD displays only a flat interaction counter (`Requests Today`) without percentage rings or reset timers, despite the user having active quotas. The session only recovers if an external caller (or the IDE frontend) queries `GetUserStatus`.

## 2. Functional Requirements

### RF-01: Session Warmup via `GetUserStatus`
- When `RetrieveUserQuotaSummaryAsync` returns a response where `Groups` is `null` or empty (`quota?.Groups is null || quota.Groups.Count == 0`), the Language Server client must support issuing a `WarmupSessionAsync` request to the endpoint's active port using the RPC method `GetUserStatus`.
- The warmup call must send the standard JSON payload and the extracted CSRF token in the `x-codeium-csrf-token` header.

#### Acceptance Criteria
1. `AntigravityLanguageServerClient` exposes a method to query `GetUserStatus` or perform session warmup on the candidate endpoint.
2. The request includes the required CSRF header and appropriate timeout / `CancellationToken`.

### RF-02: Automatic Quota Retry After Warmup
- In `AntigravityUsageProvider.TryGetLanguageServerSnapshotAsync`, if the initial quota summary returns null or empty groups, it triggers the warmup call against the endpoint and executes a single retry of `RetrieveUserQuotaSummaryAsync`.
- If the retried response contains non-empty quota groups, the provider maps them to `LimitWindow`s and returns an `Official` `Snapshot` with status `Ok`.

#### Acceptance Criteria
1. If the first quota call returns empty groups, a warmup call is dispatched followed by a retry.
2. If the retry returns valid quota groups, the resulting snapshot has `Fidelity.Official`, `Status = ProviderStatus.Ok`, and populated `LimitWindows`.
3. If the first quota call succeeds directly with groups, no warmup or retry is performed.

### RF-03: Graceful Fallback on Persistent Failure
- If the warmup call fails (HTTP error, connection reset, timeout) or the retried quota summary is still empty, the provider must not throw.
- The provider continues along the existing waterfall (Layer 2 Cloud Code, then Layer 3 Transcripts), preserving existing fallback invariants.

#### Acceptance Criteria
1. Exceptions thrown during warmup are safely caught and do not crash the provider.
2. When the retried quota summary remains empty, the provider returns `(null, isRunning: true)` to allow smooth fallback to transcripts.

## 3. Non-Functional Requirements
- **RNF-01 (Invariants)**: Keep `TokenHound.Core` pure; zero new dependencies; never invent limits or denominators.
- **RNF-02 (Performance)**: The warmup and retry must adhere to the passed `CancellationToken` and complete within the standard client timeout.
- **RNF-03 (Code Style)**: Follow repository C# guidelines (methods <= 30 lines, classes <= 300 lines, XML docs).

## 4. Out of Scope
- Altering the desktop HUD or presentation layers.
- Altering Cloud Code remote queries or transcript counting logic.
- Writing or modifying external process memory or configuration.
