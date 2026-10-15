# Stable execution context

Read `codereview.md`, then this file. Recover PRD/TechSpec TC-05 and `../validation.md#pending-real-desktop-acceptance-tc-05` on demand.

# T01: Record real desktop acceptance evidence

## Outcome and boundaries

Resolve the essential evidence gap without changing process ownership code or altering the immutable review. Depends on the implemented feature; unblocks re-review and final acceptance. No code correction is indicated by the report.

## Traceability

Local finding `codereview_01/CR-01` maps to the report's unnumbered `Limitations and open items` TC-05 paragraph. Origin: PRD/TechSpec TC-05, not an implementation defect. Classification: pending environment/manual evidence. This local ID does not modify the report.

## Work and acceptance

- [ ] Run the built app in the actual user desktop and compare live Codex quotas/fallback.
- [ ] Observe for one hour; attribute any Git process older than two minutes to its actual owner rather than killing unrelated processes.
- [ ] Verify normal close and Task Manager termination leave no app-owned Codex/Git descendants.
- [ ] Inspect Application Popup event 26 for attributable 0xc0000142 errors.
- [ ] Record observations in validation.md, then obtain an independent re-review before closing the feature.

## Verification and environment

Unit/integration evidence already exists; do not rerun unchanged suites just to replace manual evidence. E2E omitted by desktop .NET policy. Manual owner: user or a session observing the actual desktop. AGENTS.md requires Windows MCP App `launch_executable` for visible launch and Windows MCP Screenshot when inspecting the HUD. Windows MCP tools are available, but the desktop process-list attempt returned `USER_CONTROL`; no desktop acceptance run was started. Shell Start-Process must not substitute for the desktop launch. No running user app was replaced or interrupted.

Expected evidence: dated one-hour observation, live quota behavior, process ancestry/creation times before and after both exit paths, and relevant event-log results. Affected files: validation.md and this handoff; review report immutable. Recovery: retain pending state if the environment is unavailable; do not weaken acceptance or infer success from helper tests.

## Handoff

Pending execution. No desktop acceptance evidence produced; no code changes requested. Automated implementation evidence remains in validation.md. Open item: actual desktop run and one-hour observation.
