# Stable execution context

Load in this exact order:

1. `tasks/prd-17-hud-backdrop-effects/codereview_2/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T05 — Split the contour frame method and align the PRD 17 user-facing text

## Outcome

`HudContourController.Synchronize(HudContourGeometry)` is ≤ 30 lines with unchanged behavior; the ROADMAP PRD 17 row states the current status; the Settings card description names high contrast with the other conditions that turn the material off.

## Dependencies and boundaries

- Depends on: —
- Unblocks: re-review (codereview_3)
- In scope: extract the shadow part of `Synchronize(HudContourGeometry)` into a private helper; `docs/ROADMAP.md` PRD 17 row; `HudBackdropSettingsCard.xaml` description text.
- Out of scope: shadow or backdrop behavior, `App.xaml.cs` size, other ROADMAP rows.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_2/QA-04 | `codereview.md#findings` (Optional improvements) | `Synchronize(HudContourGeometry)` spans 32 lines |
| codereview_2 optional | `codereview.md#findings` | ROADMAP row says "Awaiting the visual check"; card text omits high contrast |
| workflow DEC-09 | `workflow.md#dec-09-reservations-hil-on-codereview_2` | Human-chosen scope B |

## Requirements

- The frame still updates shadow bounds, shadow geometry, shadow visibility, and then the backdrop, in the same order and on the same conditions.
- The ROADMAP row reflects DEC-08 and codereview_2: visual check and independent review done, awaiting acceptance.
- The card text lists transparency effects, energy saver, and high contrast, matching `HudBackdropPolicy` and README.

## Context to recover on demand

- Rules and skills: CLAUDE.md C# style (methods ≤ 30 lines), `dotnet-efficient-validation`.
- Code: `src/TokenHound.App/UI/Windows/HudContourController.cs:124-155`; `src/TokenHound.App/UI/Controls/Settings/HudBackdropSettingsCard.xaml:32`; `docs/ROADMAP.md:72`.

## Work

- [x] T05.1 Extract the shadow update into a private helper; `Synchronize(HudContourGeometry)` ≤ 30 lines.
- [x] T05.2 Update the ROADMAP PRD 17 row.
- [x] T05.3 Update the card description text.

## Acceptance criteria

- Method span ≤ 30 lines; file ≤ 300 lines.
- Build 0 errors, 0 warnings; full suite passes (≥ 1101 tests).

## Verification

- Unit: none new (WPF window code is not unit-tested here); existing tests must pass.
- Integration: —
- E2E: omitted by .NET desktop policy.
- Manual: — (refactor without behavior change; code inspection).
- Environment dependency: none.
- Commands: `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal`; `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal`; `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1`
- Expected evidence: build and test exit codes 0; method span.

## Affected files

- Modify: `src/TokenHound.App/UI/Windows/HudContourController.cs`
- Modify: `src/TokenHound.App/UI/Controls/Settings/HudBackdropSettingsCard.xaml`
- Modify: `docs/ROADMAP.md`
- Modify: `README.md` (scope extension recorded in the handoff)

## Observability and recovery

- Operational signal: unchanged.
- Recovery: revert the three files.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: the shadow part of `HudContourController.Synchronize(HudContourGeometry)` moved into the private `SynchronizeShadow(contour, bounds)`, with the same order and conditions; `Synchronize` now spans 14 lines. The ROADMAP PRD 17 row says the visual check and independent review are done (approved with reservations) and acceptance is pending. The card description now reads "Uses the solid background when Windows transparency effects are off, or when energy saver or high contrast is on." Scope extension, same cause: `README.md:24` said the solid background shows while transparency effects "are on", which inverts the policy. It now says "are off, or while energy saver or high contrast is on".
- Changed files: `src/TokenHound.App/UI/Windows/HudContourController.cs` (233 lines), `src/TokenHound.App/UI/Controls/Settings/HudBackdropSettingsCard.xaml`, `docs/ROADMAP.md`, `README.md`.
- Checks: App build 0 errors, 0 warnings, exit 0; test build 0 errors, 0 warnings, exit 0; `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1` 1101 total, 1101 succeeded, exit 0.
- Validated state: worktree at HEAD 2bc32ef with PRD 17 changes, codereview_1 corrections, and T05, Release, Windows 10.0.26200.
- Open items: none; this is a refactor with no behavior change, verified by code inspection.
