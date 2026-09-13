# Code review report - Shared provider failure mapping

## Summary

- Status: APPROVED
- Git scope: Not delimited - no `--base` was supplied; bounded by the T01-T04 handoffs and the current staged/worktree state at `HEAD` `312a0b32c8862321464f2c506a73d1e155f9b04b`
- Previous review: `tasks/prd-arch-20260912-04-provider-failure-mapping/codereview_1/codereview.md`

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-arch-20260912-04-provider-failure-mapping/prd.md` | read once; required sections present; acceptance evidence reconciled |
| TechSpec | `tasks/prd-arch-20260912-04-provider-failure-mapping/techspec.md` | read once; decisions, safety net, baseline, and quality profile present |
| Manifest | `tasks/prd-arch-20260912-04-provider-failure-mapping/tasks.md` | read; T01-T04 linked and marked complete |
| Handoffs | `done/task_01.md` through `done/task_04.md` | read; correction history, files, commands, IDs, and states checked |
| Previous review | `codereview_1/codereview.md` | read; CR-01 through CR-04 re-reviewed below |
| Implementation | Handoff-bounded staged/worktree files at `HEAD` above | limited but reviewable; unrelated dirty-worktree changes excluded |

The reviewable implementation set is `RateLimitedSnapshotFactory.cs`, `SnapshotFailureMapper.cs`, `CursorUsageProvider.cs`, `AntigravityUsageProvider.Snapshots.cs`, `ClaudeOAuthProvider.cs`, `OpenCodeUsageProvider.cs`, `CopilotUsageProvider.Snapshot.cs`, `CopilotHistoricalReportCollector.cs`, and `CursorUsageProviderTests.Failures.cs`. `CopilotBillingService.MapBillingFailure` and the existing provider test classes were reviewed as unchanged contract and validation evidence. Provider exception construction belongs to sibling PRD `arch-20260912-03` and is excluded from feature attribution, while its current status-code contract is covered by the composed provider tests.

All four manifest links resolve. T02 depends on T01, T04 depends on T01-T03, and the recorded completion order satisfies those dependencies. The only extra source under this PRD is the preserved prior review; there are no orphan task or handoff files.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| R-01 | Preserve Cursor 429 snapshot shape and policy deadline | `CursorUsageProvider.cs:209-239`; `RateLimitedSnapshotFactory.cs:26-82` | `CursorUsageProviderTests.Failures.cs:65-124` | conformant | Cursor class passed 9 tests; both `Retry-After: 0` and absent-header paths assert a future deadline. |
| R-02 | Preserve Antigravity 429 and 403 fall-through | `AntigravityUsageProvider.Snapshots.cs:136-167` | `AntigravityUsageProviderTests` | conformant | The explicit 403 hook returns `Ignored`; raw MTP run passed 11 tests covering the provider failure path. |
| R-03 | Preserve Claude windows after 429 | `ClaudeOAuthProvider.cs:226-250` | `ClaudeOAuthProviderTests` | conformant | The last-success windows are passed to the shared factory; 13 tests passed. |
| R-04 | Preserve OpenCode and Copilot explicit deadlines, statuses, and reasons | `OpenCodeUsageProvider.cs:136-145`; `CopilotUsageProvider.Snapshot.cs:23-55`; `CopilotHistoricalReportCollector.cs:276-296` | OpenCode, Copilot usage, billing, and historical classes | conformant | Focused runs passed 18, 11, 6, and 4 tests respectively; explicit provider-local snapshot builders remain. |
| R-05 | Preserve block timestamps and retry values for equal inputs | Shared factory delegates to unchanged `RateLimitPolicy`; persisted models/store unchanged | `RateLimitPolicyTests`; full Infrastructure project | conformant | Policy tests passed 12; `RateLimitPolicy.cs` has no worktree state; full run passed 675. |
| PRD constraint | Provider differences use explicit hooks instead of copied switches | Mapper hook in Cursor, Antigravity, Copilot usage, and Copilot historical paths | Focused provider classes | conformant | Five live provider mappings share `Classify`; each divergent status is named in one hook. |
| PRD constraint | Preserve the minimum retry floor and zero-header protection | `RateLimitedSnapshotFactory.Create` calls `RateLimitPolicy.CalculateDeadline` with the provider-owned count | Cursor failure and policy tests | conformant | Zero and absent `Retry-After` tests pass; no immediate-retry path was introduced. |
| PRD constraint | Do not change persisted state format | No Core model, archive schema, or state format is in the reviewable diff | Full Infrastructure project | conformant | 675 tests passed; persistence types are outside the feature changes. |
| PRD acceptance 1 | Check every R-NN item after refactoring | Matrix R-01 through R-05 | All focused classes | conformant | `prd.md:31` is checked with the same counts independently reproduced here. |
| PRD acceptance 2 | No silent behavior change | Shared core plus explicit provider translations | Full Infrastructure project | conformant | Current full MTP run passed 675 with no failures or skips. |
| PRD acceptance 3 | Provider failure tests pass with a nonzero minimum | Existing plus two corrected Cursor tests | Eight focused MTP commands | conformant | Every execution used `--minimum-expected-tests 1` and exited 0. |
| PRD acceptance 4 | Eliminate duplicate snapshot and mapping bodies | Two internal shared helpers | QA-01 and QA-02 commands | conformant | QA-01 is 2 to 0; QA-02 is 4 to 2 method definitions and meets its target. |
| DEC-01 | Shared parameterized builder returns snapshot and updated count | `RateLimitedSnapshotFactory.Create:26-55` | Cursor, Antigravity, Claude, policy tests | conformant | All specified inputs are explicit; count ownership remains with each provider. |
| DEC-02 | Shared mapper is keyed by status and credential with an Antigravity override | `SnapshotFailureMapper.Classify:43-59`; `ClassifyForbiddenAsIgnored:145-148` | Antigravity and other provider classes | conformant | Shared 401/403/429/default classification is present; Antigravity 403 remains explicit. |
| DEC-03 | Keep OpenCode and Copilot explicit-deadline builders | Existing provider-local builders remain | OpenCode and Copilot focused classes | conformant | Only classification was centralized; deadline sources and reason taxonomy were not forced into the factory. |
| DEC-04 | Do not alter `RateLimitPolicy` | No policy diff | `RateLimitPolicyTests` | conformant | Policy source is unchanged; 12 tests passed. |
| CMP-01 | Add an internal shared builder | `RateLimitedSnapshotFactory.cs` | Three provider classes | conformant | Internal static helper exists and is consumed by Cursor, Antigravity, and Claude. |
| CMP-02 | Cursor delegates builder and mapping | `CursorUsageProvider.cs:209-239` | Cursor class | conformant | Both delegations are present; 9 tests passed. |
| CMP-03 | Antigravity delegates and keeps 403 hook | `AntigravityUsageProvider.Snapshots.cs:136-167` | Antigravity class | conformant | Thin mapping delegate and shared factory call are present; 11 tests passed. |
| CMP-04 | Claude delegates with preserved windows | `ClaudeOAuthProvider.cs:226-250` | Claude class | conformant | Optional windows argument receives `_lastSuccessfulSnapshot?.LimitWindows`; 13 tests passed. |
| CMP-05 | OpenCode delegates only equivalent classification | `OpenCodeUsageProvider.cs:136-145` | OpenCode class | conformant | Provider-local reset/retry extraction remains; 18 tests passed. |
| CMP-06 | Copilot delegates equivalent mapping and keeps reason taxonomy | `CopilotUsageProvider.Snapshot.cs:23-55`; `CopilotHistoricalReportCollector.cs:276-296`; unchanged `CopilotBillingService.cs:153` | Three Copilot classes | conformant | Usage, billing, and historical focused runs pass; the non-HTTP billing decision remains local. |
| TC-01 | Cursor 429 with and without `Retry-After` | Cursor shared-factory path | `CursorUsageProviderTests.Failures.cs:65-124` | conformant | Both required scenarios execute in the 9-test class. |
| TC-02 | Antigravity 429 and 403 | Explicit ignored hook plus shared factory | `AntigravityUsageProviderTests` | conformant | Raw MTP total 11, all passed. |
| TC-03 | Claude 429 preserves windows | Optional factory windows argument | `ClaudeOAuthProviderTests` | conformant | 13 tests passed. |
| TC-04 | OpenCode and Copilot retain status/reason text | Provider translations after shared classification | Four focused provider/billing classes | conformant | 39 tests passed across OpenCode 18, Copilot usage 11, billing 6, and historical 4. |
| QA-01 | Remove duplicate private rate-limit snapshot builders | Shared factory | Exact profile search | conformant | HEAD baseline 2; current 0 hits (`rg` exit 1), target 0. |
| QA-02 | Reduce failure-mapping method definitions to thin delegates | Shared mapper; retained Antigravity and non-HTTP Copilot delegates | Exact profile baseline/current searches | conformant | HEAD baseline 4; current 2, target at most 2. Both residual methods existed at baseline. |
| QA-03 | Preserve rate-limit floor | Unchanged Core policy | `RateLimitPolicyTests` | conformant | 12 passed, 0 failed. |
| Sequencing | Advance builder, Claude, mapper, then full validation | T01-T04 completion and correction history | Task handoffs plus current full run | conformant | Dependencies are acyclic and all downstream evidence is present. |
| Compatibility | Keep contracts internal with no migration or persisted-format change | Internal helpers; unchanged public models | Build and full project | conformant | Build and 675 tests pass; no rollout or migration artifact is needed. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `sdd-review-code` source, link, task, state, and immutable-report rules | OK | All required artifacts and handoffs exist; `codereview_2` was the next free suffix. |
| Core purity and provider invariants | OK | Both helpers are in Infrastructure; `RateLimitPolicy` and persisted state are unchanged; zero `Retry-After` produces a future deadline. |
| C# structure and style | OK for reviewed delta | New files are 83 and 60 lines; new/changed methods are at most 30 lines; one new class per file; public contracts retain XML documentation. `OpenCodeUsageProvider.cs` remains pre-existing size debt at 326 lines versus 331 at HEAD and was reduced, not aggravated. |
| `dotnet-efficient-validation` and repository MTP rules | OK | SDK 10.0.401 selected from `global.json`; native MTP via `test.runner`; build once, then `--no-build --no-restore`; every test run enforced one expected test. |
| `repository-cli-efficiency` | OK | Graph, searches, diffs, and validation were limited to the PRD, handoff files, affected providers, and relevant tests. |
| `no-workarounds` | OK | Status differences are explicit hooks; cancellation behavior remains observable; no suppression, swallowed failure, immediate retry, or copied fallback conceals a changed contract. |
| Desktop .NET E2E policy | N/A by policy | E2E omitted. The TechSpec requires no manual acceptance. |
| Diff hygiene | OK | Scoped staged `git diff --check` returned exit 0. |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No duplicated private `CreateRateLimitedSnapshot` body | blocking | `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` | 0 current from baseline 2; 0 new/aggravated | OK |
| QA-02 | Failure mappings share one decision core | reservation | `rtk rg -n -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" src/TokenHound.Infrastructure/Providers` | 2 current from baseline 4; 0 new/aggravated | OK; residual baseline delegates are covered by DEC-02/DEC-03 |
| QA-03 | Rate-limit floor preserved | blocking | focused `RateLimitPolicyTests` | 12 passed | OK |

- Terrain baseline: applied from the TechSpec and independently reproduced against `HEAD` for QA-01 and QA-02; QA-03's pass baseline was reproduced by the focused policy run.
- Hits discounted by baseline: 2 current QA-02 definitions; both existed at `HEAD` and the feature removed the other 2 baseline definitions.
- Reservations accumulated in the feature: 0 new or aggravated hits.
- Suggested escalation: no trigger fired; there are no feature reservations, no touched file crossed 500 lines, and no block was added in three locations.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| Shared builder shape (DEC-01) | YES | `RateLimitedSnapshotFactory.cs:26-82`; three delegating providers. |
| Shared mapper and explicit hooks (DEC-02) | YES | `SnapshotFailureMapper.cs:43-59`; five live provider mapping paths in the caller graph. |
| Explicit OpenCode/Copilot deadlines (DEC-03) | YES | Provider-local rate-limit builders remain; only matching classification branches delegate. |
| Unchanged Core policy (DEC-04) | YES | No `RateLimitPolicy.cs` worktree state; 12 focused tests pass. |
| Safety-net scenarios (TC-01 through TC-04) | YES | All focused commands pass with nonzero test enforcement. |
| Quality profile (QA-01 through QA-03) | YES | Both searches meet target and the policy test gate passes. |
| Compatibility and rollout | YES | Helpers are internal; public snapshot/status contracts and persisted format are unchanged. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Shared factory exists; three provider delegations pass; missing-header coverage and method-size correction are present; QA-01 and QA-03 pass. |
| T02 | `done/task_02.md` | COMPLETE | Shared mapper and explicit hooks exist; Cursor 403 coverage is present; focused provider tests and QA-02 pass. |
| T03 | `done/task_03.md` | COMPLETE | QA-02 uses valid class `reservation`; HEAD 4/current 2 use the same definition-counting command and unit. |
| T04 | `done/task_04.md` | COMPLETE | All PRD acceptance boxes carry current, independently reproduced evidence. |

## Executed validations

- Profile and exclusions: .NET 10 / `net10.0`, native Microsoft.Testing.Platform; desktop E2E omitted by policy.
- Validated state: current handoff-bounded staged/worktree implementation, Debug/net10.0/x64 on Windows with SDK 10.0.401.
- Reused evidence: handoff results were used only to define scope and expected scenarios. Build, focused tests, full project, quality-profile searches, Terrain baselines, and diff hygiene were independently run in this review.
- Manual acceptance: none required by the TechSpec.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed; 3 projects, 0 errors, 0 warnings | Compilation and compatibility |
| Eight focused native-MTP commands with `--no-build --no-restore -- --minimum-expected-tests 1 --filter-class` | passed; Cursor 9, Antigravity 11, Claude 13, OpenCode 18, Copilot usage 11, Copilot billing 6, Copilot historical 4, policy 12 | R-01 through R-05; TC-01 through TC-04; QA-03 |
| `rtk proxy dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed; 675 total, 0 failed, 0 skipped | Full Infrastructure regression gate and PRD acceptance |
| QA-01 current `rg` command | zero hits; expected `rg` exit 1 | QA-01 target 0 |
| QA-01 baseline `git grep` against `HEAD` | 2 definitions | QA-01 Terrain baseline |
| QA-02 current `rg` command | 2 definitions | QA-02 target at most 2 |
| QA-02 baseline `git grep` against `HEAD` | 4 definitions | QA-02 Terrain baseline |
| Scoped `rtk git diff --cached --check` | passed; exit 0 | Diff hygiene |

## Findings

No findings.

## Previous findings

| Review/ID | State | Current evidence |
| --- | --- | --- |
| `codereview_1/CR-01` | resolved | Cursor 403 test at `CursorUsageProviderTests.Failures.cs:40-63` and absent-`Retry-After` test at `:95-124`; Cursor class passed 9. |
| `codereview_1/CR-02` | resolved | `techspec.md:64` now has valid class `reservation`, baseline 4, target at most 2, and a definition-only command; both counts were reproduced. |
| `codereview_1/CR-03` | resolved | `RateLimitedSnapshotFactory.Create` spans lines 26-55 inclusive, exactly 30 lines; extracted helpers span 11 and 14 lines. |
| `codereview_1/CR-04` | resolved | `prd.md:31-34` is checked with evidence reproduced by this review, including the 675-test full run. |

## Limitations and open items

- No `--base` was supplied. The repository contains substantial unrelated staged, unstaged, deleted, and new work, so commit-level feature attribution is unavailable. Scope is bounded by the SDD handoffs and current files; a caller-supplied base would be required for a complete repository delta proof.
- Provider HTTP-exception construction is intentionally owned by sibling PRD `arch-20260912-03`. This review verified the composed status-code behavior through provider tests but did not attribute those sibling client/exception edits to this feature.
- E2E is omitted by the desktop .NET policy. The TechSpec declares no essential manual acceptance.
- AA-10 remains an open product decision. Antigravity 403 continues to return no Cloud Code snapshot and fall through exactly as this PRD requires; the open decision is not an implementation gap in this review.

## Conclusion

The implementation conforms to R-01 through R-05, DEC-01 through DEC-04, TC-01 through TC-04, and QA-01 through QA-03. All four prior findings are resolved. The build is clean, every focused validation passes with nonzero-test enforcement, and the full Infrastructure suite passes 675 tests. No new or aggravated blocking or reservation profile hit remains, so the re-review status is `APPROVED` within the stated no-base scope.
