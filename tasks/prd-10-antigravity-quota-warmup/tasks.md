# Tasks Plan: Antigravity Quota Retrieval Warmup

## Objective
Implement session warmup via `GetUserStatus` in `AntigravityLanguageServerClient` and integrate automatic retry on empty quota in `AntigravityUsageProvider`.

## Tasks DAG
```mermaid
graph TD
    T1["task_01: Implement WarmupSessionAsync in AntigravityLanguageServerClient (Completed)"] --> T2["task_02: Integrate Quota Warmup and Retry in AntigravityUsageProvider (Completed)"]
```

## Inventory
- `task_01.md`: [Completed] Add `WarmupSessionAsync` to `AntigravityLanguageServerClient` with candidate port iteration and unit tests.
- `task_02.md`: [Completed] Update `AntigravityUsageProvider.TryGetLanguageServerSnapshotAsync` to invoke warmup and retry on empty quota with unit tests.
