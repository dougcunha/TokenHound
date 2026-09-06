# TechSpec — Provider Adapter: Google Antigravity / Gemini

## Sources and traceability

- PRD: [tasks/prd-provider-antigravity/prd.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-provider-antigravity/prd.md)
- Architecture: [ARCHITECTURE.md](file:///D:/MyProjects/Ideas/TokenHound/ARCHITECTURE.md)
- Domain Model: [CONTEXT.md](file:///D:/MyProjects/Ideas/TokenHound/CONTEXT.md)
- Specification: [docs/specs/06-PROVIDER-ANTIGRAVITY-GEMINI.md](file:///D:/MyProjects/Ideas/TokenHound/docs/specs/06-PROVIDER-ANTIGRAVITY-GEMINI.md)

## Technical decisions

- **DEC-01 (Process & Ephemeral Port Discovery)**: Extract `--csrf_token` via `Win32_Process` WMI query. Resolve bound TCP ports via P/Invoke to `iphlpapi.dll` (`GetExtendedTcpTable` with `TCP_TABLE_OWNER_PID_LISTENER`).
- **DEC-02 (Local HTTPS Client)**: Use `HttpClient` with custom `ServerCertificateCustomValidationCallback` accepting `127.0.0.1`. Query endpoint `https://127.0.0.1:{port}/exa.language_server_pb.LanguageServerService/RetrieveUserQuotaSummary` with `forceRefresh: true` and header `x-codeium-csrf-token`.
- **DEC-03 (Fraction Inversion)**: Compute `usedFraction = 1.0 - remainingFraction` to convert remaining quota to consumed quota.
- **DEC-04 (Derived Transcript Fallback)**: Parse `transcript.jsonl` files in `%USERPROFILE%\.gemini\*\brain\`. Aggregate steps where `source == "MODEL"` and `created_at` matches current local calendar date. Invariant: `UsedFraction` remains `null`.
- **DEC-05 (Activity Threshold)**: Consider agent `Busy` if any `transcript.jsonl` was modified within the last 45 seconds (`staleAfter = 45s`).

## Components and flow

```text
TokenHound.Infrastructure/Providers/Antigravity/
├── AntigravityEndpointDiscovery.cs       # WMI + iphlpapi.dll port resolver
├── AntigravityLanguageServerClient.cs    # Local HTTPS JSON client with CSRF auth
├── AntigravityTranscriptReader.cs        # Scans transcript.jsonl counting MODEL turns
├── AntigravityUsageProvider.cs           # Implements IUsageProvider with 3-tier fallback
└── AntigravityActivityMonitor.cs         # Implements IActivityMonitor with 45s write threshold
```

## Relevant files

- Create:
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityEndpointDiscovery.cs`
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityLanguageServerClient.cs`
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityTranscriptReader.cs`
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.cs`
  - `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityActivityMonitor.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityEndpointDiscoveryTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityLanguageServerClientTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityTranscriptReaderTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityUsageProviderTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityActivityMonitorTests.cs`
