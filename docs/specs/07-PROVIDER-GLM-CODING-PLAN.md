# Provider Technical Specification: Z.ai GLM Coding Plan

This specification defines the technical integration with **GLM Coding Plan (Z.ai / Zhipu AI)** on **Windows 11**, detailing local API key discovery across installed developer tools, console cluster routing (Global vs. China), and telemetry quota parsing.

---

## 1. Overview and Data Fidelity

- **Provider ID**: `glm`.
- **Display Name**: `GLM`.
- **Fidelity**: `.official`.
- **Headline Metric**: 5-Hour Session Cycle (`session`).

---

## 2. Local Key Discovery on Windows 11

Because Z.ai does not offer a standalone desktop client for its Coding Plan, the API key is discovered from other AI coding tools installed on the user's system.

Sources are scanned in strict order of precedence:

### A. Claude Code (`%USERPROFILE%\.claude\settings.json`)
Claude Code allows configuring custom backend providers via environment variables in its settings file:

```json
{
  "env": {
    "ANTHROPIC_AUTH_TOKEN": "xxxxxxxxxxxxxxxx.yyyyyyyyyyyyyyyy",
    "ANTHROPIC_BASE_URL": "https://api.z.ai/api/anthropic"
  }
}
```

> **MANDATORY FILTER**: Only accept the key if `ANTHROPIC_BASE_URL` contains a recognized Z.ai domain (`api.z.ai` or `bigmodel.cn`). If the URL points to `api.anthropic.com`, the key belongs to Anthropic and must be ignored by the GLM provider.

### B. ZCode (`%USERPROFILE%\.zcode\v2\config.json`)
In the ZCode editor, users configure `coding-plan` providers:

```json
{
  "provider": {
    "builtin:glm-coding-plan": {
      "enabled": true,
      "options": {
        "apiKey": "xxxxxxxxxxxxxxxx.yyyyyyyyyyyyyyyy",
        "baseURL": "https://api.z.ai/api/anthropic"
      }
    }
  }
}
```

- Entries with `"enabled": false` must be skipped.
- Tokens in `%USERPROFILE%\.zcode\v2\credentials.json` with prefix `enc:v1:` represent proprietary encrypted data and must be ignored.

### C. OpenCode (`%LOCALAPPDATA%\opencode\auth.json` or `%APPDATA%\opencode\auth.json`)
Search for keys under identifiers:
`"zai-coding-plan"`, `"zai"`, `"z.ai"`, `"zhipu"`, `"zhipuai"`.

---

## 3. Routing Between Global and China Consoles

Z.ai operates two isolated console backends:
1. **Global Console**: `https://api.z.ai`
2. **China Console (Zhipu)**: `https://open.bigmodel.cn`

> **ROUTING RULE**: The client must inspect the key's origin domain:
> - If host ends with `bigmodel.cn` or authentication ID starts with `zhipu`: Base URL = `https://open.bigmodel.cn`.
> - Otherwise: Base URL = `https://api.z.ai`.
>
> *(Note: Sending a China credential to `api.z.ai` returns HTTP 401, giving the false impression that the account was revoked).*

---

## 4. Telemetry Endpoint & Query Protocol

### HTTP Request

- **Method**: `GET`
- **URL**: `<BaseURL>/api/monitor/usage/quota/limit`
- **Mandatory Headers**:
  ```http
  Authorization: <token>
  Content-Type: application/json
  Accept: application/json
  ```
  > **CRITICAL AUTHENTICATION HEADER**:
  > The token must be sent **directly without the "Bearer " prefix**.
  > Sending `Authorization: Bearer <token>` fails with HTTP 401.

### HTTP 200 Error Envelope Trap

> **PECULIAR Z.AI API BEHAVIOR**:
> Authentication errors, expired plans, or rate limits frequently return HTTP network status **200 OK**, but with an error code encapsulated inside the JSON body:
> ```json
> { "code": 401, "success": false, "msg": "Invalid token" }
> ```
> C# must inspect the `code` and `success` fields of the payload before extracting quotas. If `code == 401` or `403`, throw `NeedsAuthException`. If `code == 429`, trigger backoff logic.

### Successful Response Schema (JSON)

```json
{
  "code": 200,
  "success": true,
  "data": {
    "level": "pro",
    "limits": [
      {
        "type": "TOKENS_LIMIT",
        "unit": 3,
        "number": 5,
        "percentage": 12.5,
        "currentValue": 1250000,
        "usage": 12000000,
        "nextResetTime": 1788682200000
      },
      {
        "type": "TOKENS_LIMIT",
        "unit": 6,
        "number": 1,
        "percentage": 8.1,
        "nextResetTime": 1789190400000
      },
      {
        "type": "TIME_LIMIT",
        "percentage": 4.0,
        "currentValue": 40,
        "usage": 1000
      }
    ]
  }
}
```

### Decoding Limit Windows by `(unit, number)` Tuples

Z.ai does not label window durations with simple strings; it uses unit/number tuples:

| `(unit, number)` Tuple | Type | Corresponding Window | Generated ID | UI Label |
| :--- | :--- | :--- | :--- | :--- |
| `unit: 3, number: 5` | `TOKENS_LIMIT` or `CREDIT_LIMIT` | 5-hour rolling token session | `session` | `"Current session"` |
| `unit: 6, number: 1` | `TOKENS_LIMIT` or `CREDIT_LIMIT` | Weekly limit (1 week) | `weekly` | `"Weekly"` |
| None | `TIME_LIMIT` | Monthly MCP call budget | `mcp` | `"MCP (1 month)"` |

### Parser Business Rules
1. The percentage returned in `percentage` (e.g. `12.5`) is divided by 100 to yield `usedFraction = 0.125`.
2. `nextResetTime`: Unix epoch timestamp in **milliseconds** UTC.
3. The `TIME_LIMIT` (MCP) window intentionally lacks `nextResetTime`. Domain models support `resetsAt == null` without discarding the entry.
4. **Display Ordering**: Session (`session`) first, followed by weekly (`weekly`), and lastly MCP (`mcp`).
