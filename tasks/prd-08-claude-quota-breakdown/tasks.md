# Implementation plan — Claude quota breakdown

## Stable sources

- Approved PRD: `tasks/prd-08-claude-quota-breakdown/prd.md` (workflow DEC-01).
- TechSpec: `tasks/prd-08-claude-quota-breakdown/techspec.md`.
- Approved exception: `tasks/prd-08-claude-quota-breakdown/exception-01.md` (workflow DEC-10).
- Provider contract: `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §3.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Claude snapshots include valid additional quotas without invented data. | — | T02 |
| T02 | Claude details label every quota and ring summaries keep their base-window meaning. | T01 | HIL 3 manual acceptance |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| OBJ-01, US-01, FR-01 | `prd.md#functional-requirements` | Show reported additional quotas. | T01, T02 | TC-01, TC-05, TC-07 |
| OBJ-02, US-04, FR-04, FR-05 | `prd.md#functional-requirements` | Keep session and overall weekly identity through deduplication and rollover. | T01, T02 | TC-02, TC-06 |
| OBJ-03, US-02, FR-03, FR-06 | `prd.md#functional-requirements` | Omit null/invalid quotas, show valid zero, and avoid invented amounts. | T01, T02 | TC-01, TC-03, TC-05 |
| US-03, FR-07 | `prd.md#functional-requirements` | Keep profile readings independent. | T01, T02 | TC-04, TC-07 |
| FR-02, NFR-04 | `prd.md#functional-requirements` | Distinct accessible labels and scope text. | T02 | TC-05, TC-07 |
| FR-08, NFR-02 | `prd.md#functional-requirements` | Preserve stale windows and pre-dispatch 429 deadline. | T01 | TC-04 and existing `UsageStoreGatingTests` |
| NFR-01 | `prd.md#non-functional-requirements` | Never write or refresh credentials. | T01 | Existing credential path unchanged; diff review |
| NFR-03 | `prd.md#non-functional-requirements` | Never invent quota data or model identity. | T01, T02 | TC-03, TC-05 |
| NFR-05 | `prd.md#non-functional-requirements` | Keep Core pure while correcting its fraction contract. | T01, T02 | Core, Infrastructure, and App builds; no UI or OS dependency in Core |
| CMP-01, CMP-02, CMP-03, CMP-07 | `techspec.md#components-and-flow` | Preserve explicit fractions, parse, normalize, and integrate the usage response. | T01 | TC-01–TC-04 and Core model tests |
| CMP-04, CMP-05 | `techspec.md#components-and-flow` | Project labels and exact ring summaries. | T02 | TC-05–TC-07 |
| DEC-02–DEC-04, DEC-06, DEC-07, DEC-09 | `techspec.md#technical-decisions` | Data, Core contract, extraction, and rate-limit rules. | T01 | Core, mapper/provider checks, and quality gate |
| DEC-05 | `techspec.md#technical-decisions` | Claude-specific HUD presentation. | T02 | Row/ring checks and manual script |

## Tasks

- [T01 — Normalize Claude quota windows](done/task_01.md): Core preserves reported fractions without invented totals, and the provider emits one accurate window per quota through a 429.
- [T02 — Present Claude quota breakdown](done/task_02.md): details display distinct model/scope labels while ring metrics remain tied to the canonical session and overall weekly windows.

## Coverage gate

- Coverage: pass. Every PRD objective, story, FR, and NFR maps to a task and evidence; monetary extra usage and entitlement changes remain out of scope.
- Traceability: pass. TC-01–TC-07 and CMP-01–CMP-05 map to T01/T02; CMP-06 is the task tests themselves.
- Dependencies: pass. T02 uses T01's canonical window names; the DAG is acyclic and tasks run one at a time.
- Atomicity: pass. T01 ends with provider snapshots and tests; T02 ends with HUD projection and tests. The local mapper extraction belongs to T01.
- Executability: pass. SDK 10.0.401, native MTP, project paths, and Windows MCP desktop tools were identified. HIL 2 and EX-01 are approved; T01 can run in the next session.
- Validation profile: pass. Desktop .NET E2E is omitted; focused unit/integration tests and a manual HUD script preserve acceptance. Build/test commands follow `AGENTS.md`.
- Idempotency: pass. Each task has stable input artifacts and explicit output evidence; re-entry reconciles current files, test output, and handoff before writing.

## Assumptions and open items

- No blocking product or technical decision is open before HIL 2.
- Required environment: .NET SDK 10.0.401 and current restore assets for Core.Tests, Infrastructure.Tests, and App builds; if assets are absent, restore once. Manual acceptance requires a user-visible Windows desktop and an authenticated Claude profile through Windows MCP `App` and `Screenshot`.
- Live account data can change or be rate limited. A missing live optional quota is recorded as a manual acceptance limit, never replaced by a fixture claim.

## State

- [x] T01 — complete; handoff in `done/task_01.md` (REC-04 adoption).
- [x] T02 — complete; handoff in `done/task_02.md`. TC-07 manual HUD acceptance pending for HIL 3.

## Problems and solutions

- T01: an earlier session left unrecorded T01 code. The resuming coordinator adopted it after the user confirmed the takeover (workflow REC-04), then reviewed, completed, and validated it before marking the task done.
- T01: the EX-01 audit covered only production assignments. `McpMetricsReaderTests.Reads_KeepStaleProvenanceAndNeverFetchProvider` still asserted the old suppression: a fixture window with `UsedFraction = 0.8` and no total was expected to read `null`. `McpMetricsReader` passes the value straight through, so the expectation now asserts the explicit 0.8 is retained in the stale reading. This is a test consequence of the approved DEC-09 contract, not new scope.
- T02: the Windows MCP desktop tools were unavailable in the implementing session, so TC-07 could not run there. It is carried to HIL 3 as a pending manual acceptance, not replaced by fixtures.
