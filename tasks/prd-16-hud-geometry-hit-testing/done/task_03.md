# Stable execution context

Load the current versions in this order: `prd.md`, `techspec.md`, then this file. Recover only missing/changed sources.

# T03: Preserve placement and prove desktop acceptance

## Outcome

The contour and passive shadow remain aligned across scale, display and placement transitions, with current-state automated/manual evidence ready for visual approval and independent review.

## Dependencies and boundaries

- Depends on: completed T02. Unblocks: human visual gate, delegated independent review and HIL 3.
- Scope: final transform/DPI/size/drag integration, compatibility regressions, full manual matrix and accurate documentation.
- Excludes: config migration, provider changes, release/push/commit, self-review approval.

## Traceability

| Source | Section | Covered obligation |
| --- | --- | --- |
| OBJ-01..03, US-01..05, FR-01..11, PD-01..04 | prd.md#outcomes-and-metrics; functional-requirements | Integrated behavior and visual evidence |
| NFR-01..05 | prd.md#non-functional-requirements | Focus, lifecycle, boundaries and evidence |
| DEC-04..08, CMP-04..07, TC-02..06 | techspec.md#technical-decisions; test-approach | Transforms, persistence, regression and manual acceptance |

## Context to recover on demand

- T02 handoff, current controller frame/lifecycle contracts, placement/scale source spans and quality baseline.
- TechSpec Manual acceptance script and observed three-monitor inventory; validation.md pending rows.
- Existing NotchPlacement/DisplayResolver/scale/tray regression tests. Graft callers before any further symbol changes.

## Work

- [x] T03.1 Complete owner-DPI/ancestor-transform synchronization, preserving one application of HUD scale and physical DPI, and event-driven unchanged-frame suppression. Integrate with existing placement/scale paths without new timers.
- [x] T03.2 Preserve plain-click versus actual DragMove distinction; verify side-to-Free orientation/contour regeneration and existing saved coordinate/clamping semantics. Add focused transform/placement boundary tests where new logic exists.
- [x] T03.3 Build current affected projects and run the complete Infrastructure test executable once for integrated regression evidence. Reuse valid earlier results only while their diff/configuration remains current.
- [x] T03.4 (physical disconnection waived by workflow DEC-05) Execute TC-04..06 and TechSpec manual script steps 2..6 across all sizes/modes and available displays; record exact results and pending human disconnection evidence. Essential missing cases remain pending.
- [x] T03.5 Synchronize README/roadmap and the applicable ARCHITECTURE input-mechanism statement with delivered, verified behavior; run scoped diff/quality checks and refresh `graft build` after the code changes.
- [x] T03.6 Record a complete handoff and present visual evidence to the coordinator's human gate. Independent review remains a separate delegated flow stage after visual approval.

## Acceptance criteria

- Live modes, DPI/size/display transitions and vertical-to-Free drag leave no stale input/shadow surface; screenshots show continuous joins and unclipped content.
- Restart/monitor fallback retain existing placement fields and meaning. Popups/menus stay inward and above HUD surfaces.
- Full regression passes with nonzero executed tests; manual checks distinguish coordinator and human evidence. Missing essential checks prevent declaring delivery complete.
- Existing Windows colors/content/shadow intent and focus behavior pass the human visual gate before review.

## Verification

- Unit: TC-02 transforms, negative origins, fractional scale/DPI, content-safe bounds and existing placement compatibility.
- Integration/regression: Infrastructure executable, including existing provider presentation/tray/placement/style tests; no provider external API call is added.
- E2E: omitted by .NET desktop policy.
- Manual: coordinator and human follow TechSpec steps 2..7; physical disconnection and human visual approval require actual evidence.
- Commands: TechSpec build route, then `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-build --no-restore -- --minimum-expected-tests 1`; check `$LASTEXITCODE` and executed count. Finish with scoped `rtk git diff --stat`, `--check`, quality-profile checks and `rtk proxy graft build`.
- Evidence: integrated diff fingerprint, test summary, monitor/mode/size/manual tables, screenshots, pending items and human decision provenance.

## Affected files

- Modify as required by the integration: `src/TokenHound.App/UI/Windows/NotchWindow.xaml.cs`, .Dock.cs, .Placement.cs, .Scale.cs, HudContourController.cs and HudShadowWindow.xaml.cs; pure HudContourFrame/Layout contracts only if current frame conversion requires it.
- Create: `tests/TokenHound.Infrastructure.Tests/Placement/HudContourTransformTests.cs`; link pure files in the existing test csproj if needed.
- Update: validation.md, README.md, docs/ROADMAP.md, ARCHITECTURE.md, handoff and generated graft graph.
- Remeasure any newly touched existing C# file before modifying it; document a baseline addendum instead of silently extending scope.

## Observability and recovery

Retain event-only structured logs and verify no recurring geometry output during idle. Preserve baseline evidence; on a failed acceptance case, capture its coordinates/state and fix the source. Changes outside the approved product/architecture contract require an exception gate.

## Handoff

- Produced result: transform extraction, ceiling pixel extent for docking, and coordinator desktop acceptance across six modes, sizes 50/125/150%, three DPIs, drag-to-Free, and restart. Human visual gate approved 2026-10-09; physical display disconnection accepted as residual risk (DEC-05).
- Changed files: new HudContourTransform.cs and HudContourTransformTests.cs; HudContourController.cs, NotchWindow.Placement.cs, test csproj, TechSpec baseline addendum and validation/workflow/snapshot/checkpoint evidence. No provider/schema change.
- Checks: App/test Release builds passed before PixelExtent changes. Full MTP run: 1,076 passed, one new fixture failed out of 1,077; corrected its expected translation and reran all 11 transform tests successfully after a clean test rebuild. Existing non-transform results are retained; 2026-10-09 headless rerun built App/test Release with zero warnings/errors and passed the full suite, 1,082/1,082 including the five PixelExtent cases. Details: validation.md T03 partial execution.
- Validated state (2026-10-09): build `2bc32ef`; coordinator matrix across six modes, sizes 50/80/100/125/150%, three DPIs, drag-to-Free, restart, StatusPopup, empty HUD and idle (validation.md); right boundary x=1920 confirmed; human visual gate approved; Anti Slop gate PASS. Earlier partial state is preserved in validation.md history.
- Open items (2026-10-09): CR-01, physical preferred-display disconnection not executed; accepted as residual risk by DEC-05; delivery accepted at HIL 3 (DEC-07). Review: codereview_01 and codereview_02 rejected on evidence and records only, no code defect.

### ADR candidates

None at this partial boundary; approved canonical contour/passive-shadow decision remains the existing TechSpec candidate.
