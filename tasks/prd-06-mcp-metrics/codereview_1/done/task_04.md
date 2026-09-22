# Stable execution context

Load in this exact order:

1. `tasks/prd-06-mcp-metrics/codereview_1/codereview.md`
2. This file

Recover the relevant PRD, TechSpec, original T01 contract, and named code spans on demand.

---

# T04 — Return coherent snapshot and success time

## Outcome

Each MCP provider result pairs its current immutable snapshot with the successful-reading timestamp belonging to that same published state, even while a provider refresh or history clear runs concurrently.

## Dependencies and boundaries

- Depends on: the existing T01–T03 implementation and `codereview_1/CR-01`; T01 is reopened for evidence reconciliation, not a prerequisite completion gate.
- Unblocks: T01 evidence reconciliation and an independent re-review.
- In scope: atomic publication and reading of the store's current snapshot and retained-good timestamp; MCP reader consumption; a coordinated regression test.
- Out of scope: provider adapters, polling cadence, network dispatch, credential ownership, archive format, HUD geometry, and new MCP fields.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_1/CR-01` | `codereview.md#findings` | A concurrent read can pair the new snapshot with old or cleared success metadata. |
| FR-05, NFR-03, NFR-05 | `prd.md#functional-requirements`, `#non-functional-requirements` | Preserve truthful reading age and concurrent read behavior. |
| DEC-07, TC-01, TC-05 | `techspec.md#technical-decisions`, `#test-approach` | Keep current and retained-success timestamps coherent. |
| T01 | `task_01.md#acceptance-criteria` | Reconcile the original task's incomplete timestamp obligation. |

## Requirements

- An `ok` snapshot must never be paired with a null or earlier successful timestamp caused by publication order.
- A cleared-history status must not expose a timestamp from the prior good reading.
- A stale snapshot with retained values must report the successful timestamp associated with those values.
- MCP reads remain synchronous, cache-only, and free of provider dispatch or archive writes.
- Keep `UsageStore.CurrentSnapshots` and existing HUD event behavior intact.

## Context to recover on demand

- TechSpec: `#contracts-and-data`, `#integrations-and-interfaces`, `#test-approach`.
- Rules and skills: `AGENTS.md`, `dotnet-efficient-validation`, `repository-cli-efficiency`, `no-workarounds`.
- Code: `UsageStore.Refresh.StoreSnapshotAsync`, `UsageStore.Refresh.LoadArchive`, `UsageStore.Metrics.GetLastSuccessfulAtUtc`, `McpMetricsReader.ListMetrics/GetMetrics/MapProvider`, and `SnapshotRetentionPolicy.Apply`.

## Work

- [x] T04.1 Add a store read boundary that returns the current snapshot and retained-success timestamp as one coherent provider state; synchronize the existing store publication paths, including archive restoration, without holding a lock across archive I/O or callbacks.
- [x] T04.2 Make list and lookup projections use that coherent state once per provider; preserve lookup states, sorting, nulls, and the two-tool wire contract.
- [x] T04.3 Coordinate the concurrent read test so reads begin before success and history-clear transitions; assert the timestamp invariant, stale retained values, and no extra provider fetch from reads.
- [x] T04.4 Run affected build and scoped MTP tests and inspect the TechSpec quality profile for touched files. The flow coordinator reconciles T01's manifest and handoff after this task.

## Acceptance criteria

- Current snapshot and `lastSuccessfulAtUtc` in one MCP provider object always belong to the same published store state under concurrent refresh.
- Existing `ok`, stale, unavailable, and synthetic behavior remains unchanged apart from the corrected timestamp pairing.
- The source order in `codereview_1/CR-01` proves the old interleaving; a concurrent regression check starts reads before both refresh transitions and asserts coherent results. The store's paired write and read use the same lock, so the partial state cannot be observed.
- T01's state and handoff accurately reflect the corrected evidence; TC-06 and the manual half of TC-05 remain explicit HIL 3 items.

## Verification

- Unit: coordinated `McpMetricsReaderTests` coverage for success, stale retention, and history clear during concurrent reads.
- Integration: rerun the scoped MCP project tests, including the real SSE client, because the reader and store code change.
- E2E: omitted by .NET desktop policy.
- Manual: TC-06 and tray-exit half of TC-05 remain for HIL 3; this correction does not add a new desktop step.
- Environment dependency: existing .NET 10 SDK, restored packages, and local loopback availability; no provider account is needed.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*Mcp*"`; build the App with `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` if the changed Infrastructure assembly invalidates its prior build. Preserve and verify `$LASTEXITCODE`.
- Expected evidence: zero build errors, nonzero passing MCP tests, a specific test for CR-01, and a scoped QA-01–QA-07 result.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Engine/UsageStore.cs`, `UsageStore.Refresh.cs`, `UsageStore.Metrics.cs`, `src/TokenHound.Infrastructure/Mcp/McpMetricsReader.cs`, `tests/TokenHound.Infrastructure.Tests/Mcp/McpMetricsReaderTests.cs`, `tasks/prd-06-mcp-metrics/tasks.md`, and `tasks/prd-06-mcp-metrics/done/task_01.md` as required by evidence reconciliation.
- Create: no production file is required; a small test helper may be added if it makes the interleaving deterministic.

## Observability and recovery

- Operational signal: no new logging; the test asserts coherent timestamp semantics and existing host diagnostics remain.
- Recovery: revert the coherent-read boundary and reader change if necessary; no persisted schema changes are involved.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `UsageStore.GetMetricsState` returns one current snapshot and retained-success timestamp under `_snapshotStateLock`. `StoreSnapshotAsync` publishes both values under that lock, as does archive restoration; archive I/O and `SnapshotUpdated` stay outside it. `McpMetricsReader` projects the paired state for list and lookup. The existing concurrency test now starts its reader loop before two refreshes (success then history clear) and checks timestamp coherence; stale retention remains covered by `Reads_KeepStaleProvenanceAndNeverFetchProvider`. The reviewed source order is the original failure proof; the test checks the corrected invariant without adding a production-only timing hook.
- Changed files: `src/TokenHound.Infrastructure/Engine/UsageStore.cs`, `UsageStore.Refresh.cs`, `UsageStore.Metrics.cs`, `src/TokenHound.Infrastructure/Mcp/McpMetricsReader.cs`, `tests/TokenHound.Infrastructure.Tests/Mcp/McpMetricsReaderTests.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` passed after a missing `TokenHound.Core.Models` using was corrected; final build had 0 errors and 0 warnings. Scoped MCP MTP command passed 20 tests, 0 warnings, exit 0. `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore --nologo --verbosity:minimal` passed with 0 errors and 0 warnings after the production edit. QA-01–QA-05 had no new hits in touched files; QA-06 matched only the existing test fixture `DateTimeOffset` constructor; QA-07 maximum was 290 lines. `rtk git diff --check` passed for tracked correction files.
- Validated state: current uncommitted correction plus original MCP feature files on Git base `97f17c79a5afeebbd8ff3a534cced7681b382476`, Debug/.NET 10.0.401 on Windows. Build and tests were rerun after the code edit; App build remains valid after the final test-only edit.
- Open items: The flow coordinator reopened and then reconciled original T01, preserving its first handoff and adding the corrected evidence in `done/task_01.md`. TC-06 and the tray-exit half of TC-05 remain for HIL 3. The concurrency test does not force the former nanosecond publication gap; the lock pairing is verified by code inspection and the test asserts the public invariant while reads overlap updates.
