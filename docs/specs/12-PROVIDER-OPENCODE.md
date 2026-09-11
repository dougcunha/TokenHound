# Provider Technical Specification: OpenCode (Go Quotas & Local Telemetry)

This specification defines the integration of **OpenCode** on **Windows 11**, covering local credential discovery, official quota telemetry via the OpenCode Go API endpoint, local SQLite session token aggregation, and process liveness monitoring.

---

## 1. Overview and Data Fidelity

- **Provider ID**: `opencode`.
- **Display Name**: `OpenCode`.
- **Fidelity**: `Fidelity.Official` (authoritative quota and reset timestamps returned by the official Anomaly / OpenCode gateway).
- **Headline Metrics**:
  - 5-Hour Rolling Limit Window (`rolling`).
  - Weekly Limit Window (`weekly`).
  - Monthly Limit Window (`monthly`).

---

## 2. Credential Location & Extraction on Windows 11

TokenHound adheres to the *Borrow-Don't-Own* architectural invariant: it never creates an independent OpenCode account or modifies authentication state. It borrows the API key already stored by the official OpenCode CLI or Desktop app.

### Storage Location

On Windows 11, OpenCode follows the XDG standard under the user's home profile:

```text
%USERPROFILE%\.local\share\opencode\auth.json
```

### File Schema

The `auth.json` file is a dictionary mapping provider identifiers to authentication objects:

```json
{
  "opencode-go": {
    "type": "api",
    "key": "zen_live_..."
  },
  "opencode": {
    "type": "api",
    "key": "zen_live_..."
  }
}
```

### Discovery Precedence

1. Environment variable `OPENCODE_GO_API_KEY` or `OPENCODE_API_KEY`.
2. Provider key `"opencode-go"` in `%USERPROFILE%\.local\share\opencode\auth.json`.
3. Provider key `"opencode"` in `%USERPROFILE%\.local\share\opencode\auth.json`.

### Token Handling Invariants

- **Read-Only Borrowing**: File is opened strictly with `FileAccess.Read` and `FileShare.ReadWrite | FileShare.Delete`.
- **No-Refresh Invariant**: TokenHound **must never** rewrite or update `auth.json`. If the telemetry call returns HTTP 401 (`AuthError`), TokenHound transitions to `ProviderStatus.NeedsAuth` and prompts the user to re-authenticate using `opencode` / `/connect` in their terminal.

---

## 3. Quota Telemetry Endpoint & Query Protocol

### Endpoint Definition

- **Method**: `GET`
- **URL**: `https://opencode.ai/zen/go/v1/usage`
- **Headers**:
  - `Authorization: Bearer <API_KEY>`
  - `User-Agent: TokenHound`
  - `Accept: application/json`

### Success Response Payload (HTTP 200)

```json
{
  "usage": {
    "rolling": {
      "status": "ok",
      "percent": 12.5,
      "resetsAt": "2026-09-12T00:45:07.613Z"
    },
    "weekly": {
      "status": "ok",
      "percent": 34.0,
      "resetsAt": "2026-09-14T00:00:00.613Z"
    },
    "monthly": {
      "status": "ok",
      "percent": 58.2,
      "resetsAt": "2026-10-10T14:55:16.613Z"
    }
  }
}
```

### Limit Window Mapping

| JSON Field | Limit Window Name | Period | UsedFraction | ResetTimeUtc |
| :--- | :--- | :--- | :--- | :--- |
| `usage.rolling` | `5-Hour Rolling` | `TimeSpan.FromHours(5)` | `percent / 100.0` | `DateTimeOffset.Parse(resetsAt)` |
| `usage.weekly` | `Weekly` | `TimeSpan.FromDays(7)` | `percent / 100.0` | `DateTimeOffset.Parse(resetsAt)` |
| `usage.monthly` | `Monthly` | `TimeSpan.FromDays(30)` | `percent / 100.0` | `DateTimeOffset.Parse(resetsAt)` |

### Rate-Limit & Error Handling (HTTP 429 & 403)

When limits are reached, the endpoint returns HTTP 429 (or HTTP 403 with `EntitlementError`):

```json
{
  "type": "error",
  "error": {
    "type": "GoUsageLimitError",
    "message": "OpenCode Go 5 hour usage limit reached."
  },
  "metadata": {
    "workspace": "wrk_...",
    "limitName": "5 hour"
  }
}
```

- **Headers**: `Retry-After: <seconds>`
- **Behavior**:
  - Extract the `Retry-After` header.
  - Set `Snapshot.ActiveBlock` with `BlockedReason.RateLimitReached` and calculated `BlockedUntilUtc`.
  - Delegate deadline calculation to `RateLimitPolicy.ComputeBackoffFloor`.

---

## 4. Local Session & Token Telemetry (SQLite WAL)

OpenCode records all conversation turns, tokens, and costs in a local SQLite database in WAL mode:

```text
%USERPROFILE%\.local\share\opencode\opencode.db
```

### Safe Concurrency Rules

- Open strictly using `SafeSqliteReader`:
  `Data Source=<path>;Mode=ReadOnly;Cache=Shared;Default Timeout=2;Pooling=False`
- Automatically fall back to `Data Source=file:<path>?immutable=1;Mode=ReadOnly;...` when the `-shm` sidecar is missing or locked.
- Never write to or lock the database.

### Schema Queries

```sql
-- Aggregated local tokens across active or recent sessions
SELECT 
    COALESCE(SUM(tokens_input), 0) AS TotalInput,
    COALESCE(SUM(tokens_output), 0) AS TotalOutput,
    COALESCE(SUM(tokens_reasoning), 0) AS TotalReasoning,
    COALESCE(SUM(tokens_cache_read), 0) AS TotalCacheRead,
    COALESCE(SUM(cost), 0.0) AS TotalCost
FROM session
WHERE time_archived IS NULL;
```

---

## 5. Agent Activity & Process Liveness

To report real-time HUD status (`AgentSession.Busy`, `AgentSession.Idle`), TokenHound monitors process liveness:

### Monitored Process Names

- `opencode` (CLI executable or Node runtime).
- `OpenCode` (Desktop application executable).

### Detection Method

- Use `ProcessDiscovery.FindProcessByName("opencode")` / `"OpenCode"`.
- Validate process PID and `StartTimeUtc` with `ProcessLiveness.IsLive(pid, startTime)`.
- If an active process exists and `session.time_updated` was modified within 60 seconds, mark status as `AgentSessionState.Busy`. Otherwise, if the process is present, mark as `AgentSessionState.Idle`.

---

## 6. Architecture & Implementation Checklist

- [ ] `OpenCodeCredentialDiscovery`: Read-only JSON extraction from `auth.json`.
- [ ] `OpenCodeApiClient`: HTTP client targeting `https://opencode.ai/zen/go/v1/usage` with timeout and 429 deadline resilience.
- [ ] `OpenCodeUsageProvider : IUsageProvider`: Generates `Snapshot` with 3 `LimitWindow` records.
- [ ] `OpenCodeActivityMonitor : IActivityMonitor`: Checks process liveness and recent database activity.
- [ ] Registration in DI and `UsageStore`.
