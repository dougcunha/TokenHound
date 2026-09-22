# MCP access to provider metrics

While TokenHound is running, it serves the provider metrics behind the HUD to local MCP clients. The server reads TokenHound's in-memory state; a call never triggers a provider refresh, never changes the polling schedule, and never touches rate-limit deadlines.

## Endpoint

| Transport | URL |
| --- | --- |
| Legacy SSE | `http://127.0.0.1:37653/mcp/sse` (the SDK announces `/mcp/message` for requests) |
| Streamable HTTP | `http://127.0.0.1:37653/mcp` |

- The server starts with the application and stops when you exit from the tray. Active SSE connections close at exit, and the port is released.
- It listens on IPv4 loopback only. Requests must use the host `127.0.0.1:37653`; a request whose `Origin` header is anything other than `http://127.0.0.1:37653` is rejected with 403. Clients that send no `Origin`, which covers most MCP clients, are accepted. CORS is not enabled.
- Local safeguards: 60 requests per minute (excess requests get HTTP 429 immediately) and at most 16 concurrent connections. These limits are unrelated to provider quotas.
- If port 37653 is already in use, the HUD keeps working and the log records `MCP endpoint ... failed to start`. MCP is unavailable until the next start.

## Connecting a client

Claude Code:

```powershell
claude mcp add --transport sse tokenhound http://127.0.0.1:37653/mcp/sse
```

Clients configured with JSON:

```json
{
  "mcpServers": {
    "tokenhound": {
      "type": "sse",
      "url": "http://127.0.0.1:37653/mcp/sse"
    }
  }
}
```

A client that supports Streamable HTTP can use `http://127.0.0.1:37653/mcp` instead.

## Tools

Both tools are read-only, idempotent, and return structured JSON.

| Tool | Arguments | Result |
| --- | --- | --- |
| `list_provider_metrics` | none | `{ observedAtUtc, providers: [...] }`: every registered, enabled, real provider that has a reading, sorted by `providerId`. The array is empty before the first readings arrive. |
| `get_provider_metrics` | `providerId` (string, case-insensitive) | `{ lookupState, observedAtUtc, provider }`. A blank ID is a tool error. |

`lookupState` values:

| Value | Meaning | `provider` |
| --- | --- | --- |
| `available` | The provider is enabled and has a reading. | metrics |
| `pending` | The provider is enabled but has no reading yet. | `null` |
| `disabled` | The provider is turned off in Settings. | `null` |
| `synthetic` | The ID is the HUD's `mock` demonstration data, never real telemetry. | `null` |
| `unknown` | No provider with that ID is registered. | `null` |

## Provider fields

| Field | Meaning |
| --- | --- |
| `providerId` | Stable ID, such as `claude`, `codex`, `copilot`, `cursor`, `gemini`, `opencode`, or `cline`. |
| `status` | `ok`, `stale`, `needsAuth`, `accessDenied`, `rateLimited`, `unsupported`, or `notRunning`. Only `ok` is a current successful reading. |
| `fidelity` | `official` (reported by the provider), `derived` (computed from indirect data), or `manual`. |
| `snapshotFetchedAtUtc` | When the current status-bearing reading was produced. |
| `lastSuccessfulAtUtc` | When the last successful reading was taken, or `null` if none is known. A `stale` provider can still show windows retained from this time. |
| `limitWindows[]` | `name`, `groupName`, `usedFraction`, `usedUnits`, `remainingUnits`, `remainingValue`, `totalUnits`, `resetTimeUtc`, `periodSeconds`. |
| `activeBlock` | `isBlocked`, `resetTimeUtc`, `retryAfterSeconds`, when the provider reports a block. |
| `copilotBilling` | Copilot only: billing `state`, `reason`, `attemptedAtUtc`, `nextRequestAtUtc`, and `usage` credit amounts, period, coverage, and `isEstimated`. |
| `clineAccount` | Cline only: `balanceCredits`, `planName`, `hasPassSubscription`. |
| `clineLocal` | Cline only: locally aggregated token counts, `modelCalls`, `windowStartUtc`, `lastActivityUtc`. |

Values TokenHound does not know stay `null` or are left out. In particular, `usedFraction` is `null` whenever the provider reports only a remaining or used count: TokenHound never invents a limit or percentage. `usedUnits` is a count already consumed in the window (for example, `gemini`'s requests today, which has no published limit); it is never a remaining count, and such windows leave `remainingUnits` out. `observedAtUtc` is when the server read its state, not when the provider was queried.

## Privacy

Responses carry usage and operational data only. They never include access tokens, cookies, credential paths, raw provider responses, free-form error text, account or owner identifiers, or session content. Any process running under your Windows account can reach the loopback endpoint; the server has no authentication of its own.
