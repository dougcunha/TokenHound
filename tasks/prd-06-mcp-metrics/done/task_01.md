# Stable execution context

Load in this order:

1. `tasks/prd-06-mcp-metrics/prd.md`
2. `tasks/prd-06-mcp-metrics/techspec.md`
3. This file

Recover only changed sources after the approved versions are loaded.

---

# T01 — Project safe current metrics

## Outcome

A reader returns the currently available real provider metrics and explicit per-provider lookup states from `UsageStore`, with truthful freshness and nullable values. Unit tests prove that reading never dispatches a provider request.

## Dependencies and boundaries

- Depends on: HIL 2 approval.
- Unblocks: T02.
- In scope: store timestamp accessor, allowlisted MCP DTOs, read-only projection, unit tests.
- Out of scope: HTTP/SSE hosting, WPF lifecycle, credential and provider adapter changes.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-02, US-02, FR-03–FR-07, NFR-02, NFR-03, NFR-05 | `prd.md#functional-requirements`, `#non-functional-requirements` | Filter visible real providers; preserve status, freshness, and nulls without extra polling. |
| DEC-06, DEC-07, CMP-03–CMP-05, TC-01, TC-02 | `techspec.md#technical-decisions`, `#components-and-flow`, `#test-approach` | Build the safe projection and its unit evidence. |

## Context to recover on demand

- Applicable skills and rules: `repository-cli-efficiency`, `dotnet-efficient-validation`, `no-workarounds`, and the C# rules in `AGENTS.md`.
- Existing code: `UsageStore.cs`, `UsageStore.Gating.cs`, `UsageStore.Refresh.cs`, `SnapshotRetentionPolicy.cs`, and `NotchViewModel.cs` at the spans named in the TechSpec.
- Contract: `techspec.md#contracts-and-data`, especially two timestamps and explicit `lookupState` values.
- Provider spec: `docs/specs/01-READING-STRATEGY-RESILIENCE.md` for stale and rate-limit semantics.

## Work

- [x] T01.1 Add an internal `UsageStore` accessor in a new partial file for the retained last-success timestamp; do not alter refresh or archive behavior.
- [x] T01.2 Add one-record-per-file response DTOs with the exact fields and null semantics in the TechSpec.
- [x] T01.3 Implement list and single-provider lookup in `McpMetricsReader`, using only concurrent store reads and explicit field projection. Exclude mock, disabled, and unmeasured providers from the list; distinguish them by ID lookup.
- [x] T01.4 Add focused unit tests for filtering, stale timestamps, null denominators, Copilot/Cline data, privacy, concurrent reads, and zero provider fetches.

## Acceptance criteria

- The list contains only real, enabled providers with current snapshots, sorted by ID. Lookup distinguishes `available`, `unknown`, `disabled`, `pending`, and `synthetic`.
- Status, fidelity, snapshot time, last success time, window values, block state, Copilot billing, and Cline usage match the allowlist; raw errors and private fields are absent.
- Repeated reads have no upstream request side effects; newly stored readings appear on the next call.

## Verification

- Unit: `McpMetricsReaderTests` covers TC-01 and TC-02, including nullable remaining-only windows and stale retained metrics.
- Integration: none; this task exposes an in-process reader only. T02 proves the MCP wire contract.
- E2E: omitted by .NET desktop policy.
- Manual: none for this task; TC-06 remains at HIL 3.
- Commands: build `tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` with `rtk dotnet build ... --no-restore --nologo --verbosity:minimal` after restoring that project only if assets are absent; run `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*McpMetricsReaderTests*"` and preserve `$LASTEXITCODE`.
- Environment dependency: .NET 10 SDK and existing test assets or one project restore; no provider account or network service.
- Expected evidence: passing named tests with nonzero executed count, clean build, and QA-01–QA-07 check over changed C# files.

## Affected files

- Modify: none of the existing provider adapters or store refresh files.
- Create: `src/TokenHound.Infrastructure/Engine/UsageStore.Metrics.cs`, `src/TokenHound.Infrastructure/Mcp/McpMetricsReader.cs`, `src/TokenHound.Infrastructure/Mcp/McpProviderMetrics.cs`, the other one-record-per-file MCP response DTOs defined by the TechSpec, and `tests/TokenHound.Infrastructure.Tests/Mcp/McpMetricsReaderTests.cs`.

## Observability and recovery

- Operational signal: reader has no network/logging side effect; a host tool call later records only safe request metadata.
- Recovery: remove the new projection files; no persisted state or provider settings are changed.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: In-process reader projects current registered and enabled real provider snapshots into explicit JSON DTOs. Single-ID lookup reports `available`, `unknown`, `disabled`, `pending`, or `synthetic`; no reader path dispatches a provider request. The store accessor reports the retained successful timestamp without changing refresh or archive behavior.
- Changed files: `src/TokenHound.Infrastructure/Engine/UsageStore.Metrics.cs`; 13 new `src/TokenHound.Infrastructure/Mcp/*.cs` DTO and reader files; `tests/TokenHound.Infrastructure.Tests/Mcp/McpMetricsReaderTests.cs`. No existing provider adapter, refresh path, project reference, or UI file changed.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal` passed with 0 errors and 0 warnings. `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*McpMetricsReaderTests*"` passed 7 tests. QA-01–QA-05 had no hits in the touched C# scope; QA-07 maximum was 271 lines. QA-06 matched one six-argument `DateTimeOffset` test fixture construction at test line 20, not a new application dependency or method signature.
- Validated state: Current uncommitted T01 source and test files, existing .NET 10 project assets, no SDK package added yet, and the local Windows/.NET 10 test environment. Unit evidence covers TC-01 and TC-02. T02 protocol and T03 desktop acceptance remain outside this task.
- Open items: None for T01. Manual HUD comparison stays with TC-06 at HIL 3.
- Correction reconciliation (`codereview_1/CR-01`, `codereview_1/done/task_04.md`): The original accessor could expose a retained timestamp from a different publication than the current snapshot. T04 added `UsageStore.GetMetricsState` and paired store publication under `_snapshotStateLock`; the MCP reader now maps that pair. The concurrent unit test starts reads before a success and a history-clear transition and asserts timestamp coherence; the stale-retention test still checks retained values. Final Infrastructure test build and App build passed with zero warnings, and the scoped MCP run passed 20 tests. T01's acceptance criteria are now met on the corrected integrated state; TC-06 and manual tray exit remain HIL 3 obligations.

### ADR candidates

None - direct TechSpec implementation.
