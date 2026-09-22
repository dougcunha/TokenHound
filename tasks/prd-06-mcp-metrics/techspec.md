# TechSpec — MCP access to live provider metrics

## Sources and traceability

- Approved PRD: `tasks/prd-06-mcp-metrics/prd.md`, SHA-256 `eaab8d018d3ac1c19159ac94b4d823da2ad37e204d79381f0a34078bc4f92565` (workflow `DEC-02`).
- Repository rules: `AGENTS.md`, `ARCHITECTURE.md`, `docs/specs/01-READING-STRATEGY-RESILIENCE.md`, and `docs/specs/README.md`.
- Existing code: `UsageStore.CurrentSnapshots`, `UsageStore.RegisteredProviderIds`, `UsageStore.IsProviderEnabled`, `UsageStore._lastGoodSnapshots`, `SnapshotRetentionPolicy.Apply`, `NotchViewModel.UpdateOrAddRing`, `App.OnStartup`, and `ApplicationLifetime.ShutdownCoreAsync`.
- External: [C# SDK transports](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/transports/transports.md), [stateful/SSE behavior](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/stateless/stateless.md), [tool registration and structured results](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/tools/tools.md), and [NuGet package 2.2.0](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore/2.2.0).

## Solution summary

Add an in-process ASP.NET Core MCP host inside `TokenHound.Infrastructure`, started and stopped by the WPF application. It reads the existing `UsageStore`; no provider adapter, poller, archive, or rate-limit path changes. Two read-only MCP tools return all currently represented real providers or one provider by ID. The response is an explicit allowlist projection of snapshot fields, with status and timestamps intact.

Bind a fixed loopback endpoint at `http://127.0.0.1:37653/mcp`. The requested legacy SSE transport is available at `/mcp/sse`, with messages at `/mcp/message`; the SDK also serves Streamable HTTP at `/mcp`. Startup failure logs an error and leaves the HUD operational. The endpoint is active only during the application lifetime.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-04 | FR-01, OBJ-01 | Put MCP hosting and metric projection in `TokenHound.Infrastructure`; reference `ModelContextProtocol.AspNetCore` 2.2.0 and `Microsoft.AspNetCore.App` there. | Infrastructure already owns integrations and `UsageStore`; the NuGet package targets .NET 10. No new solution project or WPF dependency enters Core. | Hosting directly in App would make transport tests depend on WPF. A separate project adds structure without a necessary isolation boundary. |
| DEC-05 | FR-02, NFR-01, NFR-05 | Configure `MapMcp("/mcp")` with stateful HTTP sessions and explicit legacy SSE opt-in. Bind only IPv4 loopback; validate exact Host and any Origin; leave CORS disabled. | SDK 2.2.0 documents SSE as stateful and opt-in. Streamable HTTP remains available on the mapped route. | Stateless mode cannot serve legacy SSE. Remote binding and browser cross-origin access are outside the PRD. |
| DEC-06 | FR-03–FR-07, NFR-02, NFR-03 | Expose two structured tools, `list_provider_metrics` and `get_provider_metrics`. Project allowlisted values from concurrent store state; exclude `mock`, disabled providers, raw errors, and raw snapshots. | HUD rings are built from enabled snapshots; raw `ErrorDescription`, `UsageBlock.Reason`, and future model fields are unsuitable as a public data contract. | Returning `Snapshot` directly is shorter but risks leaking diagnostic or future private fields. |
| DEC-07 | FR-05, NFR-03 | Report both `snapshotFetchedAtUtc` and nullable `lastSuccessfulAtUtc`, sourced from the store's retained good snapshot. | Some adapters construct a new stale snapshot while retaining older windows; the snapshot timestamp alone can overstate freshness. | Changing every provider adapter or retention policy expands scope. |
| DEC-08 | FR-01, FR-08, NFR-04 | Use fixed port 37653 and document the URL. A bind conflict disables MCP for that run and logs the cause without stopping TokenHound. | MCP clients need a stable URL; the desktop app must stay usable if the port is occupied. | Dynamic ports would require discovery; a configurable port can be added if needed later. |
| DEC-09 | FR-02, NFR-01, NFR-05 | Bound legacy SSE input with a small HTTP request rate limit and a bounded connection count; reject excess requests rather than queueing them. | The SDK documents that legacy SSE `POST /message` returns 202 before tool completion and has no HTTP backpressure. | Loopback alone does not protect against local request floods or browser-origin abuse. |
| DEC-10 | NFR-04, NFR-05 | Track MCP startup with the existing application startup task, and asynchronously stop the MCP host before disposing `UsageStore`. | `ApplicationLifetime` already coordinates cancellation and store disposal. Blocking on async startup or shutdown in WPF risks deadlock. | Synchronous disposal cannot safely await active SSE connections. |
| DEC-11 | DEC-05, quality profile | Suppress only SDK diagnostic `MCP9004` around the explicit `EnableLegacySse` assignment, with a local comment citing the approved SSE requirement. | The SDK marks the property obsolete because of the documented backpressure risk, which DEC-09 mitigates. | An application-wide switch would hide the opt-in and warning context. |
| DEC-24 | NFR-03, FR-05, ACC-01 (workflow DEC-23) | Add nullable `LimitWindow.UsedUnits` to Core. The Antigravity adapter sets `UsedUnits` to today's request count and leaves `RemainingUnits` null. The HUD's derived-count branches read `UsedUnits`, keeping their text. The MCP window projection adds `usedUnits`. | Manual acceptance showed `gemini` reported `remainingUnits: 0` for about 0 requests used. Only Antigravity overloads `RemainingUnits`; the model documents it as remaining units. | Mapping in the MCP reader duplicates the HUD heuristic in two places; accepting the defect leaves a false quota reading in the public contract. |
| DEC-12 | Quality profile | No preparatory refactoring. Place new App wiring in `App.Mcp.cs` and add only short calls to `App.xaml.cs`. | The touched existing files are below the 500-line structural threshold and no saturated constructor or switch is extended. | `App.xaml.cs` already exceeds the repository's 300-line style target; extracting unrelated UI/provider setup would expand this feature. |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | `src/TokenHound.Infrastructure/Mcp/McpServerHost.cs` | New | Build, start, and stop the ASP.NET Core endpoint; configure transport, security, and request limits. | DEC-04, DEC-05, DEC-08–DEC-11 |
| CMP-02 | `src/TokenHound.Infrastructure/Mcp/McpMetricsTools.cs` | New | Register exactly the two read-only tools with stable names and structured output. | CMP-01, CMP-03 |
| CMP-03 | `src/TokenHound.Infrastructure/Mcp/McpMetricsReader.cs` | New | Read store state and map safe result DTOs without scheduling provider work. | CMP-04, CMP-05 |
| CMP-04 | `src/TokenHound.Infrastructure/Engine/UsageStore.Metrics.cs` | New partial file | Return the retained successful timestamp for a provider from existing concurrent state. | Existing `UsageStore` |
| CMP-05 | `src/TokenHound.Infrastructure/Mcp/` DTO records | New, one record per file | Represent the documented list, lookup, provider, window, block, billing, and Cline shapes. | Core model values only |
| CMP-06 | `src/TokenHound.App/App.Mcp.cs`, `App.xaml.cs`, `ApplicationLifetime.cs` | New partial and modified | Create the host, track startup, and await shutdown before store disposal. | CMP-01, DEC-10 |
| CMP-07 | `src/TokenHound.Infrastructure/TokenHound.Infrastructure.csproj` | Modified | Add the SDK package and ASP.NET Core framework reference. | DEC-04 |
| CMP-08 | `docs/MCP.md`, `README.md` | New and modified | Describe URL, SSE mode, tools, response semantics, security boundary, and connection example. | FR-08 |
| CMP-09 | `LimitWindow.cs`, `AntigravityUsageProvider.Snapshots.cs`, `ProviderRingViewModel.cs`, `ProviderRingViewModel.Status.cs`, `ProviderUsageRowFactory.cs`, `McpLimitWindowMetrics.cs`, `McpMetricsReader.cs` | Modified | Carry a used count separately from remaining units, from adapter through HUD and MCP. | DEC-24 |

At startup, App creates `UsageStore`, registers providers, applies monitoring settings, creates the HUD, then creates and starts `McpServerHost`. The host registers one `McpMetricsReader` instance against the same store. Each tool call enumerates a point-in-time copy of registered IDs, checks monitoring state, retrieves current immutable snapshots, and projects allowlisted fields. A read may overlap a provider update; each returned provider object comes from one immutable snapshot. The next request sees the later update. Shutdown cancels startup, stops the host and SSE connections, then stops and disposes the store.

## Contracts and data

### Endpoint and tools

| Contract | Input | Output or behavior |
| --- | --- | --- |
| Streamable HTTP | `http://127.0.0.1:37653/mcp` | MCP endpoint supplied by the SDK. |
| Legacy SSE | `GET http://127.0.0.1:37653/mcp/sse` | Stateful SSE stream; SDK supplies the corresponding `/mcp/message` URL. |
| `list_provider_metrics` | No arguments | `{ observedAtUtc, providers: [...] }`, sorted by provider ID; only registered, enabled, non-mock providers with snapshots. Empty array before readings arrive. |
| `get_provider_metrics` | Required `providerId: string` | `{ lookupState, observedAtUtc, provider: ... | null }`; `lookupState` is `available`, `unknown`, `disabled`, `pending`, or `synthetic`. Unknown/disabled/pending/synthetic return no metrics. IDs are case-insensitive. Blank IDs are invalid tool arguments. |

The tools use `McpServerToolAttribute` with explicit names and `UseStructuredContent = true`. The response is JSON with camel-case property names and string enum values. The reader uses `TimeProvider` for `observedAtUtc`. No MCP call invokes `RefreshNowAsync`, `RefreshProviderNowAsync`, or a provider adapter.

### Provider metric projection

- Required: `providerId`, `status`, `fidelity`, `snapshotFetchedAtUtc`, `lastSuccessfulAtUtc` (nullable), and `limitWindows`.
- Each window: `name`, optional `groupName`, `usedFraction`, `usedUnits`, `remainingUnits`, `remainingValue`, `totalUnits`, `resetTimeUtc`, and `periodSeconds`, preserving `null` from the model. Never derive a denominator or percent from a remaining or used count. `usedUnits` is a count consumed in the window when no limit is published (DEC-24); it is never copied into `remainingUnits`.
- Optional block: `isBlocked`, `resetTimeUtc`, `retryAfterSeconds`. Do not expose free-form `UsageBlock.Reason`.
- Optional Copilot billing: state, reason, attempt time, next request time, and allowlisted credit amounts, period, source time, and estimated flag from `CopilotCreditUsage`. Do not expose `Filters` or owner/account identifiers.
- Optional Cline account: balance credits, plan name, pass subscription flag. Optional Cline local: input/output/cache token counts, model-call count, sample-window start, and last activity. These values remain absent for other providers.
- Do not serialize `Snapshot.ErrorDescription`, raw provider payloads, file paths, credentials, or local session content.

`lastSuccessfulAtUtc` is the retained good snapshot's timestamp when one exists; it is `null` if no successful snapshot is known. `snapshotFetchedAtUtc` is the timestamp on the current status-bearing snapshot. A stale status always remains stale even when retained metric values are present. Copilot billing carries its own state and timestamps independently of the quota snapshot.

## Integrations and interfaces

- MCP SDK 2.2.0 hosts both HTTP modes. The transport's session management, framing, and JSON-RPC errors remain SDK-owned; TokenHound provides only tools and the local host boundary.
- Bind Kestrel to `127.0.0.1:37653` only. Reject Host values other than `127.0.0.1` with the expected port, and reject a supplied Origin unless it exactly matches the endpoint origin. Clients without Origin are allowed. Do not enable CORS.
- Apply an HTTP fixed-window limit of 60 requests per minute with zero queued requests, and a 16-connection Kestrel cap. Stateful Streamable HTTP sessions keep the SDK idle defaults, because SDK 2.2.0 marks `IdleTimeout` and `MaxIdleSessionCount` obsolete (`MCP9006`); legacy SSE sessions end with their GET stream and are not subject to those settings (workflow `DEC-16`). These values are local transport safeguards, unrelated to provider quotas. Exceeded requests receive HTTP 429 without reaching MCP tools.
- Existing provider credentials, archives, polling cadence, and 429 deadlines are untouched.
- A startup bind failure is logged with the endpoint and exception type; the host cleans up its failed web application. Active SSE sessions are closed during asynchronous host shutdown.

## Errors, security, and recovery

- A provider without a snapshot is `pending`; a disabled provider is `disabled`; a synthetic mock is `synthetic`; an unregistered ID is `unknown`. The list omits all four cases except available real snapshots.
- Provider status and billing state remain explicit. A stale/blocked/unauthenticated provider may have no windows; no empty or null field is converted into zero or success.
- Client cancellation ends its MCP request without affecting refresh. Host shutdown prevents new calls and drains existing connections before store disposal.
- The host allows only the known two tools. All fields use an allowlist projection, so new internal `Snapshot` fields do not become public automatically.
- The implementation has no persisted MCP state or migration. Reversal removes the App startup hook and package reference; the HUD retains its existing behavior.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| 1. Add SDK dependency, projection DTOs, and store timestamp accessor. | Approved plan | Infrastructure builds; projection tests cover truthful nullable values and filtering. |
| 2. Add MCP tools and loopback HTTP/SSE host. | Step 1 | In-process MCP client lists and invokes tools over SSE; security and lifecycle integration checks pass. |
| 3. Wire host into WPF startup/shutdown and document client setup. | Step 2 | App builds; documentation matches the tested endpoint; manual desktop script is ready. |

## Test approach

- Stack: .NET 10 SDK selected by `global.json` (`10.0.400`, `latestFeature`); effective installed SDK observed as `10.0.401`. Infrastructure and its tests target `net10.0`; the WPF App targets `net10.0-windows` with `UseWPF=true`. Tests use xUnit v3 with Microsoft.Testing.Platform via `global.json` and `UseMicrosoftTestingPlatformRunner=true`.
- E2E: omitted by .NET desktop policy. Do not run full-application, browser, WebView, or desktop automation in the aggregate test suite.
- Restore once after the new package reference: `rtk dotnet restore TokenHound.slnx --nologo --verbosity:minimal`. Then build only `src/TokenHound.App/TokenHound.App.csproj` and `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` with `--no-restore --nologo --verbosity:minimal`.
- Run scoped MTP tests: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Mcp*"`. Inspect stdout and preserve `$LASTEXITCODE`; use `rtk proxy` for a failing test if filtering hides its details. No VSTest `--filter` or `--logger`.
- Prerequisites: NuGet access for the new package on first restore, a Windows build environment for App, and free loopback ports for integration tests. Integration tests bind port 0 and read the assigned address; production uses 37653.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-03–FR-07, NFR-02, NFR-03 | Unit | Real enabled, disabled, mock, unknown, and pending providers; stale retained windows; null denominator; Copilot and Cline fields. | Exactly the allowed data and lookup states appear; no raw diagnostics or fabricated values. | `TokenHound.Infrastructure.Tests` MCP projection classes |
| TC-02 | FR-07, OBJ-02 | Unit | Repeated reads against a provider double that records fetches. | No provider fetch is invoked; a changed store snapshot appears on the next read. | `TokenHound.Infrastructure.Tests` MCP reader class |
| TC-03 | FR-01, FR-02, FR-04, FR-05 | Integration | Start host on port 0; connect an SDK client in SSE mode, discover both tools, call both, then stop host. | Tool results are structured and current; SSE connection closes on stop. | `TokenHound.Infrastructure.Tests` MCP transport class |
| TC-04 | NFR-01, NFR-04 | Integration | Send foreign Host/Origin, exceed local request limit, and occupy the configured port. | Requests are rejected; bind failure is logged and does not throw into App startup. | `TokenHound.Infrastructure.Tests` MCP host class |
| TC-05 | NFR-05, DEC-10 | Integration/manual | Update store while reading; stop host before disposing store. | No torn result or unhandled exception; no connection survives shutdown. | `TokenHound.Infrastructure.Tests` MCP concurrency class plus manual script |
| TC-07 | NFR-03, DEC-24, ACC-01 | Unit | Antigravity transcript snapshot; HUD ring and rows for a derived used-count window; MCP projection of that window. | Adapter sets `UsedUnits` with `RemainingUnits` null; HUD text is unchanged ("~N requests today · no limit published", "~N requests"); MCP emits `usedUnits` and omits `remainingUnits`. | `TokenHound.Infrastructure.Tests` Antigravity, ViewModel, and MCP projection classes |
| TC-06 | FR-01, FR-03, FR-08 | Manual desktop | Run the application, compare HUD and MCP output, disable a provider, and close the app. | Matching real metrics, disabled provider absent, endpoint unavailable after exit. | Manual script below |

### Manual acceptance script

Owner: coordinator when Windows MCP `App` and `Screenshot` tools are available; otherwise the user on a visible Windows desktop. This evidence remains pending until executed.

1. Launch `TokenHound.App` on the interactive desktop with the Windows MCP `App` tool (`mode="launch_executable"`). Capture the primary monitor with `Screenshot` (`display: [2]`). Confirm a real provider ring and record its visible status and metric.
2. Connect an MCP client in SSE mode to `http://127.0.0.1:37653/mcp/sse`; list tools and call `list_provider_metrics`. Confirm the ring's provider ID, status, metric value, and timestamp agree with the HUD, allowing for a normal background refresh between observations.
3. Call `get_provider_metrics` for that provider and for an unknown ID. Confirm `available` and `unknown`; inspect nullable quota fields and no credentials or raw diagnostics.
4. Disable that provider in Settings; call both tools again. Confirm it disappears from the list and returns `disabled` by ID. If the HUD shows a mock fallback, confirm it is absent from the list and returns `synthetic` by ID.
5. Exit via the tray. Confirm the SSE connection closes and the local URL no longer accepts calls. Reopen the app and confirm the stable URL works again.

The current host exposes no Windows MCP `App` or `Screenshot` tool, so the visible-desktop portion requires an environment that supplies them or user execution. Automated unit and local HTTP integration checks remain independent of that environment.

## Quality profile

The commands below run on C# files touched by the feature, scoped to `src/TokenHound.Infrastructure/Mcp/`, `src/TokenHound.Infrastructure/Engine/UsageStore.Metrics.cs`, `src/TokenHound.App/App.Mcp.cs`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/ApplicationLifetime.cs`, the CMP-09 files, and new MCP test files. Exclude generated `bin/` and `obj/` paths. A blocking new hit rejects a task; a reservation is reported unless justified below.

| ID | Rule | Class | Verification command | Prior justification |
| --- | --- | --- | --- | --- |
| QA-01 | No `async void` outside event handlers or synchronous waits on tasks. | Blocking | `rtk rg -n --type cs 'async void|\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)' <touched-cs-files>` | None |
| QA-02 | No service locator in metrics or hosting logic. | Blocking | `rtk rg -n --type cs 'GetRequiredService<|GetService<|ServiceLocator' <touched-cs-files>` | None |
| QA-03 | No empty catch that hides a failure. | Blocking | `rtk rg -n --type cs 'catch\s*\{\s*\}|catch\s*\(Exception\w*\)\s*\{\s*\}' <touched-cs-files>` | None |
| QA-04 | No warning suppression without an exact decision. | Blocking | `rtk rg -n --type cs '#nullable disable|#pragma warning disable' <touched-cs-files>` | DEC-11 permits only a local `MCP9004` suppression around `EnableLegacySse`. |
| QA-05 | Use an injected clock for observation time. | Reservation | `rtk rg -n --type cs 'DateTime\.(Now|UtcNow)' <touched-cs-files>` | None |
| QA-06 | Avoid constructor dependency growth and four-plus-argument signatures. | Reservation | `rtk rg -n --type cs '\w+\((?:[^),]+,){3,}[^)]*\)' <touched-cs-files>` plus constructor inspection | Existing App call in baseline is unrelated; new host uses an options record. |
| QA-07 | Keep modified and new C# files below 500 lines, and new files below the repository's 300-line target. | Reservation | `rtk rg -c '^' --type cs <touched-cs-files>` | Existing `App.xaml.cs` has 414 lines before this feature. |

- Escalation trigger for a later review: eight or more new reservation hits, a touched file newly crossing 500 lines, or the same block duplicated in three or more places. No heavy audit runs inside this feature cycle.

### Terrain baseline

Baseline measured at Git base `97f17c79a5afeebbd8ff3a534cced7681b382476`. Only existing C# files planned for modification are listed; every new file starts with no baseline allowance.

| File | Lines | Public members | Constructor dependencies | Cases | Pre-existing profile hits | Destination |
| --- | --- | --- | --- | --- | --- | --- |
| `src/TokenHound.App/App.xaml.cs` | 414 | 0 public methods; WPF overrides and private helpers | No constructor | 0 | QA-06: existing 4-argument `ApplicationLifetime` call at line 332; QA-07: 414 lines, above repository style target but below 500-line profile threshold. No QA-01–QA-05 hit. | Record. Add only startup hook calls here; place new helpers in `App.Mcp.cs`. |
| `src/TokenHound.Core/Models/LimitWindow.cs` | 55 | Properties only | None | 0 | None recorded. | Record (DEC-24). Add one documented nullable property. |
| `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.Snapshots.cs` | 179 | Partial of existing provider | Unchanged | 0 | None recorded. | Record (DEC-24). Change one window initializer. |
| `src/TokenHound.App/ViewModels/ProviderRingViewModel.cs` | 282 | Existing view model | Unchanged | 0 | Close to the 300-line style target. | Record (DEC-24). Change one condition; no net growth. |
| `src/TokenHound.App/ViewModels/ProviderRingViewModel.Status.cs` | 37 | Partial | Unchanged | Existing status switch | None recorded. | Record (DEC-24). Change one predicate. |
| `src/TokenHound.App/ViewModels/ProviderUsageRowFactory.cs` | 159 | Static factory | None | 0 | None recorded. | Record (DEC-24). Change one condition. |
| `src/TokenHound.App/ApplicationLifetime.cs` | 196 | 3 public methods plus property and constructor | 4 required plus 1 optional parameter | 0 | No QA-01–QA-07 hit in current file. | Record. Add generic async resource tracking without increasing constructor arity. |

- Preparatory refactoring: not recommended. Neither existing file reaches the 500-line structural threshold, 10 public members, 6 constructor dependencies, or 10 switch cases. The new work does not extend a saturated structure. The pre-existing App length is contained by adding new code to a partial file.

## Observability and rollout

- Log MCP start URL, successful stop, rejected Host/Origin, local request limiting, and startup/shutdown failures with structured fields. Never log tool result bodies or credential-bearing provider objects.
- Add a short README link to `docs/MCP.md`; include a working SSE client configuration, both tool names, the stable endpoint, and a note that `snapshotFetchedAtUtc` and `lastSuccessfulAtUtc` have different meanings.
- Ship enabled for local use with the desktop application. The MCP server has no database migration, credential write, or account setup. If a port bind fails, only MCP is unavailable for that process run.

## Risks and open items

- Legacy SSE is deprecated and lacks built-in request backpressure. DEC-09 bounds incoming requests; a future transport-only migration would need a separate product decision because SSE is explicitly approved in this PRD.
- A fixed port can conflict with another local application. DEC-08 defines failure behavior; configurable port selection is deferred.
- The local HTTP endpoint has no user authentication because it binds only loopback. Host and Origin validation limit browser-mediated access, but local processes in the same Windows account can query usage data. This is the approved local-only scope, not remote access.
- Manual visible-desktop evidence was executed on 2026-09-22 (workflow DEC-22); after DEC-24 only step 2 for the Antigravity ring needs to be repeated.
- An Antigravity reading archived before DEC-24 keeps its count in `RemainingUnits`. After an upgrade it is restored once as a stale snapshot until the first Antigravity refresh replaces it, which takes seconds at startup. No archive migration is added.

## Relevant files

- Modify: `src/TokenHound.Infrastructure/TokenHound.Infrastructure.csproj`, `src/TokenHound.App/App.xaml.cs`, `src/TokenHound.App/ApplicationLifetime.cs`, `README.md`, and the CMP-09 files (DEC-24).
- Create: `src/TokenHound.Infrastructure/Engine/UsageStore.Metrics.cs`, `src/TokenHound.Infrastructure/Mcp/*.cs`, `src/TokenHound.App/App.Mcp.cs`, `docs/MCP.md`, and `tests/TokenHound.Infrastructure.Tests/Mcp/*.cs`.
