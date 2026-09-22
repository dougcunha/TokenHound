# Implementation plan — MCP access to live provider metrics

## Stable sources

- PRD: `tasks/prd-06-mcp-metrics/prd.md` (approved at HIL 1, workflow `DEC-02`).
- TechSpec: `tasks/prd-06-mcp-metrics/techspec.md` (approved at HIL 2, workflow `DEC-13`).

Read the PRD, then the TechSpec, then the selected task. HIL 2 approval is required before implementation.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Safe live metrics projection with truthful provider states and unit tests | None | T02 |
| T02 | Local MCP HTTP/SSE host and real-protocol integration tests | T01 | T03 |
| T03 | Desktop lifecycle wiring and client documentation | T02 | Review |
| T05 | Separate used counts from remaining units (ACC-01, DEC-23/DEC-24) | T01–T03 | Re-review, then HIL 3 |

Each task runs in sequence in the coordinator session. No two tasks edit the same MCP contract in parallel.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| OBJ-01, US-01 | PRD outcomes/stories | Query metrics from a local MCP client | T01, T02, T03 | TC-01, TC-03, TC-06 |
| OBJ-02 | PRD outcomes | No added provider polling or credential disclosure | T01, T02 | TC-01, TC-02, TC-04 |
| US-02 | PRD stories | Inspect one provider and edge states | T01, T02 | TC-01, TC-03 |
| FR-01 | PRD functional | Server available during App lifetime at stable URL | T02, T03 | TC-03, TC-05, TC-06 |
| FR-02 | PRD functional | Legacy SSE transport works | T02 | TC-03 |
| FR-03 | PRD functional | List real enabled HUD providers with snapshots | T01, T02 | TC-01, TC-03, TC-06 |
| FR-04 | PRD functional | Look up one provider with explicit missing states | T01, T02 | TC-01, TC-03 |
| FR-05 | PRD functional | Status, fidelity, timestamps, quota and block state | T01, T02 | TC-01, TC-03 |
| FR-06 | PRD functional | Copilot billing and Cline metrics | T01 | TC-01 |
| FR-07 | PRD functional | Read current cache without dispatch | T01, T02 | TC-02, TC-03 |
| FR-08 | PRD functional | Document MCP client setup | T03 | README/doc review, TC-06 |
| NFR-01 | PRD non-functional | Local exposure and Host/Origin protection | T02 | TC-04 |
| NFR-02 | PRD non-functional | No credential or private data in output | T01, T02 | TC-01, TC-04 |
| NFR-03 | PRD non-functional | Nullable values and degraded states stay truthful | T01 | TC-01 |
| NFR-04 | PRD non-functional | MCP failure does not stop HUD | T02, T03 | TC-04, TC-06 |
| NFR-05 | PRD non-functional | Concurrent reads and orderly shutdown | T01, T02, T03 | TC-05 |
| DEC-04, CMP-07 | TechSpec decisions/components | SDK dependency in Infrastructure | T02 | Project build and TC-03 |
| DEC-05, DEC-09, DEC-11, CMP-01 | TechSpec decisions/components | Stateful SSE host, local security and bounded traffic | T02 | TC-03, TC-04 |
| DEC-06, DEC-07, CMP-03, CMP-04, CMP-05 | TechSpec decisions/components | Allowlisted projection and last-success timestamp | T01 | TC-01, TC-02 |
| DEC-08, CMP-08 | TechSpec decisions/components | Stable URL and documentation | T02, T03 | TC-03, doc review |
| DEC-10, CMP-06 | TechSpec decisions/components | App startup and async stop | T03 | App build, TC-05, TC-06 |
| DEC-12 | TechSpec decisions | Keep new App logic in partial file | T03 | File measurement and diff review |
| CMP-02 | TechSpec components | Register exactly two structured tools | T02 | TC-03 |
| TC-01, TC-02 | TechSpec tests | Projection, privacy, and no-dispatch checks | T01 | MCP unit classes in Infrastructure.Tests |
| TC-03, TC-04, TC-05 | TechSpec tests | Real SSE, host security, concurrency, and shutdown | T02, T03 | MCP integration classes and manual shutdown |
| TC-06 | TechSpec tests | Visible desktop comparison | T03, HIL 3 | Manual script executed 2026-09-22 (workflow DEC-22); ACC-01 found |
| ACC-01, DEC-24, CMP-09, TC-07 | Workflow acceptance, TechSpec | Used count never reported as remaining; HUD text unchanged | T05 | TC-07; re-run TC-06 step 2 for Antigravity |

## Tasks

- [T01 — Project safe current metrics](done/task_01.md): map visible real providers to an explicit, testable MCP response without provider fetches.
- [T02 — Serve metrics through local MCP HTTP/SSE](done/task_02.md): host two structured tools and prove transport, security, and shutdown with a real MCP client.
- [T03 — Wire desktop lifetime and document connection](done/task_03.md): start and stop the host with TokenHound and provide an executable client guide.
- [T05 — Separate used counts from remaining units](done/task_05.md): fix ACC-01 at the root per DEC-23/DEC-24.

## Coverage gate

- Coverage: pass. OBJ-01–02, US-01–02, FR-01–08, NFR-01–05, CMP-01–08, DEC-04–12, and TC-01–06 have destinations.
- Traceability: pass. Each task links to PRD and TechSpec IDs; manual TC-06 remains a HIL 3 acceptance obligation.
- Dependencies: pass. `T01 -> T02 -> T03` is acyclic; shared contracts are changed sequentially.
- Atomicity: pass. Each task produces a reviewable behavior and its relevant tests or build evidence.
- Executability: pass subject to HIL 2 and the listed environments. NuGet restore, .NET build, and local HTTP tests are specified; visible desktop evidence needs Windows MCP tools or user execution.
- Validation profile: pass. Scoped MTP commands enforce `--minimum-expected-tests 1`; E2E omitted by .NET desktop policy. The manual HUD script is in the TechSpec.
- Idempotency: pass. Re-running a task's read-only checks or builds does not change provider state; server tests use ephemeral ports and stop their hosts.

## Assumptions and open items

- Assumption: implementation follows the approved PRD and the TechSpec's fixed local URL, two-tool contract, and legacy SSE opt-in once HIL 2 approves them.
- HIL 2 technical approval and implementation authorization were recorded in workflow `DEC-13`.
- Required environment: NuGet access to restore `ModelContextProtocol.AspNetCore` 2.2.0; Windows/.NET 10 for the App build; local loopback networking for MCP integration tests. These are routine implementation prerequisites under HIL 2.
- Required environment for TC-06: interactive Windows desktop with Windows MCP `App` and `Screenshot` tools or user execution. Those tools are unavailable in this host; the manual result stays pending for HIL 3.

## State

- [x] T01 — reconciled after `codereview_1/CR-01` through `codereview_1/done/task_04.md`; see `done/task_01.md` for prior and corrected evidence. Scoped MCP tests passed 20/20 on the integrated state.
- [x] T02 — complete; see `done/task_02.md` for handoff, 13 integration tests, and workflow `DEC-16`.
- [x] T03 — complete; see `done/task_03.md`. The manual TC-05/TC-06 script was executed 2026-09-22 (workflow DEC-22).
- [x] T05 — complete after validation reconciliation; see `done/task_05.md` and `codereview_3/done/task_06.md`. ACC-01 fixed, manual step 2 re-run for Antigravity, and mandatory scoped Antigravity tests now pass 41/41; independent re-review pending.

## Problems and solutions

- T02: SDK 2.2.0 marks `IdleTimeout` and `MaxIdleSessionCount` obsolete (`MCP9006`). Exception HIL `DEC-16` dropped both settings; SDK defaults apply to stateful Streamable HTTP sessions, and legacy SSE sessions end with their GET stream.
- T01: independent review `codereview_1/CR-01` found that a concurrent MCP read can pair a newly published snapshot with the previous or cleared last-success timestamp. T01 was reopened with its original contract and handoff preserved; correction T04 is linked to this obligation. Revalidate T02 and T03 integration evidence after the store/reader correction.
- T01 was completed again after T04's paired-state correction. The 20 MCP tests revalidated T02's real SSE transport and projection; the App rebuild revalidated T03's compiled lifecycle wiring. TC-06 and the tray-exit half of TC-05 remain manual HIL 3 work.
- Acceptance: the manual script found ACC-01 (`gemini` used-request count exposed as `remainingUnits`). DEC-23 chose a root fix; the PRD and TechSpec were amended (DEC-24) and T05 was planned.
- T05 implementation completed: `UsedUnits` carries Antigravity's count; the HUD text is unchanged, and MCP emits `usedUnits`. At the original T05 handoff, a pre-existing live Antigravity test failed on fidelity (L-03); T06 later corrected that test contract.
- `codereview_3/CR-01` made T05's validation incomplete because that live test asserted `Official` whenever `agy` ran. T06 corrected the test's source contract, kept it live, and moved it to a separate file. The full `*Antigravity*` filter now passes 41/41; MCP passes 22/22. A new session must re-review the correction before HIL 3.
