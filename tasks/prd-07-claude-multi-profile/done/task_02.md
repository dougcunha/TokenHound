# Stable execution context

Load in this exact order:

1. `tasks/prd-07-claude-multi-profile/prd.md`
2. `tasks/prd-07-claude-multi-profile/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T02 — Multi-instance provider and session monitor parameterization

## Outcome

Parameterize `ClaudeOAuthProvider` and `ClaudeSessionMonitor` so that multiple instances can be instantiated targeting different `providerId`s and profile directories, enabling isolated polling, rate-limiting, and terminal activity monitoring.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: T03
- In scope: Parameterize `ClaudeOAuthProvider` and `ClaudeSessionMonitor`, update their unit tests.
- Out of scope: Application-level registration and UI changes.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-04 | `prd.md#functional-requirements` | Dedicated provider instances |
| FR-05 | `prd.md#functional-requirements` | Dedicated activity monitors |
| NFR-02 | `prd.md#non-functional-requirements` | Independent rate limits |
| CMP-03 | `techspec.md#components-and-flow` | `ClaudeOAuthProvider.cs` multi-instance |
| CMP-04 | `techspec.md#components-and-flow` | `ClaudeSessionMonitor.cs` multi-instance |
| TC-03 | `techspec.md#test-approach` | Custom providerId snapshot |
| TC-04 | `techspec.md#test-approach` | Custom sessions directory monitor |

## Context to recover on demand

- Applicable skills and rules: `AGENTS.md` (no-refresh invariant, isolated RateLimitPolicy).
- Existing code:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs`
- Provider spec: `docs/specs/03-PROVIDER-CLAUDE-CODE.md` §2, §3, §4

## Work

- [x] T02.1 In `ClaudeOAuthProvider`, add support for custom `providerId` (defaulting to `"claude"`) and explicit `credentialsFilePath` or profile path.
- [x] T02.2 Ensure `ClaudeOAuthProvider.ProviderId` returns the configured identifier and queries the correct credentials file.
- [x] T02.3 In `ClaudeSessionMonitor`, add support for custom `providerId` (defaulting to `"claude"`) and explicit `sessionsDirectory`.
- [x] T02.4 Add/update unit tests in `ClaudeOAuthProviderTests.cs` and `ClaudeSessionMonitorTests.cs` verifying custom provider IDs and isolated paths.

## Acceptance criteria

- `ClaudeOAuthProvider` instantiated with `providerId = "claude-work"` returns `"claude-work"` for `ProviderId` and includes `"claude-work"` on its generated `Snapshot`.
- `ClaudeSessionMonitor` instantiated with `providerId = "claude-work"` returns `"claude-work"` for `ProviderId` and scans the specified `sessionsDirectory`.
- Existing tests without custom parameters continue to work unchanged (full backward compatibility).

## Verification

- Unit: Verify `ClaudeOAuthProviderTests` and `ClaudeSessionMonitorTests` with both default and custom provider IDs.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*ClaudeOAuthProviderTests*"` and `--filter-class "*ClaudeSessionMonitorTests*"`
- Expected evidence: All unit tests pass with zero failures.

## Affected files

- Modify: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs`
- Modify: `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeOAuthProviderTests.cs`
- Modify: `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeSessionMonitorTests.cs`

## Observability and recovery

- Operational signal: Snapshots and AgentSession instances carry the distinct `ProviderId`.
- Recovery: Revert constructor overloads and parameter additions.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Parameterized `ClaudeOAuthProvider` with `providerId` and `credentialsFilePath`, updated snapshot generation to use `_providerId`, parameterized `ClaudeSessionMonitor` with `providerId` and `sessionsDirectory`. Added comprehensive unit tests and verified all 747 tests in `TokenHound.Infrastructure.Tests` pass.
- Changed files:
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeOAuthProvider.cs`
  - `src/TokenHound.Infrastructure/Providers/Claude/ClaudeSessionMonitor.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeOAuthProviderTests.cs`
  - `tests/TokenHound.Infrastructure.Tests/Providers/ClaudeSessionMonitorTests.cs`
- Checks:
  - `rtk dotnet build TokenHound.slnx --no-restore` (0 errors, 0 warnings)
  - `rtk dotnet test ... --filter-class "*ClaudeOAuthProviderTests*"` (14 passed, 0 failed)
  - `rtk dotnet test ... --filter-class "*ClaudeSessionMonitorTests*"` (20 passed, 0 failed)
  - Full Infrastructure test suite: 747 passed, 0 failed.
  - Quality profile verification: zero hits on blocking QA rules across all touched files.
- Validated state: Clean build and all tests passing.
- Reconciled after corrections: Added positive custom-directory live session fixture and verified provider-scoped activity event in `ClaudeSessionMonitorTests` (T06/CR-03). All 68 Claude tests passing.
- Open items: None.

### ADR candidates

None - direct TechSpec implementation.
