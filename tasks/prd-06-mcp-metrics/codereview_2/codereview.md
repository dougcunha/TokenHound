# Code review report — MCP access to live provider metrics

## Summary

- Status: APPROVED
- Git scope: `97f17c79a5afeebbd8ff3a534cced7681b382476` through the current uncommitted worktree, limited to the MCP feature files named below.
- Previous review: `tasks/prd-06-mcp-metrics/codereview_1/codereview.md` (REJECTED for CR-01).

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-06-mcp-metrics/prd.md` | Read; SHA-256 `eaab8d01…2565` matches workflow DEC-02. |
| TechSpec | `tasks/prd-06-mcp-metrics/techspec.md` | Read; SHA-256 `0faf33b4…3546` matches workflow DEC-16. |
| Manifest | `tasks/prd-06-mcp-metrics/tasks.md` | Read; T01–T03 links resolve to `done/`; state and Problems and solutions record the CR-01 reopen and reconciliation. |
| Correction | `codereview_1/done/task_04.md` | Read; handoff complete. |
| Implementation | `src/TokenHound.Infrastructure/Mcp/`, `src/TokenHound.Infrastructure/Engine/UsageStore.cs`, `UsageStore.Refresh.cs`, `UsageStore.Metrics.cs`, `src/TokenHound.App/App.Mcp.cs`, `App.xaml.cs`, `ApplicationLifetime.cs`, `TokenHound.Infrastructure.csproj`, `tests/TokenHound.Infrastructure.Tests/Mcp/`, `docs/MCP.md`, `README.md` | Delimited by the base, handoffs, and worktree, including untracked files. |

Files modified after `codereview_1` (by mtime against that report): exactly the five T04 files — `UsageStore.cs`, `UsageStore.Refresh.cs`, `UsageStore.Metrics.cs`, `McpMetricsReader.cs`, `McpMetricsReaderTests.cs`. Every other feature file is unchanged since the first review, so its conformant states are reused and revalidated by the builds and tests below.

Unrelated tracked deletions under `tasks/prd-arch-20260912-*` and `tasks/prd-feat-20260916-01-cline-provider/` are excluded from this opinion (snapshot O-02).

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01, US-01, FR-01 | Local MCP access during the App lifetime | `App.Mcp.cs`, `McpServerHost.cs` | TC-03; App build | Conformant for code; desktop check pending HIL 3 | Unchanged since codereview_1; App build rerun with 0 warnings. |
| OBJ-02, FR-07 | Cache-only reads without provider dispatch | `McpMetricsReader.cs`, `UsageStore.Metrics.cs:GetMetricsState` | TC-02, TC-03 | Conformant | `GetMetricsState` only reads dictionaries under a lock; `ConcurrentReads_DuringRefreshStayConsistent` asserts exactly 3 fetches for 3 explicit refreshes despite continuous reads. |
| US-02, FR-04 | Lookup with distinct missing states | `McpMetricsReader.GetMetrics`, `GetLookupState` | TC-01, TC-03 | Conformant | States preserved; a snapshot vanishing between checks falls back to `pending`. |
| FR-02 | Legacy SSE transport | `McpServerHost.Pipeline.cs` | TC-03 | Conformant | Real SDK SSE client tests pass. |
| FR-03 | Enabled real provider list | `McpMetricsReader.ListMetrics` | TC-01, TC-03 | Conformant | Sorted; mock and disabled excluded; one paired state per provider. |
| FR-05, NFR-03, DEC-07 | Truthful status, timestamps, quota, blocks | `McpMetricsReader.MapProvider`, `UsageStore.Refresh.cs:130-143`, `:196-201`, `UsageStore.Metrics.cs:9-24` | TC-01 | Conformant | CR-01 resolved (see previous findings). `SnapshotRetentionPolicy` archives every `ok` reading, so an `ok` snapshot always pairs with its own time. |
| FR-06 | Copilot billing and Cline data | `McpMetricsReader.Copilot.cs`, `.Cline.cs` | TC-01 | Conformant | Unchanged since codereview_1. |
| FR-08 | Client setup and field semantics | `README.md`, `docs/MCP.md` | Documentation review | Conformant for written contract | Unchanged; interactive client setup is part of TC-06. |
| NFR-01 | Loopback and Host/Origin boundary | `McpServerHost.Pipeline.cs`, `McpRequestGuard.cs` | TC-04 | Conformant | Unchanged; tests pass. |
| NFR-02 | No credential or raw diagnostic fields | MCP DTOs, `McpMetricsReader` | TC-01 | Conformant | Allowlist projection unchanged; `MetricsState` is internal. |
| NFR-04 | HUD survives MCP startup failure | `App.Mcp.cs`, `McpServerHost.StartAsync` | TC-04 | Conformant for code; desktop check pending HIL 3 | Unchanged. |
| NFR-05, DEC-10 | Concurrent reads and orderly shutdown | `UsageStore` lock pairing, `McpServerHost.StopAsync`, `ApplicationLifetime.ShutdownCoreAsync` | TC-05 automated half | Conformant for automated half; tray exit pending HIL 3 | Concurrency test passed 5 repeated runs plus the full scoped run. Archive I/O and `SnapshotUpdated` stay outside the lock (`UsageStore.Refresh.cs:124-127`). |
| TC-01–TC-04 | Unit and HTTP/SSE integration | MCP test classes | Scoped MTP | Conformant | 20 passed, exit 0. |
| TC-05 manual, TC-06 | Tray exit and visible desktop comparison | TechSpec manual script | — | Not verifiable here; routed to HIL 3 | The TechSpec assigns these to HIL 3; no Windows desktop run was executed in this review. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `AGENTS.md` Core purity, no invented values | OK | Changes stay in Infrastructure; nullable timestamp preserved. |
| `AGENTS.md` C# structure | OK | T04 files: `UsageStore.cs` 289, `UsageStore.Refresh.cs` 264, `McpMetricsReaderTests.cs` 290 lines (all ≤ 300); new methods ≤ 30 lines; multi-argument signature split per rule. |
| `dotnet-efficient-validation` | OK | Native MTP (xUnit v3); `--no-build --no-restore`, `--minimum-expected-tests 1`, exit codes preserved. |
| `no-workarounds` | OK | Fix addresses publication ordering itself; no timing hooks or retries added. |
| Desktop E2E policy | OK | E2E omitted by .NET desktop policy; not counted as evidence. |

## Quality profile

Commands ran over the TechSpec-scoped files plus the T04-touched `UsageStore.cs` and `UsageStore.Refresh.cs`.

| ID | Rule | Class | Command | Hits | State |
| --- | --- | --- | --- | --- | --- |
| QA-01 | Async void, sync waits | Blocking | `rg 'async void\|\.Result\b\|\.Wait\(\)\|GetAwaiter\(\)\.GetResult\(\)'` | 0 | OK |
| QA-02 | Service locator | Blocking | `rg 'GetRequiredService<\|GetService<\|ServiceLocator'` | 0 | OK |
| QA-03 | Empty broad catch | Blocking | `rg 'catch\s*\{\s*\}\|catch\s*\(Exception\w*\)\s*\{\s*\}'` | 0 | OK |
| QA-04 | Warning suppression | Blocking | `rg '#nullable disable\|#pragma warning disable'` | 1 (`McpServerHost.Pipeline.cs:86`, `MCP9004`) | Justified by DEC-11 |
| QA-05 | Wall clock | Reservation | `rg 'DateTime\.(Now\|UtcNow)'` | 0 | OK |
| QA-06 | Four-plus-argument signatures | Reservation | `rg '\w+\((?:[^),]+,){3,}[^)]*\)'` | 4 lexical: baseline `App.xaml.cs:333`, two test `DateTimeOffset` constructors, one description string | Pre-existing or false positive |
| QA-07 | File length | Reservation | `wc -l` | Max new file 290; `App.xaml.cs` 415 vs baseline 414; `UsageStore.Refresh.cs` 237→264, `UsageStore.cs` 288→289 | OK |

- Terrain baseline: applied from `techspec.md#terrain-baseline`; `UsageStore.cs` and `UsageStore.Refresh.cs` were not in it (touched only by T04) and were measured against the Git base directly.
- Hits discounted by baseline or as lexical false positives: 4 (QA-06).
- Reservations accumulated in the feature: 0.
- Suggested escalation: no trigger fired.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-04–DEC-06, CMP-01–CMP-05, CMP-07 | YES | Unchanged since codereview_1. |
| DEC-07 | YES | Snapshot and retained-success time published and read as one state (`GetMetricsState`). |
| DEC-08–DEC-12, CMP-06, CMP-08 | YES for code and docs | Unchanged; App build revalidated. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | COMPLETE | Original handoff preserved; correction reconciliation at line 82 links CR-01 and T04, confirmed by this review. |
| T02 | `done/task_02.md` | COMPLETE | Transport tests pass on the corrected state. |
| T03 | `done/task_03.md` | COMPLETE for code/docs | App build passes; manual items remain HIL 3. |
| T04 (correction) | `codereview_1/done/task_04.md` | COMPLETE | Handoff claims verified: lock pairing, reader consumption, coordinated test, builds, tests. |

## Executed validations

- Profile and exclusions: native MTP xUnit v3 for `TokenHound.Infrastructure.Tests`; WPF App build; E2E omitted by .NET desktop policy.
- Validated state: current uncommitted worktree on Git base `97f17c7`, Debug, .NET SDK 10.0.401, Windows loopback.
- Reused evidence: codereview_1 conformance for files unchanged since that review; T02 restore (package versions unchanged).
- Manual acceptance: TC-06 and the tray-exit half of TC-05 not executed; open for HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | Build of corrected Infrastructure |
| `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` | passed, 0 warnings | CMP-06, DEC-10 |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Mcp*"` | passed, 20 tests, exit 0 | TC-01–TC-05 automated |
| Same project, `--filter-method "*ConcurrentReads*"`, ×5 | passed 5/5 | CR-01, NFR-05 |
| QA-01–QA-07 `rg` commands | See quality profile | Quality rules |
| `git diff --check 97f17c7 -- src README.md` | exit 0 | Patch whitespace |

## Findings

None.

## Previous findings (re-review only)

| Review/ID | State | Current evidence |
| --- | --- | --- |
| codereview_1/CR-01 | resolved | All writers of `_snapshots` and `_lastGoodSnapshots` (`UsageStore.Refresh.cs:136-141`, `:199-200`) hold `_snapshotStateLock`; the only paired reader (`UsageStore.Metrics.cs:14-23`) takes the same lock; `McpMetricsReader.MapProvider` consumes that pair. Regression test asserts `lastSuccessfulAtUtc == snapshotFetchedAtUtc` for `ok` and `null` after history clear while reads overlap both transitions. |

## Limitations and open items

- TC-06 and the tray-exit half of TC-05 were not executed. The TechSpec routes them to HIL 3; they remain open and must not be reported as passed.
- The regression test cannot deterministically force the former publication gap; the fix is proven by code inspection of lock coverage, with the test asserting the public invariant under overlap.
- `StoreSnapshotAsync` reads `_lastGoodSnapshots` (`UsageStore.Refresh.cs:116`) before the locked publish. It is pre-existing and matters only if two refreshes of the same provider run at once. That is outside CR-01 and not a finding.
- README client configurations were not exercised in each client; the SDK SSE test proves the endpoint.
- This review session authored none of T01–T04.

## Conclusion

CR-01 is resolved and no new finding was introduced. All automated obligations are conformant, tasks are complete with intact links, builds and 20 scoped tests pass, and the quality profile has no unjustified blocking hit. The review is APPROVED. The manual desktop checks (TC-06 and the tray-exit half of TC-05) remain open for HIL 3.
