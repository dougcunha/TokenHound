# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-copilot/codereview_001/codereview.md`
2. `tasks/prd-provider-copilot/task_03.md`
3. This file

Use the current rejected review and T03 handoff. Do not edit `codereview_001/codereview.md` or guess an undocumented property.

---

# T08 - Resolve the verified Copilot plaintext-config fallback gap

## Outcome

The Copilot CLI plaintext fallback is either enabled through one exact property proven by a sanitized official fixture, with read-only and precedence tests, or remains disabled with IP-01 explicitly pending when the required fixture cannot be obtained.

## Dependencies and boundaries

- Depends on: T03 and IP-01; T07 is independent
- Unblocks: Re-review of `codereview_001` for CR-02 and the provider acceptance decision
- In scope: isolated official CLI fixture acquisition, exact property verification, allowlisted default wiring, focused credential/config tests, and the T03 handoff update
- Out of scope: recursive token-shaped searches, guessed property names, credential refresh/login, real-token storage, changes to credential precedence, endpoint changes, and deferred HIL1 decisions

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_001/CR-02` | `codereview.md#findings` | The official plaintext config property was not verified and the default fallback remains unavailable. |
| DEC-03, IP-01 | `techspec.md#technical-decisions`, `techspec.md#risks-and-open-items` | Credential target/property evidence must come from validated official state before the fallback is enabled. |
| FR-02, AC-09 | `prd.md#functional-requirements`, `prd.md#acceptance-criteria` | Credential precedence must include the documented fallback while remaining read-only. |
| TC-02 | `techspec.md#test-approach` | Credential precedence, config fixture, and no-write behavior require automated evidence. |

## Requirements

- Obtain a sanitized `config.json` produced by the installed official Copilot CLI in an isolated `COPILOT_HOME`; record the CLI version, isolation context, and fixture provenance without retaining a real token.
- Verify one exact JSON/JSONC property path from that fixture. The path must be allowlisted explicitly; do not infer it from a token-shaped value, recursively search the document, or use a guessed name.
- Wire only the verified path into the default `CopilotConfigReader` fallback while retaining exact-path validation for tests and rejecting nearby or unverified properties.
- Preserve the existing shared, read-only file access and prove the borrowed config remains byte-for-byte unchanged after discovery.
- Preserve the existing credential order: environment variables, read-only `gh`, Copilot keychain, then the verified config fallback; lower-priority sources must not be probed after a usable higher-priority source.

## Context to recover on demand

- TechSpec: `integrations-and-interfaces#credential-sources`, `technical-decisions#DEC-03`, `test-approach#TC-02`, and `risks-and-open-items#IP-01`
- Rules/skills: credential ownership and read-only file-access invariants, `repository-cli-efficiency`, `no-workarounds`, `dotnet-efficient-validation`
- Code: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotConfigReader.cs` - exact-path JSONC parsing, `COPILOT_HOME`, and shared reads
- Code: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotCredentialDiscovery.cs` - source precedence and fallback ordering
- Tests: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotConfigReaderTests.cs` and `CopilotCredentialDiscoveryTests.cs` - current explicit-path and no-write coverage

## Work

- [ ] T08.1 Run or inspect the official Copilot CLI only within an isolated `COPILOT_HOME`, sanitize all token values, and record the exact property path and provenance. If no official fixture can be obtained, stop and mark IP-01 pending without changing the fallback.
- [ ] T08.2 Add the sanitized fixture at `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/Fixtures/official-config.jsonc` and document its provenance in the T08 handoff without recording secrets.
- [ ] T08.3 Enable the verified property as the default allowlisted fallback in `CopilotConfigReader` while preserving `COPILOT_HOME`/default-home resolution, shared reads, malformed-state handling, and explicit path rejection.
- [ ] T08.4 Extend config and credential-discovery tests for the official fixture, exact property path, precedence, no-write behavior, and rejection of unverified token-shaped properties.
- [ ] T08.5 Run the focused Infrastructure validation, update `task_03.md` with the result or explicit pending gap, and request a fresh review decision without changing `codereview_001/codereview.md`.

## Acceptance criteria

- The fixture provenance identifies an official CLI run and isolated `COPILOT_HOME`; no real token is committed, logged, or stored in task artifacts.
- The default fallback is enabled only for the exact property path proven by the fixture; a changed, nearby, nested, or arbitrary token-shaped property is not accepted.
- Credential discovery selects the verified config fallback only after all higher-priority usable sources are unavailable and does not probe lower sources after selection.
- The config file remains unchanged after reading, malformed or inaccessible config remains unavailable, and no credential-owned file is written or refreshed.
- If fixture evidence cannot be obtained, the task remains pending, the fallback stays disabled, IP-01 is preserved as an open item, and no guessed implementation is introduced.

## Verification

- Unit: official sanitized config parsing, exact allowlist, malformed/missing config, source precedence, and unchanged-file assertions.
- Integration: isolated `COPILOT_HOME` discovery with a fake sanitized token and no real credential-manager or live endpoint access.
- E2E: omitted by .NET desktop policy.
- Manual: official CLI fixture provenance review by the coordinator or provider reviewer; no live token disclosure.
- Environment dependency: installed official Copilot CLI, isolated `COPILOT_HOME`, permission to inspect its generated config read-only, and .NET SDK 10.0.400/native MTP. Failure to obtain the fixture is an explicit pending item.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Copilot*"`.
- Expected evidence: sanitized fixture, provenance/path record, focused test count and exit code, unchanged-file assertion, and updated T03 handoff.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotConfigReader.cs` - wire the verified default allowlist only after fixture evidence.
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotConfigReaderTests.cs` - fixture and exact-path/no-write coverage.
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotCredentialDiscoveryTests.cs` - fallback precedence coverage.
- Modify: `tasks/prd-provider-copilot/task_03.md` - record the resolved or still-pending IP-01 state.
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/Fixtures/official-config.jsonc` only after it is sanitized and provenance is recorded.
- Do not modify: `tasks/prd-provider-copilot/codereview_001/codereview.md`.

## Observability and recovery

- Operational signal: non-sensitive source classification, focused test output, fixture provenance, and unchanged-file hashes or comparisons; never log token values or config bodies.
- Recovery: If the fixture is unavailable or its property path is ambiguous, leave the default fallback disabled and return the task as pending. If implementation or tests fail, preserve the evidence and correct the cause rather than broadening the parser or weakening assertions.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: `tasks/prd-provider-copilot/task_08.md` handoff only; no provider source, test, credential-owned file, fixture, or review report changed.
- Checks: In isolated temporary `COPILOT_HOME` directories, `copilot --version` reported official CLI 1.0.83 with exit 0, `copilot --help` exited 0 with `config_created=false`, and `copilot login --help` exited 0 with `config_created=false`. Existing review evidence for the unchanged provider state remains valid: the focused Copilot run passed 37 tests. No real token was read, stored, or logged.
- Validated state: The official CLI is installed, but this session produced no sanitized config fixture and exposed no exact plaintext token property. `CopilotConfigReader` therefore remains correctly disabled by default; no guessed allowlist or recursive token search was introduced.
- Open items: CR-02 and IP-01 remain open. A successful isolated official fixture, exact property-path provenance, fixture test, default allowlist wiring, and focused regression evidence are still required. T08 remains at the root and is not approved for `done/`.
