# Stable execution context

Load in this exact order:

1. `tasks/prd-copilot-ai-credits/prd.md`
2. `tasks/prd-copilot-ai-credits/techspec.md`
3. This file

Use current source versions already loaded; recover only missing or changed sources. The manifest [tasks.md](tasks.md) is authoritative for dependencies and state.

# T04: Deliver direct billing beside the existing quota

## Outcome

A resolved, authorized billing scope returns direct AI-credit consumption in the existing Copilot snapshot while quota and billing retain independent success/error history. Unresolved scope yields the specified unavailable state.

## Dependencies and boundaries

- Depends on: T01, T02, T03. OI-01 must establish production ownership mappings for accepted resolved paths.
- Unblocks: T05, T07.
- In scope: scope resolver, direct API/seat DTOs and transport, billing service, provider/store retention integration, composition/disposal, and tests.
- Out of scope: reports (T05), tooltip work (T06), new identity selector or credentials, policy allowances, and unrelated provider refactors.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 through FR-05, FR-07 through FR-12; NFR-01 through NFR-04, NFR-06 | PRD requirements | Direct scoped billing, compatibility, independent history, security, gating. |
| DEC-01 through DEC-07; CMP-03/04/05/06/07/08/10 | TechSpec | Concrete provider integration and composition. |
| TC-01/02/03/05/06/07/08/09/12; OI-01/02/05 | TechSpec | Transport, state transitions, cancellation, and regression evidence. |

## Context to recover on demand

- Skills: repository-cli-efficiency, dotnet-efficient-validation.
- Existing code: Copilot provider/client/discovery/parser; UsageStore.Refresh; SnapshotRetentionPolicy; App.CreateUsageStore/RegisterProviders.
- Contracts: TechSpec Transport and context contracts; Read-only API routes; Gating, persistence, and retention.

## Work

- [x] T04.1 Implement only T01-evidenced principal/owner mappings and nullable internal identity hints; return Unknown/Ambiguous when proof is absent.
- [x] T04.2 Implement direct billing and seat reads with exact routes/headers/filters, nullable decimals, existing User-Agent and gated cancellation/timeout behavior.
- [x] T04.3 Build the billing service's direct-source path, 15-second/four-dispatch bounds, compatible cache restore, same-context stale retention, and structured reasons.
- [x] T04.4 Refactor provider control flow so quota parse/HTTP failure does not skip eligible billing; keep completed quota result per generation before billing and independently commit successful billing.
- [x] T04.5 Wire every Copilot send through the gate, then opt into IRequestGatedUsageProvider and adjust the store's opt-in gating branch. Keep other providers unchanged.
- [x] T04.6 Preserve incoming billing after generic quota retention in every status branch; suppress accidental billing restore from generic last_readings.json and keep quota auth/history rules.
- [x] T04.7 Share one archive instance in App composition, inject owned services/clock, and verify cancellation-before-disposal.
- [x] T04.8 Add route, independent-source, period/account rollover, quota-exhaustion vs HTTP-deadline, cancellation, and non-opt-in store regressions.

## Acceptance criteria

- Personal/org/enterprise calls use verified owners; no organization-membership promotion or silent token substitution.
- A successful billing result survives quota Stale/NeedsAuth/Unsupported; billing failure does not rewrite successful quota status.
- Missing allowance/period evidence suppresses balances; context/seat failure does not erase independently valid compatible usage.
- Operational quota exhaustion does not block billing indefinitely. HTTP 429 gates all later Copilot sends.
- Caller cancellation preserves completed source state without swallowing cancellation or leaking background work.
- A production resolver returning Unknown for every account does not satisfy this task; unresolved runtime cases remain supported.

## Verification

- Unit: resolver and decimal parsing fixtures; policy tests reused rather than duplicated.
- Integration: TC-01/02/03/06/07/08/09 with fake HTTP plus real archive; T01 evidence separately validates real contract semantics.
- E2E: omitted by .NET desktop policy.
- Manual: contract ledger review; real HUD acceptance belongs to T07.
- Commands: Follow the TechSpec validation profile: effective SDK 10.0.400, xUnit v3/MTP executable route, Release, restore only when needed, then build the affected test project with `--no-restore -c Release --nologo --verbosity:minimal`. Preserve each build exit code. Reuse a valid build for unchanged inputs; inspect actual execution counts and failures. Use `rtk proxy` only for hidden failure details. No publish or full solution validation is required. Also build `src/TokenHound.App/TokenHound.App.csproj` in Release with no restore after composition changes, checking its exit code.

```powershell
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreTests*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*SnapshotRetentionPolicyTests*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
```

Build Core.Tests as well if its retention implementation/tests changed; do not reuse stale outputs.

- Environment dependency: T01's real ownership evidence and authorized scope access. Fake HTTP tests may progress while evidence is blocked but do not close resolved-scope acceptance.
- Expected evidence: route/status matrix, nonzero passing tests, shared-archive lifetime assertions, App build, and provenance for each active scope mapping.

## Affected files

- Create: CMP-03 resolver; CMP-04 billing client/response/item/seat records; CMP-05 CopilotBillingService.
- Modify: CopilotQuotaResponse.cs, CopilotApiClient.cs, CopilotUsageProvider.cs; Core/Policies/SnapshotRetentionPolicy.cs; Infrastructure/Engine/UsageStore.Refresh.cs; App/App.xaml.cs.
- Create/modify: Infrastructure.Tests/Providers/Copilot tests, Engine/UsageStoreTests.cs, Core.Tests/Policies/SnapshotRetentionPolicyTests.cs.
- Reuse: T02 models/policy and T03 archive/gate; change their contracts only with dependent-task reconciliation.

## Observability and recovery

- Signal: source-specific reason/duration, gate transitions, cache retention; no tokens or raw bodies.
- Recovery: revert the opt-in and corresponding gate wiring together. Preserve legacy deadlines and quota files. Disabling billing must not bypass gate enforcement.

## Handoff

- Produced result: Implemented direct Copilot AI-credits billing beside operational quota.
  - CMP-03 `CopilotBillingContextResolver`: verifies Organization ownership via `GET /orgs/{org}/copilot/billing/seats` matching authenticated principal with `plan_type=business` (closing OI-01), corroborated by local session `workspace.yaml` hints. Resolves plan types, leaves unmapped personal (404) and enterprise scopes unknown, and surfaces `AmbiguousScope` when multiple candidates match.
  - CMP-04 direct billing DTOs (`CopilotBillingUsageItem`, `CopilotBillingResponse`, `CopilotSeatResponse`) and `CopilotBillingClient`: versioned GET requests with Bearer auth, `X-GitHub-Api-Version: 2026-03-10`, `Accept: application/vnd.github+json`, User-Agent `TokenHound/1.0`, bounded 15s timeout, and rate-limit gate dispatch.
  - CMP-05 `CopilotBillingService`: 15s/4-dispatch progress bound, parses and filters AI-credits items, evaluates via `CopilotCreditPolicy`, atomically persists to `copilot_billing.json`, retains cached usage on transient failures (429, 403, network, invalid data), and handles persistence failures.
  - Control flow in `CopilotUsageProvider`: implements `IRequestGatedUsageProvider`, gates quota calls through `CopilotRequestGate`, tracks credential generation, preserves quota and billing independence (quota failures do not drop eligible billing, billing failures do not rewrite quota status), and attaches `CopilotBilling` to snapshot.
  - Retention overlay in `SnapshotRetentionPolicy`: preserves incoming `CopilotBilling` across all retention branches (`Ok`, `Stale`, `NeedsAuth`, `Unsupported`), strips billing from generic `last_readings.json` archive snapshots, and prevents resurrecting absent billing. `UsageStore.CreateErrorSnapshot` marks known billing as stale with `NetworkFailure`.
  - Composition in `App.xaml.cs`: shares single `UsageArchive` instance across store, gate, and billing service, with proper cancellation-before-disposal lifecycle tracking.
- Changed files:
  - Created:
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingUsageItem.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingResponse.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotSeatResponse.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingClient.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingContextResolver.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotBillingService.cs`
    - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingClientTests.cs`
    - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingContextResolverTests.cs`
    - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingServiceTests.cs`
  - Modified:
    - `src/TokenHound.Core/Policies/SnapshotRetentionPolicy.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotQuotaResponse.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotUsageProvider.cs`
    - `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs`
    - `src/TokenHound.App/App.xaml.cs`
    - `tests/TokenHound.Core.Tests/Policies/SnapshotRetentionPolicyTests.cs`
    - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotUsageProviderTests.cs`
    - `tests/TokenHound.Infrastructure.Tests/Engine/RequestGatedUsageStoreTests.cs`
    - `tasks/prd-copilot-ai-credits/task_04.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal` -> 0 errors, 0 warnings
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal` -> 0 errors, 0 warnings
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal` -> 0 errors, 0 warnings
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"` -> 82 passed, 0 failed
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageStoreTests*"` -> 17 passed, 0 failed
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RequestGatedUsageStoreTests*"` -> 3 passed, 0 failed
  - `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*SnapshotRetentionPolicyTests*"` -> 10 passed, 0 failed
  - `rtk git diff --check` -> 0 whitespace/formatting errors
- Validated state: Release configuration, .NET 10.0, Microsoft.Testing.Platform runner with xUnit v3, all 112 affected tests passing.
- Open items: OI-01 closed for Organization scope; OI-03 (historical reports) remains open for T05.
- ADR candidates: None - direct TechSpec implementation or local decision.

