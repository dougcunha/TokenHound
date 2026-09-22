# Workflow decisions and evidence

## Baseline

- Git base: `97f17c79a5afeebbd8ff3a534cced7681b382476`.
- The worktree was clean before this feature folder was created on 2026-09-22.
- This is one feature with one primary outcome: expose the desktop application's current provider metrics to MCP clients.
- The coordinator session identifier is unavailable in this host; `owner_session` is `null` in the checkpoint.

## Decisions

### DEC-01 — Requested feature and constraints

- Source: user request on 2026-09-22.
- Decision: add MCP access to real-time metrics for providers displayed by TokenHound, using `ModelContextProtocol.AspNetCore` and HTTP/SSE.
- Scope: product intent and required integration stack; this does not approve the PRD's interpretation of SSE, freshness, or synthetic fallback behavior.

### DEC-02 — HIL 1 product approval

- Source: user response `Aprovar PRD (Recommended)` to the HIL 1 question on 2026-09-22.
- Decision: approve `prd.md`, including SSE compatibility, latest-reading semantics, and exclusion of synthetic mock telemetry.
- Approved SHA-256: `eaab8d018d3ac1c19159ac94b4d823da2ad37e204d79381f0a34078bc4f92565`.
- Scope: the product contract in that exact PRD version. The technical solution and implementation still require HIL 2.

### DEC-03 — Continue this session

- Source: user response `Continuar nesta sessão (Recommended)` to the session pause question on 2026-09-22.
- Decision: proceed to TechSpec and task planning in this coordinator session.

### DEC-13 — HIL 2 technical approval

- Source: user response `Aprovar e implementar (Recommended)` to the HIL 2 question on 2026-09-22.
- Decision: approve `techspec.md`, `tasks.md`, and `task_01.md` through `task_03.md`; authorize implementation and corrections within those exact contracts.
- Approved SHA-256: `techspec.md` `86d0a0950ccd66fb1fa5fc9b36d36dd02e86f7d67aa0fdd524482561c6710fed`; `tasks.md` `9bded76a2dec53b519b7574acd64d3d6db0126012f4c22875142c7ce2c9639e3`; `task_01.md` `424b3e436a5b369314917c925bce5110680e803e1dbfbcc776448deec89f0424`; `task_02.md` `85265a9f2461a88836ff9940929ba15b363d4d030a6ad8ce45eaf213255bdc93`; `task_03.md` `12610019f442328206ff47f05e0e5850dc839e956d8a0bb7aab3b2723bcaac85`.
- Scope: T01, then T02, then T03. A material contract change still requires exception HIL.

### DEC-14 — Snapshot and continue

- Source: user response `Salvar snapshot e continuar (Recommended)` to the session pause question on 2026-09-22.
- Decision: write `context-snapshot.md` and continue with T01 in this coordinator session.

### DEC-15 — Pause after T01

- Source: user response `Encerrar e retomar com snapshot` to the between-task session pause on 2026-09-22.
- Decision: end this session after preserving the completed T01 handoff and resume at T02 in a new session. This is a session decision; HIL 2 authorization remains valid.

### DEC-16 — Exception HIL: drop obsolete session idle settings

- Source: user response `Drop both settings` to the T02 exception HIL on 2026-09-22.
- Context: SDK 2.2.0 marks `HttpServerTransportOptions.IdleTimeout` and `MaxIdleSessionCount` obsolete (`MCP9006`). They bound only stateful Streamable HTTP sessions; legacy SSE sessions end with their GET stream. DEC-11 approves suppressing only `MCP9004`.
- Decision: do not set either property; keep the SDK defaults (2 hours, 10,000 idle sessions). The 60-requests-per-minute limit and the 16-connection Kestrel cap remain.
- Alternatives rejected: a local `MCP9006` suppression; keeping the values with visible build warnings.
- Scope: amends DEC-09 in `techspec.md` and T02.3 in `task_02.md`; the approved hashes below replace those recorded in DEC-13 for these two files.
- Approved SHA-256: `techspec.md` `0faf33b4663affef6bdd47d17c4f1a80a8269c7808ce798cc5cd571ceae53546`; `task_02.md` `17c17bc7878168bf54093268c5da834564a4d90f66443a55b8c82ca8ced036ba`.

### DEC-17 — Continue after T02

- Source: user response `Continue without snapshot` to the between-task session pause on 2026-09-22.
- Decision: start T03 in this coordinator session without rewriting `context-snapshot.md`. HIL 2 authorization (`DEC-13`, amended by `DEC-16`) remains valid.

### DEC-18 — End session before the review

- Source: user response `Snapshot and end session (Recommended)` to the session pause after T03 on 2026-09-22.
- Decision: rewrite `context-snapshot.md` for the review and end this session; `sdd-review-code` runs in a new session that authored none of T01–T03.

### DEC-19 — README MCP section

- Source: user request on 2026-09-22 after DEC-18, before the review.
- Decision: add an `MCP Server` section to `README.md` with both URLs and setup for Claude Code, Codex CLI, Cursor, VS Code (GitHub Copilot), Gemini CLI, OpenCode, and Cline, linking to `docs/MCP.md` for field semantics. Documentation only (FR-08); no code changed. Include it in the review scope.

### DEC-20 — End correction session before independent re-review

- Source: user response `Snapshot and end session (Recommended)` to the session pause after T04 on 2026-09-22.
- Decision: use the saved `context-snapshot.md`, end this authoring session, and run the re-review in a new session that did not author CR-01's correction. This is a session decision; it does not approve the correction or HIL 3 acceptance.

### DEC-21 — Re-review deferred to an independent session

- Source: user response `End session (Recommended)` to the session pause on 2026-09-22, when a resumed coordinator that authored T02 and T03 reached the `codereview_2` re-review.
- Decision: end that session without reviewing; the re-review runs in a session that authored none of T01–T04. Checkpoint and snapshot are unchanged apart from this record.

### DEC-22 — HIL 3 route: execute the manual script now

- Source: user responses `Run manual script now (Recommended)` to the HIL 3 question and `Continue in this session (Recommended)` to the session pause on 2026-09-22, after `codereview_2` returned `APPROVED`.
- Decision: the coordinator runs the TechSpec manual acceptance script (TC-06 and the tray-exit half of TC-05) with Windows MCP tools in this session, then presents HIL 3 acceptance with that evidence. This does not yet accept the delivery. Closing a running user instance of TokenHound needs separate confirmation.

### DEC-23 — Exception HIL: fix ACC-01 at the root

- Source: user responses `Fix at the root (Recommended)` to the ACC-01 exception HIL and `Snapshot and continue (Recommended)` to the session pause on 2026-09-22.
- Decision: add a nullable used-count field to `LimitWindow`; have the Antigravity adapter populate it instead of `RemainingUnits`; switch the HUD's derived-window special case to the new field; expose it as `usedUnits` in the MCP window projection and document it.
- Scope: approves the direction and the contract change in principle. It amends the PRD (the out-of-scope adapter change for this one provider) and the TechSpec (new field, decision, tests). The concrete amendments and correction task are presented for approval before code changes. HIL 3 stays pending.

### DEC-25 — Exception HIL: approve DEC-24 amendments and T05

- Source: user responses `Approve and implement (Recommended)` and `Continue in this session (Recommended)` on 2026-09-22.
- Decision: approve the amended PRD (NFR-03 and the out-of-scope exception), the TechSpec DEC-24/CMP-09/TC-07 and baseline additions, the manifest's T05 entry, and `task_05.md`; implement T05 in this session. This session becomes T05's author, so the re-review runs in another session.
- Approved SHA-256: `prd.md` `16d2613843a577edc59c4bec6bb0b5d749660bad467c82846bbae1f663ec91e9`; `techspec.md` `8bc8cd009597a0ddbeb5df17f107380dd2b395d9514b9a686cc15c97124e7a4c`; `task_05.md` `62a2ac9130651fe158bbe0b74c20d5ce986d790e02f032607e71ab61bebe1dda`.
- Scope: T05 only. HIL 3 stays pending.

### DEC-26 — End T05 authoring session before re-review

- Source: user response `Snapshot and end session (Recommended)` to the session pause after T05 on 2026-09-22.
- Decision: end this session. `codereview_3` runs in a new session that did not author T05. This does not approve T05 or HIL 3.

## Manual acceptance evidence (DEC-22)

Executed 2026-09-22 10:26–10:31 local time on the primary monitor with Windows MCP tools, using the Debug build of the current worktree (`src/TokenHound.App/bin/Debug/net10.0-windows`). The user's installed copy (`D:\Apps\TokenHound`, PID 32612) was closed from its tray with confirmation and was not reopened. Raw client output is in `acceptance/`.

| Step | Result | Evidence |
| --- | --- | --- |
| 1. Launch and HUD capture | Pass | Six rings: Antigravity (`gemini`), OpenCode, Copilot, Codex, Claude Code, Cline. Only `127.0.0.1:37653` was listening, owned by the dev process. |
| 2. SSE connect, list tools, `list_provider_metrics` | Pass for transport; **fail for `gemini`** | `acceptance/tc06_step2.txt`: SSE 200 with `/mcp/message` endpoint, both tools discovered, six providers returned. Hover tooltips matched MCP for Claude (36%/5%), Codex (89%/71%), Copilot (62%, 7,554 of 20,000; credits gross 23,358.9466, net 17,658.9466), OpenCode (1/2/17%) and Cline (needsAuth, no windows). Antigravity HUD shows "~0 requests today · no limit published" (used count); MCP returns `remainingUnits: 0` for `Requests Today`. |
| 3. Lookup by ID | Pass | `acceptance/tc06_step3.txt`: `CLAUDE` → `available` (case-insensitive), `mock` → `synthetic`, and earlier `nope` → `unknown`. No credential, cookie, path or raw diagnostic in output; the Copilot org name shown in the HUD is correctly absent. |
| 4. Disable provider | Pass | `acceptance/tc06_step4.txt`: OpenCode unchecked in Settings → absent from the list and `disabled` by ID; Cursor (already disabled) → `disabled`. OpenCode was re-enabled; Settings were restored to the original state. No mock fallback was shown in the HUD. |
| 5. Tray exit and reopen | Pass | `acceptance/tc05_hold.txt`: the open SSE stream got server EOF when the app exited from the tray; the process exited, port 37653 was freed, and new connections were refused. After relaunch, `acceptance/tc05_reopen.txt` shows the same URL working (`claude` → `available`). The dev instance was then exited from the tray. |

### ACC-01 — `gemini` request count exposed as `remainingUnits`

- Fact: `src/TokenHound.Infrastructure/Providers/Antigravity/AntigravityUsageProvider.Snapshots.cs:99-106` stores today's *used* request count in `LimitWindow.RemainingUnits`, contrary to the model's documented meaning (`LimitWindow.cs:32-34`, remaining units). The HUD compensates with a derived-fidelity special case (`ProviderRingViewModel.Status.cs:14-19`, `ProviderUsageRowFactory.cs:69-75`). `McpMetricsReader.MapWindow` copies the field verbatim, so MCP reports `remainingUnits: 0` for a provider with about 0 requests used.
- Impact: MCP clients read the `gemini` window as exhausted (or with N remaining) when it carries a usage count. This violates NFR-03 and TC-06's matching-metrics criterion for this provider. All other providers use `RemainingUnits` as documented.
- Scope: the defect predates this feature, in an adapter the PRD lists as out of scope, and the independent review could not detect it from code conformance alone. It needs an exception HIL decision before acceptance.

## Worktree observation at pause

- The T01 files remain untracked under `src/TokenHound.Infrastructure/Engine/`, `src/TokenHound.Infrastructure/Mcp/`, `tests/TokenHound.Infrastructure.Tests/Mcp/`, and this feature folder.
- `git status --short` now also shows tracked deletions under unrelated `tasks/prd-arch-20260912-*` and `tasks/prd-feat-20260916-01-cline-provider/` folders. They appeared after the T01 snapshot and were not part of this feature's writes; preserve and reconcile them on resume without restoring or deleting them.

## Review cycle

- `codereview_1/codereview.md` records an independent `REJECTED` opinion against Git base `97f17c79a5afeebbd8ff3a534cced7681b382476` on 2026-09-22. CR-01 identifies a mixed snapshot and successful-timestamp read during concurrent refresh. The 20 scoped MCP tests pass; TC-06 and the tray-exit half of TC-05 remain for HIL 3. This review session authored none of the code it judged.
- Corrections for CR-01 are within the product and HIL 2 technical scope approved by DEC-02, DEC-13, and DEC-16. Plan the correction in `codereview_1/` and reconcile T01 through its DAG owner.
- `codereview_1/done/task_04.md` records the CR-01 correction. The store now publishes and reads the snapshot and retained-success timestamp under one lock; the scoped MCP suite passed 20/20, and the Infrastructure test project and App built with zero warnings. T01 was reopened with its original handoff preserved, then completed again with a correction link in `done/task_01.md`; T02's SSE tests and T03's App build were revalidated. The next review must run in a session that did not author this correction.
- `codereview_2/codereview.md` records an independent `APPROVED` re-review on 2026-09-22 by a session that authored none of T01–T04. `codereview_1/CR-01` is resolved; no new findings. Builds passed with 0 warnings, 20 scoped MCP tests passed, and the concurrency test passed 5 repeated runs. TC-06 and the tray-exit half of TC-05 remain open for HIL 3.
- ACC-01 (manual acceptance, DEC-22) was fixed by T05 under DEC-23/DEC-24/DEC-25; see `done/task_05.md`. The session that ran `codereview_2` authored T05, so the next review (`codereview_3`) must run in a different session. Its scope is T05 plus the amended PRD/TechSpec against base `97f17c7`; the rest remains covered by `codereview_2` unless files changed.

## Pending gates

### HIL 3 — Delivery acceptance

- After T01–T03, independent review, and integrated validation, decide acceptance against the current delivery and the manual script.
