# PRD — Provider Adapter: OpenAI Codex (ChatGPT Desktop & CLI)

## Problem and context

Following the completion of the Tracer Bullet and HUD presentation layer, TokenHound requires a dedicated provider adapter for the **OpenAI Codex** ecosystem on Windows 11. OpenAI Codex is used both as a developer CLI and through ChatGPT Desktop for Windows.

Unlike purely REST-based APIs, Codex provides two distinct telemetry sources:
1. **Primary Source**: An interactive local `codex app-server` exposed via stdio JSON-RPC 2.0 that reports live rolling quota limits and active plan types (`plus`, `team`, `pro`).
2. **Secondary Source (Fallback)**: Local rollout logs (`rollout-*.jsonl`) indexed in `%USERPROFILE%\.codex\state_5.sqlite` containing tail-end token usage records from previous agent turns.

TokenHound must ingest these metrics into domain `Snapshot` models with Zero Fake Data guarantees, respect rate limits, and provide real-time session activity monitoring (`IActivityMonitor`).

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | Real-time Codex quota ingestion | Rolling primary (5h) and secondary (weekly) utilization captured into `LimitWindow` records. |
| OBJ-02 | Cascaded two-source fallback | Automatically degrades from stdio App Server to SQLite rollout logs when binary is absent or unreachable. |
| OBJ-03 | Zero Fake Data integrity | Quota fractions match exact API percentages (0.0 to 1.0); never invents limits. |
| OBJ-04 | Active agent session detection | Detects file write activity within 8 seconds on active rollout files without ghost states. |

## Requirements

### Functional requirements

- **FR-01**: Discover `codex.exe` executable locations on Windows 11 (`%LOCALAPPDATA%\Programs\ChatGPT\resources\`, `%PROGRAMFILES%\ChatGPT\resources\`, `%USERPROFILE%\.codex\bin\`, or system `PATH`).
- **FR-02**: Implement stdio JSON-RPC 2.0 client launching `codex.exe app-server`, sending `initialize`, `initialized`, and `account/rateLimits/read`, with a strict 10-second timeout watchdog.
- **FR-03**: Parse JSON-RPC rate limits response (`primary` 5h window, `secondary` 7-day window, `rateLimitReachedType`, and Unix epoch reset timestamps).
- **FR-04**: Implement secondary rollout log reader: query `%USERPROFILE%\.codex\state_5.sqlite` via `SafeSqliteReader` for active rollout paths, read the final 256 KB of `rollout-*.jsonl`, and extract `rate_limits` tokens.
- **FR-05**: Implement account identity extraction from `%USERPROFILE%\.codex\auth.json` (decoding JWT payload for email and `chatgpt_plan_type`).
- **FR-06**: Implement `CodexUsageProvider` implementing `IUsageProvider` with two-stage fallback (AppServer -> Rollout log fallback).
- **FR-07**: Implement `CodexActivityMonitor` implementing `IActivityMonitor` polling `LastWriteTimeUtc` on active rollout files within an 8-second busy threshold.

### Non-functional requirements

- **NFR-01**: Resource efficiency: Process communication must timeout cleanly; rollout reading must seek to the last 256 KB without loading multi-megabyte chat transcripts into memory.
- **NFR-02**: Read-only Borrow-Don't-Own access: Open SQLite databases via `SafeSqliteReader` (`Mode=ReadOnly`) and JSON files via `SharedFileReader`.
- **NFR-03**: C# standards: Sealed classes, <= 300 lines per file, methods <= 30 lines, nesting <= 3 levels, file-scoped namespaces, alphabetized usings.

## Constraints and boundaries

- In scope: `TokenHound.Infrastructure/Providers/Codex/` implementation and tests in `TokenHound.Infrastructure.Tests/Providers/Codex/`.
- Out of scope: HUD UI changes (HUD already supports dynamic rings via `NotchViewModel`).
