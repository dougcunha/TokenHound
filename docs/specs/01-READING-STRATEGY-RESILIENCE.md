# Reading Strategy, Lifecycle, and Resilience

This document defines the technical specification for polling scheduling algorithms, error handling policies, retry strategies, rate limiting handling (HTTP 429), and state persistence for the Windows 11 application.

---

## 1. Adaptive Polling Schedule Model

The system must not issue constant network requests at short fixed intervals. If the developer is not actively generating code or running agents, token consumption does not change. Aggressive polling during periods of inactivity only wastes the provider's rate-limit budget.

### Timing Parameters

| Parameter | Default Value | Technical Rationale |
| :--- | :--- | :--- |
| `ActiveInterval` | **180 seconds** | Refresh interval when at least one local assistant is busy. Provider quota endpoints report coarse windows (5h / 7d) and throttle aggressively at minute-level polling, so a faster cadence buys no accuracy and costs rate-limit budget. Overridable via the `Refresh` section of `appsettings.json` (minimum 30 seconds). |
| `IdleInterval` | **300 seconds (5 minutes)** | Refresh interval when no assistant is executing tasks. Overridable via the `Refresh` section of `appsettings.json` (minimum 30 seconds). |
| `StaleThreshold` | **900 seconds (15 minutes)** | Grace margin before a prior reading is considered stale. |
| `LivenessPollInterval` | **2 to 5 seconds** | Lightweight local check (zero network) for process states and session files. |

### Tick Decision Logic (`ShouldRefresh`)

On each timer cycle (configured to tick every `ActiveInterval` = 180s), the decision to dispatch network requests to providers follows this pure rule:

$$\text{ExecuteRefresh} = \text{IsAnyAgentBusy}() \lor (\text{TimeSinceLastAttempt} \ge \text{IdleInterval})$$

```csharp
public static bool ShouldRefresh(bool isBusy, TimeSpan timeSinceLastAttempt, TimeSpan idleInterval)
{
    return isBusy || timeSinceLastAttempt >= idleInterval;
}
```

### Global Activity Detection (`IsAnyAgentBusy`)

A centralized delegate evaluates the aggregated state of all registered activity monitors (`ClaudeSessionMonitor`, `CursorActivityMonitor`, `CodexActivityMonitor`, `AntigravityActivityMonitor`). If any session reports `Busy` or `Waiting`, the system operates on the active 180s cadence.

---

## 2. Reactivity to Operating System Events

Beyond scheduled timers, specific Windows events trigger immediate refreshes:

### A. System Resume from Sleep / Hibernate
When suspending a laptop or desktop on Windows 11, clocks and timers pause. Upon wake-up, displayed metrics are almost certainly out of date or rolling window resets (e.g., Claude's 5-hour cycle) have already occurred.

- **.NET Implementation**: Subscribe to `Microsoft.Win32.SystemEvents.PowerModeChanged`:
  ```csharp
  SystemEvents.PowerModeChanged += (sender, e) =>
  {
      if (e.Mode == PowerModes.Resume)
      {
          UsageStore.RefreshNow();
      }
  };
  ```

### B. Manual Forced Refresh (*Refresh Now*)
The user can trigger an immediate update via the tray context menu or hotkey. This forced call must:
1. Bypass idle timer constraints.
2. Avoid re-triggering if an identical request is already in-flight.
3. Strictly respect active HTTP 429 penalties (see Section 4).

### C. Isolated Provider Refresh (`RefreshProvider`)
Allows triggering a refresh for a single provider without expending rate-limit budgets of other configured providers.

---

## 3. State Machine and Graceful Degradation

The system must never display fabricated percentages or invented numbers. When network errors, token expirations, or service disruptions occur, the interface must degrade gracefully and honestly.

### Provider Status Hierarchy (`ProviderStatus`)

```csharp
public abstract record ProviderStatus
{
    public sealed record Ok : ProviderStatus;
    public sealed record Stale(DateTime Since) : ProviderStatus;
    public sealed record NeedsAuth(string Message) : ProviderStatus;
    public sealed record AccessDenied(string Message) : ProviderStatus;
    public sealed record Unsupported(string Reason) : ProviderStatus;
    public sealed record NotRunning : ProviderStatus;
    public sealed record Error(string Message) : ProviderStatus;
}
```

### Transition and Degradation Rules

1. **Success (`Ok`)**:
   - Updates in-memory reading (`lastGoodReadings[providerId]`).
   - Persists reading to disk (`UsageArchive.Save()`).
   - Clears any previous access-denied state.
2. **Expired Credential (`CredentialExpired`)**:
   - Local tokens rotated periodically (such as Claude Code or Antigravity) may have expired overnight while the tool was closed.
   - **Rule**: This does **not** mean the user logged out. The previous reading is preserved, but marked as `Stale(fetchedAt)`. The UI displays dimmed numbers alongside the timestamp of the last valid reading, waiting for the developer to run the official tool to refresh the token.
3. **Rate Limit Hit (`RateLimited`)**:
   - Not an error the user can fix manually.
   - The previous reading is retained with status `Stale`, preventing blank UI rings or zeroed counters.
4. **Access Denied by OS (`AccessDenied`)**:
   - Occurs if Windows Credential Manager or file ACLs refuse read access.
   - Retains the last valid reading as `Stale` so transient permission hiccups do not wipe the display.
5. **Missing Authentication / Logged Out (`NeedsAuth`)**:
   - Occurs when no credential exists or the API explicitly returns `401 Unauthorized` / `403 Forbidden` without indication of temporary expiration.
   - **History Suppression Rule (`SupersedesHistory`)**: Because the credential no longer exists or is revoked, old readings are invalid and **must be discarded immediately**, displaying an empty dash `—` and appropriate sign-in guidance.
6. **Data Source Not Running (`NotRunning`)**:
   - Occurs when a provider depends on a live local process (such as the Antigravity Language Server) that is not discovered, and neither remote nor derived fallbacks produce data.
   - **Rule**: This is not an authentication failure. Report `NotRunning` with guidance to launch the source application instead of prompting for credentials.

### Historical Decision Matrix (`SupersedesHistory`)

| Failure Condition | Resulting Status | Retain Previous Reading? | Visual Ring Effect |
| :--- | :--- | :--- | :--- |
| HTTP 401 / 403 (Revoked Auth) | `NeedsAuth` | **No** (Discard) | Empty ring with dash `—` and login prompt |
| Data source application not running (no fallback data) | `NotRunning` | **No** (Discard) | Empty ring with "Not Running" guide |
| Unsupported Plan / No Quota | `Unsupported` | **No** (Discard) | Empty ring with plan notice |
| Expired Token (Past expiry) | `Stale` | **Yes** (Retain) | Dimmed value with original timestamp |
| HTTP 429 (Rate Limit) | `Stale` | **Yes** (Retain) | Dimmed value with penalty countdown |
| Network Timeout / SocketException | `Stale` | **Yes** (Retain) | Dimmed value |
| OS Access Denied | `AccessDenied` | **Yes** (Retain) | Dimmed value and reauthorization indicator |

---

## 4. Rate Limit Management (HTTP 429) & Persistent Exponential Backoff

Rapid application restarts during development or Windows reboots can trigger a destructive barrage of initial requests, indefinitely extending provider rate-limit penalties (particularly on Anthropic endpoints).

### A. Safe Interpretation of the `Retry-After` Header
The HTTP `Retry-After` header can appear in two formats:
1. Integer seconds: `Retry-After: 120`.
2. Absolute HTTP-Date: `Retry-After: Fri, 06 Sep 2026 14:30:00 GMT`.

> **CRITICAL WARNING**: Several APIs (including Anthropic internal endpoints) frequently return `Retry-After: 0` on HTTP 429. Obeying this literally ("wait 0 seconds") causes an immediate retry loop that traps the client in an infinite rate-limit penalty.
>
> **Rule**: Any server-provided value acts **strictly as a floor-raiser**, never lowering the waiting interval below the client's calculated backoff floor.

### B. Backoff Calculation Formula

$$\text{BaseFloor} = 60\text{ seconds}$$
$$\text{MaxCeiling} = 3600\text{ seconds (1 hour)}$$
$$\text{Tier}(n) = \min\Big(\text{MaxCeiling},\ \text{BaseFloor} \times 2^{\min(n - 1,\ 10)}\Big)$$

Progressive tiers per consecutive 429 attempts:
- Attempt 1: 60 seconds (1 minute)
- Attempt 2: 120 seconds (2 minutes)
- Attempt 3: 240 seconds (4 minutes)
- Attempt 4: 480 seconds (8 minutes)
- Attempt 5: 960 seconds (16 minutes)
- Attempts $\ge 7$: 3600 seconds (1 hour)

To prevent thundering herd synchronization when multiple clients operate concurrently, the tier is drawn with full jitter and then raised back to the previous tier, so randomness varies the wait without undoing the escalation:

$$\text{Jittered} = \text{Tier}(n) \times U(0, 1)$$
$$\text{WaitTime} = \max\Big(\text{Jittered},\ \text{Tier}(n - 1),\ \text{BaseFloor},\ \text{ServerRetryAfter}\Big)$$

Attempt $n$ therefore always waits at least as long as the tier reached by attempt $n - 1$, and never less than `BaseFloor`.

> **Counter lifecycle**: `ConsecutiveAttempts` is owned by the provider adapter, incremented on every 429 and reset to zero on the next successful reading. Passing a constant here silently disables the escalation, leaving the client retrying at the floor forever.

### C. Mandatory Deadline Persistence to Disk

To ensure protection survives Windows restarts or process recycling:
1. When a 429 response is received for provider `P`, compute:
   $$\text{Deadline} = \text{DateTime.UtcNow} + \text{TimeSpan.FromSeconds}(\text{WaitTime})$$
2. Save `Deadline` to the local state file (`%LOCALAPPDATA%\TokenHound\state.json` under key `backoffUntil.<providerId>`).
3. On app startup or before executing `FetchSnapshot()` for provider `P`, check first:
   ```csharp
   if (cachedDeadline.HasValue && cachedDeadline.Value > DateTime.UtcNow)
   {
       var remaining = cachedDeadline.Value - DateTime.UtcNow;
       throw new RateLimitedException(remaining);
   }
   ```
4. If the deadline has not passed, **no network request is dispatched**. The call is rejected locally in zero milliseconds and the HUD continues displaying the archived reading in `Stale` state.

---

## 5. Archiving and Cold Start (`UsageArchive`)

When launching the application for the first time or after Windows boot:
- A cold start that cannot immediately reach the network must never show blank rings.
- The `UsageArchive` component loads cached readings from `%LOCALAPPDATA%\TokenHound\last_readings.json`.
- All readings restored from disk initialize with status:
  $$\text{Status} = \text{ProviderStatus.Stale}(\text{OriginalSavedTimestamp})$$
- The first successful network fetch silently replaces the archived state with `Ok`. If the network call fails, previous data remains visible to the user.
