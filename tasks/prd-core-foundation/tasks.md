# Implementation plan — Core Domain Models, Contracts, and Infrastructure Primitives

## Stable sources

- PRD: [tasks/prd-core-foundation/prd.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-core-foundation/prd.md)
- TechSpec: [tasks/prd-core-foundation/techspec.md](file:///D:/MyProjects/Ideas/TokenHound/tasks/prd-core-foundation/techspec.md)

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Core Domain Models & Enums | — | T02, T03, T07, T08 |
| T02 | Core Domain Contracts | T01 | T06, T08 |
| T03 | Core Resilience & Scheduling Policies | T01 | — |
| T04 | Infrastructure SharedFileReader | — | PRD 02 |
| T05 | Infrastructure SafeSqliteReader & Package Setup | — | PRD 05 |
| T06 | Infrastructure WindowsCredentialManager | T02 | PRD 06 |
| T07 | Infrastructure ProcessLiveness (PID & StartTimeUtc) | T01 | PRD 02 |
| T08 | Infrastructure MockUsageProvider & Test Fixtures | T01, T02 | PRD 02 |

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence or test |
| --- | --- | --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Immutable domain models & enums | T01 | `TC-01` (`DomainModelsTests.cs`) |
| FR-02 | `prd.md#functional-requirements` | Zero Fake Data policy on LimitWindow | T01 | `TC-01` (`LimitWindowTests.cs`) |
| FR-03 | `prd.md#functional-requirements` | System contracts (IUsageProvider, etc.) | T02 | Compiler type-check & contracts verification |
| FR-04 | `prd.md#functional-requirements` | BackoffCalculator exponential & jitter | T03 | `TC-02` (`BackoffCalculatorTests.cs`) |
| FR-05 | `prd.md#functional-requirements` | RateLimitPolicy deadline floor | T03 | `TC-03` (`RateLimitPolicyTests.cs`) |
| FR-06 | `prd.md#functional-requirements` | RefreshSchedulePolicy active/idle logic | T03 | `TC-04` (`RefreshSchedulePolicyTests.cs`) |
| FR-07 | `prd.md#functional-requirements` | SafeSqliteReader read-only WAL access | T05 | `TC-06` (`SafeSqliteReaderTests.cs`) |
| FR-08 | `prd.md#functional-requirements` | SharedFileReader non-locking file access | T04 | `TC-05` (`SharedFileReaderTests.cs`) |
| FR-09 | `prd.md#functional-requirements` | WindowsCredentialManager advapi32 P/Invoke | T06 | `WindowsCredentialManagerTests.cs` |
| FR-10 | `prd.md#functional-requirements` | ProcessLiveness PID + start time recycling | T07 | `TC-07` (`ProcessLivenessTests.cs`) |
| FR-11 | `prd.md#functional-requirements` | MockUsageProvider offline test fixtures | T08 | `TC-08` (`MockUsageProviderTests.cs`) |

## Tasks

- [T01 — Core Domain Models & Enums](done/task_01.md): Defines pure C# records and enums with immutability and Zero Fake Data enforcement.
- [T02 — Core Domain Contracts](done/task_02.md): Defines standard asynchronous interfaces for providers, activity monitors, and credentials.
- [T03 — Core Resilience & Scheduling Policies](done/task_03.md): Implements stateless algorithms for exponential backoff, rate limit floors, and refresh scheduling.
- [T04 — Infrastructure SharedFileReader](done/task_04.md): Implements non-locking file reading with FileShare.ReadWrite \| FileShare.Delete.
- [T05 — Infrastructure SafeSqliteReader & Dependencies](done/task_05.md): Adds Microsoft.Data.Sqlite and implements concurrent read-only WAL reader with immutable fallback.
- [T06 — Infrastructure WindowsCredentialManager](done/task_06.md): Implements non-prompting advapi32.dll CredReadW / CredFree P/Invoke wrapper.
- [T07 — Infrastructure ProcessLiveness](done/task_07.md): Implements PID existence and StartTimeUtc verification to prevent false positives on recycled PIDs.
- [T08 — Infrastructure MockUsageProvider & Fixtures](done/task_08.md): Implements configurable offline test provider with realistic snapshot fixtures.

## Coverage gate

- Coverage: Pass — All functional requirements (FR-01 to FR-11) mapped to specific tasks.
- Traceability: Pass — Direct mapping from PRD obligations and TechSpec decisions to T01–T08.
- Dependencies: Pass — Acyclic DAG; Core domain contracts precede infrastructure implementations.
- Atomicity: Pass — Each task consists of 1 production component + 1 test class (<= 150-200 lines).
- Executability: Pass — All tasks use standard MTP test commands (`rtk dotnet test`).
- Validation profile: Pass — E2E omitted by .NET desktop policy; verified with MTP unit and integration tests.
- Idempotency: Pass — Clean creation of independent files without shared mutable state.

## Assumptions and open items

- Assumption: Host running tests has .NET 10 SDK installed.
- Required environment: Windows 11 development machine.
- Open items: None.

## State

- [x] T01 — Core Domain Models & Enums
- [x] T02 — Core Domain Contracts
- [x] T03 — Core Resilience & Scheduling Policies
- [x] T04 — Infrastructure SharedFileReader
- [x] T05 — Infrastructure SafeSqliteReader & Dependencies
- [x] T06 — Infrastructure WindowsCredentialManager
- [x] T07 — Infrastructure ProcessLiveness
- [x] T08 — Infrastructure MockUsageProvider & Fixtures

## Problems and solutions

- None.
