# Stable execution context

Load in this exact order:

1. `tasks/prd-17-hud-backdrop-effects/codereview_3/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T07 — Align the transient log flag with the repository rule and the immediate-next-transition intent

## Outcome

Only the transition right after an arrange-invalid frame logs at Debug. A same-mode frame with another reason clears the transient flag. The reason comparison follows the CLAUDE.md `OrdinalIgnoreCase` rule.

## Dependencies and boundaries

- Depends on: codereview_2/T06
- Unblocks: re-review (codereview_4)
- In scope: `HudBackdropController.Apply` flag handling and string comparison.
- Out of scope: fill and companion behavior, log message text, other controllers.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_3 optional | `codereview.md#findings` (Optional improvements) | `StringComparison.Ordinal` vs the CLAUDE.md `OrdinalIgnoreCase` rule |
| codereview_3 optional | `codereview.md#findings` (Optional improvements) | `_transient` survives same-mode frames and demotes a later unrelated transition |
| workflow DEC-10 | `workflow.md#dec-10-reservations-hil-on-codereview_3` | Human-chosen scope |

## Requirements

- Every `Apply` call updates the transient flag from its reason, before the same-mode early return.
- A transition logs at Debug only when its own reason is the arrange-invalid one, or the call right before it had that reason.
- The comparison uses `StringComparison.OrdinalIgnoreCase`.

## Context to recover on demand

- TechSpec: Observability and rollout (DEC-09 wording).
- Rules and skills: CLAUDE.md string comparison rule, `dotnet-efficient-validation`.
- Code: `src/TokenHound.App/UI/Windows/HudBackdropController.cs` — `Apply`.

## Work

- [x] T07.1 Move the flag update before the same-mode return and keep the previous value for the level choice.
- [x] T07.2 Switch the comparison to `OrdinalIgnoreCase`.

## Acceptance criteria

- Code inspection: after an arrange-invalid frame followed by a same-mode frame with another reason, the next transition logs at Information.
- Build 0 errors, 0 warnings; full suite passes.

## Verification

- Unit: none new (WPF controller); existing tests must pass.
- Integration: —
- E2E: omitted by .NET desktop policy.
- Manual: — (code inspection).
- Environment dependency: none.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal`; `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: build and test exit codes 0.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/HudBackdropController.cs`

## Observability and recovery

- Operational signal: `HUD background switched to {Mode} ({Reason})`, at Debug only for the transient pair.
- Recovery: revert the file.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `Apply` now reads the arrange-invalid reason with `StringComparison.OrdinalIgnoreCase`. It saves the previous flag as `afterTransient` and sets `_transient` from the current reason before the same-mode early return. A same-mode frame with another reason therefore clears the flag, and a transition logs at Debug only when its own reason is arrange-invalid or the call right before it had that reason. Fill and companion behavior are unchanged.
- Changed files: `src/TokenHound.App/UI/Windows/HudBackdropController.cs`.
- Checks: App build 0 errors, 0 warnings, exit 0; test build 0 errors, 0 warnings, exit 0; full MTP run with `--minimum-expected-tests 1`: 1101 total, 1101 succeeded, exit 0. No `StringComparison.Ordinal` or QA-01..03 hits in the file.
- Validated state: worktree at HEAD 2bc32ef with PRD 17 changes and all corrections through T07, Release, Windows 10.0.26200.
- Open items: log levels are verified by code inspection only.
