# Implementation plan — Refactoring orphaned public members

## Stable sources

- PRD: `tasks/prd-arch-20260912-01-dead-exports/prd.md`
- TechSpec: `tasks/prd-arch-20260912-01-dead-exports/techspec.md`

> Common sources before the task and mutable state; read order does not guarantee a cache hit.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Four orphaned public members removed, build green | — | T02 |
| T02 | Focused Copilot/Codex tests pass with the members gone | T01 | — |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| R-01 | `prd.md#behaviors-to-preserve` | Budget members keep semantics | T01, T02 | `CopilotMetricsClientTests` |
| R-02 | `prd.md#behaviors-to-preserve` | `GetQuotaAsync` is the sole quota entry | T01, T02 | `CopilotApiClientTests` |
| R-03 | `prd.md#behaviors-to-preserve` | `DiscoverAsync` unchanged | T01, T02 | `CopilotCredentialDiscoveryTests` |
| R-04 | `prd.md#behaviors-to-preserve` | `DiscoverAsync` unchanged (Codex) | T01, T02 | `CodexAuthDiscoveryTests` |
| R-05 | `prd.md#behaviors-to-preserve` | No external consumer | T01 | `rtk dotnet build` |
| QA-01..QA-04 | `techspec.md#quality-profile` | Reference counts reach 0 | T01 | scoped `rtk rg` |
| TC-01..TC-03 | `techspec.md#safety-net` | Build + focused tests | T02 | MTP test commands |

## Tasks

- [T01 — Remove orphaned public members](done/task_01.md): delete the four unreferenced members.
- [T02 — Validate removals](done/task_02.md): build and run focused provider tests.

## Coverage gate

- Coverage: pass — all four dead-export findings (AA-01..AA-04) map to T01.
- Traceability: pass — R-01..R-05, QA-01..04, TC-01..03 mapped.
- Dependencies: pass — T01 → T02; no cycles.
- Atomicity: pass — T01 is a self-contained removal; T02 is verification.
- Executability: pass — scoped `rg` and MTP commands recorded.
- Validation profile: pass — E2E omitted for .NET desktop; MTP runner recorded.
- Idempotency: pass — removals are idempotent; re-running yields 0 hits.

## Assumptions and open items

- Assumption: no out-of-repo consumer of the `Infrastructure` public members (R-05).
- Open item: none blocking. AA-05/AA-06 remain outside this plan.
- Required environment: none — MTP test projects are local.

## State

- [x] T01 — completed
- [x] T02 — completed

## Problems and solutions

- T01: Removed the four orphaned members (`RemainingDispatches`, Copilot `GetUsageAsync`, Copilot `DiscoverCredentialAsync`, Codex `DiscoverAccountAsync`); 30 deletions, 0 insertions across 4 files. Verified QA-01..04 each return 0 hits, build 0 errors/0 warnings, Infrastructure 673 passed and Core 91 passed (`--minimum-expected-tests 1`). No new blocking hits; no unused usings introduced.
- T02: Independent verification on the same state — build 0 errors/0 warnings; focused classes passed CopilotApiClient 4, CopilotCredentialDiscovery 5, CodexAuthDiscovery 7, CopilotMetricsClient 17 (33 total) with `--minimum-expected-tests 1`; QA-01..04 still 0 hits. No source changed. Work checkboxes were left unticked because T02's write limit covered only the Handoff section; acceptance was verified and recorded there.
