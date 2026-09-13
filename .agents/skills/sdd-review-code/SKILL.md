---
name: sdd-review-code
description: SDD review when implementation must be audited against PRD, TechSpec, and tasks; does not correct findings.
argument-hint: --prd feature-name [--base git-reference]
---

# Review SDD code

1. Require `prd.md`, `techspec.md`, and `tasks.md` under `tasks/prd-[slug]/`. Read PRD and TechSpec once per version; then manifest, tasks, and handoffs. Check every link, extra file, ID, state, and dependency.
   **Output:** every task has proven location and state; a missing source blocks review with the exact path.
2. Bound implementation by `--base` resolved to a commit, including relevant committed, staged, unstaged, and new files. Without a base, use handoffs and worktree; state the scope limitation. In re-review, include correction reports and handoffs without altering history.
   **Output:** reviewable set identified. An invalid base requests correction and never silently changes scope.
3. Build a matrix of every obligation: origin, implementation, test, state, and evidence. Reuse IDs; read each task's details according to its criteria without transcribing sources. Mark `conformant`, `non-conformant`, `pending`, or `not verifiable`.
   **Output:** no orphan obligation; incomplete tasks, broken links, and acceptance without evidence remain gaps.
4. Trace callers, effects, contracts, errors, and risks in changed paths; consult relevant rules/skills. Check commands and results; reuse execution proven for the same code/configuration/environment, running missing or invalidated checks.
   Run all TechSpec quality profile commands over the reviewable set, blocking and reservation, and subtract the Terrain baseline: debt that existed before the feature is not a finding of this review, and treating it as one hides what the implementation actually introduced. Conformance with acceptance and tests does not replace the profile: a diff that meets every obligation and still carries a new, unjustified blocking hit has a defect. A TechSpec without a quality profile or without a baseline is a gap recorded in limitations, not an empty profile or a zeroed baseline.
   In desktop C#/.NET, omit E2E and record the policy, including for inherited commands. Omission is neither a defect nor approved testing; check unit, integration, and manual acceptance evidence required by the TechSpec. Unexecuted essential manual work is `not verifiable`.
   **Output:** states supported by evidence or an explicit limitation; zero tests do not prove acceptance.
5. Number findings `CR-01`, `CR-02` within this review. Record origin, fact, file/symbol/line, impact, severity, and evidence. Recommend a correction only with a proven cause. Identify a prior finding by review path + ID and mark resolved, persistent, or not verifiable.
   A blocking profile hit becomes a finding with the `QA-NN` rule as its origin; a reservation hit becomes an optional improvement with the same traceability. Count the feature's reservations and, when a profile trigger fires, record in the escalation section the relevant skill and the number that justifies it — as a suggestion to the HIL, never executed in this review.
   **Output:** actionable findings distinct from optional improvements; all verifiable without conversation history; escalation suggested only with a counted trigger.
6. Read [references/TEMPLATE.md](references/TEMPLATE.md) in full when issuing the report. Reserve the next free numeric suffix under `codereview_[num]/`, considering all existing folders. Write a new `codereview.md`; preserve code, tasks, and previous reports.
   **Output:** immutable report with matrix, findings, validations, limitations, and status below; report path and blocks.

## Status

- `APPROVED`: all obligations conformant, tasks complete, links intact, required validations proven, and no unjustified blocking quality profile hit.
- `APPROVED WITH RESERVATIONS`: only optional improvements, including profile reservation hits, with no requirement, security, or essential evidence pending.
- `REJECTED`: any non-conformant/incomplete obligation, inconsistent state, failing mandatory test, missing essential evidence, or blocking profile hit not covered by `DEC-NN`.

If sources/code change during review, revalidate the affected part before the opinion. A missing environment records command, error, and affected IDs, without turning missing evidence into approval.
