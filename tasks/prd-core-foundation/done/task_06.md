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

- Produced result: Implemented `WindowsCredentialManager` in `TokenHound.Infrastructure.Security` wrapping native Win32 `advapi32.dll` (`CredReadW`, `CredFree`) with safe unmanaged memory disposal in `try...finally`, implementing `TokenHound.Core.Contracts.ICredentialStore` without UI prompts. Guarded against non-Windows platforms with `OperatingSystem.IsWindows()`. Handled `ERROR_NOT_FOUND` (1168) and `ERROR_NO_SUCH_LOGON_SESSION` (1312) gracefully returning `null`. Implemented robust UTF-8 and Unicode (UTF-16LE) blob decoding with automatic base64 unpacking for `go-keyring-base64:` prefixes. Implemented comprehensive test suite in `WindowsCredentialManagerTests.cs` validating non-existent targets, cancellation token propagation, argument validation for null/whitespace, and interface contract adherence.
- Changed files:
  - `src/TokenHound.Infrastructure/Security/WindowsCredentialManager.cs`
  - `tests/TokenHound.Infrastructure.Tests/Security/WindowsCredentialManagerTests.cs`
  - `tasks/prd-core-foundation/task_06.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (exit code: 0, 0 errors)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*WindowsCredentialManagerTests*"` (exit code: 0, 12 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (exit code: 0, 39 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (exit code: 0, 34 passed, 0 failed, 0 skipped)
- Validated state: All acceptance criteria met; `ReadCredentialAsync` and `ReadCredential` return null gracefully when credential does not exist without throwing; unmanaged memory released safely in `finally` via `CredFree`; guarded with OS check (`OperatingSystem.IsWindows()`); cancellation token propagated; C# invariants verified (sealed class, files <= 300 lines, methods <= 30 lines, nesting <= 3 levels, alphabetized usings, file-scoped namespaces, XML doc comments on all public members, no secret strings logged).
- Open items: None.

### ADR candidates

None.
