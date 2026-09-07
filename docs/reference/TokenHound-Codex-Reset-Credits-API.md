# Using Codex App Server Reset Credits in TokenHound

## Purpose

This document describes how TokenHound can query and redeem ChatGPT/Codex **rate-limit reset credits** ("banked resets") using the Codex CLI's local **App Server** interface.

The recommended integration path is:

1. Start `codex app-server` as a child process.
2. Communicate with it over stdin/stdout using JSON-RPC.
3. Initialize the App Server session.
4. Call `account/rateLimits/read` to read current Codex rate limits and available reset credits.
5. When the user explicitly chooses to redeem a reset, call `account/rateLimitResetCredit/consume`.
6. Re-read rate limits after a successful redemption.

This approach is preferable to reading Codex authentication files or calling ChatGPT private backend endpoints directly. TokenHound can rely on the Codex CLI to use the currently authenticated ChatGPT account and manage its authentication lifecycle.

---

## 1. Prerequisites

TokenHound should require:

- A recent Codex CLI installation.
- The user to already be authenticated in Codex CLI with their ChatGPT account.
- `codex` to be available on `PATH`, or TokenHound to have a configured path to the executable.

Basic validation:

```powershell
codex --version
```

TokenHound should not need the user's ChatGPT access token, refresh token, cookies, or `auth.json`.

---

## 2. App Server transport

Start the local App Server:

```powershell
codex app-server
```

The process communicates over standard input and standard output.

TokenHound should:

- launch `codex app-server` as a child process;
- redirect stdin;
- redirect stdout;
- redirect stderr separately;
- keep the process alive while it needs account information;
- send one JSON-RPC message per line;
- read stdout line-by-line and dispatch responses by JSON-RPC `id`;
- treat unsolicited JSON-RPC notifications separately from request/response messages.

Do not parse human-readable CLI output for this integration.

### Recommended process lifetime

For a desktop application such as TokenHound, prefer keeping one App Server process alive instead of starting a new process for every refresh.

A reasonable lifecycle is:

```text
TokenHound starts
    |
    +-- Start codex app-server
    |
    +-- initialize
    +-- initialized
    |
    +-- account/rateLimits/read
    |
    +-- reuse the process for subsequent refreshes
    |
TokenHound exits
    |
    +-- close stdin / terminate child process
```

If the App Server exits unexpectedly, TokenHound can restart it and repeat the initialization handshake.

---

## 3. JSON-RPC initialization

The App Server must be initialized before normal requests are sent.

### Step 1: `initialize`

Example:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "clientInfo": {
      "name": "TokenHound",
      "version": "1.0.0"
    }
  }
}
```

TokenHound should wait for the response associated with request `id: 1`.

### Step 2: `initialized`

After receiving a successful `initialize` response, send:

```json
{
  "jsonrpc": "2.0",
  "method": "initialized"
}
```

This is a notification, so it does not require an `id`.

After this handshake, account-related requests can be sent.

### Request IDs

TokenHound should generate unique request IDs for concurrent or outstanding requests.

Integers are sufficient, although string IDs are also appropriate if the client abstraction supports them.

Example:

```text
1    initialize
2    account/rateLimits/read
3    account/rateLimitResetCredit/consume
4    account/rateLimits/read
```

---

## 4. Reading rate limits and banked resets

Use:

```text
account/rateLimits/read
```

A basic request can be sent without parameters:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "account/rateLimits/read"
}
```

Recent Codex versions also support request parameters used by the Codex TUI itself. TokenHound should remain compatible with older versions by being prepared to retry without parameters if a server returns JSON-RPC `-32600` or `-32602`.

The Codex TUI follows this compatibility strategy internally.

---

## 5. Relevant response model

The current protocol exposes a response conceptually equivalent to:

```typescript
type GetAccountRateLimitsResponse = {
  rateLimits: RateLimitSnapshot;
  rateLimitsByLimitId: Record<string, RateLimitSnapshot> | null;
  rateLimitResetCredits: RateLimitResetCreditsSummary | null;
};
```

The most important field for TokenHound is:

```text
rateLimitResetCredits
```

Its current structure is:

```typescript
type RateLimitResetCreditsSummary = {
  availableCount: number;
  credits: RateLimitResetCredit[] | null;
};
```

Each detailed reset credit currently has the following logical shape:

```typescript
type RateLimitResetCredit = {
  id: string;
  resetType: "codexRateLimits" | string;
  status: "available" | "redeeming" | "redeemed" | string;
  grantedAt: number;
  expiresAt: number | null;
  title: string | null;
  description: string | null;
};
```

`grantedAt` and `expiresAt` are Unix timestamps in seconds.

---

## 6. Example rate-limit response

A response may look similar to:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "rateLimits": {
      "limitId": "codex",
      "primary": {
        "usedPercent": 37,
        "windowDurationMins": 300,
        "resetsAt": 1788782400
      },
      "secondary": {
        "usedPercent": 61,
        "windowDurationMins": 10080,
        "resetsAt": 1789214400
      },
      "credits": null,
      "planType": "plus",
      "rateLimitReachedType": null
    },
    "rateLimitsByLimitId": {
      "codex": {
        "limitId": "codex",
        "primary": {
          "usedPercent": 37,
          "windowDurationMins": 300,
          "resetsAt": 1788782400
        },
        "secondary": {
          "usedPercent": 61,
          "windowDurationMins": 10080,
          "resetsAt": 1789214400
        },
        "credits": null,
        "planType": "plus",
        "rateLimitReachedType": null
      }
    },
    "rateLimitResetCredits": {
      "availableCount": 2,
      "credits": [
        {
          "id": "RateLimitResetCredit_abc123",
          "resetType": "codexRateLimits",
          "status": "available",
          "grantedAt": 1788264000,
          "expiresAt": 1790856000,
          "title": "Full reset (Weekly + 5 hr)",
          "description": "Ready to redeem"
        }
      ]
    }
  }
}
```

The exact values and optional fields depend on the account, plan, backend response, and Codex version.

---

## 7. `availableCount` is authoritative

TokenHound should use:

```text
rateLimitResetCredits.availableCount
```

as the authoritative number of reset credits currently available.

Do **not** calculate the available count from:

```text
rateLimitResetCredits.credits.length
```

The Codex protocol explicitly allows the backend to cap the number of detailed credit rows returned.

For example:

```json
{
  "rateLimitResetCredits": {
    "availableCount": 5,
    "credits": [
      {
        "id": "credit-1",
        "status": "available"
      },
      {
        "id": "credit-2",
        "status": "available"
      }
    ]
  }
}
```

means:

```text
Available resets: 5
Detailed rows returned: 2
```

not two available resets.

The semantic distinction for `credits` is:

- `null`: only the count is known; detailed credit rows were not provided.
- `[]`: details were fetched, but no available rows were returned.
- non-empty array: detailed rows are available, possibly capped by the backend.

---

## 8. Do not hard-code `primary` as 5 hours

TokenHound should not permanently assume:

```text
primary   = 5-hour limit
secondary = weekly limit
```

Instead, classify rate-limit windows using:

```text
windowDurationMins
```

Common Codex values currently include:

| Duration | Meaning |
|---:|---|
| `300` | 5 hours |
| `10080` | 7 days |
| other values | plan-specific or future rate-limit window |

Recommended mapping logic:

```csharp
static string GetWindowName(int minutes) => minutes switch
{
    300 => "5-hour",
    10080 => "weekly",
    _ => $"{minutes}-minute"
};
```

A more future-proof TokenHound data model should preserve the raw duration rather than reducing every response to only two fixed properties.

For example:

```csharp
public sealed record CodexRateLimitWindow(
    double UsedPercent,
    int WindowDurationMinutes,
    DateTimeOffset? ResetsAt);
```

---

## 9. Prefer the `codex` bucket when available

The modern response can contain:

```text
rateLimitsByLimitId
```

This supports multiple metered rate-limit buckets.

For Codex usage, TokenHound should prefer:

```text
rateLimitsByLimitId["codex"]
```

when present.

Otherwise, fall back to the legacy-compatible:

```text
rateLimits
```

Recommended logic:

```csharp
var snapshot =
    response.RateLimitsByLimitId?.GetValueOrDefault("codex")
    ?? response.RateLimits;
```

This keeps TokenHound compatible with both newer multi-bucket responses and older single-bucket behavior.

---

## 10. Redeeming a reset credit

Use:

```text
account/rateLimitResetCredit/consume
```

Request:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "account/rateLimitResetCredit/consume",
  "params": {
    "idempotencyKey": "a927514b-c3f3-4eef-878c-bbe558011983",
    "creditId": "RateLimitResetCredit_abc123"
  }
}
```

The request parameters are:

```typescript
type ConsumeAccountRateLimitResetCreditParams = {
  idempotencyKey: string;
  creditId?: string | null;
};
```

### `idempotencyKey`

This value identifies one logical redemption attempt.

A UUID/GUID is recommended:

```csharp
var idempotencyKey = Guid.NewGuid().ToString();
```

The same key must be reused when retrying the **same logical redemption attempt** after a timeout, transport interruption, or uncertain result.

Do not generate a new key merely because the first request timed out.

Otherwise, TokenHound could turn one user action into multiple independent redemption attempts.

### `creditId`

`creditId` is optional.

When supplied:

```json
{
  "idempotencyKey": "...",
  "creditId": "RateLimitResetCredit_abc123"
}
```

the specified reset credit is selected.

When omitted:

```json
{
  "idempotencyKey": "..."
}
```

the backend selects the next available reset credit.

For TokenHound, explicitly passing `creditId` is preferable when detailed rows are available because it makes the user's selection deterministic.

When only `availableCount` is available and no detailed IDs are returned, TokenHound may redeem without `creditId`.

---

## 11. Consume response

The current response is:

```typescript
type ConsumeAccountRateLimitResetCreditResponse = {
  outcome:
    | "reset"
    | "nothingToReset"
    | "noCredit"
    | "alreadyRedeemed";
};
```

Example success:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "outcome": "reset"
  }
}
```

### Outcome handling

#### `reset`

A reset credit was consumed and eligible rate-limit windows were reset.

TokenHound should immediately refresh:

```text
account/rateLimits/read
```

Do not modify the local percentages manually.

The backend response after the reset is the authoritative state.

#### `nothingToReset`

No current rate-limit window is eligible for reset.

TokenHound should:

1. not treat this as a transport failure;
2. show the outcome to the user;
3. refresh `account/rateLimits/read`.

#### `noCredit`

The account currently has no earned reset credits available.

TokenHound should refresh account rate limits because the locally cached reset count may be stale.

#### `alreadyRedeemed`

The supplied `idempotencyKey` already completed a successful reset.

This is an expected idempotency result, not necessarily an error.

TokenHound should refresh `account/rateLimits/read` and treat the logical operation as already completed.

---

## 12. Recommended redemption flow

Reset redemption should always be an explicit user action.

Recommended TokenHound workflow:

```text
User opens Codex usage
        |
        v
account/rateLimits/read
        |
        v
Display:
- current rate-limit windows
- reset times
- availableCount
- detailed reset credits, when available
        |
        v
User clicks "Use reset"
        |
        v
Confirmation dialog
        |
        v
Generate idempotencyKey
        |
        v
account/rateLimitResetCredit/consume
        |
        +---- reset ----------+
        |                     |
        +---- alreadyRedeemed-+
        |                     |
        +---- noCredit -------+
        |                     |
        +---- nothingToReset -+
                              |
                              v
                 account/rateLimits/read
                              |
                              v
                     Refresh TokenHound UI
```

TokenHound should never automatically consume a reset simply because a rate limit reaches a threshold.

The reset has account value and should require explicit confirmation.

---

## 13. Suggested TokenHound UI

A compact UI could show:

```text
Codex

5-hour
Usage:       82%
Resets:      11:42

Weekly
Usage:       94%
Resets:      Sep 10, 08:15

Banked resets
Available:   2

Full reset (Weekly + 5 hr)
Expires: Sep 18, 2026
[Use reset]
```

If:

```text
availableCount > credits.Count
```

TokenHound could show:

```text
2 additional reset credits are available.
```

without inventing expiry information for credits whose details were not returned.

---

## 14. Suggested C# domain models

TokenHound should isolate Codex protocol DTOs from its application/domain model.

Example domain model:

```csharp
public sealed record CodexUsage(
    string? PlanType,
    IReadOnlyList<CodexRateLimitWindow> Windows,
    CodexResetCredits ResetCredits);

public sealed record CodexRateLimitWindow(
    string? LimitId,
    double UsedPercent,
    int WindowDurationMinutes,
    DateTimeOffset? ResetsAt);

public sealed record CodexResetCredits(
    long AvailableCount,
    IReadOnlyList<CodexResetCredit> Credits);

public sealed record CodexResetCredit(
    string Id,
    string ResetType,
    string Status,
    DateTimeOffset GrantedAt,
    DateTimeOffset? ExpiresAt,
    string? Title,
    string? Description);
```

Protocol DTOs can mirror the JSON-RPC schema exactly, while these records provide a stable abstraction for the rest of TokenHound.

---

## 15. Unix timestamp conversion

Codex currently exposes timestamps such as:

```text
grantedAt
expiresAt
resetsAt
```

as Unix seconds.

Convert them with:

```csharp
DateTimeOffset.FromUnixTimeSeconds(timestamp);
```

For display, convert to the user's local timezone:

```csharp
var local = DateTimeOffset
    .FromUnixTimeSeconds(timestamp)
    .ToLocalTime();
```

Keep the original `DateTimeOffset` value internally.

---

## 16. JSON-RPC client architecture

A reusable TokenHound abstraction might look like:

```csharp
public interface ICodexAppServerClient : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task<CodexUsage> GetUsageAsync(
        CancellationToken cancellationToken = default);

    Task<CodexResetOutcome> ConsumeResetAsync(
        string? creditId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
```

Internally:

```text
CodexAppServerClient
    |
    +-- Process
    |     codex app-server
    |
    +-- stdin writer
    |
    +-- stdout reader task
    |
    +-- pending request map
    |     request ID -> TaskCompletionSource
    |
    +-- notification dispatcher
    |
    +-- stderr logger
```

A concurrent dictionary is appropriate for correlating outstanding requests:

```csharp
ConcurrentDictionary<long, TaskCompletionSource<JsonElement>>
```

The stdout reader should be the only component reading stdout. It can deserialize each line and route responses to the matching pending request.

---

## 17. Do not mix stderr with stdout

JSON-RPC data is received over stdout.

Keep stderr redirected separately:

```csharp
new ProcessStartInfo
{
    FileName = "codex",
    Arguments = "app-server",
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true
};
```

Do not merge stderr into stdout, because diagnostics could corrupt the JSON-RPC stream.

---

## 18. Cancellation versus idempotency

Cancellation requires special care during reset redemption.

Consider:

```text
TokenHound sends consume request
    |
    +-- backend consumes reset
    |
    +-- user closes dialog / cancellation fires
    |
    +-- TokenHound never receives response
```

TokenHound must not assume the reset failed.

For a redemption operation, persist the logical attempt's:

```text
idempotencyKey
creditId
```

until the result is known.

If the connection is lost after sending the request, restart/reconnect and retry the same operation using the **same idempotency key**.

This is precisely what the idempotency mechanism is designed to protect.

---

## 19. Post-redemption refresh

Regardless of the consume outcome, TokenHound should normally perform:

```text
account/rateLimits/read
```

after the operation.

In particular, after:

```text
reset
alreadyRedeemed
noCredit
nothingToReset
```

a re-read gives TokenHound the current authoritative account state.

Do not calculate expected post-reset values locally.

---

## 20. Polling and caching

Rate-limit state does not require aggressive polling.

A practical TokenHound strategy is:

- fetch when the Codex section becomes visible;
- refresh when the user explicitly requests it;
- refresh after consuming a reset;
- optionally refresh periodically while the UI is visible;
- cache the last successful snapshot for display during temporary App Server failures.

Avoid launching a new `codex app-server` process for every poll.

### Notifications

Recent App Server versions also expose rate-limit update notifications. These can be used later to reduce polling.

TokenHound should nevertheless keep `account/rateLimits/read` as the authoritative full snapshot operation because rolling notifications may be sparse and are intended to be merged into an existing snapshot or followed by a refetch.

---

## 21. Compatibility strategy

The Codex App Server protocol evolves with the Codex CLI.

TokenHound should therefore:

1. detect the installed Codex version;
2. avoid depending on undocumented ChatGPT backend URLs;
3. tolerate nullable optional properties;
4. ignore unknown JSON properties;
5. tolerate unknown enum values;
6. prefer `rateLimitsByLimitId["codex"]` when available;
7. fall back to `rateLimits`;
8. treat `availableCount` as authoritative;
9. tolerate `credits == null`;
10. retry `account/rateLimits/read` without newer params when the server reports invalid request/params.

For enums, avoid strict deserialization that fails when OpenAI adds a future value.

For example, a domain-layer string value is safer than assuming the protocol can never add another reset status.

---

## 22. Error handling

TokenHound should distinguish at least four failure categories.

### Codex CLI unavailable

Examples:

```text
codex executable not found
codex app-server cannot start
```

Suggested UI:

```text
Codex CLI is not available.
Install or configure Codex CLI to read ChatGPT/Codex usage.
```

### Authentication required

If the App Server indicates that the user is not authenticated, TokenHound should ask the user to authenticate using Codex CLI rather than attempting to obtain ChatGPT credentials itself.

### JSON-RPC error

Capture:

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "error": {
    "code": -32602,
    "message": "..."
  }
}
```

Do not treat JSON-RPC errors as malformed responses.

### Transport failure

Examples:

```text
child process exited
broken pipe
stdout closed
timeout
```

Restart the App Server if appropriate.

For a reset redemption whose result is uncertain, retry using the same idempotency key.

---

## 23. Security considerations

TokenHound should **not**:

- read or copy ChatGPT OAuth tokens;
- read browser cookies;
- send Codex credentials to TokenHound servers;
- call private ChatGPT `backend-api` endpoints;
- log authentication material;
- automatically redeem reset credits.

Using `codex app-server` keeps authentication responsibility inside Codex CLI.

TokenHound only needs to exchange local JSON-RPC messages with the Codex process.

---

## 24. Example minimal request sequence

The complete logical exchange is:

### Initialize

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"clientInfo":{"name":"TokenHound","version":"1.0.0"}}}
```

### Signal initialization completed

```json
{"jsonrpc":"2.0","method":"initialized"}
```

### Read usage and reset credits

```json
{"jsonrpc":"2.0","id":2,"method":"account/rateLimits/read"}
```

### Consume a specific reset

```json
{"jsonrpc":"2.0","id":3,"method":"account/rateLimitResetCredit/consume","params":{"idempotencyKey":"a927514b-c3f3-4eef-878c-bbe558011983","creditId":"RateLimitResetCredit_abc123"}}
```

### Refresh usage

```json
{"jsonrpc":"2.0","id":4,"method":"account/rateLimits/read"}
```

---

## 25. Example redemption without a detailed credit ID

If:

```json
{
  "rateLimitResetCredits": {
    "availableCount": 1,
    "credits": null
  }
}
```

TokenHound can still request redemption without `creditId`:

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "account/rateLimitResetCredit/consume",
  "params": {
    "idempotencyKey": "67d266df-ef41-47db-b61d-b26afac39ca4"
  }
}
```

The backend selects an available reset credit.

---

## 26. Recommended TokenHound service boundary

A useful layering is:

```text
TokenHound UI
    |
    v
ICodexUsageService
    |
    v
CodexUsageService
    |
    v
ICodexAppServerClient
    |
    v
CodexAppServerClient
    |
    v
codex app-server
```

This keeps App Server protocol details out of the UI and makes the integration testable.

For example:

```csharp
public interface ICodexUsageService
{
    Task<CodexUsage> GetUsageAsync(
        CancellationToken cancellationToken = default);

    Task<CodexResetResult> RedeemResetAsync(
        string? creditId,
        CancellationToken cancellationToken = default);
}
```

`CodexUsageService` can own the idempotency/retry rules while `CodexAppServerClient` remains a generic JSON-RPC transport.

---

## 27. Recommended persistence for redemption attempts

TokenHound does not need to persist every read operation.

For reset redemption, however, persisting an in-flight logical operation can improve correctness.

Example:

```json
{
  "creditId": "RateLimitResetCredit_abc123",
  "idempotencyKey": "a927514b-c3f3-4eef-878c-bbe558011983",
  "createdAt": "2026-09-07T13:00:00Z"
}
```

Clear this state once TokenHound knows the logical operation completed.

This protects against:

- TokenHound crashes;
- Codex App Server crashes;
- machine shutdown;
- connection loss immediately after the backend accepted the reset.

After restart, TokenHound can reconcile account state and, if necessary, retry with the same idempotency key.

---

## 28. What TokenHound should not rely on

Avoid coupling TokenHound to:

```text
https://chatgpt.com/backend-api/...
```

even though the open-source Codex implementation ultimately talks to ChatGPT backend services.

Those backend routes are implementation details of Codex and can change independently.

TokenHound's integration boundary should be:

```text
codex app-server
```

This also avoids duplicating authentication and token-refresh behavior.

---

## 29. Implementation checklist

### App Server client

- [ ] Locate `codex`.
- [ ] Start `codex app-server`.
- [ ] Redirect stdin/stdout/stderr.
- [ ] Implement JSON-RPC request correlation.
- [ ] Send `initialize`.
- [ ] Send `initialized`.
- [ ] Handle App Server restart.
- [ ] Keep stderr separate from JSON-RPC stdout.

### Usage

- [ ] Call `account/rateLimits/read`.
- [ ] Prefer `rateLimitsByLimitId["codex"]`.
- [ ] Fall back to `rateLimits`.
- [ ] Classify windows by `windowDurationMins`.
- [ ] Convert Unix timestamps correctly.
- [ ] Use `availableCount` as the authoritative reset count.
- [ ] Handle `credits == null`.
- [ ] Do not assume `credits.Count == availableCount`.

### Redemption

- [ ] Require explicit user action.
- [ ] Show a confirmation.
- [ ] Generate one idempotency key per logical attempt.
- [ ] Reuse that key on retries.
- [ ] Pass `creditId` when available.
- [ ] Support redemption without `creditId`.
- [ ] Handle all current outcomes.
- [ ] Re-read rate limits after redemption.

### Compatibility

- [ ] Ignore unknown JSON fields.
- [ ] Handle unknown enum values.
- [ ] Handle missing optional fields.
- [ ] Support older Codex App Server versions.
- [ ] Do not call private ChatGPT backend APIs directly.

---

## 30. References

The implementation details in this document were validated against the current OpenAI Codex open-source repository and generated App Server protocol definitions.

### Codex repository

https://github.com/openai/codex

### App Server

https://github.com/openai/codex/tree/main/codex-rs/app-server

### App Server protocol: account types

https://github.com/openai/codex/blob/main/codex-rs/app-server-protocol/src/protocol/v2/account.rs

This source defines:

- `GetAccountRateLimitsResponse`
- `RateLimitResetCreditsSummary`
- `RateLimitResetCredit`
- `ConsumeAccountRateLimitResetCreditParams`
- `ConsumeAccountRateLimitResetCreditResponse`
- `ConsumeAccountRateLimitResetCreditOutcome`

### Generated TypeScript: rate-limit response

https://github.com/openai/codex/blob/main/codex-rs/app-server-protocol/schema/typescript/v2/GetAccountRateLimitsResponse.ts

### Generated TypeScript: consume parameters

https://github.com/openai/codex/blob/main/codex-rs/app-server-protocol/schema/typescript/v2/ConsumeAccountRateLimitResetCreditParams.ts

### App Server test helper

https://github.com/openai/codex/blob/main/codex-rs/app-server/tests/common/test_app_server.rs

The Codex test suite sends:

```text
account/rateLimits/read
account/rateLimitResetCredit/consume
```

through the App Server protocol.

### Codex TUI implementation

https://github.com/openai/codex/blob/main/codex-rs/tui/src/app/background_requests.rs

The TUI implementation is particularly useful as a reference for:

- reading rate limits;
- requesting reset-credit details;
- backward compatibility with older App Server versions;
- consuming a reset;
- refreshing state after redemption.

---

## 31. Versioning note

This document reflects the Codex App Server protocol available in the OpenAI Codex repository as of **September 7, 2026**.

TokenHound should treat the App Server as a versioned/evolving local API and keep protocol-specific code isolated behind an adapter so future Codex changes can be accommodated without affecting the rest of the application.
