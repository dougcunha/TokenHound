# Stable execution context

Load in this exact order:

1. `tasks/prd-core-foundation/prd.md`
2. `tasks/prd-core-foundation/techspec.md`
3. This file

---

# T01 — Core Domain Models & Enums

## Outcome

Delivers the immutable domain models and enums in `TokenHound.Core/Models/`, enforcing thread-safety, Zero Fake Data policy, and compliance with `AGENTS.md` rules.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02, T03, T07, T08
- In scope: C# records and enums (`Snapshot`, `LimitWindow`, `UsageBlock`, `AgentSession`, `ProviderStatus`, `Fidelity`, `AgentSessionState`) and corresponding unit tests.
- Out of scope: System contracts or storage code.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Immutable domain models & enums |
| FR-02 | `prd.md#functional-requirements` | Zero Fake Data policy on LimitWindow |
| DEC-01 | `techspec.md#technical-decisions` | Sealed records with init/required |
| DEC-02 | `techspec.md#technical-decisions` | UsedFraction = null when denominator is missing |
| CMP-01 | `techspec.md#components-and-flow` | src/TokenHound.Core/Models/ |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Existing code: `src/TokenHound.Core/TokenHound.Core.csproj`
- Contract: `techspec.md#contracts-and-data`

## Work

- [ ] T01.1 Create `ProviderStatus.cs`, `Fidelity.cs`, and `AgentSessionState.cs` enums under `TokenHound.Core/Models/`.
- [ ] T01.2 Create `LimitWindow.cs` sealed record enforcing nullable `UsedFraction` when `TotalUnits` is null.
- [ ] T01.3 Create `UsageBlock.cs`, `AgentSession.cs`, and `Snapshot.cs` sealed records.
- [ ] T01.4 Create `DomainModelsTests.cs` in `TokenHound.Core.Tests` verifying immutability, properties, and Zero Fake Data invariant.

## Acceptance criteria

- All model classes are sealed records with file-scoped namespaces.
- `LimitWindow` accepts null `UsedFraction` when no total quota exists.
- Unit tests verify serialization and model integrity without errors.

## Verification

- Unit: Verify `LimitWindow` preserves `null` for `UsedFraction` and snapshot immutability.
- Integration: None (pure models).
- E2E: Omitted by .NET desktop policy.
- Commands: `rtk dotnet test tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: None.
- Expected evidence: MTP test report with all domain model tests passing.

## Affected files

- Create:
  - `src/TokenHound.Core/Models/ProviderStatus.cs`
  - `src/TokenHound.Core/Models/Fidelity.cs`
  - `src/TokenHound.Core/Models/AgentSessionState.cs`
  - `src/TokenHound.Core/Models/LimitWindow.cs`
  - `src/TokenHound.Core/Models/UsageBlock.cs`
  - `src/TokenHound.Core/Models/AgentSession.cs`
  - `src/TokenHound.Core/Models/Snapshot.cs`
  - `tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs`

## Observability and recovery

- Operational signal: Compiler verification and unit test execution.
- Recovery: Revert created files if contracts fail validation.

## Handoff

- Produced result: Implemented core domain models and enums (`ProviderStatus`, `Fidelity`, `AgentSessionState`, `LimitWindow`, `UsageBlock`, `AgentSession`, `Snapshot`) conforming to `AGENTS.md` standards (sealed records, file-scoped namespaces, XML documentation, zero UI/OS dependencies). Enforced the Zero Fake Data policy on `LimitWindow` where `UsedFraction` evaluates to `null` when `TotalUnits` is null or not provided. Added unit tests in `DomainModelsTests.cs` verifying immutability, `with` expressions, Zero Fake Data preservation, and JSON round-trip serialization.
- Changed files:
  - `src/TokenHound.Core/Models/ProviderStatus.cs`
  - `src/TokenHound.Core/Models/Fidelity.cs`
  - `src/TokenHound.Core/Models/AgentSessionState.cs`
  - `src/TokenHound.Core/Models/LimitWindow.cs`
  - `src/TokenHound.Core/Models/UsageBlock.cs`
  - `src/TokenHound.Core/Models/AgentSession.cs`
  - `src/TokenHound.Core/Models/Snapshot.cs`
  - `tests/TokenHound.Core.Tests/Models/DomainModelsTests.cs`
  - `tasks/prd-core-foundation/task_01.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj`: Exit code 0, 2 projects built, 0 errors, 0 warnings.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`: Exit code 0, 9 tests passed (8 domain model tests + 1 smoke test).
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --filter-class "*DomainModelsTests*" --minimum-expected-tests 1`: Exit code 0, 8 tests passed.
- Validated state: All acceptance criteria met. Pure BCL `net10.0` models compiled and verified.
- Open items: None.

### ADR candidates

None.
