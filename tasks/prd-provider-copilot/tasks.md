# Implementation plan: provider-copilot

## Stable sources

- PRD: `tasks/prd-provider-copilot/prd.md`
- TechSpec: `tasks/prd-provider-copilot/techspec.md`
- Workflow and authorization: `tasks/prd-provider-copilot/workflow.md`, DEC-0004
- Authoritative provider specification: `docs/specs/11-PROVIDER-COPILOT.md`
- Shared resilience specification: `docs/specs/01-READING-STRATEGY-RESILIENCE.md`
- Windows credential and file-access specification: `docs/specs/02-WINDOWS-CREDENTIALS-SECURITY.md`
- Validation policy: `AGENTS.md` and the .NET profile recorded in `techspec.md`

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Pure Core contracts and policies for unsupported status, fractional remainder, retention, cadence, and deadlines | — | T02, T03 |
| T02 | Durable archive and UsageStore dispatch/activity orchestration | T01 | T03, T04, T05 |
| T03 | Copilot credentials, HTTP client, quota parser, and provider mapping | T01, T02, IP-01 | T05 |
| T04 | Copilot activity monitor and host detection | T02 | T05 |
| T05 | Existing Notch/App registration and generic Copilot presentation | T02, T03, T04 | T06 |
| T06 | Integrated validation and manual desktop acceptance | T01, T02, T03, T04, T05 | — |

T03 and T04 are independent after T02 and have disjoint source and test ownership. T05 starts only after both provider and activity deliveries are approved. T06 is serialized after all implementation tasks because it shares build outputs and validates the integrated desktop state.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Copilot provider ID, name, official fidelity, and monthly premium-interaction headline | T03, T05 | CopilotUsageProviderTests and generic Provider Ring presentation |
| FR-02 | `prd.md#functional-requirements` | Strict credential precedence and VS Code identity-only boundary | T03 | CopilotCredentialDiscoveryTests and CopilotConfigReaderTests |
| FR-03 | `prd.md#functional-requirements` | Lightweight GET endpoint, headers, and 15-second timeout | T03 | CopilotApiClientTests with fake HttpMessageHandler |
| FR-04 | `prd.md#functional-requirements` | Open quota map selection and unsupported empty result | T03 | CopilotQuotaParserTests and CopilotUsageProviderTests |
| FR-05 | `prd.md#functional-requirements` | Honest fraction, fractional remainder, and top-level reset | T01, T03 | Domain serialization tests and quota/provider tests |
| FR-06 | `prd.md#functional-requirements` | Permitted and disallowed overage semantics | T01, T03 | Core UsageBlock tests and provider mapping tests |
| FR-07 | `prd.md#functional-requirements` | Status/history matrix for success, auth, unsupported, stale, and rate limit | T02, T03, T06 | Provider, archive, and integrated validation evidence |
| FR-08 | `prd.md#functional-requirements` | Terminal sign-in guidance without pasted PAT requests | T03 | Credential/provider error tests and redacted diagnostics assertions |
| FR-09 | `prd.md#functional-requirements` | Two-second activity poll, freshness threshold, and qualifying host | T04 | CopilotActivityMonitorTests and host detector tests |
| FR-10 | `prd.md#functional-requirements` | 120 ms debounce and read-only activity files | T04 | Burst-write and unchanged-file assertions |
| FR-11 | `prd.md#functional-requirements` | 60/300-second quota cadence and deadline gate | T01, T02, T06 | Core policy tests, UsageStore tests, and command evidence |
| FR-12 | `prd.md#functional-requirements` | Existing Snapshot, Provider Ring, status, activity, and sign-in path | T03, T05, T06 | Provider/App tests and manual Notch acceptance |
| NFR-01 | `prd.md#non-functional-requirements` | Borrow credentials and files read-only | T03, T02, T06 | File-sharing, no-write, and archive tests |
| NFR-02 | `prd.md#non-functional-requirements` | Preserve official values without invented denominators or zeros | T01, T03, T05 | Parser/model tests and architecture review |
| NFR-03 | `prd.md#non-functional-requirements` | Persist and honor 429 deadlines including Retry-After zero | T01, T02, T06 | RateLimitPolicy and restart integration tests |
| NFR-04 | `prd.md#non-functional-requirements` | Open-map and schema resilience with stale retention | T02, T03, T06 | Parser, provider, and archive tests |
| NFR-05 | `prd.md#non-functional-requirements` | Timing, cadence, debounce, and HTTP timeout | T02, T03, T04, T06 | Timing fixtures and project-scoped tests |
| NFR-06 | `prd.md#non-functional-requirements` | Core purity and Infrastructure ownership | T01, T03, T04, T05, T06 | Project build and dependency inspection |
| NFR-07 | `prd.md#non-functional-requirements` | Non-activating, click-through HUD behavior | T05, T06 | App build and Windows MCP manual script |
| AC-01 | `prd.md#acceptance-criteria` | Finite premium interaction response | T03, T05 | TC-01, TC-03, TC-10 |
| AC-02 | `prd.md#acceptance-criteria` | Alternate finite open-map category | T03 | TC-03 |
| AC-03 | `prd.md#acceptance-criteria` | Only unlimited or non-entitled categories | T03, T05 | TC-03, TC-04 |
| AC-04 | `prd.md#acceptance-criteria` | 401 and both 403 classes | T03 | TC-02, TC-04 |
| AC-05 | `prd.md#acceptance-criteria` | Timeout, schema drift, and stale retention | T02, T03 | TC-04, TC-06, TC-12 |
| AC-06 | `prd.md#acceptance-criteria` | Positive and zero Retry-After deadline | T01, T02 | TC-06, TC-07 |
| AC-07 | `prd.md#acceptance-criteria` | Activity signal combinations and debounce | T04 | TC-08, TC-09 |
| AC-08 | `prd.md#acceptance-criteria` | Overage and exhausted quota block semantics | T01, T03 | TC-05 |
| AC-09 | `prd.md#acceptance-criteria` | Multiple credentials and read-only discovery | T03 | TC-02 |
| AC-10 | `prd.md#acceptance-criteria` | Busy/idle cadence, forced refresh, and Notch interaction | T02, T05, T06 | TC-07, TC-09, TC-11 |
| DEC-01 | `techspec.md#technical-decisions` | Keep provider integrations in Infrastructure and retain existing contracts | T03, T04, T05 | Project boundaries and compile evidence |
| DEC-02 | `techspec.md#technical-decisions` | Add pure Unsupported and RemainingValue semantics | T01 | Core tests |
| DEC-03 | `techspec.md#technical-decisions` | Credential precedence, target resolution, JSONC fixture, and no writes | T03 | Credential/config tests and IP-01 evidence |
| DEC-04 | `techspec.md#technical-decisions` | HTTP client and open-map parser | T03 | TC-01 through TC-04 |
| DEC-05 | `techspec.md#technical-decisions` | Durable snapshot retention | T01, T02, T03 | TC-06 and archive/provider tests |
| DEC-06 | `techspec.md#technical-decisions` | Shared persistent rate-limit gate | T01, T02 | TC-06, TC-07, TC-12 |
| DEC-07 | `techspec.md#technical-decisions` | Activity freshness and host liveness | T04 | TC-08 |
| DEC-08 | `techspec.md#technical-decisions` | Separate activity poll and existing ring event path | T02, T04, T05 | TC-09 and manual activity evidence |
| DEC-09 | `techspec.md#technical-decisions` | MTP validation and desktop E2E omission | T06 | Validation commands and manual script |
| DEC-10 | `techspec.md#technical-decisions` | Preserve product deferrals and generic Notch conventions | T05, T06 | Scope review and manual acceptance |
| CMP-01..CMP-04 | `techspec.md#components-and-flow` | Provider, HTTP, credential, and quota components | T03 | Provider test suite |
| CMP-05..CMP-06 | `techspec.md#components-and-flow` | Activity and process-host components | T04 | Activity/host test suite |
| CMP-07..CMP-09 | `techspec.md#components-and-flow` | Archive, UsageStore, and pure policy components | T01, T02 | Core and Infrastructure engine tests |
| CMP-10 | `techspec.md#components-and-flow` | Existing App and Notch integration | T05 | App build and manual script |
| TC-01..TC-05 | `techspec.md#test-approach` | Provider, credentials, parser, status, and overage evidence | T03 | Copilot Infrastructure tests |
| TC-06..TC-07 | `techspec.md#test-approach` | Archive restart and rate-limit deadline evidence | T01, T02 | Core policy and UsageStore integration tests |
| TC-08 | `techspec.md#test-approach` | Activity monitor and host evidence | T04 | Activity monitor tests |
| TC-09 | `techspec.md#test-approach` | Activity polling and event routing | T02, T05 | UsageStore activity tests and App integration |
| TC-10 | `techspec.md#test-approach` | Core serialization and purity | T01 | Core tests and build |
| TC-11 | `techspec.md#test-approach` | App compilation and manual HUD acceptance | T05, T06 | App build and Windows MCP evidence |
| TC-12 | `techspec.md#test-approach` | Archive atomicity and unrelated-state preservation | T02 | UsageArchiveTests |

## Tasks

- [T01 - Establish pure Core quota and resilience policies](task_01.md): Add the additive Core model and policy seams with focused tests while keeping Core free of external dependencies.
- [T02 - Add durable archive and UsageStore orchestration](task_02.md): Persist last-good readings and deadlines, gate dispatches, and provide the separate activity polling/event plumbing.
- [T03 - Implement Copilot provider and credential-safe HTTP path](task_03.md): Add credential discovery, verified config/keychain handling, HTTP mapping, open quota parsing, and provider tests.
- [T04 - Implement Copilot activity and host detection](task_04.md): Infer activity from read-only file freshness plus qualifying process liveness with debounce and focused tests.
- [T05 - Integrate Copilot into the existing App and Notch](task_05.md): Register provider/activity services and route generic status/activity behavior without a new HUD surface.
- [T06 - Validate the integrated desktop feature](task_06.md): Run affected-project validation and perform the required Windows MCP manual acceptance without E2E automation.

## Coverage gate

- Coverage: pass. FR-01 through FR-12, NFR-01 through NFR-07, AC-01 through AC-10, DEC-01 through DEC-10, CMP-01 through CMP-10, and TC-01 through TC-12 map to one or more tasks.
- Traceability: pass. Every row points to a stable PRD or TechSpec section and names evidence owned by a task.
- Dependencies: pass. The graph is acyclic: T01 -> T02 -> {T03, T04} -> T05 -> T06, with T03 also depending on T01 and T02.
- Atomicity: pass. T01 through T05 are reviewable vertical deliveries with disjoint implementation/test ownership; T06 is validation-only and serialized.
- Executability: pass with IP-01. The tasks name files, commands, project scopes, manual steps, and the fixture prerequisite; no code task may silently guess the plaintext config schema.
- Validation profile: pass. Core and Infrastructure use project-scoped native MTP commands with `--minimum-expected-tests 1`; the WPF app is built separately; desktop E2E remains omitted by policy.
- Idempotency: pass. Each task reuses existing contracts, writes TokenHound-owned archive files atomically, and never writes borrowed credentials; rerunning tests uses isolated fixtures and temporary state.

## Assumptions and open items

- Assumption: HIL1-01 through HIL1-06 remain deferred exactly as recorded in DEC-0002; generic existing Notch conventions are the only presentation contract.
- Open item: IP-01 blocks only the Copilot plaintext config branch. T03 must obtain a sanitized official CLI fixture or return a concrete acceptance gap; it must not invent a property or use a recursive token-shaped search.
- Open item: the endpoint remains undocumented and unversioned. T03 must preserve explicit schema-drift handling and no fabricated values.
- Required environment: Windows 11, .NET SDK 10.0.400, installed Microsoft.Testing.Platform test runner, an approved read-only Copilot/gh session for manual acceptance, and Windows MCP App/Screenshot tools for T06. No live credential is stored in tests or artifacts.

## State

- [ ] T01 - pending
- [ ] T02 - pending
- [ ] T03 - pending
- [ ] T04 - pending
- [ ] T05 - pending
- [ ] T06 - pending

## Problems and solutions

- Three delegated task-planning attempts produced no artifacts and were stopped after reconciliation. The coordinator created this plan locally under the already approved DEC-0004 scope; no source, test, or product contract was changed by those attempts.
