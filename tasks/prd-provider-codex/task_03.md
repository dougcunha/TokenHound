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

- [x] T03.1 Implement `CodexRolloutLogReader.cs` under `TokenHound.Infrastructure/Providers/Codex/`.
- [x] T03.2 Query `state_5.sqlite` for active threads using `SafeSqliteReader`: `SELECT rollout_path FROM threads WHERE archived = 0 ORDER BY updated_at_ms DESC LIMIT 8;`.
- [x] T03.3 Implement tail reading seeking to `Math.Max(0, Length - 262144)` with `FileShare.ReadWrite | FileShare.Delete`.
- [x] T03.4 Scan lines backwards to find the last `event_msg` with payload type `token_count` containing `rate_limits`.
- [x] T03.5 Implement `CodexRolloutLogReaderTests.cs` using temporary SQLite databases and JSONL files.

## Acceptance criteria

- Successfully parses rate limits from tail of simulated rollout JSONL file.
- Reads files without locking concurrent writes (`FileShare.ReadWrite | FileShare.Delete`).
- Bounded memory usage: reads at most 256 KB from disk.
- Gracefully handles missing database, missing rollout files, or files with no rate limits.
- Tests pass with `rtk dotnet test`.

## Verification

- Command: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexRolloutLogReaderTests*"`

## Handoff

- Produced result: Implemented read-only active-rollout indexing and bounded 256 KB JSONL tail parsing for Codex rate limits.
- Changed files: `src/TokenHound.Infrastructure/Providers/Codex/CodexRolloutLogReader.cs`; `tests/TokenHound.Infrastructure.Tests/Providers/Codex/CodexRolloutLogReaderTests.cs`.
- Checks: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-restore --nologo --verbosity:minimal`; `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*CodexRolloutLogReaderTests*"`.
- Validated state: Build passed with 0 errors; focused MTP test run passed 4 tests with 0 test warnings. Build reported the pre-existing NU1903 SQLitePCLRaw vulnerability warning.
- Open items: None for T03. T04-T05 remain pending in the provider plan.

### ADR candidates

None.
