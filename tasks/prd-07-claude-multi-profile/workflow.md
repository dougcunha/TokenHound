# Workflow decisions and evidence

## Baseline

- Git base: `3dc0c4e0a83a18149e5bba4524cf84606955de18`.
- The worktree was clean before this feature folder was created on 2026-09-23.
- This is one feature with one primary outcome: support multi-profile discovery and simultaneous display of all Claude Code accounts in TokenHound.
- The coordinator session identifier is `47459d3f-75de-474e-8a65-5aa9b2448198`.

## Decisions

### DEC-01 — Requested feature and constraints

- Source: user request on 2026-09-23 ("Tenho 2 contas do claude code. Qual delas o tokenhound vai exibir? E como eu poderia fazer para exibir todas? ... /sdd-orchestrate-flow sim, implemente no fluxo de sdd, com prd e tech specs").
- Decision: implement full multi-profile discovery and display for Claude Code accounts in TokenHound, following `docs/specs/03-PROVIDER-CLAUDE-CODE.md`.
- Scope: product intent and multi-profile integration; does not yet approve PRD or TechSpec until human gates HIL 1 and HIL 2.

### DEC-02 — HIL 1 product approval

- Source: user response to HIL 1 on 2026-09-23: "Aprova, mas confirma que o PRD define que tem que exibir a identificação da conta no popup do hud para que o usuario consiga distinguir".
- Decision: approve `prd.md`, with explicit requirement `FR-08` guaranteeing clear account distinction in the HUD details flyout/popup header.
- Approved SHA-256: `fc90d8fe3a1162808d2508411eb6425b4cb22450082963c8e2f14d2525a83c97`.
- Scope: the product contract in `prd.md`. Technical specification and execution tasks require HIL 2.

### DEC-03 — Continue in this session

- Source: user response `(Recommended) Continuar nesta sessão (gerar TechSpec e plano de tarefas)` to the session pause question on 2026-09-23.
- Decision: proceed directly to TechSpec creation (`sdd-create-techspec`) and task planning (`sdd-plan-tasks`) in this coordinator session.

### DEC-11 — HIL 2 technical approval

- Source: user response `(Recommended) Aprovar e autorizar implementação das tarefas T01 a T03` to the HIL 2 question on 2026-09-23.
- Decision: approve `techspec.md`, `tasks.md`, `task_01.md`, `task_02.md`, and `task_03.md`; authorize implementation and corrections within those exact contracts.
- Approved SHA-256:
  - `techspec.md`: `7e217a4e4aa715fd918c2609112e3f7bc4b4c4489b389ebeeded538c0fe7505f`
  - `tasks.md`: `f673f29b779210b9de92dcdc9e37dd54477bdb8d4b949f61c4440b4dc3a02e75`
  - `task_01.md`: `03def325468a07014c339eae851ab309e1a66be9eb63b5e83c0337232b047b7c`
  - `task_02.md`: `3eb39342bbbc46c8fd1e2cfc6f65b785cdc93eb517a012c44a15ce53bae5e7aa`
  - `task_03.md`: `951990333932b21e7427fdb4c596832b24044908af81d7bd84da913dc67a2c58`
- Scope: execution of T01, T02, and T03 in dependency order.

### DEC-12 — Continue in this session for T01

- Source: user response `(Recommended) Continuar nesta sessão e executar a primeira tarefa (T01)` to the session pause question on 2026-09-23.
- Decision: execute T01 in this coordinator session.

### DEC-13 — Continue in this session for T02

- Source: user response `(Recommended) Continuar nesta sessão e executar a tarefa T02` to the session pause question on 2026-09-23.
- Decision: execute T02 in this coordinator session.

### DEC-14 — Continue in this session for T03

- Source: user response `(Recommended) Continuar nesta sessão e executar a tarefa final (T03)` to the session pause question on 2026-09-23.
- Decision: execute T03 in this coordinator session.

### DEC-15 — End session for independent code review

- Source: user response `(Recommended) Salvar snapshot e encerrar esta sessão (Regra de independência para revisão por sessão independente)` on 2026-09-23.
- Decision: write `context-snapshot.md` and pause this session so that `sdd-review-code` can be executed in an independent session that did not author the implementation code.

## Recovery events

### REC-01 — Rejected review and original task reconciliation

- Source: independent `codereview_01/codereview.md` on 2026-09-23, literal status `REJECTED`.
- The review found incomplete active-profile qualification, mock fallback, positive session evidence, and startup timing evidence, plus inconsistent completed-task placement.
- The DAG owner reopened T01 through T03 in `tasks.md` while preserving their original handoffs and root locations. `CR-05` remains open until their corrected evidence is reconciled, the files move to `done/`, and the manifest links and states match.
- Correction tasks: `codereview_01/task_04.md` covers CR-01; `task_05.md` covers CR-02; `task_06.md` covers CR-03; `task_07.md` covers CR-04 after T04. CR-05 uses original T01 through T03 rather than duplicating their contracts.
- DEC-11 authorizes these corrections within the approved PRD and TechSpec; no new product scope or external action is planned.

### REC-02 — Correction T04 completed

- T04 in `codereview_01/done/task_04.md` corrected CR-01 by parsing credential contents through the read-only shared reader before qualifying a profile.
- The focused profile suite passed 23 tests, the broader Claude suite passed 68 tests, and the current source and test files remain below 300 lines. The task handoff records the commands and an initial test assertion correction.
- Original T01 remains reopened pending T07 timing evidence and final DAG reconciliation.

### REC-03 — Corrections T05 and T06 completed

- T05 in `codereview_01/done/task_05.md` repaired CR-02 by applying the all-profile mock fallback decision to snapshot events. The solution build passed and 13 focused view-model tests passed.
- T06 in `codereview_01/done/task_06.md` repaired CR-03 with a live isolated session fixture and provider-scoped `UsageStore` activity event. The solution build passed, 68 Claude tests passed, and the new focused test passed independently.
- T07 remains pending. Original T01-T03 stay reopened until their corrected evidence and task locations are reconciled.

### REC-04 — T07 performance evidence requires an exception decision

- `codereview_01/task_07.md` records additional Release measurements on a controlled three- and ten-profile SSD fixture. Ten-profile first calls took 25.010–69.449 ms; warm p95 was 5.617–8.999 ms, with isolated samples above 25 ms. A three-profile first call took 25.576 ms.
- The approved `NFR-03` says startup enumeration must finish in less than 25 ms under standard SSD conditions, without defining whether first-call runtime/JIT and scheduling time count or how repeated measurements are summarized. The observed cold calls do not prove that bound.
- T07 stays pending at the correction root. An exception HIL must choose whether to amend the contract to a defined warm-call percentile with the cold-call limitation disclosed, or retain a strict first-call bound and authorize further investigation. No contract change is assumed before the human decision.

### DEC-16 — End correction session after T04

- Source: user response "Snapshot and end session" to the required session pause after T04 on 2026-09-23.
- Decision: preserve the active context snapshot and end this session. The next session resumes the SDD flow at correction T05; this session starts no further correction task.

### DEC-17 — Continue correction session after T05

- Source: user response "continue" on 2026-09-23 after T05 implementation and validation.
- Decision: continue this coordinator session with the next eligible correction task, T06, after recording T05 and the checkpoint.

### DEC-18 — Save snapshot and end after T06

- Source: user response "Save snapshot and end session" to the required session pause after T06 on 2026-09-23.
- Decision: update `context-snapshot.md` and pause this session. The next session resumes the SDD flow at correction T07; this session starts no further correction task.

### DEC-19 — Exception HIL NFR-03 contract amendment

- Source: user response to Exception HIL on 2026-09-23: "(Recommended) Amend NFR-03 to warm p95 < 25 ms with cold-call overhead documented, accept T07 benchmark evidence, and proceed with T01-T03 reconciliation".
- Decision: amend NFR-03 in `prd.md` so that profile discovery startup performance requires warm p95 < 25 ms on standard SSD conditions, with cold first-call overhead (~25–70 ms) documented due to runtime/JIT and file system initialization. Accept existing benchmark measurements and approve T07 completion.
- Scope: performance contract NFR-03 and resolution of task T07 / CR-04.

### DEC-20 — Continue in this session to finalize corrections

- Source: user response to session pause question on 2026-09-23: "(Recommended) Continue in this session".
- Decision: continue in this coordinator session to complete T07, reconcile original tasks T01–T03 under CR-05, update the manifest, and prepare state for independent re-review.

### REC-05 — Correction T07 completed

- `codereview_01/done/task_07.md` resolves CR-04 with reproducible benchmark evidence on the controlled 10-profile Release fixture: `first=24.889ms; median=1.518ms; p95=2.499ms; p99=4.326ms; max=22.345ms`.
- NFR-03 was amended under DEC-19; all 1,000 warm benchmark calls were under 25 ms.
- T07 was approved and moved to `codereview_01/done/task_07.md`.

### REC-06 — Original tasks T01–T03 reconciled and CR-05 closed

- The DAG owner reconciled the evidence of all original obligations against the completed correction tasks:
  - T01: Active credential qualification verified with T04; discovery latency verified with T07. All 23 discovery tests passing.
  - T02: Live session monitor activity event verified with T06. All 68 Claude tests passing.
  - T03: All-profile mock fallback verified with T05. 13 view-model tests and 14 catalog tests passing.
- Original task files were moved to `tasks/prd-07-claude-multi-profile/done/` (`done/task_01.md`, `done/task_02.md`, `done/task_03.md`).
- `tasks.md` was updated with `done/` links and verified states. CR-05 is closed.
- All correction tasks T04–T07 are complete in `codereview_01/done/`. The correction round is complete.

### REC-07 — Independent re-review codereview_02 completed with APPROVED status

- Source: independent `codereview_02/codereview.md` on 2026-09-23, literal status `APPROVED`.
- The re-review verified that findings CR-01 through CR-05 are resolved:
  - CR-01: Profile qualification uses read-only parsing of credentials; 23 discovery tests pass.
  - CR-02: `NotchViewModel` routes snapshot fallback through all-profile check; 13 view-model tests pass.
  - CR-03: `ClaudeMultiProfileActivityTests` proves isolated session activity attribution to `claude-work`.
  - CR-04: Controlled 10-profile benchmark confirms warm p95 = 2.444 ms (< 25 ms), fulfilling amended NFR-03 under DEC-19.
  - CR-05: Original tasks T01–T03 verified in `done/` with manifest links intact.
- Quality profile scans passed with zero new hits. Solution build passed (0 errors, 0 warnings); full test suite passed (861 tests: 91 Core, 770 Infrastructure).
- Step 5 closes; flow proceeds to Step 6 (HIL 3 Acceptance).

### DEC-21 — HIL 3 human acceptance

- Source: user response to HIL 3 question on 2026-09-23: "(Recommended) Aceitar e aprovar a entrega final da funcionalidade (HIL 3)".
- Decision: approve and accept the final delivery of Claude Code multi-profile support. All obligations, automated tests (861 passing), benchmark timings (warm p95 = 2.444 ms), and code review approvals are accepted.
- Scope: full feature delivery `prd-07-claude-multi-profile`.

### DEC-22 — Finalize in this session

- Source: user response to session pause question on 2026-09-23: "(Recommended) Continuar nesta sessão para concluir o fluxo e marcar como completed".
- Decision: finalize the SDD flow in this coordinator session, set checkpoint phase and status to `completed`, and mark context snapshot as `closed`.

