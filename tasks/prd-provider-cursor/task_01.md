# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-cursor/prd.md`
2. `tasks/prd-provider-cursor/techspec.md`
3. This file

---

# T01 — Cursor SQLite Session & Credential Discovery

## Outcome

Implements `CursorSessionDiscovery` to query `%APPDATA%\Cursor\User\globalStorage\state.vscdb` using `SafeSqliteReader`, extracting `cursorAuth/accessToken`, `cursorAuth/stripeMembershipAuthId`, `cursorAuth/cachedEmail`, and `cursorAuth/stripeMembershipType`.

## Work

- [x] T01.1 Implement `CursorAuthDto.cs` under `TokenHound.Infrastructure/Providers/Cursor/`.
- [x] T01.2 Implement `CursorSessionDiscovery.cs` with injectable database path for testing.
- [x] T01.3 Query `ItemTable` for the four required auth keys.
- [x] T01.4 Implement `CursorSessionDiscoveryTests.cs` verifying happy path extraction and missing/corrupted database safety.

## Acceptance criteria

- Extracts access token, membership ID, email, and plan type from valid database.
- Returns null safely when database is missing or keys are not found.
- Reads without locking using read-only connection.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorSessionDiscoveryTests*"`

## Handoff

- Produced result: `CursorAuthDto` and `CursorSessionDiscovery` querying SQLite `ItemTable` for auth keys without locking.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorAuthDto.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorSessionDiscovery.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorSessionDiscoveryTests.cs`
- Checks: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorSessionDiscoveryTests*"` (4 tests passed, exit code 0).
- Validated state: Validated auth extraction, missing database handling, missing token handling, and cancellation token propagation.
- Open items: None.

### ADR candidates

None.
