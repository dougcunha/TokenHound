# Stable execution context

Load in this exact order:

1. `tasks/prd-arch-20260912-04-provider-failure-mapping/codereview_1/codereview.md`
2. This file

Use the already loaded report version; recover relevant contracts and code afterward. This order does not guarantee a host cache hit.

---

# T03 — Repair the QA-02 quality gate

## Outcome

`QA-02` in `techspec.md` is a valid, reproducible quality rule: valid class, a command that counts the intended unit, a baseline verified from the pre-feature revision, and a target that the corrected tree meets.

## Dependencies and boundaries

- Depends on: —
- Unblocks: T04
- In scope: the `QA-02` row and the "Target measures" narrative in `techspec.md`.
- Out of scope: any source, test, PRD, or manifest change; reclassifying QA-01/QA-03.

## Traceability

| Source | Section | Finding covered |
| --- | --- | --- |
| codereview_1/CR-02 | `codereview.md#findings` | QA-02 has an invalid `booking` class, a baseline that conflicts with the handoffs (7 versus 8), and a non-reproducible `≤ 2` target over a command that counts both definitions and call sites. |

## Requirements

- The `Class` cell must be one of the two valid classes from `references/quality-dotnet.md`: `blocking` or `reservation`. Use `reservation`: the residual hits are DEC-justified thin delegates (maintenance cost, no demonstrated failure path).
- The verification command must count the intended unit (mapping **method definitions**), not definitions plus call sites.
- Baseline must be verified, not asserted: run the command against `HEAD` (`git grep`) and against the current worktree.
- Target must be expressible against the same command.

## Context to recover on demand

- TechSpec: `#quality-profile` (`QA-02`), `#technical-decisions` (`DEC-02`, `DEC-03`).
- Rules/skills: `sdd-create-techspec` `references/quality-dotnet.md` (classes, baseline semantics).
- Code: `src/TokenHound.Infrastructure/Providers/SnapshotFailureMapper.cs`, `Antigravity/AntigravityUsageProvider.Snapshots.cs`, `Copilot/CopilotBillingService.cs`.

## Work

- [x] T03.1 Replace the `QA-02` row with: class `reservation`; command `rtk rg -n "private .*\bMap(HttpFailure|CloudCodeFailure|BillingFailure)\(" src/TokenHound.Infrastructure/Providers`; verified baseline `4`; target `≤ 2`. Evidence: `techspec.md` QA-02 row now class `reservation`, baseline `4`, target `≤ 2 (thin delegates)`.
- [x] T03.2 Verify the baseline is `4` at `HEAD` via `rtk git grep -n -E "private .*Map(HttpFailure|CloudCodeFailure|BillingFailure)\(" HEAD -- src/TokenHound.Infrastructure/Providers` and the current count is `2`. Evidence: `git grep` returned 4 method definitions at `HEAD`; `rg` returned 2 definitions in the worktree (Antigravity `MapCloudCodeFailure`, Copilot `MapBillingFailure`).
- [x] T03.3 Update the "Target measures today/end" lines so the counts match the repaired command and unit. Evidence: `3 near-identical builders; 4 mapping method definitions (HEAD baseline)` / `1 builder; 2 mapping method definitions (DEC-02/DEC-03 thin delegates)`.

## Acceptance criteria

- [x] `QA-02` uses a valid class and a command whose output unit matches the baseline and target. Evidence: class `reservation` (valid per `references/quality-dotnet.md`); the command counts only `private ... Map*Failure(` method definitions, so baseline `4` and target `≤ 2` share one unit.
- [x] The recorded baseline is reproduced from `HEAD` and the current hit count meets the target. Evidence: `HEAD` = 4 lines; worktree = 2 lines (`≤ 2`).

## Verification

- Commands: the two `rg`/`git grep` commands above.
- Expected evidence: `HEAD` baseline 4 lines; current worktree 2 lines; `techspec.md` row consistent with both.
- Environment dependency: none.

## Affected files

- Modify: `tasks/prd-arch-20260912-04-provider-failure-mapping/techspec.md`

## Observability and recovery

- Operational signal: none (planning artifact).
- Recovery: revert the `techspec.md` edit.

## Handoff

> Updated by `sdd-execute-corrections` during implementation.

- Produced result: QA-02 repaired for CR-02. `techspec.md` QA-02 is class `reservation`, baseline `4`, target `≤ 2 (thin delegates)`, with a command that counts mapping method definitions only; the "Target measures today/end" lines use the same unit.
- Changed files: `tasks/prd-arch-20260912-04-provider-failure-mapping/techspec.md`; `tasks/prd-arch-20260912-04-provider-failure-mapping/task_03.md`.
- Checks:
  - Baseline (HEAD): `rtk git -C "D:\MyProjects\TokenHound" grep -n -E -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" HEAD -- src/TokenHound.Infrastructure/Providers` → 4 lines: Antigravity `AntigravityUsageProvider.Snapshots.cs:136`, Copilot `CopilotBillingService.cs:153`, Cursor `CursorUsageProvider.cs:209`, OpenCode `OpenCodeUsageProvider.cs:136`. (`git grep` needs `-E` so `\(` is a literal paren; its default BRE rejects `\(` as an unmatched group.)
  - Current (worktree): `rtk rg -n -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" src/TokenHound.Infrastructure/Providers` → 2 lines: Antigravity `AntigravityUsageProvider.Snapshots.cs:136`, Copilot `CopilotBillingService.cs:153`.
  - New QA-02 row (pipe-free `rg -e` form, identical in raw and rendered markdown): `| QA-02 | Failure-mapping switches share one decision core | reservation | rtk rg -n -e "private .* MapHttpFailure\(" -e "private .* MapCloudCodeFailure\(" -e "private .* MapBillingFailure\(" src/TokenHound.Infrastructure/Providers | 4 | ≤ 2 (thin delegates) |`.
- Validated state: Planning-artifact-only edit; no code/config/test change. Baseline 4 reproduced at HEAD and current count 2 meets target `≤ 2`. Worktree preserved (dirty with pre-existing sibling changes); nothing committed.
- Open items: None for this task. CR-01, CR-03, and CR-04 remain with their own owners; this correction is limited to CR-02.
