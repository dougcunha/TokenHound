# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T03 — Claude OAuth Provider Adapter (IUsageProvider)

## Outcome

Implements `ClaudeOAuthProvider` in `TokenHound.Infrastructure/Providers/Claude/` implementing `IUsageProvider`, uniting profile discovery and HTTP telemetry to produce domain `Snapshot` records.

## Dependencies and boundaries

- Depends on: T01, T02
- Unblocks: T05
- In scope: Implementation of `ClaudeOAuthProvider`, mapping telemetry to `LimitWindow` records, and unit tests.
- Out of scope: Session activity monitoring or UI rendering.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-03 | `prd.md#functional-requirements` | Claude usage to domain Snapshot mapping |
| DEC-03 | `techspec.md#technical-decisions` | ClaudeOAuthProvider implementing IUsageProvider |
| CMP-03 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Contracts: `src/TokenHound.Core/Contracts/IUsageProvider.cs`
- Models: `src/TokenHound.Core/Models/Snapshot.cs`

## Work

- [ ] T03.1 Implement `ClaudeOAuthProvider.cs` implementing `IUsageProvider`.
- [ ] T03.2 Connect `ClaudeProfileDiscovery` to retrieve active credentials.
- [ ] T03.3 Query `ClaudeOAuthClient` and map results to `five_hour` and `seven_day` `LimitWindow` records.
- [ ] T03.4 Handle `NeedsAuth` state when credentials file is missing or token is expired.
- [ ] T03.5 Implement `ClaudeOAuthProviderTests.cs` verifying snapshot generation across all states.

## Acceptance criteria

- `ProviderId` returns `"claude"`.
- Produces immutable `Snapshot` with status `Ok`, `NeedsAuth`, or `RateLimited`.
- Respects `CancellationToken` and configures `.ConfigureAwait(false)`.

## Verification

- Unit: Test snapshot creation under authenticated, missing credential, and rate-limited scenarios.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeOAuthProviderTests*"`
- Expected evidence: MTP test suite passes.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeOAuthProviderTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert mapping logic if snapshot model contracts diverge.

## Handoff

- Produced result: Implemented `ClaudeOAuthProvider` implementing `IUsageProvider` under `TokenHound.Infrastructure.Providers.Claude`. Integrates `ClaudeProfileDiscovery` and `ClaudeOAuthClient` to map usage telemetry into domain `Snapshot` records. Maps `five_hour` and `seven_day` windows to `LimitWindow` records, handles missing or expired credentials returning `NeedsAuth` with "Execute 'claude login' in terminal", translates HTTP 429 to `RateLimited` with calculated `ActiveBlock` deadlines via `RateLimitPolicy.CalculateDeadline`, and degrades to `Stale` on network failures. 10 MTP unit tests created in `ClaudeOAuthProviderTests` covering all functional states and cancellation.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeOAuthProviderTests.cs`
  - `tasks/prd-tracer-bullet-claude/task_03.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (Passed: 0 errors, 4 warnings)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeOAuthProviderTests*"` (Passed: 10 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 102 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-restore -- --minimum-expected-tests 1` (Passed: 34 passed, 0 failed, 0 skipped)
- Validated state: .NET 10 (`net10.0`), all acceptance criteria satisfied, zero regressions in existing test suite. Adheres strictly to AGENTS.md (sealed classes, file-scoped namespaces, <= 300 lines per file, <= 30 lines per method, <= 3 nesting levels, `.ConfigureAwait(false)`, cancellation token propagation).
- Open items: None. Ready for downstream integration.

### ADR candidates

None. Implementation conforms to TechSpec DEC-03 and CMP-03 without architectural deviations.
