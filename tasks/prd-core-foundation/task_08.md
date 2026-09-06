# Stable execution context

Load in this exact order:

1. `tasks/prd-core-foundation/prd.md`
2. `tasks/prd-core-foundation/techspec.md`
3. This file

---

# T08 — Infrastructure MockUsageProvider & Fixtures

## Outcome

Implements `MockUsageProvider` in `TokenHound.Infrastructure/Providers/Mock/` along with static snapshot fixtures, providing a configurable implementation of `IUsageProvider` that supports offline UI development, state switching, and reliable automated testing.

## Dependencies and boundaries

- Depends on: T01, T02
- Unblocks: PRD 02 (Tracer Bullet HUD presentation)
- In scope: Implementation of `MockUsageProvider`, scenario presets (`Normal`, `Warning`, `RateLimited`, `NeedsAuth`, `Stale`, `Unidirectional`), and unit tests.
- Out of scope: Live network communication.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-11 | `prd.md#functional-requirements` | MockUsageProvider offline test fixtures |
| OBJ-04 | `prd.md#outcomes-and-metrics` | 100% offline testability of HUD states |
| DEC-09 | `techspec.md#technical-decisions` | MockUsageProvider with preset scenarios |
| CMP-08 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Providers/Mock/MockUsageProvider.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Models: `src/TokenHound.Core/Models/Snapshot.cs`
- Contracts: `src/TokenHound.Core/Contracts/IUsageProvider.cs`

## Work

- [ ] T08.1 Implement `MockUsageProvider.cs` under `TokenHound.Infrastructure/Providers/Mock/`.
- [ ] T08.2 Define configurable scenarios: `Normal` (20% session / 15% weekly), `Warning` (85% weekly), `RateLimited` (429 with countdown), `NeedsAuth` (unauthenticated), and `Unidirectional` (null denominator).
- [ ] T08.3 Expose methods to update current state at runtime: `SetScenario(MockScenario scenario)` and `SetCustomSnapshot(Snapshot snapshot)`.
- [ ] T08.4 Implement `MockUsageProviderTests.cs` verifying snapshot generation across all scenarios.

## Acceptance criteria

- Implements `IUsageProvider` faithfully with zero network or external I/O.
- Returns valid snapshots matching the active scenario.
- Supports runtime state switching without allocations on the query path.

## Verification

- Unit: Switch scenarios and verify `GetSnapshotAsync` reflects the new status, limits, and blocks.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*MockUsageProviderTests*"`
- Expected evidence: MTP test suite passes verifying mock provider outputs.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/Providers/Mock/MockUsageProvider.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/MockUsageProviderTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert file if mock output signatures mismatch Core contracts.

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
