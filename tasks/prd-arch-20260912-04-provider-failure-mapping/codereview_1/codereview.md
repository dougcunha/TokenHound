# Code review report - Shared provider failure mapping

## Summary

- Status: REJECTED
- Git scope: Not delimited - no `--base` was supplied; bounded by the T01/T02 handoffs and the current worktree at `HEAD` `2dd0a69`
- Previous review: none

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-arch-20260912-04-provider-failure-mapping/prd.md` | read once; all required sections present |
| TechSpec | `tasks/prd-arch-20260912-04-provider-failure-mapping/techspec.md` | read once; quality-profile defect recorded as CR-02 |
| Manifest | `tasks/prd-arch-20260912-04-provider-failure-mapping/tasks.md` | read; T01 and T02 linked and marked complete |
| Handoffs | `done/task_01.md`, `done/task_02.md` | read; files and commands checked against the current worktree |
| Implementation | Eight provider files named by the handoffs | limited but reviewable; unrelated dirty-worktree changes excluded |

The reviewable implementation set is `RateLimitedSnapshotFactory.cs`, `SnapshotFailureMapper.cs`, `CursorUsageProvider.cs`, `AntigravityUsageProvider.Snapshots.cs`, `ClaudeOAuthProvider.cs`, `OpenCodeUsageProvider.cs`, `CopilotUsageProvider.Snapshot.cs`, and `CopilotHistoricalReportCollector.cs`. Existing provider and store tests were included as evidence. The staged skill changes, deleted historical task folders, `opencode.json`, sibling `prd-arch-20260912-*` work, and other provider-client changes were excluded.

All manifest links resolve. T02 depends on T01 and the dependency order is satisfied. No extra task or handoff file exists under this PRD.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| R-01 | Preserve Cursor 429 snapshot shape and policy deadline | `CursorUsageProvider.cs:209-239`; `RateLimitedSnapshotFactory.cs:26-65` | `CursorUsageProviderTests.Failures.cs:40-68` | conformant | Current focused run passed 7 tests; status, block, zero `Retry-After`, and floor are asserted. |
| R-02 | Preserve Antigravity 429 and 403 fall-through | `AntigravityUsageProvider.Snapshots.cs:136-167` | `AntigravityUsageProviderTests.Failures.cs:47-67`; `AntigravityUsageProviderTests.cs:187-223` | conformant | The explicit forbidden hook returns `Ignored`; 429 block and 403 transcript fallback are covered. |
| R-03 | Preserve Claude windows after 429 | `ClaudeOAuthProvider.cs:232-250` | `ClaudeOAuthProviderTests.cs:108-272` | conformant | Retry values, escalation, and last-success windows are asserted; 13 tests passed. |
| R-04 | Preserve OpenCode and Copilot explicit deadlines and reasons | `OpenCodeUsageProvider.cs:136-145`; `CopilotUsageProvider.Snapshot.cs:23-55`; `CopilotHistoricalReportCollector.cs:276-296` | OpenCode, Copilot usage, billing, and historical focused runs | conformant | 47 relevant tests passed; custom OpenCode exceptions retain 401/403/429 status codes and Copilot outcomes match the former switches. |
| R-05 | Preserve persisted block timestamps and retry values | Factory delegates to unchanged `RateLimitPolicy`; persisted models/store unchanged | `UsageStoreTests.cs:332-370`; `RateLimitPolicyTests` | conformant | 17 UsageStore tests and 12 policy tests passed; `RateLimitPolicy.cs` has no diff. |
| PRD acceptance | Check every R-NN after refactoring | Review matrix above | Current validations | non-conformant | The behavior rows are checked here, but the source acceptance boxes remain unchecked and TC-01 evidence is incomplete (CR-01, CR-04). |
| PRD acceptance | No silent behavior change | Shared core plus explicit hooks | Focused and full project runs | conformant | Diff comparison shows the former status/reason branches retained. |
| PRD acceptance | Provider failure tests pass with a nonzero minimum | Existing provider tests | Native MTP commands | conformant | Every test command used `--minimum-expected-tests 1`; no zero-test success was accepted. |
| PRD acceptance | Eliminate duplicate snapshot/mapping bodies | Two shared helpers | QA-01 plus source review | conformant | No duplicate snapshot constructor or duplicated mapping switch remains. |
| DEC-01 | Shared parameterized snapshot builder returning snapshot and count | `RateLimitedSnapshotFactory.Create` | Cursor, Antigravity, Claude tests | conformant | All specified inputs are parameters and callers retain count ownership. |
| DEC-02 | Mapper keyed by status/credential with Antigravity override | `SnapshotFailureMapper.Classify`; provider hooks | Provider failure tests | conformant | Shared 401/403/429/default core is used by five changed paths; 403 divergence is explicit. |
| DEC-03 | Keep OpenCode/Copilot explicit-deadline builders | Provider-local rate-limit builders remain | OpenCode/Copilot tests | conformant | Only classification delegates; deadline sources were not forced into the shared factory. |
| DEC-04 | Do not change `RateLimitPolicy` | No Core policy diff | 12 `RateLimitPolicyTests` | conformant | Policy source is unchanged and focused tests pass. |
| TC-01 | Cursor 429 with and without `Retry-After` | Cursor delegates to shared factory | One 429 test with `Retry-After: 0` | non-conformant | No provider test covers absent `Retry-After`; the required Cursor 403 T02 scenario is also absent (CR-01). |
| TC-02 | Antigravity 429 and 403 | Explicit ignored hook and shared factory | Antigravity class | conformant | 13 tests passed, including both required paths. |
| TC-03 | Claude 429 preserves windows | Optional `limitWindows` factory argument | Claude class | conformant | 13 tests passed; `GetSnapshotAsync_WhenRateLimitedAfterSuccess_PreservesLastKnownLimitWindows` directly covers it. |
| TC-04 | OpenCode/Copilot statuses and reasons | Shared classification with provider translations | Four focused classes | conformant | OpenCode 401/403/429, Copilot 401/403/429, billing 403/network, and historical 429 are covered. |
| QA-01 | No duplicate private provider snapshot builders | Shared factory | Exact profile search | conformant | Search returned zero hits, the expected `rg` exit code 1. |
| QA-02 | Failure-mapping switches share one core and meet target | Shared mapper plus two retained thin delegates | Exact profile search | not verifiable | The rule class is invalid and its metric is internally inconsistent (CR-02). |
| QA-03 | Preserve rate-limit floor | Unchanged policy | `RateLimitPolicyTests` | conformant | 12 tests passed. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `sdd-review-code` sources, links, IDs, tasks, and immutable report | OK | Required artifacts exist; next free suffix was `codereview_1`. |
| Repository provider invariants | OK | Core policy and persisted state format are unchanged; zero-`Retry-After` still receives a future deadline. |
| `AGENTS.md` file/class/method size | NOT OK | `RateLimitedSnapshotFactory.Create`, lines 26-65, spans 40 lines against the 30-line maximum (CR-03). |
| `AGENTS.md` C# structure and public documentation | OK | One type per file, internal helpers documented, and affected files remain below 300 lines. |
| `dotnet-efficient-validation` and repository MTP rules | OK | SDK 10.0.401 selected by `global.json`; native MTP; build once, then `--no-build --no-restore`; every run enforced one expected test. |
| `repository-cli-efficiency` | OK | Searches and diffs were limited to the PRD, named providers, and relevant tests. |
| `no-workarounds` | OK | Provider differences are explicit hooks; no suppression, swallowed failure, immediate retry, or fallback hides a changed contract. |
| Desktop .NET E2E policy | N/A by policy | E2E omitted. The TechSpec requires no manual acceptance. |
| Diff hygiene | OK | Scoped `git diff --check` returned exit 0. |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No duplicated `CreateRateLimitedSnapshot` body | blocking | `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` | 0 total; 0 new/aggravated from baseline 2 | OK |
| QA-02 | Failure mappings share one decision core | invalid: `booking` | `rtk rg -n "MapHttpFailure\|MapCloudCodeFailure\|MapBillingFailure" src/TokenHound.Infrastructure/Providers` | 4 total versus baseline 7 and literal target `<= 2` | NOT OK / not classifiable |
| QA-03 | Rate-limit floor preserved | blocking | focused `RateLimitPolicyTests` | 12 passed | OK |

- Terrain baseline: applied as written for QA-01 and QA-03. QA-02 cannot be reliably subtracted because the TechSpec says 7, both handoffs report an observed baseline of 8, and no Git base was supplied.
- Hits discounted by baseline: QA-01 discounted 2 removed hits. QA-02 is not counted because its baseline and unit of measure conflict.
- Reservations accumulated in the feature: 0 verifiable. T02 calls QA-02 one reservation, but the TechSpec does not classify it as `reservation`.
- Suggested escalation: no counted trigger is defined or fired.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| Shared builder shape (DEC-01) | YES | `RateLimitedSnapshotFactory.cs:26-65`; three delegating providers. |
| Shared mapper and explicit hooks (DEC-02) | YES | `SnapshotFailureMapper.cs:43-59`; Cursor, Antigravity, Copilot hooks. |
| Explicit OpenCode/Copilot deadlines (DEC-03) | YES | Existing local deadline builders remain; only classification changed. |
| Unchanged Core policy (DEC-04) | YES | Empty `git diff` for `RateLimitPolicy.cs`; 12 tests passed. |
| Safety-net scenarios | PARTIAL | All executions pass, but two required Cursor scenarios have no direct test (CR-01). |
| Quality profile | NO | QA-02 has an invalid class, conflicting baseline, and an unmet literal search target (CR-02). |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Shared factory is present; QA-01 is zero; Cursor, Antigravity, Claude, policy, store, and full-project tests pass. CR-03 is a repository-rule defect but does not invalidate the preserved behavior. |
| T02 | `done/task_02.md` | INCOMPLETE | Mapper and hooks are present, but the handoff's required Cursor 403 coverage does not exist and QA-02 is not validly closed (CR-01, CR-02). |

## Executed validations

- Profile and exclusions: .NET 10 / `net10.0`, native Microsoft.Testing.Platform; desktop E2E omitted by policy.
- Validated state: current worktree implementation bounded by the two handoffs; Debug/net10.0 on Windows with SDK 10.0.401.
- Reused evidence: handoff results were contextual only. Build, focused tests, full project, profile searches, and diff checks were rerun.
- Manual acceptance: none required by the TechSpec.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed; 3 projects, 0 errors, 0 warnings | Implementation compilation |
| Eight focused MTP commands for Cursor, Antigravity, Claude, OpenCode, Copilot usage, Copilot billing, Copilot historical, and `RateLimitPolicyTests` | passed; 92 tests total | R-01 to R-04; TC-01 to TC-04; QA-03 |
| Focused `*UsageStoreTests*` MTP command | passed; 17 tests | R-05 persistence/refresh evidence |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed; 673 tests | Infrastructure regression gate |
| QA-01 exact `rg` command | passed; zero hits, `rg` exit 1 | QA-01 |
| QA-02 exact `rg` command | 4 occurrences: two calls and two thin delegates | QA-02 / CR-02 |
| `rtk rg ... "Forbidden|403" tests/TokenHound.Infrastructure.Tests/Providers/Cursor` | zero hits, `rg` exit 1 | CR-01 evidence |
| Scoped `git diff --check` | passed; exit 0 | Diff hygiene |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Medium | TC-01; T02 verification | `done/task_02.md:56` requires Cursor 401/403/429. `CursorUsageProviderTests.Failures.cs:15-68` contains 401 and one 429 case; `CreateRateLimitResponse` at lines 118-125 always sends `Retry-After: 0`. An exhaustive Cursor-test search finds no `Forbidden`/403 case. | The newly introduced `ClassifyForbiddenAsAuth` hook and the factory's null `retryAfterSeconds` path lack required provider-level regression evidence. Passing the seven-test class and full project does not exercise those cases. | Add a Cursor 403 provider test asserting `NeedsAuth`, and a Cursor 429-without-`Retry-After` provider test asserting a null retry value and a future policy deadline; rerun the focused class with the minimum-test guard. |
| CR-02 | Medium | QA-02 | `techspec.md:64` classifies QA-02 as `booking`, not `blocking` or `reservation`; its exact command returns 4 occurrences against a literal target of `<= 2`. The TechSpec baseline is 7, while both handoffs say the observed baseline was 8 and reinterpret four occurrences as two delegates. | The mandatory quality gate cannot be classified or reproducibly compared to Terrain, so T02's completion claim is not independently verifiable. | Correct QA-02 to a valid class and define a command that counts the intended unit (for example, mapping method definitions rather than both definitions and call sites), set the verified baseline/target, and rerun it. |
| CR-03 | Low | `AGENTS.md` method-size rule | `RateLimitedSnapshotFactory.Create` spans `RateLimitedSnapshotFactory.cs:26-65`, 40 lines including its signature and body. | The new shared hot path violates the repository's 30-line method maximum, increasing the review surface of centralized deadline construction. | Extract either deadline/count calculation or snapshot materialization into a focused private method so each method is at most 30 lines without changing behavior. |
| CR-04 | Low | PRD acceptance state; manifest state | `prd.md:31-34` keeps all four implementation acceptance boxes unchecked, while `tasks.md:53-54` marks T01 and T02 complete and both handoffs claim acceptance. | The authoritative artifacts disagree about whether feature acceptance occurred; this prevents a self-contained future review from trusting the recorded state. | After CR-01 and CR-02 are corrected and rerun, update the PRD acceptance state with evidence, or explicitly document that those checkboxes are immutable criteria and record acceptance in one designated manifest field. |

## Previous findings

Not applicable; this is the first review.

## Limitations and open items

- No `--base` was supplied. The dirty worktree contains substantial unrelated staged, unstaged, deleted, and untracked work. Handoffs make this feature reviewable, but they do not provide commit-level attribution; a caller-supplied base is required for a complete delta proof.
- QA-02's Terrain baseline cannot be reconstructed reliably from the supplied artifacts (CR-02).
- Full-project success proves regression health but does not replace missing scenario coverage (CR-01).
- E2E is omitted by desktop .NET policy. No manual acceptance is required for the snapshot-only change.
- The open AA-10 question remains unchanged: Antigravity 403 continues to fall through to transcript fallback.

## Conclusion

The shared factory and mapper preserve the reviewed provider outcomes, and all executed code validations are green: clean build, 92 focused tests, 17 focused store tests, and 673 Infrastructure tests. Approval is nevertheless blocked. T02 lacks two explicitly required Cursor regression cases, QA-02 is not a valid or reproducible quality gate, the new factory violates the repository's method-size limit, and the PRD acceptance state conflicts with the completed manifest. Status is `REJECTED` until CR-01 through CR-04 are corrected and the affected checks are rerun.
