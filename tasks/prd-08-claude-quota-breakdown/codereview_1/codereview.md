# Code review report — 08-claude-quota-breakdown

## Summary

- Status: APPROVED WITH RESERVATIONS
- Git scope: `606426652bdf3a286d44db6eb169e9b426d5d251..uncommitted worktree` (10 modified, 6 new files under `src/` and `tests/`; HEAD equals base)
- Previous review: —
- Reviewer session: `01WwY4xqC3VkRoMYCRqVMDjY`. This session authored none of the code under review; the author session was `1346bbc2-2fdc-428e-ba40-42a7b65870cc` (workflow REC-06).

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-08-claude-quota-breakdown/prd.md` (SHA matches DEC-01) | read |
| TechSpec | `tasks/prd-08-claude-quota-breakdown/techspec.md` (SHA matches DEC-10) | read |
| Exception | `tasks/prd-08-claude-quota-breakdown/exception-01.md` (SHA matches DEC-10) | read |
| Manifest | `tasks/prd-08-claude-quota-breakdown/tasks.md` | read |
| Tasks | `done/task_01.md`, `done/task_02.md` | read |
| Snapshot | `context-snapshot.md` — header, next step brief, open threads, `on-run` only (independent stage) | read |
| Implementation | `git diff 6064266` plus untracked `.cs` files | delimited |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01, OBJ-01, US-01 | Show each reported model or scoped quota, including 0% | `ClaudeQuotaWindowMapper.cs:33-117` | `ClaudeQuotaProviderTests.ReportedQuotas_AreOrderedAndKeepOnlyReportedMeasurements`; `ClaudeQuotaPresentationTests.Rows_LabelEveryClaudeQuotaDistinctly` | conformant | `seven_day_sonnet` at 0% kept; `weekly_scoped` and `weekly_opus` produce rows |
| FR-02, NFR-04 | Distinct labels; model name only when identified; neutral label for unknown scope | `ProviderUsageRowFactory.Claude.cs:40-64` | `Rows_LabelEveryClaudeQuotaDistinctly`, `Rows_HumanizeUnknownSuffixWithoutGuessingAModel` | conformant | See optional improvement OI-01 for unobserved kind shapes |
| FR-03, OBJ-03, US-02 | Null, absent, or invalid quotas yield no row | `ClaudeQuotaWindowMapper.cs:49-59,100-108,178-185`; `ClaudeWindowDto.cs:28-29` | `MalformedEntries_AreSkippedIndividuallyAtRollover` | conformant | null, string, `null`, and 101 values skipped individually |
| FR-04, OBJ-02 | Merge proven aliases; keep top-level base windows at rollover | `ClaudeQuotaWindowMapper.cs:20-24,119-152` | `ReportedQuotas_...` (limits wins over 99% top-level), `MalformedEntries_...` (rollover) | conformant | Matches `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §3 rollover merge |
| FR-05 | Session, then overall weekly, then deterministic rest; session stays primary | `ClaudeQuotaWindowMapper.cs:26-30,216-225`; `ProviderRingViewModel.cs:240-247` | `Ring_SelectsExactCanonicalWindows`, `Ring_WithOnlyAdditionalQuotas_LeavesBaseSummariesUnmeasured` | conformant | Additional quota is never promoted to the ring |
| FR-06, NFR-03 | Percent only; reset only when supplied; no count or total | `ClaudeQuotaWindowMapper.cs:128-135`; `LimitWindow.cs:21` | `ReportedQuotas_...` (all `TotalUnits`/`RemainingUnits` null); `LimitWindowTests`; `Rows_...` (no secondary text, no reset on unsupplied rows) | conformant | Previous fabricated `TotalUnits = 100` path removed |
| FR-07, US-03 | Independent profile breakdowns | Per-profile provider instances (unchanged); `IsClaudeProvider` handles `claude-<slug>` | `SeparateProfiles_KeepTheirOwnQuotaBreakdowns`; `Rows_...` theory with `claude-work` | conformant | Stateless mapper |
| FR-08, NFR-02, US-04 | 429 retains the whole breakdown; deadline respected | Unchanged provider stale path and `UsageStore` gate | `RateLimitAfterSuccess_KeepsTheWholeBreakdown` (`Retry-After: 0` → future deadline); existing `UsageStoreGatingTests` | conformant | Full suite 781/781 includes the gate tests |
| NFR-01 | Credentials read-only | No change to `ClaudeProfileDiscovery` or `ClaudeOAuthClient` | Existing credential tests | conformant | Diff scope |
| NFR-05 | Core stays pure | `LimitWindow.cs` only | Core.Tests 92/92 | conformant | No new `using` in Core |
| DEC-09 / EX-01 | Explicit fraction survives without total; remaining-only stays null; test files ≤ 300 lines | `LimitWindow.cs:21` | `LimitWindowTests` (5 tests) | conformant | `DomainModelsTests.cs` 302 → 204 lines; `LimitWindowTests.cs` 97 |
| TC-07, US-01–US-04 | Live HUD manual acceptance | — | — | not verifiable | Windows MCP `App`/`Screenshot` unavailable in this session too; carried to HIL 3 per TechSpec |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Never invent limits or denominators (`CLAUDE.md`) | OK | `ClaudeQuotaWindowMapper.cs:128-135` sets no units |
| Read-only credentials, 429 discipline | OK | No change to client, discovery, or store |
| Core purity | OK | `LimitWindow.cs` diff |
| File ≤ 300 lines, method ≤ 30 lines | OK | Largest changed source: `ProviderRingViewModel.cs` 287; `FindLimitWindow` 29 lines |
| `<inheritdoc />`/XML docs on public members | OK | New public DTO members documented in `ClaudeUsageResponse.cs`, `ClaudeWindowDto.cs` |
| Split calls with ≥ 4 arguments | OK | `ClaudeQuotaWindowMapper.cs:64-69,81-86,110-115,196-201` |
| `dotnet-efficient-validation`, `repository-cli-efficiency` | OK | Project-scoped builds and MTP runs below |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No sync-over-async | blocking | `rg -n '\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)' <changed .cs>` | 0 | OK |
| QA-02 | No empty/catch-and-default handler | blocking | `rg -n 'catch…' <changed .cs>` plus review | 0 new; 4 `catch` in `ClaudeOAuthProvider.cs:115-130` unchanged | pre-existing |
| QA-03 | No nullable/warning suppression | blocking | `rg -n '#nullable disable|#pragma warning disable' <changed .cs>` | 0 | OK |
| QA-04 | ≥ 4 parameters review | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' <changed .cs>` | 1 new of 3 | 1 reservation: `ClaudeQuotaWindowMapper.cs:12`; `ClaudeQuotaPresentationTests.cs:15` is a BCL `DateTimeOffset` call (not a signature); `McpMetricsReaderTests.cs:20` pre-existing at HEAD |
| QA-05 | Source file ≤ 300 lines | blocking | `wc -l <changed .cs>` | 0 new | OK; `ClaudeOAuthProviderTests.cs` 440 lines at HEAD and now (pre-existing, not in Terrain baseline) |

- Terrain baseline: applied from TechSpec; `ClaudeOAuthProviderTests.cs` was missing from it and is treated as pre-existing via `git show HEAD` (440 lines both before and after).
- Hits discounted by baseline: 5 (4 QA-02 lines, 1 QA-04).
- Reservations accumulated in the feature: 1.
- Suggested escalation: no trigger fired (1 < 8 reservations; no touched file > 500 lines; no logic duplicated in three places).

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-02 | YES | `limits` + `seven_day_*` only; `extra_usage`, `spend`, and `seven_day_breakdown` ignored (`ClaudeQuotaWindowMapper.cs:100-108,154-156`) |
| DEC-03 | YES | `ToPercent` accepts finite `[0,100]` including 0 (`:178-185`) |
| DEC-04 | YES | `CanonicalName` (`:138-152`), limits-first precedence, scoped identity by group (`:122-123`) |
| DEC-05 | YES | Claude labels before generic rules; exact-name ring selection |
| DEC-06 | YES | Mapping extracted; provider 276 → 221 lines; constructor unchanged |
| DEC-07 | YES | Stale/429 path unchanged and covered |
| DEC-09 | YES | `LimitWindow.UsedFraction` is a plain `init` property |
| Contracts: `limits` optional array, per-entry skipping, nullable utilization | YES | `ClaudeUsageResponse.cs:27-34`, `ClaudeWindowDto.cs:16-36` |
| Affected files | PARTIAL (justified) | T02 added `ProviderUsageRowFactory.Claude.cs` (partial of the named class, existing Copilot/Cline pattern) and the linked `<Compile>` in the test csproj; recorded in the T02 handoff |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Work items checked; handoff lists files, checks, QA; REC-04 adoption and REC-05 completion recorded |
| T02 | `done/task_02.md` | COMPLETE | Work items checked; handoff lists files, checks, QA; TC-07 explicitly pending |

Manifest `State` matches `done/`; links resolve; DAG T01 → T02 honored. The current hashes of `tasks.md` and both task files differ from the DEC-08/DEC-10 hashes. REC-05 records that only work checkboxes, handoffs, links, State, and Problems and solutions changed. The approved versions are not stored, so this review can confirm that the current contracts are consistent with the TechSpec, but it cannot diff them byte-for-byte.

## Executed validations

- Profile and exclusions: Core/Infrastructure `net10.0`, App WPF `net10.0-windows`, native MTP (SDK 10.0.401). E2E omitted by .NET desktop policy.
- Validated state: uncommitted worktree on base `6064266` as listed in the summary, Debug.
- Reused evidence: none. All checks were rerun in this session.
- Manual acceptance: TC-07 not executed (no Windows MCP desktop tools in this session). Pending for HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 errors, 0 warnings | NFR-05, DEC-09 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 errors, 0 warnings | CMP-01–CMP-06 |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 errors, 0 warnings | CMP-04, CMP-05 |
| `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed 92/92, exit 0 | TC-03, DEC-09 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed 781/781, exit 0 | TC-01–TC-06, FR-08 store gate |
| Quality profile QA-01–QA-05 over changed `.cs` files | see Quality profile | QA-01–QA-05 |

## Findings

None. No non-conformant obligation, failing test, or unjustified blocking profile hit was found.

## Optional improvements

| ID | Source | Evidence | Impact | Suggestion |
| --- | --- | --- | --- | --- |
| OI-01 | FR-02 | `ProviderUsageRowFactory.cs` `ResolveQuotaWindowLabel` → `ResolveNamedLabel`: a Claude window whose name is neither canonical, `*_scoped`, nor `seven_day_*`/`weekly_*` falls through to the generic heuristic. That heuristic matches `session`/`five` and `week`/`seven`. A hypothetical `limits` kind such as `session_opus` would therefore be labeled "Current session (5h)", which duplicates the real session label. | Low. No observed response or the provider spec contains such a kind, but the endpoint is undocumented and the PRD asks for neutral labels for unfamiliar scopes. | For Claude providers, return a humanized name instead of the generic period heuristic. Add one row test. |
| OI-02 | QA-04 (reservation) | `ClaudeQuotaWindowMapper.cs:12` private positional record `QuotaCandidate` with four members | Negligible: a private value carrier, not an API | Accept as is, or keep it as recorded justification. |

## Limitations and open items

- TC-07 manual HUD acceptance (US-01–US-04, OBJ-01–OBJ-03 live) is not verifiable in this review. Neither this session nor the author session had the Windows MCP `App`/`Screenshot` tools. Per the TechSpec, it is an acceptance item for HIL 3 and does not block the review.
- Approved task-contract bytes are not retained, so REC-05's claim that only execution sections changed is accepted on record. The current contracts were checked for consistency with the TechSpec.
- Observation outside feature scope: `ProviderUsageRowFactory.AddQuotaRows` (unchanged line) lends `snapshot.ActiveBlock.ResetTimeUtc` to row 0 when that window has no reset. In a Claude `RateLimited` snapshot whose first window lacks `resets_at`, the row would show the 429 retry deadline as its reset. This behavior predates the feature, which does not change it. It is recorded for a possible later decision.

## Conclusion

The implementation meets every PRD requirement and TechSpec decision covered by automated evidence. The builds are clean, and the full Core (92) and Infrastructure (781) suites pass in an independent run. The profile has no new blocking hits. The only open items are one low-impact label robustness improvement (OI-01), one justified reservation hit (OI-02), and TC-07 manual acceptance, which is deferred to HIL 3 by design. Status: **APPROVED WITH RESERVATIONS**.
