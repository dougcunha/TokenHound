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
| Visual check | Approved 2026-10-09 | Human text "Aprovado" after the collage `t03-visual-gate-20261009.png`; see Events |
| Independent review | APPROVED WITH RESERVATIONS (round 3) | `codereview_01` and `codereview_02` REJECTED on evidence and records only; `codereview_03/codereview.md` approved with reservations; DEC-06 |
| HIL 3 | Accepted 2026-10-09 | DEC-07; residual risk DEC-05 |

### DEC-05: Exception HIL, accept physical display disconnection as residual risk

- Date: 2026-10-09.
- Human text: selected "Aceitar como risco residual (Recomendado)" for CR-01 (codereview_01/CR-01, codereview_02/CR-01). Earlier answer on the same item: "Aceitar sem esse teste".
- Decision: TechSpec manual acceptance step 6 (physical disconnection and reconnection of the preferred display) is waived for this delivery and accepted as residual risk.
- Reason: the preferred-display fallback and return logic belongs to PRD 14 and is unchanged by this feature (no diff in `src/TokenHound.Infrastructure` or display resolution); it is covered by the existing display resolver unit tests; this feature's contour and companion follow placement changes, verified across three displays, DPI transitions, and restart.
- Scope: FR-08 is accepted with this residual risk. HIL 3 still applies. No product or code change.
- Provenance: AskUserQuestion answer at the exception HIL after codereview_02.

### DEC-06: Reservations HIL, finalize with accepted reservations

- Date: 2026-10-09.
- Human text: selected "Finalizar e aceitar (Recomendado)".
- Decision: close the review cycle on `codereview_03` (APPROVED WITH RESERVATIONS). Accepted open items: QA-07 `HudShadowInterop.Bounds` record; style (blank line before `return` in `HudContourLayout.cs:157-158`, fully qualified `HudDockMode` in `HudContourGeometry.cs:41`); `HudContourDecorator.ArrangeOverride` double layout build and per-render Pen allocation. Records-precision items synchronized at closing.
- Provenance: AskUserQuestion answer at the reservations HIL.

### DEC-07: HIL 3, accept the delivery

- Date: 2026-10-09.
- Human text: selected "Aceitar a entrega".
- Decision: accept the PRD 16 delivery at commit `2bc32ef` plus the recorded documentation and evidence. Residual risk per DEC-05; open improvements per DEC-06.
- Scope: feature completed. Commit, push, and release are not authorized by this decision.
- Provenance: AskUserQuestion answer at HIL 3, together with DEC-06.

## HIL 1 material

- Artifact: `prd.md`.
- SHA-256: `3d7d4f27cfae5305309e578dd40dbd0e8d6a5f379654cefb2e255238b27302b3`.
- Coverage: 3 outcomes, 5 stories, 11 functional requirements, 5 non-functional requirements, and 4 proposed product decisions.
- Proposed choices: retain current Windows colors/content; fully rounded horizontal Free capsule; bound/omit unavailable corner continuations; exclude shadow-only pixels from hit testing.
- Evidence checks: unique requirement/decision IDs and local links passed. No build, tests, native input baseline measurement, or visual implementation was performed.
- Decision: approved by DEC-02. Approval was explicitly given after HIL 0.
- Next authorized stage after product approval: `sdd-create-techspec`, then task planning and HIL 2.


## Events

- 2026-10-09: DEC-06 and DEC-07 recorded; records synchronized; checkpoint completed and snapshot closed; ROADMAP and README updated.
- 2026-10-09: round-2 re-review codereview_03/codereview.md: APPROVED WITH RESERVATIONS (delegated reviewer), no blocks, no code defect. Worktree matched the pre-review record plus the report folder. Reservations HIL and HIL 3 presented together.
- 2026-10-09: DEC-05 recorded (CR-01 accepted as residual risk; TechSpec step 6 waived for this delivery). TechSpec manual script annotated. Next: round-2 re-review in codereview_03.
- 2026-10-09: re-review `codereview_02/codereview.md` REJECTED (delegated reviewer): CR-01 persistent (physical disconnection), CR-02 remaining stale records and no Anti Slop gate record. Round 2: Anti Slop delivery gate run and recorded PASS in `anti-slop-delivery-gate.md` (DEC-04 closed); stale lines in validation.md, done/task_03.md and tasks.md corrected. CR-01 taken to an exception HIL.
- 2026-10-09: correction round 1 executed: task_01 (CR-02 desktop evidence in validation.md) and task_02 (CR-03 records). Next: re-review by a new delegated reviewer in codereview_02.
- 2026-10-09: delegated review received: `codereview_01/codereview.md`, status REJECTED, Execution: delegated reviewer. Worktree after the review matched the pre-review record except the expected report folder. No code defect; findings are evidence and records only. Correction round 1 planned in codereview_01: task_01 (CR-02 desktop evidence), task_02 (CR-03 records). CR-01 (physical disconnection) is pending for an explicit HIL 3 decision; no task.
- 2026-10-09: visual gate approved. Human text: "Aprovado", after reviewing the collage `t03-visual-gate-20261009.png` sent in chat. T03 moved to done/. Next: delegated independent review in `codereview_01/` against base `6f2e92b`.
- 2026-10-09: human decision on TechSpec step 6, selected "Aceitar sem esse teste": physical disconnection and reconnection of the preferred display is an accepted open item, covered only by the display resolver unit tests; present it at HIL 3. Visual gate still pending: the human is away from the computer and asked for the screenshots in chat; collage `t03-visual-gate-20261009.png` sent.
- 2026-10-09: T03 coordinator desktop acceptance recorded in validation.md. ARCHITECTURE click-through statement synchronized; quality checks clean; graft rebuilt. Test processes stopped, temporary baseline worktree removed, user settings restored (hash verified). Next: human visual gate and physical disconnection evidence, then delegated review.
- 2026-10-09: human released the desktop ("Sim. Ninguém está usando agora."). ENV-T03-DESKTOP cleared. User settings backed up to `%LOCALAPPDATA%/TokenHound/settings.json.before-prd16-t03-20261009.bak` (TopCenter on Display 1, 80%) for restoration after checks. Repository Release PID 26392 launched at Right edge 50% on primary DISPLAY2 (150% DPI): owner and companion bounds (1860,450)-(1920,630), Right=1920 within the work area; the PixelExtent ceiling fix is confirmed on the desktop. Evidence `t03-native-primary-50-right-edge-after-fix.json`. Installed PID 8068 preserved.
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
