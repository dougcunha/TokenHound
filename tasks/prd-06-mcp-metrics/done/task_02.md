# Stable execution context

Load in this order:

1. `tasks/prd-06-mcp-metrics/prd.md`
2. `tasks/prd-06-mcp-metrics/techspec.md`
3. This file

Recover only changed sources after the approved versions are loaded.

---

# T02 — Serve metrics through local MCP HTTP/SSE

## Outcome

An in-process MCP client can connect through legacy SSE, discover two structured read-only tools, and query current metrics. Host integration tests prove local-only access, bounded requests, bind failure, and orderly stop.

## Dependencies and boundaries

- Depends on: T01 approved and HIL 2 authorization.
- Unblocks: T03.
- In scope: SDK dependency, MCP tools, local web host, transport/security integration tests.
- Out of scope: desktop startup wiring, remote access, MCP mutation tools, provider polling changes.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01, FR-01–FR-05, FR-07, NFR-01, NFR-02, NFR-04, NFR-05 | `prd.md#functional-requirements`, `#non-functional-requirements` | Expose safe current metrics over local MCP and legacy SSE. |
| DEC-04–DEC-06, DEC-08, DEC-09, DEC-11, CMP-01, CMP-02, CMP-07, TC-03–TC-05 | `techspec.md#technical-decisions`, `#components-and-flow`, `#test-approach` | Implement and validate SDK transport, security, and lifecycle. |

## Context to recover on demand

- Applicable skills and rules: `repository-cli-efficiency`, `dotnet-efficient-validation`, `no-workarounds`, and the C# rules in `AGENTS.md`.
- Existing code: T01 reader and DTO files; `src/TokenHound.Infrastructure/TokenHound.Infrastructure.csproj` for package placement.
- Contract: `techspec.md#contracts-and-data` and `#integrations-and-interfaces`.
- External: official SDK transport, stateful/SSE, and tools pages linked by the TechSpec. Verify APIs against the pinned 2.2.0 package during implementation.

## Work

- [x] T02.1 Add `ModelContextProtocol.AspNetCore` 2.2.0 and ASP.NET Core framework reference to Infrastructure; restore once after the dependency change.
- [x] T02.2 Register only `list_provider_metrics` and `get_provider_metrics` as structured tools backed by the T01 reader.
- [x] T02.3 Start/stop the loopback host with stateful SSE opt-in, exact Host/Origin checks, no CORS, bounded request and connection limits (SDK session idle defaults per workflow `DEC-16`), and isolated startup failure logging. Limit `MCP9004` suppression to the explicit SSE opt-in.
- [x] T02.4 Add real SDK-client SSE tests and HTTP security/lifecycle tests using an ephemeral loopback port; cover live updates, shutdown, and no provider dispatch.

## Acceptance criteria

- `http://127.0.0.1:37653/mcp/sse` is the production SSE URL and `/mcp` is also mapped for Streamable HTTP. Tests can bind port 0 without changing the production URL.
- A real MCP client lists both tools and receives structured results; no write or refresh tool is registered.
- Foreign Host/Origin requests, excess traffic, and non-loopback exposure are rejected; a bind conflict logs and leaves the calling application able to continue.
- `StopAsync` closes active sessions before `UsageStore` disposal; concurrent reads and updates do not crash.

## Verification

- Unit: T01 projection tests remain valid; no replacement with wire mocks.
- Integration: `McpTransportTests` and `McpServerHostTests` start the actual ASP.NET Core host and use a real MCP SSE client or raw HTTP for Host/Origin/rate cases.
- E2E: omitted by .NET desktop policy.
- Manual: none for this task; desktop comparison is TC-06 at HIL 3.
- Commands: `rtk dotnet restore TokenHound.slnx --nologo --verbosity:minimal` after the new package; build `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` with `rtk dotnet build ... --no-restore --nologo --verbosity:minimal`; run `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Mcp*"`, preserving `$LASTEXITCODE`.
- Environment dependency: NuGet access for the new package, .NET 10 SDK, and available ephemeral loopback ports. No real provider service or account is required.
- Expected evidence: package restore and build exit 0; nonzero executed integration tests; verified SSE exchange and rejected hostile requests; QA-01–QA-07 checks.

## Affected files

- Modify: `src/TokenHound.Infrastructure/TokenHound.Infrastructure.csproj`.
- Create: `src/TokenHound.Infrastructure/Mcp/McpServerHost.cs`, `McpMetricsTools.cs`, host options/security helpers as needed within that folder, and `tests/TokenHound.Infrastructure.Tests/Mcp/McpTransportTests.cs` plus `McpServerHostTests.cs`.

## Observability and recovery

- Operational signal: structured start/stop, bind error, rejected Host/Origin, and rate-limit diagnostics without metric payloads.
- Recovery: stop the host and remove the package/host files; no schema or provider data migration exists.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `McpServerHost` serves the two structured, read-only tools on `127.0.0.1` (production port 37653, tests port 0) with Streamable HTTP at `/mcp` and legacy SSE at `/mcp/sse` + `/mcp/message`, stateful session mode, and the `EnableLegacySse` opt-in. A Host/Origin guard returns 403 for any Host other than `127.0.0.1:<bound port>` or an Origin other than `http://127.0.0.1:<bound port>`; clients without Origin pass. A global fixed-window limiter allows 60 requests per minute with no queue and answers 429; Kestrel caps connections at 16; no CORS. A bind conflict logs an error with endpoint and exception type and `StartAsync` returns `false`. `StopAsync` stops Kestrel with a 5-second shutdown budget and closes active SSE sessions. `get_provider_metrics` returns an MCP tool error for a blank ID.
- Changed files: `src/TokenHound.Infrastructure/TokenHound.Infrastructure.csproj` (ASP.NET Core framework reference, `ModelContextProtocol.AspNetCore` 2.2.0); new `src/TokenHound.Infrastructure/Mcp/McpServerHost.cs`, `McpServerHost.Pipeline.cs`, `McpServerHostOptions.cs`, `McpMetricsTools.cs`, `McpRequestGuard.cs`; new `tests/TokenHound.Infrastructure.Tests/Mcp/McpTransportTests.cs`, `McpServerHostTests.cs`, and the shared `McpHostFixture.cs`. T01 reader and DTOs unchanged.
- Checks: `rtk dotnet restore TokenHound.slnx --nologo --verbosity:minimal` exit 0. `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` 0 errors, 0 warnings; `src/TokenHound.App/TokenHound.App.csproj` also builds with 0 warnings. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Mcp*"` passed 20 tests (7 T01 + 13 T02) covering TC-03 (real SDK client over SSE: discovery, both calls, live update, no dispatch, blank ID), TC-04 (foreign Host/Origin, matching Origin, 429 after 60 requests, occupied port), and the automated half of TC-05 (concurrent reads during refresh, prompt stop closing an active session and the endpoint). The full Infrastructure project ran 739 passed, 1 failed: `AntigravityUsageProviderTests.GetSnapshotAsync_LiveIntegration_WhenAgyRunning_ReturnsOfficialMetrics` calls the real provider against a running `agy` process and got `Derived` fidelity; it touches no T02 code and is an environment-dependent pre-existing test. Quality profile: QA-01, QA-02, QA-03, QA-05 no hits; QA-04 only the `MCP9004` suppression permitted by DEC-11; QA-06 two false positives (test `DateTimeOffset` constructor, a comma-containing `Description` string); QA-07 largest touched file 137 lines.
- Validated state: current uncommitted T01 + T02 files on Git base `97f17c7`, `ModelContextProtocol.AspNetCore` 2.2.0 restored, .NET SDK per `global.json`, Windows loopback networking.
- Open items: SDK 2.2.0 marks `IdleTimeout` and `MaxIdleSessionCount` obsolete (`MCP9006`); per workflow `DEC-16` they are not set and SDK defaults apply to stateful Streamable HTTP sessions. Store-disposal ordering (TC-05 second half) and TC-06 belong to T03 and HIL 3.

### ADR candidates

None - direct TechSpec implementation; the session-setting change is recorded as workflow `DEC-16`.
