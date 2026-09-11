# Stable execution context

Load in this exact order:

1. `tasks/prd-provider-opencode/codereview_1/codereview.md`
2. This file

---

# T08 — Restore objective traceability

## Outcome

The zero-configuration onboarding objective has an explicit, reviewable trace from the PRD through the implementation task and its tests without altering the historical handoff records.

## Classification

- Actionable: `codereview_1/CR-04`.

## Dependencies and boundaries

- Depends on: T01
- Unblocks: —
- In scope: Add the missing OBJ-02 mapping to the active implementation plan and preserve the existing completed task/handoff history.
- Out of scope: Credential implementation changes, test behavior changes, report edits, and unrelated objective or requirement redesign.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| `codereview_1/CR-04` | `codereview.md#findings` | `tasks.md` and T01 trace FR-01 but omit the explicit PRD objective OBJ-02. |
| OBJ-02 | `prd.md#outcomes-and-metrics` | Zero-configuration onboarding through automatic credential extraction. |
| FR-01 | `prd.md#functional-requirements` | Environment and `auth.json` credential discovery. |
| TC-01 | `techspec.md#test-approach` | Credential discovery tests. |

## Requirements

- Add an OBJ-02 row to the active `tasks.md` traceability matrix, linked to the existing T01 implementation and credential tests.
- Preserve `done/task_01.md` and its handoff; do not rewrite completed-task history solely to repair metadata.
- Keep the dependency graph acyclic and do not duplicate an existing task or finding ID.

## Context to recover on demand

- TechSpec: `techspec.md#components-and-flow`, `techspec.md#test-approach`
- Rules/skills: `sdd-plan-corrections`; preserve completed task handoffs
- Code: `src/TokenHound.Infrastructure/Providers/OpenCode/OpenCodeCredentialDiscovery.cs` — implementation already covered by T01
- Tests: `tests/TokenHound.Infrastructure.Tests/Providers/OpenCode/OpenCodeCredentialDiscoveryTests.cs` — evidence for OBJ-02

## Work

- [x] T08.1 Add an OBJ-02 trace row to `tasks.md` pointing to T01 and the existing credential discovery tests.
- [x] T08.2 Recheck the manifest's objective, requirement, test, dependency, and state references for duplicate or orphan IDs without modifying the prior report or handoff.

## Acceptance criteria

- `tasks.md` explicitly maps OBJ-02 to T01 and its credential discovery evidence.
- Existing task IDs, finding IDs, done-task content, dependency edges, and handoffs are preserved.
- The traceability inventory has no missing OBJ-02 mapping and no duplicate ID.

## Verification

- Unit: Not applicable; no code behavior changes.
- Integration: Not applicable; no runtime changes.
- E2E: Omitted by desktop .NET policy.
- Manual: Review the resulting manifest and compare IDs against the PRD, TechSpec, and task records.
- Environment dependency: None.
- Commands: `rtk rg -n "OBJ-02|FR-01|TC-01" tasks/prd-provider-opencode`; `rtk git diff --check`
- Expected evidence: One explicit OBJ-02 row in `tasks.md`, preserved done-task files, and clean diff validation.

## Affected files

- Modify: `tasks/prd-provider-opencode/tasks.md`

## Observability and recovery

- Operational signal: The manifest traceability row is the review artifact.
- Recovery: Remove only the new OBJ-02 row if the source objective is withdrawn; preserve all prior task and review history.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: Added one explicit OBJ-02 traceability row linking the PRD outcome to T01, FR-01, TC-01, and `OpenCodeCredentialDiscoveryTests`.
- Changed files: `tasks/prd-provider-opencode/tasks.md`; this handoff in `tasks/prd-provider-opencode/task_08.md`.
- Checks: `rtk rg -n "OBJ-02|FR-01|TC-01" tasks/prd-provider-opencode` passed; the manifest contains the explicit OBJ-02 row and linked T01, FR-01, TC-01, and credential-test references. `rtk git diff --check` passed.
- Validated version/state: `codereview_1` and the current T08 execution context; post-edit manifest verified as documentation-only with no code, configuration, project, or environment changes.
- Open items: None for CR-04. CR-01, CR-02, and CR-03 remain outside T08 scope.
