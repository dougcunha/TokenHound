# Code review report - Refactoring shared HTTP failure construction

## Summary

- Status: APPROVED
- Git scope: Not delimited - no `--base` was supplied; reviewed the T01/T02 handoff file set against the current worktree. See limitations.
- Previous review: none

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-arch-20260912-03-http-failure-factory/prd.md` | read; links intact |
| TechSpec | `tasks/prd-arch-20260912-03-http-failure-factory/techspec.md` | read; links intact |
| Manifest | `tasks/prd-arch-20260912-03-http-failure-factory/tasks.md` | read; T01 and T02 complete |
| Handoffs | `done/task_01.md`, `done/task_02.md` | read; task IDs, dependencies, affected files, and evidence present |
| External predecessor | `tasks/prd-arch-20260912-01-dead-exports/tasks.md` | T01 and T02 marked complete; prerequisite change is present in the worktree |
| Implementation | T01/T02 handoff-declared files plus their current worktree diff | limited but reviewable |

The implementation set is `ProviderHttpException.cs`, `CursorApiClient.cs`, `AntigravityCloudCodeClient.cs`, `CopilotRateLimitExtractor.cs`, `CopilotApiClient.cs`, `CopilotBillingClient.cs`, `CopilotMetricsClient.cs`, and `CopilotRequestGate.cs`. Corresponding provider tests were inspected and the full Infrastructure test project was executed.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| R-01 | Preserve Cursor message, status, and retry metadata | `CursorApiClient.GetUsageSummaryAsync`; `ProviderHttpException.FromResponse` | `CursorApiClientTests` | conformant | Existing message literal is passed unchanged at `CursorApiClient.cs:75-77`; 401 and 429 assertions at `CursorApiClientTests.cs:82-123`; full test project passed. |
| R-02 | Preserve Antigravity message, status, and retry metadata | `AntigravityCloudCodeClient.RetrieveUserQuotaSummaryAsync`; shared factory | `AntigravityCloudCodeClientTests`; `AntigravityUsageProviderTests` | conformant | Existing message literal is passed unchanged at `AntigravityCloudCodeClient.cs:134-136`; 403 assertions at `AntigravityCloudCodeClientTests.cs:156-180` and 429 integration assertions at `AntigravityUsageProviderTests.Failures.cs:48-61`. |
| R-03 | Preserve delta, HTTP-date, raw-integer order and ceiling behavior | unchanged `HttpRetryAfterParser.ExtractSeconds` | provider/parser-path tests in Infrastructure suite | conformant | Parser implementation at `HttpRetryAfterParser.cs:10-32` has no worktree diff; direct callers still use it; 673 Infrastructure tests passed. |
| R-04 | Preserve Retry-After parsing at four Copilot call sites | direct calls in API, billing, metrics, and request gate | `CopilotApiClientTests`, `CopilotBillingClientTests`, `CopilotMetricsClientTests`, `CopilotRequestGateTests` | conformant | Calls at `CopilotApiClient.cs:168`, `CopilotBillingClient.cs:232`, `CopilotMetricsClient.cs:284`, and `CopilotRequestGate.cs:246` preserve the prior time-provider arguments; current full run passed. |
| R-05 | Parse Retry-After only for 429 in the new failure factory | `ProviderHttpException.FromResponse` | Cursor 401/429; Antigravity 403/429 paths | conformant | Conditional at `ProviderHttpException.cs:36-38`; non-429 tests observe null and 429 tests observe parsed values. |
| PRD constraint: caller message | Keep provider-specific nouns and wording | Cursor and Antigravity construct and pass their original literals | focused provider tests plus diff review | conformant | Deleted and added literals are byte-for-byte identical in the worktree diff. |
| PRD constraint: clock | Keep `TimeProvider.System` at current client call sites | shared factory and three Copilot clients use `TimeProvider.System`; request gate keeps `_timeProvider` | Copilot request-gate time assertions | conformant | `ProviderHttpException.cs:37`; Copilot calls at `:168`, `:232`, `:284`, and request gate at `:246`. |
| PRD constraint: exclusions | Do not unify exception hierarchies or change `TryExtractRateLimit` | exception types unchanged; extractor method retained | full Infrastructure suite | conformant | `CopilotRateLimitExtractor.TryExtractRateLimit` remains at `CopilotRateLimitExtractor.cs:12-39` with its request-gate caller; no hierarchy files changed. |
| Acceptance: R-01..R-05 checked | Review every preserved behavior | matrix above | current and reused validations | conformant | Every R-ID has implementation and test evidence. |
| Acceptance: no silent behavior | Limit change to extraction/retargeting | eight implementation files | build and full suite | conformant | Diff contains the planned factory, call replacements, wrapper deletion, and an unrelated predecessor deletion already identified by its handoff. |
| Acceptance: Infrastructure tests | Run with a nonzero minimum | Infrastructure test project | 673 tests | conformant | Current MTP command passed with `--minimum-expected-tests 1`. |
| Acceptance: remove duplicates/wrapper | Delete both private factories and pass-through | T01/T02 implementation set | QA-01, QA-02 | conformant | Both scoped searches return zero hits. |
| DEC-01 | Add `ProviderHttpException.FromResponse` with 429-only parsing | `ProviderHttpException.cs:31-41` | Cursor and Antigravity failure tests | conformant | Signature, message parameter, status, parser, and `TimeProvider.System` match the decision. |
| DEC-02 | Retarget Cursor and remove private factory | `CursorApiClient.cs:71-77` | `CursorApiClientTests` | conformant | Shared factory is called and old signature has zero hits. |
| DEC-03 | Retarget Antigravity and remove private factory | `AntigravityCloudCodeClient.cs:130-136` | Antigravity client/provider tests | conformant | Shared factory is called and old signature has zero hits. |
| DEC-04 | Delete pass-through and retarget four Copilot sites | four Copilot callers; `CopilotRateLimitExtractor` | four focused Copilot classes; full suite | conformant | `ExtractRetryAfterSeconds` has zero source hits; `TryExtractRateLimit` remains unchanged. |
| TC-01 | Cursor 429 with/without header and non-429 preservation | shared factory through Cursor | `CursorApiClientTests`; `CursorUsageProviderTests` | conformant | Focused handoff run 4/4; current full project run passed. |
| TC-02 | Antigravity non-success including 429 | shared factory through Antigravity | client and usage-provider tests | conformant | Focused handoff runs 6/6 and 11/11; current full project run passed. |
| TC-03 | Copilot Retry-After behavior through four retargeted sites | four direct parser calls | four Copilot test classes | conformant | Focused handoff run 40/40; current full project run passed. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Repository architecture and invariants | OK | Change remains in Infrastructure; Core, UI, credentials, SQLite, and HUD behavior are untouched. Retry-After handling remains 429-gated in the shared factory. |
| C# structure and style | OK | One sealed exception class remains in its file; factory is 11 lines, uses existing layout conventions, and the build reports zero warnings. |
| `repository-cli-efficiency` | OK | Scope began with status/stat/name-status/check, then narrowed to the eight implementation files and relevant tests. |
| `dotnet-efficient-validation` | OK | Native MTP identified from `global.json` and the test project; build reused assets and tests used `--no-build --no-restore --minimum-expected-tests 1`. |
| `no-workarounds` | OK | The change removes CLONE/SCATTER decision points without swallowing failures, weakening exception contracts, adding retries, or suppressing diagnostics. |
| Desktop E2E policy | N/A | E2E omitted as required for this desktop C#/.NET review; the transport behavior is covered by unit/integration tests. |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No duplicate `CreateException(HttpResponseMessage)` bodies | blocking | `rtk rg -n "private static ProviderHttpException CreateException\(HttpResponseMessage response\)" src` | 0 current; baseline 2; target 0 | OK |
| QA-02 | No Retry-After pass-through | blocking | `rtk rg -n "ExtractRetryAfterSeconds" src` | 0 current; baseline 5; target 0 | OK |
| QA-03 | Single parser definition in parser file | blocking | `rtk rg -n "ExtractSeconds\(" src/TokenHound.Infrastructure/Providers/HttpRetryAfterParser.cs` | 1 current; baseline 1; target 1 | OK |

- Terrain baseline: applied from the TechSpec.
- Hits discounted by baseline: 1 retained QA-03 definition; no excess hit.
- Reservations accumulated in the feature: 0.
- Suggested escalation: no trigger fired.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-01 | YES | Shared factory has the specified signature and 429-only parser call. |
| DEC-02 | YES | Cursor calls the factory and has no private duplicate. |
| DEC-03 | YES | Antigravity calls the factory and has no private duplicate. |
| DEC-04 | YES | Four Copilot sites call the parser directly; wrapper is gone. |
| CMP-01..CMP-05 allowed file scope | YES | All planned files changed; `CopilotApiClient.cs` overlap is attributable to the completed predecessor handoff. |
| Dependency sequencing | YES | The external predecessor is marked complete and its shared-file deletion is present before/alongside T02. |
| Compatibility and exclusions | YES | Exception metadata, types, and parser implementation remain unchanged; AA-11 stays out of scope. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Factory and two migrations present; QA-01 is 0; focused Cursor 4/4, Antigravity client 6/6, and usage-provider 11/11 evidence is consistent with the current full run. |
| T02 | `done/task_02.md` | COMPLETE | Wrapper absent and four call sites direct; QA-02 is 0 and QA-03 is 1; focused Copilot 40/40 evidence is consistent with the current full run. |

## Executed validations

- Profile and exclusions: .NET 10, native Microsoft.Testing.Platform; E2E omitted by desktop policy.
- Validated state: current worktree, SDK 10.0.401, `net10.0`, all seven solution projects, and the Infrastructure test executable.
- Reused evidence: T01/T02 focused handoff runs supply per-class counts; the current build and full project run revalidated the same implementation paths.
- Manual acceptance: none required by the TechSpec.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet --version` | passed; `10.0.401` selected under `global.json` roll-forward | validation environment |
| `rtk git diff --check` | passed | diff hygiene |
| QA-01 command | passed; 0 hits (`rg` exit 1 means no match) | QA-01, acceptance removal criterion |
| QA-02 command | passed; 0 hits (`rg` exit 1 means no match) | QA-02, acceptance removal criterion |
| QA-03 command | passed; 1 hit at `HttpRetryAfterParser.cs:10` | QA-03 |
| `rtk dotnet build TokenHound.slnx --no-restore --nologo --verbosity:minimal` | passed; 7 projects, 0 errors, 0 warnings | DEC-01..DEC-04, compilation |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed; 673 tests, 0 warnings | R-01..R-05, TC-01..TC-03, acceptance test criterion |

## Findings

No findings.

## Optional improvements

No reservations or optional improvements.

## Previous findings

Not applicable; this is the first review for this PRD.

## Limitations and open items

- No `--base` was supplied. The feature package is untracked and the implementation is mixed with other current worktree changes, so commit-level attribution is unavailable. The review used the handoff-declared file set and narrowed relevant hunks; unrelated modifications and task deletions elsewhere in the worktree were not reviewed.
- `CopilotApiClient.cs` also contains the completed `prd-arch-20260912-01-dead-exports` removal. That predecessor is marked complete and its handoff identifies the overlap, but there is no commit boundary separating it from T02.
- E2E was omitted by policy. This is not an approval of E2E coverage; the TechSpec classifies the affected behavior as unit/integration-testable and requires no manual acceptance.

## Conclusion

APPROVED. Every PRD behavior, constraint, acceptance criterion, TechSpec decision, quality rule, safety-net case, and task state has conformant evidence. The current solution builds cleanly, all 673 Infrastructure tests pass with a nonzero minimum, the duplicate factories and pass-through are absent, and no new blocking or reservation-quality hit was introduced. The lack of a base limits commit attribution but does not leave an implementation obligation or essential validation unverified.
