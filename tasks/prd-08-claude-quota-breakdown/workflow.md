# Claude quota breakdown workflow

## REC-01 — Initial state

- Git base: `606426652bdf3a286d44db6eb169e9b426d5d251`.
- At flow start, `tasks/prd-08-claude-quota-breakdown/prd.md` was an untracked artifact authored in the preceding session. No code changes were present.
- The request selected this existing PRD explicitly. No earlier checkpoint, approval, TechSpec, or task plan existed in this feature folder.

## DEC-01 — Product approval

- Decision: The user approved `prd.md` at HIL 1 and asked to continue in this session.
- Scope: Conditional model or scope quotas belong in the existing Claude details; monetary extra-usage credits remain out of scope.
- Approved SHA-256: `dbf3cb45264c921cd227a9ed520afa0e80a8e8e88a9a7e279894d5ddb6a9186d`.
- Provenance: user message, "Aprovado. Pode continuar".

## REC-02 — Technical plan prepared

- `techspec.md`, `tasks.md`, `task_01.md`, and `task_02.md` were written from the approved PRD. No implementation code has been changed.
- Terrain was measured before implementation. The TechSpec records local mapper extraction in T01 and no separate preparatory refactoring.
- The two-task DAG and manual desktop acceptance script were ready for HIL 2 review. At this point, the user had not yet approved the technical plan or implementation.

## DEC-08 — Technical plan and execution approval

- Decision: The user approved the TechSpec, two-task DAG, implementation, and corrections within those contracts at HIL 2, and continued this session.
- Provenance: user message, "sim", answering the HIL 2 question after the prior "Pode continuar" session preference.
- Approved SHA-256: `techspec.md` `e0b61ce1cf0c179bbf9e2ad0bef97853dbf72490a80ec47988fac92369fcde4f`; `tasks.md` `ec6ba378fc65178544bb797cef62d95eb5758129c3f9b3007fe665203f4c4b60`; `task_01.md` `60783267de58a2155b7a2b851ee1eca413fa7e4d616169b96a79a984faf181fd`; `task_02.md` `5ed2ef8051ac091c06ac6b1b3c9a1b27a74f6e6cf531395135398f0daedfa315`.
- Scope: T01 then T02, with focused .NET tests and manual desktop acceptance. No external publication was authorized.

## REC-03 — Exception discovered before T01 code

- `LimitWindow.UsedFraction` suppresses an explicit percentage when `TotalUnits` is null. This conflicts with approved percent-only Claude windows.
- `exception-01.md` contains the proposed minimal Core contract amendment, affected tests, validation, alternatives, and impact. This requires exception HIL because DEC-08 approved a TechSpec with no Core model change.
- No implementation code had been written and T01 remained pending. DEC-08 remained valid for its original scope; the proposed amendment had not yet been approved at this point.

## DEC-10 — EX-01 approved; resume in another session

- Decision: The user approved `exception-01.md` and asked for a context snapshot to continue in another session.
- Provenance: user message, "Aprovo. Salve o snapshot para outra sessão".
- Scope: change `LimitWindow.UsedFraction` to preserve an explicit fraction without `TotalUnits`; move and update its Core tests; expand T01 validation. The product scope and T02 remain unchanged.
- Approved SHA-256: `exception-01.md` `8ead1e0aadce7b327aaf64b32254103172cc2c533360ddb706a7a7f41a3399cb`; amended `techspec.md` `a95fa257790b52d2f7e7df18c5565c125d4d9bf2ea1b35c8eb5510692f9e2e2c`; amended `tasks.md` `619daa2a43ee600775af90a93638107e7bf80a91d99245b96cdf91a0dcfcc082`; amended `task_01.md` `22fa56a150a765439bb3d11a71c43f17e5adc37919dacceb35f9c1fd5e61c57f`.
- T01 has not started. No implementation code or tests changed or ran in this session. The next session may execute T01 under DEC-08 and this exception approval.

## Authorization

- The user authorized creation of the PRD, invoked the flow for this feature, approved HIL 1 in DEC-01, and approved HIL 2 in DEC-08.

## REC-04 — Partial T01 diff adopted by resuming coordinator

- On resume (2026-09-23 19:28), checkpoint generation 6 read `active` / `safe_to_stop: false`, and nothing had been recorded as started. The worktree nevertheless held uncommitted T01 changes. Modified: `LimitWindow.cs`, `ClaudeOAuthProvider.cs`, `ClaudeUsageResponse.cs`, `ClaudeWindowDto.cs`, `DomainModelsTests.cs`, `ClaudeOAuthProviderTests.cs`. New: `ClaudeQuotaWindowMapper.cs`, `LimitWindowTests.cs`, `ClaudeQuotaProviderTests.cs`. The last write was at 19:22:42.
- The user confirmed that the authoring session is closed and that this session is now the only coordinator. Provenance: answer "Closed, take over (Recommended)".
- The partial diff is treated as unverified T01 work, not as completed evidence. This session reviews it against `task_01.md`, completes it, and validates it before any subtask is marked done.

## REC-05 — T01 completed

- T01 was reviewed, completed, and validated. It moved to `done/task_01.md` with its handoff; the manifest State and Problems and solutions are updated. After approval, `task_01.md` and `tasks.md` changed only in Work checkboxes, Handoff, links, State, and Problems and solutions. Their approved contracts, recorded under DEC-10, are unchanged.
- The full Core.Tests (92) and Infrastructure.Tests (775) suites passed on the T01 state. One MCP test expectation was updated as a consequence of the DEC-09 contract; see the manifest.
- Next: T02 under DEC-08. This session authored T01 code, so the global review must run in a different session.

## REC-06 — T02 completed; implementation closed

- T02 was implemented and validated: the App build and full Infrastructure.Tests (781) are green. It moved to `done/task_02.md`. Integrated validation: the full Core.Tests (92) passed on the T01 state, and T02 touched no Core-linked file.
- TC-07 manual HUD acceptance is pending: the Windows MCP tools were unavailable in this session.
- Independence: session `1346bbc2-2fdc-428e-ba40-42a7b65870cc` authored T01 completion and T02, so `sdd-review-code` must run in another session.

## REC-07 — Independent review issued

- Session `01WwY4xqC3VkRoMYCRqVMDjY` (not the author session) took over as coordinator on 2026-09-23 and ran `sdd-review-code` against base `6064266`.
- Report: `codereview_1/codereview.md`, status `APPROVED WITH RESERVATIONS`. It contains no findings, optional improvements OI-01 (Claude label fallback for unrecognized kinds) and OI-02 (QA-04 reservation), and TC-07 as not verifiable and carried to HIL 3.
- Independent rerun: 0-warning builds for Core.Tests, Infrastructure.Tests, and App; Core.Tests 92/92 and Infrastructure.Tests 781/781 passed.
- Next: reservations HIL.

## DEC-11 — Reservations HIL resolved

- Decision: correct `codereview_1/OI-01`; accept `codereview_1/OI-02` as an open item. Continue in this session.
- Provenance: user answer "Fix OI-01, accept OI-02 (Recommended)" and "Continue in this session (Recommended)".
- Scope: a correction round limited to OI-01 (Claude label fallback plus one row test), within the DEC-08 contracts. The re-review must run in a session that did not make the correction.

## REC-08 — Correction round 1 executed

- `codereview_1/done/task_03.md` (OI-01) was implemented and validated by session `01WwY4xqC3VkRoMYCRqVMDjY`. The full Infrastructure.Tests suite passed 782/782, and the App build is clean.
- One existing ring-test fixture was updated from `"Session"` to the canonical `five_hour`. The rationale is in the task handoff.
- Independence: this session reviewed the feature (codereview_1) and authored T03, so the re-review must run in a session other than `01WwY4xqC3VkRoMYCRqVMDjY` and `1346bbc2-…`.

## REC-09 — Resume attempt deferred for independence

- On 2026-09-23, session `01WwY4xqC3VkRoMYCRqVMDjY` was resumed after `/clear` and detected that it authored T03. The user chose to end it and run the re-review in a new session. Provenance: answer "End; resume in new session (Recommended)".
- No code, report, or task state changed.

## REC-10 — Independent re-review issued

- Session `01XDQuf2TU3GqApSXLnbnvRy`, which authored none of T01–T03, took over as the only coordinator on 2026-09-23 and ran `sdd-review-code` against base `6064266`.
- Report: `codereview_2/codereview.md`, status `APPROVED WITH RESERVATIONS`. It has no findings. `codereview_1/OI-01` is resolved, and the T03 fixture deviation is judged justified. The only reservation is `codereview_1/OI-02`, already accepted under DEC-11, so no new reservations HIL is needed.
- Independent rerun: Core.Tests, Infrastructure.Tests, and App built with 0 warnings. Core.Tests passed 92/92 and Infrastructure.Tests 782/782.
- TC-07 remains unexecuted: this session also lacks the Windows MCP `App`/`Screenshot` tools.
- Next: step 6, HIL 3.

## DEC-12 — HIL 3 acceptance

- Decision: accept the current delivery. TC-07 (live HUD manual acceptance through Windows MCP `App`/`Screenshot`) stays an open item, to run when a session has those tools. `codereview_1/OI-02` stays accepted (DEC-11). The out-of-scope `ActiveBlock` reset fallback noted in both reviews is recorded only as a possible future decision.
- Provenance: user answers "Accept, TC-07 open (Recommended)" and "Continue here (Recommended)".
- Scope: feature closure. No commit, push, PR, or ADR was requested; the feature diff remains uncommitted in the worktree.
