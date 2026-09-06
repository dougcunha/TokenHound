# Technical Specifications: Windows 11 C# / .NET Implementation

This documentation directory contains detailed reverse-engineering findings and technical specifications structured for the native **C# / .NET 10** implementation of **TokenHound** on **Windows 11**.

The central objective is to monitor resource usage metrics, rate limits, and plan thresholds for AI coding assistants without relying on conventional public APIs (which often do not exist). Instead, TokenHound reuses existing local sessions, authenticated developer tool tokens, local language server IPC, or direct internal endpoints queried by official clients.

---

## Technical Documentation Index

1. **[Reading Strategy, Lifecycle, and Resilience](01-READING-STRATEGY-RESILIENCE.md)**
   - Request scheduling: active mode (60s) vs. idle mode (5min) vs. stale threshold (15min).
   - System event reactivity: power suspend/resume, filesystem modification triggers.
   - Persistent exponential backoff algorithm for handling HTTP 429 rate limits.
   - Provider state machine and graceful error degradation policies.

2. **[Windows 11 Credential Storage and Security](02-WINDOWS-CREDENTIALS-SECURITY.md)**
   - Storage mechanisms: Windows Credential Manager (`advapi32.dll`), DPAPI (`System.Security.Cryptography.ProtectedData`), NTFS permissions.
   - Concurrent access patterns for SQLite databases under Write-Ahead Log (WAL) mode.
   - Non-prompting background token discovery.
   - Legacy Go `keyring` discovery and Base64 format decoding.

3. **[Provider Specification: Claude Code (Anthropic)](03-PROVIDER-CLAUDE-CODE.md)**
   - Tracking OAuth tokens in `%USERPROFILE%\.claude\.credentials.json` and multi-profile setups (`~/.claude-<slug>`).
   - Telemetry endpoint `/api/oauth/usage`, beta header `oauth-2025-04-20`.
   - Handling 5-hour rolling window gaps during reset rollovers.
   - Active session monitoring via JSON files, validating PID liveness and process start times.

4. **[Provider Specification: Cursor](04-PROVIDER-CURSOR.md)**
   - Session token extraction from editor SQLite database (`state.vscdb`).
   - Cookie-based authentication via `WorkosCursorSessionToken`.
   - Telemetry endpoint `/api/usage-summary` and allowance calculations (Free vs. Pro).
   - Agent activity tracking and composer blocker states (`composerHeaders`).

5. **[Provider Specification: OpenAI Codex (ChatGPT Desktop / CLI)](05-PROVIDER-CODEX.md)**
   - Primary mechanism: bidirectional JSON-RPC 2.0 stdio communication with `codex app-server`.
   - Fallback mechanism: tailing rollout JSONL logs discovered from `state_5.sqlite`.
   - Decoding absolute Unix epoch reset timestamps.
   - Activity heuristics driven by disk write telemetry.

6. **[Provider Specification: Google Antigravity / Gemini](06-PROVIDER-ANTIGRAVITY-GEMINI.md)**
   - Local Language Server discovery (ephemeral TCP port and CSRF token via process command lines and TCP table).
   - REST/gRPC-Web `/RetrieveUserQuotaSummary` call with self-signed TLS and forced cache invalidation.
   - Fallback to Google Cloud Code backend (`:loadCodeAssist` and `:retrieveUserQuotaSummary`).
   - Local contingency fallback: aggregating daily prompts from transcripts (`transcript.jsonl`).

7. **[Provider Specification: Z.ai GLM Coding Plan](07-PROVIDER-GLM-CODING-PLAN.md)**
   - Key scanning across Claude Code (`settings.json`), ZCode (`config.json`), and OpenCode (`auth.json`).
   - Routing between Global (`api.z.ai`) and China (`open.bigmodel.cn`) console clusters.
   - Monitoring endpoint `/api/monitor/usage/quota/limit` with raw token `Authorization` headers.
   - Decoding limit arrays structured as `(unit, number)` tuples.

8. **[Provider Specification: Perplexity (WebSession via WebView2)](08-PROVIDER-PERPLEXITY.md)**
   - Cloudflare Bot Management isolation via native Windows WebView2 runtime.
   - Runtime quota extraction at `/rest/rate-limit/all`.
   - Modeling unidirectional counters (remaining queries without explicit denominators).

9. **[Technical Specification: Authentication Flows and Session Management](09-AUTHENTICATION-LOGIN-FLOWS.md)**
   - *Borrow-Don't-Own* philosophy and duplicate account prevention.
   - Detailed token injection mechanics across HTTP/RPC requests.
   - Login route management (`SignInRoute`) and under-the-hood flow breakdown (OAuth PKCE, WorkOS, Google OAuth, Stdio IPC, WebView2).
   - C# (.NET) architectural implementation recommendations.

10. **[Technical Specification: Architecture and Project Structure (.NET 10 / C#)](10-ARCHITECTURE-PROJECT-STRUCTURE.md)**
    - Comprehensive comparison between WPF (.NET 10) and WinUI 3 for edge notches.
    - Full solution folder layout (`TokenHound.Core`, `TokenHound.Infrastructure`, `TokenHound.App`).

11. **[Provider Specification: GitHub Copilot (Lightweight Internal Quota)](11-PROVIDER-COPILOT.md)**
    - Borrowed `gh` / Copilot CLI OAuth token discovery (PATs rejected with 403).
    - Lightweight quota endpoint `copilot_internal/user` and defensive `quota_snapshots` parsing.
    - Heuristic activity tracking via CLI `session-state/events.jsonl` writes plus process liveness.

---

## Provider Comparison Matrix

| Provider | Windows Credential Source | Query Mechanism | Provided Telemetry | Real-Time Activity Tracking |
| :--- | :--- | :--- | :--- | :--- |
| **Claude Code** | `%USERPROFILE%\.claude\.credentials.json` | HTTPS GET `api.anthropic.com` | % Session (5h), % Weekly | Yes (`sessions/<pid>.json` + PID validation) |
| **Cursor** | `%APPDATA%\Cursor\User\globalStorage\state.vscdb` | HTTPS GET `cursor.com` (WorkOS Cookie) | % Used of plan allowance and API | Yes (`state.vscdb` -> `composerHeaders`) |
| **OpenAI Codex** | `%USERPROFILE%\.codex\auth.json` (metadata label) | Stdio JSON-RPC (`codex app-server`) or Rollout Logs | % Used primary (5h) & secondary (weekly/monthly) | Heuristic via recent disk write timestamps |
| **Antigravity** | Windows Credential Manager (`gemini:antigravity`) | Local HTTPS (Language Server) or Cloud Code | % Weekly per model or daily request counts | Yes (recent activity in `transcript.jsonl`) |
| **GLM (Z.ai)** | Config files (Claude Code / ZCode / OpenCode) | HTTPS GET `api.z.ai` / `bigmodel.cn` | % Session (5h), % Weekly, MCP Quota | Not directly available |
| **Perplexity** | Isolated WebView2 session profile | JS execution in authenticated page context | Absolute remaining count (Pro, Labs, etc.) | N/A |
| **Copilot** | `gh` OAuth token (OS keychain) / `%USERPROFILE%\.copilot\config.json` | HTTPS GET `api.github.com/copilot_internal/user` | % Monthly premium interactions, reset date | Heuristic (`session-state/events.jsonl` writes + host process liveness) |

---

## Architectural Principles for .NET / C# Implementation

1. **UI-Framework Agnostic Core**: All domain logic, network resilience, persistence, and credential discovery live in pure class libraries (.NET 10), completely decoupled from WPF, WinUI 3, MAUI, or Avalonia.
2. **Zero Fake Data**: If an API only reports remaining quota without a maximum limit, TokenHound will never invent a hypothetical percentage. If a request fails or a token expires, true degraded states are preserved (`stale`, `needsAuth`, etc.).
3. **Respect Credential Ownership**: The application never writes or forcefully refreshes tokens belonging to third-party tools (such as Claude Code or Cursor). TokenHound borrows access read-only (`FileShare.ReadWrite | FileShare.Delete`, SQLite `Mode=ReadOnly`) and lets official tools manage token renewal during their standard lifecycle.
4. **Restart Resilience**: Rate-limit penalties (HTTP 429) and last-known valid readings survive process shutdowns, ensuring that application restarts never hammer protected endpoints.
