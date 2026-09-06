# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-cursor/prd.md`
2. `tasks/prd-provider-cursor/techspec.md`
3. This file

---

# T03 — Cursor Composer Header Reader & State Tracker

## Outcome

Implements `CursorComposerReader` querying the `composerHeaders` table in `state.vscdb` to evaluate Composer agent runs (`unfinishedRunAt`, `hasBlockingPendingActions`, `hasPendingPlan`, `conversationCheckpointLastUpdatedAt`).

## Work

- [x] T03.1 Implement `CursorComposerHeaderDto.cs` under `TokenHound.Infrastructure/Providers/Cursor/`.
- [x] T03.2 Query `composerHeaders` with `isArchived = 0 ORDER BY recency DESC LIMIT 40`.
- [x] T03.3 Parse JSON payloads and identify active or pending approval states.
- [x] T03.4 Implement `CursorComposerReaderTests.cs` using temporary SQLite databases.

## Acceptance criteria

- Successfully deserializes composer run states.
- Flags waiting/approval state when `hasBlockingPendingActions` or `hasPendingPlan` is true.
- Flags execution run when `unfinishedRunAt` is non-null.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorComposerReaderTests*"`

## Handoff

- Produced result: Implemented `CursorComposerHeaderDto` and `CursorComposerReader` using `SafeSqliteReader` to query unarchived `composerHeaders` with JSON deserialization, waiting state detection, and unfinished run tracking.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorComposerHeaderDto.cs`
  - `src/TokenHound.Infrastructure/Providers/Cursor/CursorComposerReader.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/Cursor/CursorComposerReaderTests.cs`
- Checks: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CursorComposerReaderTests*"` (5 tests passed, 0 warnings, exit code 0).
- Validated state: 5 tests passed covering valid parsing, missing DB, missing table, malformed JSON rows, and cancellation.
- Open items: None.

### ADR candidates

None.
