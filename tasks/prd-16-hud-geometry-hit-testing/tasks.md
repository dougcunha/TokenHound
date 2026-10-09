# Implementation plan: HUD Edge Geometry and Contour Hit Testing

## Stable sources

- [Approved PRD](prd.md), workflow DEC-02.
- [TechSpec](techspec.md), proposed for HIL 2 together with this plan.
- Read sources before task/handoff state. No implementation is authorized until HIL 2.

## Dependency graph

| ID | Delivery | Depends on | Unblocks |
| --- | --- | --- | --- |
| T01 | Recorded desktop baseline and tested canonical contour definition | HIL 2 | T02 |
| T02 | New HUD contour with shadow-only pass-through and preserved inside input | T01 | T03 |
| T03 | Placement/DPI/restart continuity and integrated desktop acceptance evidence | T02 | Human visual gate, independent review, HIL 3 |

Execute serially. T02/T03 share Window, controller, and placement integration files; parallel writing is prohibited. T01's pure geometry foundation is necessary for both render surfaces and their common boundary.

## Traceability matrix

| Source ID | Source section | Obligation | Tasks | Evidence |
| --- | --- | --- | --- | --- |
| OBJ-01 | prd.md#outcomes-and-metrics | Inside/outside agrees across six modes | T02, T03 | TC-04 |
| OBJ-02 | prd.md#outcomes-and-metrics | Continuous contained edge joins | T01, T02, T03 | TC-01, TC-05 |
| OBJ-03 | prd.md#outcomes-and-metrics | Preserve existing interaction/placement | T01, T03 | Baseline, TC-02..06 |
| US-01 | prd.md#stories-and-journeys | Docked developer | T01, T02 | TC-01, TC-05 |
| US-02 | prd.md#stories-and-journeys | Underlying application clicks | T02, T03 | TC-04 |
| US-03 | prd.md#stories-and-journeys | Reposition and restore Free | T03 | TC-02, TC-05 |
| US-04 | prd.md#stories-and-journeys | Multiple displays and scale | T03 | TC-02, TC-05 |
| US-05 | prd.md#stories-and-journeys | Provider details and menus | T02, T03 | TC-04, TC-05 |
| FR-01 | prd.md#functional-requirements | Mirrored smooth docked joins | T01, T02, T03 | TC-01, TC-05 |
| FR-02 | prd.md#functional-requirements | Contained top corners | T01, T03 | TC-01, TC-05 |
| FR-03 | prd.md#functional-requirements | Rounded horizontal Free | T01, T02, T03 | TC-01, TC-05 |
| FR-04 | prd.md#functional-requirements | Cross-process outside input | T02, T03 | TC-04 |
| FR-05 | prd.md#functional-requirements | Preserve fill/stroke interaction | T02, T03 | TC-03, TC-04 |
| FR-06 | prd.md#functional-requirements | Coherent live contour changes | T02, T03 | TC-03..05 |
| FR-07 | prd.md#functional-requirements | Drag-to-Free and restart | T03 | TC-02, TC-05 |
| FR-08 | prd.md#functional-requirements | Sizes/DPI/display coherence | T01, T03 | TC-01, TC-02, TC-05 |
| FR-09 | prd.md#functional-requirements | Placement schema compatibility | T03 | TC-02, TC-05 |
| FR-10 | prd.md#functional-requirements | Existing popups/menus | T02, T03 | TC-04, TC-05 |
| FR-11 | prd.md#functional-requirements | Existing provider presentation | T01, T02, T03 | TC-01, TC-05 |
| NFR-01 | prd.md#non-functional-requirements | Preserve focus/native composition | T02, T03 | TC-03, TC-04 |
| NFR-02 | prd.md#non-functional-requirements | Event-driven bounded work | T02, T03 | TC-03, TC-06 |
| NFR-03 | prd.md#non-functional-requirements | Core/data boundaries | T01, T02, T03 | Scoped diff and regression tests |
| NFR-04 | prd.md#non-functional-requirements | Existing accessibility/theme | T02, T03 | TC-05 |
| NFR-05 | prd.md#non-functional-requirements | Honest desktop evidence | T01, T02, T03 | TC-01..06, validation.md |
| PD-01 | prd.md#user-experience | Windows palette/content/shadow | T02, T03 | TC-05 |
| PD-02 | prd.md#user-experience | Rounded Free | T01, T02, T03 | TC-01, TC-05 |
| PD-03 | prd.md#user-experience | Omit unavailable continuation | T01, T03 | TC-01, TC-05 |
| PD-04 | prd.md#user-experience | Shadow excluded from input | T02, T03 | TC-04 |
| CMP-01 | techspec.md#components-and-flow | Pure contour | T01 | TC-01 |
| CMP-02..06 | techspec.md#components-and-flow | Rendering/native/controller/window integration | T02, T03 | TC-02..06 |
| CMP-07 | techspec.md#components-and-flow | Linked tests | T01, T02, T03 | MTP outputs |
| DEC-01..08 | techspec.md#technical-decisions | Shared geometry, alpha, companion, lifecycle and compatibility | T01, T02, T03 | Matching component work; scoped quality gate |
| TC-01 | techspec.md#test-approach | Geometry | T01 | HudContourLayoutTests |
| TC-02 | techspec.md#test-approach | Transforms/placement | T03 | HudContourTransformTests and placement regressions |
| TC-03 | techspec.md#test-approach | Styles/lifecycle | T02, T03 | HudContourStyleTests and manual lifecycle |
| TC-04 | techspec.md#test-approach | Real mouse/focus | T02, T03 | Windows desktop script |
| TC-05 | techspec.md#test-approach | Visual/placement matrix | T03 | Screenshots and recorded results |
| TC-06 | techspec.md#test-approach | Idle/disposal/evidence | T03 | Inspection and manual lifecycle |
| QA-01..07 | techspec.md#quality-profile | Applicable quality contract | T01, T02, T03 | Scoped checks and baseline comparison |

## Tasks

- [T01: Define and verify the shared contour](done/task_01.md).
- [T02: Render the interactive contour and passive shadow](done/task_02.md).
- [T03: Preserve placement and prove desktop acceptance](task_03.md).

## Coverage gate

- Coverage: pass for planning; every product and technical obligation has a task and evidence destination. Actual evidence is pending.
- Traceability: pass; stable IDs connect PRD, decisions/components, scenarios and tasks.
- Dependencies: pass; acyclic T01 -> T02 -> T03, with explicit serial file ownership.
- Atomicity: pass; one necessary shared foundation, one usable render/input integration, one placement/acceptance delivery. No separate test-only cleanup task.
- Executability: pass for unit/build work on the supplied Windows/.NET stack; desktop checks depend on the user desktop and display-disconnection evidence.
- Validation profile: desktop .NET, MTP executable route, nonzero tests enforced, E2E omitted. Three mixed-DPI monitors are available; physical disconnection is not yet verified.
- Idempotency: reruns inspect current artifacts/code and reuse valid evidence; do not add duplicate controllers/windows/test links or replay completed manual checks as new passes.

## Assumptions and open items

- HIL 2 must approve the two-surface design and this exact task plan before T01 production edits.
- Required environment: Windows MCP user desktop, a disposable different-process click target, available 100/125/150% displays, and human assistance for physical disconnection if needed. Plan approval does not supply unperformed manual evidence.
- Product scope is unchanged. Commit/push/release and unrelated tooling changes remain outside this plan.

## State

- [x] T01: completed, baseline/geometry/build/test/quality evidence in done/task_01.md and validation.md.
- [x] T02: completed; fresh six-mode mouse/focus, shadow scaling, hide/show, graceful exit and task review recorded.
- [ ] T03: in progress; transform extraction and pixel-extent correction built and unit-tested (full suite 1,082/1,082 on 2026-10-09), 50% primary checks partial, right-boundary desktop reproduction pending. Desktop unavailable at the human's request; see task_03.md/validation.md.

## Problems and solutions

- T01's first undersized-host fixture returned the correct pre-layout null because its other dimension was zero; the fixture now arranges both dimensions before expecting content rejection. Final targeted run passes.
- Baseline corner/shadow clicks did not reach the disposable editor, while clear outside clicks did. T02 must verify the approved two-surface mechanism against this measured behavior.
- Read-only T01 risk check requested body-origin containment and empty-content evidence; final assertions cover them and segment mutation rejection. No production defect or quality reservation remained.
- T02 uses Dispatcher.InvokeAsync(Action, priority) to resolve the initial overloaded BeginInvoke compile error. Current App/test builds and targeted MTP pass.
- T02 six-mode primary outside-click/focus smoke and hide/show passed; context reset interrupted graceful Exit/task review. Task stays at root. Shadow blur/depth scale deserves a focused check before completion.
- T02 resumed: transforming the complete shadow source preserves original effect scale. Grid layout clipped the unscaled source at 80%; a Canvas measures it independently before transformation, then the shared envelope mask clips the final effect. Fresh six-mode input/visual smoke and graceful disposal pass. Earlier partial-state evidence is retained above; T02 is now complete.
