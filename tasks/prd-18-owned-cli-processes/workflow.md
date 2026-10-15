# Workflow

## DEC-01: Scope and authorization

User supplied `D:/problema-tokenhound.md`, then said "Pode implementar". This authorizes the proposed long-lived server, Windows process-tree ownership, graceful shutdown, and equivalent protection for other owned CLI processes. No unresolved product choice requires another permission request. The higher-priority collaboration instructions require carrying authorized work through implementation, so no redundant artifact approval is requested. Product/technical scope is recorded in PRD and TechSpec; final manual acceptance is not presumed.

Triage: full SDD signals S3 shared polling/concurrency, S4 Infrastructure plus App, S6 reuse/recovery/crash cleanup/shutdown/compatibility; S1 no serialized contract change, S2 borrowed credential path touched without credential writes, S5 no open product choice, S7 regression tests feasible, S8 no irreversible persistence. One integrated task preserves coherent process ownership. Git base `577a55a79f52e225c28f2e4fb92362c6b35a818d`; worktree initially clean. Only existing checkpoint was completed prd-17; it is not resumed.

## DEC-02: Review correction within the original contract

Read-only independent risk review identified expected gh startup errors escaping discovery and bypassing keychain/config fallback. T01 had implementation/test evidence but had not been independently approved. Corrected the source boundary and added four expected-failure cases plus cancellation propagation. Final integrated Infrastructure run: 1145 passed, exit code 0; App build zero errors/warnings. No scope expansion or new authorization required. Real desktop acceptance remains pending.

## DEC-03: Formal review received; manual evidence remains open

Read the immutable `codereview_01/codereview.md`: literal status `REJECTED`, with no current implementation defect and only TC-05 essential evidence pending. Reviewer authored no implementation and inherited no authoring-session history; it disclosed prior read-only risk-review context. Worktree reconciliation found only the reserved report added during formal review. No accepted status or human acceptance is inferred.

After receiving the review, a Marshal.Copy call was expanded to match argument formatting and two test-helper comparisons were made ordinal case-insensitive. This changed no production behavior. Rebuilt and reran all 1145 Infrastructure tests (exit 0), rebuilt App (zero errors/warnings), and refreshed graft. The report remains historical; manual evidence and re-review are still required before feature completion. Manual TC-05 is classified pending in `codereview_01/task_01.md`; no automatic correction/review loop can replace the missing desktop observation.

Windows MCP tools were confirmed available. A desktop process-list attempt returned `USER_CONTROL`; no desktop test session was started, no running app was interrupted, and no environment-unavailability claim is retained. Manual evidence remains pending for an appropriate desktop observation session.
