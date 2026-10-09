# Workflow: HUD Edge Geometry and Contour Hit Testing

## Feature context

- Feature: `prd-16-hud-geometry-hit-testing`.
- Workspace: `D:/MyProjects/TokenHound`.
- Git base: `6f2e92b0f4cc40c5cde1326149f000768fe3a781` (v0.1.13).
- Coordinator: current Codex session; no externally supplied session ID.
- Existing feature artifacts: none at initialization.
- Worktree baseline: modified `.agents/settings.json`, `README.md`, `docs/ROADMAP.md`, and `tasks/triage-log.jsonl`; untracked `.agents/hooks/`, `.agents/settings.local.json`, `.agents/skills/chat-clean/`, `.codex/`, `.context-brake/`, and `context-brake.config.json`.
- README/roadmap synchronization and the triage entry belong to the preceding documentation and triage work. Local agent configuration and untracked tooling are unrelated.
- One primary outcome: the visible HUD contour and interactive area agree for all existing docking modes without focus or placement regressions.

## Decisions

### DEC-01: HIL 0, proceed with sdd-full

- Date: 2026-10-08.
- Decision: follow the recommended `sdd-full` path.
- Scope: process selection for this feature; HIL 1, HIL 2, visual acceptance, independent review, and HIL 3 remain required.
- Human text: "continue".
- Provenance: user continuation after the pending HIL 0 question offered `sdd-full` first and explained the deciding risks.
- Interpretation: continue with the recommended process. This does not approve an unwritten PRD, technical plan, implementation, commit, push, or release.
- Evidence: `tasks/triage-log.jsonl`, entry dated 2026-10-08 for HUD edge geometry and contour hit testing.
- Deciding signals: S2 (HUD input/focus invariants), S5 (product and hit-testing decisions), and S6 (six observable behavior groups).

### DEC-02: HIL 1, approve the product contract

- Date: 2026-10-08.
- Human text: "Pode aprovar e continuar".
- Decision: approve the presented PRD and PD-01 through PD-04; continue technical design and task planning in this session.
- Presented PRD SHA-256: `3d7d4f27cfae5305309e578dd40dbd0e8d6a5f379654cefb2e255238b27302b3`.
- Administrative update: mark the PRD approval checkbox; no product requirement changed. The checkpoint records the resulting approved file hash.
- Scope: product approval and planning continuation. HIL 2 is still required before implementation; this does not authorize publishing or a release.
- Provenance: explicit chat approval after the user reported that tool questions did not appear. An accepted tool call was not treated as a human answer.

## Gates

### DEC-03: HIL 2, approve the technical plan and implementation

- Date: 2026-10-08.
- Human text: "Sim, pode implementar".
- Decision: approve the presented TechSpec and serial T01 -> T02 -> T03 plan; continue implementation in this resumed session.
- Scope: implementation, validations, and corrections within the approved contracts. Visual check, independent review, and HIL 3 remain required.
- Provenance: explicit chat response to the HIL 2 plan and session choice after all artifact hashes were verified.
- Approved artifact hashes: the five entries preserved in checkpoint generation 5's `pending_hil.artifact_sha256`; copied into `approved_sources` on recording this decision.

| Gate | State | Evidence |
| --- | --- | --- |
| HIL 0 | Decided | DEC-01 |
| HIL 1 | Approved | DEC-02; prd.md; PD-01 through PD-04 |
| HIL 2 | Approved | DEC-03; techspec.md and three-task plan |
| Visual check | Pending implementation | Manual acceptance will be defined in the TechSpec |
| Independent review | Pending implementation | Delegation required by the flow |
| HIL 3 | Pending review and validation | No delivery acceptance recorded |

## HIL 1 material

- Artifact: `prd.md`.
- SHA-256: `3d7d4f27cfae5305309e578dd40dbd0e8d6a5f379654cefb2e255238b27302b3`.
- Coverage: 3 outcomes, 5 stories, 11 functional requirements, 5 non-functional requirements, and 4 proposed product decisions.
- Proposed choices: retain current Windows colors/content; fully rounded horizontal Free capsule; bound/omit unavailable corner continuations; exclude shadow-only pixels from hit testing.
- Evidence checks: unique requirement/decision IDs and local links passed. No build, tests, native input baseline measurement, or visual implementation was performed.
- Decision: approved by DEC-02. Approval was explicitly given after HIL 0.
- Next authorized stage after product approval: `sdd-create-techspec`, then task planning and HIL 2.

## Events

- 2026-10-09: headless T03 validation, no desktop interaction. App/test Release builds passed with zero warnings/errors; full Infrastructure suite passed 1,082/1,082 including the five PixelExtent cases. The human explicitly asked to commit the in-progress feature and update documentation, superseding the DEC-03 no-commit constraint for this commit only (no push/release). Refreshed tasks.md/task_03.md checkpoint hashes for administrative state/handoff changes only; contracts unchanged. ROADMAP status synchronized. Right-boundary desktop reproduction and remaining T03 gates stay pending.
- 2026-10-08: T03 partial checkpoint. Extracted pure transform conversion; builds passed, full Infrastructure run had 1,076 passes plus one incorrect new fixture, corrected and reran 11 transform tests successfully. Six-mode 50%/150%-DPI visual exploration found one-pixel right docking overflow. Confirmed WPF SizeToContent ceiling rule; wrote PixelExtent helper, two placement expressions and five tests, not yet built/validated. ContextBrake reached RED. Human text: "O que aconteceu? Não terminou os testes? Vou precisar usar o computador." Closed and confirmed validation processes 18084/33928, restored installed HUD 23240. Preserve partial evidence; no further desktop interactions until human confirms availability. No visual/final approval or independent review occurred.
- 2026-10-08: resumed T03 from handoff 20261008T191951.071Z. All six approved source hashes matched; only installed process 23240 is running. DEC-03/04 reused. Current controller already synchronizes ancestor transforms and owner/shadow DPI with unchanged-frame suppression. Extract the numeric conversion into a pure App record for TC-02 without changing WPF placement or persistence contracts. Three-monitor inventory is unchanged. Human physical-disconnection and visual evidence remain pending.
- 2026-10-08: resumed T02 from handoff 20261008T185039.324Z; all six source hashes matched. Corrected shadow effect scaling by transforming the whole Path and replacing its Grid host with a Canvas after reproducing small-size clipping. Final App build passed; unchanged linked-source 23 contour/12 style results retained. Fresh six-mode primary click/focus, side-to-Free, hide/show and graceful Exit/disposal passed. Coordinator task review found no new block/reservation; T02 moved to done/. Full T03 acceptance, human visuals, independent review and HIL 3 remain pending.
- 2026-10-08: ContextBrake reached RED at the T02 boundary. Final repository process 35448 exited gracefully, disposable target 19216 closed, installed process 23240 restored. Graph build completed. Save snapshot/checkpoint with T03 next; no T03 writing started and no approval repeated.
- 2026-10-08: resumed T02 from handoff 20261008T181618.192Z; all approved hashes matched, Git/worktree matched, DEC-03/04 reused. Added rendering/controller/native integration and style tests. Builds and 23 contour + 12 style tests passed. Six-mode primary cross-process outside-click/focus smoke, side-to-Free drag and hide/show were measured. T02 remains at root for graceful Exit/scale-effect check/final task review. No T03 writing or final acceptance occurred.
- 2026-10-08: ContextBrake reached 66% RED during T02. Stabilized partial state, preserved native/click evidence, stopped only repository validation process 22692 and disposable target 20252, restored installed process 23240. Final formatting-only App rebuild passed. Save snapshot/checkpoint/handoff with T02 as next unit; no approval is repeated.

### DEC-04: Anti Slop during implementation

- Date: 2026-10-08.
- Human text: "Apply while implementing (Recommended)".
- Decision: apply Anti Slop design checks during this feature's implementation. No global preference was written.
- Provenance: explicit answer to the asynchronous mode question in the resumed implementation session. Approved PRD PD-01..04 remains the design direction; this does not expand product scope.

- 2026-10-08: resumed T01 under DEC-03 after all six approved source hashes matched; newer checkpoint superseded the stale planning snapshot. Captured baseline before pure production edits and preserved unrelated worktree changes.
- 2026-10-08: completed T01 canonical geometry, linked tests, desktop baseline, and scoped task review. Final native MTP: 19 passed with minimum expected tests 1; both Release projects build with zero warnings/errors. Read-only risk check reported no production defect; body/empty/immutability test gaps were fixed. No final feature acceptance or visible integration is claimed.
- 2026-10-08: moved T01 to done/ and updated its manifest/handoff. Approved artifact hashes/paths are refreshed only for these administrative execution records; the approved task contracts and scope remain unchanged.
- 2026-10-08: ContextBrake measured 66%, RED. No T02 implementation started. Persist next action T02 with HIL 2 and Anti Slop selection retained; save the snapshot and context handoff before the required session reset.

- 2026-10-08: verified the worktree and absence of a matching feature folder; selected the next feature number, 16.
- 2026-10-08: loaded the flow state, continuity, delegation, PRD, and snapshot instructions. Direct exploration is sufficient for the product stage; no explorer was started.
- 2026-10-08: recorded DEC-01 and opened product-stage state. No production code, build, tests, or application launch performed for this feature.

- 2026-10-08: wrote and verified the PRD. Prepared HIL 1 material; product and technical approvals remain pending.
- 2026-10-08: wrote the planning snapshot before the mandatory HIL stop. No explorer or background process is active.
- 2026-10-08: recorded explicit HIL 1 approval and resumed technical planning; production code remains unchanged.
- 2026-10-08: resumed from ContextBrake handoff `20261008T153953.882Z.md` in a fresh coordinator context. Git HEAD and the worktree match the planning snapshot; approved PRD and all five HIL 2 artifact hashes match. No competing coordinator, explorer, or background process is active. HIL 2 remains unanswered; the resume invocation does not approve implementation. Re-present the existing plan and session choice in chat because the prior question UI was not visible.

## HIL 2 material

- Solution: shared contour, main zero-alpha routing, owned non-activating WS_EX_TRANSPARENT shadow-only companion; Microsoft sources and trade-offs in techspec.md.
- Artifacts: techspec.md, tasks.md, task_01.md through task_03.md; pending hashes in checkpoint.json.
- Serial T01 baseline/geometry -> T02 rendering/input -> T03 placement/DPI/acceptance. No separate preparatory refactor recommended.
- Human decision pending: approve this concrete technical plan and execution or request corrections. Session choice is separate; recommend a new session because ContextBrake reached RED/CRITICAL.
- No feature code/build/test/baseline interactions performed. Display inventory confirms three mixed-DPI monitors; physical disconnection, visual approval and independent review remain pending.
- 2026-10-08: refreshed tasks.md/task_02.md checkpoint hashes for administrative execution/handoff changes only; product/technical/task contracts unchanged. Graft build refreshed the graph. Verified snapshot source paths and checkpoint generation 12, safe to stop.
