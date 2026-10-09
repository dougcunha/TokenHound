# Context snapshot: prd-16-hud-geometry-hit-testing

> Hints only; checkpoint, workflow, contracts and code win on conflict. Protocol: `.agents/skills/sdd-snapshot/SKILL.md`.

## Header

- status: active
- generated: 2026-10-09
- stage: tasks
- stage_source: tasks.md
- covers_through: T01/T02 completed; T03 partial, not approved
- authored_code: yes
- git_head: 6f2e92b
- worktree: feature App/test edits and artifacts; preceding docs/triage and unrelated tooling preserved
- next_step: sdd-orchestrate-flow, resume T03 right-boundary desktop reproduction once the desktop is available
- other_eligible: none
- superseded_by: none

## Load map

Read now entries before choosing work, on-select for T03, on-run before commands, and on-edit before source changes. Contracts and current evidence remain authoritative.

## Next step brief

T03 has partial code and validation. Headless validation is current: on 2026-10-09 App/test Release builds passed and the full Infrastructure suite passed 1,082/1,082, including the five PixelExtent cases; the work was committed as in-progress. Six 50%/150%-DPI contours were observed; right docking ended at physical x=1921 rather than 1920. Primary WPF source confirms non-layout-rounded SizeToContent uses ceiling; placement used nearest rounding. The fix in NotchWindow.Placement.cs passes unit tests; desktop reproduction remains pending. Read task_03.md, validation.md T03 partial section, workflow latest Events, and checkpoint first. The user needs the computer: do not launch or manipulate desktop windows until availability is explicitly confirmed. ContextBrake reached RED; processes 18084/33928 exited and installed 23240 restored. No final visual/review/acceptance gate has passed.

## Decisions

- [D-01] (when: now) Workflow DEC-03 permits implementation/validation/corrections within existing contracts; no commit/push/release; src: workflow.md#dec-03-hil-2-approve-the-technical-plan-and-implementation; until: material scope change.
- [D-02] (when: on-select: T03) Preserve the canonical contour and passive WS_EX_TRANSPARENT shadow companion; src: techspec.md#technical-decisions; until: technical decision changes.
- [D-03] (when: on-select: T03) Anti Slop during is an explicit session override, PD-01..04 supplies Windows direction and dials 1/1/1; src: workflow.md#dec-04-anti-slop-during-implementation; until: preference changes.

## Learnings

- [L-03] (when: on-run: desktop screenshot) Inventory primary is index 1/DISPLAY2; required Screenshot [2] is upper DISPLAY3. Regions spanning a boundary use live pillow; single-monitor dxcam may be stale; src: validation.md#validated-baseline; until: MCP changes.
- [L-04] (when: on-run: shell hashing) Nested Windows PowerShell lacks SHA256.HashData and Convert.ToHexString; use SHA256.Create/ComputeHash plus BitConverter; src: workflow.md#events; until: environment changes.
- [L-05] (when: on-run: native bounds) Temporary ContourProbe sets thread DPI context -4 before native queries; actual target HWND was 9832970; src: validation.md#t03-partial-execution-stopped-for-desktop-availability; until: target recreated.
- [L-07] (when: on-run: desktop target) Oversized all-desktop target covers taskbar/overflow; Snapshot still lists covered UI. Use the original primary target, inspect visible screenshots, and use integer chrome points for menus; src: validation.md#t03-partial-execution-stopped-for-desktop-availability; until: harness changes.
- [L-08] (when: on-edit: NotchWindow.Placement.cs; HudContourTransform.cs) Fractional SizeToContent pixels use ceiling in HwndSource.RoundDeviceSize; new PixelExtent fixes Math.Round mismatch, but current correction is unverified; src: validation.md#t03-partial-execution-stopped-for-desktop-availability; until: correction validated.

## Code map

- [M-02] (when: on-edit: NotchWindow*) ApplyChrome sets mode/padding/popups; drag records Free only when Left/Top change; src: done/task_02.md#handoff; until: integration changes.
- [M-03] (when: on-run: MTP) Native executable links pure App files; filters follow -- and minimum expected tests 1/exit code are mandatory. Latest five PixelExtent tests passed on 2026-10-09; src: task_03.md#handoff; until: current validation supersedes it.
- [M-04] (when: on-edit: HudContourController*; HudShadowWindow*) Controller captures ancestor basis, converts owner/shadow DPI, compares contour/matrix/bounds/DPI; shadow Path uses Canvas+whole-effect RenderTransform and shared post-effect exclusion; src: done/task_02.md#handoff; until: integration changes.

## Open threads

- [O-02] (when: on-select: T03) Current build/tests, right-boundary reproduction, full size/DPI/display/restart/provider/popup matrix, docs/quality/graph and physical disconnection remain pending; src: validation.md#t03-partial-execution-stopped-for-desktop-availability; until: evidence recorded.
- [O-03] (when: on-edit: worktree) Preserve preceding README/roadmap/triage and unrelated tooling; src: workflow.md#feature-context; until: scope changes.
- [O-04] (when: now) Human visual gate precedes independent delegated review/HIL 3; Anti Slop delivery gate pending; src: workflow.md#gates; until: gates completed.
- [O-05] (when: now) Human requested use of the computer. Desktop validation is unavailable until explicitly released; headless work can resume separately; src: workflow.md#events; until: human confirms desktop availability.
