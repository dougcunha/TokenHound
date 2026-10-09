# Stable execution context

Load in this exact order:

1. `tasks/prd-17-hud-backdrop-effects/codereview_2/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T06 — Log transient arrange-invalid transitions at Debug

## Outcome

A frame where the HUD arrange is invalid still swaps to the solid fill and back, but the two transitions it causes (`Solid (ArrangeInvalid)` and the next `Material`) log at Debug. Every other transition still logs once at Information with its deciding input.

## Dependencies and boundaries

- Depends on: T05 (same file `HudContourController.cs`)
- Unblocks: re-review (codereview_3)
- In scope: `HudBackdropController` log level selection; a shared constant for the arrange-invalid reason used by `HudContourController`; the TechSpec Observability line for this transient case (DEC-09).
- Out of scope: fill and companion behavior on any path; other log messages.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_2 optional | `codereview.md#findings` (Optional improvements) | Two Information lines per invalid frame during resize |
| workflow DEC-09 | `workflow.md#dec-09-reservations-hil-on-codereview_2` | Human-chosen scope D; amends TechSpec Observability for the transient case |

## Requirements

- The arrange-invalid hide transition and the first transition after it log at `Debug`; all other transitions log at `Information` as today.
- The reason string is one constant shared by both controllers (no duplicated literal).
- Fill and companion behavior are unchanged (CR-01 fix preserved).

## Context to recover on demand

- TechSpec: Observability and rollout.
- Rules and skills: CLAUDE.md (structured logging, `UPPER_CASE` constants, `string.Equals` with an explicit comparison), `dotnet-efficient-validation`.
- Code: `src/TokenHound.App/UI/Windows/HudBackdropController.cs` — `Hide`, `Apply`; `src/TokenHound.App/UI/Windows/HudContourController.cs:105`.

## Work

- [x] T06.1 Add the reason constant and the transient-aware log level in `Apply`.
- [x] T06.2 Use the constant in `HudContourController`.
- [x] T06.3 Amend the TechSpec Observability line and re-hash it under DEC-09.

## Acceptance criteria

- Code inspection: only the arrange-invalid pair goes to Debug.
- Build 0 errors, 0 warnings; full suite passes.

## Verification

- Unit: none new; existing tests must pass.
- Integration: —
- E2E: omitted by .NET desktop policy.
- Manual: — (log level only; code inspection).
- Environment dependency: none.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal`; `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: build and test exit codes 0.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/HudBackdropController.cs`
- Modify: `src/TokenHound.App/UI/Windows/HudContourController.cs`
- Modify: `tasks/prd-17-hud-backdrop-effects/techspec.md`

## Observability and recovery

- Operational signal: `HUD background switched to {Mode} ({Reason})` at Debug for the transient pair.
- Recovery: revert the files.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `HudBackdropController.ARRANGE_INVALID_REASON` ("ArrangeInvalid") is the shared reason, and `HudContourController` passes it from the arrange-invalid branch. `Apply` logs at `Debug` when the transition reason is that constant, or when the previous transition had it (the `_transient` flag). Every other transition logs once at `Information`, as before. A frame that is already Solid does not set the flag, because `Apply` returns early on the same mode. Fill and companion behavior are unchanged. The TechSpec Observability line is amended under DEC-09.
- Changed files: `src/TokenHound.App/UI/Windows/HudBackdropController.cs` (144 lines), `src/TokenHound.App/UI/Windows/HudContourController.cs` (233 lines), `tasks/prd-17-hud-backdrop-effects/techspec.md` (Observability and rollout).
- Checks: App build 0 errors, 0 warnings, exit 0; test build 0 errors, 0 warnings, exit 0; full MTP run with `--minimum-expected-tests 1`: 1101 total, 1101 succeeded, exit 0. QA-01..03 `rg` over the touched files: 0 hits; no duplicated "ArrangeInvalid" literal.
- Validated state: worktree at HEAD 2bc32ef with PRD 17 changes, codereview_1 corrections, T05, and T06, Release, Windows 10.0.26200.
- Open items: the log levels are verified by code inspection only, with no runtime log capture.
