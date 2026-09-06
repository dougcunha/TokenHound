# Stable execution context

Load in this exact order:

1. `tasks/prd-core-foundation/prd.md`
2. `tasks/prd-core-foundation/techspec.md`
3. This file

---

# T06 — Infrastructure WindowsCredentialManager

## Outcome

Implements `WindowsCredentialManager` in `TokenHound.Infrastructure/Security/` wrapping Win32 `advapi32.dll` (`CredReadW`, `CredFree`) via source-generated P/Invoke, implementing `ICredentialStore` without UI prompts.

## Dependencies and boundaries

- Depends on: T02
- Unblocks: PRD 06 (Antigravity provider)
- In scope: Implementation of `WindowsCredentialManager`, safe memory handling (`CredFree`), and unit tests for credential lookup.
- Out of scope: Interactive credential creation or DPAPI storage fallback.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-09 | `prd.md#functional-requirements` | WindowsCredentialManager advapi32 P/Invoke |
| NFR-03 | `prd.md#non-functional-requirements` | Credential security |
| DEC-07 | `techspec.md#technical-decisions` | advapi32.dll CredReadW / CredFree |
| CMP-06 | `techspec.md#components-and-flow` | src/TokenHound.Infrastructure/Security/WindowsCredentialManager.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Spec: `docs/specs/02-WINDOWS-CREDENTIALS-SECURITY.md`

## Work

- [ ] T06.1 Define native structs (`CREDENTIAL`) and P/Invoke signatures for `CredReadW` and `CredFree` using source-generated interop.
- [ ] T06.2 Implement `WindowsCredentialManager.cs` implementing `ICredentialStore.ReadCredentialAsync`.
- [ ] T06.3 Ensure unmanaged memory is released safely via `CredFree` in a `try...finally` block.
- [ ] T06.4 Implement `WindowsCredentialManagerTests.cs` verifying non-existent targets return null without throwing exceptions.

## Acceptance criteria

- `ReadCredentialAsync` returns null gracefully when the target credential does not exist (error code `ERROR_NOT_FOUND`).
- Safely cleans up unmanaged memory after reading credential blob.
- Guarded with OS check (`OperatingSystem.IsWindows()`).

## Verification

- Unit: Query non-existent credential target and verify null return without crashing.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --filter-class "*WindowsCredentialManagerTests*"`
- Expected evidence: MTP test suite passes.

## Affected files

- Create:
  - `src/TokenHound.Infrastructure/Security/WindowsCredentialManager.cs`
  - `tests/TokenHound.Infrastructure.Tests/Security/WindowsCredentialManagerTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert interop bindings if marshalling fails.

## Handoff

- Produced result: Pending execution.
- Changed files: Pending execution.
- Checks: Pending execution.
- Validated state: Pending execution.
- Open items: Pending execution.

### ADR candidates

Pending execution.
