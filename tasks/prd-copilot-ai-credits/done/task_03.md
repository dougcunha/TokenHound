# Stable execution context

Load in this exact order:

1. `tasks/prd-copilot-ai-credits/prd.md`
2. `tasks/prd-copilot-ai-credits/techspec.md`
3. This file

Use current source versions already loaded; recover only missing or changed sources. The manifest [tasks.md](tasks.md) is authoritative for dependencies and state.

# T03: Persist billing state and enforce every request deadline

## Outcome

Tested Infrastructure primitives retain owner/period-specific billing state and prevent a second HTTP dispatch after a persisted 429. These shared primitives unlock direct billing and report downloads without changing active provider behavior prematurely.

## Dependencies and boundaries

- Depends on: T02.
- Unblocks: T04, T05.
- In scope: opt-in contract, request gate, independent billing archive, atomic file semantics, and focused tests.
- Out of scope: switching Copilot's active store path before all sends are wired (T04), production scope mappings, and report parsing.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-10, FR-11; NFR-01, NFR-02, NFR-03, NFR-04 | PRD requirements | Independent retention, identity isolation, durable gating, cancellation. |
| DEC-05/06/07; CMP-06/07; TC-07/08/09; OI-05 | TechSpec | Shared archive/gate contracts and real-file evidence. |

## Context to recover on demand

- Skills: repository-cli-efficiency, dotnet-efficient-validation.
- Existing code: UsageArchive partials, RateLimitPolicy, BackoffCalculator, and UsageStore.Refresh.
- Integration: TechSpec Gating, persistence, and retention; Errors, security, and recovery.

## Work

- [x] T03.1 Define IRequestGatedUsageProvider with its dispatch-ownership contract; do not opt Copilot in until T04 wires every send.
- [x] T03.2 Add versioned independent billing-cache reads/writes and day-summary storage boundaries to UsageArchive using its shared semaphore and atomic replacement.
- [x] T03.3 Add copilotHttp deadline/streak persistence while preserving all unrelated state.json properties and honoring legacy backoffUntil.copilot.
- [x] T03.4 Implement the shared gate with serialized dispatch, existing backoff policy, cancellation, no immediate retries, and process-lifetime suppression after deadline persistence failure.
- [x] T03.5 Exercise real temporary files, corrupt/old schemas, period rollover, owner changes, failed writes, forced refresh, restart, and 429 at multiple request positions.

## Acceptance criteria

- No HTTP send occurs before the effective deadline, including Retry-After zero/date/missing variants and restart.
- The 429 update is committed before the gate permits another send; persistence failure blocks further process-local sends visibly.
- Billing cache cannot cross principal/owner/period/filter boundaries; corrupt billing state does not erase unrelated quota/deadline data.
- Production store/provider behavior is unchanged until the complete T04 integration.

## Verification

- Unit: fake clock and fake handler prove gate order, failure streaks, cancellation, and deadline monotonicity.
- Integration: TC-07/08/09 with real temporary directories and atomic archive writes; no in-memory filesystem substitute.
- E2E: omitted by .NET desktop policy.
- Manual: inspect archive JSON for absence of credentials/signed URLs and preservation of unrelated keys.
- Commands: Follow the TechSpec validation profile: effective SDK 10.0.400, xUnit v3/MTP executable route, Release, restore only when needed, then build the affected test project with `--no-restore -c Release --nologo --verbosity:minimal`. Preserve each build exit code. Reuse a valid build for unchanged inputs; inspect actual execution counts and failures. Use `rtk proxy` only for hidden failure details. No publish or full solution validation is required.

```powershell
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageArchiveTests*"
$taskExit = $LASTEXITCODE
if ($taskExit -ne 0) { throw "Task validation failed: $taskExit" }
```

- Environment dependency: writable isolated local temporary directory; no GitHub credentials needed.
- Expected evidence: nonzero passing gate/cache tests, explicit persisted-deadline assertions, no unrelated archive mutation.

## Affected files

- Create: `src/TokenHound.Core/Contracts/IRequestGatedUsageProvider.cs`.
- Create: `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRequestGate.cs`; `src/TokenHound.Infrastructure/Engine/UsageArchive.CopilotBilling.cs`.
- Modify: `src/TokenHound.Infrastructure/Engine/UsageArchive.cs` and `UsageArchive.Persistence.cs`.
- Create/modify: Copilot gate/cache tests under Infrastructure.Tests/Providers/Copilot and existing Engine/UsageArchiveTests.cs.

## Observability and recovery

- Signal: structured gate transition/persistence-failure reason, never request secrets.
- Recovery: retain existing quota/deadline files and ignore the separate billing file on rollback; never clear a deadline to restore availability.

## Handoff

- Produced result:
  - Defined `IRequestGatedUsageProvider` marker interface in Core for request-level rate-limit gated providers.
  - Updated `UsageStore.RefreshProviderAsync` to bypass outer `IsRateLimited` and `PersistRateLimitDeadlineAsync` for `IRequestGatedUsageProvider` while preserving standard behavior for non-gated providers.
  - Added versioned independent billing cache in `copilot_billing.json` and `copilotHttp` deadline/failure persistence in `state.json` (`UsageArchive.CopilotBilling.cs`, `UsageArchive.CopilotBillingKey.cs`, `UsageArchive.CopilotHttp.cs`).
  - Guaranteed `last_readings.json` snapshot persistence strips `CopilotBilling` to keep the billing archive authoritative.
  - Implemented `CopilotRequestGate` serializing check/send/429-update using `RateLimitPolicy.CalculateDeadline`, minimum 60s floor, monotonic deadlines (never lowered), durable persistence before gate release, process-lifetime suppression on persistence failure, streak reset on success, and conservative legacy `backoffUntil.copilot` honoring.
  - Added comprehensive test suites: `CopilotRequestGateTests` (10 tests across partials), `CopilotBillingArchiveTests` (5 tests), `RequestGatedUsageStoreTests` (2 tests), and expanded `UsageArchiveTests` (7 tests).
- Changed files:
  - Created:
    - `src/TokenHound.Core/Contracts/IRequestGatedUsageProvider.cs`
    - `src/TokenHound.Infrastructure/Engine/CopilotHttpGateState.cs`
    - `src/TokenHound.Infrastructure/Engine/CopilotDailyUsageSummary.cs`
    - `src/TokenHound.Infrastructure/Engine/UsageArchive.CopilotHttp.cs`
    - `src/TokenHound.Infrastructure/Engine/UsageArchive.CopilotBilling.cs`
    - `src/TokenHound.Infrastructure/Engine/UsageArchive.CopilotBillingKey.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRateLimitExtractor.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/CopilotRequestGate.cs`
    - `src/TokenHound.Infrastructure/Providers/Copilot/RateLimitBlockedException.cs`
    - `tests/TokenHound.Infrastructure.Tests/Engine/RequestGatedUsageStoreTests.cs`
    - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotBillingArchiveTests.cs`
    - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotRequestGateTests.cs`
    - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/CopilotRequestGateTests.Lifecycle.cs`
    - `tests/TokenHound.Infrastructure.Tests/Providers/Copilot/MutableTimeProvider.cs`
  - Modified:
    - `src/TokenHound.Infrastructure/Engine/UsageStore.Refresh.cs`
    - `src/TokenHound.Infrastructure/Engine/UsageArchive.cs`
    - `tests/TokenHound.Infrastructure.Tests/Engine/UsageArchiveTests.cs`
    - `tasks/prd-copilot-ai-credits/task_03.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore -c Release --nologo --verbosity:minimal`: 3 projects, 0 errors, 0 warnings.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*Copilot*"`: 52/52 tests passed.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*UsageArchiveTests*"`: 7/7 tests passed.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RequestGatedUsageStoreTests*"`: 2/2 tests passed.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1`: 376/376 tests passed.
  - `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1`: 72/72 tests passed.
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj --no-restore -c Release --nologo --verbosity:minimal`: 4 projects, 0 errors, 0 warnings.
  - `rtk git diff --check`: 0 issues.
  - Code hygiene: all files <= 300 lines, all methods <= 30 lines, nesting <= 3 levels.
- Validated state: Release configuration, net10.0 runtime, git diff verified without warnings.
- Open items: None for T03. Unblocks T04 (direct billing integration) and T05 (historical reports fallback).

### ADR candidates

None - direct TechSpec implementation or local decision.


