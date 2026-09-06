# Provider Technical Specification: OpenAI Codex (ChatGPT Desktop / CLI)

This specification defines the integration with the **OpenAI Codex** ecosystem on **Windows 11**, covering ChatGPT Desktop for Windows and the Codex CLI through a cascaded two-source architecture (App Server via stdio JSON-RPC and local rollout log parsing).

---

## 1. Overview and Data Fidelity

- **Provider ID**: `codex`.
- **Display Name**: `Codex`.
- **Fidelity**: `.official`.
- **Headline Metric**: Primary Window (`primary` - typically the 5-hour rolling session).

---

## 2. Two-Stage Fallback Architecture

```
           +---------------------------------------------+
           | Fetch Snapshot Request (FetchSnapshot)      |
           +---------------------------------------------+
                                  |
                                  v
                [Binary 'codex.exe' Available?]
                   /                             \
                (Yes)                            (No)
                 /                                 \
                v                                   v
    +------------------------+          +-------------------------+
    | Source 1: App Server   |          | Source 2: Local Parsing |
    | stdio JSON-RPC 2.0     |          | Rollout Logs (JSONL)    |
    +------------------------+          +-------------------------+
                |                                   |
       [Responded with Quota?]                      |
         /             \                            |
      (Yes)            (No)                         |
       /                 \                          |
      v                   +----------> + <----------+
   [Success]                           |
                                       v
                        [state_5.sqlite -> rollout tail]
                                       |
                                       v
                                 [Success / Stale]
```

---

## 3. Primary Source: `codex app-server` Bridge (JSON-RPC 2.0 via Stdio)

When ChatGPT Desktop or the CLI are installed on Windows 11, the local `codex.exe` binary exposes an internal JSON-RPC server that reports current account quotas.

### Executable Locations on Windows 11
1. `%LOCALAPPDATA%\Programs\ChatGPT\resources\codex.exe`
2. `%PROGRAMFILES%\ChatGPT\resources\codex.exe`
3. `%USERPROFILE%\.codex\bin\codex.exe`
4. Resolution via Windows `PATH` (`where.exe codex`).

### Stdio Communication Protocol
The process launches with argument `app-server`. Three messages are written sequentially to `StandardInput`:

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"clientInfo":{"name":"TokenHound","title":"TokenHound","version":"1.0"}}}
{"jsonrpc":"2.0","method":"initialized","params":{}}
{"jsonrpc":"2.0","id":2,"method":"account/rateLimits/read","params":null}
```

### Server Response (`id == 2`)

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "rateLimits": {
      "planType": "plus",
      "rateLimitReachedType": null,
      "primary": {
        "usedPercent": 78.0,
        "windowDurationMins": 300,
        "resetsAt": 1788659888
      },
      "secondary": {
        "usedPercent": 12.0,
        "windowDurationMins": 10080,
        "resetsAt": 1789246688
      }
    }
  }
}
```

### Parsing Rules
- `resetsAt`: Absolute Unix epoch timestamp in **seconds**.
- `windowDurationMins: 300` $\rightarrow$ 5-hour window.
- `windowDurationMins: 10080` $\rightarrow$ Weekly window (7 days).
- `rateLimitReachedType`: If non-null (e.g. `"rate_limit_reached"`, `"workspace_owner_credits_depleted"`), flags an active rate limit. Produces a `UsageBlock` warning that the assistant is blocked even if quota percentage remains.
- **Timeout Watchdog**: Enforce a strict 10-second timeout to kill the child process if it hangs or fails to respond.

---

## 4. Secondary Source (Fallback): Local Rollout Log Parsing

If the executable is missing or the app-server call fails, quotas are extracted from historical run records saved to disk on Windows.

### Locating Active Thread Index
- **SQLite Database**: `%USERPROFILE%\.codex\state_5.sqlite`
- **SQL Query**:
  ```sql
  SELECT rollout_path FROM threads WHERE archived = 0 ORDER BY updated_at_ms DESC LIMIT 8;
  ```

### Rollout Log Tail Reading
The `rollout_path` points to a JSONL file on Windows (e.g. `%USERPROFILE%\.codex\sessions\2026\09\05\rollout-*.jsonl`).
Because these files accumulate long chat transcripts, they can reach tens of megabytes.
- **Optimization**: Read only the final **256 KB** of the file using `FileStream.Seek(Length - 262144, SeekOrigin.Begin)`.
- Split lines backwards searching for `"rate_limits"`.

### Log Event Schema

```json
{
  "timestamp": "2026-09-05T21:52:17.646Z",
  "type": "event_msg",
  "payload": {
    "type": "token_count",
    "rate_limits": {
      "limit_id": "codex",
      "plan_type": "plus",
      "primary": {
        "used_percent": 78.0,
        "window_minutes": 300,
        "resets_at": 1788659888
      },
      "secondary": {
        "used_percent": 12.0,
        "window_minutes": 10080,
        "resets_at": 1789246688
      },
      "credits": {
        "has_credits": false,
        "unlimited": false
      }
    }
  }
}
```

### Staleness Rules for File-Based Readings
Unlike live web APIs, file readings reflect the moment of the last executed turn:
- If `DateTime.UtcNow - recordedAt <= 5 minutes`, status = `Ok`.
- If older than 5 minutes, status = `Stale(recordedAt)`.

---

## 5. Account Identity Extraction

To label account identity and subscription plan without hitting network endpoints:
- File: `%USERPROFILE%\.codex\auth.json`
- Schema:
  ```json
  {
    "tokens": {
      "id_token": "eyJhbGciOiJSUzI1NiIs..."
    }
  }
  ```
- `id_token` is a standard JWT. The Base64Url-decoded payload contains:
  - `email`: User's OpenAI email address.
  - `claims["https://api.openai.com/auth"]["chatgpt_plan_type"]`: Subscription plan (e.g. `"plus"`, `"team"`, `"pro"`).

---

## 6. Live Agent Activity Monitoring on Windows 11

Because Codex does not expose an explicit live boolean session state flag:
1. The monitor polls `LastWriteTimeUtc` every 2 seconds for the active rollout file and the desktop database (`%USERPROFILE%\.codex\sqlite\codex-dev.db`).
2. If the file was written within the last **8 seconds**, the assistant is classified as **Busy**.
3. After 8 seconds with no further writes, the active animation stops immediately, avoiding ghost activity indicators.
