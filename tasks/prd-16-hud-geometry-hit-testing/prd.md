# PRD: HUD Edge Geometry and Contour Hit Testing

## Problem and context

TokenHound v0.1.13 supports five docked placements and a Free position. Its Windows HUD uses a rounded rectangular capsule whose chrome changes with orientation. The original design references show inverse rounded joins between a side notch and the display edge. The current native message hook has no dedicated contour hit-testing policy.

The next feature must make the visible capsule and its interactive area agree across the existing placements. A user must be able to operate the HUD inside its contour and operate another application through the space outside it. Non-activation, placement, and existing provider interactions remain essential behavior.

This is a new feature, not a claim that the current transparent window blocks every outside click. Existing outside-click behavior must be measured as a baseline before changing it. The product contract specifies observable behavior; the TechSpec chooses how Windows delivers that behavior.

## Outcomes and metrics

| ID | Expected outcome | Metric or evidence |
| --- | --- | --- |
| OBJ-01 | The visible HUD outline matches its interactive area. | Inside/outside acceptance cases pass for all six existing placement modes, including a target application in a different process. |
| OBJ-02 | Docked contours visually join the selected work-area edge. | Screenshots demonstrate continuous edge joins for top and side modes, with corner cases contained in the selected work area. |
| OBJ-03 | The feature preserves the accepted v0.1.13 interaction and placement behavior. | Focus, drag, menus, provider details, scale, monitor changes, and restart checks pass against the recorded baseline. |

## Stories and journeys

| ID | User | Need | Benefit | Flow or edge |
| --- | --- | --- | --- | --- |
| US-01 | Developer using a docked HUD | See a notch joined to the selected edge | Consistent shape across placements | Switch among Top left, Top center, Top right, Left edge, and Right edge. |
| US-02 | Developer using an application under the HUD | Click through transparent space outside the capsule | Access underlying controls | Click near inverse corners, rounded corners, gutters, and shadow. |
| US-03 | Developer repositioning the HUD | Drag it and keep the chosen Free position | Retain current placement workflow | Drag a docked capsule, use Free, then restart. |
| US-04 | Developer with multiple displays | Keep geometry and interaction aligned after scale or display changes | Avoid clipped content and misplaced hit areas | Resize the HUD, change displays, disconnect a preferred display, or rearrange monitors. |
| US-05 | Developer inspecting usage | Use provider hover details, menus, and status views as before | Preserve access to quota information | Open details and menus from both horizontal and vertical capsules. |

## Functional requirements

| ID | Requirement | Acceptance criterion |
| --- | --- | --- |
| FR-01 | Provide an edge-joined capsule contour for every docked mode, using smooth inverse rounded joins at the work-area edge. | Top modes are horizontal, side modes are vertical, and mirrored left/right or top transitions have a consistent curve profile. Joins are continuous without gaps, self-intersections, or clipped provider content. |
| FR-02 | Contain corner-docked geometry within the selected work area. | Top left and Top right keep their existing edge anchoring. A decorative continuation is omitted or bounded on the side without available work-area space; it does not displace the HUD off-screen or clip a provider cell. |
| FR-03 | Give Free mode a horizontal floating capsule with a closed, fully rounded contour and no edge-attachment joins. | Selecting Free or finishing a drag removes edge joins at the dropped position. Free stays horizontal, including when the drag began from a vertical docked capsule. |
| FR-04 | Deliver mouse input outside the visible capsule contour to the underlying application. | Left clicks, right clicks, and double clicks reach their expected target in an application running in a different process. Test points include transparent corners, layout gutters, space outside inverse joins, and shadow-only pixels. The HUD opens no menu or drag gesture for those points. |
| FR-05 | Preserve input inside the capsule. | Provider hover and existing provider actions work; right-click opens the existing HUD menu; dragging from an eligible part of the capsule works. Painted fill and stroke belong to the interactive contour; shadow alone does not. |
| FR-06 | Apply contour changes immediately when the placement changes. | Switching placement through Settings or the Position menu updates shape, orientation, and hit area together. Repeated switches leave no outdated hit area or invisible input surface. |
| FR-07 | Preserve the existing drag-to-Free workflow and its persistence. | A drag starting inside the capsule ends in Free mode at the visible dropped position. Restart restores that mode and its saved position, subject to existing off-screen recovery behavior. |
| FR-08 | Keep geometry, hit area, and placement coherent across supported HUD sizes, DPI settings, and display changes. | At every existing size selection, the minimum-size capsule and a populated capsule remain usable. Checks cover top/side/Free modes, displays with different DPI settings when available, negative monitor coordinates, and preferred-display disconnection. |
| FR-09 | Preserve placement configuration compatibility. | The existing mode, display preference, and Free coordinates retain their format and meaning. No migration or reset is introduced; the existing primary-display fallback and normal off-screen correction remain available. |
| FR-10 | Preserve hover details, popups, and context menus. | Current surfaces open on the existing inward-facing side for each docked edge. Their content is not clipped by the new capsule contour, and their existing controls and dismissal behavior remain usable. |
| FR-11 | Preserve provider layout and existing presentation states. | Provider order, glyphs, labels, quota values, badges, and the existing busy pulse are unchanged. The contour fits the existing empty/minimum layout and supported provider combinations without overlapping or clipping content. |

## Non-functional requirements

| ID | Attribute | Limit or criterion |
| --- | --- | --- |
| NFR-01 | Focus preservation | Clicking, right-clicking, hovering, or dragging the HUD does not activate its window or steal keyboard input from the previously active application. Existing native non-activation and layered-window composition invariants remain satisfied. |
| NFR-02 | Bounded input and rendering work | No additional periodic polling or global input interception is introduced for this feature. Shape updates follow actual size, placement, or display changes; sustained idle use adds no repeating redraw loop. |
| NFR-03 | Domain and data boundaries | Core remains free of UI/OS dependencies. Provider credentials, adapters, quota semantics, MCP/API contracts, and configuration schemas are unchanged. |
| NFR-04 | Accessibility and continuity | Existing keyboard access through Settings, tray actions, and dialogs is preserved. Existing text/glyph contrast and size controls are preserved; no new motion or transparency dependency is introduced. |
| NFR-05 | Verifiable desktop acceptance | The TechSpec must cover geometry/policy unit checks and scripted Windows desktop checks for actual cross-application input and focus. No E2E suite is added. Evidence must distinguish automated checks, coordinator desktop checks, and checks performed by the human. |

## User experience

The feature is applied to the existing HUD without adding a new setting or changing the placement menu. Selecting a docked position changes its contour at once. Dragging detaches it into the Free contour. Inside interactions keep their current purpose; outside space remains available to the underlying application.

Product decisions proposed for HIL 1:

| ID | Proposal | Reason and acceptance |
| --- | --- | --- |
| PD-01 | Preserve the current Windows colors, stroke treatment, provider cells, spacing, and existing shadow intent. Adapt the reference images' edge-joining shape only. | The user has accepted the current Windows release. This feature changes the contour without redesigning its information or theme; screenshots retain the current visual identity. |
| PD-02 | Use a fully rounded floating capsule in Free mode. | A detached HUD has no edge to join; FR-03 provides an observable shape and orientation contract. |
| PD-03 | Bound or omit unavailable decorative continuations at top-left/top-right work-area corners. | Corner anchoring must not require off-screen geometry or extra monitor space; FR-02 defines acceptance. |
| PD-04 | Exclude shadow-only pixels from the interactive area. | The shadow communicates elevation; it is not a control. FR-04 and FR-05 distinguish it from the painted capsule. |

Exact radii, curve control points, stroke construction, and the Windows input mechanism are technical design choices. They must satisfy this product contract and be presented in the TechSpec at HIL 2.

Existing stale, unavailable, rate-limited, and empty provider feedback stays in place. This feature introduces no provider state or new error flow.

## Constraints and dependencies

- Windows 11, WPF, and .NET 10 remain the supplied platform and stack.
- PRD 14 docking and PRD 15 Settings refactoring are completed dependencies at the Git base `6f2e92b0f4cc40c5cde1326149f000768fe3a781`.
- Preserve the `WM_MOUSEACTIVATE -> MA_NOACTIVATE` behavior and the `SWP_NOZORDER`/no-`SWP_SHOWWINDOW` composition requirements from `AGENTS.md`.
- Use existing placement, display, and HUD size choices; do not add a configuration schema or migrate stored placement.
- The human approves this product contract at HIL 1 before TechSpec and task planning. Implementation requires HIL 2.
- Desktop validation uses Windows MCP launch/screenshot tools on the user desktop, with the repository's primary-monitor convention. Visual acceptance precedes independent review.
- Do not modify unrelated local tooling/configuration present in the worktree.

## Out of scope

- Mica/Acrylic or other backdrop effects.
- New ring interpolation, hover animations, or changes to the existing busy pulse.
- New placement modes, automatic hiding, fullscreen detection, or draggable provider ordering.
- Consumption alerts, new providers, billing changes, or usage history.
- New configuration settings, coordinate migrations, or changes to stored monitor identity.
- Tooltip redesign, new provider actions, theme redesign, or generated raster assets.
- Installer, update, packaging, commit, push, or release work.

## Assumptions and sources

| Kind | Source | Supported fact or impact |
| --- | --- | --- |
| User request | Current conversation: next-feature recommendation accepted, followed by "continue" | Proceed with the recommended feature and `sdd-full`; later product and execution gates remain separate. |
| Project invariant | [AGENTS.md](../../AGENTS.md) | Non-activation, composition, Core purity, desktop validation, and repository conventions. |
| Delivered baseline | [PRD 14](../prd-14-hud-multimonitor-docking/prd.md) and [PRD 15 checkpoint](../prd-15-settings-window-split/checkpoint.json) | Current docking modes, placement persistence, and completed Settings refactor. |
| Current implementation | `src/TokenHound.App/UI/Windows/NotchWindow.Dock.cs:20-72`, `NotchWindow.xaml.cs:90-150`, `UI/Placement/HudEdgeLayout.cs:9-25` | Orientation-aware rounded chrome, focus/display handling, drag behavior, and Free's horizontal orientation. |
| Roadmap | [Next candidate](../../docs/ROADMAP.md#next-candidate-hud-edge-geometry-and-contour-hit-testing) | Single outcome and separation of geometry/input work from later visual effects. |
| Design reference | [Original design](../../docs/design/2026-08-28-usage-notch-design.md), [hover frame](../../docs/design/frame-124-hover-tooltip.png), and [detail frame](../../docs/design/frame-125-detail.png) | Inverse rounded right-edge joins. These are macOS reference visuals, not a specification of Windows behavior or a demand to copy their colors, content, or platform assumptions. |
| Assumption | Current Windows palette/content remains the product baseline | PD-01 needs HIL 1 approval; a broader redesign would change scope. |
| Assumption | Free should visibly detach with a fully rounded contour | PD-02 needs HIL 1 approval; preserve current horizontal orientation. |
| Measurement pending | Outside-click and focus baseline | Baseline behavior is not yet measured for this feature; TechSpec validation must record it before implementation. |
| Environment pending | Availability of mixed-DPI monitors during acceptance | Exact available desktop arrangements must be recorded in the TechSpec. A missing required environment must remain an explicit validation limitation, not a fabricated pass. |

## PRD acceptance gate

- [x] Every requirement has a stable ID and an observable criterion.
- [x] Outcomes, scope boundaries, and exclusions are explicit.
- [x] Internal rules have user or repository sources; proposals and unmeasured behavior are identified.
- [x] Implementation details remain for the TechSpec.
- [x] HIL 1: human approval of the current PRD, including PD-01 through PD-04 (workflow DEC-02).
