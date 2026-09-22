# Code review report — MCP access to live provider metrics

## Summary

- Status: REJECTED
- Git scope: `97f17c79a5afeebbd8ff3a534cced7681b382476` through the current worktree, limited to the MCP feature files named below.
- Previous review: none.

## Sources and scope

| Source | Path or reference | State |
| --- | --- | --- |
| PRD | `tasks/prd-06-mcp-metrics/prd.md` | Read; SHA-256 matches workflow DEC-02. |
| TechSpec | `tasks/prd-06-mcp-metrics/techspec.md` | Read; SHA-256 matches workflow DEC-16. |
| Manifest | `tasks/prd-06-mcp-metrics/tasks.md` | Read; T01–T03 links and `done/` files exist. |
| Implementation | `src/TokenHound.Infrastructure/Mcp/`, `src/TokenHound.Infrastructure/Engine/UsageStore.Metrics.cs`, `src/TokenHound.App/App.Mcp.cs`, `App.xaml.cs`, `ApplicationLifetime.cs`, `TokenHound.Infrastructure.csproj`, `tests/TokenHound.Infrastructure.Tests/Mcp/`, `docs/MCP.md`, `README.md` | Delimited by the base and task handoffs; includes new untracked files. |

Unrelated tracked deletions under `tasks/prd-arch-20260912-*` and `tasks/prd-feat-20260916-01-cline-provider/` were already present at resumption and are excluded from this opinion.

## Coverage matrix

| Source | Obligation | Implementation | Test | State | Evidence |
| --- | --- | --- | --- | --- | --- |
| OBJ-01, US-01, FR-01 | Local MCP access during the App lifetime | `App.Mcp.cs`, `McpServerHost.cs` | TC-03; App build handoff | Conformant for code; desktop check pending HIL 3 | Production port 37653 and routes are configured; the SSE client integration test passes. |
| OBJ-02, FR-07 | Cache-only reads without new provider dispatch | `McpMetricsReader.cs`, `McpMetricsTools.cs` | TC-02, TC-03 | Conformant | Reader uses `UsageStore` state; tests prove repeated reads do not invoke the provider. |
| US-02, FR-04 | Lookup and distinct missing states | `McpMetricsReader.GetMetrics` | TC-01, TC-03 | Conformant | Unknown, disabled, pending, synthetic, and available are represented; blank IDs are tool errors. |
| FR-02 | Legacy SSE transport | `McpServerHost.Pipeline.cs` | TC-03 | Conformant | Stateful SSE opt-in and real SDK client exchange pass. |
| FR-03 | Enabled real provider list | `McpMetricsReader.ListMetrics` | TC-01, TC-03 | Conformant | Registered enabled snapshots are sorted; mock and disabled providers are excluded. |
| FR-05, NFR-03, DEC-07 | Truthful status, fidelity, timestamps, quota, and blocks | `McpMetricsReader.MapProvider`, `UsageStore.Metrics.cs` | TC-01 | Non-conformant | CR-01: `lastSuccessfulAtUtc` can belong to a different state than the returned snapshot during concurrent refresh. |
| FR-06 | Copilot billing and Cline data | `McpMetricsReader.Copilot.cs`, `McpMetricsReader.Cline.cs` | TC-01 | Conformant | Allowlisted billing, account, and local fields are projected. |
| FR-08 | Client setup and field semantics | `README.md`, `docs/MCP.md` | Documentation review | Conformant for written contract | Both URLs, tools, lookup states, nullable values, and client examples are present. Interactive setup remains part of TC-06. |
| NFR-01 | Loopback and Host/Origin boundary | `McpServerHost.Pipeline.cs`, `McpRequestGuard.cs` | TC-04 | Conformant | Kestrel binds IPv4 loopback; foreign Host and Origin receive 403. |
| NFR-02 | No credential or raw diagnostic fields | MCP DTOs and `McpMetricsReader` | TC-01 | Conformant | Projection is an allowlist; tests exclude raw error, reason, owner, and filter strings. |
| NFR-04 | HUD survives MCP startup failure | `App.Mcp.cs`, `McpServerHost.StartAsync` | TC-04; App build handoff | Conformant for code; desktop check pending HIL 3 | Occupied port returns false and logs; App keeps startup asynchronous. |
| NFR-05, DEC-10 | Concurrent reads and orderly shutdown | `McpServerHost.StopAsync`, `ApplicationLifetime.ShutdownCoreAsync` | TC-05 automated half | Non-conformant in concurrent result semantics | Host stop tests pass, but CR-01 permits a mixed snapshot/timestamp result. Tray exit is pending HIL 3. |
| TC-01–TC-04 | Unit and HTTP/SSE integration checks | MCP test classes | Scoped MTP command | Conformant execution | 20 tests passed with minimum expected tests enforced. TC-01 lacks an assertion for the CR-01 interleaving. |
| TC-05–TC-06 | Shutdown and visible desktop acceptance | Host tests and TechSpec manual script | TC-05 automated half passed | Pending HIL 3 | This host exposes no Windows MCP App or Screenshot tool; tray exit and HUD comparison were not executed. |

## Compliance with rules and skills

| Rule or skill | State | Evidence |
| --- | --- | --- |
| `AGENTS.md` Core purity and truthful quota values | OK | New hosting code is in Infrastructure; projection passes nullable quota fields through. |
| `AGENTS.md` C# structure | OK within the TechSpec baseline | New C# files are below 300 lines; `App.xaml.cs` rose from 414 to 415 lines without extending its existing large methods. |
| `dotnet-efficient-validation` | OK | Native MTP selected by `global.json` and xUnit v3 project; scoped test command used `--no-build --no-restore` and `--minimum-expected-tests 1`. |
| `no-workarounds` | OK for review | The single warning suppression is local to approved legacy SSE opt-in. CR-01 is traced to update ordering, not attributed to an unproven test failure. |
| Desktop E2E policy | OK | No E2E suite was run or counted as evidence. |

## Quality profile

The TechSpec's QA-01–QA-07 commands ran over the touched C# files, with `bin/` and `obj/` excluded by the explicit paths.

| ID | Rule | Class | Hits | State |
| --- | --- | --- | --- | --- |
| QA-01 | Async void and synchronous waits | Blocking | 0 | OK |
| QA-02 | Service locator | Blocking | 0 | OK |
| QA-03 | Empty broad catch | Blocking | 0 | OK |
| QA-04 | Warning suppression | Blocking | 1 local `MCP9004` | Justified by DEC-11 |
| QA-05 | Wall clock use | Reservation | 0 | OK |
| QA-06 | Constructor/signature growth | Reservation | 4 lexical hits: one baseline App call, two test time constructors, one description string | No new semantic hit |
| QA-07 | File length | Reservation | Largest new file: 271 lines; modified `App.xaml.cs`: 415 versus baseline 414 | OK under profile threshold |

- Terrain baseline: applied from `techspec.md#terrain-baseline` at the recorded Git base.
- Hits discounted by baseline or lexical false positives: four QA-06 hits.
- Reservations accumulated in this feature: zero.
- Suggested escalation: no trigger fired.

## TechSpec adherence

| Decision or contract | State | Evidence |
| --- | --- | --- |
| DEC-04–DEC-06, CMP-01–CMP-05, CMP-07 | YES | SDK 2.2.0 and ASP.NET Core reference in Infrastructure; exactly two structured tools; allowlisted DTO projection. |
| DEC-07, per-provider timestamp coherence | NO | CR-01. |
| DEC-08–DEC-11, CMP-06, CMP-08 | YES for code and written docs | Fixed URL, bounded local transport, local `MCP9004` suppression, App startup/shutdown wiring, and client guide. |
| DEC-12 | YES | App-specific composition is in `App.Mcp.cs`; `App.xaml.cs` adds one call. |

## Verified tasks

| Task | Location | State | Handoff and evidence |
| --- | --- | --- | --- |
| T01 | `done/task_01.md` | INCOMPLETE | Projection and seven tests exist, but CR-01 leaves its truthful timestamp acceptance incomplete. |
| T02 | `done/task_02.md` | COMPLETE for its transport scope | Thirteen HTTP/SSE integration tests and host safeguards passed; its tools expose the T01 projection. |
| T03 | `done/task_03.md` | COMPLETE for its code/documentation scope | App build is recorded with zero warnings; manual TC-05/TC-06 obligations remain at HIL 3. README's later client section is in review scope. |

The manifest marks all three tasks complete. T01 must be reconciled by its DAG owner after CR-01 is corrected; a completed correction task alone cannot close the original obligation.

## Executed validations

- Profile and exclusions: native MTP xUnit v3 for Infrastructure tests; desktop E2E omitted by repository policy.
- Validated state: current uncommitted MCP source and test files on Git base `97f17c79a5afeebbd8ff3a534cced7681b382476`, .NET SDK 10.0.401, Windows loopback networking.
- Reused evidence: T03 App build and T02 restore/build handoffs remain applicable because source and package versions have not changed since those checks; the later README change is documentation only.
- Manual acceptance: TC-06 and the tray-exit half of TC-05 remain unexecuted for HIL 3.

| Command | Result | Obligations covered |
| --- | --- | --- |
| `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Mcp*"` | 20 passed, 0 warnings; exit 0 | TC-01–TC-05 automated portion |
| TechSpec QA-01–QA-07 scoped `rtk rg` commands | Results recorded in quality profile | Quality rules |
| `rtk git diff --check 97f17c7 --` tracked feature files | Exit 0 | Patch whitespace |

## Findings

| ID | Severity | Source | Evidence | Impact | Proven recommendation |
| --- | --- | --- | --- | --- | --- |
| CR-01 | Medium | FR-05, NFR-03, NFR-05, DEC-07, T01 | `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs:124-136` publishes `_snapshots[providerId]` before updating or clearing `_lastGoodSnapshots`; `src/TokenHound.Infrastructure/Mcp/McpMetricsReader.cs:84-99` reads the new snapshot and the separate retained timestamp. | A concurrent MCP call can report an `ok` snapshot with a null or previous `lastSuccessfulAtUtc`, or a status transition with a timestamp from another state. The freshness field can misstate the metric's provenance. | Publish the current snapshot and its successful timestamp as one coherent provider state for MCP reads, including archive restoration and history clearing. Add a coordinated regression check for the interleaving and preserve the existing no-dispatch behavior. |

## Previous findings (re-review only)

None.

## Limitations and open items

- This review did not launch the visible desktop. The required Windows MCP `App` and `Screenshot` tools are absent from this host, so TC-06 and the tray-exit portion of TC-05 stay pending for HIL 3. The user's installed TokenHound was reported running in the T03 handoff; launching another instance would duplicate provider polling.
- The seven README client configurations were checked against the current documented syntax where available, but were not exercised in each client. The real SDK SSE client test proves the server endpoint itself.
- The unrelated task-folder deletions were preserved and excluded from the feature scope.

## Conclusion

The local MCP transport and scoped tests pass. CR-01 violates the approved timestamp contract during concurrent refresh, so the review is REJECTED until the projection receives a coherent snapshot and success timestamp. Manual desktop acceptance remains a separate HIL 3 obligation.
