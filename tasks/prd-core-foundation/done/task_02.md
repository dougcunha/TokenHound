# Stable execution context

Load in this exact order:

1. `tasks/prd-core-foundation/prd.md`
2. `tasks/prd-core-foundation/techspec.md`
3. This file

---

# T02 — Core Domain Contracts

## Outcome

Defines the standard asynchronous interfaces in `TokenHound.Core/Contracts/` (`IUsageProvider`, `IActivityMonitor`, `ICredentialStore`) that decouple provider implementations from the orchestration engine.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: T06, T08
- In scope: System interfaces accepting `CancellationToken` and returning `ValueTask<T>` and contract smoke tests.
- Out of scope: Implementation of storage or provider adapters.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-03 | `prd.md#functional-requirements` | System contracts |
| NFR-04 | `prd.md#non-functional-requirements` | Async & thread safety |
| DEC-03 | `techspec.md#technical-decisions` | Standard asynchronous interfaces |
| CMP-02 | `techspec.md#components-and-flow` | src/TokenHound.Core/Contracts/ |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Existing code: `src/TokenHound.Core/Models/Snapshot.cs`
- Contract: `techspec.md#contracts-and-data`

## Work

- [ ] T02.1 Create `IUsageProvider.cs` under `TokenHound.Core/Contracts/`.
- [ ] T02.2 Create `IActivityMonitor.cs` under `TokenHound.Core/Contracts/`.
- [ ] T02.3 Create `ICredentialStore.cs` under `TokenHound.Core/Contracts/`.
- [ ] T02.4 Create `ContractsSmokeTests.cs` verifying interface type signatures and XML documentation.

## Acceptance criteria

- Interfaces define `ProviderId` property and async methods accepting `CancellationToken`.
- Methods return `ValueTask<Snapshot>`, `ValueTask<AgentSession?>`, and `ValueTask<string?>`.
- Compiles with 0 warnings in `TokenHound.Core`.

## Verification

- Unit: Verify interfaces compile and can be substituted via NSubstitute in test project.
- Integration: None.
- E2E: Omitted by .NET desktop policy.
- Commands: `rtk dotnet test tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`
- Environment dependency: None.
- Expected evidence: MTP test execution confirming contract substitution.

## Affected files

- Create:
  - `src/TokenHound.Core/Contracts/IUsageProvider.cs`
  - `src/TokenHound.Core/Contracts/IActivityMonitor.cs`
  - `src/TokenHound.Core/Contracts/ICredentialStore.cs`
  - `tests/TokenHound.Core.Tests/Contracts/ContractsSmokeTests.cs`

## Observability and recovery

- Operational signal: Compiler verification.
- Recovery: Revert files if interface definitions require adjustment.

## Handoff

- Produced result: Defined standard asynchronous domain interfaces in `TokenHound.Core/Contracts/` (`IUsageProvider`, `IActivityMonitor`, `ICredentialStore`) returning `ValueTask<T>` and accepting `CancellationToken`, fully compliant with `AGENTS.md` (XML doc comments, file-scoped namespaces, alphabetized usings, zero OS/UI dependencies). Implemented comprehensive smoke tests in `ContractsSmokeTests.cs` validating mock substitution via NSubstitute and interface reflection signatures.
- Changed files:
  - `src/TokenHound.Core/Contracts/IUsageProvider.cs`
  - `src/TokenHound.Core/Contracts/IActivityMonitor.cs`
  - `src/TokenHound.Core/Contracts/ICredentialStore.cs`
  - `tests/TokenHound.Core.Tests/Contracts/ContractsSmokeTests.cs`
  - `tasks/prd-core-foundation/task_02.md`
- Checks:
  - `rtk dotnet build src/TokenHound.Core/TokenHound.Core.csproj`: Exit code 0, 1 project built, 0 errors, 0 warnings.
  - `rtk dotnet build tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj`: Exit code 0, 2 projects built, 0 errors, 0 warnings.
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1`: Exit code 0, 15 tests passed (6 contracts smoke tests + 8 domain model tests + 1 core smoke test).
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --filter-class "*ContractsSmokeTests*" --minimum-expected-tests 1`: Exit code 0, 6 tests passed.
- Validated state: All acceptance criteria met. Interfaces define `ProviderId` properties and asynchronous methods returning `ValueTask<Snapshot>`, `ValueTask<AgentSession?>`, and `ValueTask<string?>`. Zero compiler warnings.
- Open items: None.

### ADR candidates

None.
