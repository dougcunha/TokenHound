# Code review report — arch-20260912-01-dead-exports

## Summary

- Status: APPROVED
- Git scope: `Not delimited — see limitations` (no `--base`; reviewable set = the four working-tree-modified `src/TokenHound.Infrastructure` files identified by the T01/T02 handoffs)
- Previous review: —

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-arch-20260912-01-dead-exports/prd.md` | read |
| TechSpec | `tasks/prd-arch-20260912-01-dead-exports/techspec.md` | read |
| Manifest | `tasks/prd-arch-20260912-01-dead-exports/tasks.md` | read |
| Implementation | `done/task_01.md`, `done/task_02.md` handoffs + working-tree diff of 4 files | limited (no base commit) |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| R-01 | Budget `TryAcquire`/`CanDispatch`/`DispatchesUsed` semantics preserved; only `RemainingDispatches` removed | `CopilotPassDispatchBudget.cs` (declaration removed) | `CopilotMetricsClientTests` | conformant | handoff: 17 passed; `rtk rg RemainingDispatches src` = 0 |
| R-02 | `GetQuotaAsync` remains the sole quota entry; alias removed | `CopilotApiClient.cs` (`GetUsageAsync` + doc removed) | `CopilotApiClientTests` | conformant | handoff: 4 passed; scoped `rtk rg GetUsageAsync` = 0 |
| R-03 | `DiscoverAsync` unchanged; alias removed | `CopilotCredentialDiscovery.cs` (`DiscoverCredentialAsync` removed) | `CopilotCredentialDiscoveryTests` | conformant | handoff: 5 passed; scoped `rtk rg DiscoverCredentialAsync` = 0 |
| R-04 | `DiscoverAsync` unchanged (Codex); alias removed | `CodexAuthDiscovery.cs` (`DiscoverAccountAsync` removed) | `CodexAuthDiscoveryTests` | conformant | handoff: 7 passed; `rtk rg DiscoverAccountAsync src` = 0 |
| R-05 | No external consumer of removed members; build green | all four files | build | conformant | handoff: 7 projects, 0 errors/0 warnings (application assembly; see limitations) |
| DEC-01..DEC-04 | Delete each member, nothing else | 30 deletions, 0 insertions, 4 files | diff review | conformant | `rtk git diff --stat -- src` = exactly the 4 files |
| CMP-01..CMP-04 | Only the four listed components change | same 4 files | diff review | conformant | `rtk git status --porcelain -- src tests` = 4 modified, no additions |
| QA-01..QA-04 | Scoped reference counts reach 0 | removals | profile commands | conformant | all four `rtk rg` return 0 hits (run in this review) |
| TC-01..TC-03 | Build + focused provider tests | removals | MTP focused classes | conformant | handoff: 33 focused passed; full 673 + 91 passed |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `AGENTS.md` (English, no unrequested comments, one class per file) | OK | diff is deletion-only; no code/comments added |
| `no-workarounds` | OK | behavior preserved; no suppression or shim introduced |
| `dotnet-efficient-validation` / MTP filters after `--` with `--minimum-expected-tests 1` | OK | handoff commands match the documented MTP form |
| `repository-cli-efficiency` | OK | `rtk rg` scoped; profile sweep only |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No `RemainingDispatches` reference | blocking | `rtk rg -n "RemainingDispatches" src` | 0 (baseline 1) | OK |
| QA-02 | No Copilot `GetUsageAsync` alias | blocking | `rtk rg -n "GetUsageAsync" src/TokenHound.Infrastructure/Providers/Copilot` | 0 (baseline 1) | OK |
| QA-03 | No Copilot `DiscoverCredentialAsync` alias | blocking | `rtk rg -n "DiscoverCredentialAsync" src/TokenHound.Infrastructure/Providers/Copilot` | 0 (baseline 1) | OK |
| QA-04 | No `DiscoverAccountAsync` | blocking | `rtk rg -n "DiscoverAccountAsync" src` | 0 (baseline 1) | OK |

- Terrain baseline: applied from TechSpec `#quality-profile` (each rule baseline = 1 declaration).
- Hits discounted by baseline: 4 (the pre-existing declarations being intentionally removed).
- Reservations accumulated in the feature: 0.
- Suggested escalation: no trigger fired.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 delete `RemainingDispatches` | YES | diff `CopilotPassDispatchBudget.cs` |
| DEC-02 delete `GetUsageAsync` + doc | YES | diff `CopilotApiClient.cs` |
| DEC-03 delete `DiscoverCredentialAsync` | YES | diff `CopilotCredentialDiscovery.cs` |
| DEC-04 delete `DiscoverAccountAsync` | YES | diff `CodexAuthDiscovery.cs` |
| PRD constraint: no behavioral change | YES | deletion-only diff; build + test suites green |
| PRD constraint: preserve XML docs of remaining members | YES | only the removed members' docs were deleted |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | 4 members removed; QA 0; build 0/0; Infrastructure 673 + Core 91 passed |
| T02 | `done/task_02.md` | COMPLETE | Verification on same state; focused 33 passed; QA 0; no source changed |

## Executed validations

- Profile and exclusions: .NET 10 / xUnit v3 via MTP; E2E omitted by the .NET desktop policy (no UI surface touched).
- Validated state: working tree over current `HEAD`, Debug, `net10.0`, SDK 10.0.400 per `global.json`. No source changes occurred after the handoffs.
- Reused evidence: T01 and T02 handoffs (build + full and focused suites) — the reviewable tree is byte-identical to the state they validated; nothing was edited between their runs and this review.
- Manual acceptance: none required by the TechSpec (compile-time surface only).

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk rg -n "RemainingDispatches" src` | passed (0 hits) | QA-01 |
| `rtk rg -n "GetUsageAsync" src/TokenHound.Infrastructure/Providers/Copilot` | passed (0 hits) | QA-02 |
| `rtk rg -n "DiscoverCredentialAsync" src/TokenHound.Infrastructure/Providers/Copilot` | passed (0 hits) | QA-03 |
| `rtk rg -n "DiscoverAccountAsync" src` | passed (0 hits) | QA-04 |
| `rtk dotnet build` (reused from T01) | passed (0 errors/0 warnings) | R-05, TC-01 |
| `rtk dotnet test … Infrastructure.Tests … --minimum-expected-tests 1` (reused) | passed (673) | R-01..R-04, TC-02/TC-03 |
| focused `--filter-class` Copilot/Codex classes (reused) | passed (33) | R-01..R-04, TC-02/TC-03 |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| — | — | — | No findings: no non-conformance, no failing mandatory test, no new or aggravated blocking profile hit. | — | — |

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| — | — | First review of this feature. |

## Limitations and open items

- No `--base` was supplied, so the reviewable set is bounded by the task handoffs and the working tree rather than a commit range. The diff of `src`/`tests` contains exactly the four intended files, so this limitation does not affect the opinion.
- The worktree contains unrelated pre-existing changes excluded from this review: modifications under `.agents/skills/**` and deletions of legacy `tasks/prd-provider-opencode`, `prd-settings-02-cadence-and-retries`, `prd-system-tray-notifyicon`, `prd-user-settings-persistence` trees. They are not part of this feature and were not touched by its executors.
- R-05 (no external consumer) rests on the build succeeding and the repository being an application assembly; there is no package-publishing proof. If `TokenHound.Infrastructure` is ever consumed out-of-repo, the four removed `public` members would need to be re-added.
- Process note (non-blocking): T02's Work checklist remained unticked because its write authorization covered only the Handoff section; all Work items were executed and are evidenced in `done/task_02.md`. The manifest state is correct.

## Conclusion

All obligations of `arch-20260912-01-dead-exports` are conformant: the four orphaned `public` members are removed with 30 deletions and no insertions, the blocking quality profile (QA-01..QA-04) is at zero hits against its recorded baseline, links and task states are consistent, and build plus full/focused MTP suites pass on the reviewed state. No new blocking hit, no missing evidence, and no reservation trigger. **APPROVED.** Global follow-up on the wider audit remains with the caller; the remaining planned workstreams (`03`, `04`, `05`) are unaffected.
