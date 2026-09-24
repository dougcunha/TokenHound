# Code review report — 08-claude-quota-breakdown

## Summary

- Status: APPROVED WITH RESERVATIONS
- Git scope: `606426652bdf3a286d44db6eb169e9b426d5d251..uncommitted worktree`. HEAD equals base. The scope is 11 modified and 5 new `.cs`/`.csproj` files under `src/` and `tests/`, plus the correction from `codereview_1/done/task_03.md`.
- Previous review: `tasks/prd-08-claude-quota-breakdown/codereview_1/codereview.md`
- Reviewer session: `01XDQuf2TU3GqApSXLnbnvRy`. This session authored none of the code under review. The authors were `1346bbc2-2fdc-428e-ba40-42a7b65870cc` (T01, T02) and `01WwY4xqC3VkRoMYCRqVMDjY` (T03); see workflow REC-06 and REC-08.
- The only reservation is `codereview_1/OI-02`, which the user already accepted as an open item under workflow DEC-11. No new reservation was raised.

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-08-claude-quota-breakdown/prd.md` (SHA-256 matches DEC-01) | read |
| TechSpec | `tasks/prd-08-claude-quota-breakdown/techspec.md` (SHA-256 matches DEC-10) | read |
| Manifest | `tasks/prd-08-claude-quota-breakdown/tasks.md` | read |
| Tasks | `done/task_01.md`, `done/task_02.md`, `codereview_1/done/task_03.md` | read (handoffs) |
| Previous review | `codereview_1/codereview.md` | read |
| Snapshot | `context-snapshot.md`: header, next step brief, open threads, and `on-run` only (independent stage) | read |
| Implementation | `git diff 6064266` plus untracked `.cs` files | delimited |

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| FR-01, OBJ-01, US-01 | Show each reported model or scoped quota, including 0% | `ClaudeQuotaWindowMapper.cs:14-117` | `ClaudeQuotaProviderTests.ReportedQuotas_AreOrderedAndKeepOnlyReportedMeasurements`, `ClaudeQuotaPresentationTests.Rows_LabelEveryClaudeQuotaDistinctly` | conformant | `seven_day_sonnet` at 0% is kept; `weekly_scoped` and `weekly_opus` each produce a window and a row |
| FR-02, NFR-04 | Distinct labels; model name only when identified; neutral label for an unfamiliar kind | `ProviderUsageRowFactory.cs:97-110` (Claude branch), `ProviderUsageRowFactory.Claude.cs:40-64` | `Rows_LabelEveryClaudeQuotaDistinctly`, `Rows_HumanizeUnknownSuffixWithoutGuessingAModel`, `Rows_KeepUnrecognizedKindsNeutral` | conformant | The Claude path is now total: `session_opus` is labeled "Session opus", not a base label (closes OI-01) |
| FR-03, OBJ-03, US-02 | Null, absent, or invalid quotas yield no row | `ClaudeQuotaWindowMapper.cs:49-59,100-108,178-185`; `ClaudeWindowDto.cs:16-36` | `MalformedEntries_AreSkippedIndividuallyAtRollover` | conformant | `null`, `7`, a string percent, 101, and a null `seven_day_*` are each skipped without dropping neighbors |
| FR-04, OBJ-02 | Merge proven aliases; keep the top-level base windows at rollover | `ClaudeQuotaWindowMapper.cs:20-24,119-152` | `ReportedQuotas_...` (the `limits` entry beats the 99% top-level alias), `MalformedEntries_...` (rollover), `ScopedGroups_AreNotDeduplicatedByKindOrPercentage` | conformant | Scoped identity includes the group; no merge on equal percentages |
| FR-05 | Session, then overall weekly, then a deterministic rest; session stays primary | `ClaudeQuotaWindowMapper.cs:26-30,216-225`; `ProviderRingViewModel.cs:240-247`; `ProviderUsageRowFactory.Claude.cs:32-38` | `Ring_SelectsExactCanonicalWindows`, `Ring_WithOnlyAdditionalQuotas_LeavesBaseSummariesUnmeasured`, `NonClaudeProvider_KeepsGenericSelectionAndLabels` | conformant | An additional quota is never promoted to the ring |
| FR-06, NFR-03 | Percent only; reset only when supplied; no count or total | `ClaudeQuotaWindowMapper.cs:128-135`; `LimitWindow.cs:21` | `ReportedQuotas_...` (all units null), `LimitWindowTests`, `Rows_...` (no secondary text) | conformant | The fabricated `TotalUnits = 100` path was removed. Non-Claude `LimitWindow` producers that set a fraction also set `TotalUnits` (Cursor, OpenCode, Codex, Copilot, Cline, Antigravity), so the DEC-09 getter change alters no other provider's reading |
| FR-07, US-03 | Independent profile breakdowns | Stateless mapper; `IsClaudeProvider` covers `claude` and `claude-<slug>` | `SeparateProfiles_KeepTheirOwnQuotaBreakdowns`; the `claude-work` theory case | conformant | No `claude-*` provider ID other than Claude profiles exists in `src/` |
| FR-08, NFR-02, US-04 | A 429 retains the whole breakdown; the deadline is respected | Unchanged provider stale path and `UsageStore` gate | `RateLimitAfterSuccess_KeepsTheWholeBreakdown` (`Retry-After: 0` yields a future deadline); existing `UsageStoreGatingTests` | conformant | The full suite (782) includes the gate tests |
| NFR-01 | Credentials read-only | No change to `ClaudeProfileDiscovery` or `ClaudeOAuthClient` | Existing credential tests | conformant | Diff scope |
| NFR-05 | Core stays pure | `LimitWindow.cs` only | Core.Tests 92/92 | conformant | No new `using` in Core |
| DEC-09 / EX-01 | An explicit fraction survives without a total; test files stay ≤ 300 lines | `LimitWindow.cs:21` | `LimitWindowTests` | conformant | `DomainModelsTests.cs` 204 lines, `LimitWindowTests.cs` 97; the `McpMetricsReaderTests` expectation follows the approved contract (manifest, Problems and solutions) |
| codereview_1/OI-01 (T03) | Unrecognized Claude kinds keep neutral labels | `ProviderUsageRowFactory.cs:106-107`, `ProviderUsageRowFactory.Claude.cs:55` | `Rows_KeepUnrecognizedKindsNeutral` | conformant | See Previous findings |
| TC-07, US-01–US-04 | Live HUD manual acceptance | — | — | not verifiable | This session has no Windows MCP `App`/`Screenshot` tools. Carried to HIL 3 per the TechSpec |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| Never invent limits or denominators (`CLAUDE.md`) | OK | `ClaudeQuotaWindowMapper.cs:128-135` sets no units |
| Read-only credentials, 429 discipline | OK | No change to the client, discovery, or store |
| Core purity | OK | `LimitWindow.cs` diff |
| File ≤ 300 lines, method ≤ 30 lines | OK | Largest changed source is `ProviderRingViewModel.cs` at 287 lines; the T03 files are at 176 and 65 lines |
| XML docs on public or internal API members | OK | `ClaudeUsageResponse.cs`, `ClaudeWindowDto.cs`, `ProviderUsageRowFactory.Claude.cs:17-38` |
| Split calls with ≥ 4 arguments | OK | `ClaudeQuotaWindowMapper.cs:64-69,81-86,110-115,196-201` |
| `dotnet-efficient-validation`, `repository-cli-efficiency` | OK | Project-scoped builds and MTP runs below |

## Quality profile

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | No sync-over-async | blocking | `rg -n '\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)' <changed .cs>` | 0 | OK |
| QA-02 | No empty or catch-and-default handler | blocking | `rg -n 'catch…' <changed .cs>` plus review | 0 new; the 4 `catch` clauses at `ClaudeOAuthProvider.cs:115-130` are unchanged | pre-existing |
| QA-03 | No nullable or warning suppression | blocking | `rg -n '#nullable disable\|#pragma warning disable' <changed .cs>` | 0 | OK |
| QA-04 | Review of ≥ 4 parameters | reservation | `rg -n '\w+\((?:[^),]+,){3,}[^)]*\)' <changed .cs>` | 1 new of 3 | `ClaudeQuotaWindowMapper.cs:12` was accepted under DEC-11 (`codereview_1/OI-02`). The other two are BCL `DateTimeOffset` calls in tests (`ClaudeQuotaPresentationTests.cs:15`) or already present at HEAD (`McpMetricsReaderTests.cs:20`) |
| QA-05 | Source file ≤ 300 lines | blocking | `wc -l <changed .cs>` | 0 new | OK; `ClaudeOAuthProviderTests.cs` has 440 lines both at HEAD and now (pre-existing) |

- Terrain baseline: applied from the TechSpec. `ClaudeOAuthProviderTests.cs` is absent from it, so it is treated as pre-existing through `git show 6064266` (440 → 440).
- Hits discounted by the baseline: 5 (4 QA-02 lines, 1 QA-04).
- Reservations accumulated in the feature: 1, decided under DEC-11.
- Suggested escalation: no trigger fired (1 < 8 reservations; no touched file > 500 lines; no logic duplicated in three places).
- `git diff --check 6064266`: clean.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-02 | YES | Only `limits` and `seven_day_*` are read; `extra_usage`, `spend`, and `seven_day_breakdown` are ignored (`ClaudeQuotaWindowMapper.cs:100-108,154-156`) |
| DEC-03 | YES | `ToPercent` accepts finite values in `[0,100]`, including 0 (`:178-185`) |
| DEC-04 | YES | `CanonicalName` (`:138-152`), `limits`-first precedence, scoped identity by group (`:122-123`) |
| DEC-05 | YES | Claude labels come from canonical identity with no generic fallback (after T03); the ring selects by exact name |
| DEC-06 | YES | Mapping extracted; provider 276 → 221 lines; constructor unchanged |
| DEC-07 | YES | Stale/429 path unchanged and covered |
| DEC-09 | YES | `LimitWindow.UsedFraction` is a plain `init` property |
| Contracts: optional `limits` array, per-entry skipping, nullable utilization | YES | `ClaudeUsageResponse.cs:24-34`, `ClaudeWindowDto.cs:16-36`; the DTOs are only deserialized (`ClaudeOAuthClient.cs:136`), so default `JsonElement` members never reach a serializer |
| Affected files | PARTIAL (justified) | `ProviderUsageRowFactory.Claude.cs` (partial of the named class) and its linked `<Compile>` were recorded in the T02 handoff. The T03 fixture change in `ProviderRingViewModelTests.cs` is judged under Previous findings |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Handoff, REC-04 adoption, REC-05 completion |
| T02 | `done/task_02.md` | COMPLETE | Handoff; TC-07 explicitly pending |
| T03 (correction for `codereview_1/OI-01`) | `codereview_1/done/task_03.md` | COMPLETE | Work items checked. The handoff lists the changed files, 782/782 tests, clean builds, and one declared deviation (fixture) |

The manifest `State` matches `done/`, links resolve, and the DAG T01 → T02 → T03 was followed. As `codereview_1` already noted, the current hashes of `tasks.md` and the task files differ from the approved hashes only through recorded execution sections. The approved bytes are not stored, so this is accepted on record (REC-05).

## Executed validations

- Profile and exclusions: Core and Infrastructure target `net10.0`, and App targets WPF `net10.0-windows`. Tests use native MTP (SDK 10.0.401). E2E is omitted by the .NET desktop policy.
- Validated state: the uncommitted worktree on base `6064266` as listed in the summary, Debug, on 2026-09-23.
- Reused evidence: none; every check was rerun in this session.
- Manual acceptance: TC-07 was not executed and is pending for HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 errors, 0 warnings | NFR-05, DEC-09 |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 errors, 0 warnings | CMP-01–CMP-06 |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 errors, 0 warnings | CMP-04, CMP-05 (linked partial compiles in WPF) |
| `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed 92/92, exit 0 | TC-03, DEC-09 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` | passed 782/782, exit 0 | TC-01–TC-06, FR-08 store gate, OI-01 |
| Quality profile QA-01–QA-05 over changed `.cs` files; `git diff --check` | see Quality profile | QA-01–QA-05 |

## Findings

None. There is no non-conformant obligation, failing test, or unjustified blocking profile hit.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_1/OI-01 | resolved | `ProviderUsageRowFactory.cs:106-107` routes every named Claude window to `ResolveClaudeLabel`, which is non-nullable and ends with `Humanize(name)` (`ProviderUsageRowFactory.Claude.cs:55`). `Rows_KeepUnrecognizedKindsNeutral` asserts "Session opus" and "Seven hour pool". |
| codereview_1/OI-02 | not a defect; accepted under DEC-11 | `ClaudeQuotaWindowMapper.cs:12` is unchanged, a private four-member record |
| T03 declared deviation (snapshot O-04) | justified | `ProviderRingViewModelTests.cs:220` changes the fixture name from `"Session"` to `five_hour`. After T01, no Claude production path emits `"Session"`: the mapper canonicalizes `session` to `five_hour` (`ClaudeQuotaWindowMapper.cs:141-142`). Keeping the old fixture would require the generic heuristic that OI-01 removes. The test's intent and asserted label are unchanged. |

## Limitations and open items

- TC-07, manual HUD acceptance for US-01–US-04 and OBJ-01–OBJ-03 against live data, cannot be verified in this review. No session so far has had the Windows MCP `App`/`Screenshot` tools. Per the TechSpec, it is an HIL 3 acceptance item and does not block this review.
- `codereview_1/OI-02` remains an accepted open item (DEC-11).
- Carried from `codereview_1`, outside the feature's scope: `ProviderUsageRowFactory.AddQuotaRows` lends `snapshot.ActiveBlock.ResetTimeUtc` to row 0 when that window has no reset. In a Claude `RateLimited` snapshot whose session lacks `resets_at`, that row would show the retry deadline as its reset. This behavior predates the feature, which does not change it.
- Approved task-contract bytes are not retained; see Verified tasks.

## Conclusion

The implementation meets every PRD requirement and TechSpec decision that automated evidence covers. The correction for `codereview_1/OI-01` is resolved, and the fixture deviation declared by T03 is justified. An independent run produced clean builds and full passes: Core 92/92 and Infrastructure 782/782. The profile has no new blocking hit. The only reservation is OI-02, already accepted under DEC-11. TC-07 remains the manual acceptance item for HIL 3. Status: **APPROVED WITH RESERVATIONS**, with every reservation already decided. The feature therefore proceeds to acceptance without another reservations HIL.
