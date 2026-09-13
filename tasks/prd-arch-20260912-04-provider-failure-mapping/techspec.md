# TechSpec — Refactoring shared rate-limit snapshots and failure mapping

## Sources and traceability

- PRD: `prd.md`
- Current code and tests: `src/TokenHound.Infrastructure/Providers/Cursor/CursorUsageProvider.cs`, `…/Antigravity/AntigravityUsageProvider.Snapshots.cs`, `…/Claude/ClaudeOAuthProvider.cs`, `…/OpenCode/OpenCodeUsageProvider.cs`, `…/Copilot/CopilotUsageProvider.Snapshot.cs`, `…/Copilot/CopilotBillingService.cs`, `…/Copilot/CopilotHistoricalReportCollector.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/**`.
- Applicable instructions and skills: `AGENTS.md`; `dotnet-efficient-validation`; `repository-cli-efficiency`; `no-workarounds`.

## Technical decisions

| ID | Requirements | Decision | Evidence and reason | Alternatives and trade-offs |
| --- | --- | --- | --- | --- |
| DEC-01 | R-01, R-03 | Extract an internal shared rate-limited snapshot builder parameterized by provider id, reason, `retryAfterSeconds`, `timeProvider`, `rateLimitPolicy`, backoff jitter, consecutive-count mutation, and optional windows-from-last-success. | Cursor `:217-246`, Antigravity `:145-174`, Claude `:231-261` are the same algorithm; Claude differs only by `LimitWindows = _lastSuccessfulSnapshot?.LimitWindows ?? []`. | A static method returning `(Snapshot, newCount)` keeps state mutation at the caller; a mutable context object is the alternative. Prefer the returned tuple to avoid `ref` fields. |
| DEC-02 | R-02 | Shared `SnapshotFailureMapper` keyed by `(HttpStatusCode, hasCredential)` with an explicit per-provider override for Antigravity 403 → `null`. | Switches at `CursorUsageProvider.cs:209`, `AntigravityUsageProvider.Snapshots.cs:136`, `OpenCodeUsageProvider.cs:136` share a decision. | Full unification refused for 403 until AA-10's sub-item is decided. |
| DEC-03 | R-04 | OpenCode and Copilot retain their explicit-deadline snapshots; only their failure-mapping switch delegates to the shared mapper where semantics match. | Their signatures differ (`deadlineUtc` passed in) so the R-01 builder does not apply directly. | Forcing them into the same builder — rejected, would change the deadline source. |
| DEC-04 | R-05 | Do not alter `RateLimitPolicy`; pass its outputs through unchanged. | Policy is Core and shared. | — |

## Affected components

| ID | Component | Current state | Allowed change | Risk and dependencies |
| --- | --- | --- | --- | --- |
| CMP-01 | new shared builder (e.g. `Providers/RateLimitedSnapshotFactory.cs`) | absent | Create internal helper | Medium; central path. |
| CMP-02 | `CursorUsageProvider` | Own builder + switch | Delegate to helper/mapper | Medium; R-01. |
| CMP-03 | `AntigravityUsageProvider.Snapshots` | Own builder + switch | Delegate; keep 403 hook | Medium; R-02. |
| CMP-04 | `ClaudeOAuthProvider` | Overloaded builders | Delegate with windows flag | Medium; R-03. |
| CMP-05 | `OpenCodeUsageProvider` | Own switch | Delegate mapping where equal | Low; R-04. |
| CMP-06 | `CopilotUsageProvider.Snapshot`, `CopilotBillingService`, `CopilotHistoricalReportCollector` | Own switches/reasons | Delegate where equal; keep reason taxonomy | Low-Medium; R-04. |

## Safety net

- Profile: .NET 10 (`net10.0`), MTP runner. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`.
- E2E: omitted by the .NET desktop policy.
- Command prerequisites and exclusions: build first; run provider failure classes focused, then the full project.
- Manual acceptance: none (snapshot shape is asserted in unit tests).

| ID | Requirement | Level | Scenario | Expected result | Command or script |
| --- | --- | --- | --- | --- | --- |
| TC-01 | R-01, R-05 | unit | Cursor 429 with/without Retry-After | Snapshot fields and deadline unchanged | `CursorUsageProviderTests.Failures` |
| TC-02 | R-02, R-05 | unit | Antigravity 429 and 403 | 429 snapshot unchanged; 403 still `null` | `AntigravityUsageProviderTests.Failures` |
| TC-03 | R-03 | unit | Claude 429 | Windows preserved from last success | `ClaudeOAuthProviderTests` |
| TC-04 | R-04 | unit | OpenCode/Copilot failure paths | Same status/reason text | `OpenCodeUsageProviderTests`, `CopilotUsageProviderTests.Billing`, `CopilotBillingServiceTests` |

## Dependency sequencing

| Step | Depends on | Change | Evidence to advance | Reversal |
| --- | --- | --- | --- | --- |
| 1 | — | Add shared builder; migrate Cursor + Antigravity | TC-01, TC-02 pass | revert |
| 2 | 1 | Migrate Claude with windows flag | TC-03 passes | revert |
| 3 | 2 | Extract shared mapper; migrate switches with explicit hooks | TC-04 passes | revert |
| 4 | 3 | Full provider test run | all pass | revert |

## Compatibility and rollout

- Preserved contracts: R-01..R-05.
- Migration or coexistence: none; the helper is internal.
- Observability: unchanged `Snapshot` fields and `ProviderStatus`.
- Rollback: `git revert` per step.

## Quality profile

| ID | Rule | Class | Verification command | Baseline | Target |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No duplicated `CreateRateLimitedSnapshot` body across Cursor/Antigravity | blocking | `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` | 2 | 0 |
| QA-02 | Failure-mapping switches share one decision core | reservation | `rtk rg -n -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" src/TokenHound.Infrastructure/Providers` | 4 | ≤ 2 (thin delegates) |
| QA-03 | Rate-limit floor preserved | blocking | focused `RateLimitPolicy` tests | pass | pass |

- Target measures today: 3 near-identical builders; 4 mapping method definitions (HEAD baseline); 1 semantic divergence (403).
- Expected measures at the end: 1 builder; 2 mapping method definitions (DEC-02/DEC-03 thin delegates); 403 divergence explicitly encoded.

## Risks and open items

- Risk: touching the persisted 429 deadline path. Mitigation: preserve `RateLimitPolicy` unchanged; assert `ResetTimeUtc`/`RetryAfterSeconds` in tests.
- Open item: AA-10 sub-item — confirm Antigravity `403 → null` is intentional.
