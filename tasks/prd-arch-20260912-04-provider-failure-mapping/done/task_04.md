# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-04-provider-failure-mapping/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T04 — Reconcile the PRD acceptance state

## Outcome

The PRD acceptance checklist and the manifest agree with the verified post-correction evidence, so a future reviewer can trust the recorded state.

## Dependencies and boundaries

- Depends on: T01, T02, T03
- Unblocks: —
- In scope: the acceptance checklist in `prd.md` (`prd.md:31-34`).
- Out of scope: source, tests, `techspec.md`; the manifest `tasks.md` is written by the orchestrator, not this task.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/CR-04 | `codereview.md#findings` | `prd.md:31-34` keeps all four acceptance boxes unchecked while the manifest marked T01/T02 complete, so the artifacts disagreed about acceptance. |

## Requirements

- Do not certify an item the corrections did not re-verify. Each box is checked only against evidence produced after CR-01..CR-03 were corrected.
- Preserve the checklist wording; only the check state changes, plus an evidence pointer where one already exists.
- If any acceptance item cannot be evidenced, leave it unchecked and record a concrete pending item instead of asserting acceptance.

## Context to recover on demand

- PRD: `#acceptance-criteria`; `#behaviors-to-preserve` (`R-01`..`R-05`).
- TechSpec: `#safety-net` (`TC-01`..`TC-04`), `#quality-profile`.
- Report: `codereview_1/codereview.md` `#coverage-matrix`.
- Rules/skills: `dotnet-efficient-validation`.

## Work

- [x] T04.1 Re-check `R-01`..`R-05` against the corrected provider, store, and policy test evidence. Evidence: focused MTP classes `*CursorUsageProviderTests*` 9, `*AntigravityUsageProviderTests*` 11, `*ClaudeOAuthProviderTests*` 13, `*OpenCodeUsageProviderTests*` 18, `*CopilotUsageProviderTests*` 11, `*CopilotBillingServiceTests*` 6, `*CopilotBillingServiceHistoricalTests*` 4, `*RateLimitPolicyTests*` 12; full `TokenHound.Infrastructure.Tests` 675 passed (includes store tests).
- [x] T04.2 Update the four `prd.md` acceptance checkboxes to reflect the verified state, with the test command or evidence named inline.
- [x] T04.3 Report to the orchestrator the exact text the manifest should record; do not edit `tasks.md`. Evidence: exact text returned in the T04 final message (no edit to `tasks.md`).

## Acceptance criteria

- [x] Every acceptance item is checked only with post-correction evidence, or explicitly left unchecked with a pending item. Evidence: all four `prd.md` boxes are checked against commands rerun in the current worktree after CR-01..CR-03.
- [x] `prd.md` contains no checked acceptance item without verifiable evidence. Evidence: each box names its test class/count or `rg` command and result inline.

## Verification

- Unit: the corrected Cursor/Antigravity/Claude/OpenCode/Copilot classes plus `UsageStoreTests` and `RateLimitPolicyTests`.
- Integration: full `TokenHound.Infrastructure.Tests` project.
- E2E: omitted by .NET desktop policy.
- Manual: none.
- Commands: the focused MTP classes named in the task handoffs plus the full Infrastructure project run, each with `--minimum-expected-tests 1`.
- Expected evidence: pass counts and the corrected `prd.md` check state.

## Affected files

- Modify: `tasks/prd-arch-20260912-04-provider-failure-mapping/prd.md`

## Observability and recovery

- Operational signal: none (planning artifact).
- Recovery: revert the `prd.md` edit.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: Done. CR-04 reconciled. The four `## Acceptance criteria` items in `prd.md` are now checked, each with post-correction evidence named inline. Evidence was independently rerun in the current worktree; the build was reused (`--no-build`) because `bin/Debug/net10.0/TokenHound.Infrastructure.Tests.dll` (2026-09-12T20:51:37) is newer than every modified source/test file (newest 2026-09-12T20:51:25).
- Changed files:
  - Modify: `tasks/prd-arch-20260912-04-provider-failure-mapping/prd.md` — four acceptance checkboxes checked with inline evidence.
  - Modify: `tasks/prd-arch-20260912-04-provider-failure-mapping/task_04.md` — work/acceptance boxes and this handoff.
  - No code, tests, `techspec.md`, `tasks.md`, `done/`, or sibling feature touched. Dirty worktree preserved; nothing committed.
- Checks (all exit 0 unless noted; MTP native, Debug/net10.0, `--no-build --no-restore -- --minimum-expected-tests 1`):
  - `--filter-class "*CursorUsageProviderTests*"` → 9 passed / 0 failed.
  - `--filter-class "*AntigravityUsageProviderTests*"` → 11 passed / 0 failed.
  - `--filter-class "*ClaudeOAuthProviderTests*"` → 13 passed / 0 failed.
  - `--filter-class "*OpenCodeUsageProviderTests*"` → 18 passed / 0 failed.
  - `--filter-class "*CopilotUsageProviderTests*"` → 11 passed / 0 failed.
  - `--filter-class "*CopilotBillingServiceTests*"` → 6 passed / 0 failed.
  - `--filter-class "*CopilotBillingServiceHistoricalTests*"` → 4 passed / 0 failed.
  - `*RateLimitPolicyTests*` (project `tests/TokenHound.Core.Tests`) → 12 passed / 0 failed.
  - Full `rtk dotnet test --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -- --minimum-expected-tests 1` → 675 passed / 0 failed.
  - QA-01 `rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src` → 0 hits, `rg` exit 1.
  - Corrected QA-02 `rtk rg -n -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" src/TokenHound.Infrastructure/Providers` → 2 hits (`AntigravityUsageProvider.Snapshots.cs:136`, `CopilotBillingService.cs:153`), exit 0, baseline 4 target ≤ 2.
- Validated state: Planning-artifact-only edit. Project `TokenHound.Infrastructure.Tests` (Debug/net10.0/x64), SDK 10.0.401 with native Microsoft.Testing.Platform runner; build from the T01/T02 correction reused unchanged. Worktree dirty with pre-existing sibling changes; no checkout/stash/reset/clean.
- Final `prd.md` acceptance text (now on disk):
  - `- [x] All \`R-NN\` items were checked after refactoring. Evidence: focused MTP classes passed with \`--minimum-expected-tests 1\` — \`*CursorUsageProviderTests*\` 9 (R-01), \`*AntigravityUsageProviderTests*\` 11 (R-02), \`*ClaudeOAuthProviderTests*\` 13 (R-03), \`*OpenCodeUsageProviderTests*\` 18 and \`*CopilotUsageProviderTests*\` 11 plus \`*CopilotBillingServiceTests*\` 6 and \`*CopilotBillingServiceHistoricalTests*\` 4 (R-04), \`*RateLimitPolicyTests*\` 12 (R-05 policy); R-05 store coverage is included in the full project run.`
  - `- [x] No new behavior entered the scope silently. Evidence: full \`TokenHound.Infrastructure.Tests\` run passed 675 tests with \`--minimum-expected-tests 1\` (exit 0), and the provider failure classes assert the preserved statuses, reasons, and deadlines.`
  - `- [x] Provider failure tests pass with \`--minimum-expected-tests 1\`. Evidence: every focused MTP class run above and the full Infrastructure project run used the guard and exited 0.`
  - `- [x] Duplicate snapshot/mapping bodies are eliminated. Evidence: QA-01 \`rtk rg -n "private Snapshot CreateRateLimitedSnapshot\(ProviderHttpException" src\` returned 0 hits (\`rg\` exit 1); corrected QA-02 \`rtk rg -n -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" src/TokenHound.Infrastructure/Providers\` returned 2 thin delegates against baseline 4 (target ≤ 2).`
- Exact text to record in the manifest (orchestrator owns `tasks.md`; not edited here): "T04 complete — CR-04 reconciled. Re-ran post-correction evidence in the current worktree: `*CursorUsageProviderTests*` 9, `*AntigravityUsageProviderTests*` 11, `*ClaudeOAuthProviderTests*` 13, `*OpenCodeUsageProviderTests*` 18, `*CopilotUsageProviderTests*` 11, `*CopilotBillingServiceTests*` 6, `*CopilotBillingServiceHistoricalTests*` 4, `*RateLimitPolicyTests*` 12, full `TokenHound.Infrastructure.Tests` 675 passed; QA-01 0 hits, corrected QA-02 2 hits (baseline 4, target ≤2). All four `prd.md:31-34` acceptance boxes now checked with inline evidence."
- Open items:
  - AA-10 remains open: Antigravity `403 → null` preserved pending a decision.
  - Feature-level re-review after CR-01..CR-04 is owned by the orchestrator (`sdd-review-code`), not this task.
- ADR candidates: None.
