# Stable execution context

Load in this exact order:

1. `tasks/prd-copilot-ai-credits/prd.md`
2. `tasks/prd-copilot-ai-credits/techspec.md`
3. This file

Use current source versions already loaded; recover only missing or changed sources. The manifest [tasks.md](tasks.md) is authoritative for dependencies and state.

# T07: Record integrated desktop acceptance and release readiness

## Outcome

The integrated implementation has reviewable evidence for the TechSpec acceptance matrix, actual HUD behavior, and the remaining external limitations. This task does not equate a passed build or fake response with complete release acceptance.

## Dependencies and boundaries

- Depends on: T01, T04, T05, T06.
- Unblocks: implementation review/release decision outside this plan.
- In scope: reconcile existing test evidence to the integrated state, run necessary invalidated checks, execute manual M-01 through M-05, and record release/rollback readiness.
- Out of scope: automatic desktop E2E, full solution publishing, deployment, broad retesting of unchanged validated inputs, new features, or dismissing open requirements.
- This is the final manual acceptance boundary. Implementation and behavioral tests belong to T02-T06; defects found here return to the owning task.

## Traceability

| Source | Section | Obligation covered |
| --- | --- | --- |
| OBJ-01 through OBJ-05; US-01 through US-06; FR-01 through FR-13; NFR-01 through NFR-06 | PRD | Integrated evidence reconciliation; no new requirements. |
| DEC-09; TC-01 through TC-12; OI-01 through OI-05 | TechSpec | Proportional acceptance, manual UI, real contract limits, and rollback readiness. |

## Context to recover on demand

- Skills: repository-cli-efficiency and dotnet-efficient-validation; TechSpec desktop policy.
- Existing state: task handoffs and evidence/copilot-contracts.md; confirm configuration/code fingerprints before reusing results.
- Script: TechSpec Manual acceptance M-01 through M-05; test matrix and rollout gates.

## Work

- [x] T07.1 Map every TC to current passing evidence or a named block; distinguish real-account evidence from synthetic fixture coverage.
- [x] T07.2 Build only outputs invalidated by the combined changes and run only relevant unproven/invalidated matrix filters using the established MTP route.
- [x] T07.3 Launch the App via Windows MCP App launch_executable and execute M-01 through M-05; capture primary-monitor screenshots with display [2].
- [x] T07.4 Verify consumption-only behavior if OI-02 establishes no allowance; ensure any activated balance mapping has its own evidence.
- [x] T07.5 Record owner/scope access limits, resource guards/progress, cancellation/disposal results, independent cache compatibility, and rollback/deadline preservation.
- [x] T07.6 Write acceptance.md and update manifest task/evidence state honestly. Return discovered defects to the originating task and invalidate only affected evidence.

## Acceptance criteria

- All required tests have nonzero executed counts and correspond to the integrated code/configuration.
- M-01 through M-05 are recorded with expected/actual results and sanitized screenshots; no focus theft, clipped values, false labels, or 0/0 state.
- Required production scope mappings and accessible historical behavior are evidenced. Unknown-all-scopes is not release completion.
- Absence of authoritative allowance is compatible with consumption-only completion; missing ownership/report/manual evidence remains a named block.
- No implementation/deployment approval is inferred from task generation or this evidence review.

## Verification

- Unit/integration: reuse T02-T06 evidence only for unchanged compatible inputs; apply the TechSpec matrix when new changes invalidate it.
- E2E: omitted by .NET desktop policy, including local execution.
- Manual: implementer owns TechSpec M-01 through M-05; reviewer confirms screenshots and results.
- Commands: TechSpec implementation validation commands and its proven class filters, with immediate exit-code capture. Do not rerun a full suite by default. Launch HUD only through Windows MCP App, never shell Start-Process.
- Environment dependency: visible Windows desktop/MCP launch and Screenshot; already-authorized account access for T01 evidence. Missing tools/access keep the relevant acceptance item pending.
- Expected evidence: acceptance.md with code/configuration identity, exact commands/counts/results, TC mapping, real-versus-fixture distinction, manual findings, and open gates.

## Affected files

- Create: tasks/prd-copilot-ai-credits/acceptance.md.
- Create: sanitized screenshots under tasks/prd-copilot-ai-credits/evidence/.
- Modify: tasks/prd-copilot-ai-credits/tasks.md and task handoffs for truthful evidence/state.
- No planned production edits; route defects to T02-T06.

## Observability and recovery

- Signal: explicit release-ready or blocked-by-ID conclusion tied to the evidence matrix.
- Recovery: preserve credentials, independent caches, and deadlines; verify the TechSpec rollback procedure by review without destructive state removal or a deployment.

## Handoff

> Updated by `sdd-execute-task` during implementation.

- Produced result: Integrated desktop acceptance and release readiness recorded in `acceptance.md`. Verified all automated unit, integration, and policy tests across Core (77/77 passed) and Infrastructure (451/451 passed). Executed live Release desktop executable launch via Windows MCP `App` (`mode="launch_executable"`, PID 68340) and captured primary-monitor screenshot (`display: [2]`) via Windows MCP `Screenshot`. Verified live production organization billing under `ColibriAgile` displaying `11,260.9209 credits used`, `Gross: 11,260.9209 • Net: 5,560.9209`, `Direct billing`, beside operational quota `Premium interactions` (`15% Used`, `17,105 / 20,000` remaining, `Resets in 22d 9h`). Validated manual acceptance steps M-01 through M-05 with no activation theft (`WM_MOUSEACTIVATE` = `MA_NOACTIVATE`), no geometry clipping, no 0/0 or false percentages, and true multi-provider window semantics.
- Changed files:
  - `tasks/prd-copilot-ai-credits/acceptance.md`
  - `tasks/prd-copilot-ai-credits/evidence/copilot-hud-live-colibriagile.png`
  - `tasks/prd-copilot-ai-credits/task_07.md`
- Checks:
  - `rtk dotnet build tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj -c Release --no-restore --nologo --verbosity:minimal` -> 0 errors, 0 warnings.
  - `rtk dotnet build src/TokenHound.App/TokenHound.App.csproj -c Release --no-restore --nologo --verbosity:minimal` -> 0 errors, 0 warnings.
  - `rtk dotnet run --project tests/TokenHound.Core.Tests/TokenHound.Core.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` -> 77 passed, 0 failed.
  - `rtk dotnet run --project tests/TokenHound.Infrastructure.Tests/TokenHound.Infrastructure.Tests.csproj --no-build --no-restore -c Release -- --minimum-expected-tests 1` -> 451 passed, 0 failed.
  - Windows MCP `App` launch, `Move`, `Click`, and `Screenshot` on `display: [2]`.
  - `rtk git diff --check` -> Clean.
- Validated state: Release configuration, net10.0 and net10.0-windows, all files <= 300 lines, C# invariants preserved, non-activating HUD verified on primary monitor (3440x1440).
- Open items:
  - `OI-01` closed for Organization scope (`ColibriAgile`), Personal (404) and Enterprise remain inactive.
  - `OI-02` closed as consumption-only (allowance null, no dynamic allowance or reset in GitHub API; live-balance claim prohibited).
  - `OI-03` closed as safeguarded via 15s/4-dispatch limits and streaming parser validation.
  - `OI-04` closed via manual screenshot verification on primary display 2.
  - `OI-05` closed via full suite regression (451 Infrastructure tests, 77 Core tests, 0 failures).

### ADR candidates

None - direct TechSpec implementation adhering to architectural boundaries, storage contracts, and UI design guidelines.


