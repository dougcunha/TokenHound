# TokenHound Implementation Roadmap

This roadmap records delivered capabilities and the next development candidates for TokenHound on Windows 11 (.NET 10).

Delivery baseline: **v0.1.13**, commit `6f2e92b`. Completed feature checkpoints and their acceptance records establish delivery status. Historical task directories may have been removed or renumbered; use the current paths below.

## Delivered Foundation

The initial tracer bullet established the pure Core models and contracts, resilience policies, read-only credential and storage primitives, and the WPF HUD. Provider integrations now cover Claude Code, Codex, Cursor, Antigravity/Gemini, Copilot, OpenCode Go, and Cline.

| Capability | Delivery evidence |
| --- | --- |
| Provider enablement, status badges, and engine gating | Commit `ba002f4` |
| Active/idle polling cadence and rate-limit retry configuration | Commit `2ae5620` |
| System tray commands and background lifecycle | Commit `e5b3fbd` |

## Completed SDD Tracks

Their SDD artifacts were removed after completion; recover one with `git checkout 7a1ee4c -- tasks/prd-NN-<slug>/`. Earlier roadmap numbers for visual polish and docking are obsolete.

| Track | Delivered behavior | Evidence |
| --- | --- | --- |
| PRD 06 | Local read-only MCP provider metrics | SDD artifacts at `7a1ee4c` |
| PRD 07 | Claude multi-profile support | SDD artifacts at `7a1ee4c` |
| PRD 08 | Claude quota breakdown | SDD artifacts at `7a1ee4c` |
| PRD 09 | Provider Status window | SDD artifacts at `7a1ee4c` |
| PRD 10 | Antigravity empty-quota session warmup | SDD artifacts at `7a1ee4c` |
| PRD 11 | GitHub release checks and updates | SDD artifacts at `7a1ee4c` |
| PRD 12 | Start-with-Windows setting compatible with the installer | SDD artifacts at `7a1ee4c` |
| PRD 13 | HUD size setting | SDD artifacts at `7a1ee4c` |
| PRD 14 | Multi-monitor selection, edge docking, and persisted placement | SDD artifacts at `7a1ee4c` |
| PRD 15 | Settings controls and dialog resource split | SDD artifacts at `7a1ee4c` |
| PRD 16 | HUD edge geometry and contour hit testing | SDD artifacts at `7a1ee4c` |

### HUD Placement Baseline

Multi-monitor docking landed in commit `90d6748` and shipped in v0.1.13. Settings and the HUD Position menu share the placement state.

- Docking modes: Top left, Top center, Top right, Left edge, Right edge, and Free.
- Side docking uses a vertical capsule; top docking uses a horizontal capsule.
- Docked placement can follow the primary monitor or remember a specific display.
- A disconnected preferred display falls back to the primary monitor without discarding the preference.
- Dragging switches to Free mode. Stored coordinates from earlier versions migrate to Free mode.
- Placement accounts for display changes, work areas, scale, and DPI-related resizing.

The Settings resource refactor landed in commit `6f2e92b` and shipped in the same release. It preserves the existing controls and behavior.

## Completed: HUD Edge Geometry and Contour Hit Testing

**Status:** completed 2026-10-09 under `sdd-full` (commit `2bc32ef` plus evidence). The independent review approved it with reservations; the open improvements and the residual risk (physical preferred-display disconnection not tested) are recorded as DEC-05 and DEC-06 in the prd-16 `workflow.md` (SDD artifacts at `7a1ee4c`).

**Outcome:** the visible HUD contour and its interactive area agree in every supported docking mode, while the HUD keeps its non-activating behavior.

Candidate scope:

- Define a shared capsule geometry with inverse rounded corners where it joins a screen edge.
- Apply geometry appropriate to each top or side placement, with a defined Free-mode contour.
- Verify click-through outside the visible contour, including transparent corners and layout gutters.
- Preserve clicks, drag, context menus, and provider interaction inside the contour.
- Validate placement and interaction across HUD scales, DPI settings, and monitors.

The implementation paints the capsule as a contour decorator over a canonical geometry and relies on per-pixel transparency of the layered HUD window, not `HTTRANSPARENT`, so clicks outside the contour reach other processes. The shadow renders in an owned `WS_EX_TRANSPARENT` companion window, so it never receives input.

The [original design reference](design/2026-08-28-usage-notch-design.md) describes a macOS right-edge notch. Its visual references inform this feature; Windows behavior must also cover the top, left, and Free modes already delivered by PRD 14.

### Separate Follow-up Candidates

The following work is outside the geometry and hit-testing candidate:

| Candidate | Source and status |
| --- | --- |
| Acrylic backdrop effect (PRD 17) | Implemented under `sdd-lean` ([plan](../tasks/prd-17-hud-backdrop-effects/tasks.md), [validation](../tasks/prd-17-hud-backdrop-effects/validation.md)): a host-backdrop composition companion under the layered HUD, with a Settings toggle and live fallback. Accepted on 2026-10-09 with recorded open items (outside-click checks in five docking modes, failure-log field, merge test) |
| Smooth ring progress interpolation | Deferred visual work; the existing busy indicator already pulses |
| Consumption threshold alerts with per-provider mute | Product idea in the [original implementation plan](reference/2026-08-28-usage-notch-plan.md), not an approved Windows feature |
| Automatic hiding for fullscreen applications | Product idea in the same reference plan, not an approved Windows feature |
| Provider ordering in Settings | Product idea in the same reference plan, not an approved Windows feature |

## Provider Expansion Candidates

| Provider | Status | Reference |
| --- | --- | --- |
| Z.ai GLM Coding Plan | Planned integration; no completed implementation track is recorded | [Technical specification](specs/07-PROVIDER-GLM-CODING-PLAN.md) |
| Perplexity | Planned integration; no completed implementation track is recorded | [Technical specification](specs/08-PROVIDER-PERPLEXITY.md) |

Provider priorities depend on actual usage. Revalidate endpoint and credential assumptions before implementing either specification.

## Execution and Validation

- Apply [sdd-triage](../.agents/skills/sdd-triage/SKILL.md) before opening a new feature; the human decides the process level.
- Follow the selected SDD path and keep independent review for `sdd-full` and `sdd-lean`.
- Follow [AGENTS.md](../AGENTS.md) and the repository validation skills for builds and MTP tests. Enforce at least one executed test; listing or building alone does not validate behavior.
- Verify HUD interaction on the user desktop through Windows MCP. Preserve `WM_MOUSEACTIVATE -> MA_NOACTIVATE` and the non-activating window-style invariants.
- When parallel implementation is explicitly authorized, use isolated worktrees to avoid shared Windows build locks.
