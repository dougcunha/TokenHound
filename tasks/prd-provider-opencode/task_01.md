# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/prd.md`
2. `tasks/prd-provider-opencode/techspec.md`
3. This file

---

# T01 — Credential discovery and auth DTO

## Outcome

Extracts the OpenCode Go API key from `%USERPROFILE%\.local\share\opencode\auth.json` (or environment variables) in read-only mode (`FileShare.ReadWrite | FileShare.Delete`) and packages it into an immutable `OpenCodeAuthDto`.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02, T03
- In scope:
  - `OpenCodeAuthDto` record.
  - `OpenCodeCredentialDiscovery` class with configurable file path for testing.
  - Precedence: `OPENCODE_GO_API_KEY`, `OPENCODE_API_KEY`, `auth.json` (`opencode-go`, then `opencode`).
  - Read-only file sharing without file locking or mutations.
- Out of scope:
  - Making network requests (handled in T02).
  - Modifying or refreshing `auth.json`.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01 | `prd.md#functional-requirements` | Borrow API key from `auth.json` or env variables |
| FR-02 | `prd.md#functional-requirements` | Read-only file sharing (`FileShare.ReadWrite \| FileShare.Delete`) |
| NFR-03 | `prd.md#non-functional-requirements` | Borrow-Don't-Own credential invariant |
| CMP-01 | `techspec.md#components-and-flow` | `OpenCodeAuthDto` |
| CMP-03 | `techspec.md#components-and-flow` | `OpenCodeCredentialDiscovery` |
| TC-01 | `techspec.md#test-approach` | Credential discovery unit tests |

## Context to recover on demand

- Existing reference: [ClaudeProfileDiscovery.cs](file:///D:/MyProjects/TokenHound/src/TokenHound.Infrastructure/Providers/Claude/ClaudeProfileDiscovery.cs)
- Provider Spec: [12-PROVIDER-OPENCODE.md](file:///D:/MyProjects/TokenHound/docs/specs/12-PROVIDER-OPENCODE.md)

## Work

- [ ] T01.1 Create `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeAuthDto.cs`.
- [ ] T01.2 Create `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeCredentialDiscovery.cs` implementing path resolution and JSON extraction.
- [ ] T01.3 Create unit tests in `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeCredentialDiscoveryTests.cs` verifying environment variable overrides, valid `auth.json` extraction, missing keys, and absent files.

## Acceptance criteria

- Successfully parses `"opencode-go": { "type": "api", "key": "zen_..." }` from `auth.json`.
- Falls back to `"opencode": { ... }` if `"opencode-go"` is not present.
- Respects `OPENCODE_GO_API_KEY` and `OPENCODE_API_KEY` when defined.
- Returns `null` when neither environment variables nor valid keys in `auth.json` exist.
- Never writes to or locks `auth.json`.

## Verification

- Unit: Test suite `OpenCodeCredentialDiscoveryTests` passes with 100% assertions.
- Integration: Validated against simulated `auth.json` on disk with concurrent read-write handles.
- E2E: Omitted by desktop .NET policy.
- Commands: `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --filter-class "*OpenCodeCredentialDiscoveryTests*" --minimum-expected-tests 1`
- Environment dependency: None.

## Affected files

- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeAuthDto.cs`
- Create: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeCredentialDiscovery.cs`
- Create: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeCredentialDiscoveryTests.cs`

## Observability and recovery

- Operational signal: Structured debug log when discovering credentials (`"Discovered OpenCode credential from {Source}"`).
- Recovery: None required; read-only operations.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
