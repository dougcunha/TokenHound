# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-copilot/prd.md`
2. `tasks/prd-provider-copilot/techspec.md`
3. This file

Use the approved PRD and TechSpec versions. T01 and T02 are completed dependencies before execution.

---

# T03 - Implement Copilot provider and credential-safe HTTP path

## Outcome

The Infrastructure Copilot provider discovers a borrowed token in the approved order, calls the lightweight endpoint safely, parses the open quota map honestly, and maps all specified responses into Snapshot/status/history outcomes without writing credentials.

## Dependencies and boundaries

- Depends on: T01, T02, and IP-01
- Unblocks: T05
- In scope: Copilot provider, HTTP client, DTOs/parser, credential discovery, host/login target resolution, JSONC config reader, status mapping, and provider tests.
- Out of scope: Core contract changes outside T01, TokenHound archive internals outside T02, activity heuristics, WPF code, login/refresh/PAT flows, and display decisions deferred by HIL1.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 through FR-08 | `prd.md#functional-requirements` | Provider identity, credential order, endpoint, parsing, fraction/reset, overage, status/history, and sign-in guidance. |
| NFR-01, NFR-02, NFR-04, NFR-05, NFR-06 | `prd.md#non-functional-requirements` | Read-only credentials, source fidelity, open-map resilience, timeout, and Infrastructure ownership. |
| AC-01 through AC-05, AC-08, AC-09 | `prd.md#acceptance-criteria` | Success/fallback/unsupported/auth/schema/overage/multiple-credential journeys. |
| DEC-01 through DEC-05 | `techspec.md#technical-decisions` | Provider boundary, pure model use, credential evidence, HTTP/parser, and history mapping. |
| DEC-03, IP-01 | `techspec.md#integrations-and-interfaces`, `techspec.md#risks-and-open-items` | Host/login target resolution and verified plaintext fixture condition. |
| CMP-01 through CMP-04 | `techspec.md#components-and-flow` | Provider, HTTP, credential, DTO, and parser components. |
| TC-01 through TC-05 | `techspec.md#test-approach` | Provider, credential, parser, error, and overage evidence. |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`.
- Existing code: `src/TokenHound.Infrastructure/Providers/Claude`, `Cursor`, `Codex`, and `Antigravity` adapters; `src/TokenHound.Infrastructure/Security/WindowsCredentialManager.cs`; `src/TokenHound.Infrastructure/Storage/SharedFileReader.cs`; `src/TokenHound.Core/Contracts/IUsageProvider.cs` and `ICredentialStore.cs`.
- Contract or integration: TechSpec sections `Credential sources`, `HTTP endpoint`, `Status and history matrix`, `Copilot response mapping`, and `Errors, security, and recovery`.

## Work

- [x] T03.1 Satisfy IP-01 before coding the plaintext branch: obtain a sanitized fixture produced by the installed official Copilot CLI in an isolated `COPILOT_HOME`, or record the config fallback as unavailable with the exact acceptance gap. Never store a real token in the repository.
- [x] T03.2 Implement `CopilotCredentialDiscovery` with exact precedence: `COPILOT_GITHUB_TOKEN`, `GH_TOKEN`, `GITHUB_TOKEN`, read-only `gh auth token`, Copilot CLI keychain, then verified JSONC config fallback; stop probing lower sources after a usable source.
- [x] T03.3 Implement `CopilotCredentialTargetResolver` using `copilot-cli` service evidence and validated `loggedInUsers`/`lastLoggedInUser` host/login pairs. Support the observed qualified Windows target shape without hard-coding an account or enumerating arbitrary credentials.
- [x] T03.4 Implement `CopilotConfigReader` with shared file access and an allowlisted property from the verified fixture only. Treat malformed, missing, locked, or unverified state as unavailable and never recursively select token-shaped strings.
- [x] T03.5 Implement `CopilotApiClient` for one GET request with Bearer authorization, JSON accept header, 15-second timeout, cancellation, typed 401/403/429/other failures, and no token/body logging.
- [x] T03.6 Implement open-map DTO/parser behavior: prefer finite `premium_interactions`, fall back to another finite category, omit unlimited/non-entitled entries, preserve `quota_remaining`, calculate `UsedFraction`, validate reset/entitlement, and classify schema drift separately from Unsupported.
- [x] T03.7 Implement `CopilotUsageProvider` mapping for Ok, NeedsAuth, Unsupported, Stale, overage, sign-in guidance, and archive interaction through existing contracts.
- [x] T03.8 Add provider, credential, config, HTTP, parser, and error tests with fake handlers/processes/files and assertions that borrowed files and secrets remain unchanged/unlogged.

## Acceptance criteria

- The first usable credential source wins in the exact order and lower sources are not probed after selection; VS Code state is never a bearer token.
- Windows target candidates are derived from validated host/login state and the official service evidence; no account-specific target or arbitrary credential scan is used.
- IP-01 is evidenced or the config fallback is explicitly reported unavailable; no undocumented token property is guessed.
- The HTTP request is exact, bounded to 15 seconds, cancellable, and never retries inside the provider.
- Finite quota mapping preserves raw fractional remainder, formula-derived fraction, positive entitlement, and top-level reset; no denominator, credit math, or zero is invented.
- All specified statuses and history actions are covered, including PAT-shaped 403 guidance and no-seat 403 Unsupported.

## Verification

- Unit: credential precedence, target resolution, JSONC fixture, HTTP headers/timeout, parser selection, schema drift, error matrix, overage, and guidance tests.
- Integration: fake HttpMessageHandler and temporary shared-read fixtures; no live endpoint and no real token.
- E2E: omitted by .NET desktop policy.
- Manual: None; live credential/display confirmation belongs to T06 and must use an approved existing session without pasting a PAT.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Copilot*"`.
- Environment dependency: IP-01, Windows Credential Manager read access, installed `gh`/Copilot CLI only for sanitized fixture discovery, .NET SDK 10.0.400, and native MTP runner.
- Expected evidence: provider test output with non-zero count, sanitized fixture provenance, exact request assertions, status/history matrix results, and no changed borrowed files.

## Affected files

- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotUsageProvider.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotApiClient.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialDiscovery.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialTargetResolver.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotConfigReader.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotConfigReader.Models.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredential.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotQuotaResponse.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotQuotaSnapshotDto.cs`
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotQuotaParser.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotUsageProviderTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotApiClientTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotCredentialDiscoveryTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotConfigReaderTests.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotQuotaParserTests.cs`

## Observability and recovery

- Operational signal: non-sensitive provider source classification, Snapshot status, ErrorDescription guidance, and UsageBlock; never log tokens, config contents, or response bodies.
- Recovery: disable the provider registration if needed. Do not delete/overwrite Copilot CLI, gh, VS Code, or credential-manager state. If IP-01 is unavailable, keep the config source disabled and report the gap rather than weakening validation.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Implemented the Copilot Infrastructure provider, strict credential discovery, validated Windows target derivation, JSONC reader boundary, bounded HTTP client, open-map quota parser, status/overage mapping, and focused tests. The default plaintext config fallback remains disabled because IP-01 could not produce a sanitized official fixture or verify a token property.
- Changed files: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredential.cs`, `CopilotCredentialDiscovery.cs`, `CopilotCredentialTargetResolver.cs`, `CopilotConfigReader.cs`, `CopilotConfigReader.Models.cs`, `CopilotApiClient.cs`, `CopilotQuotaResponse.cs`, `CopilotQuotaSnapshotDto.cs`, `CopilotQuotaParser.cs`, `CopilotUsageProvider.cs`; tests under `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/` for credentials, config, HTTP, parser, and provider status/error behavior.
- Checks: current Infrastructure build exit 0 with the pre-existing NU1903 and xUnit1051 advisories; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Copilot*"` — 24 passed; same command without filter — 324 passed; Core tests — 43 passed; `git diff --check` — clean.
- Validated state: .NET SDK 10.0.400/native Microsoft.Testing.Platform, Infrastructure project build and full test project pass. HTTP tests use fake handlers only; credential tests use fake stores and temporary files; no live Copilot endpoint or real token was used. Installed official Copilot CLI 1.0.83 was run with an isolated `COPILOT_HOME` and a deliberately fake token; it returned 401 without creating a config. The existing config was inspected only for property names/state shape and no secret value was read into the repository.
- Open items: IP-01 remains a concrete acceptance gap: the official CLI's plaintext fallback property is undocumented and could not be verified, so `CopilotConfigReader` accepts a token only when a caller supplies an explicit verified property path. T05 may use keychain/env/gh paths; enabling plaintext fallback requires a sanitized official fixture and a follow-up change. App registration and integrated/manual acceptance remain for T05/T06.

### ADR candidates

None - direct TechSpec implementation; IP-01 is an explicit prerequisite gap, not an architectural decision.
