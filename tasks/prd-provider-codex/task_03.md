# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-codex/prd.md`
2. `tasks/prd-provider-codex/techspec.md`
3. This file

---

# T03 — Codex Rollout Log & SQLite Tail Reader

## Outcome

Implements `CodexRolloutLogReader` to query active thread rollout paths from `%USERPROFILE%\.codex\state_5.sqlite` and efficiently tail the final 256 KB of `rollout-*.jsonl` files to extract the latest rate limits without memory exhaustion.

## Work

- [ ] T03.1 Implement `CodexRolloutLogReader.cs` under `TokenHound.Infrastructure/Providers/Codex/`.
- [ ] T03.2 Query `state_5.sqlite` for active threads using `SafeSqliteReader`: `SELECT rollout_path FROM threads WHERE archived = 0 ORDER BY updated_at_ms DESC LIMIT 8;`.
- [ ] T03.3 Implement tail reading seeking to `Math.Max(0, Length - 262144)` with `FileShare.ReadWrite | FileShare.Delete`.
- [ ] T03.4 Scan lines backwards to find the last `event_msg` with payload type `token_count` containing `rate_limits`.
- [ ] T03.5 Implement `CodexRolloutLogReaderTests.cs` using temporary SQLite databases and JSONL files.

## Acceptance criteria

- Successfully parses rate limits from tail of simulated rollout JSONL file.
- Reads files without locking concurrent writes (`FileShare.ReadWrite | FileShare.Delete`).
- Bounded memory usage: reads at most 256 KB from disk.
- Gracefully handles missing database, missing rollout files, or files with no rate limits.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexRolloutLogReaderTests*"`

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

None.
