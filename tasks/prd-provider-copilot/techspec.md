# TechSpec: provider-copilot

Status: ready for HIL 2 review. HIL 2 is not approved, so this document does not authorize implementation.

## Sources and traceability

- PRD: tasks/prd-provider-copilot/prd.md
- Product-stage decision: tasks/prd-provider-copilot/workflow.md, DEC-0002
- Provider specification: docs/specs/11-PROVIDER-COPILOT.md
- Shared resilience specification: docs/specs/01-READING-STRATEGY-RESILIENCE.md
- Windows credential and file-access specification: docs/specs/02-WINDOWS-CREDENTIALS-SECURITY.md
- Architecture: ARCHITECTURE.md
- Repository instructions: AGENTS.md
- Domain vocabulary: CONTEXT.md
- Applicable skills: sdd-create-techspec, sdd-create-techspec/references/dotnet.md, dotnet-efficient-validation, repository-cli-efficiency
- Destination: tasks/prd-provider-copilot/techspec.md

Evidence inspected in the existing code includes:

- Core contracts and models: src/TokenHound.Core/Contracts/IUsageProvider.cs, src/TokenHound.Core/Contracts/IActivityMonitor.cs, src/TokenHound.Core/Contracts/ICredentialStore.cs, src/TokenHound.Core/Models/LimitWindow.cs, src/TokenHound.Core/Models/Snapshot.cs, src/TokenHound.Core/Models/UsageBlock.cs, src/TokenHound.Core/Models/ProviderStatus.cs, and src/TokenHound.Core/Models/Fidelity.cs.
- Shared policies and orchestration: src/TokenHound.Core/Policies/RateLimitPolicy.cs, src/TokenHound.Core/Policies/BackoffCalculator.cs, src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs, src/TokenHound.Infrastructure/Engine/UsageStore.cs, and src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs.
- Read-only infrastructure: src/TokenHound.Infrastructure/Storage/SharedFileReader.cs, src/TokenHound.Infrastructure/Storage/SafeSqliteReader.cs, src/TokenHound.Infrastructure/System/ProcessLiveness.cs, and src/TokenHound.Infrastructure/Security/WindowsCredentialManager.cs.
- Representative adapters and tests: the Claude, Cursor, Codex, and Antigravity provider and activity-monitor implementations under src/TokenHound.Infrastructure/Providers, with corresponding tests under tests/TokenHound.Infrastructure.Tests/Providers.
- Existing presentation path: src/TokenHound.App/App.xaml.cs, src/TokenHound.App/ViewModels/NotchViewModel.cs, src/TokenHound.App/ViewModels/ProviderRingViewModel.cs, src/TokenHound.App/ViewModels/ProviderCatalog.cs, src/TokenHound.App/UI/Controls/ProviderRing.xaml.cs, and src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs.

External authentication evidence inspected on 2026-09-07 includes:

- [Authenticating GitHub Copilot CLI](https://docs.github.com/en/copilot/how-tos/copilot-cli/set-up-copilot-cli/authenticate-copilot-cli) confirms the precedence `COPILOT_GITHUB_TOKEN`, `GH_TOKEN`, `GITHUB_TOKEN`, system keychain, then GitHub CLI; it names the operating-system service `copilot-cli` and Windows Credential Manager.
- [Copilot CLI configuration directory](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-config-dir-reference) confirms `%USERPROFILE%/.copilot/config.json` or `COPILOT_HOME`, identifies `config.json` as automatically managed authentication/application state, and documents `loggedInUsers` as retained state. It does not publish a stable plaintext token property or Windows target-name contract.
- [Copilot CLI command reference](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference) confirms that plaintext fallback is under the Copilot configuration directory and that user-editable settings moved to `settings.json`.
- [GitHub Copilot CLI issue #4527](https://github.com/github/copilot-cli/issues/4527) reports a current Windows Credential Manager target shaped as `https://<host>:<login>.copilot-cli` for a data-residency host. This is implementation evidence, not a public compatibility contract.
- A read-only inspection of the installed Copilot CLI 1.0.82 profile found JSONC `config.json` state with `loggedInUsers` and `lastLoggedInUser` entries containing `host` and `login`, and no top-level token property. The same inspection found the host/login-qualified Copilot target in Windows Credential Manager. No secret value was read into this artifact.

## Solution summary

Add a Copilot adapter under TokenHound.Infrastructure that implements the existing IUsageProvider and IActivityMonitor contracts. The adapter will use the single lightweight Copilot HTTP endpoint, strict read-only credential discovery, an open-map quota parser, and the existing Snapshot, LimitWindow, UsageBlock, ProviderStatus, and Fidelity models. Core remains free of Windows, HTTP, filesystem, process, and WPF dependencies.

Complete the existing shared infrastructure gap for durable snapshots and rate-limit deadlines with a JSON UsageArchive and a dispatch gate in UsageStore. Add a two-second activity polling path that publishes the existing AgentSession state to the existing Provider Ring path. Register Copilot with the existing Notch; do not create a Copilot-specific surface, metadata contract, identity-only state, archive-retention rule, or launch metric. HIL1-01 through HIL1-06 remain explicit deferrals under DEC-0002.

## Technical decisions

| ID | PRD obligations | Decision | Reason and evidence | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | FR-01, FR-12, NFR-06 | Keep Copilot-specific network, credential, file, process, and parsing code in TokenHound.Infrastructure. Implement IUsageProvider and IActivityMonitor without changing either interface. | IUsageProvider and IActivityMonitor are the existing seams, and ARCHITECTURE.md requires Core purity and Infrastructure ownership of external integrations. | Adding Copilot contracts to Core would make the domain OS- or provider-specific. A provider-specific UI adapter would duplicate the existing Snapshot and Provider Ring path. |
| DEC-02 | FR-01, FR-04, FR-05, FR-06, NFR-02, AC-01 through AC-03 and AC-08 | Add ProviderStatus.Unsupported and an optional double? RemainingValue to the pure LimitWindow model. Map a valid finite Copilot category to Name = Monthly Premium Interactions, TotalUnits = the reported positive entitlement, RemainingUnits = the reported integer remaining value, RemainingValue = the reported quota_remaining float, UsedFraction = 1.0 - percent_remaining / 100.0, and ResetTimeUtc = quota_reset_date_utc. Leave Period null because the response supplies a reset instant but no safe fixed duration. | The current LimitWindow can represent an official denominator only through TotalUnits and can represent only integral RemainingUnits. The response supplies both entitlement and a fractional remainder, so preserving both values is necessary. No denominator or credit conversion is invented. | Rounding quota_remaining into RemainingUnits would lose source fidelity. Treating every category as a percentage of 100 would invent a denominator. Adding a Copilot-only snapshot type would break the existing provider contract. |
| DEC-03 | FR-02, FR-08, NFR-01, AC-04 and AC-09 | Implement strict credential precedence in CopilotCredentialDiscovery: COPILOT_GITHUB_TOKEN, GH_TOKEN, GITHUB_TOKEN, gh auth token, Copilot CLI keychain, then the plaintext config.json fallback under COPILOT_HOME or %USERPROFILE%/.copilot. Read files with SharedFileReader and read the keychain through the existing read-only credential abstraction. Resolve Windows targets from the documented `copilot-cli` service plus validated host/login metadata; do not hard-code one account-specific target. Parse JSONC and accept a plaintext token only from a verified installed-CLI fixture. Do not refresh, write, or request a PAT. | The official authentication documentation defines precedence, the `copilot-cli` service name, Windows Credential Manager, and the configuration directory. The configuration-directory documentation names `loggedInUsers` but does not define a token property or target-name API. The installed profile and current upstream issue provide host/login target evidence without making it a compatibility promise. SharedFileReader already uses FileShare.ReadWrite | FileShare.Delete. | A new TokenHound account, login loop, token cache, arbitrary recursive token search, or PAT prompt would violate the product boundary. The implementation prerequisite is to obtain a sanitized plaintext-fallback fixture from the installed official CLI before coding that branch; no property name or target is invented in the TechSpec. |
| DEC-04 | FR-03, FR-04, FR-05, NFR-02, NFR-04, AC-01 through AC-05 | Add a disposable CopilotApiClient for GET https://api.github.com/copilot_internal/user with Authorization: Bearer and Accept: application/json. Bound each request to 15 seconds, deserialize quota_snapshots as a case-insensitive open dictionary, and let CopilotQuotaParser distinguish valid finite data, a valid map with no finite entry, and schema drift. | The endpoint and timeout are defined by 11-PROVIDER-COPILOT.md. A dictionary avoids making premium_interactions the only accepted key, while explicit parse outcomes preserve the distinction between Unsupported and Stale. | The official Copilot SDK and all other endpoints are out of scope. A closed DTO with only chat, completions, and premium_interactions would fail open-map compatibility. |
| DEC-05 | FR-07, FR-12, NFR-04, AC-04 through AC-06 and AC-08 | Add a shared UsageArchive and a pure SnapshotRetentionPolicy. A successful Snapshot replaces and archives the last good reading. Stale results retain the last good windows and original FetchedAtUtc, while using a new rate-limit block when present. NeedsAuth and Unsupported clear the current and archived reading. | UsageStore currently retains snapshots only in memory and replaces them with empty Stale results. The PRD requires stale history retention and auth/unsupported history suppression, including across restart. | Keeping history only in CopilotUsageProvider would not survive restart and would duplicate policy. Treating all non-OK states alike would either fabricate current data or retain invalid auth data. |
| DEC-06 | FR-07, FR-11, NFR-03, AC-06 and AC-10 | Make UsageStore consult UsageArchive and the current blocked UsageBlock before every provider dispatch, including forced refresh. Persist the absolute Copilot deadline under backoffUntil.copilot before publishing the stale result. Reuse the shared RateLimitPolicy and align its implementation with spec 01: 60-second floor, 900-second ceiling, server Retry-After as a floor-raiser, and positive jitter. Retry-After: 0 must still produce a future deadline. | The current UsageStore gate is memory-only, and the current BackoffCalculator constants differ from the shared specification. The persisted absolute deadline is the product invariant; the gate must run before the HTTP client is called. | A Copilot-only gate would allow another dispatch path to bypass the invariant. Leaving the current 3600-second/full-jitter policy unchanged would preserve an existing code behavior but conflict with the authoritative shared resilience specification. |
| DEC-07 | FR-09, FR-10, NFR-05, AC-07 | Implement CopilotActivityMonitor with a two-second polling call, a 30-second write-freshness threshold, a 120 ms FileSystemWatcher debounce for session-state bursts, and read-only LastWriteTimeUtc checks over session-state/*/events.jsonl and logs. Require a qualifying live copilot, gh, or Code.exe process with an installed Copilot extension before returning Busy. Reuse ProcessLiveness for process checks. | The provider has no PID-scoped busy API. The specification requires both local write evidence and host liveness to prevent ghost activity. Existing monitors use IActivityMonitor and ProcessLiveness, while SharedFileReader supplies the required file-sharing behavior. | A process-only signal creates false Busy states. A write-only signal survives abrupt process termination. Reading event contents or writing marker files would expand scope and risk contention with the official tools. |
| DEC-08 | FR-11, FR-12, NFR-07, AC-08 and AC-10 | Add a separate two-second activity timer to UsageStoreLifetime/UsageStore and an ActivityUpdated event carrying provider ID and AgentSession. NotchViewModel will route the event to ProviderRingViewModel.UpdateActivity. Keep the existing 60-second refresh timer and 300-second idle decision; Copilot contributes no faster quota cadence. Add only generic Unsupported and ActiveBlock handling to existing ring/status mapping. | The existing store checks activity only when the 60-second refresh timer ticks, and no production path currently calls ProviderRingViewModel.UpdateActivity. Existing UI already has the required ring, status, activity, non-activating, and click-through conventions. | A new Copilot HUD or provider-specific interaction would violate DEC-0002. Putting activity in a new Copilot UI model would bypass the existing Provider Ring. |
| DEC-09 | NFR-05, NFR-06, NFR-07 and all acceptance scenarios | Validate Core and Infrastructure with the repository's native MTP route, build the affected WPF app separately, and use unit/integration evidence plus a manual desktop script. E2E is omitted by desktop .NET policy and no aggregate command may include a desktop E2E target. | global.json selects Microsoft.Testing.Platform, both test projects set UseMicrosoftTestingPlatformRunner=true and use xunit.v3.mtp-v2, and the effective SDK is 10.0.400. The app targets net10.0-windows with UseWPF. | Running the full desktop application as an automated E2E suite would violate the repository validation policy and would not improve parser, persistence, or concurrency evidence. |
| DEC-10 | HIL1-01 through HIL1-06, NFR-02 and NFR-07 | Preserve existing Notch conventions and defer exact status copy/layout, fractional formatting, optional metadata, identity-only presentation, archive expiry, and launch metrics. Do not add plan, SKU, login identity, entitlement, overage_count, token_based_billing, or VS Code identity fields to Snapshot or the UI. | DEC-0002 explicitly approves technical planning while deferring all six HIL1 items. Existing ProviderCatalog and ProviderGlyphs already contain Copilot entries, so no new design system or mark is required. | Guessing any deferred product choice would turn a technical plan into an unapproved product decision. |

## Components and flow

| ID | Component | New or modified | Responsibility | Dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | src/TokenHound.Infrastructure/Providers/Copilot/CopilotUsageProvider.cs | Create | Implement IUsageProvider, coordinate credential discovery and API calls, map status outcomes, finite quota, overage, and sign-in guidance into Snapshot. | CMP-02, CMP-03, CMP-04, RateLimitPolicy, SnapshotRetentionPolicy |
| CMP-02 | src/TokenHound.Infrastructure/Providers/Copilot/CopilotApiClient.cs | Create | Issue the single Copilot GET request, enforce the 15-second timeout, parse Retry-After seconds or HTTP date, and translate non-success responses into typed provider errors without retaining the bearer token. | HttpClient, System.Text.Json |
| CMP-03 | src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialDiscovery.cs, CopilotCredentialTargetResolver.cs, CopilotConfigReader.cs, and CopilotCredential.cs | Create | Resolve the strict credential precedence, invoke gh auth token read-only, resolve Copilot CLI Windows targets from validated host/login state, parse JSONC config state, read only a verified plaintext-fallback token property, and return only a non-empty borrowed token plus non-sensitive source classification. | ICredentialStore, WindowsCredentialManager, SharedFileReader, Process |
| CMP-04 | src/TokenHound.Infrastructure/Providers/Copilot/CopilotQuotaResponse.cs, CopilotQuotaSnapshotDto.cs, and CopilotQuotaParser.cs | Create | Represent the top-level response and open quota map; validate finite fields, select premium_interactions first and any valid finite category second, and report schema drift separately from no finite quota. | System.Text.Json, LimitWindow |
| CMP-05 | src/TokenHound.Infrastructure/Providers/Copilot/CopilotActivityMonitor.cs | Create | Poll Copilot session and log file timestamps, debounce session-state bursts, and return Busy only when fresh writes and a qualifying host coexist. Implement IDisposable for watcher cleanup. | IActivityMonitor, SharedFileReader, CMP-06, TimeProvider |
| CMP-06 | src/TokenHound.Infrastructure/Providers/Copilot/CopilotProcessHostDetector.cs | Create | Detect copilot and gh processes and Code.exe only when the Copilot extension is installed; use ProcessLiveness to validate returned process handles and expose an injectable detector for tests. | Process, ProcessLiveness, read-only extension-directory inspection |
| CMP-07 | src/TokenHound.Infrastructure/Engine/UsageArchive.cs | Create | Load and atomically persist last successful readings and provider backoff deadlines under %LOCALAPPDATA%/TokenHound. Serialize JSON and preserve unrelated provider keys. | System.Text.Json, local TokenHound-owned files |
| CMP-08 | src/TokenHound.Infrastructure/Engine/UsageStore.cs and UsageStoreLifetime.cs | Modify | Load archive state, merge retained readings, gate dispatch on persisted/current deadlines, persist successes and 429 deadlines, and run the separate two-second activity poll. | CMP-07, CMP-09, IUsageProvider, IActivityMonitor |
| CMP-09 | src/TokenHound.Core/Models/ProviderStatus.cs, LimitWindow.cs, Policies/RateLimitPolicy.cs, Policies/BackoffCalculator.cs, and new Policies/SnapshotRetentionPolicy.cs | Modify/create | Keep pure status, fractional remainder, retention, schedule, and deadline rules. No Windows, WPF, HTTP, process, or file reference is permitted. | Core models only |
| CMP-10 | src/TokenHound.App/App.xaml.cs, ViewModels/NotchViewModel.cs, ViewModels/ProviderRingViewModel.cs, UI/Controls/ProviderRing.xaml.cs, and UI/Controls/TooltipCard.xaml.cs | Modify | Register Copilot and its monitor, route activity updates, surface generic Unsupported and block guidance, and preserve existing Notch focus behavior. | CMP-01, CMP-05, CMP-08, existing Provider Ring |

The runtime flow is:

1. App creates the UsageArchive, passes it to UsageStore, registers CopilotUsageProvider and CopilotActivityMonitor, and adds disposable provider resources to the existing application lifetime.
2. UsageStore loads last successful readings as Stale and loads provider deadlines before starting its timers. The archive loader never reads or writes credentials.
3. The activity timer polls monitors every two seconds. CopilotActivityMonitor combines the latest qualifying file write within 30 seconds with host liveness, applies the 120 ms watcher debounce, and emits ActivityUpdated when the provider session changes.
4. The existing 60-second refresh timer asks the cached activity state whether the active cadence applies. A refresh occurs when any monitor is Busy or when the idle interval has elapsed. Before each provider call, UsageStore checks the in-memory and persisted deadline.
5. CopilotCredentialDiscovery selects one source without probing lower-priority sources after a usable source is found. CopilotApiClient sends one request. CopilotQuotaParser produces a finite LimitWindow, Unsupported, or schema-drift outcome.
6. UsageStore applies SnapshotRetentionPolicy, saves a successful reading or deadline, and raises SnapshotUpdated. NotchViewModel updates the existing ProviderRingViewModel. No provider-specific HUD surface is added.

## Contracts and data

### Existing contracts retained

- IUsageProvider remains the only quota-provider contract. ProviderId is copilot and GetSnapshotAsync accepts and propagates CancellationToken.
- IActivityMonitor remains the only activity contract. ProviderId is copilot and CheckLivenessAsync returns AgentSession or null.
- ICredentialStore remains read-only. No refresh, save, delete, or token-mint method is added.
- Snapshot remains the integration object. Its ProviderId, Status, Fidelity, FetchedAtUtc, LimitWindows, ActiveBlock, and ErrorDescription fields carry the result.

### Pure model changes

- ProviderStatus gains Unsupported. It means the service is reachable or the response is structurally valid but no usable Copilot entitlement or finite quota is available. It is distinct from NeedsAuth and Stale.
- LimitWindow gains optional RemainingValue of type double?. Existing providers leave it null. Copilot sets it to the exact quota_remaining value and leaves RemainingUnits as the reported integer cross-check. Existing UsedFraction behavior remains honest: it is non-null only when the response also supplies a valid positive entitlement through TotalUnits.
- A finite candidate must have a valid quota_id or map key, has_quota true, unlimited false, finite quota_remaining, finite percent_remaining in the reported range, valid integer remaining and entitlement, and positive entitlement. Missing or invalid fields are schema drift, not a zero.
- UsageBlock uses IsBlocked = false for permitted metered overage and IsBlocked = true for exhausted non-overage quota and HTTP 429 deadlines. The non-blocking overage block is an explicit diagnostic and must not prevent dispatch.
- Snapshot does not gain display name, plan, SKU, login, entitlement metadata, identity hint, launch metric, or a SignInRoute object. ProviderRing and ProviderCatalog continue to derive the Copilot display name and glyph through existing conventions.

### Activity event

UsageStore adds an Infrastructure event named ActivityUpdated with an event-args type containing ProviderId and AgentSession?. This is plumbing for the existing ProviderRingViewModel.UpdateActivity method, not a new product-facing UI contract. Activity failures publish an idle/null result so an abrupt process exit cannot leave a ghost Busy indicator.

### Copilot response mapping

The DTO maps quota_snapshots to an open, case-insensitive dictionary. Unknown category keys are accepted. The parser prefers a valid premium_interactions entry and otherwise selects the first valid finite entry with has_quota true. unlimited true or has_quota false entries are ignored. quota_reset_at is not used.

The top-level quota_reset_date_utc must parse as an ISO-8601 UTC DateTimeOffset. quota_remaining is preserved in RemainingValue without rounding. percent_remaining is used exactly for UsedFraction. entitlement and remaining remain source values, and token_based_billing is not converted into credit math. A valid finite category without a positive entitlement cannot be represented honestly by the current LimitWindow contract and is treated as schema drift.

### Archive data

UsageArchive owns two TokenHound files:

- %LOCALAPPDATA%/TokenHound/last_readings.json stores the latest successful Snapshot by provider ID. Loading it converts the reading to Stale while retaining its original FetchedAtUtc. A successful fetch atomically replaces only the relevant provider entry.
- %LOCALAPPDATA%/TokenHound/state.json stores the absolute deadline under the key backoffUntil.copilot. Writes preserve unrelated provider keys and use a serialized writer plus an atomic replacement. The archive is TokenHound-owned state, not a borrowed tool credential.

Archive files are optional and backward-compatible. A missing file means no archived reading or deadline. A malformed archive is reported as a local persistence error and cannot fabricate a quota value. The implementation must not silently erase a future deadline while repairing unrelated state.

## Integrations and interfaces

### Credential sources

| Order | Source | Read behavior | Failure behavior |
| --- | --- | --- | --- |
| 1 | COPILOT_GITHUB_TOKEN | Trim a non-empty environment value and use it as the bearer token. | Skip only when absent or whitespace. Do not fall through after a usable value is selected. |
| 2 | GH_TOKEN | Same read-only environment behavior. | Same. |
| 3 | GITHUB_TOKEN | Same read-only environment behavior. | Same. |
| 4 | gh auth token | Start gh auth token with redirected output, no login action, no interactive input, cancellation, and a short local process timeout. | Treat missing gh, non-zero exit, empty output, and cancellation caused by the local timeout as no token; preserve caller cancellation. |
| 5 | Copilot CLI keychain, then config.json | Read the official CLI keychain service `copilot-cli` through ICredentialStore. On Windows, derive host/login-qualified target candidates from validated `loggedInUsers`/`lastLoggedInUser` state and the observed current target shape; use %COPILOT_HOME%/config.json or %USERPROFILE%/.copilot/config.json as a JSONC plaintext fallback only after a sanitized official fixture verifies the token property. File reads use FileShare.ReadWrite | FileShare.Delete. | Malformed, missing, or locked data falls through to the next approved source. Account-specific targets are not constants, and an unverified config shape is treated as unavailable rather than searched heuristically. |
| 6 | VS Code extension state | Optional identity hint only. It is never parsed as a bearer token, persisted, or rendered until HIL1-04 is resolved. | No usable bearer token means the existing NeedsAuth route; no identity-only status is invented. |

The selected token is held only for the request lifetime. Error messages and diagnostics must not contain it. A token with a classic or fine-grained PAT prefix is allowed to reach the endpoint so that a PAT-shaped HTTP 403 can produce the specified NeedsAuth guidance; TokenHound never asks the user to paste one.

The config fallback is an implementation dependency, not a license to infer a schema: the official docs describe the file as managed state and do not name the plaintext token property. Before the credential task starts, the owner must capture a sanitized fixture produced by the installed official CLI with its documented plaintext fallback enabled in an isolated `COPILOT_HOME`. If that fixture cannot be obtained, the task must leave the config source unavailable and report the missing evidence instead of selecting arbitrary token-shaped strings.

### HTTP endpoint

- Method: GET
- URI: https://api.github.com/copilot_internal/user
- Headers: Authorization: Bearer followed by the selected borrowed token, and Accept: application/json
- Timeout: 15 seconds per request, with caller cancellation distinguished from a provider timeout
- Idempotency: GET only, no retry loop in the adapter. A later dispatch is allowed only after the persisted deadline gate permits it.
- External dependency: no Copilot SDK RPC, no account.getQuota, no session.usage.getMetrics, and no endpoint other than copilot_internal/user.

### Status and history matrix

| Condition | Snapshot status | History | ActiveBlock and guidance |
| --- | --- | --- | --- |
| No usable token | NeedsAuth | Clear | ErrorDescription contains Run 'gh auth login' or 'copilot login' in your terminal, then retry. Do not paste a PAT. |
| HTTP 401 | NeedsAuth | Clear | Same terminal guidance. |
| HTTP 403 with a PAT-shaped selected token | NeedsAuth | Clear | Terminal guidance plus a warning to check an unrelated GH_TOKEN or GITHUB_TOKEN override. |
| HTTP 403 without a PAT-shaped token, indicating no seat or unlicensed access | Unsupported | Clear | Empty ring and an explicit unavailable message; include the environment-override warning without claiming subscription revocation. |
| HTTP 429 | Stale | Retain | Keep the last good LimitWindows, attach IsBlocked = true and the computed future deadline, and persist it. |
| Timeout, network failure, missing quota map, malformed finite data, or unhandled non-200 | Stale | Retain | Keep the last good reading and expose a stale diagnostic. No zero or current value is substituted. |
| Valid map with no finite category | Unsupported | Clear | Empty ring; unlimited and non-entitled categories are omitted. |
| Valid finite category | Ok | Replace and archive | Official fidelity, finite LimitWindow, top-level reset, and overage UsageBlock semantics. |

### Activity integration

CopilotActivityMonitor resolves the session-state and logs directories from the user profile. It enumerates only the specified events.jsonl and log files, opens candidates with shared read access when needed, and reads timestamps without appending, truncating, touching, or deleting files. The watcher observes the session-state directory and consolidates burst notifications for 120 ms; the two-second poll remains the source of the freshness decision.

CopilotProcessHostDetector qualifies copilot and gh processes directly. It qualifies Code.exe only when an installed GitHub Copilot extension directory is found in the supported VS Code extension locations. It validates each selected PID with ProcessLiveness and returns the process start time where available. If extension discovery or process access fails, the host signal is false.

### Existing Notch integration

App.xaml.cs registers the provider and monitor. ProviderCatalog and Assets/Logos/ProviderGlyphs.xaml already contain Copilot name, badge, glyph, and scale entries and require no change. NotchViewModel subscribes to ActivityUpdated and dispatches updates through the existing UI dispatcher. ProviderRingViewModel keeps the existing fraction, reset, status, and activity properties. Its status resolution must surface an existing ActiveBlock reason for permitted overage and blocking exhaustion and must map Unsupported to the generic unavailable message. ProviderRing and TooltipCard use existing neutral/error status treatments; exact copy and layout remain HIL1-01.

The existing NotchWindow WM_MOUSEACTIVATE hook, WindowStyles WS_EX_NOACTIVATE behavior, click-through geometry, and SWP_NOZORDER rule are not changed by Copilot. NFR-07 is proved by the existing implementation plus the manual desktop script.

## Errors, security, and recovery

- Errors and edges: Caller cancellation propagates. A local timeout becomes Stale. A malformed JSON response, missing quota_snapshots map, invalid reset, invalid finite fields, or unknown non-200 becomes Stale unless it is one of the explicit 401, 403, or 429 cases. An empty but structurally valid quota map with no finite category is Unsupported. Unknown open-map categories do not fail a valid finite category.
- HTTP 403 classification: PAT-shaped means the selected token uses an allowlisted PAT prefix such as ghp_ or github_pat_. Other 403 responses follow the no-seat/unlicensed Unsupported path and carry the override warning. The implementation must not infer subscription revocation from a 403.
- Credential security: TokenHound never calls a login, refresh, revoke, mint, save, overwrite, or delete operation in another tool. Environment values are read only. Copilot and VS Code files are opened with FileShare.ReadWrite | FileShare.Delete. JSONC parsing is read-only and allowlisted by the verified fixture. Tokens are not logged, archived, or included in ErrorDescription.
- Concurrency: UsageStore keeps one refresh semaphore, UsageArchive serializes state writes, and atomic file replacement prevents partially written JSON. Activity watcher callbacks update a synchronized timestamp and never perform UI work. The existing lifetime drain cancels both timers before disposing resources.
- Rate limits: UsageStore checks the persisted and current absolute deadline before each dispatch. It does not dispatch a forced refresh while the deadline is in the future. Retry-After supports seconds and HTTP-date forms; zero and negative values cannot lower the 60-second floor.
- History recovery: A successful response replaces the archived reading. Stale and 429 retain it. NeedsAuth and Unsupported remove it from the active state and archive. A cold start shows an archived reading as Stale; a first success changes it to Ok.
- Rollback or reversal: Removing the Copilot registration stops all Copilot network and activity work without touching borrowed credentials. TokenHound-owned archive entries can remain for compatibility and are ignored when the provider is not registered. No rollback action deletes a user-owned tool file.

## Sequencing

| Step | Depends on | Verifiable result |
| --- | --- | --- |
| 1. Align pure status, fractional remainder, retention, schedule, and rate-limit policy seams. | DEC-02, DEC-06 | Core remains OS-free and unit tests prove Unsupported, RemainingValue, retention, 60/300-second scheduling, and durable-deadline policy inputs. |
| 2. Add UsageArchive and connect startup loading, atomic persistence, and pre-dispatch deadline checks to UsageStore. | Step 1, CMP-07, CMP-08 | A restart fixture loads Stale history and a future Copilot deadline prevents a forced provider call. |
| 3. Add Copilot DTOs, parser, credential discovery, HTTP client, and provider mapping. | Steps 1 and 2, DEC-03, DEC-04 | HTTP/header, credential precedence, open-map, status, reset, fraction, and overage tests pass without a live endpoint. |
| 4. Add Copilot activity monitor, host detector, two-second activity polling, debounce, and activity event routing. | Step 2, DEC-07, DEC-08 | Recent write plus live host is Busy; either signal missing is Idle; burst writes are consolidated and the ring receives activity updates. |
| 5. Register the provider in the existing App composition root and cover generic Notch status behavior. | Steps 3 and 4 | Copilot uses the existing name/glyph/ring/status path and no new HUD surface or focus behavior appears. |
| 6. Execute affected-project validation and the manual desktop script. | Steps 1 through 5, HIL 2 approval | Test counts and exit codes are recorded, E2E remains omitted by policy, and manual HUD evidence is separately recorded. |

## Test approach

- Profile: Windows 11 desktop solution on .NET SDK 10.0.400. TokenHound.Core targets net10.0 and remains pure. TokenHound.Infrastructure targets net10.0 and owns Windows/file/process/HTTP code. TokenHound.App targets net10.0-windows, OutputType WinExe, and UseWPF=true. Both test projects target net10.0, are executable test projects, set UseMicrosoftTestingPlatformRunner=true, reference xunit.v3.mtp-v2, and are governed by global.json test.runner = Microsoft.Testing.Platform. Repository evaluation confirmed the selected SDK is 10.0.400 and UseMicrosoftTestingPlatformRunner is true for both test projects.
- Runner: native Microsoft.Testing.Platform through dotnet test with --project. MTP arguments follow the -- separator. xUnit MTP filters use --filter-class or --filter-method, not VSTest --filter.
- E2E: omitted by .NET desktop policy. Full-application, WPF, browser/WebView, and desktop automation are not part of aggregate test commands. Existing or future desktop E2E tests are not deleted, but they remain outside this feature's automated route.
- Command prerequisites and exclusions: restore once if assets are missing or package references change. Build affected projects without restore when the existing assets are valid. Run Core.Tests and Infrastructure.Tests separately, with --minimum-expected-tests 1. Do not run a solution-wide command that could include a desktop E2E target. Preserve and report each command's exit code; do not treat zero executed tests or listing-only as success.
- Planned commands, not executed during TechSpec drafting:
  - rtk dotnet restore tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --nologo --verbosity:minimal, only when restore assets are missing.
  - rtk dotnet restore tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --nologo --verbosity:minimal, only when restore assets are missing.
  - rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore --nologo --verbosity:minimal
  - rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal
  - rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal
  - rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1
  - rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Copilot*"
  - rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*UsageStore*"

Manual acceptance is separate from automated validation and remains pending execution. The owner is the coordinator or desktop reviewer:

1. Launch the built TokenHound.App through the Windows MCP App tool with mode=launch_executable. Do not use shell Start-Process for visual evidence.
2. With an approved gh or Copilot CLI OAuth session available, confirm a Copilot ring appears with Copilot identity, official fidelity data, the finite monthly window, and the reported reset. Do not paste a PAT into TokenHound.
3. Exercise a local Copilot session and confirm the ring becomes Busy only when a recent qualifying write and host process coexist, then returns to Idle after the freshness threshold or host exit.
4. Use the primary-monitor Windows MCP Screenshot with display [2] to record the HUD state. Click and drag the Notch while another application is foreground and confirm the foreground application remains active.
5. Use automated fixtures for 401, PAT-shaped 403, unlicensed 403, 429, timeout, schema drift, no finite quota, overage, and restart persistence. Do not use the live undocumented endpoint to manufacture failure states.

| ID | Obligations | Level | Scenario | Expected result | Command or project |
| --- | --- | --- | --- | --- | --- |
| TC-01 | FR-01, FR-03, AC-01 | Infrastructure unit/integration | Send a successful request through a fake HttpMessageHandler. | GET uses the exact endpoint, Bearer authorization, JSON accept header, 15-second bound, Copilot provider ID, Copilot display convention, and Official fidelity. | Infrastructure.Tests, CopilotApiClientTests and CopilotUsageProviderTests |
| TC-02 | FR-02, FR-08, NFR-01, AC-09 | Infrastructure unit | Provide multiple environment values, gh output, host/login-qualified keychain targets, a sanitized official JSONC plaintext fixture, malformed files, and a VS Code identity-only fixture. | First usable source wins, lower sources are not probed after selection, account-specific targets are derived rather than hard-coded, files remain unchanged, VS Code state is never a bearer token, unverified config shapes are ignored, and guidance never requests a pasted PAT. | Infrastructure.Tests, CopilotCredentialDiscoveryTests and CopilotConfigReaderTests |
| TC-03 | FR-04, FR-05, NFR-02, NFR-04, AC-01, AC-02, AC-03 | Infrastructure unit | Parse premium_interactions, a renamed finite category, only unlimited/non-entitled entries, unknown keys, missing maps, and malformed fields. | Premium is preferred, any valid finite entry is a fallback, raw fractional remainder and formula-derived fraction are preserved, and no finite data becomes Unsupported while schema drift becomes Stale. | Infrastructure.Tests, CopilotQuotaParserTests and CopilotUsageProviderTests |
| TC-04 | FR-07, FR-08, NFR-04, AC-04, AC-05 | Infrastructure unit | Return 401, PAT-shaped 403, non-PAT 403, timeout, malformed JSON, missing map, and unknown non-200 responses. | Auth and unsupported statuses clear history as specified; transient and schema failures retain stale history; terminal guidance and override warning are present without token disclosure. | Infrastructure.Tests, CopilotUsageProviderTests |
| TC-05 | FR-06, FR-12, AC-08 | Core and Infrastructure unit | Use remaining <= 0 with overage_permitted true and false. | Permitted overage creates IsBlocked=false and does not gate dispatch. Disallowed exhaustion creates IsBlocked=true through the reported reset. | Core.Tests and Infrastructure.Tests, CopilotUsageProviderTests and UsageStoreTests |
| TC-06 | FR-07, NFR-03, NFR-04, AC-05, AC-06 | Infrastructure integration | Save a successful reading, return timeout/schema drift/429, restart the store, and force refresh before and after the deadline. | Stale uses the original reading, 429 retains it and persists a future deadline, restart restores the deadline, and no HTTP request is sent before expiry. | Infrastructure.Tests, UsageArchiveTests and UsageStoreTests |
| TC-07 | FR-11, NFR-03, AC-06, AC-10 | Core and Infrastructure unit | Exercise Retry-After seconds, HTTP-date, zero, negative, omitted, and repeated deadlines. | Shared policy enforces the 60-second floor and configured ceiling, zero never retries immediately, and the dispatch gate is applied to forced refresh. | Core.Tests RateLimitPolicyTests and Infrastructure.Tests UsageStoreTests |
| TC-08 | FR-09, FR-10, NFR-05, AC-07 | Infrastructure unit/integration | Use temporary session/log files and an injected host detector across recent, old, absent, and burst-write cases. | Recent write plus qualifying host is Busy; either missing signal is Idle; after 30 seconds it is Idle; watcher bursts are debounced at 120 ms; no file contents or timestamps are modified. | Infrastructure.Tests, CopilotActivityMonitorTests |
| TC-09 | FR-11, FR-12, NFR-05, AC-07, AC-10 | Infrastructure integration | Run activity polling with a busy and idle monitor and inspect refresh calls and ActivityUpdated events. | Activity polling is two seconds, quota refresh remains 60 seconds when busy and 300 seconds when idle, and activity reaches the existing ring method. | Infrastructure.Tests, UsageStoreTests and UsageStoreActivityTests |
| TC-10 | NFR-02, NFR-06 | Core unit/build | Serialize LimitWindow and Snapshot with RemainingValue, null fractions, Unsupported, and non-blocking UsageBlock. | Existing providers remain source-compatible, optional fields round-trip, and no Core project reference or using introduces UI/OS/network dependencies. | Core.Tests and Core project build |
| TC-11 | NFR-07, AC-10 | App compilation and manual desktop | Build the WPF app, launch it through the Windows MCP App tool, inspect display [2], click and drag the Notch. | Existing non-activating and click-through behavior remains intact; Copilot uses the existing ring/status presentation. | App build plus manual script; E2E omitted |
| TC-12 | NFR-01, NFR-03, NFR-04 | Infrastructure unit | Interrupt archive writes, read a missing archive, and supply unrelated provider state keys. | Writes are serialized/atomic, missing state degrades without fabricated data, unrelated keys survive, and a future known deadline is never cleared silently. | Infrastructure.Tests, UsageArchiveTests |

## Observability and rollout

- Signals: existing SnapshotUpdated events, ActivityUpdated plumbing, Snapshot.Status, Snapshot.ErrorDescription, LimitWindow values, UsageBlock.ResetTimeUtc, and the archived timestamps. Do not log bearer tokens, config contents, or response bodies.
- Migration and compatibility: no migration of credentials or provider files. New JSON files are optional and TokenHound-owned. RemainingValue is nullable and does not change existing provider data. Unsupported is an additive enum member consumed by the generic presentation path.
- Rollout: register Copilot with the same App composition root as the other providers. A missing credential, no seat, schema change, timeout, or rate limit produces an explicit status and does not prevent other providers from refreshing. Gate rollout on Core and Infrastructure tests, App compilation, and the manual focus check.
- Rollback: remove the Copilot provider and monitor registrations and ignore Copilot archive keys. Do not delete or rewrite gh, Copilot CLI, VS Code, session, or log files.
- Product telemetry: no new launch metric or telemetry contract is added because HIL1-06 is deferred.

## Risks and open items

- Risk: The endpoint is undocumented and unversioned. Probability is medium and impact is stale or unsupported Copilot data. Mitigation is an open-map parser, explicit schema-drift status, archived last-good data, and fixture coverage; no guessed zero is allowed.
- Risk: The Copilot CLI keychain target is account-qualified on the inspected Windows installation, while the public docs expose only the `copilot-cli` service name; the plaintext config token property is not documented. Probability is medium and impact is incomplete credential-source coverage. Mitigation is host/login target resolution from validated config state, read-only keychain access, and a sanitized official plaintext fixture before implementation. Do not invent a target or property in the task stage.
- Risk: Existing BackoffCalculator currently uses a 3600-second ceiling and full jitter while spec 01 defines a 900-second ceiling and positive one-to-five-second jitter. Probability is certain and impact is cross-provider behavior drift. Mitigation is to reconcile the shared policy and its existing tests before implementation; HIL 2 must review the shared-policy change.
- Risk: Code.exe extension discovery can vary between VS Code installations. Probability is medium and impact is false Idle. Mitigation is an injected host detector, supported extension locations, ProcessLiveness validation, and a documented open item if another official location is required.
- Risk: The current generic Notch maps one LimitWindow into session/weekly presentation slots and does not define monthly remainder formatting. Probability is certain and impact is presentation ambiguity. Mitigation is to preserve the raw value and existing ring convention while leaving exact treatment to HIL1-01 and HIL1-02.
- Open item HIL1-01: Product owner must confirm Notch treatment and copy for Ok, Stale, Unsupported, NeedsAuth, empty-ring, and overage states. Until then, use the existing generic status, empty-ring, and tooltip paths.
- Open item HIL1-02: Product owner must decide fractional quota_remaining formatting and whether raw remainder, percentage, or both are shown. Until then, preserve the raw double in LimitWindow and do not round in Infrastructure.
- Open item HIL1-03: Product owner must decide whether plan, SKU, identity, entitlement, overage_count, or token_based_billing appears. The TechSpec intentionally omits all of them.
- Open item HIL1-04: Product owner must decide the identity-only VS Code journey. The technical boundary is no new identity-only status or UI contract; without a bearer token, the existing NeedsAuth path remains the only available status.
- Open item HIL1-05: Product owner must confirm treatment and expiry across long stale periods and reset boundaries. The implementation uses existing archive behavior and adds no new retention duration or reset presentation rule.
- Open item HIL1-06: Product owner must decide whether launch success needs a metric. No metric, event, or dashboard contract is introduced.
- Implementation prerequisite IP-01: before the credential-discovery task starts, obtain and record a sanitized official CLI plaintext-fallback fixture in an isolated COPILOT_HOME, or explicitly mark the config fallback unavailable with the resulting acceptance gap. This prerequisite does not authorize TokenHound to write or refresh credentials.
- Authorization blocker: HIL 2 approval of this TechSpec, validation route, and later concrete tasks is required before any implementation or task artifact is authorized.

## Relevant files

### Modify

- src/TokenHound.Core/Models/ProviderStatus.cs
- src/TokenHound.Core/Models/LimitWindow.cs
- src/TokenHound.Core/Policies/RateLimitPolicy.cs
- src/TokenHound.Core/Policies/BackoffCalculator.cs
- src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs
- src/TokenHound.Infrastructure/Engine/UsageStore.cs
- src/TokenHound.Infrastructure/Engine/UsageStoreLifetime.cs
- src/TokenHound.App/App.xaml.cs
- src/TokenHound.App/ViewModels/NotchViewModel.cs
- src/TokenHound.App/ViewModels/ProviderRingViewModel.cs
- src/TokenHound.App/UI/Controls/ProviderRing.xaml.cs
- src/TokenHound.App/UI/Controls/TooltipCard.xaml.cs
- tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs
- tests/TokenHound.Core.Tests/Policies/BackoffCalculatorTests.cs
- tests/TokenHound.Core.Tests/Policies/RateLimitPolicyTests.cs
- tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreTests.cs
- tests/TokenHound.Infrastructure.Tests/ViewModels/ProviderRingViewModelTests.cs

### Create

- src/TokenHound.Core/Policies/SnapshotRetentionPolicy.cs
- src/TokenHound.Infrastructure/Engine/UsageArchive.cs
- src/TokenHound.Infrastructure/Engine/ProviderActivityChangedEventArgs.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotUsageProvider.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotApiClient.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialDiscovery.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialTargetResolver.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotConfigReader.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredential.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotQuotaResponse.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotQuotaSnapshotDto.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotQuotaParser.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotActivityMonitor.cs
- src/TokenHound.Infrastructure/Providers/Copilot/CopilotProcessHostDetector.cs
- tests/TokenHound.Core.Tests/Policies/SnapshotRetentionPolicyTests.cs
- tests/TokenHound.Infrastructure.Tests/Engine/UsageArchiveTests.cs
- tests/TokenHound.Infrastructure.Tests/Engine/UsageStoreActivityTests.cs
- tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotUsageProviderTests.cs
- tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotApiClientTests.cs
- tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotCredentialDiscoveryTests.cs
- tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotQuotaParserTests.cs
- tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotActivityMonitorTests.cs
- tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotProcessHostDetectorTests.cs

### Reuse without modification

- src/TokenHound.Core/Contracts/IUsageProvider.cs
- src/TokenHound.Core/Contracts/IActivityMonitor.cs
- src/TokenHound.Core/Contracts/ICredentialStore.cs
- src/TokenHound.Core/Models/Snapshot.cs
- src/TokenHound.Core/Models/UsageBlock.cs
- src/TokenHound.Core/Models/Fidelity.cs
- src/TokenHound.Infrastructure/Storage/SharedFileReader.cs
- src/TokenHound.Infrastructure/Storage/SafeSqliteReader.cs, because Copilot has no SQLite source
- src/TokenHound.Infrastructure/System/ProcessLiveness.cs
- src/TokenHound.Infrastructure/Security/WindowsCredentialManager.cs
- src/TokenHound.App/ViewModels/ProviderCatalog.cs
- src/TokenHound.App/Assets/Logos/ProviderGlyphs.xaml
