# Stable execution context

Load in this exact order:

1. `tasks/prd-copilot-ai-credits/prd.md`
2. `tasks/prd-copilot-ai-credits/techspec.md`
3. This file

Use current source versions already loaded; recover only missing or changed sources. The manifest [tasks.md](tasks.md) is authoritative for dependencies and state.

# T05: Recover bounded historical consumption when billing is unavailable

## Outcome

Accessible daily user reports supply explicitly historical or partial consumption for the resolved managed scope, with bounded progress, no overlapping sums, and safe signed downloads.

## Dependencies and boundaries

- Depends on: T01, T02, T03, T04.
- Unblocks: T07.
- In scope: metrics manifests/downloads, NDJSON validation, per-day commit/revision logic, resumable bounded service path, and tests.
- Out of scope: personal metrics endpoint invention, monthly conversion of 28-day totals, invoice reconciliation, and new quota/credential formulas.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-04, FR-05, FR-06, FR-08 through FR-12; NFR-01 through NFR-04 | PRD requirements | Fallback source semantics, compatible aggregation, bounded independent retrieval. |
| DEC-03/04/05/06/07; CMP-02/05/06/07; TC-04/05/06/07/08/09; OI-03 | TechSpec | Daily reports, safe transport, coverage, and persisted progress. |

## Context to recover on demand

- Skills: repository-cli-efficiency, dotnet-efficient-validation.
- Existing integration: T04 billing service and T03 request gate/archive; T01 report dossier.
- Contract: TechSpec Historical report processing and signed-download requirements.

## Work

- [x] T05.1 Add organization/enterprise daily manifest requests and separate credential-free signed report download handling with T01-validated host/redirect/resource guards.
- [x] T05.2 Stream typed NDJSON rows, validate scope/day/user/decimal fields, and reject conflicting duplicate rows.
- [x] T05.3 Track all partitions and commit complete day aggregates atomically; retain uncommitted progress without presenting it as complete or storing signed secrets.
- [x] T05.4 Integrate billing preference plus fallback, preserving distinct source/fidelity/coverage. Use missing-day and rotating revision progress without starving reports behind repeated billing probes.
- [x] T05.5 Demonstrate 15-second/four-dispatch bounds with multi-pass manifests, expired/revised manifests, partial periods, missing users, and report-only corrections.
- [x] T05.6 Add 429-mid-download, cancellation, unsafe redirect, zero/absent quantity, overlap, and cache-restart regressions.

## Acceptance criteria

- Each compatible day contributes once; billing and historical totals are never added together.
- Missing days/users/partitions remain partial; 204/404 or empty payload never proves zero.
- Signed hosts receive no bearer token; expired/revised manifests cannot double-count a partial day.
- Large reports make bounded multi-pass progress using validated stream guards. Unknown resource/partition semantics keep OI-03 open.
- Historical data retains reporting cutoff and estimated fidelity; partial data cannot yield a full-period remaining balance.

## Verification

- Unit: per-row parsing and day aggregation/duplicate logic.
- Integration: TC-04/06/07/08/09 fake HTTP streams plus real temporary archive; sanitized T01 reports validate actual shapes separately.
- E2E: omitted by .NET desktop policy.
- Manual: review report/source metadata and T01 real-response evidence.
- Commands: Follow the TechSpec validation profile: effective SDK 10.0.400, xUnit v3/MTP executable route, Release, restore only when needed, then build the affected test project with `--no-restore -c Release --nologo --verbosity:minimal`. Preserve each build exit code. Reuse a valid build for unchanged inputs; inspect actual execution counts and failures. Use `rtk proxy` only for hidden failure details. No publish or full solution validation is required.

```powershell
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
```

- Environment dependency: T01/OI-03 report contracts and already-authorized reporting access for real validation; fixture tests require no network.
- Expected evidence: tested multi-pass timeline, preserved deadlines, no signed-secret persistence, and explicit complete/partial totals.

## Affected files

- Create: Infrastructure/Providers/Copilot/CopilotMetricsClient.cs, CopilotMetricsReportParser.cs, CopilotMetricsManifest.cs, CopilotMetricsUserRow.cs.
- Modify: CopilotBillingService.cs and Engine/UsageArchive.CopilotBilling.cs.
- Create/modify: Infrastructure.Tests/Providers/Copilot report/service tests and sanitized Fixtures.
- Reuse unchanged: Core credit compatibility policy; revise only if a real contract exposes an unresolved inconsistency.

## Observability and recovery

- Signal: source fallback, covered/missing days, partition completion, budget deferral, and download error category without URLs/user rows.
- Recovery: stop report dispatch and retain eligible values as stale; rollback must not erase direct billing cache or deadlines. Replace corrupted uncommitted work only for its scoped day.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Bounded historical daily user report ingestion and fallback system for Copilot AI credits when direct billing is unavailable. Fully implements manifest retrieval via rate-limited gate, credential-free signed downloads with redirect and private IP guards, streaming NDJSON line-by-line parsing with deduplication and conflict validation, multi-partition tracking and atomic daily summary persistence, 15-second pass timeout and 4-dispatch budget enforcement, and integration into `CopilotBillingService` respecting the absolute rule against summing direct billing and historical reports together.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsManifest.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsUserRow.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsReportParseResult.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsReportParser.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotPassDispatchBudget.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotDownloadUriValidator.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/DisposingStream.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotMetricsClient.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.Direct.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.Historical.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.Mapping.cs`
  - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingClient.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotMetricsReportParserTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotMetricsClientTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingServiceHistoricalTests.cs`
  - `tasks/prd-copilot-ai-credits/task_05.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` -> 0 errors, 0 warnings.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"` -> 106 passed, 0 failed.
  - `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"` -> 27 passed, 0 failed.
  - `rtk git diff --check` -> Clean, 0 whitespace errors.
- Validated state: Release configuration, net10.0, zero compile warnings/errors, all 133 Copilot test cases passing. C# invariants verified (all classes <= 300 lines, methods <= 30 lines, nesting <= 3 levels, sealed classes, XML docs on public members).
- Open items:
  - `OI-03` remains open: real payload size limits and authoritative token contract evidence for multi-partition enterprise reports when live data becomes accessible.

### ADR candidates

None - direct TechSpec implementation adhering to architectural boundaries, storage contracts, and security rules.

