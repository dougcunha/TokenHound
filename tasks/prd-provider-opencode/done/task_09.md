# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/codereview_1/codereview.md`
2. This file

---

# T09 — Reconcile rate-limit status terminology

## Outcome

The PRD, TechSpec, task records, and repository contract use one explicitly approved name for the rate-limited provider state.

## Classification

- Decision resolved: caller approved `ProviderStatus.RateLimited` as the canonical status name.

## Dependencies and boundaries

- Depends on: T03
- Unblocks: —
- In scope: Resolve the `RateLimitReached` versus `RateLimited` terminology mismatch and update only the authoritative SDD references required by that decision.
- Out of scope: Rate-limit behavior implementation in T05/T06, new enum members without approval, and unrelated status names.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_1` limitations | `codereview.md#limitations-and-open-items` | The PRD/task wording uses `RateLimitReached`, while the repository enum and implementation use `RateLimited`. |
| FR-07 | `prd.md#functional-requirements` | Provider status state machine terminology. |
| T03 | `done/task_03.md#traceability` and `#handoff` | Existing task and handoff status terminology. |
| `ProviderStatus` | `src/TokenHound.Core/Models/ProviderStatus.cs` | Existing repository contract. |

## Requirements

- Record the approved canonical status name before modifying source artifacts.
- Make the PRD, TechSpec, task matrix, task records used for active planning, and implementation references consistent with that decision.
- Do not add an enum alias or change Core behavior unless the decision explicitly authorizes a contract change.
- Preserve the report and completed handoff history.

## Context to recover on demand

- TechSpec: `techspec.md#errors-security-and-recovery`, `techspec.md#test-approach`
- Rules/skills: `sdd-plan-corrections`; Core purity and stable contract rules in `AGENTS.md`
- Code: `src/TokenHound.Core/Models/ProviderStatus.cs` — current canonical enum member is `RateLimited`
- Code: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeUsageProvider.cs` — current implementation value

## Work

- [x] T09.1 Obtain and record the canonical status-name decision; keep this task pending if HIL does not authorize a choice.
- [x] T09.2 Update the affected SDD references and active task metadata consistently, or document the approved compatibility mapping if the Core enum is intentionally changed.
- [x] T09.3 Recheck all status-name references and ensure no duplicate or contradictory contract remains.

## Acceptance criteria

- A recorded decision identifies one canonical status name or an explicitly approved compatibility mapping.
- All active PRD/TechSpec/task references agree with the approved contract, or the authorized compatibility mapping is documented.
- No unapproved Core enum or implementation change is introduced.

## Verification

- Unit: Not applicable unless the approved decision changes the Core enum or runtime contract; then run the affected status tests.
- Integration: Not applicable for documentation-only reconciliation.
- E2E: Omitted by desktop .NET policy.
- Manual: HIL reviews and approves the canonical terminology decision.
- Environment dependency: HIL decision resolved by the caller; no runtime contract change authorized.
- Commands: `rtk rg -n "RateLimitReached|RateLimited" tasks/prd-provider-opencode src/TokenHound.Core src/TokenHound.Infrastructure`; `rtk git diff --check`
- Expected evidence: Recorded decision, consistent source references, and no unapproved runtime contract change.

## Affected files

- Modify: `tasks/prd-provider-opencode/prd.md`, `tasks/prd-provider-opencode/techspec.md`, and/or `tasks/prd-provider-opencode/tasks.md` according to the approved decision

## Observability and recovery

- Operational signal: The canonical terminology is documented in the source contract.
- Recovery: Revert only the terminology edits if HIL rejects the decision; preserve the implementation and historical report.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: Reconciled active PRD, TechSpec, and task-matrix provider status terminology to `ProviderStatus.RateLimited`; preserved `BlockedReason.RateLimitReached` as the distinct block-reason contract.
- Changed files: `tasks/prd-provider-opencode/prd.md`, `tasks/prd-provider-opencode/techspec.md`, `tasks/prd-provider-opencode/tasks.md`, and this handoff in `tasks/prd-provider-opencode/task_09.md`; no runtime or historical task files changed.
- Checks: `rtk rg -n "RateLimitReached|RateLimited" tasks/prd-provider-opencode src/TokenHound.Core src/TokenHound.Infrastructure` passed (exit 0; active SDD references use `ProviderStatus.RateLimited`, while historical and distinct block-reason references remain); `rtk git diff --check` passed (exit 0; no output). Status tests not run because the change is documentation-only and the runtime contract is unchanged. E2E omitted by desktop .NET policy.
- Validated version: Current `codereview_1/codereview.md` and root `task_09.md` versions read once before editing, with the current `ProviderStatus.RateLimited` Core contract verified after the documentation changes.
- HIL decision: `ProviderStatus.RateLimited` is the canonical status name, matching the existing Core enum and implementation; no enum alias or runtime change is authorized.
- Open items: None for T09. CR-01 through CR-03 and all out-of-scope runtime work remain outside this terminology-only correction.
