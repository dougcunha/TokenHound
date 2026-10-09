# Stable execution context

Load in this exact order:

1. `tasks/prd-16-hud-geometry-hit-testing/codereview_01/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T01 — Record StatusPopup, busy, badge, and empty-HUD desktop evidence

## Outcome

`validation.md` holds desktop evidence on the current build for the StatusPopup, the busy pulse, any reachable provider badge, and the empty or minimum HUD state, in at least one docked and one Free mode.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T02
- In scope: desktop observation of the existing build, screenshots, a temporary settings change to reach the empty state, restored afterwards.
- Out of scope: code changes; physical display disconnection (CR-01, pending for HIL 3).

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_01/CR-02 | `codereview.md#findings` | Missing desktop evidence for FR-10 (StatusPopup) and FR-11 (empty/minimum layout, badges, busy pulse) |

## Requirements

- TechSpec manual step 3: StatusPopup opens inward and dismisses; the empty/minimum HUD keeps a valid contour; badges and busy pulse render inside the capsule without clipping.
- User settings are backed up before and restored after; the installed instance is preserved.

## Context to recover on demand

- TechSpec: `techspec.md#manual-acceptance-script` step 3.
- Code: `HudActionsViewModel.IsStatusVisible` (status text after a manual Refresh), `NotchWindow.Dock.cs:ApplyStatusPopupPlacement`, `NotchWindow.xaml` `ProviderBadge`/`IsBusy` bindings.
- Prior evidence and procedure: `validation.md#t03-desktop-acceptance-2026-10-09`.

## Work

- [x] T01.1 Launch the Release build; trigger Refresh from the HUD menu; capture the busy state and the StatusPopup in a docked mode and in Free; check inward placement and dismissal.
- [x] T01.2 Record any provider badge that the current provider states show; if none is reachable without altering credentials, record that limitation.
- [x] T01.3 Temporarily disable all providers in the settings copy, relaunch, capture the empty/minimum HUD in a docked mode and in Free, then restore the settings backup and verify its hash.
- [x] T01.4 Append the results and screenshot paths to `validation.md`.

## Acceptance criteria

- Screenshots and observations exist for StatusPopup, busy pulse, and empty/minimum HUD in docked and Free modes on build `2bc32ef`.
- Badge evidence is recorded or its unreachability is stated.
- User settings hash matches the backup after the task.

## Verification

- Unit: none (evidence only).
- Integration: none.
- E2E: omitted by .NET desktop policy.
- Manual: coordinator through Windows MCP.
- Environment dependency: user desktop available (released 2026-10-09).
- Commands: Windows MCP App/Screenshot; native probe script as in T03.
- Expected evidence: screenshots under the feature folder, validation.md section.

## Affected files

- Modify: `tasks/prd-16-hud-geometry-hit-testing/validation.md`
- Create: screenshot evidence in the feature folder

## Observability and recovery

- Operational signal: none.
- Recovery: restore `%LOCALAPPDATA%/TokenHound/settings.json` from the task backup.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: CR-02 evidence recorded in validation.md "Correction round 1 evidence (codereview_01 CR-02), 2026-10-09"; screenshots `cr01-*.png`, native JSON `cr01-native-*.json`. Settings restored (hash verified). Limitation: no badge state beyond the current provider glyphs and attention state is reachable without changing credentials; busy pulse motion is not visible in a still capture.
- Changed files: validation.md and evidence files (task_01); workflow.md, done/task_03.md, context-snapshot.md, checkpoint.json (task_02). No code.
- Checks: settings hash restored; records read through for consistency.
- Validated state: HEAD 2bc32ef, Release build, three-monitor inventory unchanged.
- Open items: CR-01 for HIL 3.
