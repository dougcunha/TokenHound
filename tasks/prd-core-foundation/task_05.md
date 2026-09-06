# Stable execution context

Load in this exact order:

1. `tasks/prd-core-foundation/prd.md`
2. `tasks/prd-core-foundation/techspec.md`
3. This file

---

# T05 — Infrastructure SafeSqliteReader & Dependencies

## Outcome

Adds `Microsoft.Data.Sqlite` to `TokenHound.Infrastructure.csproj` and implements `SafeSqliteReader` in `TokenHound.Infrastructure/Storage/` to support concurrent, non-locking reads of SQLite Write-Ahead Log (WAL) databases with automatic fallback to `immutable=1`.

## Dependencies and boundaries

- Depends on: —
- Unblocks: PRD 05 (Cursor provider)
- In scope: NuGet package reference, `SafeSqliteReader` implementation, and concurrent read integration tests against WAL SQLite databases.
- Out of scope: Cursor-specific table schemas or data models.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-07 | `prd.md#functional-requirements` | SafeSqliteReader read-only WAL access |
| OBJ-02 | `prd.md#outcomes-and-metrics` | Non-locking concurrent storage access |
| DEC-05 | `techspec.md#technical-decisions` | Microsoft.Data.Sqlite with Mode=ReadOnly |
| CMP-05 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Storage/SafeSqliteReader.cs |
| CMP-09 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/TokenHound.Infrastructure.csproj |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Spec: `docs/specs/02-WINDOWS-CREDENTIALS-SECURITY.md`

## Work

- [ ] T05.1 Add `<PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.0" />` to `TokenHound.Infrastructure.csproj`.
- [ ] T05.2 Implement `SafeSqliteReader.cs` with connection string builder (`Mode=ReadOnly`, `Cache=Shared`, busy timeout).
- [ ] T05.3 Implement fallback mechanism to `immutable=1` when `-shm` sidecar file is missing or on locked access.
- [ ] T05.4 Implement `SafeSqliteReaderTests.cs` in `TokenHound.Infrastructure.Tests` executing queries on a temporary WAL database while an active transaction is open.

## Acceptance criteria

- `SafeSqliteReader` opens connections strictly in read-only mode without creating locks.
- Falls back to `immutable=1` if `-shm` or `-wal` sidecars cannot be accessed.
- Executes query and returns scalar or mapped results asynchronously with cancellation token support.

## Verification

- Integration: Test WAL SQLite database with concurrent writer can be queried concurrently without `SqliteException` (error code 5).
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*SafeSqliteReaderTests*"`
- Expected evidence: Integration test passes verifying safe concurrent reading.

## Affected files

- Modify: `src/TokenHound.Infrastructure/TokenHound.Infrastructure.csproj`
- Create:
  - `src/TokenHound.Infrastructure/Storage/SafeSqliteReader.cs`
  - `tests/TokenHound.Infrastructure.Tests/Storage/SafeSqliteReaderTests.cs`

## Observability and recovery

- Operational signal: Integration test execution.
- Recovery: Revert connection string logic if connection fails on Windows.

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
