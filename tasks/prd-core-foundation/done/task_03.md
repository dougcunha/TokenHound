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

- Produced result: Implemented pure, stateless domain policies in `TokenHound.Core/Policies/` (`BackoffCalculator`, `RateLimitPolicy`, `RefreshSchedulePolicy`) complying strictly with `AGENTS.md` (sealed static classes, file-scoped namespaces, alphabetized usings, XML documentation on all public members, methods <= 30 lines, nesting <= 3 levels, zero UI/OS dependencies). `BackoffCalculator` implements exponential backoff with full jitter, a 60s minimum floor, a 3600s maximum ceiling, and monotonically increasing expected intervals. `RateLimitPolicy` provides `CanDispatch` (blocking calls prior to deadline expiration) and `CalculateDeadline` (enforcing a 60-second floor on `Retry-After: 0` or values under 60 seconds, while delegating to `BackoffCalculator` when `retryAfterSeconds` is null). `RefreshSchedulePolicy` evaluates `ShouldRefresh` using `isBusy || timeSinceLastAttempt >= idleInterval` alongside standard interval defaults (60s active, 300s idle, 900s stale). Implemented comprehensive unit test suites in `TokenHound.Core.Tests/Policies/` (`BackoffCalculatorTests`, `RateLimitPolicyTests`, `RefreshSchedulePolicyTests`) validating edge cases, 0, 1, and 5 consecutive failures, ceiling caps, jitter variance, and idle/busy cadences.
- Changed files:
  - `src/TokenHound.Core/Policies/BackoffCalculator.cs`
  - `src/TokenHound.Core/Policies/RateLimitPolicy.cs`
  - `src/TokenHound.Core/Policies/RefreshSchedulePolicy.cs`
  - `tests/TokenHound.Core.Tests/Policies/BackoffCalculatorTests.cs`
  - `tests/TokenHound.Core.Tests/Policies/RateLimitPolicyTests.cs`
  - `tests/TokenHound.Core.Tests/Policies/RefreshSchedulePolicyTests.cs`
  - `tasks/prd-core-foundation/task_03.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj`: Exit code 0, 0 errors, 0 warnings.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`: Exit code 0, 34 tests passed, 0 warnings.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --filter-class "*BackoffCalculatorTests*" --minimum-expected-tests 1`: Exit code 0, 6 tests passed.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --filter-class "*RateLimitPolicyTests*" --minimum-expected-tests 1`: Exit code 0, 8 tests passed.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --filter-class "*RefreshSchedulePolicyTests*" --minimum-expected-tests 1`: Exit code 0, 5 tests passed.
- Validated state: All acceptance criteria satisfied. 19 new unit tests passing under MTP (total 34 in test project), zero build warnings, zero runtime failures.
- Open items: None.

### ADR candidates

None. Pure stateless policies implement existing design specifications from PRD FR-04..06 and TechSpec DEC-04 without architectural divergence.
