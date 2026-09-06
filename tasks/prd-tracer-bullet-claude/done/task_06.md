# Stable execution context

Load in this exact order:

1. `tasks/prd-tracer-bullet-claude/prd.md`
2. `tasks/prd-tracer-bullet-claude/techspec.md`
3. This file

---

# T06 — Win32 WindowStyles Interop Helper

## Outcome

Implements `WindowStyles` in `TokenHound.App/Interop/` to inject Win32 extended window styles `WS_EX_NOACTIVATE (0x08000000)`, `WS_EX_TOOLWINDOW (0x00000080)`, and `WS_EX_TOPMOST (0x00000008)` via `HwndSource`, ensuring the Notch HUD never steals keyboard focus.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T09
- In scope: Native Win32 interop (`GetWindowLongW`, `SetWindowLongW`), bitmask manipulation helper, and unit tests.
- Out of scope: Window XAML layout or controls.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| FR-06 | `prd.md#functional-requirements` | Win32 WS_EX_NOACTIVATE styling |
| NFR-01 | `prd.md#non-functional-requirements` | Focus safety guarantee |
| DEC-06 | `techspec.md#technical-decisions` | WindowStyles with SetWindowLongW |
| CMP-06 | `techspec.md#components-and-flow` | src/TokenHound.App/Interop/WindowStyles.cs |

## Context to recover on demand

- Applicable skills: `dotnet-efficient-validation`
- Spec: `ARCHITECTURE.md#1-technological-decision-why-wpf-net-10-over-winui-3`

## Work

- [ ] T06.1 Implement `WindowStyles.cs` under `TokenHound.App/Interop/`.
- [ ] T06.2 Define constants `GWL_EXSTYLE = -20`, `WS_EX_NOACTIVATE = 0x08000000`, `WS_EX_TOOLWINDOW = 0x00000080`, `WS_EX_TOPMOST = 0x00000008`.
- [ ] T06.3 Implement `EnableNonActivating(IntPtr hwnd)` and bitmask helper `ApplyExtendedStyles(int currentStyle)`.
- [ ] T06.4 Implement `WindowStylesTests.cs` verifying bitmask calculations and style composition.

## Acceptance criteria

- `ApplyExtendedStyles` correctly sets `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST` without removing existing styles.
- P/Invoke bindings compile cleanly with source-generated or safe native interop.
- Guarded against null or zero HWND pointers.

## Verification

- Unit: Test bitmask manipulation with sample style values.
- Commands: `rtk dotnet test tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*WindowStylesTests*"`
- Expected evidence: Tests pass verifying bitmask calculation.

## Affected files

- Create:
  - `src/TokenHound.App/Interop/WindowStyles.cs`
  - `tests/TokenHound.Infrastructure.Tests/Interop/WindowStylesTests.cs`

## Observability and recovery

- Operational signal: Unit test execution.
- Recovery: Revert bitmask flags if window rendering shows artifacts.

## Handoff

- Produced result: Implemented native Win32 window styles interop helper `WindowStyles` in `TokenHound.App.Interop`. Defines constants `GWL_EXSTYLE (-20)`, `WS_EX_NOACTIVATE (0x08000000)`, `WS_EX_TOOLWINDOW (0x00000080)`, and `WS_EX_TOPMOST (0x00000008)`. Provides bitmask composition via `ApplyExtendedStyles(currentStyle)` preserving existing style bits, style validation via `HasNonActivatingStyles(style)`, and native HWND style injection via `EnableNonActivating(hwnd)` utilizing safe P/Invoke (`GetWindowLongW` and `SetWindowLongW` from `user32.dll`) guarded by `OperatingSystem.IsWindows()` and `hwnd != IntPtr.Zero`. Implemented comprehensive unit test suite in `TokenHound.Infrastructure.Tests.Interop.WindowStylesTests` covering flag application from zero, preservation of existing style bits, matrix evaluation of required flags in `HasNonActivatingStyles`, and graceful handling of `IntPtr.Zero`.
- Changed files:
  - `src/TokenHound.App/Interop/WindowStyles.cs`
  - `tests/TokenHound.Infrastructure.Tests/Interop/WindowStylesTests.cs`
  - `tasks/prd-tracer-bullet-claude/task_06.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj` (Passed: 0 errors, 4 warnings)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1 --filter-class "*WindowStylesTests*"` (Passed: 12 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 141 passed, 0 failed, 0 skipped)
  - `rtk dotnet test --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` (Passed: 34 passed, 0 failed, 0 skipped)
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj` (Passed: 0 errors, 4 warnings)
- Validated state: All 175 tests pass across the solution. Strict adherence to AGENTS.md (sealed static class, file-scoped namespaces, XML documentation on all public members, methods <= 30 lines, file <= 300 lines, alphabetized usings, upper-case constants, single-line if statement formatting).
- Open items: None. Ready for downstream integration in T09 (`NotchWindow`).

### ADR candidates

None. Implementation conforms strictly to TechSpec DEC-06 and CMP-06.
