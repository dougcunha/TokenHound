# Stable execution context

Load in this exact order:

1. `tasks/prd-12-windows-autostart/prd.md`
2. `tasks/prd-12-windows-autostart/techspec.md`
3. This file

Use the current PRD and TechSpec versions already loaded; recover only missing or changed sources. This order does not guarantee a host cache hit.

---

# T01 — Startup shortcut service

## Outcome

`StartupLaunchService` reports whether TokenHound starts at logon, can turn it on, and can turn it off. On means `TokenHound.lnk` exists in the Startup folder and Windows "Startup apps" has not disabled it. Turning it on writes the shortcut to a given exe and clears the disabled flag; turning it off deletes the shortcut. Tests prove this on a temp folder with a fake approval store.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02
- In scope: CMP-01..CMP-06, and the TC-01..TC-06 tests (CMP-12, service and policy part).
- Out of scope: the ViewModel, XAML, App wiring, and installer (T02).

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-01..FR-04 | `prd.md#functional-requirements` | Entry path, state read, enable, disable |
| NFR-01..NFR-03 | `prd.md#non-functional-requirements` | Core purity and seams, per-user only, fast |
| DEC-01..DEC-05, DEC-11 | `techspec.md#technical-decisions` | Path, COM interop, approval flag, seams, overwrite, sync |
| CMP-01..CMP-06 | `techspec.md#components-and-flow` | New files |
| TC-01..TC-06 | `techspec.md#test-approach` | Tests |

## Context to recover on demand

- Applicable skills and rules: `dotnet-efficient-validation`, `repository-cli-efficiency`, and the C# rules in `CLAUDE.md` (one class per file, sealed, XML docs, `UPPER_CASE` constants, and the blank-line rules)
- Existing code: `src/TokenHound.Infrastructure/Security/WindowsCredentialManager.cs` for the interop style. `src/TokenHound.Core/Policies/` for policy class conventions. `src/TokenHound.Infrastructure/Updates/InstallModeDetector.cs` for the static-helper and XML-doc style.
- Contract: `techspec.md#contracts-and-data` (shortcut name kept equal to the installer's `{#MyAppName}.lnk`, and the registry path is read and delete only)

## Work

- [ ] T01.1 `StartupApprovalPolicy.IsDisabled(ReadOnlySpan<byte>)` in Core, with `StartupApprovalPolicyTests` (TC-01).
- [ ] T01.2 `ShellLink` internal COM interop (`IShellLinkW`, `IPersistFile`, `CLSID_ShellLink`): `Save` and `ReadTarget`, releasing COM objects in `finally`.
- [ ] T01.3 `IStartupApprovalStore` and `RegistryStartupApprovalStore` (HKCU `StartupApproved\StartupFolder`, read and delete only, guarded by `OperatingSystem.IsWindows()`).
- [ ] T01.4 `IStartupLaunchService` and `StartupLaunchService`, with `SHORTCUT_FILE_NAME`, `CreateDefault()`, and structured logs, plus `StartupLaunchServiceTests` (TC-02..TC-06) on a unique temp folder with a fake approval store.

## Acceptance criteria

- `IsDisabled` returns true only for a non-empty value whose first byte is odd (TC-01).
- `IsEnabled()` is false for an empty folder (TC-02). It is also false when the shortcut exists but the approval value is disabled (TC-05).
- `Enable(exe)` creates `<folder>\TokenHound.lnk` with target `exe` and working directory `Path.GetDirectoryName(exe)`. It creates the folder when missing and deletes a disabled approval value. `IsEnabled()` is true afterwards (TC-03, TC-05).
- `Enable(B)` over an existing shortcut to A leaves the target at B (TC-04).
- `Disable()` deletes the shortcut, and running it again with no shortcut does not throw (TC-06).
- Per-user only (NFR-02): the only locations touched are `Environment.SpecialFolder.Startup` and `Registry.CurrentUser`. Verified by `rtk rg -n "LocalMachine|CommonStartup|SpecialFolder\.\w+" src/TokenHound.Infrastructure/Startup`, which must show only `SpecialFolder.Startup`, and no `LocalMachine`.
- Synchronous API (DEC-11, NFR-03): `IStartupLaunchService` exposes no `Task`-returning members, and each call does at most one file check, one registry read or delete, and one shortcut write. TC-03 runs `Enable` then `IsEnabled` in well under 200 ms (the test asserts `< 200 ms` with `Stopwatch` as a coarse guard).
- Core has no `Microsoft.Win32`, `InteropServices`, or `System.IO` usage in the new policy (QA-04). COM objects are released (QA-05). The build has 0 warnings.

## Verification

- Unit: TC-01 (Core.Tests); TC-02, TC-05, TC-06 (Infrastructure.Tests, fake approval store).
- Integration: TC-03, TC-04 use real COM shell links on a temp folder (Windows only).
- E2E: omitted by .NET desktop policy.
- Manual: none in this task (MA-2 is in T02).
- Commands: `rtk dotnet build tests/TokenHound.Core.Tests` and `rtk dotnet build tests/TokenHound.Infrastructure.Tests`. Then `rtk dotnet run --project tests/TokenHound.Core.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*StartupApproval*"` and `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*StartupLaunch*"`, followed by full runs of both test projects.
- Environment dependency: Windows (local).
- Expected evidence: test output with TC names, a 0-warning build, and the quality profile greps.

## Affected files

- Create: `src/TokenHound.Core/Policies/StartupApprovalPolicy.cs`, `src/TokenHound.Infrastructure/Startup/ShellLink.cs`, `src/TokenHound.Infrastructure/Startup/IStartupApprovalStore.cs`, `src/TokenHound.Infrastructure/Startup/RegistryStartupApprovalStore.cs`, `src/TokenHound.Infrastructure/Startup/IStartupLaunchService.cs`, `src/TokenHound.Infrastructure/Startup/StartupLaunchService.cs`, `tests/TokenHound.Core.Tests/Policies/StartupApprovalPolicyTests.cs`, `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs`

## Observability and recovery

- Operational signal: `Log.Information` or `Log.Warning` with `{Operation}` and `{ExecutablePath}` (TechSpec Observability).
- Recovery: new files only. Revert the commit.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: `StartupApprovalPolicy.IsDisabled` in Core (odd first byte means disabled). In Infrastructure/Startup: `ShellLink` (COM `IShellLinkW`/`IPersistFile`, `Save`/`ReadTarget`, `FinalReleaseComObject` in `finally`), `IStartupApprovalStore` plus `RegistryStartupApprovalStore` (HKCU `StartupApproved\StartupFolder`, read and delete only, guarded by `OperatingSystem.IsWindows()`), and `IStartupLaunchService` plus `StartupLaunchService` (`SHORTCUT_FILE_NAME`, `ShortcutPath`, `CreateDefault()`, `IsEnabled`/`Enable`/`Disable`, Serilog `{Operation}` logs). `ShellLink` and `StartupLaunchService` are `[SupportedOSPlatform("windows")]`, so the `net10.0` project has no CA1416 warnings.
- Changed files: created `src/TokenHound.Core/Policies/StartupApprovalPolicy.cs`, `src/TokenHound.Infrastructure/Startup/{ShellLink,IStartupApprovalStore,RegistryStartupApprovalStore,IStartupLaunchService,StartupLaunchService}.cs`, `tests/TokenHound.Core.Tests/Policies/StartupApprovalPolicyTests.cs`, `tests/TokenHound.Infrastructure.Tests/Startup/StartupLaunchServiceTests.cs`.
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests --no-incremental` and the same for Core.Tests: 0 warnings, 0 errors.
  - Core.Tests `--filter-class "*StartupApproval*"`: 5/5 passed. `IsDisabled_UsesOddFirstByte` covers TC-01.
  - Infrastructure.Tests `--filter-class "*StartupLaunch*"`: 5/5 passed:
    - `IsEnabled_WhenShortcutMissing_ReturnsFalse`: TC-02
    - `Enable_CreatesShortcutToExecutable`: TC-03, including the working-directory assertion (`ShellLink.ReadWorkingDirectory`) and the < 200 ms guard
    - `Enable_WhenShortcutTargetsAnotherExecutable_RewritesTarget`: TC-04
    - `Enable_WhenWindowsDisabledEntry_ClearsFlag`: TC-05
    - `Disable_DeletesShortcutAndToleratesMissingShortcut`: TC-06
  - Full runs after the last change (`--no-incremental` builds with 0 warnings): Core.Tests 166/166 and Infrastructure.Tests 944/944 passed.
  - Quality profile: QA-01..QA-03, QA-04, and QA-06 are empty. QA-05 has 2 `ReleaseComObject` hits (both paths). QA-07: all files under 300 lines. NFR-02 grep: only `SpecialFolder.Startup` and `Registry.CurrentUser`.
- Validated state: worktree on base `c744030` plus the T01 files above. Debug build, Windows 11, .NET 10 SDK.
- J3 gate (active): 6/9 claims `auto`. Three were below threshold:
  - Claim 9 (full runs): fixed by rerunning after the last change.
  - Claim 6 (per-user only): justified by the lines `StartupLaunchService.cs:53` (`SpecialFolder.Startup`) and `RegistryStartupApprovalStore.cs:25,39` (`Registry.CurrentUser`), and the NFR-02 grep, which found no `LocalMachine`.
  - Claim 8: justified by `StartupApprovalPolicy.cs:1`, whose only using is `System`; by `ShellLink.cs`, whose `finally` blocks call `FinalReleaseComObject`; and by the latest `--no-incremental` builds, 0 warnings.
  - Rubric-only `escalate` (blast_radius/test_gap, safe_to_apply 0.44) is an unspecific signal. `RegistryStartupApprovalStore` has no automated test by design (NFR-01 forbids touching the real key); MA-2 covers it.
- Open items: none. MA-2 (the real StartupApproved format) belongs to the T02 visual check.
- Reservation hits: none.

### ADR candidates

None - direct TechSpec implementation or local decision.
