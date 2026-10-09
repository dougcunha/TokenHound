# Workflow: HUD Backdrop Effects

## Feature context

- Feature: `prd-17-hud-backdrop-effects`.
- Workspace: `D:/MyProjects/TokenHound`.
- Git base: `2bc32ef80e0e3aff4070bcbd54a45be73ac23855` (PRD 16 in-progress commit).
- Coordinator: current Claude Code session; no externally supplied session ID.
- Existing feature artifacts: none at initialization.
- Worktree baseline: modified `.agents/settings.json`; untracked `.agents/hooks/`, `.agents/settings.local.json`, `.agents/skills/chat-clean/`, `.codex/`, `.context-brake/`, and `context-brake.config.json`. All are unrelated local tooling.
- One primary outcome: the HUD capsule shows a Windows 11 Mica/Acrylic material instead of the flat fill, without losing click-through outside the contour, non-activation, or readability.
- Dependency: PRD 16 (`prd-16-hud-geometry-hit-testing`) owns the contour, the shadow companion, and the layered-window input path this feature builds on. Its T03 acceptance is still pending.

## Decisions

### DEC-01: HIL 0, proceed with sdd-lean

- Date: 2026-10-09.
- Decision: follow `sdd-lean` instead of the recommended `sdd-full`.
- Scope: short PRD (problem, FR with acceptance, out of scope); HIL 1 and HIL 2 merged into one decision; a plan of one or two tasks; independent review, visual check, and HIL 3 kept.
- Human text: selected "sdd-lean" and "PRD agora, código depois (Recomendado)".
- Sequencing: write the PRD (and the TechSpec and plan for the merged gate) now; implementation starts only after PRD 16 is accepted, because both features change the same HUD composition surface.
- Evidence: `tasks/triage-log.jsonl`, entry dated 2026-10-09 for HUD backdrop effects. Rubric `sdd-full` by S2, S5, S6.
- Interpretation: this does not approve an unwritten PRD, technical plan, implementation, commit, push, or release.

### DEC-02: HIL 1+2 (merged), approve product, technical plan, and execution

- Date: 2026-10-09.
- Human text: selected "Aprovar" for the merged HIL 1+2, and "Continuar nesta sessão" for the session.
- Decision: approve the PRD (PD-01..04), the TechSpec (DEC-01..08), and the two-task plan (T01 spike, T02 implementation), including corrections within these contracts.
- Approved SHA-256: `prd.md` `76ca309ede01bb2af876b9343c0b5b9b946e5a4efeb41e4e1d7924e38ed53d76`; `techspec.md` `0c528d06e3a06f11d5f6eb301c4ae48f31a51a61599c7a7700fccd54239ff450`; `tasks.md` `999f4d2cc7fbe0992c714b5614e433d8cc4624d83ea98021a39922cf3286c619`; `task_01.md` `961516440b842ae34c8ca5a7d7ee86b9e9d02c5c3f2f217b364d01a50bebe918`; `task_02.md` `0b15f3644ec9ed88e157562eb1de8426b8458852d42dcf754717eb62c787e407`.
- Scope: execution remains gated by PRD 16 acceptance (DEP-PRD16) and by the user releasing the desktop. The PD-04 trade-off stays an exception HIL inside T01. Commit, push, and release are not authorized.
- Provenance: AskUserQuestion answer after the checkpoint was saved with `pending_hil`.

## Events

- 2026-10-09: state opened after HIL 0. Product questions that change acceptance are asked before drafting the short PRD.
- 2026-10-09: product answers through the question tool, recorded as PRD input (not HIL 1 approval). Human selections, verbatim: material "Acrylic (Recomendado)"; setting "Liga/desliga, ligado por padrão (Recomendado)"; fallback "Volta ao preenchimento atual (Recomendado)"; trade-off "Decidir com a evidência do spike (Recomendado)". Mapped to PRD PD-01 through PD-04.
- 2026-10-09: short PRD drafted. Because the spike needs the desktop and PRD 16 acceptance, the merged HIL 1+2 approves the candidate mechanisms and a two-task plan (T01 spike, T02 implementation); the PD-04 trade-off is decided at an exception HIL inside T01 with screenshots.
- 2026-10-09: headless research (read-only subagent, no desktop) ranked three candidate mechanisms; TechSpec, tasks.md, task_01.md, and task_02.md drafted. Merged HIL 1+2 presented: approve PRD, TechSpec, and the T01 spike / T02 implementation plan. Approval does not lift DEP-PRD16 or the desktop prerequisite.
- 2026-10-09: merged HIL 1+2 approved (DEC-02); session continues. PRD 17 waits for PRD 16 acceptance.
- 2026-10-09: PRD 16 accepted (its DEC-07); DEP-PRD16 cleared. T01 still needs the desktop released for automation.

### DEC-03: Desktop released for T01 automation

- Date: 2026-10-09.
- Human text: selected "Liberar agora (Recomendado)" when asked to release the desktop for Windows MCP automation (prototype windows, clicks, drags, screenshots on three monitors).
- Decision: clears blocker ENV-DESKTOP; T01 starts in this session.
- Scope: desktop automation for T01 only; does not authorize production code outside T02, commit, push, or release.
- Provenance: AskUserQuestion answer on resume; checkpoint generation 5 recorded the blocker.

### DEC-04: Enable Windows transparency effects for the spike, then restore

- Date: 2026-10-09.
- Context: T01.1 found `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\EnableTransparency = 0`; every Acrylic candidate renders solid with it off, so no DEC-03 verdict is valid until it is on.
- Human text: selected "Eu ligo e restauro (Recomendado)".
- Decision: the coordinator sets `EnableTransparency = 1` (registry plus `WM_SETTINGCHANGE` `ImmersiveColorSet`), confirms it visually, and restores the original value `0` at T01.5.
- Product note (open thread, not a gate): with FR-05 as written, this user's machine shows the solid fallback by default. Raise at the T01 verdict / HIL 3.

## Events (T01)

- 2026-10-09: T01 executed (DEC-03 desktop release, DEC-04 transparency toggle). Candidate A passes DEC-03; variants AL/AR/ALR pass cross-process outside-click checks; B and C not evaluated. Transparency restored to 0. Verdict: `validation.md#t01-spike-2026-10-09`; T01 moved to `done/`. `tasks.md` State/links and `done/task_01.md` Work/Handoff changed as execution records; their approved contract text is unchanged.
- 2026-10-09: incident during T01: DPI-unaware scripted mouse input landed in the user's Chrome page twice (text selection only). Fixed and recorded in `tasks.md#problems-and-solutions`; a Chrome screenshot was deleted from the evidence.
- 2026-10-09: exception HIL opened for T02 contract details (DEC-05 pending): projection route (DEC-08) and clip route (DEC-02/PD-04), plus CMP-03 raw HWND and DEC-05 z-order amendments.

### DEC-05: T01 exception HIL — hand-written WinRT interop and contour region clip

- Date: 2026-10-09.
- Human text: selected "Interop à mão (Recomendado)", "Region do contorno (Recomendado)", and "Continuar nesta sessão (Recomendado)".
- Decision: T02 implements candidate A without the versioned TFM: the composition calls go through hand-written WinRT ABI interop (no `Microsoft.Windows.SDK.NET` projection), proven first in the scratch spike; if that proof fails, T02 stops and returns here with evidence. The material is clipped by a window region built from the flattened PRD 16 contour (no D2D, no Win2D).
- Scope: amends TechSpec DEC-02 (variant), DEC-05 (z-order), DEC-08 (no TFM change), CMP-02/CMP-03 (raw HWND companion), and risks; amends `task_02.md` work items accordingly. Approval of those edits is this decision; it does not authorize commit, push, or release.
- Provenance: AskUserQuestion answer after checkpoint generation 8 with `pending_hil`.
- Approved SHA-256 after amendment: `techspec.md` 6a3d4ec97218ae4112702aedfa520006f1209cc1a7768865abbc10a3da35da85; `task_02.md` 34e8d51468751835092d0bc0132d6003915bba7b76333072b4afd8d54b2e426c.

### DEC-06: Desktop and Windows effect toggles authorized for T02

- Date: 2026-10-09.
- Human text: selected "Sim, alterne e restaure (Recomendado)".
- Decision: T02 may use the desktop through Windows MCP (with DPI-aware guards for scripted input) and toggle Transparency effects and energy saver for the interop proof and TC-05..08; original values (transparency 0, energy saver state as found) are restored at the end of T02.

### DEC-07: Tint 93% and NFR-02 aligned with WCAG

- Date: 2026-10-09.
- Context: at 70% tint over a pure white background, ring arcs fell to 1.36-3.06:1. The solid HUD already had grey (3.66:1) and purple (4.18:1) arcs below the original 4.5:1 rule.
- Human text: selected "93% + NFR-02 WCAG (Recomendado)" and "Continuar nesta sessão (Recomendado)".
- Decision: material tint alpha 0xED (93%); PRD NFR-02 now requires icons/text ≥ 4.5:1 and ring arcs ≥ 3:1 in every state over white and black; TechSpec TC-07 and the tint open item amended accordingly.

- 2026-10-09: T02 completed (done/task_02.md); 1101 tests pass. Visual check gate opened. Scope note on DEC-06: transparency stays ON and the installed app stays closed through the visual check; both are restored right after it. Session stopped at ContextBrake CRITICAL; snapshot not rewritten (stale header: covers T01, see checkpoint).

### DEC-08: Visual check approved

- Date: 2026-10-09.
- Human text: selected "Aprovado" and "Continuar nesta sessão (Recomendado)".
- Decision: the visual check of the TechSpec manual acceptance script (TC-05 remaining cases: Top left, Top right, Right edge, HUD sizes 50 % and 150 %, 150 % primary monitor) is approved by the human; no adjustment task. Next: restore the DEC-06 toggles and delegate the independent review against base 2bc32ef.
- Provenance: AskUserQuestion answer after checkpoint generation 11 with `pending_hil: visual-check`.

- 2026-10-09: delegated review `codereview_1/` received (Execution: delegated reviewer); worktree unchanged except the report. Status REJECTED (CR-01, CR-02, both Low). Correction round 1 opened under HIL 2 authorization.
- 2026-10-09: correction round 1: `codereview_1/done/task_03.md` (CR-01, QA-05 calls) and `codereview_1/done/task_04.md` (CR-02, QA-04 `Create` span) executed in the authoring session under HIL 2; build clean, 1101 tests pass. Re-review delegated to a new reviewer in `codereview_2/`.
- 2026-10-09: delegated re-review `codereview_2/` received (Execution: delegated reviewer); worktree unchanged except the report. Status APPROVED WITH RESERVATIONS; codereview_1 CR-01, CR-02 resolved. Reservations HIL opened.

### DEC-09: Reservations HIL on codereview_2

- Date: 2026-10-09.
- Human text: selected "B: método + docs" and "D: menos log no resize"; "Continuar nesta sessão (Recomendado)".
- Decision: open a correction round limited to (B) splitting `HudContourController.Synchronize(HudContourGeometry)` to ≤ 30 lines, fixing the PRD 17 row in `docs/ROADMAP.md`, and naming high contrast in the `HudBackdropSettingsCard` description; (D) logging the arrange-invalid transient transitions (Solid and the following Material) at Debug instead of Information. This amends the TechSpec Observability wording for that transient case only. Not chosen, carried to HIL 3 as accepted open items: (A) outside clicks in five docking modes, (C) `Candidate` log field and TC-02 merge test, plus the listed limitations.
- Provenance: AskUserQuestion answer after checkpoint generation 15 with `pending_hil: reservations`.
- 2026-10-09: correction round 2 (DEC-09 scope): `codereview_2/done/task_05.md` (Synchronize split, ROADMAP row, card text; README wording extension, same cause) and `codereview_2/done/task_06.md` (arrange-invalid transient logs at Debug; TechSpec Observability amended) executed; build clean, 1101 tests pass. TechSpec re-approved under DEC-09 with SHA-256 7188433eb57880df5ae7dd4c8eda592c12b1aea6d781c3c9a03dfa42d3808847. Re-review delegated to `codereview_3/`.
- 2026-10-09: delegated re-review `codereview_3/` received (Execution: delegated reviewer); worktree unchanged except the report. Status APPROVED WITH RESERVATIONS; no blocks. New optional items from T06: `StringComparison.Ordinal` vs the CLAUDE.md `OrdinalIgnoreCase` rule, and the `_transient` flag surviving same-mode frames. Reservations HIL opened.

### DEC-10: Reservations HIL on codereview_3

- Date: 2026-10-09.
- Human text: selected "Corrigir as duas (Recomendado)" and "Continuar nesta sessão (Recomendado)".
- Decision: open a correction round limited to the two codereview_3 T06 items: use `StringComparison.OrdinalIgnoreCase` for the reason comparison, and clear the transient flag on same-mode frames so only the transition right after an arrange-invalid frame logs at Debug. Re-review delegated to `codereview_4/`.
- Provenance: AskUserQuestion answer after checkpoint generation 17 with `pending_hil: reservations`.
- 2026-10-09: correction round 3 (DEC-10 scope): `codereview_3/done/task_07.md` executed; build clean, 1101 tests pass. Re-review delegated to `codereview_4/`.
- 2026-10-09: delegated re-review `codereview_4/` received (Execution: delegated reviewer); worktree unchanged except the report. Status APPROVED WITH RESERVATIONS; no blocks; T07 conformant. New optional item: `techspec.md:115` wording predates the DEC-10 narrowing. Reservations HIL and HIL 3 presented together.

### DEC-11: Reservations on codereview_4 and HIL 3 acceptance

- Date: 2026-10-09.
- Human text: selected "Ajustar o texto (Recomendado)" and "Aceitar com as pendências".
- Decision: `techspec.md` Observability wording aligned with DEC-10 (doc only, no code change, no new review); TechSpec re-approved with SHA-256 3f63a311c1b35b326ce14cbd49d8cc45315e2c4cd2d86b812675e672a38acd34. The PRD 17 delivery is accepted on the latest review `codereview_4/` (APPROVED WITH RESERVATIONS). Accepted open items: (A) FR-03/TC-05 outside clicks in Left, Right, Top left, Top right, and Free not verified (TechSpec medium risk); (C) `Candidate` log field and TC-02 merge test; limitations in `codereview_4/codereview.md#limitations-and-open-items` (FR-06 ≈ 2 s, TC-08 60 s runs, no clean-exit evidence, Windows 10 and live high contrast by unit tests only, `graft build` not run, O-04 solid fallback by default on this machine). `docs/ROADMAP.md` PRD 17 row updated to accepted. Suggested after acceptance (not run): `simplify` for the `SetWindowPos` P/Invoke in four files; ADR candidate for the hand-written WinRT ABI interop (DEC-05). No commit, push, or release is authorized by this decision.
- Provenance: AskUserQuestion answer after checkpoint generation 19 with `pending_hil: reservations + HIL 3`.
