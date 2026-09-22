# Code review report — MCP access to live provider metrics

## Summary

- Status: REJECTED
- Git scope: `97f17c79a5afeebbd8ff3a534cced7681b382476..bc4208db6ab83df7bf9d21f4e9f0e21d83b6c495`, with a clean worktree. The unrelated task-folder deletions in `cd80673` are outside this feature.
- Previous review: `codereview_2/codereview.md` (APPROVED before T05 and DEC-24).
- This review session authored none of the code it judged.

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `../prd.md` | Read; current SHA-256 matches workflow DEC-25. |
| TechSpec | `../techspec.md` | Read; current SHA-256 matches workflow DEC-25. |
| Manifest | `../tasks.md` | Read; T01–T03 and T05 point to existing `done/` files. |
| Prior correction | `../codereview_1/done/task_04.md` | Preserved; codereview_2 verified CR-01. |
| T05 | `../done/task_05.md` | Read; handoff and completed checklist present. |
| Implementation | Base-to-HEAD feature diff, T05 handoff, code and tests | Delimited; T05's model, adapter, HUD, MCP DTO/reader, docs, and tests inspected. |

The approved `task_05.md` hash in the checkpoint refers to the original root task. Execution moved and amended that task to `done/task_05.md`, so the historical hash is not expected to match the handoff file. DEC-25 records the original approval; the current contract and handoff are readable.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01, US-01, FR-01 | Local metrics during App lifetime | `App.Mcp.cs`, `McpServerHost.cs` | TC-03, TC-06 | conformant | Prior review; current App build and manual DEC-22 evidence. |
| OBJ-02, FR-07 | Cached reads without dispatch | `McpMetricsReader`, `UsageStore.Metrics.cs` | TC-02, TC-03 | conformant | Prior review; 22 MCP tests pass on current HEAD. |
| US-02, FR-04 | Distinct lookup states | `McpMetricsReader.GetMetrics` | TC-01, TC-03 | conformant | Prior review; current MCP tests pass. |
| FR-02 | Legacy SSE transport | `McpServerHost.Pipeline.cs` | TC-03 | conformant | Current MCP tests pass; `acceptance/t05_gemini.txt` records SSE 200 and both tools. |
| FR-03 | Enabled real provider list | `McpMetricsReader.ListMetrics` | TC-01, TC-03, TC-06 | conformant | Prior review and DEC-22 list evidence; T05 uses the same `MapProvider`/`MapWindow` path as lookup. |
| FR-05, NFR-03, DEC-24, CMP-09 | Truthful window status, used count, and nullable values | `LimitWindow.UsedUnits`, Antigravity snapshot, `McpMetricsReader.MapWindow` | TC-01, TC-07 | conformant | Adapter assigns `UsedUnits` and null `RemainingUnits`; 22 MCP tests and three targeted Antigravity tests pass; manual `t05_gemini.txt` shows `usedUnits: 0` and no `remainingUnits`. |
| FR-06 | Copilot and Cline data | `McpMetricsReader.Copilot.cs`, `.Cline.cs` | TC-01 | conformant | Prior review; current MCP tests pass. |
| FR-08 | Client documentation | `README.md`, `docs/MCP.md` | Doc review, TC-06 | conformant | Documented SSE URL, tools, setup, field semantics and `usedUnits`. |
| NFR-01 | Loopback, Host and Origin boundary | `McpRequestGuard`, host pipeline | TC-04 | conformant | Prior review; current MCP tests pass. |
| NFR-02 | Safe allowlist | MCP DTOs and reader | TC-01, TC-04 | conformant | No raw snapshot, error text, credential, or session fields in projection; current MCP tests pass. |
| NFR-04 | MCP failure leaves HUD running | `App.Mcp.cs`, host | TC-04, TC-06 | conformant | Prior review and DEC-22 manual evidence; current App build passes. |
| NFR-05, DEC-10 | Concurrent reads and shutdown | `UsageStore` paired state, host stop, `ApplicationLifetime` | TC-05 | conformant | Prior review; current MCP tests pass; DEC-22 tray-exit evidence. |
| TC-07 | Antigravity adapter, HUD text, MCP projection | T05 files and tests | Targeted MTP, manual step 2 | conformant | Targeted Antigravity 3/3, ring 19/19, rows 23/23, MCP 22/22; handoff records unchanged HUD text. |
| T05.6 | Scoped validation of the Antigravity test class | `AntigravityUsageProviderTests` | `--filter-class '*Antigravity*'` | non-conformant | 40 passed, 1 failed on current HEAD; CR-01 below. |
| TC-01–TC-06 | Other unit, integration, and manual scenarios | T01–T03 and correction T04 | MCP suite and DEC-22 | conformant | Prior review plus current MCP suite; manual steps 1, 3–5 remained valid after T05, with step 2 rechecked for Gemini. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `AGENTS.md` Core purity and truthful values | OK | `LimitWindow` remains a pure nullable model; used count is never copied to remaining units. |
| `AGENTS.md` C# structure | OK with baseline note | New `McpWindowProjectionTests.cs` is 85 lines. `AntigravityUsageProviderTests.cs` is 314 lines versus 311 at base; both exceed the 300-line style target, but the breach predates T05 and neither reaches the 500-line profile threshold. |
| `dotnet-efficient-validation` | OK | Native MTP via .NET 10.0.401; all executions used `--minimum-expected-tests 1`, `--no-build`, `--no-restore`, and preserved exit codes. |
| `no-workarounds` | OK for implementation; test issue open | T05 fixes the data source and all consumers. The live test's expectation conflicts with the provider's documented fallback; CR-01 requires a test correction that preserves meaningful coverage. |
| Desktop E2E policy | OK | E2E omitted; unit, integration, and manual evidence treated separately. |

## Quality profile

All QA-01–QA-06 patterns from the TechSpec ran with `rtk rg -n --type cs` over the changed C# files in `src/` and `tests/`; QA-07 counted each changed C# file. This includes the full reviewable code set, not just T05.

| ID | Rule | Class | Hits | State |
| --- | --- | --- | --- | --- |
| QA-01 | Async void and synchronous waits | blocking | 0 | OK |
| QA-02 | Service locator | blocking | 0 | OK |
| QA-03 | Empty catch | blocking | 0 | OK |
| QA-04 | Warning suppression | blocking | 1: local `MCP9004` at `McpServerHost.Pipeline.cs:86` | justified by DEC-11 |
| QA-05 | Wall clock | reservation | 0 | OK |
| QA-06 | Four-plus-argument lexical pattern | reservation | 19 | Existing calls or false positives (dates, strings, `Path.Combine`); no new four-argument signature. |
| QA-07 | File length | reservation | App 415 (base 414), Antigravity tests 314 (base 311), largest new file 290 | No file reaches 500; no new file exceeds 300. |

- Terrain baseline: applied from `techspec.md#terrain-baseline` and base diff for T04 files.
- New actionable reservation hits: 0. Suggested escalation: no trigger fired.
- `git diff --check` over base-to-HEAD passed.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-04–DEC-12, CMP-01–CMP-08 | YES | `codereview_2`; current MCP suite and App build revalidate the integrated state. |
| DEC-24, CMP-09 | YES | `UsedUnits` is carried from Antigravity through HUD and MCP; `docs/MCP.md` documents semantics. |
| TC-07 | YES | Targeted deterministic tests and manual Gemini evidence. |
| T05 scoped validation gate | NO | The required Antigravity class run fails; see CR-01. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `../done/task_01.md` | COMPLETE | T04 reconciliation and codereview_2 approval; current MCP suite passes. |
| T02 | `../done/task_02.md` | COMPLETE | SSE and host tests pass; its handoff records the same pre-existing live Antigravity failure. |
| T03 | `../done/task_03.md` | COMPLETE | App build and DEC-22 manual evidence. |
| T04 | `../codereview_1/done/task_04.md` | COMPLETE | CR-01 resolved in codereview_2 and unchanged. |
| T05 | `../done/task_05.md` | INCOMPLETE validation | Behavior and targeted tests conform; its required `*Antigravity*` scoped test run fails. |

## Executed validations

- Validated state: clean `bc4208d` HEAD, Debug, .NET SDK 10.0.401, Windows. xUnit v3 uses native Microsoft.Testing.Platform.
- Reused evidence: codereview_2 for T01–T04; DEC-22 manual script; T05 manual Gemini handoff. Those paths were not changed by this review.
- Manual acceptance: DEC-22 steps 1, 3–5 passed. T05 rechecked Gemini through SSE and a HUD tooltip; the saved raw T05 client output is a `get_provider_metrics` call, while the handoff says step 2 was rechecked. The `list_provider_metrics` T05 path is corroborated by its shared mapping and automated tests, but the exact post-T05 list call is not retained in that file.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | Pass; 0 errors, 0 warnings | Infrastructure and test compilation |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | Pass; 0 errors, 0 warnings | App and HUD compilation |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class '*Mcp*'` | Pass; 22 tests | TC-01–TC-05, TC-07 MCP |
| Same command, `--filter-class '*ProviderRingViewModel*'` | Pass; 19 tests | TC-07 HUD ring |
| Same command, `--filter-class '*ProviderUsageRowFactory*'` | Pass; 23 tests | TC-07 HUD rows |
| Same command, `--filter-method '*WhenLanguageServerUnavailable*'`, `'*WhenCloudCode403*'`, and `'*WhenZeroRequestsToday*'` separately | Pass; 1 each | TC-07 Antigravity deterministic cases |
| `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class '*DomainModels*'` | Pass; 10 tests | Core model compatibility |
| Infrastructure tests, `--filter-class '*Antigravity*'` | **Fail; 40 passed, 1 failed, exit 2** | T05.6 scoped validation |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Medium | `done/task_05.md` T05.6 and acceptance criterion “Scoped tests pass” | `tests/TokenHound.Infrastructure.Tests/Providers/Antigravity/AntigravityUsageProviderTests.cs:44-55` asserts `Fidelity.Official` whenever `agy` runs; current run returned `Derived`. `AntigravityUsageProvider.cs:81-104` permits transcript fallback when official metrics are unavailable. The same failure is recorded in `done/task_02.md`, before T05. | The required Antigravity class validation cannot pass on a supported live fallback state. This blocks an APPROVED opinion, though no T05 product behavior defect is proven. | Correct the live test's contract so it validates the actual status/fidelity and window semantics of the returned source without assuming process presence guarantees official metrics. Retain deterministic official and derived tests; rerun the full scoped class. |

## Previous findings

| Review/ID | State | Current evidence |
| --- | --- | --- |
| `codereview_1/CR-01` | resolved | Paired state implementation and regression tests unchanged; current 22-test MCP suite passes. |

## Limitations and open items

- CR-01 is pre-existing test debt, but the T05 contract newly makes its failing class run mandatory. The review status follows the explicit failing mandatory test rule.
- A pre-T05 archived Antigravity snapshot may carry the old count in `RemainingUnits` until refresh; DEC-25 accepts this temporary upgrade behavior.
- This session had no Windows MCP `App` or `Screenshot` tool, so it reused recorded visible-desktop evidence. The raw T05 file retains the lookup result, not the full list or screenshot.
- Unrelated task-folder deletions committed before the feature commit were excluded from this opinion.

## Conclusion

T05's implementation and targeted regression checks conform to DEC-24 and TC-07. The approved T05 validation gate still fails because a pre-existing live test asserts an unsupported invariant. Correct that test and obtain a new independent re-review before HIL 3.
