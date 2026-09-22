# Stable execution context

Load in this order:

1. `tasks/prd-06-mcp-metrics/prd.md`
2. `tasks/prd-06-mcp-metrics/techspec.md`
3. This file

Recover only changed sources after the approved versions are loaded.

---

# T03 — Wire desktop lifetime and document connection

## Outcome

The WPF application starts the local MCP host alongside the HUD, stops it before the usage store, and documents a working SSE client connection. An MCP startup failure is diagnosed without disabling the HUD.

## Dependencies and boundaries

- Depends on: T02 approved and HIL 2 authorization.
- Unblocks: independent code review and HIL 3 acceptance.
- In scope: minimal App hook, asynchronous lifetime tracking, README link, detailed MCP guide, affected builds and checks.
- Out of scope: changing HUD visuals, a settings UI, remote hosting, provider adapters, and desktop E2E automation.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01, US-01, FR-01, FR-03, FR-08, NFR-04, NFR-05 | `prd.md#outcomes-and-metrics`, `#functional-requirements`, `#non-functional-requirements` | Run MCP with TokenHound and make the endpoint usable by a client. |
| DEC-08, DEC-10, DEC-12, CMP-06, CMP-08, TC-05, TC-06 | `techspec.md#technical-decisions`, `#components-and-flow`, `#test-approach` | Wire and document the desktop lifecycle without widening scope. |

## Context to recover on demand

- Applicable skills and rules: `repository-cli-efficiency`, `dotnet-efficient-validation`, `no-workarounds`, and the C# rules in `AGENTS.md`.
- Existing code: `App.OnStartup`, `App.InitializeUi`, `App.ScheduleInitialRefresh`, and `ApplicationLifetime.ShutdownCoreAsync` at the TechSpec's cited spans.
- Contract: `techspec.md#observability-and-rollout` and `#test-approach` manual script.
- Environment: AGENTS.md requires Windows MCP `App` for visible launch and `Screenshot` on display `[2]` for HUD positioning; those tools are not present in this host.

## Work

- [x] T03.1 Add short startup hooks to `App.xaml.cs`; put MCP-specific composition in `App.Mcp.cs`. Track the host startup task and surface bind failure in structured logs without blocking HUD startup.
- [x] T03.2 Extend `ApplicationLifetime` with generic asynchronous resource tracking; cancel and stop the MCP host before stopping or disposing `UsageStore`.
- [x] T03.3 Write `docs/MCP.md` with the SSE URL, two tool names, response meanings, privacy boundary, and a working client configuration; link it from `README.md`.
- [x] T03.4 Build the App and scoped MCP test project, inspect the diff/quality profile, and reconcile the HIL 3 manual script as pending or executed evidence.

## Acceptance criteria

- Starting TokenHound makes the documented local MCP endpoint available while the HUD remains responsive; a port conflict leaves the HUD usable and logs why MCP failed.
- Tray exit ends active MCP sessions before `UsageStore` is disposed, and the port is released.
- Documentation matches tested tool names, URL, SSE mode, data timestamps, and absent/null value semantics.
- No new provider request path or UI geometry change is introduced.

## Verification

- Unit: existing reader tests stay green; no test is added solely to mirror App wiring.
- Integration: rerun scoped MCP host/transport tests after App integration only if that code or dependency state changes them; verify orderly host stop through TC-05.
- E2E: omitted by .NET desktop policy.
- Manual: TC-06 script in `techspec.md#manual-acceptance-script` at HIL 3; owner is coordinator with Windows MCP tools or the user on a visible desktop. Do not claim it passed without execution.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` after the T02 restore; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Mcp*"` only if the test build is still valid, preserving `$LASTEXITCODE`.
- Environment dependency: Windows .NET 10 build environment; visible desktop plus Windows MCP `App`/`Screenshot` or user execution for manual TC-06. The desktop tools are unavailable in this host.
- Expected evidence: successful App build, scoped MCP test summary from current code, reviewed docs and diff, and explicit manual acceptance status.

## Affected files

- Modify: `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/ApplicationLifetime.cs`, `README.md`.
- Create: `src/TokenHound.App/App.Mcp.cs`, `docs/MCP.md`.

## Observability and recovery

- Operational signal: App logs MCP startup or bind failure and host stop without exposing metrics or credentials.
- Recovery: remove the App startup hook to disable MCP while preserving the HUD; no persisted MCP data needs cleanup.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `App.OnStartup` calls `StartMcpServer` after the initial refresh is scheduled. `App.Mcp.cs` creates `McpServerHost` over the shared `UsageStore`, registers it with `ApplicationLifetime.TrackAsyncResource`, and tracks its start as a startup task run on the thread pool, so the HUD thread never builds or binds Kestrel. A bind conflict logs a warning (the host already logs the error with endpoint and exception type); any other startup exception is logged and the HUD continues. `ApplicationLifetime` now tracks several startup tasks instead of overwriting one (single-task behavior unchanged) and disposes tracked async resources after startup tasks drain and before `UsageStore.StopAsync`/`Dispose`, so MCP sessions close and the port is released before store disposal. `docs/MCP.md` documents both URLs, loopback/Host/Origin rules, local limits, bind-failure behavior, Claude Code and JSON client setup, both tools, `lookupState` values, every field, null semantics, and the privacy boundary; `README.md` links it.
- Changed files: new `src/TokenHound.App/App.Mcp.cs` (40 lines) and `docs/MCP.md`; modified `src/TokenHound.App/App.xaml.cs` (+1 line), `src/TokenHound.App/ApplicationLifetime.cs` (243 lines, constructor arity unchanged), and `README.md` (+1 link).
- Checks: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` 0 errors, 0 warnings. The scoped MCP tests were not rerun: T03 changes only App files that the test project does not compile, so the T02 run (20 passed) stays valid for the Infrastructure code. Docs checked against the DTO `JsonPropertyName` values, the `ProviderStatus`/`Fidelity` enums, provider ID constants, and the tested routes. Quality profile over the three App files: QA-01 to QA-05 no hits; QA-06 only the baseline four-argument `ApplicationLifetime` call (now line 333); QA-07 `App.xaml.cs` 415 lines (baseline 414, below 500), others below 300.
- Validated state: current uncommitted T01–T03 files on Git base `97f17c7`, Debug build of the App.
- Open items: TC-06 (visible desktop comparison) and the manual half of TC-05 (tray exit releases the port) were not executed. This session does have the Windows MCP `App` and `Screenshot` tools, but the user's installed TokenHound (`D:\Apps\TokenHound\TokenHound.App.exe`) is running and the app has no single-instance guard; launching the Debug build beside it would duplicate the HUD and provider polling. Run TC-06 at HIL 3 after closing that instance or with user consent.

- Post-completion addition (workflow `DEC-19`): `README.md` gained an `MCP Server` section with per-harness setup; documentation only.

### ADR candidates

None - direct TechSpec implementation.
