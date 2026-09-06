# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T01 — Claude Profile & Credential Discovery

## Outcome

Implements `ClaudeProfileDiscovery` in `TokenHound.Infrastructure/Providers/Claude/` to locate and parse `.credentials.json` from `%USERPROFILE%\.claude\` and multi-profile directories (`~/.claude-<slug>`) using `SharedFileReader`.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T03
- In scope: Profile discovery, path resolution, JSON token parsing into `ClaudeCredentialDto`, and unit tests.
- Out of scope: Network communication or token refresh.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Discover Claude Code OAuth credentials |
| DEC-01 | `techspec.md#technical-decisions` | ClaudeProfileDiscovery with SharedFileReader |
| CMP-01 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Shared storage: `src/TokenHound.Infrastructure/Storage/SharedFileReader.cs`
- Spec: `docs/specs/03-PROVIDER-CLAUDE-CODE.md`

## Work

- [ ] T01.1 Define `ClaudeCredentialDto.cs` under `TokenHound.Infrastructure/Providers/Claude/`.
- [ ] T01.2 Implement `ClaudeProfileDiscovery.cs` locating credentials across `%USERPROFILE%\.claude\.credentials.json` and multi-profile directories.
- [ ] T01.3 Use `SharedFileReader.ReadAllTextAsync` to safely read credential files without locks.
- [ ] T01.4 Implement `ClaudeProfileDiscoveryTests.cs` testing valid JSON, missing files, and expired token detection.

## Acceptance criteria

- Successfully extracts `accessToken` and `expiresAt` from valid `.credentials.json`.
- Returns `null` gracefully when no profile or credentials file exists.
- Adheres to `AGENTS.md` (sealed classes, file-scoped namespaces, <= 300 lines).

## Verification

- Unit: Test temporary `.credentials.json` parsing and missing directory handling.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeProfileDiscoveryTests*"`
- Expected evidence: MTP test suite passes.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeCredentialDto.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileDiscoveryTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert discovery logic if file paths differ on Windows.

## Handoff

- Produced result: Implemented `ClaudeCredentialDto` record and `ClaudeProfileDiscovery` to discover and parse `.credentials.json` without file locking across default (`%USERPROFILE%\.claude`) and multi-profile (`%USERPROFILE%\.claude-*`) paths, handling missing paths, expired tokens, and malformed JSON gracefully. Added comprehensive unit tests in `ClaudeProfileDiscoveryTests`.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeCredentialDto.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeProfileDiscoveryTests.cs`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (Exit code: 0; 0 errors, 4 warnings)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeProfileDiscoveryTests*"` (Exit code: 0; 14 tests passed, 0 failures, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Exit code: 0; 81 tests passed, 0 failures, 0 skipped)
- Validated state: All T01 acceptance criteria satisfied. Strict AGENTS.md compliance verified (sealed types, file-scoped namespaces, lines <= 300, methods <= 30 lines, nesting <= 3 levels, `.ConfigureAwait(false)` in Infrastructure, cancellation token propagation, non-locking file access via `SharedFileReader.ReadAllTextAsync`).
- Open items: None. Ready for downstream task T03 (`ClaudeOAuthProvider`).

### ADR candidates

None.
