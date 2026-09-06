# Provider Technical Specification: Cursor

This specification defines the technical integration with the **Cursor** editor on **Windows 11**, detailing local SQLite database access, proprietary session cookie assembly, allowance metrics interpretation, and real-time Composer agent tracking.

---

## 1. Overview and Data Fidelity

- **Provider ID**: `cursor`.
- **Display Name**: `Cursor`.
- **Fidelity**: `.official` (official metric from Cursor's control panel).
- **Headline Metric**: Included Plan Allowance (`included` - corresponding to `totalPercentUsed`).

---

## 2. Session Location & Extraction on Windows 11

Cursor persists its global state using the standard VS Code SQLite schema:

### Database Path
- **Windows Path**: `%APPDATA%\Cursor\User\globalStorage\state.vscdb`
- **Table**: `ItemTable`
- **Schema**: `(key TEXT PRIMARY KEY, value TEXT)`

### Required Authentication Keys

| SQLite Key | Type | Description |
| :--- | :--- | :--- |
| `cursorAuth/accessToken` | `TEXT` | JWT access token of the authenticated user. |
| `cursorAuth/stripeMembershipAuthId` | `TEXT` | Account / WorkOS identifier (e.g., `user_01JT4P1FS4AB8WA4N...`). |
| `cursorAuth/cachedEmail` | `TEXT` | Authenticated account email (for non-secret display identification). |
| `cursorAuth/stripeMembershipType` | `TEXT` | Subscription tier (e.g., `free`, `pro`, `enterprise`). |

### C# SQL Query

```sql
SELECT value FROM ItemTable WHERE key = @KeyParam;
```

---

## 3. Mandatory `WorkosCursorSessionToken` Cookie Requirement

> **AUTHENTICATION TRAP**: Sending the token as an `Authorization: Bearer <accessToken>` header **fails with HTTP 401/403** on Cursor's web API.
>
> The API strictly requires credentials sent as a **Cookie** combining the Account ID and Access Token:

```http
Cookie: WorkosCursorSessionToken=<stripeMembershipAuthId>::<accessToken>
```

### C# Implementation:
```csharp
var cookieValue = $"WorkosCursorSessionToken={accountAuthId}::{accessToken}";
httpRequest.Headers.Add("Cookie", cookieValue);
httpRequest.Headers.Add("Accept", "application/json");
```

---

## 4. Telemetry Endpoint & Allowance Parsing

### HTTP Request

- **Method**: `GET`
- **URL**: `https://cursor.com/api/usage-summary`
- **Headers**:
  ```http
  Cookie: WorkosCursorSessionToken=<accountAuthId>::<accessToken>
  Accept: application/json
  ```

### JSON Response Schema

```json
{
  "billingCycleStart": "2026-08-24T03:32:15.933Z",
  "billingCycleEnd": "2026-09-24T03:32:15.933Z",
  "membershipType": "free",
  "isUnlimited": false,
  "individualUsage": {
    "plan": {
      "enabled": true,
      "used": 0,
      "limit": 0,
      "remaining": 0,
      "breakdown": {
        "included": 0,
        "bonus": 19,
        "total": 19
      },
      "autoPercentUsed": 0,
      "apiPercentUsed": 19,
      "totalPercentUsed": 9.5
    },
    "onDemand": {
      "enabled": false,
      "used": 0,
      "limit": null
    }
  }
}
```

### Allowance Calculation Business Rules

1. **Free Tier / Bonus Trap**:
   - On free plans and promotional accounts, `used` and `limit` return **$0$** even during heavy usage. Quota is delivered via bonus pools (`breakdown.bonus`).
   - Calculating `used / limit` would erroneously report 0%.
   - **The official dashboard metric is `individualUsage.plan.totalPercentUsed`**. This percentage (0 to 100) must be divided by 100 for domain models (0.0 to 1.0).
2. **API Usage Window (`api`)**:
   - If `apiPercentUsed > 0`, expose a secondary limit window labeled `"API usage"`.
3. **On-Demand Window**:
   - Add only when `onDemand.enabled == true` and `onDemand.limit > 0`. Used fraction is `onDemand.used / onDemand.limit`.
4. **Renewal Date**:
   - Parsed from the ISO-8601 field `billingCycleEnd`.

---

## 5. Composer Agent Activity Monitoring

Cursor does not create PID-specific JSON files, but records the state of every Composer conversation in the same `state.vscdb` database.

### Querying the `composerHeaders` Table

```sql
SELECT value FROM composerHeaders WHERE isArchived = 0 ORDER BY recency DESC LIMIT 40;
```

Each row in the `value` column contains a serialized JSON object:

```json
{
  "composerId": "c4d5e6f7-1234-5678-9abc-def012345678",
  "name": "Authentication Refactoring",
  "subtitle": "Cursor Composer",
  "unfinishedRunAt": 1788645000000,
  "hasBlockingPendingActions": false,
  "hasPendingPlan": false,
  "conversationCheckpointLastUpdatedAt": 1788645120000,
  "lastUpdatedAt": 1788645120000,
  "createdAt": 1788644000000
}
```

### State Inference Logic

1. **Waiting State (`Waiting`)**:
   - If `hasBlockingPendingActions == true` or `hasPendingPlan == true`, the agent is paused waiting for user terminal confirmation or plan approval.
2. **Busy State (`Busy`)**:
   - If `unfinishedRunAt` is populated (non-null), an execution run was initiated.
   - **Liveness Verification**: `unfinishedRunAt` marks when the run began and may remain set if Cursor was force-closed. To confirm whether the execution is genuinely active:
     - The process `Cursor.exe` must be running on Windows.
     - The timestamp of the latest checkpoint (`conversationCheckpointLastUpdatedAt` or `lastUpdatedAt`) must be **after** the start time of the `Cursor.exe` process.
     - The time elapsed since the latest checkpoint must be less than 15 minutes (`staleAfter`).
3. **SQLite WAL Concurrency**:
   - As detailed in the security document, queries must open with `Mode=ReadOnly` without `immutable=1` to observe uncheckpointed writes in Cursor's `-wal` file.
