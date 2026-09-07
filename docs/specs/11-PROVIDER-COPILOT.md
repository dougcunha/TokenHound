# Provider Technical Specification: GitHub Copilot (Lightweight Internal Quota)

This specification defines the lightweight integration with **GitHub Copilot** on **Windows 11**, covering borrowed OAuth credential discovery, the undocumented internal quota endpoint queried by official clients, defensive parsing of open quota snapshots, and heuristic agent activity monitoring.

> **SCOPE NOTE**: This document covers the lightweight HTTP path only (`copilot_internal/user` via a borrowed `gh` / Copilot CLI OAuth token). The official Copilot SDK path (`account.getQuota` RPC, `session.usage.getMetrics`) is a separate, heavier runtime dependency and is explicitly out of scope here.

---

## 1. Overview and Data Fidelity

- **Provider ID**: `copilot`.
- **Display Name**: `Copilot`.
- **Fidelity**: `.official` (authoritative billing total shown on the GitHub billing dashboard, served over an undocumented transport).
- **Headline Metric**: Monthly Premium Interactions (`premium_interactions`).

---

## 2. Credential Location & Extraction on Windows 11

TokenHound never creates its own GitHub login. It borrows the OAuth token that `gh`, Copilot CLI, or VS Code already stored.

### Discovery Precedence

Sources are checked in strict order; the first usable token wins:

1. `COPILOT_GITHUB_TOKEN` environment variable.
2. `GH_TOKEN` environment variable.
3. `GITHUB_TOKEN` environment variable.
4. `gh auth token` (GitHub CLI session in the OS credential store).
5. Copilot CLI keychain entry, with plaintext fallback at `%USERPROFILE%\.copilot\config.json` (override via `COPILOT_HOME`).
6. VS Code extension state at `%APPDATA%\Code\User\globalStorage\github.copilot` (identity hint only).

### C# Discovery Sketch

```csharp
public static string? DiscoverGitHubToken()
{
    foreach (var name in new[] { "COPILOT_GITHUB_TOKEN", "GH_TOKEN", "GITHUB_TOKEN" })
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();
    }

    return TryReadGhCliToken() ?? TryReadCopilotConfigFile();
}
```

> **ALLOWLIST TRAP**: `copilot_internal/*` accepts only tokens minted by GitHub's allowlisted OAuth Apps (e.g. `gho_*` from `gh auth login` / `copilot login`). Classic or fine-grained PATs are rejected with HTTP 403 even with broad scopes. A 403 here means "wrong token kind", not "revoked subscription".

### Token Handling Rules

- **No-Refresh Invariant**: TokenHound **must never** refresh, re-mint, or overwrite the borrowed token. If the call returns 401, report `NeedsAuth` (discarding history) and guide the developer to run `gh auth login` or `copilot login` in their terminal.
- **Read-Only Borrowing**: Config and state files are opened with `FileShare.ReadWrite | FileShare.Delete`; credential-store reads never write back.
- **Env Override Warning**: An unrelated `GH_TOKEN`/`GITHUB_TOKEN` set for another tool silently overrides the stored OAuth token. Treat persistent 403s as a hint to check the environment.

---

## 3. Telemetry Endpoint & Query Protocol

### HTTP Request

- **Method**: `GET`
- **URL**: `https://api.github.com/copilot_internal/user`
- **Headers**:
  ```http
  Authorization: Bearer <githubOAuthToken>
  Accept: application/json
  ```
- **Recommended Timeout**: 15 seconds.

### JSON Response Schema

```json
{
  "login": "octocat",
  "copilot_plan": "business",
  "access_type_sku": "copilot_standalone_seat_quota",
  "token_based_billing": true,
  "quota_reset_date": "2026-07-01",
  "quota_reset_date_utc": "2026-07-01T00:00:00.000Z",
  "endpoints": {
    "api": "https://api.business.githubcopilot.com"
  },
  "quota_snapshots": {
    "chat": {
      "quota_id": "chat",
      "unlimited": true,
      "has_quota": false,
      "entitlement": 0,
      "remaining": 0,
      "quota_remaining": 0.0,
      "percent_remaining": 100.0,
      "overage_permitted": false,
      "timestamp_utc": "2026-06-02T13:41:20.798Z"
    },
    "completions": {
      "quota_id": "completions",
      "unlimited": true,
      "has_quota": false,
      "entitlement": 0,
      "remaining": 0,
      "quota_remaining": 0.0,
      "percent_remaining": 100.0,
      "overage_permitted": false,
      "timestamp_utc": "2026-06-02T13:41:20.798Z"
    },
    "premium_interactions": {
      "quota_id": "premium_interactions",
      "unlimited": false,
      "has_quota": true,
      "entitlement": 3000,
      "remaining": 252,
      "quota_remaining": 252.6,
      "percent_remaining": 8.4,
      "overage_count": 0,
      "overage_permitted": true,
      "timestamp_utc": "2026-06-02T13:41:20.798Z"
    }
  }
}
```

### Field Reference

| Field | Description |
| :--- | :--- |
| `copilot_plan` / `access_type_sku` | Subscription tier and billing SKU (plan context for `entitlement`). |
| `token_based_billing` | When `true`, quota is metered as AI Credits / premium interactions, not legacy request counts. Never hard-code credit math. |
| `quota_reset_date_utc` | ISO-8601 UTC reset instant for the billing period. Prefer over per-snapshot `quota_reset_at` (observed as `0`). |
| `quota_snapshots` | Open map keyed by category (`chat`, `completions`, `premium_interactions`; new keys may appear). |
| `entitlement` / `remaining` | Integer allocation and integer remainder for the period. |
| `quota_remaining` | Float remainder (fractional interactions); prefer over `remaining` for display. |
| `percent_remaining` | Pre-computed `(quota_remaining / entitlement) * 100`. |
| `has_quota` / `unlimited` | `true` / `false` guards for finite vs. uncapped categories. |
| `overage_permitted` / `overage_count` | Whether usage continues past exhaustion and how much overage was consumed. |

---

## 4. Parsing & Modeling Rules

```csharp
public static LimitWindow? ParseFiniteQuota(QuotaSnapshots snapshots, string resetDateUtc)
{
    var finite = snapshots.PremiumInteractions?.HasQuota == true
        ? snapshots.PremiumInteractions
        : snapshots.Values.FirstOrDefault(s => s.HasQuota);

    if (finite is null)
        return null;

    return new LimitWindow(
        Id: finite.QuotaId,
        Label: "Monthly interactions",
        UsedFraction: 1.0 - (finite.PercentRemaining / 100.0),
        ResetsAt: DateTimeOffset.Parse(resetDateUtc, null, DateTimeStyles.RoundtripKind)
    );
}
```

1. **Prefer `premium_interactions`, fall back to any snapshot with `has_quota == true`**. Never hard-code the category key; the schema is open and GitHub may rename it.
2. **Honest fractions**: `usedFraction = 1.0 - (percent_remaining / 100.0)`; display remainder from `quota_remaining` (float). `entitlement - remaining` is the integer cross-check.
3. **Zero Fake Data**: Categories with `unlimited == true` or `has_quota == false` (commonly `chat`, `completions` with `entitlement == 0`) **must be omitted**, never rendered as `0%`. If no finite snapshot exists, the provider reports `Unsupported` with an empty ring.
4. **Reset date**: Parse top-level `quota_reset_date_utc` (ISO-8601). Ignore per-snapshot `quota_reset_at` when `0`.
5. **Overage**: If `remaining <= 0` and `overage_permitted == true`, emit a `UsageBlock` noting metered overage continues instead of a hard block. If overage is not permitted and quota is exhausted, emit a blocking `UsageBlock` until `ResetsAt`.
6. **Schema drift**: This endpoint is undocumented and unversioned. Any missing `quota_snapshots` map, moved key, or non-200 response degrades to `Stale` with the archived reading retained, never to fabricated zeros.

---

## 5. Live Agent Activity Monitoring on Windows 11

Copilot exposes no PID-scoped `busy` flag for standalone polling. Activity is inferred heuristically from local CLI session writes plus process liveness.

1. The monitor polls `LastWriteTimeUtc` every 2 seconds over `%USERPROFILE%\.copilot\session-state\*\events.jsonl` and `%USERPROFILE%\.copilot\logs\` (read-only, `FileShare.ReadWrite | FileShare.Delete`).
2. If any session file was written within the last **30 seconds** (`staleAfter = 30`) **and** a Copilot host process (`copilot`, `gh`, or `Code.exe` with the Copilot extension) is alive per `ProcessLiveness`, the assistant is classified as **Busy**.
3. After 30 seconds without writes, or when no host process is alive, the state returns to **Idle**, avoiding ghost indicators from abrupt kills.
4. A `FileSystemWatcher` over the session-state directory (120ms debounce) consolidates burst writes between polls, matching the Claude session-monitor pattern.

---

## 6. Status, Backoff, and Sign-In Route

| Condition | Resulting Status | History |
| :--- | :--- | :--- |
| HTTP 401, revoked/expired OAuth token | `NeedsAuth` | Discard, show `—` |
| HTTP 403 with PAT-shaped token | `NeedsAuth` ("use `gh auth login`, PATs are rejected") | Discard |
| HTTP 403 unlicensed / no Copilot seat | `Unsupported` | Discard |
| HTTP 429 | `Stale` + persistent deadline per spec 01 (`Retry-After: 0` acts only as floor-raiser) | Retain |
| Network timeout / schema drift | `Stale` | Retain |
| Success | `Ok` (persist to `UsageArchive`) | Update |

- **Route**: `SignInRoute.Guidance("Run 'gh auth login' or 'copilot login' in your terminal, then retry. Do not paste a PAT.")`.
- **Polling**: Standard cadence from spec 01 applies (`ActiveInterval` 180s when any monitor reports `Busy`, else `IdleInterval` 300s). A monthly quota needs no faster cadence; forced refreshes still respect active 429 deadlines.
