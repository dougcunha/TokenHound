# PRD — Provider Adapter: Cursor

## 1. Executive Summary

This feature integrates TokenHound with the **Cursor** AI code editor on **Windows 11**, extracting authenticated session tokens from local SQLite storage (`state.vscdb`), querying the usage telemetry API via proprietary session cookie formatting, and monitoring active Composer agent runs.

---

## 2. Objectives

- **OBJ-01**: Extract `cursorAuth/accessToken` and `cursorAuth/stripeMembershipAuthId` safely from `%APPDATA%\Cursor\User\globalStorage\state.vscdb` using `SafeSqliteReader`.
- **OBJ-02**: Call `https://cursor.com/api/usage-summary` sending the mandatory `Cookie: WorkosCursorSessionToken=<accountAuthId>::<accessToken>` header.
- **OBJ-03**: Parse usage metrics, mapping `totalPercentUsed` (0.0 to 100.0) to `UsedFraction` (0.0 to 1.0) and handling bonus quota pools on free accounts.
- **OBJ-04**: Track Composer agent state (`Waiting`, `Busy`, `Idle`) by inspecting the `composerHeaders` table and `Cursor.exe` process liveness.
- **OBJ-05**: Implement `IUsageProvider` (`ProviderId => "cursor"`) and `IActivityMonitor`.

---

## 3. Requirements

### Functional Requirements
- **FR-01**: Read Cursor auth keys from SQLite `ItemTable` non-lockingly in read-only mode.
- **FR-02**: Format and inject the `WorkosCursorSessionToken` cookie for web API telemetry requests.
- **FR-03**: Expose primary quota using `plan.totalPercentUsed` / 100.0, avoiding the `used / limit` zero-division/bonus trap.
- **FR-04**: Expose secondary `API usage` window when `apiPercentUsed > 0`.
- **FR-05**: Detect active Composer runs where `unfinishedRunAt` is set, `Cursor.exe` is running, and recent checkpoints are within 15 minutes.
- **FR-06**: Detect waiting approval states (`hasBlockingPendingActions` or `hasPendingPlan`).

### Non-Functional Requirements
- **NFR-01**: Security: never log access tokens or raw session cookies.
- **NFR-02**: Concurrency: open SQLite with `Mode=ReadOnly;Cache=Shared` to avoid write blocking.
- **NFR-03**: Architecture: sealed classes, alphabetized usings, XML documentation, Zero Fake Data.
