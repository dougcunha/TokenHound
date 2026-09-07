# State and human decisions

## On-disk memory

Use `tasks/prd-[slug]/checkpoint.json` as the single operational index for resumption. `workflow.md` stores human decisions, authorizations, and recovery events with stable IDs; manifest + tasks + handoffs remain execution authority and reports preserve review history. The checkpoint references these sources without copying their contents.

When creating the first checkpoint, read [../assets/checkpoint.template.json](../assets/checkpoint.template.json) in full. Fill the template without creating a PRD or other files just to populate paths. Missing source fields are `null`; during execution, objective, feature, workspace, and next action are required and non-empty.

| Fields | Contract |
| --- | --- |
| `schema_version`, `generation` | Version 1; generation increases on every confirmed write |
| `feature`, `workspace`, `owner_session` | Slug, resolved absolute repository directory, and real session ID, or `null` when unavailable |
| `phase` | `product`, `plan-project`, `implementation`, `review`, `corrections`, `acceptance`, or `completed` |
| `status`, `safe_to_stop` | `active`, `awaiting-hil`, `paused`, `blocked`, or `completed`; safe to switch only without pending writers/processes and after persistence is verified |
| `objective`, `constraints` | Requested result and short indispensable invariants, including .NET desktop policy when applicable; details by reference |
| `sources` | Paths relative to the feature folder, or `null`; no artifact path outside it |
| `review_status` | Literal status of the latest `codereview.md` (`APPROVED`, `APPROVED WITH RESERVATIONS`, `REJECTED`), or `null` before the first review; `APPROVED` with no later code change requires no new review |
| `approved_sources` | Items `{path, sha256, decision_id}` linking approved content to the human decision in `workflow.md` |
| `decisions_to_read` | Decision IDs relevant to the next action; each record contains decision, scope, relevant human text, and available provenance |
| `git_base`, `worktree_evidence` | Resolved commit or `null`; reference to pre-existing/uncommitted changes in workflow or handoff, since HEAD alone does not identify state |
| `active_work` | Items `{kind, handle, owner, scope, state}` for real agents/processes; `handle: null` requires reconciliation and never assumes completion |
| `pending_hil`, `blockers` | Gate/question/affected IDs or `null`; blockers by ID, summary, and evidence reference |
| `correction_round`, `rounds_without_progress` | Counters preserved between sessions; rotation does not reset the stagnation limit |
| `next_action` | Type `delegate`, `validate`, `await-decision`, or `close`, a short instruction, and `read` list of `{path, section}` with paths relative to the workspace and required sections |

Keep the index preferably below 8 KiB: move details to referenced sources without truncating objective, constraints, blockers, or handles. Do not store conversations, complete PRD/TechSpec documents, or logs in JSON. The coordinator reads only the index and relevant decisions; technical documents go to the stage subagent. The checkpoint is persistent memory until resumption, not a disposable temporary-system file.

## Save and offer a pause

1. Update evidence and decisions in the authoritative file before pointing to them in the checkpoint. Save before each HIL, after the human response, when reconciling each batch, and when receiving a review. During active work, record handles and `safe_to_stop: false`; an abrupt interruption should recover this conservative state.
2. Before a strategic stop, stop scheduling batches. Wait for executors/processes to finish and receive handoff; close idle contexts when the host allows it. If work must be interrupted, confirm termination and record the partial diff and open items before allowing resumption. A timeout or inaccessible handle is not confirmation of termination.
3. Check paths inside the feature, version, types, required fields, and references. Write `checkpoint.json.tmp` in the same folder, parse the JSON, and check values. Preserve the last valid version in `checkpoint.previous.json`, replace the main file with a supported atomic rename/replacement, and reread it. A write failure keeps the last valid version and prevents announcing a ready checkpoint.
4. After operations complete and the save is verified, set `safe_to_stop: true` and offer **continue in this session** or **pause for a new session**. Combine the choice with any required HIL; between batches, ask only about continuation, without repeating technical approval. Gate and session choice are independent: pausing does not equal approval. Record responses and save again; silence leaves the gate/continuation awaiting and does not start a new batch.
5. If the choice is pause, save `status: paused`, preserve `pending_hil` if unanswered, and provide the resume instruction: `Use $sdd-orchestrate-flow to resume feature <slug> in this repository.` End the turn without new agents. If continuing, save `active` and proceed only with valid authorization; before delegating, set `safe_to_stop: false` and record handles as soon as they return.

**Complete when:** the checkpoint was reread, sources and next action were checked, and no writer remains active in a pause announced as safe. Do not promise to avoid compaction or automatic session changes; the human chooses the pause. Creating a checkpoint does not mean the feature is complete.

## HIL

| Gate | Material already prepared | Decision required |
| --- | --- | --- |
| HIL 1 | PRD with scope, acceptance, and assumptions | Approve the product or correct requirements |
| HIL 2 | TechSpec, DAG, tasks, risks, and validations | Approve the solution and execution, including corrections within the contract |
| Exception | Evidence, impact, and concrete proposal | Resolve scope/architecture deviation, indispensable environment, irreversible risk, or stagnation |
| Reservations | Summary of items reserved in the review, with impact and effort | Correct the chosen items or finalize the feature |
| HIL 3 | Final review, tests, open items, and manual acceptance | Accept the current delivery |

Use the available question tool or a textual question. Stop only work dependent on the answer; silence, elapsed time, or a subagent's approval do not equal consent. Explain which gate is missing and point to the artifacts. Reuse authorization already given for the same scope; do not ask twice to save a draft and then execute it.

If the user explicitly changes HIL stops, preserve the instruction and record the new autonomy scope. Missing evidence remains pending even when the user approves the solution. Human acceptance does not replace mandatory technical validation.

## Resumption and invalidation

1. On invocation with an explicit feature, read only its checkpoint. Without a feature, discover only `tasks/prd-*/checkpoint.json` and read metadata to select the sole incomplete one. With several, ask which to resume, offering first, among checkpoints sharing a prefix, the next pending slice in dependency order; an invalid checkpoint is a recovery candidate, not a reason to ignore the feature. If all are complete, report that state without restarting. Without a checkpoint, reconcile existing sources and `workflow.md` before creating the first one.
2. Validate JSON, version, fields, and workspace/feature. Preserve an invalid file; try `checkpoint.previous.json` only after validating and reconciling it with current files. If no version is valid, reconstruct state from available evidence while keeping uncertain authorizations pending. A different workspace requires confirming the destination, without executing inherited paths.
3. Read only the checkpoint and referenced decisions; check source existence/hashes without loading all content. Consult relevant manifest/handoff sections for the next action. Validate handles and real process state, including in a supposedly safe checkpoint, if another coordinator may exist. An active coordinator means executing/scheduling work, not merely an old idle conversation. If a writer is active or completion is uncertain, block new writes until reconciliation; do not create replacement executors in parallel. When sole coordinator, record the new owner; a later reactivated session must check that owner before scheduling.
4. Reuse artifacts and approvals with matching content and provenance without asking everything again. The human decision record preserves prior authorization; agent-produced text does not create new authorization. Inconsistent or unverifiable approval requires only the affected decision. `paused` does not prevent automatic resumption by a new invocation; `pending_hil` still requires a response, and `completed` only reports the result.
5. A material PRD change invalidates product approval and affected derivatives; a TechSpec change invalidates affected planning/execution; a code change invalidates corresponding test/review evidence. Preserve IDs and history. New scope requires HIL; an already authorized correction requires technical revalidation without restarting valid human gates.
6. For interrupted task movement, reconcile origin/destination, review, and handoff before fixing links/state. Preserve completed contracts and reports. A proven incorrect completion requires reopening by the DAG owner under `sdd-orchestrate-tasks`: preserve prior evidence in `Problems and solutions`, move it to the root, and update link/state while retaining IDs and contract. Record the correction link; complete again only after reviewing current evidence and affected dependents.
7. Recalculate the next action from the first invalid/missing evidence; the saved instruction does not override current files or a new request. Summarize phase and next step in one line and proceed without loading the full history. At finish, check all obligations, tasks/corrections, integrated evidence, HIL 3, and absence of essential pending validation; save `phase/status: completed`, `next_action.kind: close`, and preserve the checkpoint for reference.

**Complete when:** the next action has a current source, matching authorization, and one coordinator; or recovery identifies the specific missing evidence/decision. The checkpoint accelerates resumption but does not replace contracts or prove technical success by itself.
