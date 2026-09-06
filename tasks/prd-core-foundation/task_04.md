# Stable execution context

Load in this exact order:

1. `tasks/prd-core-foundation/prd.md`
2. `tasks/prd-core-foundation/techspec.md`
3. This file

---

# T04 — Infrastructure SharedFileReader

## Outcome

Implements `SharedFileReader` in `TokenHound.Infrastructure/Storage/` ensuring non-blocking file access with `FileShare.ReadWrite | FileShare.Delete`, allowing safe reads of external developer files while parent processes write or rotate them.

## Dependencies and boundaries

- Depends on: —
- Unblocks: PRD 02 (Claude Code provider)
- In scope: Implementation of `SharedFileReader` (asynchronous text and stream reading) and integration tests with concurrent file locking.
- Out of scope: Specific JSON deserialization of third-party formats.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-08 | `prd.md#functional-requirements` | SharedFileReader non-locking file access |
| DEC-06 | `techspec.md#technical-decisions` | FileShare.ReadWrite \| FileShare.Delete |
| CMP-04 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Storage/SharedFileReader.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Spec: `docs/specs/02-WINDOWS-CREDENTIALS-SECURITY.md`

## Work

- [ ] T04.1 Create `SharedFileReader.cs` under `TokenHound.Infrastructure/Storage/` exposing `ReadAllTextAsync` and `OpenReadAsync`.
- [ ] T04.2 Implement stream handling using `FileShare.ReadWrite | FileShare.Delete` and non-blocking buffers.
- [ ] T04.3 Create `SharedFileReaderTests.cs` in `TokenHound.Infrastructure.Tests` testing concurrent read during active external stream writing.

## Acceptance criteria

- `SharedFileReader` successfully reads files currently held open by another process with write access.
- Propagates `CancellationToken` and configures `.ConfigureAwait(false)`.
- Handles non-existent files gracefully without unhandled crashes.

## Verification

- Integration: Test file opened with `FileShare.ReadWrite` in writer thread is simultaneously read by `SharedFileReader`.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*SharedFileReaderTests*"`
- Expected evidence: Integration test passes verifying concurrent read without `IOException`.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/Storage/SharedFileReader.cs`
  - `tests/TokenHound.Infrastructure.Tests/Storage/SharedFileReaderTests.cs`

## Observability and recovery

- Operational signal: Integration test execution.
- Recovery: Revert file changes if file locking behavior diverges.

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
