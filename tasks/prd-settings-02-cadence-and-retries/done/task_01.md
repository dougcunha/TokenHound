# Stable execution context

Load in this exact order:

1. `tasks/prd-settings-02-cadence-and-retries/prd.md`
2. `tasks/prd-settings-02-cadence-and-retries/techspec.md`
3. This file

---

# T01 - Make the retry floor dynamically safe

## Outcome

Subsequent HTTP 429 deadline calculations use a runtime-configurable retry floor that can never fall below 60 seconds, including for `Retry-After: 0`, while previously recorded deadlines remain untouched.

## Dependencies and boundaries

- Depends on: -
- Unblocks: T04
- In scope: `RateLimitPolicy` effective-floor contract, its thread-safe state transition, and focused Core regression tests.
- Out of scope: Settings persistence/UI, provider dispatch changes, any change that shortens an already-recorded deadline, or changes to the 3600s ceiling.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-02, OBJ-03, OBJ-05 | `prd.md#outcomes-and-metrics` | Configurable retry floor that remains safe and applies live |
| FR-05, FR-10 | `prd.md#functional-requirements` | Floor validation and subsequent policy evaluation without restart |
| NFR-01, NFR-02 | `prd.md#non-functional-requirements` | Core purity and inviolable 60-second floor |
| DEC-05, CMP-04 | `techspec.md#technical-decisions`, `techspec.md#components-and-flow` | Effective-floor policy extension |
| TC-03 | `techspec.md#test-approach` | Clamp low floor and preserve penalty for `Retry-After: 0` |

## Context to recover on demand

- Applicable skills: `no-workarounds`, `dotnet-efficient-validation`, `repository-cli-efficiency`.
- Existing code: `src/TokenHound.Core/Policies/RateLimitPolicy.cs` - current penalty logic owns the safety invariant.
- Contract or integration: `techspec.md#ratelimitpolicy-configurable-floor-extension`; callers persist deadlines outside this policy.

## Work

- [x] T01.1 Add a public effective-floor configuration API whose reads and updates are safe under concurrent evaluation; clamp every supplied value below `MINIMUM_RETRY_FLOOR`.
- [x] T01.2 Route both deadline-calculation overloads and all penalty-floor paths through the effective floor, retaining exponential escalation and the `Retry-After: 0` minimum wait.
- [x] T01.3 Add isolated Core tests for default behavior, raised-floor behavior, sub-floor clamp, and `Retry-After: 0`; reset shared policy state safely between tests.

## Acceptance criteria

- A 120-second effective floor causes subsequent eligible 429 penalties to wait at least 120 seconds.
- A zero, negative, or 10-second requested floor resolves to 60 seconds; retry-after zero never enables immediate dispatch.
- Existing `CanDispatch` and previously persisted deadlines retain their semantics, and Core acquires no UI, OS, file, or logging dependency.

## Verification

- Unit: `RateLimitPolicyConfigTests` proves default, raised, and clamped floors, including retry-after zero.
- Integration: None; the policy is a pure Core boundary.
- E2E: Omitted by .NET desktop policy.
- Manual: MAN-02 later confirms that UI-applied floor changes affect future 429s; owner: implementer.
- Commands: `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RateLimitPolicyConfigTests*"`
- Environment dependency: .NET SDK 10.0.400 and restored Core test output.
- Expected evidence: MTP reports at least one selected test executed and all pass.

## Affected files

- Modify: `src/TokenHound.Core/Policies/RateLimitPolicy.cs`
- Create: `tests/TokenHound.Core.Tests/Policies/RateLimitPolicyConfigTests.cs`

## Observability and recovery

- Operational signal: Runtime composition in T05 logs the applied effective floor once at the application boundary; Core remains logging-free.
- Recovery: Reset the effective floor to `MINIMUM_RETRY_FLOOR`; no deadline data migration is needed.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Implemented thread-safe configurable effective retry floor in `RateLimitPolicy` with hard clamping to `MINIMUM_RETRY_FLOOR` (60s) for all penalty calculations and `Retry-After: 0`. Added comprehensive unit test suite `RateLimitPolicyConfigTests` verifying default floor, raised floor, clamp behavior, monotonic escalation, random overload, deadline preservation, and thread safety.
- Changed files:
  - `src/TokenHound.Core/Policies/RateLimitPolicy.cs`
  - `tests/TokenHound.Core.Tests/Policies/RateLimitPolicyConfigTests.cs`
- Checks:
  - `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1 --filter-class "*RateLimitPolicyConfigTests*"` (12 passed, 0 failed, duration: 372ms, exit code 0)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` (89 passed, 0 warnings, duration: 516ms, exit code 0)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (89 passed, 0 warnings, duration: 566ms, exit code 0)
- Validated state: Clean build and all 89 Core tests passing on .NET SDK 10.0.400 on Windows.
- Open items: None for T01; unblocks T04 (`CadenceSettingsViewModel`). Manual verification MAN-02 remains pending for slice-level integration.

### ADR candidates

None - direct TechSpec implementation of DEC-05 and CMP-04.
