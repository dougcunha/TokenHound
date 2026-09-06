# Stable execution context

Load in this exact order:

1. `tasks/prd-core-foundation/prd.md`
2. `tasks/prd-core-foundation/techspec.md`
3. This file

---

# T03 — Core Resilience & Scheduling Policies

## Outcome

Implements stateless domain algorithms in `TokenHound.Core/Policies/` for exponential backoff with jitter (`BackoffCalculator`), rate limit penalty deadlines (`RateLimitPolicy`), and adaptive polling schedule checks (`RefreshSchedulePolicy`).

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope: Pure static or domain policy classes and comprehensive unit tests covering edge cases (zero Retry-After, jitter bounds, idle vs busy timing).
- Out of scope: State persistence on disk (handled by engine archive).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-04 | `prd.md#functional-requirements` | BackoffCalculator exponential & jitter |
| FR-05 | `prd.md#functional-requirements` | RateLimitPolicy deadline floor |
| FR-06 | `prd.md#functional-requirements` | RefreshSchedulePolicy active/idle logic |
| DEC-04 | `techspec.md#technical-decisions` | Pure stateless resilience policies |
| CMP-03 | `techspec.md#components-and-flow` | src/TokenHound.Core/Policies/ |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Existing code: `src/TokenHound.Core/Models/`
- Spec: `docs/specs/01-READING-STRATEGY-RESILIENCE.md`

## Work

- [ ] T03.1 Implement `BackoffCalculator.cs` with exponential backoff, jitter calculation, and 60s minimum floor.
- [ ] T03.2 Implement `RateLimitPolicy.cs` checking deadline expiration and enforcing 60s floor on `Retry-After: 0`.
- [ ] T03.3 Implement `RefreshSchedulePolicy.cs` with pure `ShouldRefresh(isBusy, timeSinceLastAttempt, idleInterval)` rule.
- [ ] T03.4 Implement unit tests in `TokenHound.Core.Tests/Policies/` covering all backoff steps, deadline edge cases, and refresh scenarios.

## Acceptance criteria

- `BackoffCalculator` generates monotonically increasing expected intervals capped at 3600 seconds.
- `RateLimitPolicy.CanDispatch` returns false when `nowUtc < deadlineUtc`.
- `RefreshSchedulePolicy.ShouldRefresh` returns true if `isBusy` is true OR `timeSinceLastAttempt >= idleInterval`.

## Verification

- Unit: Verify backoff progression, jitter variance, deadline calculation, and schedule evaluation.
- Commands: `rtk dotnet test tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: MTP test suite passes with 100% assertions satisfied.

## Affected files

- Create:
  - `src/TokenHound.Core/Policies/BackoffCalculator.cs`
  - `src/TokenHound.Core/Policies/RateLimitPolicy.cs`
  - `src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs`
  - `tests/TokenHound.Core.Tests/Policies/BackoffCalculatorTests.cs`
  - `tests/TokenHound.Core.Tests/Policies/RateLimitPolicyTests.cs`
  - `tests/TokenHound.Core.Tests/Policies/RefreshSchedulePolicyTests.cs`

## Observability and recovery

- Operational signal: Unit test coverage of math and edge conditions.
- Recovery: Revert files if calculation invariants fail.

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
