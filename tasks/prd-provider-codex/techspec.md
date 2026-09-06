# TechSpec — Provider Adapter: OpenAI Codex (ChatGPT Desktop & CLI)

## Sources and traceability

- PRD: [tasks/prd-provider-codex/prd.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-codex/prd.md)
- Architecture: [ARCHITECTURE.md](file:///D:/MyProjects/Ideas/TokenHound/ARCHITECTURE.md)
- Domain Model: [CONTEXT.md](file:///D:/MyProjects/Ideas/TokenHound/CONTEXT.md)
- Specification: [docs/specs/05-PROVIDER-CODEX.md](file:///D:/MyProjects/Ideas/TokenHound/docs/specs/05-PROVIDER-CODEX.md)

## Technical decisions

- **DEC-01 (Two-Stage Fallback)**: Attempt `codex app-server` stdio JSON-RPC first. If executable is missing or fails, fall back to `state_5.sqlite` + JSONL log tail.
- **DEC-02 (Tail Optimization)**: Read only the last 256 KB of `rollout-*.jsonl` using `FileStream.Seek(Math.Max(0, Length - 262144), SeekOrigin.Begin)` to avoid memory spikes on large files.
- **DEC-03 (JWT Claims Decoding)**: Extract `auth.json` `id_token` and parse middle Base64Url segment to read `email` and `chatgpt_plan_type` without external cryptographic libraries.
- **DEC-04 (Zero Fake Data)**: Quota percentages from `usedPercent` (0.0 to 100.0) map to `UsedFraction` (0.0 to 1.0). Total units are set to 100 and remaining to `100 - usedPercent`.
- **DEC-05 (Session Activity Heuristic)**: Consider agent busy if active rollout file or `codex-dev.db` was written within the last 8 seconds.

## Components and flow

```text
TokenHound.Infrastructure/Providers/Codex/
├── CodexAuthDiscovery.cs       # Reads auth.json and parses JWT claims
├── CodexAppServerClient.cs     # Stdio JSON-RPC 2.0 client for codex.exe
├── CodexRolloutLogReader.cs    # Queries state_5.sqlite and reads tail of rollout-*.jsonl
├── CodexUsageProvider.cs       # Implements IUsageProvider with 2-stage fallback
└── CodexActivityMonitor.cs     # Implements IActivityMonitor with 8s write threshold
```

## Relevant files

- Create:
  - `src/TokenHound.Infrastructure/Providers/Codex/CodexAuthDiscovery.cs`
  - `src/TokenHound.Infrastructure/Providers/Codex/CodexAppServerClient.cs`
  - `src/TokenHound.Infrastructure/Providers/Codex/CodexRolloutLogReader.cs`
  - `src/TokenHound.Infrastructure/Providers/Codex/CodexUsageProvider.cs`
  - `src/TokenHound.Infrastructure/Providers/Codex/CodexActivityMonitor.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexAuthDiscoveryTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexAppServerClientTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexRolloutLogReaderTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexUsageProviderTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexActivityMonitorTests.cs`
