# Stable execution context

Load in this exact order:

1. `tasks/prd-17-hud-backdrop-effects/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T04 — Destroy the companion HWND when its backdrop opt-in fails

## Outcome

When `DwmSetWindowAttribute(DWMWA_USE_HOSTBACKDROPBRUSH)` fails after `CreateWindowExW` succeeded, `HudBackdropInterop.Create` destroys the new window before it rethrows. No hidden companion HWND leaks on that path.

## Dependencies and boundaries

- Depends on: —
- Unblocks: re-review (codereview_2)
- In scope: `HudBackdropInterop.Create`, with a small private helper for the attribute call so `Create` drops to ≤ 30 lines (QA-04 reservation on the same method).
- Out of scope: other interop methods, the shared `SetWindowPos` P/Invoke duplication, `HudBackdropWindow`.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/CR-02 | `codereview.md#findings` | HWND leaks when the backdrop attribute fails after creation |
| codereview_1/QA-04 | `codereview.md#quality-profile` | `HudBackdropInterop.Create` spans 33 lines |
| TechSpec Errors | `techspec.md` | Native resources released; fail closed |

## Requirements

- A failed HRESULT from `DwmSetWindowAttribute` calls `DestroyWindow(handle)` and then throws the same exception type as today (`Marshal.ThrowExceptionForHR`), so `HudBackdropController.TryShow` still falls back to Solid.
- The success path is unchanged: the handle is returned with the attribute set.
- `Create` is ≤ 30 lines.

## Context to recover on demand

- TechSpec: Errors, DEC-02.
- Rules and skills: CLAUDE.md C# style, `dotnet-efficient-validation`.
- Code: `src/TokenHound.App/Interop/HudBackdropInterop.cs:63-95` — `Create`; `:136` `Destroy`; `:214` `DestroyWindow`.

## Work

- [x] T04.1 Move the attribute call into a private helper that destroys the handle and rethrows on a failed HRESULT.
- [x] T04.2 Call the helper from `Create`; confirm `Create` is ≤ 30 lines and the file stays ≤ 300 lines.

## Acceptance criteria

- Code inspection: no path from a successful `CreateWindowExW` exits `Create` with an exception without `DestroyWindow`.
- Build passes with 0 warnings; the full test suite passes with no regression.

## Verification

- Unit: none new; the native failure cannot be injected without a seam the TechSpec does not plan.
- Integration: —
- E2E: omitted by .NET desktop policy.
- Manual: — (code inspection only).
- Environment dependency: none.
- Commands: same as T03 (App and test builds, full MTP run with `--minimum-expected-tests 1`).
- Expected evidence: build 0 errors and 0 warnings; tests pass with exit code 0; `Create` line span.

## Affected files

- Modify: `src/TokenHound.App/Interop/HudBackdropInterop.cs`

## Observability and recovery

- Operational signal: the existing `HUD backdrop unavailable; using the solid fill ({HResult})` warning.
- Recovery: revert the file.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `HudBackdropInterop.Create` calls the new private `EnableHostBackdrop(handle)`, which sets `DWMWA_USE_HOSTBACKDROPBRUSH` and, on a failed HRESULT, calls `DestroyWindow(handle)` before `Marshal.ThrowExceptionForHR`. The exception type and the `TryShow` fallback are unchanged. `Create` now spans 26 lines (`HudBackdropInterop.cs:63-88`), down from 33.
- Changed files: `src/TokenHound.App/Interop/HudBackdropInterop.cs` (239 lines).
- Checks: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal` 0 errors, 0 warnings, exit 0; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` 0 errors, 0 warnings, exit 0; `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` 1101 total, 1101 succeeded, exit 0 (App dll rebuilt after the edits). QA-01..03 `rg` over the touched files: 0 hits. QA-05: remaining hits are declarations, plus the pre-existing `HudContourController.cs:215` call outside the diff hunks.
- Validated state: worktree at HEAD 2bc32ef with PRD 17 uncommitted changes plus T03 and T04 together, Release, Windows 10.0.26200; both corrections were built and tested in the same run.
- Open items: the native failure cannot be injected without a seam the TechSpec does not plan, so it is verified by code inspection only.
