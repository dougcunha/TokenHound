# Stable execution context

Load in this exact order:

1. `tasks/prd-17-hud-backdrop-effects/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T03 — Restore the solid fill whenever the backdrop companion is hidden

## Outcome

Every path that hides the backdrop companion also shows the solid `#18181B` capsule fill. After a shadow synchronization failure, the HUD keeps the solid fill for the rest of the session, never the 0xED tint without material.

## Dependencies and boundaries

- Depends on: —
- Unblocks: re-review (codereview_2)
- In scope: `HudBackdropController.Hide` applies `HudBackdropMode.Solid` with a reason; the `HudContourController` hide paths (HUD hidden or no contour, arrange invalid, shadow `Win32Exception`) pass that reason; splitting the two QA-05 calls in `HudBackdropController.cs` (lines 47 and 112) because the file is edited.
- Out of scope: the shadow failure handling itself, the policy, the companion window, `App.xaml.cs` size, the `Candidate` log field.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/CR-01 | `codereview.md#findings` | Shadow failure and arrange-invalid paths hide the companion but leave the tint fill |
| codereview_1/QA-05 | `codereview.md#quality-profile` | ≥ 4-argument calls at `HudBackdropController.cs:47` and `:112` on one line |
| PRD FR-05 | `prd.md` | Fallback renders exactly `#18181B` |
| TechSpec Flow and Errors | `techspec.md` | No frame shows tint without blur; any failure resolves to Solid |

## Requirements

- When the companion is hidden for any reason, the decorator background is the solid brush captured at construction.
- The mode transition logs once, through the existing `Apply` path, with the reason (`Hidden`, `ArrangeInvalid`, `ShadowFailure`, or equivalent).
- The next successful `Synchronize` in Material mode restores the tint in the same pass that shows the companion.
- After a shadow failure (`_failed` in `HudContourController`), no later frame re-applies the tint.

## Context to recover on demand

- TechSpec: Flow, Errors, DEC-05.
- Rules and skills: CLAUDE.md C# style (≥ 4-argument calls split, methods ≤ 30 lines), `dotnet-efficient-validation`.
- Code: `src/TokenHound.App/UI/Windows/HudBackdropController.cs` — `Hide`, `Apply`; `src/TokenHound.App/UI/Windows/HudContourController.cs` — `Schedule`, `Synchronize`, `HideCompanions`.

## Work

- [x] T03.1 `HudBackdropController.Hide(string reason)` hides the companion and calls `Apply(HudBackdropMode.Solid, reason)`.
- [x] T03.2 `HudContourController.HideCompanions` takes and forwards the reason from each of its three callers.
- [x] T03.3 Split the calls at `HudBackdropController.cs:47` and `:112` one argument per line.

## Acceptance criteria

- Code inspection: every `HudBackdropWindow.Hide` call reached from `HudContourController` goes through `Apply(Solid, …)`.
- Build passes with 0 warnings; the full test suite passes with no regression (1101 or more tests).
- Manual: with transparency effects ON, the HUD shows the material; toggling the HUD hidden and shown again shows the material again (no stuck solid fill).

## Verification

- Unit: none new; WPF window and decorator types are not unit-tested in this repository (TechSpec Test approach). Existing `HudBackdrop*` tests must still pass.
- Integration: —
- E2E: omitted by .NET desktop policy.
- Manual: the shadow `Win32Exception` cannot be triggered on demand; verified by code inspection. The show/hide round trip is optional and needs transparency ON, which DEC-06 no longer authorizes; skip it unless the human authorizes it again.
- Environment dependency: none for build and tests.
- Commands: `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --nologo --verbosity:minimal`; `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --nologo --verbosity:minimal`; `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: build 0 errors and 0 warnings; tests pass with exit code 0.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/HudBackdropController.cs`
- Modify: `src/TokenHound.App/UI/Windows/HudContourController.cs`

## Observability and recovery

- Operational signal: `HUD background switched to Solid ({Reason})` log line on each hide transition.
- Recovery: revert the two files.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: `HudBackdropController.Hide(string reason)` hides the companion and applies `HudBackdropMode.Solid` through `Apply`, so the decorator shows the solid brush on every hide path. `HudContourController.HideCompanions(string reason)` forwards `Hidden` (HUD hidden or no contour), `ArrangeInvalid`, and `ShadowFailure`. After a shadow failure, `Schedule` and `Synchronize` return early, so the solid fill stays for the session. The next successful Material frame restores the tint in the same pass that shows the companion. The two QA-05 calls (`TryShow`, `Color.FromArgb`) are split one argument per line.
- Changed files: `src/TokenHound.App/UI/Windows/HudBackdropController.cs` (137 lines), `src/TokenHound.App/UI/Windows/HudContourController.cs` (227 lines).
- Checks: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal` 0 errors, 0 warnings, exit 0; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` 0 errors, 0 warnings, exit 0; `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` 1101 total, 1101 succeeded, exit 0 (App dll rebuilt after the edits). QA-01..03 `rg` over the touched files: 0 hits. QA-05: remaining hits are declarations, plus the pre-existing `HudContourController.cs:215` call outside the diff hunks.
- Validated state: worktree at HEAD 2bc32ef with PRD 17 uncommitted changes plus T03 and T04 together, Release, Windows 10.0.26200; both corrections were built and tested in the same run.
- Open items: the shadow `Win32Exception` path cannot be triggered on demand, so it is verified by code inspection only. The optional show/hide manual round trip was not run, because it needs transparency ON and DEC-06 covered T02 only. A transient arrange-invalid frame now shows the solid fill for that frame, as the TechSpec flow requires.
